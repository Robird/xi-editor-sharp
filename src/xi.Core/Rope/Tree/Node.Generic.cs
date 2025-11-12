using System;
using System.Collections.Generic;

namespace Xi.Core.Rope.Tree;

/// <summary>
/// Generic rope node skeleton that mirrors the xi-editor <c>Node&lt;N&gt;</c> structure.
/// Currently implements the minimal functionality required to validate contracts
/// while we migrate the existing string-specialized node toward the generic version.
/// </summary>
/// <typeparam name="TInfo">Aggregate metadata type stored on each node.</typeparam>
/// <typeparam name="TLeaf">Underlying leaf payload type.</typeparam>
/// <typeparam name="TLeafOps">Helper providing leaf-specific operations.</typeparam>
public sealed class Node<TInfo, TLeaf, TLeafOps>
    where TInfo : struct, ITreeNodeInfo<TInfo, TLeaf>
    where TLeafOps : ILeafOperations<TLeaf>
{
    private static readonly Node<TInfo, TLeaf, TLeafOps> s_empty = new(new NodeBody(
        0,
        0,
        TInfo.Identity,
        TLeafOps.Empty,
        null));

    private readonly NodeBody _body;

    private Node(NodeBody body)
    {
        _body = body;
    }

    /// <summary>Gets an empty leaf node.</summary>
    public static Node<TInfo, TLeaf, TLeafOps> Empty => s_empty;

    /// <summary>Gets the height of the node. Leaves have height 0.</summary>
    public int Height => _body.Height;

    /// <summary>Gets the aggregated base length.</summary>
    public int Length => _body.Length;

    /// <summary>Gets the aggregated metadata.</summary>
    public TInfo Info => _body.Info;

    /// <summary>Gets a value indicating whether the node is a leaf.</summary>
    public bool IsLeaf => _body.Children is null;

    /// <summary>Gets a value indicating whether the node is empty.</summary>
    public bool IsEmpty => Length == 0;

    /// <summary>Gets the leaf payload. Only valid when <see cref="IsLeaf"/> is true.</summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing the leaf on an internal node.</exception>
    public TLeaf Leaf
    {
        get
        {
            if (!IsLeaf)
            {
                throw new InvalidOperationException("Cannot access leaf payload on an internal node.");
            }

            return _body.Leaf;
        }
    }

    /// <summary>Gets the children of the node (empty when the node is a leaf).</summary>
    public IReadOnlyList<Node<TInfo, TLeaf, TLeafOps>> Children
        => _body.Children ?? Array.Empty<Node<TInfo, TLeaf, TLeafOps>>();

    /// <summary>Creates a leaf node from the specified payload.</summary>
    public static Node<TInfo, TLeaf, TLeafOps> FromLeaf(TLeaf leaf)
    {
        var length = TLeafOps.GetLength(leaf);
        var info = TInfo.FromLeaf(leaf);
        return new Node<TInfo, TLeaf, TLeafOps>(new NodeBody(
            0,
            length,
            info,
            leaf,
            null));
    }

    /// <summary>Creates an internal node from the provided children.</summary>
    public static Node<TInfo, TLeaf, TLeafOps> CreateInternal(IReadOnlyList<Node<TInfo, TLeaf, TLeafOps>> children)
    {
        if (children is null)
        {
            throw new ArgumentNullException(nameof(children));
        }

        if (children.Count == 0)
        {
            return Empty;
        }

        var expectedChildHeight = children[0].Height;
        var newHeight = expectedChildHeight + 1;
        var length = 0;
        var info = TInfo.Identity;
        var array = new Node<TInfo, TLeaf, TLeafOps>[children.Count];

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (child.Height != expectedChildHeight)
            {
                throw new ArgumentException("All children must share the same height.", nameof(children));
            }

            array[i] = child;
            length = checked(length + child.Length);
            info = info.Accumulate(child.Info);
        }

        return new Node<TInfo, TLeaf, TLeafOps>(new NodeBody(
            newHeight,
            length,
            info,
            TLeafOps.Empty,
            array));
    }

    /// <summary>Enumerates leaves in a left-to-right order.</summary>
    public IEnumerable<Node<TInfo, TLeaf, TLeafOps>> TraverseLeaves()
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

    private sealed record NodeBody(int Height, int Length, TInfo Info, TLeaf Leaf, Node<TInfo, TLeaf, TLeafOps>[]? Children);
}
