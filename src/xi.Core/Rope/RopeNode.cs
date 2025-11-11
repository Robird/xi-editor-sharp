using System;
using System.Collections.Generic;
using System.Text;

namespace Xi.Core.Rope;

/// <summary>
/// Immutable rope node that mirrors the xi-editor rope tree structure.
/// Supports leaf and internal nodes, providing aggregation metadata for higher-level operations.
/// </summary>
public sealed class RopeNode
{
    public const int MinLeafSize = 511;
    public const int MaxLeafSize = 1024;

    private readonly RopeNodeBody _body;

    private RopeNode(RopeNodeBody body)
    {
        _body = body;
    }

    public static RopeNode Empty { get; } = new RopeNode(new RopeNodeBody(0, 0, RopeInfo.Identity, string.Empty, null));

    public int Height => _body.Height;

    public int Length => _body.Length;

    public RopeInfo Info => _body.Info;

    public bool IsLeaf => _body.Height == 0;

    public bool IsEmpty => Length == 0;

    public int ChildCount => _body.Children?.Length ?? 0;

    public IReadOnlyList<RopeNode> Children => _body.Children ?? Array.Empty<RopeNode>();

    public ReadOnlySpan<char> LeafSpan => _body.Leaf is null ? ReadOnlySpan<char>.Empty : _body.Leaf.AsSpan();

    public static RopeNode FromLeaf(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Empty;
        }

        var span = text.AsSpan();
        var info = RopeInfo.FromLeaf(span);
        return new RopeNode(new RopeNodeBody(0, span.Length, info, text, null));
    }

    public static RopeNode Concat(RopeNode left, RopeNode right)
    {
        if (left is null) throw new ArgumentNullException(nameof(left));
        if (right is null) throw new ArgumentNullException(nameof(right));

        if (left.IsEmpty)
        {
            return right;
        }

        if (right.IsEmpty)
        {
            return left;
        }

        if (left.Height == right.Height)
        {
            return CreateInternal(left.Height + 1, new[] { left, right });
        }

        if (left.Height < right.Height)
        {
            return ConcatLeftShorter(left, right);
        }

        return ConcatRightShorter(left, right);
    }

    public IEnumerable<RopeNode> TraverseLeaves()
    {
        if (IsLeaf)
        {
            yield return this;
            yield break;
        }

        if (_body.Children is null)
        {
            yield break;
        }

        foreach (var child in _body.Children)
        {
            foreach (var leaf in child.TraverseLeaves())
            {
                yield return leaf;
            }
        }
    }

    public RopeNode Slice(int start, int length)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start), start, "Start must be non-negative.");
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Length must be non-negative.");
        }

        if (start > Length || start + length > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Requested slice exceeds node bounds.");
        }

        if (length == 0)
        {
            return Empty;
        }

        if (start == 0 && length == Length)
        {
            return this;
        }

        var (_, remainder) = SplitAt(start);
        var (middle, _) = remainder.SplitAt(length);
        return middle;
    }

    public override string ToString()
    {
        if (_body.Leaf is { } leaf)
        {
            return leaf;
        }

        if (_body.Children is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var child in _body.Children)
        {
            builder.Append(child.ToString());
        }
        return builder.ToString();
    }

    public RopeNode Insert(int start, string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (start < 0 || start > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(start), start, "Start must be within node bounds.");
        }

        if (text.Length == 0)
        {
            return this;
        }

        var (prefix, suffix) = SplitAt(start);
        var builder = new TreeBuilder();
        builder.PushNode(prefix);
        builder.PushString(text);
        builder.PushNode(suffix);
        return builder.Build();
    }

    public RopeNode Delete(int start, int length)
    {
        if (length == 0)
        {
            return this;
        }

        if (start < 0 || length < 0 || start + length > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Deletion range must be within node bounds.");
        }

        if (length == Length)
        {
            return Empty;
        }

        var (prefix, remainder) = SplitAt(start);
        var (_, suffix) = remainder.SplitAt(length);

        if (prefix.IsEmpty)
        {
            return suffix;
        }

        if (suffix.IsEmpty)
        {
            return prefix;
        }

        return Concat(prefix, suffix);
    }

    public RopeNode EnsureWritableLeaf()
    {
        if (!IsLeaf)
        {
            throw new InvalidOperationException("EnsureWritableLeaf can only be called on leaf nodes.");
        }

        if (_body.Leaf is null)
        {
            return Empty;
        }

        var source = _body.Leaf;
        if (source.Length == 0)
        {
            return Empty;
        }

        var cloneText = string.Create(source.Length, source, static (span, s) => s.AsSpan().CopyTo(span));
        if (ReferenceEquals(source, cloneText))
        {
            return this;
        }

        return new RopeNode(new RopeNodeBody(0, cloneText.Length, _body.Info, cloneText, null));
    }

    public IReadOnlyList<RopeNode> SplitLeafByBounds()
    {
        if (!IsLeaf)
        {
            throw new InvalidOperationException("SplitLeafByBounds can only be called on leaf nodes.");
        }

        if (_body.Leaf is null)
        {
            return Array.Empty<RopeNode>();
        }

        if (_body.Leaf.Length <= MaxLeafSize)
        {
            return new[] { this };
        }

        var segments = new List<RopeNode>();
        foreach (var segment in LeafSplitter.Split(_body.Leaf))
        {
            segments.Add(FromLeaf(segment));
        }

        return segments;
    }

    public RopeNode CloneWithChildren(IReadOnlyList<RopeNode> newChildren)
    {
        if (newChildren is null)
        {
            throw new ArgumentNullException(nameof(newChildren));
        }

        if (IsLeaf)
        {
            throw new InvalidOperationException("Cannot clone children for a leaf node.");
        }

        if (newChildren.Count == 0)
        {
            throw new ArgumentException("Internal node must have at least one child.", nameof(newChildren));
        }

        var expectedChildHeight = Height - 1;
        var length = 0;
        var info = RopeInfo.Identity;
        var array = new RopeNode[newChildren.Count];

        for (var i = 0; i < newChildren.Count; i++)
        {
            var child = newChildren[i] ?? throw new ArgumentNullException(nameof(newChildren), "Child node cannot be null.");

            if (child.Height != expectedChildHeight)
            {
                throw new InvalidOperationException($"Child at index {i} has height {child.Height}, expected {expectedChildHeight}.");
            }

            length = checked(length + child.Length);
            info = info.Accumulate(child.Info);
            array[i] = child;
        }

        return new RopeNode(new RopeNodeBody(Height, length, info, null, array));
    }

    public RopeNode WithChildReplaced(int index, RopeNode newChild)
    {
        if (newChild is null)
        {
            throw new ArgumentNullException(nameof(newChild));
        }

        if (IsLeaf)
        {
            throw new InvalidOperationException("Cannot replace child on a leaf node.");
        }

        var children = RequireChildren();

        if ((uint)index >= (uint)children.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Child index must be within node bounds.");
        }

        if (ReferenceEquals(children[index], newChild))
        {
            return this;
        }

        var clone = new RopeNode[children.Length];
        Array.Copy(children, clone, children.Length);
        clone[index] = newChild;

        return CloneWithChildren(clone);
    }

    public (RopeNode Left, RopeNode Right) SplitAt(int index)
    {
        if (index < 0 || index > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Split index must be within node bounds.");
        }

        if (index == 0)
        {
            return (Empty, this);
        }

        if (index == Length)
        {
            return (this, Empty);
        }

        if (IsLeaf)
        {
            if (_body.Leaf is null)
            {
                return (Empty, Empty);
            }

            var leftText = _body.Leaf[..index];
            var rightText = _body.Leaf[index..];
            return (FromLeaf(leftText), FromLeaf(rightText));
        }

        var children = RequireChildren();
        var leftSegments = new List<RopeNode>();
        var rightSegments = new List<RopeNode>();
        var remaining = index;

        foreach (var child in children)
        {
            if (remaining == 0)
            {
                rightSegments.Add(child);
                continue;
            }

            if (remaining >= child.Length)
            {
                leftSegments.Add(child);
                remaining -= child.Length;
                continue;
            }

            var (childLeft, childRight) = child.SplitAt(remaining);
            if (!childLeft.IsEmpty)
            {
                leftSegments.Add(childLeft);
            }

            if (!childRight.IsEmpty)
            {
                rightSegments.Add(childRight);
            }

            remaining = 0;
        }

        var leftNode = BuildFromSegments(leftSegments);
        var rightNode = BuildFromSegments(rightSegments);
        return (leftNode, rightNode);
    }

    private static RopeNode ConcatLeftShorter(RopeNode left, RopeNode right)
    {
        var children = right.RequireChildren();
        var firstChild = children[0];
        var merged = Concat(left, firstChild);

        if (merged.Height == right.Height - 1)
        {
            var newChildren = new RopeNode[children.Length];
            newChildren[0] = merged;
            Array.Copy(children, 1, newChildren, 1, children.Length - 1);
            return CreateInternal(right.Height, newChildren);
        }

        if (merged.Height == right.Height)
        {
            var mergedChildren = merged.RequireChildren();
            var combined = new RopeNode[mergedChildren.Length + children.Length - 1];
            Array.Copy(mergedChildren, 0, combined, 0, mergedChildren.Length);
            Array.Copy(children, 1, combined, mergedChildren.Length, children.Length - 1);
            return CreateInternal(right.Height, combined);
        }

        throw new InvalidOperationException("Unexpected height relationship during rope concatenation.");
    }

    private static RopeNode ConcatRightShorter(RopeNode left, RopeNode right)
    {
        var children = left.RequireChildren();
        var lastIndex = children.Length - 1;
        var lastChild = children[lastIndex];
        var merged = Concat(lastChild, right);

        if (merged.Height == left.Height - 1)
        {
            var newChildren = new RopeNode[children.Length];
            Array.Copy(children, 0, newChildren, 0, lastIndex);
            newChildren[lastIndex] = merged;
            return CreateInternal(left.Height, newChildren);
        }

        if (merged.Height == left.Height)
        {
            var mergedChildren = merged.RequireChildren();
            var combined = new RopeNode[children.Length + mergedChildren.Length - 1];
            Array.Copy(children, 0, combined, 0, lastIndex);
            Array.Copy(mergedChildren, 0, combined, lastIndex, mergedChildren.Length);
            return CreateInternal(left.Height, combined);
        }

        throw new InvalidOperationException("Unexpected height relationship during rope concatenation.");
    }

    private RopeNode[] RequireChildren()
    {
        if (_body.Children is { Length: > 0 } children)
        {
            return children;
        }

        throw new InvalidOperationException("Operation requires an internal node with children.");
    }

    private static RopeNode CreateInternal(int height, IReadOnlyList<RopeNode> children)
    {
        if (children.Count == 0)
        {
            return Empty;
        }

        var expectedChildHeight = height - 1;
        var length = 0;
        var info = RopeInfo.Identity;
        var array = new RopeNode[children.Count];

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (child.Height != expectedChildHeight && height > 0)
            {
                throw new InvalidOperationException($"Child height {child.Height} does not match expected {expectedChildHeight}.");
            }

            array[i] = child;
            length = checked(length + child.Length);
            info = info.Accumulate(child.Info);
        }

        return new RopeNode(new RopeNodeBody(height, length, info, null, array));
    }

    private sealed record RopeNodeBody(int Height, int Length, RopeInfo Info, string? Leaf, RopeNode[]? Children);

    private static RopeNode BuildFromSegments(List<RopeNode> segments)
    {
        if (segments.Count == 0)
        {
            return Empty;
        }

        if (segments.Count == 1)
        {
            return segments[0];
        }

        var builder = new TreeBuilder();
        foreach (var segment in segments)
        {
            builder.PushNode(segment);
        }

        return builder.Build();
    }

}
