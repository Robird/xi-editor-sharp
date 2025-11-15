using System;
using System.Buffers;
using System.Globalization;
using System.Text;
using Xi.Core.Rope.Tree;

namespace Xi.Core.Rope.Navigation;

/// <summary>
/// Minimal grapheme navigator that relies on <see cref="StringInfo"/> for segmentation, supplements the current leaf with at most one neighbour, and falls back to code-point navigation when necessary.
/// This aligns with the degraded plan captured in design-divergence-log.md (2025-11-16) until the Rust trace-driven implementation is ported.
/// </summary>
public sealed class DegradedGraphemeNavigator : IGraphemeNavigator
{
    private static readonly int MaxContextChars = Node.MaxLeafSize * 2;
    private readonly GraphemeNavigationMetrics _metrics;

    public DegradedGraphemeNavigator(GraphemeNavigationMetrics? metrics = null)
    {
        _metrics = metrics ?? new GraphemeNavigationMetrics();
    }

    public GraphemeNavigationMetrics Metrics => _metrics;

    public int? MoveNext(NodeCursor cursor)
    {
        ArgumentNullException.ThrowIfNull(cursor);
        _metrics.RecordMoveNext();

        if (cursor.Position >= cursor.TotalLength)
        {
            return null;
        }

        if (!TryCaptureLeaf(cursor, out var leaf, out var leafStart, out var offsetInLeaf))
        {
            return null;
        }

        var context = BuildForwardContext(cursor, leaf, leafStart, offsetInLeaf);
        if (context.Length == 0)
        {
            return null;
        }

        var advance = GetFirstTextElementLength(context);
        if (!advance.HasValue || advance.Value == 0)
        {
            _metrics.RecordScalarFallback();
            advance = Math.Max(1, GetForwardScalarLength(context));
        }

        var nextPosition = Math.Min(cursor.TotalLength, cursor.Position + advance.Value);
        cursor.SetPosition(nextPosition);
        return nextPosition;
    }

    public int? MovePrevious(NodeCursor cursor)
    {
        ArgumentNullException.ThrowIfNull(cursor);
        _metrics.RecordMovePrevious();

        if (cursor.Position <= 0)
        {
            return null;
        }

        if (!TryCaptureLeaf(cursor, out var leaf, out var leafStart, out var offsetInLeaf))
        {
            return null;
        }

        var context = BuildBackwardContext(cursor, leaf, leafStart, offsetInLeaf);
        if (context.Length == 0)
        {
            return null;
        }

        var rewind = GetLastTextElementLength(context);
        if (!rewind.HasValue || rewind.Value == 0)
        {
            _metrics.RecordScalarFallback();
            rewind = Math.Max(1, GetBackwardScalarLength(context));
        }

        var previousPosition = Math.Max(0, cursor.Position - rewind.Value);
        cursor.SetPosition(previousPosition);
        return previousPosition;
    }

    public bool IsBoundary(NodeCursor cursor)
    {
        ArgumentNullException.ThrowIfNull(cursor);

        if (cursor.Position == 0 || cursor.Position == cursor.TotalLength)
        {
            return true;
        }

        if (!TryCaptureLeaf(cursor, out var leaf, out var leafStart, out var offsetInLeaf))
        {
            return false;
        }

        var before = BuildBackwardContext(cursor, leaf, leafStart, offsetInLeaf);
        var after = BuildForwardContext(cursor, leaf, leafStart, offsetInLeaf);
        if (before.Length == 0 || after.Length == 0)
        {
            return true;
        }

        var builder = new StringBuilder(before.Length + after.Length);
        builder.Append(before);
        var remaining = Math.Max(0, MaxContextChars - before.Length);
        if (remaining > 0)
        {
            var sliceLength = Math.Min(remaining, after.Length);
            builder.Append(after.AsSpan(0, sliceLength));
        }

        var combined = builder.ToString();
        var boundaryIndex = before.Length;
        var enumerator = StringInfo.GetTextElementEnumerator(combined);
        var cumulative = 0;
        while (enumerator.MoveNext())
        {
            cumulative += enumerator.GetTextElement().Length;
            if (cumulative == boundaryIndex)
            {
                return true;
            }

            if (cumulative > boundaryIndex)
            {
                return false;
            }
        }

        return false;
    }

    private string BuildForwardContext(NodeCursor cursor, string leaf, int leafStart, int offsetInLeaf)
    {
        var builder = new StringBuilder();
        if (offsetInLeaf < leaf.Length)
        {
            builder.Append(leaf.AsSpan(offsetInLeaf));
        }

        if (builder.Length < MaxContextChars)
        {
            var neighborStart = leafStart + leaf.Length;
            if (neighborStart < cursor.TotalLength)
            {
                var neighbor = TryFetchLeaf(cursor.Root, neighborStart);
                if (neighbor.HasValue)
                {
                    _metrics.RecordNeighborRequest(true);
                    var span = neighbor.Value.Leaf.AsSpan();
                    var take = Math.Min(span.Length, MaxContextChars - builder.Length);
                    builder.Append(span.Slice(0, take));
                }
            }
        }

        if (builder.Length > MaxContextChars)
        {
            builder.Length = MaxContextChars;
        }

        return builder.ToString();
    }

    private string BuildBackwardContext(NodeCursor cursor, string leaf, int leafStart, int offsetInLeaf)
    {
        var builder = new StringBuilder();

        if (leafStart > 0)
        {
            var neighbor = TryFetchLeaf(cursor.Root, leafStart - 1);
            if (neighbor.HasValue)
            {
                _metrics.RecordNeighborRequest(false);
                builder.Append(neighbor.Value.Leaf);
            }
        }

        if (offsetInLeaf > 0)
        {
            builder.Append(leaf.AsSpan(0, offsetInLeaf));
        }

        if (builder.Length == 0)
        {
            return string.Empty;
        }

        if (builder.Length > MaxContextChars)
        {
            builder.Remove(0, builder.Length - MaxContextChars);
        }

        return builder.ToString();
    }

    private static bool TryCaptureLeaf(NodeCursor cursor, out string leaf, out int leafStart, out int offsetInLeaf)
    {
        var info = cursor.GetLeaf();
        if (!info.HasValue)
        {
            leaf = string.Empty;
            leafStart = 0;
            offsetInLeaf = 0;
            return false;
        }

        leaf = info.Value.Leaf ?? string.Empty;
        offsetInLeaf = info.Value.Offset;
        leafStart = cursor.Position - offsetInLeaf;
        return true;
    }

    private static (string Leaf, int Start)? TryFetchLeaf(Node root, int absolutePosition)
    {
        if (absolutePosition < 0 || absolutePosition >= root.Length)
        {
            return null;
        }

        var cursor = new NodeCursor(root, absolutePosition);
        var info = cursor.GetLeaf();
        if (!info.HasValue)
        {
            return null;
        }

        var offsetInLeaf = info.Value.Offset;
        var start = cursor.Position - offsetInLeaf;
        return (info.Value.Leaf ?? string.Empty, start);
    }

    private static int? GetFirstTextElementLength(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var enumerator = StringInfo.GetTextElementEnumerator(text);
        if (!enumerator.MoveNext())
        {
            return null;
        }

        return enumerator.GetTextElement().Length;
    }

    private static int? GetLastTextElementLength(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var enumerator = StringInfo.GetTextElementEnumerator(text);
        var cumulative = 0;
        var previous = 0;
        while (enumerator.MoveNext())
        {
            previous = cumulative;
            cumulative += enumerator.GetTextElement().Length;
        }

        return cumulative == 0 ? null : cumulative - previous;
    }

    private static int GetForwardScalarLength(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var status = Rune.DecodeFromUtf16(text.AsSpan(), out _, out var consumed);
        return status == OperationStatus.Done ? consumed : 1;
    }

    private static int GetBackwardScalarLength(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        if (text.Length >= 2 && char.IsLowSurrogate(text[^1]) && char.IsHighSurrogate(text[^2]))
        {
            return 2;
        }

        return 1;
    }
}
