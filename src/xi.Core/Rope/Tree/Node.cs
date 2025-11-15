using System;
using System.Collections.Generic;
using System.Text;

using Xi.Core.Rope;

namespace Xi.Core.Rope.Tree;

// ================================================================================
// ⚠️ M3-M4 过渡期警告 ⚠️
// ================================================================================
// 本类为字符串特化实现（Node），M4 将全面切换至泛型版本 Node<TInfo,TLeaf,TLeafOps>
//
// 过渡期约束：
// 1. 禁止新增字符串专属 API（如 ToString()、LeafSpan 等扩展方法）
// 2. 新功能应在 Node.Generic.cs 实现或通过 ILeafOperations 接口提供
// 3. 外部代码应使用 TypeAliases.cs 中的 RopeNode 别名，避免硬编码 Node 类型
//
// M4 切换清单：
// - 修改 TypeAliases.cs 启用泛型版本
// - 验证所有 106+ 项测试通过
// - 将本类标记为 [Obsolete] 或删除
// ================================================================================

/// <summary>
/// Immutable rope node that mirrors the xi-editor rope tree structure.
/// Supports leaf and internal nodes, providing aggregation metadata for higher-level operations.
/// </summary>
public sealed class Node
{
    public static int MinLeafSize => StringLeafOperations.MinLeafSize;
    public static int MaxLeafSize => StringLeafOperations.MaxLeafSize;

    private readonly SharedNode _shared;

    private Node(NodeBody body)
        : this(new SharedNode(body))
    {
    }

    private Node(SharedNode shared)
    {
        _shared = shared;
    }

    private NodeBody Body => _shared.Body;

    private static Node FromShared(SharedNode shared)
    {
        return new Node(shared);
    }

    public static Node Empty { get; } = new Node(SharedNode.Empty);

    private sealed class NodeBody
    {
        public NodeBody(int height, int length, RopeInfo info, string? leaf, Node[]? children)
        {
            Height = height;
            Length = length;
            Info = info;
            Leaf = leaf;
            Children = children;
        }

        public int Height { get; }
        public int Length { get; }
        public RopeInfo Info { get; }
        public string? Leaf { get; }
        public Node[]? Children { get; }

        public NodeBody Clone(Node[]? overrideChildren = null)
        {
            Node[]? nextChildren = overrideChildren;
            if (nextChildren is null && Children is { } existing)
            {
                nextChildren = (Node[])existing.Clone();
            }

            return new NodeBody(Height, Length, Info, Leaf, nextChildren);
        }
    }

    private sealed class SharedNode
    {
        private readonly NodeBody _body;

        public SharedNode(NodeBody body)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public static SharedNode Empty { get; } = new SharedNode(new NodeBody(0, 0, RopeInfo.Identity, string.Empty, null));

        public NodeBody Body => _body;
        public int Height => _body.Height;
        public int Length => _body.Length;
        public RopeInfo Info => _body.Info;
        public string? Leaf => _body.Leaf;
        public Node[]? Children => _body.Children;

        public SharedNode EnsureUnique()
        {
            return new SharedNode(_body.Clone());
        }

        public SharedNode CloneWithChildren(IReadOnlyList<Node> newChildren)
        {
            if (newChildren is null)
            {
                throw new ArgumentNullException(nameof(newChildren));
            }

            if (_body.Height == 0)
            {
                throw new InvalidOperationException("Cannot clone children for a leaf node.");
            }

            var (length, info, array) = MaterializeChildren(_body.Height, newChildren);
            return new SharedNode(new NodeBody(_body.Height, length, info, null, array));
        }

        public SharedNode ReplaceChildRange(int index, int removeCount, IReadOnlyList<Node> replacements)
        {
            if (replacements is null)
            {
                throw new ArgumentNullException(nameof(replacements));
            }

            if (_body.Children is null)
            {
                throw new InvalidOperationException("Cannot replace children on a leaf node.");
            }

            var source = _body.Children;

            if ((uint)index > (uint)source.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (removeCount < 0 || index + removeCount > source.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(removeCount));
            }

            var replacementCount = replacements.Count;
            var newCount = source.Length - removeCount + replacementCount;

            if (newCount == 0)
            {
                throw new InvalidOperationException("Internal node cannot have zero children.");
            }

            var newChildren = new Node[newCount];

            if (index > 0)
            {
                Array.Copy(source, 0, newChildren, 0, index);
            }

            for (var i = 0; i < replacementCount; i++)
            {
                var child = replacements[i] ?? throw new ArgumentNullException(nameof(replacements), "Child node cannot be null.");
                newChildren[index + i] = child;
            }

            var trailing = source.Length - index - removeCount;
            if (trailing > 0)
            {
                Array.Copy(source, index + removeCount, newChildren, index + replacementCount, trailing);
            }

            var (length, info) = AggregateChildren(_body.Height, newChildren);
            return new SharedNode(new NodeBody(_body.Height, length, info, null, newChildren));
        }

        public static SharedNode FromInternal(int height, IReadOnlyList<Node> children)
        {
            var (length, info, array) = MaterializeChildren(height, children);
            return new SharedNode(new NodeBody(height, length, info, null, array));
        }

        private static (int Length, RopeInfo Info, Node[] Array) MaterializeChildren(int parentHeight, IReadOnlyList<Node> children)
        {
            if (children.Count == 0)
            {
                throw new ArgumentException("Internal node must have at least one child.", nameof(children));
            }

            var expectedChildHeight = parentHeight - 1;
            if (expectedChildHeight < 0)
            {
                throw new InvalidOperationException("Parent height must be greater than zero for internal nodes.");
            }

            var length = 0;
            var info = RopeInfo.Identity;
            var array = new Node[children.Count];

            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i] ?? throw new ArgumentNullException(nameof(children), "Child node cannot be null.");
                if (child.Height != expectedChildHeight)
                {
                    throw new InvalidOperationException($"Child at index {i} has height {child.Height}, expected {expectedChildHeight}.");
                }

                length = checked(length + child.Length);
                info = info.Accumulate(child.Info);
                array[i] = child;
            }

            return (length, info, array);
        }

        private static (int Length, RopeInfo Info) AggregateChildren(int parentHeight, Node[] children)
        {
            var expectedChildHeight = parentHeight - 1;
            var length = 0;
            var info = RopeInfo.Identity;

            for (var i = 0; i < children.Length; i++)
            {
                var child = children[i] ?? throw new InvalidOperationException("Child node cannot be null.");

                if (expectedChildHeight < 0 || child.Height != expectedChildHeight)
                {
                    throw new InvalidOperationException($"Child at index {i} has height {child.Height}, expected {expectedChildHeight}.");
                }

                length = checked(length + child.Length);
                info = info.Accumulate(child.Info);
            }

            return (length, info);
        }
    }

    public int Height => Body.Height;

    public int Length => Body.Length;

    public RopeInfo Info => Body.Info;

    public bool IsLeaf => Body.Height == 0;

    public bool IsEmpty => Length == 0;

    public int ChildCount => Body.Children?.Length ?? 0;

    public IReadOnlyList<Node> Children => Body.Children ?? Array.Empty<Node>();

    public ReadOnlySpan<char> LeafSpan => Body.Leaf is null ? ReadOnlySpan<char>.Empty : Body.Leaf.AsSpan();

    internal int ConvertFromDefaultMetric(IMetric metric, int offset)
    {
        if (metric is null)
        {
            throw new ArgumentNullException(nameof(metric));
        }

        if ((uint)offset > (uint)Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "Offset must be within node bounds.");
        }

        if (offset == 0 || Length == 0)
        {
            return 0;
        }

        return ConvertMetrics(this, offset, BaseMetric.Instance, metric);
    }

    internal int ConvertToDefaultMetric(IMetric metric, int value)
    {
        if (metric is null)
        {
            throw new ArgumentNullException(nameof(metric));
        }

        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Metric coordinate must be non-negative.");
        }

        if (value == 0 || Length == 0)
        {
            return 0;
        }

        return ConvertMetrics(this, value, metric, BaseMetric.Instance);
    }

    private static int ConvertMetrics(Node node, int value, IMetric fromMetric, IMetric toMetric)
    {
        if (value == 0)
        {
            return 0;
        }

        var remaining = value;
        var accumulated = 0;
        var current = node;
        var fudge = fromMetric.CanFragment ? 1 : 0;

        while (!current.IsLeaf)
        {
            var children = current.RequireChildren();
            var foundChild = false;

            foreach (var child in children)
            {
                var childFrom = fromMetric.Measure(child.Info, child.Length);
                if (remaining < childFrom + fudge)
                {
                    current = child;
                    foundChild = true;
                    break;
                }

                accumulated = checked(accumulated + toMetric.Measure(child.Info, child.Length));
                remaining -= childFrom;
            }

            if (!foundChild)
            {
                current = children[^1];
                remaining = 0;
            }
        }

        var leafMeasure = fromMetric.Measure(current.Info, current.Length);
        if (remaining > leafMeasure)
        {
            remaining = leafMeasure;
        }

        var leafText = GetLeafText(current);
        var baseUnits = fromMetric.ToBaseUnits(leafText, remaining);
        var measured = toMetric.FromBaseUnits(leafText, baseUnits);
        return checked(accumulated + measured);
    }

    public static Node FromLeaf(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Empty;
        }

        var span = text.AsSpan();
        var info = RopeInfo.FromLeaf(span);
        return new Node(new NodeBody(0, span.Length, info, text, null));
    }

    public static Node Concat(Node left, Node right)
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

    public IEnumerable<Node> TraverseLeaves()
    {
        if (IsLeaf)
        {
            yield return this;
            yield break;
        }

        if (Body.Children is null)
        {
            yield break;
        }

        foreach (var child in Body.Children)
        {
            foreach (var leaf in child.TraverseLeaves())
            {
                yield return leaf;
            }
        }
    }

    public Node Slice(int start, int length)
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
        if (Body.Leaf is { } leaf)
        {
            return leaf;
        }

        if (Body.Children is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var child in Body.Children)
        {
            builder.Append(child.ToString());
        }
        return builder.ToString();
    }

    public Node Insert(int start, string text)
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

        if (TryInsertInSingleLeaf(start, text, out var optimized, out var splitNodes))
        {
            if (splitNodes is not null)
            {
                return BuildFromSegments(new List<Node>(splitNodes)).NormalizeLeafMinimum();
            }

            return optimized;
        }

        var (prefix, suffix) = SplitAt(start);
        var builder = new TreeBuilder();
        builder.PushNode(prefix);
        builder.PushString(text);
        builder.PushNode(suffix);
        return builder.Build().NormalizeLeafMinimum();
    }

    public Node Delete(int start, int length)
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

        if (TryDeleteInSingleSegment(start, length, out var optimized))
        {
            return optimized;
        }

        var (prefix, remainder) = SplitAt(start);
        var (_, suffix) = remainder.SplitAt(length);

        Node result;

        if (prefix.IsEmpty)
        {
            result = suffix;
        }
        else if (suffix.IsEmpty)
        {
            result = prefix;
        }
        else
        {
            result = Concat(prefix, suffix);
        }

        return result.NormalizeLeafMinimum();
    }

    public Node Replace(int start, int length, string? text)
    {
        if (text is null)
        {
            text = string.Empty;
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Length must be non-negative.");
        }

        if (start < 0 || start > Length || start + length > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(start), start, "Replacement range must be within node bounds.");
        }

        if (length == 0 && text.Length == 0)
        {
            return this;
        }

        if (length == 0)
        {
            return Insert(start, text);
        }

        if (text.Length == 0)
        {
            return Delete(start, length);
        }

        if (TryReplaceInSingleSegment(start, length, text, out var optimized, out var splitNodes))
        {
            if (splitNodes is not null)
            {
                return BuildFromSegments(new List<Node>(splitNodes)).NormalizeLeafMinimum();
            }

            return optimized;
        }

        var afterDelete = Delete(start, length);
        return afterDelete.Insert(start, text).NormalizeLeafMinimum();
    }

    public Node EnsureWritableLeaf()
    {
        if (!IsLeaf)
        {
            throw new InvalidOperationException("EnsureWritableLeaf can only be called on leaf nodes.");
        }

        if (Body.Leaf is null)
        {
            return Empty;
        }

        var source = Body.Leaf;
        if (source.Length == 0)
        {
            return Empty;
        }

        var cloneText = StringLeafOperations.Clone(source);
        if (ReferenceEquals(source, cloneText))
        {
            return this;
        }

        return new Node(new NodeBody(0, cloneText.Length, Body.Info, cloneText, null));
    }

    public IReadOnlyList<Node> SplitLeafByBounds()
    {
        if (!IsLeaf)
        {
            throw new InvalidOperationException("SplitLeafByBounds can only be called on leaf nodes.");
        }

        if (Body.Leaf is null)
        {
            return Array.Empty<Node>();
        }

        if (Body.Leaf.Length <= MaxLeafSize)
        {
            return new[] { this };
        }

        var segments = new List<Node>();
        foreach (var segment in StringLeafOperations.SplitByCapacity(Body.Leaf))
        {
            segments.Add(FromLeaf(segment));
        }

        return segments;
    }

    private bool TryInsertInSingleLeaf(int start, string text, out Node result, out IReadOnlyList<Node>? splitNodes)
    {
        splitNodes = null;

        if (IsLeaf)
        {
            var leafText = Body.Leaf ?? string.Empty;

            if (start < 0 || start > leafText.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            var newLeafText = StringLeafOperations.Insert(leafText, start, text);
            var newLeaf = FromLeaf(newLeafText);

            if (newLeaf.Length <= MaxLeafSize)
            {
                result = newLeaf;
                return true;
            }

            splitNodes = newLeaf.SplitLeafByBounds();

            if (splitNodes.Count == 1)
            {
                result = splitNodes[0];
                splitNodes = null;
                return true;
            }

            result = Empty;
            return true;
        }

        var children = RequireChildren();
        var offset = 0;

        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var childStart = offset;
            var childEnd = offset + child.Length;

            var isLastChild = i == children.Length - 1;
            var belongsToChild = start < childEnd || (isLastChild && start == childEnd);

            if (!belongsToChild)
            {
                offset = childEnd;
                continue;
            }

            var relativeStart = Math.Min(start - childStart, child.Length);

            if (child.TryInsertInSingleLeaf(relativeStart, text, out var newChild, out var childSplitNodes))
            {
                if (childSplitNodes is not null)
                {
                    result = ReplaceChildWithSegments(children, i, childSplitNodes);
                    splitNodes = null;
                }
                else
                {
                    result = WithChildReplaced(i, newChild);
                }

                return true;
            }

            if (relativeStart == child.Length && !isLastChild)
            {
                offset = childEnd;
                continue;
            }

            break;
        }

        result = Empty;
        return false;
    }

    private bool TryDeleteInSingleSegment(int start, int length, out Node result)
    {
        if (IsLeaf)
        {
            var leafText = Body.Leaf ?? string.Empty;

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (start < 0 || start > leafText.Length || start + length > leafText.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (length == leafText.Length)
            {
                result = Empty;
                return true;
            }

            var updated = StringLeafOperations.RemoveRange(leafText, start, length);
            result = updated.Length == 0 ? Empty : FromLeaf(updated);
            return true;
        }

        var children = RequireChildren();
        var offset = 0;

        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var childStart = offset;
            var childEnd = offset + child.Length;

            var coversRange = start >= childStart && start + length <= childEnd;
            if (!coversRange)
            {
                offset = childEnd;
                continue;
            }

            var relativeStart = start - childStart;
            if (child.TryDeleteInSingleSegment(relativeStart, length, out var newChild))
            {
                if (newChild.IsEmpty)
                {
                    if (children.Length == 1)
                    {
                        result = Empty;
                        return true;
                    }

                    if (children.Length == 2)
                    {
                        result = i == 0 ? children[1] : children[0];
                        return true;
                    }

                    var shared = _shared.ReplaceChildRange(i, 1, Array.Empty<Node>());
                    result = FromShared(shared);
                    return true;
                }

                if (TryMergeLeafWithSibling(children, i, newChild, out var mergedResult))
                {
                    result = mergedResult;
                    return true;
                }

                if (newChild.IsLeaf && newChild.Length < MinLeafSize &&
                    TryRebalanceLeafWithSibling(children, i, newChild, out var rebalancedResult))
                {
                    result = rebalancedResult;
                    return true;
                }

                var replacement = new[] { newChild };
                var sharedWithReplacement = _shared.ReplaceChildRange(i, 1, replacement);
                result = FromShared(sharedWithReplacement);
                return true;
            }

            break;
        }

        result = Empty;
        return false;
    }

    private bool TryReplaceInSingleSegment(int start, int length, string text, out Node result, out IReadOnlyList<Node>? splitNodes)
    {
        splitNodes = null;

        if (IsLeaf)
        {
            var leafText = Body.Leaf ?? string.Empty;

            var newLeafText = StringLeafOperations.ReplaceRange(leafText, start, length, text);
            var newLeaf = FromLeaf(newLeafText);

            if (newLeaf.Length <= MaxLeafSize)
            {
                result = newLeaf;
                return true;
            }

            splitNodes = newLeaf.SplitLeafByBounds();

            if (splitNodes.Count == 1)
            {
                result = splitNodes[0];
                splitNodes = null;
                return true;
            }

            result = Empty;
            return true;
        }

        var children = RequireChildren();
        var offset = 0;
        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var childStart = offset;
            var childEnd = childStart + child.Length;
            var rangeEnd = start + length;

            var withinChild = start >= childStart && rangeEnd <= childEnd;
            if (!withinChild)
            {
                offset = childEnd;
                continue;
            }

            var relativeStart = start - childStart;
            if (child.TryReplaceInSingleSegment(relativeStart, length, text, out var newChild, out var childSplitNodes))
            {
                if (childSplitNodes is not null)
                {
                    result = ReplaceChildWithSegments(children, i, childSplitNodes);
                    splitNodes = null;
                }
                else
                {
                    if (TryMergeLeafWithSibling(children, i, newChild, out var merged))
                    {
                        result = merged;
                        splitNodes = null;
                        return true;
                    }

                    if (newChild.IsLeaf && newChild.Length < MinLeafSize &&
                        TryRebalanceLeafWithSibling(children, i, newChild, out var rebalanced))
                    {
                        result = rebalanced;
                        splitNodes = null;
                        return true;
                    }

                    var sharedWithReplacement = _shared.ReplaceChildRange(i, 1, new[] { newChild });
                    result = FromShared(sharedWithReplacement);
                }

                return true;
            }

            break;
        }

        result = Empty;
        return false;
    }

    public Node CloneWithChildren(IReadOnlyList<Node> newChildren)
    {
        if (newChildren is null)
        {
            throw new ArgumentNullException(nameof(newChildren));
        }

        if (IsLeaf)
        {
            throw new InvalidOperationException("Cannot clone children for a leaf node.");
        }

        var shared = _shared.CloneWithChildren(newChildren);
        return FromShared(shared);
    }

    public IReadOnlyList<string> CollectInvariantIssues(bool enforceLeafMinimum = false)
    {
        var issues = new List<string>();
        ValidateNode(this, isRoot: true, enforceLeafMinimum, issues, "root");
        return issues.Count == 0 ? Array.Empty<string>() : issues;
    }

    public void ValidateInvariants(bool enforceLeafMinimum = false)
    {
        var issues = CollectInvariantIssues(enforceLeafMinimum);

        if (issues.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, issues));
        }
    }

    public Node NormalizeLeafMinimum()
    {
        var current = this;

        for (var iteration = 0; iteration < 16; iteration++)
        {
            var issues = current.CollectInvariantIssues(true);
            if (issues.Count == 0)
            {
                return current;
            }

            string? targetIssue = null;
            foreach (var issue in issues)
            {
                if (issue.Contains("Leaf below MinLeafSize", StringComparison.Ordinal))
                {
                    targetIssue = issue;
                    break;
                }
            }

            if (targetIssue is null)
            {
                return current;
            }

            if (!TryParseInvariantPath(targetIssue, out var path))
            {
                return current;
            }

            if (!current.TryResolveLeafUnderflow(path, out var updated))
            {
                return current;
            }

            current = updated;
        }

        return current;
    }

    private static bool TryParseInvariantPath(string issue, out int[] indices)
    {
        indices = Array.Empty<int>();

        if (string.IsNullOrEmpty(issue))
        {
            return false;
        }

        var start = issue.IndexOf("[root/", StringComparison.Ordinal);
        if (start < 0)
        {
            return false;
        }

        var end = issue.IndexOf(']', start);
        if (end < 0)
        {
            return false;
        }

        var pathText = issue.Substring(start + 1, end - start - 1);
        var parts = pathText.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 1)
        {
            return false;
        }

        var result = new int[parts.Length - 1];
        for (var i = 1; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out result[i - 1]))
            {
                return false;
            }
        }

        indices = result;
        return true;
    }

    private bool TryResolveLeafUnderflow(ReadOnlySpan<int> path, out Node updated)
    {
        updated = this;

        if (path.Length == 0)
        {
            return false;
        }

        var nodes = new Node[path.Length + 1];
        var indices = new int[path.Length];
        nodes[0] = this;

        var current = this;
        for (var depth = 0; depth < path.Length; depth++)
        {
            if (current.IsLeaf)
            {
                return false;
            }

            var children = current.RequireChildren();
            var index = path[depth];
            if ((uint)index >= (uint)children.Length)
            {
                return false;
            }

            current = children[index];
            nodes[depth + 1] = current;
            indices[depth] = index;
        }

        var leaf = nodes[path.Length];
        if (!leaf.IsLeaf || leaf.Length >= MinLeafSize)
        {
            return false;
        }

        var parentDepth = path.Length - 1;
        if (parentDepth < 0)
        {
            return false;
        }

        var parent = nodes[parentDepth];
        var parentChildren = parent.RequireChildren();
        var targetIndex = indices[parentDepth];
        Node newParent;

        if (parent.TryMergeLeafWithSibling(parentChildren, targetIndex, leaf, out var merged))
        {
            newParent = merged;
        }
        else if (parent.TryRebalanceLeafWithSibling(parentChildren, targetIndex, leaf, out var rebalanced))
        {
            newParent = rebalanced;
        }
        else
        {
            return false;
        }

        var subtree = newParent;

        for (var depth = parentDepth - 1; depth >= 0; depth--)
        {
            var ancestor = nodes[depth];
            var ancestorChildren = ancestor.RequireChildren();
            var list = new List<Node>(ancestorChildren.Length);
            for (var i = 0; i < ancestorChildren.Length; i++)
            {
                list.Add(i == indices[depth] ? subtree : ancestorChildren[i]);
            }

            subtree = BuildFromSegments(list);
        }

        updated = subtree;
        return true;
    }

    private Node ReplaceChildWithSegments(Node[] children, int index, IReadOnlyList<Node> segments)
    {
        if (segments is null)
        {
            throw new ArgumentNullException(nameof(segments));
        }

        if (segments.Count == 0)
        {
            throw new ArgumentException("Replacement segments must contain at least one node.", nameof(segments));
        }

        var shared = _shared.ReplaceChildRange(index, 1, segments);
        return FromShared(shared);
    }

    public Node WithChildReplaced(int index, Node newChild)
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

        var shared = _shared.ReplaceChildRange(index, 1, new[] { newChild });
        return FromShared(shared);
    }

    public (Node Left, Node Right) SplitAt(int index)
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
            if (Body.Leaf is null)
            {
                return (Empty, Empty);
            }

            var leftText = Body.Leaf[..index];
            var rightText = Body.Leaf[index..];
            return (FromLeaf(leftText), FromLeaf(rightText));
        }

        var children = RequireChildren();
        var leftSegments = new List<Node>();
        var rightSegments = new List<Node>();
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

    private static Node ConcatLeftShorter(Node left, Node right)
    {
        var children = right.RequireChildren();
        var firstChild = children[0];
        var merged = Concat(left, firstChild);

        if (merged.Height == right.Height - 1)
        {
            var newChildren = new Node[children.Length];
            newChildren[0] = merged;
            Array.Copy(children, 1, newChildren, 1, children.Length - 1);
            return CreateInternal(right.Height, newChildren);
        }

        if (merged.Height == right.Height)
        {
            var mergedChildren = merged.RequireChildren();
            var combined = new Node[mergedChildren.Length + children.Length - 1];
            Array.Copy(mergedChildren, 0, combined, 0, mergedChildren.Length);
            Array.Copy(children, 1, combined, mergedChildren.Length, children.Length - 1);
            return CreateInternal(right.Height, combined);
        }

        throw new InvalidOperationException("Unexpected height relationship during rope concatenation.");
    }

    private static Node ConcatRightShorter(Node left, Node right)
    {
        var children = left.RequireChildren();
        var lastIndex = children.Length - 1;
        var lastChild = children[lastIndex];
        var merged = Concat(lastChild, right);

        if (merged.Height == left.Height - 1)
        {
            var newChildren = new Node[children.Length];
            Array.Copy(children, 0, newChildren, 0, lastIndex);
            newChildren[lastIndex] = merged;
            return CreateInternal(left.Height, newChildren);
        }

        if (merged.Height == left.Height)
        {
            var mergedChildren = merged.RequireChildren();
            var combined = new Node[children.Length + mergedChildren.Length - 1];
            Array.Copy(children, 0, combined, 0, lastIndex);
            Array.Copy(mergedChildren, 0, combined, lastIndex, mergedChildren.Length);
            return CreateInternal(left.Height, combined);
        }

        throw new InvalidOperationException("Unexpected height relationship during rope concatenation.");
    }

    private Node[] RequireChildren()
    {
        if (Body.Children is { Length: > 0 } children)
        {
            return children;
        }

        throw new InvalidOperationException("Operation requires an internal node with children.");
    }

    private static string GetLeafText(Node node)
    {
        if (!node.IsLeaf)
        {
            throw new InvalidOperationException("Requested leaf text from an internal node.");
        }

        return node.Body.Leaf ?? string.Empty;
    }

    private bool TryMergeLeafWithSibling(Node[] children, int index, Node replacement, out Node result)
    {
        result = Empty;

        if (!replacement.IsLeaf || replacement.Length >= MinLeafSize || children.Length == 1)
        {
            return false;
        }

        var replacementText = GetLeafText(replacement);

        if (index > 0)
        {
            var left = children[index - 1];
            if (left.IsLeaf && left.Length + replacement.Length <= MaxLeafSize)
            {
                var mergedText = StringLeafOperations.Merge(GetLeafText(left), replacementText);
                var mergedLeaf = FromLeaf(mergedText);
                result = BuildMergedNode(children, index - 1, index, mergedLeaf);
                return true;
            }
        }

        if (index < children.Length - 1)
        {
            var right = children[index + 1];
            if (right.IsLeaf && replacement.Length + right.Length <= MaxLeafSize)
            {
                var mergedText = StringLeafOperations.Merge(replacementText, GetLeafText(right));
                var mergedLeaf = FromLeaf(mergedText);
                result = BuildMergedNode(children, index, index + 1, mergedLeaf);
                return true;
            }
        }

        return false;
    }

    private bool TryRebalanceLeafWithSibling(Node[] children, int index, Node replacement, out Node result)
    {
        result = Empty;

        if (!replacement.IsLeaf || replacement.Length >= MinLeafSize || children.Length <= 1)
        {
            return false;
        }

        if (index > 0 && TryRebalancePair(children, index - 1, index, children[index - 1], replacement, out result))
        {
            return true;
        }

        if (index < children.Length - 1 && TryRebalancePair(children, index, index + 1, replacement, children[index + 1], out result))
        {
            return true;
        }

        return false;
    }

    private bool TryRebalancePair(Node[] children, int firstIndex, int secondIndex, Node first, Node second, out Node result)
    {
        result = Empty;

        if (!first.IsLeaf || !second.IsLeaf)
        {
            return false;
        }

        var combinedLength = first.Length + second.Length;
        if (combinedLength <= MaxLeafSize)
        {
            return false;
        }

        var firstText = GetLeafText(first);
        var secondText = GetLeafText(second);

        if (!StringLeafOperations.TryComputeBalancedSplit(firstText, secondText, out var firstSegment, out var secondSegment))
        {
            return false;
        }

        var newFirst = FromLeaf(firstSegment);
        var newSecond = FromLeaf(secondSegment);

        if (firstIndex > secondIndex)
        {
            (firstIndex, secondIndex) = (secondIndex, firstIndex);
            (newFirst, newSecond) = (newSecond, newFirst);
        }

        var removeCount = secondIndex - firstIndex + 1;
        if (removeCount != 2)
        {
            throw new InvalidOperationException("Rebalance expects an adjacent child pair.");
        }
        var replacements = new[] { newFirst, newSecond };
        var shared = _shared.ReplaceChildRange(firstIndex, removeCount, replacements);
        result = FromShared(shared);
        return true;
    }

    private static void ValidateNode(Node node, bool isRoot, bool enforceLeafMinimum, List<string> issues, string path)
    {
        if (node.IsLeaf)
        {
            if (node.Length != node.Info.Utf16Length)
            {
                issues.Add($"[{path}] Leaf length mismatch: length={node.Length}, utf16={node.Info.Utf16Length}, preview=\"{FormatLeafPreview(node)}\"");
            }

            if (node.Length > MaxLeafSize)
            {
                issues.Add($"[{path}] Leaf exceeds MaxLeafSize: {node.Length}, preview=\"{FormatLeafPreview(node)}\"");
            }

            if (enforceLeafMinimum && !isRoot && node.Length > 0 && node.Length < MinLeafSize)
            {
                issues.Add($"[{path}] Leaf below MinLeafSize: {node.Length}, preview=\"{FormatLeafPreview(node)}\"");
            }

            return;
        }

        var children = node.RequireChildren();

        if (children.Length == 0)
        {
            issues.Add($"[{path}] Internal node has zero children.");
            return;
        }

        var expectedHeight = node.Height - 1;
        var totalLength = 0;
        var aggregate = RopeInfo.Identity;

        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var childPath = $"{path}/{i}";

            if (child.Height != expectedHeight)
            {
                issues.Add($"[{childPath}] Height mismatch: expected {expectedHeight}, actual {child.Height} (length={child.Length})");
            }

            ValidateNode(child, isRoot: false, enforceLeafMinimum, issues, childPath);

            totalLength = checked(totalLength + child.Length);
            aggregate = aggregate.Accumulate(child.Info);
        }

        var childSummary = SummarizeChildren(children);

        if (totalLength != node.Length)
        {
            issues.Add($"[{path}] Length aggregate mismatch: expected {node.Length}, actual {totalLength}; child lengths {childSummary}");
        }

        if (aggregate.Utf16Length != node.Info.Utf16Length || aggregate.LineCount != node.Info.LineCount)
        {
            issues.Add($"[{path}] Info aggregate mismatch; child lengths {childSummary}");
        }
    }

    private static string FormatLeafPreview(Node node)
    {
        if (!node.IsLeaf)
        {
            return string.Empty;
        }

        var text = node.ToString();
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        const int maxPreviewLength = 32;
        var previewLength = Math.Min(maxPreviewLength, text.Length);
        var previewSegment = text.Substring(0, previewLength);
        var escaped = EscapePreview(previewSegment);
        return text.Length > previewLength ? escaped + "…" : escaped;
    }

    private static string EscapePreview(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text
            .Replace("\\", "\\\\")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t")
            .Replace("\"", "\\\"");
    }

    private static string SummarizeChildren(Node[] children)
    {
        if (children.Length == 0)
        {
            return "[] (count=0)";
        }

        const int maxEntries = 6;
        var builder = new StringBuilder();
        builder.Append('[');

        var displayCount = Math.Min(children.Length, maxEntries);
        for (var i = 0; i < displayCount; i++)
        {
            builder.Append(children[i].Length);
            if (i < displayCount - 1)
            {
                builder.Append(", ");
            }
        }

        if (children.Length > maxEntries)
        {
            builder.Append(", …");
        }

        builder.Append(']');
        builder.Append(" (count=");
        builder.Append(children.Length);
        builder.Append(')');

        return builder.ToString();
    }

    private Node BuildMergedNode(Node[] children, int firstIndex, int secondIndex, Node mergedLeaf)
    {
        if (children is null)
        {
            throw new ArgumentNullException(nameof(children));
        }

        if (mergedLeaf is null)
        {
            throw new ArgumentNullException(nameof(mergedLeaf));
        }

        if (firstIndex > secondIndex)
        {
            (firstIndex, secondIndex) = (secondIndex, firstIndex);
        }

        var removeCount = secondIndex - firstIndex + 1;

        if (removeCount <= 0)
        {
            throw new InvalidOperationException("Merge operation requires a positive child range.");
        }

        if (children.Length == removeCount)
        {
            return mergedLeaf;
        }

        var shared = _shared.ReplaceChildRange(firstIndex, removeCount, new[] { mergedLeaf });
        return FromShared(shared);
    }

    private static Node CreateInternal(int height, IReadOnlyList<Node> children)
    {
        if (children.Count == 0)
        {
            return Empty;
        }

        var shared = SharedNode.FromInternal(height, children);
        return FromShared(shared);
    }

    private static Node BuildFromSegments(List<Node> segments)
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
