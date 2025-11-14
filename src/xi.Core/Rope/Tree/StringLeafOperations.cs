using System;
using System.Collections.Generic;

namespace Xi.Core.Rope.Tree;

/// <summary>
/// Centralizes string-specific leaf operations used by the rope <see cref="Node"/> implementation.
/// Implements <see cref="ILeafOperations{TLeaf}"/> to support future generic node helpers.
/// Helpers operate on UTF-16 code unit offsets; Rust counterparts work with UTF-8 byte offsets, so parity tooling must translate units explicitly.
/// </summary>
internal readonly struct StringLeafOperations : ILeafOperations<string>
{
    public static int MinLeafSize => 511;

    public static int MaxLeafSize => 1024;

    public static string Empty => string.Empty;

    /// <summary>
    /// Exposes the newline preference window shared with <see cref="LeafSplitter"/>.
    /// </summary>
    public static int NewlinePreferenceWindow => LeafSplitter.NewlinePreferenceWindow;

    public static int GetLength(string leaf) => leaf?.Length ?? 0;

    public static bool IsValidChild(string leaf)
    {
        if (leaf is null)
        {
            return false;
        }

        return leaf.Length >= MinLeafSize;
    }

    public static string Clone(string leaf)
    {
        if (leaf.Length == 0)
        {
            return Empty;
        }

        return string.Create(leaf.Length, leaf, static (span, source) =>
        {
            source.AsSpan().CopyTo(span);
        });
    }

    public static string Insert(string leaf, int index, string text)
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

    public static string RemoveRange(string leaf, int index, int length)
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

    public static string ReplaceRange(string leaf, int index, int length, string replacement)
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

    public static string Merge(string left, string right)
    {
        if (left is null)
        {
            throw new ArgumentNullException(nameof(left));
        }

        if (right is null)
        {
            throw new ArgumentNullException(nameof(right));
        }

        var totalLength = left.Length + right.Length;
        if (totalLength == 0)
        {
            return Empty;
        }

        return string.Create(totalLength, (left, right), static (span, state) =>
        {
            var (first, second) = state;
            var firstSpan = first.AsSpan();
            firstSpan.CopyTo(span);
            second.AsSpan().CopyTo(span[firstSpan.Length..]);
        });
    }

    public static bool TryComputeBalancedSplit(string left, string right, out string newLeft, out string newRight)
    {
        if (left is null)
        {
            throw new ArgumentNullException(nameof(left));
        }

        if (right is null)
        {
            throw new ArgumentNullException(nameof(right));
        }

        newLeft = left;
        newRight = right;

        var totalLength = left.Length + right.Length;
        if (totalLength == 0)
        {
            return false;
        }

        var minSplit = Math.Max(MinLeafSize, totalLength - MaxLeafSize);
        var maxSplit = Math.Min(MaxLeafSize, totalLength - MinLeafSize);

        if (minSplit > maxSplit)
        {
            return false;
        }

        var candidate = Math.Clamp(totalLength / 2, minSplit, maxSplit);
        candidate = PreferNewlineBoundary(left, right, candidate, minSplit, maxSplit);

        if (!TryEnsureSurrogateBoundary(left, right, ref candidate, minSplit, maxSplit))
        {
            return false;
        }

        var leftSegment = CreateCombinedSegment(left, right, 0, candidate);
        var rightSegment = CreateCombinedSegment(left, right, candidate, totalLength - candidate);

        if (leftSegment.Length < MinLeafSize || rightSegment.Length < MinLeafSize)
        {
            return false;
        }

        newLeft = leftSegment;
        newRight = rightSegment;
        return true;
    }

    public static IEnumerable<string> SplitByCapacity(string leaf)
    {
        if (string.IsNullOrEmpty(leaf))
        {
            yield break;
        }

        var max = MaxLeafSize;
        var window = NewlinePreferenceWindow;
        var offset = 0;
        var length = leaf.Length;

        while (offset < length)
        {
            var remaining = length - offset;
            var desired = Math.Min(max, remaining);
            var split = desired;

            if (remaining > desired)
            {
                var searchEnd = offset + desired - 1;
                var windowStart = Math.Max(offset, searchEnd - (window - 1));

                for (var index = searchEnd; index >= windowStart; index--)
                {
                    if (leaf[index] == '\n')
                    {
                        split = index - offset + 1;
                        break;
                    }
                }

                if (offset + split < length &&
                    split > 0 &&
                    char.IsHighSurrogate(leaf[offset + split - 1]) &&
                    char.IsLowSurrogate(leaf[offset + split]))
                {
                    split--;
                }

                if (split <= 0)
                {
                    split = desired;
                }
            }

            yield return leaf.Substring(offset, split);
            offset += split;
        }
    }

    private static int PreferNewlineBoundary(string left, string right, int candidate, int minSplit, int maxSplit)
    {
        var window = NewlinePreferenceWindow;
        var lowerBound = Math.Max(minSplit, candidate - window);

        for (var split = candidate; split >= lowerBound; split--)
        {
            if (split == 0)
            {
                break;
            }

            if (GetCombinedChar(left, right, split - 1) == '\n')
            {
                return split;
            }
        }

        return candidate;
    }

    private static bool TryEnsureSurrogateBoundary(string left, string right, ref int splitIndex, int minSplit, int maxSplit)
    {
        var total = left.Length + right.Length;

        if (!IsSafeBoundary(left, right, splitIndex))
        {
            if (splitIndex + 1 <= maxSplit && IsSafeBoundary(left, right, splitIndex + 1))
            {
                splitIndex += 1;
            }
            else if (splitIndex - 1 >= minSplit && IsSafeBoundary(left, right, splitIndex - 1))
            {
                splitIndex -= 1;
            }
            else
            {
                return false;
            }
        }

        if (splitIndex <= 0 || splitIndex >= total)
        {
            return false;
        }

        return true;
    }

    private static bool IsSafeBoundary(string left, string right, int index)
    {
        if (index <= 0)
        {
            return true;
        }

        var total = left.Length + right.Length;
        if (index >= total)
        {
            return true;
        }

        var prev = GetCombinedChar(left, right, index - 1);
        var next = GetCombinedChar(left, right, index);
        return !(char.IsHighSurrogate(prev) && char.IsLowSurrogate(next));
    }

    private static string CreateCombinedSegment(string left, string right, int start, int length)
    {
        if (length == 0)
        {
            return string.Empty;
        }

        return string.Create(length, (left, right, start), static (span, state) =>
        {
            var (first, second, offset) = state;
            var remaining = span.Length;
            var writeIndex = 0;
            var currentOffset = offset;

            if (currentOffset < first.Length)
            {
                var take = Math.Min(first.Length - currentOffset, remaining);
                first.AsSpan(currentOffset, take).CopyTo(span);
                writeIndex += take;
                currentOffset += take;
                remaining -= take;
            }

            if (remaining > 0)
            {
                var secondOffset = currentOffset - first.Length;
                second.AsSpan(secondOffset, remaining).CopyTo(span[writeIndex..]);
            }
        });
    }

    private static char GetCombinedChar(string left, string right, int index)
    {
        return index < left.Length ? left[index] : right[index - left.Length];
    }
}
