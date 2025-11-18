using System;

namespace Xi.Core.Rope.Tree;

/// <summary>
/// Traces TreeBuilder activity so Stage&nbsp;D slice traces can correlate with C# events.
/// Implementations can capture diagnostics when <see cref="Tree.TreeBuilder"/> raises lifecycle events.
/// </summary>
internal interface ITreeBuilderTracer
{
    /// <summary>Indicates whether the tracer wants to observe events.</summary>
    bool IsEnabled { get; }

    /// <summary>Captures a TreeBuilder event emitted by the builder.</summary>
    void Trace(in TreeBuilderEvent builderEvent);
}

/// <summary>
/// Represents which part of the TreeBuilder lifecycle raised the trace event.
/// The enum mirrors Rust's TreeBuilderTracer and keeps event kinds compact for logging.
/// </summary>
internal enum TreeBuilderEventKind
{
    None = 0,
    PushLeaf,
    PushNode,
    MergeLeaf,
    MergeInternal,
    PopFrame,
    BuildCompleted,
    Reset,
}

/// <summary>
/// Immutable payload that carries coarse snapshot data for Stage&nbsp;D diagnostics consumers.
/// </summary>
/// <param name="Kind">Kind of lifecycle event being reported.</param>
/// <param name="StackDepth">Current TreeBuilder stack depth.</param>
/// <param name="NodeHeight">Height of the node involved in the event.</param>
/// <param name="ByteLength">UTF-8 byte span (leaf) or base length (internal) touched by the event.</param>
/// <param name="Metrics">Snapshot of Base/Utf16/Lines/Breaks measurements captured by <see cref="MetricAdapter"/>.</param>
/// <param name="LeafPreview">Optional snippet of the leaf text, trimmed for diagnostics.</param>
internal readonly record struct TreeBuilderEvent(
    TreeBuilderEventKind Kind,
    int StackDepth,
    int NodeHeight,
    int ByteLength,
    MetricSnapshot Metrics,
    string? LeafPreview)
{
    /// <summary>Number of characters covered by the event (TreeBuilder base metric).</summary>
    public int BaseLength => Metrics.BaseLength;

    /// <summary>UTF-16 span of the payload affected by the event.</summary>
    public int Utf16Length => Metrics.Utf16Length;

    /// <summary>Total newline count covered by the event.</summary>
    public int LineCount => Metrics.LineCount;

    /// <summary>Total breaks recorded for the event (wraps or explicit newlines).</summary>
    public int BreakCount => Metrics.BreakCount;

    /// <summary>Leaf-specific break offsets captured via the Breaks metric.</summary>
    public ReadOnlyMemory<int> BreakOffsets => Metrics.BreakOffsets;

    /// <summary>Creates an event using measured ranges instead of raw lengths.</summary>
    internal static TreeBuilderEvent FromRanges(
        TreeBuilderEventKind kind,
        int stackDepth,
        int nodeHeight,
        Range byteRange,
        Range utf16Range,
        string? leafPreview = null)
    {
        var byteLength = Math.Max(0, byteRange.End.Value - byteRange.Start.Value);
        var utf16Length = Math.Max(0, utf16Range.End.Value - utf16Range.Start.Value);
        var metrics = new MetricSnapshot(byteLength, utf16Length, 0, 0, ReadOnlyMemory<int>.Empty);
        return new TreeBuilderEvent(kind, stackDepth, nodeHeight, byteLength, metrics, leafPreview);
    }
}

/// <summary>
/// Default tracer that intentionally does nothing until Stage&nbsp;D tracing lands on the C# side.
/// </summary>
internal sealed class NoOpTreeBuilderTracer : ITreeBuilderTracer
{
    internal static NoOpTreeBuilderTracer Instance { get; } = new();

    public bool IsEnabled => false;

    public void Trace(in TreeBuilderEvent builderEvent)
    {
        // No-op by design: `[TS-B2]` still tracks the missing TreeBuilder slice tracer.
    }
}
