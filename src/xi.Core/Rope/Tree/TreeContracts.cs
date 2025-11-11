using System.Diagnostics.CodeAnalysis;

namespace Xi.Core.Rope.Tree;

/// <summary>
/// Describes operations required for rope leaf values. Mirrors xi-editor's <c>Leaf</c> trait.
/// </summary>
/// <typeparam name="TLeaf">The underlying leaf data type.</typeparam>
public interface ILeafOperations<TLeaf>
{
    /// <summary>Returns the length of the leaf in base units.</summary>
    int GetLength(TLeaf leaf);

    /// <summary>
    /// Determines whether the leaf respects structural constraints and can remain a child in the tree.
    /// </summary>
    bool IsValidChild(TLeaf leaf, int minLeafSize, int maxLeafSize);

    /// <summary>
    /// Attempts to append a slice of <paramref name="other"/> described by <paramref name="interval"/> to <paramref name="destination"/>.
    /// When the resulting leaf exceeds capacity, the overflow segment is returned via <paramref name="splitLeaf"/>.
    /// </summary>
    bool TryPushMaybeSplit(ref TLeaf destination, TLeaf other, Interval interval, [MaybeNullWhen(false)] out TLeaf splitLeaf);

    /// <summary>
    /// Extracts a subsequence of <paramref name="leaf"/> described by <paramref name="interval"/>.
    /// </summary>
    TLeaf Slice(TLeaf leaf, Interval interval);
}

/// <summary>
/// Aggregated metadata kept on tree nodes. Mirrors xi-editor's <c>NodeInfo</c> trait.
/// </summary>
/// <typeparam name="TSelf">The struct implementing this interface.</typeparam>
/// <typeparam name="TLeaf">The leaf type used in the tree.</typeparam>
public interface ITreeNodeInfo<TSelf, TLeaf>
    where TSelf : struct, ITreeNodeInfo<TSelf, TLeaf>
{
    /// <summary>Gets the identity value for the aggregation monoid.</summary>
    static abstract TSelf Identity { get; }

    /// <summary>Computes metadata for a single leaf.</summary>
    static abstract TSelf FromLeaf(TLeaf leaf);

    /// <summary>Merges the metadata from the provided subtree into the current value.</summary>
    TSelf Accumulate(TSelf other);

    /// <summary>
    /// Maps the prefix described by <paramref name="prefixLength"/> base units into the coordinate space represented by this info.
    /// Equivalent to Rust's <c>NodeInfo::interval</c> default method.
    /// </summary>
    Interval IntervalForPrefix(int prefixLength);
}

/// <summary>
/// Marks a node info type that provides a default metric. Aligns with Rust's <c>DefaultMetric</c> trait.
/// </summary>
/// <typeparam name="TSelf">The node info type.</typeparam>
/// <typeparam name="TLeaf">The leaf type.</typeparam>
/// <typeparam name="TMetric">The metric type.</typeparam>
public interface IDefaultMetricProvider<TSelf, TLeaf, TMetric>
    where TSelf : struct, ITreeNodeInfo<TSelf, TLeaf>
    where TMetric : class, ITreeMetric<TLeaf, TSelf>
{
    /// <summary>Gets the metric that acts as the canonical coordinate system for this node info type.</summary>
    static abstract TMetric DefaultMetric { get; }
}

/// <summary>
/// Metric abstraction used to traverse and measure rope nodes. Mirrors xi-editor's <c>Metric</c> trait.
/// </summary>
/// <typeparam name="TLeaf">The underlying leaf type.</typeparam>
/// <typeparam name="TInfo">The node info type.</typeparam>
public interface ITreeMetric<TLeaf, TInfo>
    where TInfo : struct, ITreeNodeInfo<TInfo, TLeaf>
{
    /// <summary>Indicates whether the measured unit may span multiple leaves.</summary>
    bool CanFragment { get; }

    /// <summary>Computes the size of a node in this metric.</summary>
    int Measure(TInfo info, int nodeLength);

    /// <summary>Converts a position measured in this metric into base units.</summary>
    int ToBaseUnits(TLeaf leaf, int measuredUnits);

    /// <summary>Converts a position expressed in base units into this metric's units.</summary>
    int FromBaseUnits(TLeaf leaf, int baseUnits);

    /// <summary>Checks whether the provided base offset is a valid boundary in this metric.</summary>
    bool IsBoundary(TLeaf leaf, int offset);

    /// <summary>Finds the previous boundary strictly before <paramref name="offset"/>.</summary>
    int? GetPreviousBoundary(TLeaf leaf, int offset);

    /// <summary>Finds the next boundary at or after <paramref name="offset"/>.</summary>
    int? GetNextBoundary(TLeaf leaf, int offset);
}