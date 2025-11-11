using System;

namespace Xi.Core.Rope;

/// <summary>
/// Metric interface allowing traversal and measurement of rope nodes in different coordinate systems.
/// </summary>
public interface IMetric
{
    /// <summary>
    /// Measures the given node metadata and returns the size in this metric's unit.
    /// </summary>
    int Measure(RopeInfo info, int nodeLength);

    /// <summary>
    /// Converts a position expressed in the metric's unit into base units (UTF-16 code units).
    /// </summary>
    int ToBaseUnits(ReadOnlySpan<char> leaf, int measuredUnits);

    /// <summary>
    /// Converts a position expressed in base units (UTF-16 code units) into the metric's measured unit.
    /// </summary>
    int FromBaseUnits(ReadOnlySpan<char> leaf, int baseUnits);

    /// <summary>
    /// Determines whether the given offset (in base units) is a boundary in this metric.
    /// </summary>
    bool IsBoundary(ReadOnlySpan<char> leaf, int offset);

    /// <summary>
    /// Returns the previous boundary before the given offset or <c>null</c> when none exists.
    /// </summary>
    int? GetPreviousBoundary(ReadOnlySpan<char> leaf, int offset);

    /// <summary>
    /// Returns the next boundary strictly after the given offset or <c>null</c> when none exists.
    /// </summary>
    int? GetNextBoundary(ReadOnlySpan<char> leaf, int offset);

    /// <summary>
    /// Indicates whether measured units may span multiple leaves (for example lines spanning multiple blocks).
    /// </summary>
    bool CanFragment { get; }
}
