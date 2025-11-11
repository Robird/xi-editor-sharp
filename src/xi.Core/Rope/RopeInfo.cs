using System;
using System.Runtime.CompilerServices;
using Xi.Core.Rope.Tree;

namespace Xi.Core.Rope;

/// <summary>
/// Aggregated metadata stored on each rope node. Mirrors the information gathered in xi-editor's RopeInfo.
/// </summary>
public readonly struct RopeInfo : ITreeNodeInfo<RopeInfo, string>, IDefaultMetricProvider<RopeInfo, string, BaseMetric>
{
    public int LineCount { get; }
    public int Utf16Length { get; }

    private RopeInfo(int lineCount, int utf16Length)
    {
        LineCount = lineCount;
        Utf16Length = utf16Length;
    }

    /// <summary>
    /// Creates an empty identity info.
    /// </summary>
    public static RopeInfo Identity => new(0, 0);

    /// <summary>
    /// Calculates info for a leaf payload.
    /// </summary>
    public static RopeInfo FromLeaf(ReadOnlySpan<char> span)
    {
        var (lines, utf16) = AnalyzeSpan(span);
        return new RopeInfo(lines, utf16);
    }

    /// <summary>
    /// Calculates info for a leaf represented as a string.
    /// </summary>
    public static RopeInfo FromLeaf(string leaf)
    {
        if (leaf is null)
        {
            throw new ArgumentNullException(nameof(leaf));
        }

        return FromLeaf(leaf.AsSpan());
    }

    /// <summary>
    /// Returns the aggregate info from two child nodes.
    /// </summary>
    public RopeInfo Accumulate(RopeInfo other)
    {
        return new RopeInfo(LineCount + other.LineCount, Utf16Length + other.Utf16Length);
    }

    /// <inheritdoc />
    public Interval IntervalForPrefix(int prefixLength)
    {
        if (prefixLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(prefixLength), prefixLength, "Prefix length must be non-negative.");
        }

        if (prefixLength > Utf16Length)
        {
            prefixLength = Utf16Length;
        }

        return new Interval(0, prefixLength);
    }

    /// <summary>
    /// Counts newline characters and UTF-16 code units in the span.
    /// </summary>
    private static (int lines, int utf16) AnalyzeSpan(ReadOnlySpan<char> span)
    {
        var lines = 0;
        foreach (var ch in span)
        {
            if (ch == '\n')
            {
                lines++;
            }
        }

        var utf16 = 0;
        foreach (var rune in span.EnumerateRunes())
        {
            utf16 += rune.Utf16SequenceLength;
        }

        return (lines, utf16);
    }

    /// <summary>
    /// Adds UTF-16 code unit length to a base length, guarding for overflow.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int AddLength(int baseLength)
    {
        checked
        {
            return baseLength + Utf16Length;
        }
    }

    /// <inheritdoc />
    static BaseMetric IDefaultMetricProvider<RopeInfo, string, BaseMetric>.DefaultMetric => BaseMetric.Instance;

    /// <inheritdoc />
    static RopeInfo ITreeNodeInfo<RopeInfo, string>.FromLeaf(string leaf) => FromLeaf(leaf);
}
