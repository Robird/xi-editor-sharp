using System.Diagnostics.CodeAnalysis;

namespace Xi.Core.Rope.Tree;

/// <summary>
/// Describes operations required for rope leaf values. Mirrors xi-editor's <c>Leaf</c> trait。
/// 采用 static abstract 成员以便在泛型节点中通过类型实参直接调用。
/// </summary>
/// <typeparam name="TLeaf">叶片数据类型。</typeparam>
public interface ILeafOperations<TLeaf>
{
    /// <summary>最小叶片容量。</summary>
    static abstract int MinLeafSize { get; }

    /// <summary>最大叶片容量。</summary>
    static abstract int MaxLeafSize { get; }

    /// <summary>空叶片实例。</summary>
    static abstract TLeaf Empty { get; }

    /// <summary>返回叶片长度（以基础单位计）。</summary>
    static abstract int GetLength(TLeaf leaf);

    /// <summary>判断叶片是否满足结构约束。</summary>
    static abstract bool IsValidChild(TLeaf leaf);

    /// <summary>克隆叶片，确保写时复制场景不共享底层存储。</summary>
    static abstract TLeaf Clone(TLeaf leaf);

    /// <summary>在指定位置插入 <paramref name="insertion"/> 内容并返回新叶片。</summary>
    static abstract TLeaf Insert(TLeaf leaf, int index, TLeaf insertion);

    /// <summary>删除指定范围的内容并返回新叶片。</summary>
    static abstract TLeaf RemoveRange(TLeaf leaf, int index, int length);

    /// <summary>将指定范围替换为 <paramref name="replacement"/> 内容并返回新叶片。</summary>
    static abstract TLeaf ReplaceRange(TLeaf leaf, int index, int length, TLeaf replacement);

    /// <summary>合并相邻叶片内容。</summary>
    static abstract TLeaf Merge(TLeaf left, TLeaf right);

    /// <summary>尝试根据容量约束对两个叶片进行平衡拆分。</summary>
    static abstract bool TryComputeBalancedSplit(TLeaf left, TLeaf right, [MaybeNullWhen(false)] out TLeaf newLeft, [MaybeNullWhen(false)] out TLeaf newRight);
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