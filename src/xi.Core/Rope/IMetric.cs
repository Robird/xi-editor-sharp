using System;

using Xi.Core.Rope.Tree;

namespace Xi.Core.Rope;

/// <summary>
/// Metric interface allowing traversal and measurement of rope nodes in different coordinate systems.
/// This narrows <see cref="ITreeMetric{TLeaf, TInfo}"/> to the string/RopeInfo combination used by the main rope type.
/// </summary>
public interface IMetric : ITreeMetric<string, RopeInfo>
{
}
