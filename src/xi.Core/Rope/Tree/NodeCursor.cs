using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xi.Core.Rope;

namespace Xi.Core.Rope.Tree;

/// <summary>
/// Cursor for efficient traversal of rope nodes with path caching.
/// Aligns with xi-editor's <c>Cursor</c> implementation (rust tree.rs ~1129-1450) and can optionally tie to a <see cref="Rope"/> to detect edit-version changes.
/// </summary>
public sealed class NodeCursor
{
    private const int CacheSizeLimit = 4; // Matches Rust CURSOR_CACHE_SIZE

    private readonly Node _root;
    private readonly Node _rootSharedNode; // Reference snapshot for invalidation detection
    private readonly Rope? _owner;
    private readonly long _capturedEditVersion;
    private bool _ownerVersionMismatch;
    private int _position;
    private readonly PathFrame?[] _pathCache;
    private string? _currentLeaf;
    private Node? _currentLeafNode;
    private int _offsetOfLeaf;
    private bool _isValid;

    public NodeCursor(Node root, int position)
        : this(root, position, owner: null, ownerVersionSnapshot: -1)
    {
    }

    public NodeCursor(Rope owner, int position)
        : this((owner ?? throw new ArgumentNullException(nameof(owner))).DebugRoot, position, owner, owner.EditVersion)
    {
    }

    private NodeCursor(Node root, int position, Rope? owner, long ownerVersionSnapshot)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        _rootSharedNode = root;
        _owner = owner;
        _capturedEditVersion = ownerVersionSnapshot;

        if (position < 0 || position > root.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(position),
                $"Position {position} is out of range [0, {root.Length}].");
        }

        _position = position;
        _pathCache = new PathFrame?[CacheSizeLimit];
        Descend();
    }

    public Node Root => _root;

    public int Position => _position;

    public int TotalLength => _root.Length;

    public bool IsValid => _isValid;

    private bool EnsureOwnerVersionMatches()
    {
        if (_owner is null || _ownerVersionMismatch)
        {
            return !_ownerVersionMismatch;
        }

        if (_owner.EditVersion == _capturedEditVersion)
        {
            return true;
        }

        _ownerVersionMismatch = true;
        Invalidate();
        return false;
    }

    public (string Leaf, int Offset)? GetLeaf()
    {
        if (!EnsureOwnerVersionMatches())
        {
            return null;
        }

        if (!_isValid || _currentLeaf == null)
        {
            return null;
        }

        return (_currentLeaf, _position - _offsetOfLeaf);
    }

    public void SetPosition(int position)
    {
        if (!EnsureOwnerVersionMatches())
        {
            throw new InvalidOperationException("Rope has changed since the cursor was constructed. Create a new cursor to continue.");
        }

        AssertRootStable();

        if (position < 0 || position > _root.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(position),
                $"Position {position} is out of range [0, {_root.Length}].");
        }

        _position = position;

        if (_isValid && _currentLeaf != null &&
            position >= _offsetOfLeaf &&
            position < _offsetOfLeaf + _currentLeaf.Length)
        {
            return;
        }

        Descend();
    }

    public bool IsBoundary(IMetric metric)
    {
        if (!EnsureOwnerVersionMatches())
        {
            return false;
        }

        AssertRootStable();

        if (metric == null)
        {
            throw new ArgumentNullException(nameof(metric));
        }

        if (!_isValid || _currentLeaf == null)
        {
            return false;
        }

        if ((_position == 0 || _position == _root.Length) && !metric.CanFragment)
        {
            return true;
        }

        if (_position == _offsetOfLeaf && _position > 0)
        {
            var prevLeaf = PeekPrevLeaf();
            return prevLeaf != null && metric.IsBoundary(prevLeaf, prevLeaf.Length);
        }

        return metric.IsBoundary(_currentLeaf, _position - _offsetOfLeaf);
    }

    public int? MoveToPrevious(IMetric metric)
    {
        if (!EnsureOwnerVersionMatches())
        {
            return null;
        }

        AssertRootStable();

        if (metric == null)
        {
            throw new ArgumentNullException(nameof(metric));
        }

        if (_position == 0)
        {
            InvalidateToStart();
            return null;
        }

        if (!_isValid)
        {
            Descend();
            if (!_isValid)
            {
                InvalidateToStart();
                return null;
            }
        }

        int originalPosition = _position;
        int offsetInLeaf = originalPosition - _offsetOfLeaf;

        var previousInside = PreviousInsideLeaf(metric, offsetInLeaf);
        if (previousInside.HasValue)
        {
            return previousInside.Value;
        }

        if (!PrevLeaf())
        {
            InvalidateToStart();
            return null;
        }

        var candidate = LastInsideLeaf(metric, originalPosition);
        if (candidate.HasValue)
        {
            return candidate.Value;
        }

        int measure = MeasureLeaf(metric, _position);
        if (measure == 0)
        {
            InvalidateToStart();
            return null;
        }

        DescendMetric(metric, measure);
        candidate = LastInsideLeaf(metric, originalPosition);
        if (candidate.HasValue)
        {
            return candidate.Value;
        }

        InvalidateToStart();
        return null;
    }

    public int? MoveToNext(IMetric metric)
    {
        if (!EnsureOwnerVersionMatches())
        {
            return null;
        }

        AssertRootStable();

        if (metric == null)
        {
            throw new ArgumentNullException(nameof(metric));
        }

        if (_position >= _root.Length || !_isValid)
        {
            InvalidateToEnd();
            return null;
        }

        var inside = NextInsideLeaf(metric);
        if (inside.HasValue)
        {
            return inside.Value;
        }

        if (!NextLeaf())
        {
            InvalidateToEnd();
            return null;
        }

        inside = NextInsideLeaf(metric);
        if (inside.HasValue)
        {
            return inside.Value;
        }

        int measure = MeasureLeaf(metric, _position);
        DescendMetric(metric, checked(measure + 1));

        inside = NextInsideLeaf(metric);
        if (inside.HasValue)
        {
            return inside.Value;
        }

        InvalidateToEnd();
        return null;
    }

    public int? AtOrNext(IMetric metric)
    {
        if (!EnsureOwnerVersionMatches())
        {
            return null;
        }

        return IsBoundary(metric) ? _position : MoveToNext(metric);
    }

    public int? AtOrPrevious(IMetric metric)
    {
        if (!EnsureOwnerVersionMatches())
        {
            return null;
        }

        return IsBoundary(metric) ? _position : MoveToPrevious(metric);
    }

    /// <summary>
    /// Captures an owning descriptor that mirrors xi-editor's <c>CursorDescriptor</c> snapshot semantics.
    /// </summary>
    public CursorDescriptor ToDescriptor()
    {
        if (!EnsureOwnerVersionMatches())
        {
            return CursorDescriptor.CreateInvalid(_position);
        }

        if (!_isValid || _currentLeafNode is null)
        {
            return CursorDescriptor.CreateInvalid(_position);
        }

        var frames = BuildDescriptorFrames();
        return CursorDescriptor.CreateValid(_position, _offsetOfLeaf, _currentLeafNode, frames);
    }

    /// <summary>
    /// Attempts to rehydrate cursor caches from a descriptor. Leaves the cursor unchanged when validation fails.
    /// </summary>
    public bool TryApplyDescriptor(CursorDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (!EnsureOwnerVersionMatches())
        {
            return false;
        }

        if (!descriptor.IsValid)
        {
            return false;
        }

        var snapshot = CaptureSnapshot();
        if (ApplyDescriptorInternal(descriptor))
        {
            return true;
        }

        RestoreSnapshot(snapshot);
        return false;
    }

    private void Descend()
    {
        if (!EnsureOwnerVersionMatches())
        {
            return;
        }

        AssertRootStable();

        _position = Math.Min(_position, _root.Length);
        ClearCache();
        _currentLeaf = null;
        _isValid = false;

        Node current = _root;
        int offset = 0;
        int target = _position;

        while (!current.IsLeaf)
        {
            var children = RequireChildren(current);
            int childIndex = 0;

            while (childIndex + 1 < children.Length)
            {
                int nextOffset = offset + children[childIndex].Length;
                if (nextOffset > target)
                {
                    break;
                }

                offset = nextOffset;
                childIndex++;
            }

            int cacheIndex = current.Height - 1;
            if (cacheIndex < CacheSizeLimit)
            {
                _pathCache[cacheIndex] = new PathFrame(current, childIndex);
            }

            current = children[childIndex];
        }

        SetLeafFromNode(current, offset);
    }

    private bool PrevLeaf()
    {
        if (!_isValid || _currentLeaf == null)
        {
            return false;
        }

        if (_offsetOfLeaf == 0)
        {
            InvalidateToStart();
            return false;
        }

        // Mirrors Rust prev_leaf (tree.rs ~1411-1445)
        for (int i = 0; i < CacheSizeLimit; i++)
        {
            if (!_pathCache[i].HasValue)
            {
                break;
            }

            var frame = _pathCache[i]!.Value;
            if (frame.ChildIndex > 0)
            {
                var updatedFrame = frame.WithChildIndex(frame.ChildIndex - 1);
                _pathCache[i] = updatedFrame;
                Node nodeDown = RequireChildren(frame.Node)[updatedFrame.ChildIndex];

                for (int k = i - 1; k >= 0; k--)
                {
                    var downChildren = RequireChildren(nodeDown);
                    int lastIndex = downChildren.Length - 1;
                    _pathCache[k] = new PathFrame(nodeDown, lastIndex);
                    nodeDown = downChildren[lastIndex];
                }

                int newOffset = _offsetOfLeaf - nodeDown.Length;
                _position = newOffset;
                SetLeafFromNode(nodeDown, newOffset);
                return true;
            }
        }

        _position = _offsetOfLeaf - 1;
        Descend();
        _position = _offsetOfLeaf;
        return _isValid;
    }

    private bool NextLeaf()
    {
        if (!_isValid || _currentLeaf == null)
        {
            return false;
        }

        int newOffset = _offsetOfLeaf + _currentLeaf.Length;
        _position = newOffset;

        // Mirrors Rust next_leaf (tree.rs ~1376-1409)
        for (int i = 0; i < CacheSizeLimit; i++)
        {
            if (!_pathCache[i].HasValue)
            {
                break;
            }

            var frame = _pathCache[i]!.Value;
            var children = RequireChildren(frame.Node);
            if (frame.ChildIndex + 1 < children.Length)
            {
                var updatedFrame = frame.WithChildIndex(frame.ChildIndex + 1);
                _pathCache[i] = updatedFrame;
                Node nodeDown = children[updatedFrame.ChildIndex];

                for (int k = i - 1; k >= 0; k--)
                {
                    var downChildren = RequireChildren(nodeDown);
                    _pathCache[k] = new PathFrame(nodeDown, 0);
                    nodeDown = downChildren[0];
                }

                SetLeafFromNode(nodeDown, newOffset);
                return true;
            }
        }

        if (newOffset == _root.Length)
        {
            InvalidateToEnd();
            return false;
        }

        Descend();
        return _isValid;
    }

    private string? PeekPrevLeaf()
    {
        if (!_isValid || _currentLeaf == null)
        {
            return null;
        }

        var snapshot = CaptureSnapshot();
        if (!PrevLeaf())
        {
            RestoreSnapshot(snapshot);
            return null;
        }

        string? prevLeaf = _currentLeaf;
        RestoreSnapshot(snapshot);
        return prevLeaf;
    }

    private int? LastInsideLeaf(IMetric metric, int originalPosition)
    {
        if (!_isValid || _currentLeaf == null)
        {
            return null;
        }

        int leafLength = _currentLeaf.Length;
        int leafEnd = _offsetOfLeaf + leafLength;

        if (leafEnd < originalPosition && metric.IsBoundary(_currentLeaf, leafLength))
        {
            if (NextLeaf())
            {
                return _position;
            }

            return null;
        }

        int? offsetInLeaf = metric.GetPreviousBoundary(_currentLeaf, leafLength);
        if (!offsetInLeaf.HasValue)
        {
            return null;
        }

        _position = _offsetOfLeaf + offsetInLeaf.Value;
        return _position;
    }

    private int? NextInsideLeaf(IMetric metric)
    {
        if (!_isValid || _currentLeaf == null)
        {
            return null;
        }

        int offsetInLeaf = _position - _offsetOfLeaf;
        int? next = metric.GetNextBoundary(_currentLeaf, offsetInLeaf);
        if (!next.HasValue)
        {
            return null;
        }

        int candidate = next.Value;
        int absolute = _offsetOfLeaf + candidate;

        if (candidate == _currentLeaf.Length && absolute != _root.Length)
        {
            return NextLeaf() ? _position : null;
        }

        _position = absolute;
        if (_position == _root.Length)
        {
            InvalidateToEnd();
        }

        return _position;
    }

    private int? PreviousInsideLeaf(IMetric metric, int offsetInLeaf)
    {
        if (!_isValid || _currentLeaf == null || offsetInLeaf <= 0)
        {
            return null;
        }

        int searchOffset = offsetInLeaf;
        while (searchOffset > 0)
        {
            int? prev = metric.GetPreviousBoundary(_currentLeaf, searchOffset);
            if (!prev.HasValue)
            {
                return null;
            }

            int absolute = _offsetOfLeaf + prev.Value;
            if (absolute < _position)
            {
                _position = absolute;
                return _position;
            }

            searchOffset = prev.Value;
            if (searchOffset == 0)
            {
                break;
            }

            searchOffset--;
        }

        return null;
    }

    private int MeasureLeaf(IMetric metric, int position)
    {
        var node = _root;
        int metricValue = 0;
        int remaining = Math.Min(position, _root.Length);

        while (!node.IsLeaf)
        {
            var children = RequireChildren(node);

            foreach (var child in children)
            {
                int childLength = child.Length;
                if (remaining < childLength)
                {
                    node = child;
                    break;
                }

                remaining -= childLength;
                metricValue = checked(metricValue + metric.Measure(child.Info, child.Length));
            }
        }

        return metricValue;
    }

    private void DescendMetric(IMetric metric, int measure)
    {
        if (!EnsureOwnerVersionMatches())
        {
            return;
        }

        AssertRootStable();

        if (measure < 0)
        {
            measure = 0;
        }

        ClearCache();

        var node = _root;
        int offset = 0;
        int remainingMeasure = measure;

        while (!node.IsLeaf)
        {
            var children = RequireChildren(node);
            int childIndex = 0;
            int lastIndex = children.Length - 1;

            while (childIndex < lastIndex)
            {
                var child = children[childIndex];
                int childMeasure = metric.Measure(child.Info, child.Length);
                if (childMeasure >= remainingMeasure)
                {
                    break;
                }

                offset = checked(offset + child.Length);
                remainingMeasure -= childMeasure;
                childIndex++;
            }

            int cacheIndex = node.Height - 1;
            if (cacheIndex < CacheSizeLimit)
            {
                _pathCache[cacheIndex] = new PathFrame(node, childIndex);
            }

            node = children[childIndex];
        }

        _position = offset;
        SetLeafFromNode(node, offset);
    }

    private CursorSnapshot CaptureSnapshot()
    {
        var cacheCopy = new PathFrame?[CacheSizeLimit];
        Array.Copy(_pathCache, cacheCopy, CacheSizeLimit);
        return new CursorSnapshot(_position, _offsetOfLeaf, _currentLeaf, _currentLeafNode, _isValid, cacheCopy);
    }

    private void RestoreSnapshot(CursorSnapshot snapshot)
    {
        _position = snapshot.Position;
        _offsetOfLeaf = snapshot.OffsetOfLeaf;
        _currentLeaf = snapshot.CurrentLeaf;
        _currentLeafNode = snapshot.CurrentLeafNode;
        _isValid = snapshot.IsValid;
        Array.Copy(snapshot.Cache, _pathCache, CacheSizeLimit);
    }

    private void SetLeafFromNode(Node leafNode, int offset)
    {
        _currentLeafNode = leafNode ?? throw new ArgumentNullException(nameof(leafNode));
        _currentLeaf = leafNode.GetLeaf() ?? string.Empty;
        _offsetOfLeaf = offset;
        _isValid = true;
    }

    private static Node[] RequireChildren(Node node)
    {
        var children = node.GetChildren();
        if (children == null || children.Length == 0)
        {
            throw new InvalidOperationException("Internal node must have children.");
        }

        return children;
    }

    private void ClearCache()
    {
        for (int i = 0; i < _pathCache.Length; i++)
        {
            _pathCache[i] = null;
        }

        _currentLeafNode = null;
    }

    private IReadOnlyList<CursorDescriptorFrame> BuildDescriptorFrames()
    {
        var frames = new List<CursorDescriptorFrame>();
        var current = _root;
        int absoluteOffset = 0;
        int target = Math.Min(_position, _root.Length);

        while (!current.IsLeaf)
        {
            var children = RequireChildren(current);
            int childIndex = 0;
            int childOffset = 0;

            while (childIndex + 1 < children.Length)
            {
                int nextOffset = childOffset + children[childIndex].Length;
                if (absoluteOffset + nextOffset > target)
                {
                    break;
                }

                childOffset = nextOffset;
                childIndex++;
            }

            frames.Add(new CursorDescriptorFrame(current, childIndex, childOffset));
            absoluteOffset += childOffset;
            current = children[childIndex];
        }

        return frames;
    }

    private bool ApplyDescriptorInternal(CursorDescriptor descriptor)
    {
        if (descriptor is null || !descriptor.IsValid)
        {
            return false;
        }

        if (descriptor.Position > _root.Length)
        {
            return false;
        }

        if (descriptor.OffsetOfLeaf > descriptor.Position)
        {
            return false;
        }

        var frames = descriptor.Frames;
        Node current = _root;
        int accumulatedOffset = 0;

        ClearCache();

        foreach (var frame in frames)
        {
            if (!ReferenceEquals(frame.Node, current))
            {
                return false;
            }

            var children = RequireChildren(current);
            if ((uint)frame.ChildIndex >= (uint)children.Length)
            {
                return false;
            }

            if (!TryCalculateChildOffset(current, frame.ChildIndex, out int computedOffset))
            {
                return false;
            }
            if (computedOffset != frame.ChildOffset)
            {
                return false;
            }

            int cacheIndex = current.Height - 1;
            if (cacheIndex < CacheSizeLimit)
            {
                _pathCache[cacheIndex] = new PathFrame(current, frame.ChildIndex);
            }

            accumulatedOffset += computedOffset;
            current = children[frame.ChildIndex];
        }

        if (!ReferenceEquals(descriptor.LeafNode, current))
        {
            return false;
        }

        if (accumulatedOffset != descriptor.OffsetOfLeaf)
        {
            return false;
        }

        int offsetInLeaf = descriptor.Position - descriptor.OffsetOfLeaf;
        var leaf = current.GetLeaf() ?? string.Empty;
        if (offsetInLeaf > leaf.Length)
        {
            return false;
        }

        _position = descriptor.Position;
        _offsetOfLeaf = descriptor.OffsetOfLeaf;
        _currentLeafNode = current;
        _currentLeaf = leaf;
        _isValid = true;

        return true;
    }

    private static bool TryCalculateChildOffset(Node parent, int childIndex, out int offset)
    {
        var children = parent.GetChildren();
        if (children == null || childIndex < 0 || childIndex > children.Length)
        {
            offset = 0;
            return false;
        }

        int result = 0;
        for (int i = 0; i < childIndex; i++)
        {
            result += children[i].Length;
        }

        offset = result;
        return true;
    }

    private void Invalidate()
    {
        _isValid = false;
        _currentLeaf = null;
        ClearCache();
    }

    private void InvalidateToStart()
    {
        _position = 0;
        _offsetOfLeaf = 0;
        Invalidate();
    }

    private void InvalidateToEnd()
    {
        _position = Math.Min(_position, _root.Length);
        _offsetOfLeaf = _position;
        Invalidate();
    }

    [Conditional("DEBUG")]
    private void AssertRootStable()
    {
        Debug.Assert(ReferenceEquals(_rootSharedNode, _root), "Cursor root reference changed unexpectedly.");
    }

    private readonly struct PathFrame
    {
        public PathFrame(Node node, int childIndex)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            ChildIndex = childIndex;
        }

        public Node Node { get; }
        public int ChildIndex { get; }

        public PathFrame WithChildIndex(int childIndex) => new(Node, childIndex);
    }

    private readonly struct CursorSnapshot
    {
        public CursorSnapshot(int position, int offsetOfLeaf, string? currentLeaf, Node? currentLeafNode, bool isValid, PathFrame?[] cache)
        {
            Position = position;
            OffsetOfLeaf = offsetOfLeaf;
            CurrentLeaf = currentLeaf;
            CurrentLeafNode = currentLeafNode;
            IsValid = isValid;
            Cache = cache;
        }

        public int Position { get; }
        public int OffsetOfLeaf { get; }
        public string? CurrentLeaf { get; }
        public Node? CurrentLeafNode { get; }
        public bool IsValid { get; }
        public PathFrame?[] Cache { get; }
    }
}
