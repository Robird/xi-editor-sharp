using System;
using System.Collections.Generic;

namespace Xi.Core.Rope.Tree;

internal static class LeafSplitter
{
    internal const int NewlinePreferenceWindow = 64;

    internal static IEnumerable<string> Split(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

    var max = StringLeafOperations.MaxLeafSize;
        var offset = 0;
        var length = text.Length;

        while (offset < length)
        {
            var remaining = length - offset;
            if (remaining <= max)
            {
                yield return text.Substring(offset, remaining);
                yield break;
            }

            var chunkLength = Math.Min(max, remaining);
            var preferred = FindPreferredSplit(text, offset, chunkLength);
            if (preferred <= 0)
            {
                preferred = chunkLength;
            }

            yield return text.Substring(offset, preferred);
            offset += preferred;
        }
    }

    private static int FindPreferredSplit(string text, int offset, int initialLength)
    {
        var length = text.Length;
        var candidate = initialLength;
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
