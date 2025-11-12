using System;

namespace Xi.Core.Rope.Tree;

/// <summary>
/// Centralizes string-specific leaf operations used by the rope <see cref="Node"/> implementation.
/// Extracted to ease the upcoming transition to generic leaf helpers.
/// </summary>
internal static class StringLeafOperations
{
    internal const int MinLeafSize = 511;
    internal const int MaxLeafSize = 1024;

    internal static int GetLength(string? leaf) => leaf?.Length ?? 0;

    internal static bool IsValidChild(string? leaf)
    {
        if (leaf is null)
        {
            return false;
        }

        return leaf.Length >= MinLeafSize;
    }

    internal static string Clone(string leaf)
    {
        if (leaf.Length == 0)
        {
            return string.Empty;
        }

        return string.Create(leaf.Length, leaf, static (span, source) =>
        {
            source.AsSpan().CopyTo(span);
        });
    }

    internal static string Insert(string leaf, int index, string text)
    {
        if (text.Length == 0)
        {
            return leaf;
        }

        if ((uint)index > (uint)leaf.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var newLength = leaf.Length + text.Length;
        return string.Create(newLength, (leaf, index, text), static (span, state) =>
        {
            var (source, insertIndex, insertText) = state;
            var head = source.AsSpan(0, insertIndex);
            head.CopyTo(span);

            var insertSpan = insertText.AsSpan();
            insertSpan.CopyTo(span.Slice(insertIndex, insertSpan.Length));

            var tail = source.AsSpan(insertIndex);
            tail.CopyTo(span.Slice(insertIndex + insertSpan.Length));
        });
    }

    internal static string RemoveRange(string leaf, int index, int length)
    {
        if (length == 0)
        {
            return leaf;
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        if ((uint)index > (uint)leaf.Length || index + length > leaf.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (length == leaf.Length)
        {
            return string.Empty;
        }

        var newLength = leaf.Length - length;
        return string.Create(newLength, (leaf, index, length), static (span, state) =>
        {
            var (source, removeStart, removeLength) = state;
            var head = source.AsSpan(0, removeStart);
            head.CopyTo(span);

            var tail = source.AsSpan(removeStart + removeLength);
            tail.CopyTo(span.Slice(head.Length));
        });
    }

    internal static string ReplaceRange(string leaf, int index, int length, string replacement)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        if ((uint)index > (uint)leaf.Length || index + length > leaf.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (length == 0)
        {
            return Insert(leaf, index, replacement);
        }

        if (replacement.Length == 0)
        {
            return RemoveRange(leaf, index, length);
        }

        var newLength = leaf.Length - length + replacement.Length;
        return string.Create(newLength, (leaf, index, length, replacement), static (span, state) =>
        {
            var (source, replaceStart, replaceLength, replacementText) = state;

            var head = source.AsSpan(0, replaceStart);
            head.CopyTo(span);

            var replacementSpan = replacementText.AsSpan();
            replacementSpan.CopyTo(span.Slice(replaceStart, replacementSpan.Length));

            var tail = source.AsSpan(replaceStart + replaceLength);
            tail.CopyTo(span.Slice(replaceStart + replacementSpan.Length));
        });
    }
}
