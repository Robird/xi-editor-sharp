using System;
using System.Diagnostics.CodeAnalysis;

namespace Xi.Core.Rope;

/// <summary>
/// Base metric that counts UTF-16 code units; effectively the default coordinate system.
/// </summary>
public sealed class BaseMetric : IRopeMetric
{
    public static BaseMetric Instance { get; } = new();

    private BaseMetric()
    {
    }

    public bool CanFragment => false;

    public int Measure(RopeInfo info, int nodeLength) => nodeLength;

    public int ToBaseUnits(ReadOnlySpan<char> leaf, int measuredUnits)
    {
        if ((uint)measuredUnits > (uint)leaf.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(measuredUnits));
        }

        if (!Utf16BoundaryHelper.IsBoundary(leaf, measuredUnits))
        {
            throw new ArgumentException("Offset does not align to a UTF-16 boundary.", nameof(measuredUnits));
        }

        return measuredUnits;
    }

    public int FromBaseUnits(ReadOnlySpan<char> leaf, int baseUnits)
    {
        if ((uint)baseUnits > (uint)leaf.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(baseUnits));
        }

        if (!Utf16BoundaryHelper.IsBoundary(leaf, baseUnits))
        {
            throw new ArgumentException("Offset does not align to a UTF-16 boundary.", nameof(baseUnits));
        }

        return baseUnits;
    }

    public bool IsBoundary(ReadOnlySpan<char> leaf, int offset) => Utf16BoundaryHelper.IsBoundary(leaf, offset);

    public int? GetPreviousBoundary(ReadOnlySpan<char> leaf, int offset) => Utf16BoundaryHelper.GetPreviousBoundary(leaf, offset);

    public int? GetNextBoundary(ReadOnlySpan<char> leaf, int offset) => Utf16BoundaryHelper.GetNextBoundary(leaf, offset);
}

/// <summary>
/// Metric counting the number of newline characters encountered.
/// </summary>
public sealed class LinesMetric : IRopeMetric
{
    public static LinesMetric Instance { get; } = new();

    private LinesMetric()
    {
    }

    public bool CanFragment => true;

    public int Measure(RopeInfo info, int nodeLength) => info.LineCount;

    public int ToBaseUnits(ReadOnlySpan<char> leaf, int measuredUnits)
    {
        if (measuredUnits < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(measuredUnits));
        }

        var offset = 0;
        var found = 0;
        while (offset < leaf.Length && found < measuredUnits)
        {
            var index = leaf[offset..].IndexOf('\n');
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(measuredUnits), measuredUnits, "Measured units exceed available lines.");
            }

            offset += index + 1;
            found++;
        }

        return offset;
    }

    public int FromBaseUnits(ReadOnlySpan<char> leaf, int baseUnits)
    {
        if ((uint)baseUnits > (uint)leaf.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(baseUnits));
        }

        var span = leaf[..baseUnits];
        var count = 0;
        foreach (var ch in span)
        {
            if (ch == '\n')
            {
                count++;
            }
        }

        return count;
    }

    public bool IsBoundary(ReadOnlySpan<char> leaf, int offset)
    {
        if (offset <= 0 || offset > leaf.Length)
        {
            return false;
        }

        return leaf[offset - 1] == '\n';
    }

    public int? GetPreviousBoundary(ReadOnlySpan<char> leaf, int offset)
    {
        if (offset <= 0)
        {
            return null;
        }

        for (var i = offset - 1; i >= 0; i--)
        {
            if (leaf[i] == '\n')
            {
                return i + 1;
            }
        }

        return null;
    }

    public int? GetNextBoundary(ReadOnlySpan<char> leaf, int offset)
    {
        if (offset >= leaf.Length)
        {
            return null;
        }

        for (var i = offset; i < leaf.Length; i++)
        {
            if (leaf[i] == '\n')
            {
                return i + 1;
            }
        }

        return null;
    }
}

/// <summary>
/// Metric counting UTF-16 code units; primarily used for aligning with front-end APIs.
/// </summary>
public sealed class Utf16Metric : IRopeMetric
{
    public static Utf16Metric Instance { get; } = new();

    private Utf16Metric()
    {
    }

    public bool CanFragment => false;

    public int Measure(RopeInfo info, int nodeLength) => info.Utf16Length;

    public int ToBaseUnits(ReadOnlySpan<char> leaf, int measuredUnits)
    {
        if (measuredUnits < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(measuredUnits));
        }

        var currentUnits = 0;
        var utf16Units = 0;
        foreach (var rune in leaf.EnumerateRunes())
        {
            if (currentUnits + rune.Utf16SequenceLength > measuredUnits)
            {
                break;
            }

            currentUnits += rune.Utf16SequenceLength;
            utf16Units += rune.Utf16SequenceLength;
        }

        if (currentUnits < measuredUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(measuredUnits), measuredUnits, "Measured units exceed available length.");
        }

        return utf16Units;
    }

    public int FromBaseUnits(ReadOnlySpan<char> leaf, int baseUnits)
    {
        if (baseUnits < 0 || baseUnits > leaf.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(baseUnits));
        }

        return baseUnits;
    }

    public bool IsBoundary(ReadOnlySpan<char> leaf, int offset) => Utf16BoundaryHelper.IsBoundary(leaf, offset);

    public int? GetPreviousBoundary(ReadOnlySpan<char> leaf, int offset) => Utf16BoundaryHelper.GetPreviousBoundary(leaf, offset);

    public int? GetNextBoundary(ReadOnlySpan<char> leaf, int offset) => Utf16BoundaryHelper.GetNextBoundary(leaf, offset);
}

internal static class Utf16BoundaryHelper
{
    public static bool IsBoundary(ReadOnlySpan<char> leaf, int offset)
    {
        if ((uint)offset > (uint)leaf.Length)
        {
            return false;
        }

        if (offset == 0 || offset == leaf.Length)
        {
            return true;
        }

        return !(char.IsLowSurrogate(leaf[offset]) && char.IsHighSurrogate(leaf[offset - 1]));
    }

    public static int? GetPreviousBoundary(ReadOnlySpan<char> leaf, int offset)
    {
        if (offset <= 0)
        {
            return null;
        }

        var prev = offset - 1;
        if (prev < leaf.Length && char.IsLowSurrogate(leaf[prev]) && prev > 0 && char.IsHighSurrogate(leaf[prev - 1]))
        {
            prev -= 1;
        }

        return prev;
    }

    public static int? GetNextBoundary(ReadOnlySpan<char> leaf, int offset)
    {
        if (offset >= leaf.Length)
        {
            return null;
        }

        var next = offset + 1;
        if (offset < leaf.Length && char.IsHighSurrogate(leaf[offset]) && next < leaf.Length && char.IsLowSurrogate(leaf[next]))
        {
            next += 1;
        }

        return next <= leaf.Length ? next : null;
    }
}
