using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Xi.Core.Rope;

namespace Xi.Core.Rope.Tree;

/// <summary>
/// Bridges Rust's <c>Metric</c> trait to the C# <see cref="IMetric"/> types so Stage D traces can
/// be replayed using familiar metric names such as <c>LinesMetric</c> and <c>Utf16CodeUnitsMetric</c>.
/// The adapter also exposes a coarse Breaks metric approximation so soft-wrap traces have a
/// placeholder path until the dedicated Breaks tree lands on the C# side.
/// </summary>
internal sealed class MetricAdapter
{
    private const int BreakWrapWidth = 80;

    /// <summary>Logical metric identifiers understood by the adapter.</summary>
    internal enum MetricKind
    {
        Base,
        Lines,
        Utf16CodeUnits,
        Breaks
    }

    internal static MetricAdapter Default { get; } = new();

    /// <summary>Measures aggregate metric lengths for the provided node.</summary>
    internal MetricSnapshot Measure(Node node, string? leafOverride = null)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        var baseLength = node.Length;
        var lines = LinesMetric.Instance.Measure(node.Info, baseLength);
        var utf16 = Utf16CodeUnitsMetric.Instance.Measure(node.Info, baseLength);
        var breakOffsets = ReadOnlyMemory<int>.Empty;
        var breaks = 0;

        if (node.IsLeaf)
        {
            if (TryGetLeafText(node, leafOverride, out var text) && !string.IsNullOrEmpty(text))
            {
                var offsets = CollectBreakOffsets(text);
                breakOffsets = offsets;
                breaks = offsets.Length;
            }
        }
        else
        {
            breaks = CountBreaks(node, null);
        }

        return new MetricSnapshot(baseLength, utf16, lines, breaks, breakOffsets);
    }

    /// <summary>Converts the specified interval from one metric to another.</summary>
    internal Interval ConvertInterval(Node node, Interval interval, MetricKind source, MetricKind target, string? leafOverride = null)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (source == target)
        {
            return interval;
        }

        var start = ConvertCoordinate(node, interval.Start, source, target, leafOverride);
        var end = ConvertCoordinate(node, interval.End, source, target, leafOverride);
        return new Interval(start, end);
    }

    /// <summary>Maps a Stage D metric string to the corresponding <see cref="MetricKind"/>.</summary>
    internal static bool TryParse(string? metricName, out MetricKind kind)
    {
        switch (metricName)
        {
            case null:
            case "" or "base" or "BaseMetric" or "bytes":
                kind = MetricKind.Base;
                return true;
            case "lines" or "LinesMetric":
                kind = MetricKind.Lines;
                return true;
            case "utf16" or "Utf16" or "Utf16CodeUnits" or "Utf16CodeUnitsMetric":
                kind = MetricKind.Utf16CodeUnits;
                return true;
            case "breaks" or "BreaksMetric":
                kind = MetricKind.Breaks;
                return true;
            default:
                kind = MetricKind.Base;
                return false;
        }
    }

    private static int ConvertCoordinate(Node node, int value, MetricKind source, MetricKind target, string? leafOverride)
    {
        if (source == target)
        {
            return value;
        }

        var baseValue = source == MetricKind.Base ? value : ConvertToBase(node, value, source, leafOverride);
        return target == MetricKind.Base ? baseValue : ConvertFromBase(node, baseValue, target, leafOverride);
    }

    private static int ConvertToBase(Node node, int measuredUnits, MetricKind source, string? leafOverride)
    {
        return source switch
        {
            MetricKind.Base => measuredUnits,
            MetricKind.Lines => node.ConvertToDefaultMetric(LinesMetric.Instance, measuredUnits),
            MetricKind.Utf16CodeUnits => node.ConvertToDefaultMetric(Utf16CodeUnitsMetric.Instance, measuredUnits),
            MetricKind.Breaks => ConvertBreaksToBase(node, measuredUnits, leafOverride),
            _ => measuredUnits
        };
    }

    private static int ConvertFromBase(Node node, int baseUnits, MetricKind target, string? leafOverride)
    {
        return target switch
        {
            MetricKind.Base => baseUnits,
            MetricKind.Lines => node.ConvertFromDefaultMetric(LinesMetric.Instance, baseUnits),
            MetricKind.Utf16CodeUnits => node.ConvertFromDefaultMetric(Utf16CodeUnitsMetric.Instance, baseUnits),
            MetricKind.Breaks => ConvertBaseToBreaks(node, baseUnits, leafOverride),
            _ => baseUnits
        };
    }

    private static int ConvertBreaksToBase(Node node, int measuredUnits, string? leafOverride)
    {
        if (!TryGetLeafText(node, leafOverride, out var text))
        {
            throw new InvalidOperationException("Breaks metric conversion currently requires a leaf node context.");
        }

        var breaks = CollectBreakOffsets(text);
        return BreaksMetricHelper.GetNthBreakOffset(breaks, text.Length, measuredUnits);
    }

    private static int ConvertBaseToBreaks(Node node, int baseUnits, string? leafOverride)
    {
        if (!TryGetLeafText(node, leafOverride, out var text))
        {
            throw new InvalidOperationException("Breaks metric conversion currently requires a leaf node context.");
        }

        var breaks = CollectBreakOffsets(text);
        return BreaksMetricHelper.CountBreaksUpTo(breaks, baseUnits);
    }

    private static bool TryGetLeafText(Node node, string? overrideText, [NotNullWhen(true)] out string? leaf)
    {
        if (!string.IsNullOrEmpty(overrideText))
        {
            leaf = overrideText;
            return true;
        }

        leaf = node.GetLeaf();
        return leaf is not null;
    }

    private static int CountBreaks(Node node, string? leafOverride)
    {
        if (node.IsLeaf)
        {
            if (!TryGetLeafText(node, leafOverride, out var text) || string.IsNullOrEmpty(text))
            {
                return 0;
            }

            var breaks = CollectBreakOffsets(text);
            return breaks.Length;
        }

        var children = node.GetChildren();
        if (children is null || children.Length == 0)
        {
            return 0;
        }

        var total = 0;
        foreach (var child in children)
        {
            total += CountBreaks(child, null);
        }

        return total;
    }

    private static int[] CollectBreakOffsets(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<int>();
        }

        var breaks = new List<int>(Math.Max(4, text.Length / BreakWrapWidth + 1));
        var column = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            column++;

            if (ch == '\n')
            {
                breaks.Add(i + 1);
                column = 0;
                continue;
            }

            if (column >= BreakWrapWidth)
            {
                breaks.Add(i + 1);
                column = 0;
            }
        }

        return breaks.ToArray();
    }
}

/// <summary>Snapshot of metric totals captured for a builder event.</summary>
internal readonly record struct MetricSnapshot(
    int BaseLength,
    int Utf16Length,
    int LineCount,
    int BreakCount,
    ReadOnlyMemory<int> BreakOffsets);
