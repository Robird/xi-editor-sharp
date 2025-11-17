using System;

namespace Xi.Core.Rope.Breaks;

/// <summary>
/// Mirrors the Rust BreakBuilder entry point so Stage D fixture ingestion has a landing spot even while
/// the underlying algorithm is stubbed out.
/// </summary>
public sealed class BreakBuilder
{
    /// <summary>
    /// Converts a Stage D descriptor view into a BreakPlan so downstream code can observe sample metadata.
    /// </summary>
    public BreakPlan CreatePlan(BreakSetDescriptorView descriptor)
    {
        return BreakPlan.FromDescriptor(descriptor);
    }

    /// <summary>Placeholder for the real Breaks computation.</summary>
    public BreakResult Compute(ReadOnlySpan<char> text, BreakComputationOptions options)
    {
        throw new NotImplementedException("BreakBuilder.Compute will be implemented alongside the Breaks runtime.");
    }
}

/// <summary>Options that will eventually flow from Stage D manifests into the BreakBuilder API.</summary>
public readonly record struct BreakComputationOptions(int WrapWidthUnits, string Metric, bool IncludeLeafRuns);

/// <summary>Result placeholder that surfaces the BreakPlan and optional tree.</summary>
public readonly record struct BreakResult(BreakPlan Plan, BreaksTree? Tree);
