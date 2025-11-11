using System;
using System.Collections.Generic;

namespace Xi.Core.Rope.Tree;

/// <summary>
/// Incremental builder that assembles rope nodes from strings and spans, mirroring the xi-editor TreeBuilder.
/// Keeps the tree approximately balanced while respecting leaf size boundaries.
/// </summary>
public sealed class TreeBuilder
{
    private readonly List<Node> _pending = new();
    public void PushString(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        foreach (var segment in SplitIntoLeaves(text))
        {
            AppendNode(Node.FromLeaf(segment));
        }
    }

    public void PushSpan(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
        {
            return;
        }

        PushString(new string(span));
    }

    public void PushNode(Node node)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (node.IsEmpty)
        {
            return;
        }

        AppendNode(node);
    }

    public Node Build()
    {
        if (_pending.Count == 0)
        {
            return Node.Empty;
        }

        var result = _pending[0];
        for (var i = 1; i < _pending.Count; i++)
        {
            result = Node.Concat(result, _pending[i]);
        }

        return result;
    }

    public void Reset() => _pending.Clear();

    private void AppendNode(Node node)
    {
        var current = node;
        for (var index = _pending.Count - 1; index >= 0; index--)
        {
            var candidate = _pending[index];
            if (candidate.Height == current.Height)
            {
                current = Node.Concat(candidate, current);
                _pending.RemoveAt(index);
            }
            else
            {
                break;
            }
        }

        _pending.Add(current);
    }

    private static IEnumerable<string> SplitIntoLeaves(string text)
    {
        return LeafSplitter.Split(text);
    }
}
