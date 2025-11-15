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

    /// <summary>
    /// Validates tree invariants and returns a list of issues found.
    /// </summary>
    /// <param name="enforceLeafMinimum">If true, also checks that leaves meet minimum size requirements.</param>
    /// <returns>A list of invariant violation messages, or empty if all invariants are satisfied.</returns>
    public List<string> ValidateInvariants(bool enforceLeafMinimum = false)
    {
        var issues = new List<string>();
        ValidateNode(this, isRoot: true, enforceLeafMinimum, issues, "root");
        return issues;
    }

    /// <summary>
    /// Generates a debug string representation showing the tree structure.
    /// </summary>
    public string ToDebugString()
    {
        var builder = new System.Text.StringBuilder();
        AppendDebugString(builder, depth: 0);
        return builder.ToString();
    }

    private void AppendDebugString(System.Text.StringBuilder builder, int depth)
    {
        var indent = new string(' ', depth * 2);
        
        if (IsLeaf)
        {
            builder.Append(indent);
            builder.Append("Leaf[len=");
            builder.Append(Length);
            builder.Append("]");
            builder.AppendLine();
        }
        else
        {
            builder.Append(indent);
            builder.Append("Internal[h=");
            builder.Append(Height);
            builder.Append(", len=");
            builder.Append(Length);
            builder.Append(", children=");
            builder.Append(_body.Children?.Length ?? 0);
            builder.Append("]");
            builder.AppendLine();

            if (_body.Children is not null)
            {
                foreach (var child in _body.Children)
                {
                    child.AppendDebugString(builder, depth + 1);
                }
            }
        }
    }

    private static void ValidateNode(
        Node<TInfo, TLeaf, TLeafOps> node,
        bool isRoot,
        bool enforceLeafMinimum,
        List<string> issues,
        string path)
    {
        if (node.IsLeaf)
        {
            ValidateLeaf(node, isRoot, enforceLeafMinimum, issues, path);
            return;
        }

        ValidateInternal(node, isRoot, enforceLeafMinimum, issues, path);
    }

    private static void ValidateLeaf(
        Node<TInfo, TLeaf, TLeafOps> node,
        bool isRoot,
        bool enforceLeafMinimum,
        List<string> issues,
        string path)
    {
        var leafLength = node.Length;

        // Check maximum size
        if (leafLength > TLeafOps.MaxLeafSize)
        {
            issues.Add($"[{path}] Leaf exceeds MaxLeafSize: length={leafLength}, max={TLeafOps.MaxLeafSize}");
        }

        // Check minimum size (except for root or empty leaves)
        if (enforceLeafMinimum && !isRoot && leafLength > 0 && leafLength < TLeafOps.MinLeafSize)
        {
            issues.Add($"[{path}] Leaf below MinLeafSize: length={leafLength}, min={TLeafOps.MinLeafSize}");
        }

        // Validate leaf is a valid child according to operations
        if (!isRoot && leafLength > 0 && !TLeafOps.IsValidChild(node.Leaf))
        {
            issues.Add($"[{path}] Leaf fails IsValidChild check: length={leafLength}");
        }
    }

    private static void ValidateInternal(
        Node<TInfo, TLeaf, TLeafOps> node,
        bool isRoot,
        bool enforceLeafMinimum,
        List<string> issues,
        string path)
    {
        var children = node._body.Children;

        if (children is null || children.Length == 0)
        {
            issues.Add($"[{path}] Internal node has no children (height={node.Height})");
            return;
        }

        var expectedChildHeight = node.Height - 1;
        if (expectedChildHeight < 0)
        {
            issues.Add($"[{path}] Internal node has invalid height: {node.Height}");
            return;
        }

        var totalLength = 0;
        var aggregateInfo = TInfo.Identity;

        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var childPath = $"{path}/{i}";

            // Validate child height
            if (child.Height != expectedChildHeight)
            {
                issues.Add($"[{childPath}] Height mismatch: expected={expectedChildHeight}, actual={child.Height}, length={child.Length}");
            }

            // Recursively validate child
            ValidateNode(child, isRoot: false, enforceLeafMinimum, issues, childPath);

            // Accumulate metrics
            totalLength = checked(totalLength + child.Length);
            aggregateInfo = aggregateInfo.Accumulate(child.Info);
        }

        // Validate length aggregation
        if (totalLength != node.Length)
        {
            var childLengths = FormatChildLengths(children);
            issues.Add($"[{path}] Length aggregate mismatch: expected={node.Length}, actual={totalLength}; children={childLengths}");
        }

        // Note: We can't easily validate TInfo aggregation without knowing its equality semantics,
        // but we've accumulated it above to ensure the operation itself doesn't throw
    }

    private static string FormatChildLengths(Node<TInfo, TLeaf, TLeafOps>[] children)
    {
        if (children.Length == 0)
        {
            return "[](count=0)";
        }

        var builder = new System.Text.StringBuilder();
        builder.Append('[');

        const int maxDisplay = 6;
        var displayCount = Math.Min(children.Length, maxDisplay);

        for (var i = 0; i < displayCount; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }
            builder.Append(children[i].Length);
        }

        if (children.Length > maxDisplay)
        {
            builder.Append(", …");
        }

        builder.Append("](count=");
        builder.Append(children.Length);
        builder.Append(')');

        return builder.ToString();
    }

    private sealed record NodeBody(int Height, int Length, TInfo Info, TLeaf Leaf, Node<TInfo, TLeaf, TLeafOps>[]? Children);
}
