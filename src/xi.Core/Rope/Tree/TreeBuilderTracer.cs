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
/// TODO(TS-B2): extend with richer metrics (byte/utf16 deltas, stack snapshot) when the tracer is wired up.
/// </summary>
/// <param name="Kind">Kind of lifecycle event being reported.</param>
/// <param name="StackDepth">Current TreeBuilder stack depth.</param>
/// <param name="NodeHeight">Height of the node involved in the event.</param>
/// <param name="ByteLength">Byte span of the payload affected by the event.</param>
/// <param name="Utf16Length">UTF-16 span of the payload affected by the event.</param>
/// <param name="LeafPreview">Optional snippet of the leaf text, trimmed for diagnostics.</param>
internal readonly record struct TreeBuilderEvent(
    TreeBuilderEventKind Kind,
    int StackDepth,
    int NodeHeight,
    int ByteLength,
    int Utf16Length,
    string? LeafPreview)
{
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
        return new TreeBuilderEvent(kind, stackDepth, nodeHeight, byteLength, utf16Length, leafPreview);
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
