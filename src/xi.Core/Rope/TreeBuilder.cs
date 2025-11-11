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
    private const int NewlinePreferenceWindow = 64;

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

            if (remaining <= max)
            {
                yield return text.Substring(offset, remaining);
                yield break;
            }

            var preferred = FindPreferredSplit(text, offset, chunkLength);
            if (preferred <= 0)
            {
                preferred = Math.Min(remaining, max);
            }

            yield return text.Substring(offset, preferred);
            offset += preferred;
        }
    }

    private static int FindPreferredSplit(string text, int offset, int initialLength)
    {
        var length = text.Length;
        var candidate = initialLength;

        // Prefer newline close to the target split.
        var searchEnd = offset + candidate - 1;
        var windowStart = Math.Max(offset, searchEnd - (NewlinePreferenceWindow - 1));
        for (var index = searchEnd; index >= windowStart; index--)
        {
            if (text[index] == '\n')
            {
                candidate = index - offset + 1;
                break;
            }
        }

        // Ensure we do not split surrogate pairs in half.
        if (offset + candidate < length && candidate > 0 &&
            char.IsHighSurrogate(text[offset + candidate - 1]) &&
            char.IsLowSurrogate(text[offset + candidate]))
        {
            candidate--;
        }

        if (candidate <= 0)
        {
            return Math.Min(initialLength, length - offset);
        }

        return candidate;
    }
}
