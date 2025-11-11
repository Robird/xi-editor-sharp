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
}
