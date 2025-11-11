using System;
using System.Collections.Generic;

namespace Xi.Core.Rope;

/// <summary>
/// Incremental builder that assembles rope nodes from strings and spans, mirroring the xi-editor TreeBuilder.
/// Keeps the tree approximately balanced while respecting leaf size boundaries.
/// </summary>
public sealed class TreeBuilder
{
    private readonly List<RopeNode> _pending = new();

    public void PushString(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        foreach (var segment in SplitIntoLeaves(text))
        {
            AppendNode(RopeNode.FromLeaf(segment));
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

    public void PushNode(RopeNode node)
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

    public RopeNode Build()
    {
        if (_pending.Count == 0)
        {
            return RopeNode.Empty;
        }

        var result = _pending[0];
        for (var i = 1; i < _pending.Count; i++)
        {
            result = RopeNode.Concat(result, _pending[i]);
        }

        return result;
    }

    public void Reset() => _pending.Clear();

    private void AppendNode(RopeNode node)
    {
        var current = node;
        for (var index = _pending.Count - 1; index >= 0; index--)
        {
            var candidate = _pending[index];
            if (candidate.Height == current.Height)
            {
                current = RopeNode.Concat(candidate, current);
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
        var max = RopeNode.MaxLeafSize;
        var offset = 0;
        var length = text.Length;

        while (offset < length)
        {
            var remaining = length - offset;
            var chunkLength = Math.Min(max, remaining);
            if (chunkLength == 0)
            {
                break;
            }

            if (offset + chunkLength < length &&
                char.IsHighSurrogate(text[offset + chunkLength - 1]) &&
                char.IsLowSurrogate(text[offset + chunkLength]))
            {
                chunkLength--;
            }

            if (chunkLength <= 0)
            {
                chunkLength = Math.Min(remaining, 2);
            }

            yield return text.Substring(offset, chunkLength);
            offset += chunkLength;
        }
    }
}
