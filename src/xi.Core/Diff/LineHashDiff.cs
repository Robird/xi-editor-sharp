using System;
using System.Collections.Generic;

namespace Xi.Core.Diff;

/// <summary>
/// Skeleton for the Rust line-hash diff representation referenced by <c>diff_regions@1.0.0</c>.
/// </summary>
public sealed class LineHashDiff
{
    public LineHashDiff(string algorithm, IReadOnlyList<DiffRegion> regions)
    {
        Algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
        Regions = regions ?? Array.Empty<DiffRegion>();
    }

    /// <summary>Name of the diff strategy (line-hash, minimal, etc.).</summary>
    public string Algorithm { get; }

    /// <summary>Regions surfaced by Stage D descriptor ingestion.</summary>
    public IReadOnlyList<DiffRegion> Regions { get; }

    /// <summary>Projects a single Stage D descriptor view into the diff skeleton.</summary>
    public static LineHashDiff FromDescriptor(DiffCaseDescriptorView descriptor)
    {
        var region = DiffRegion.FromDescriptor(descriptor);
        return new LineHashDiff("line-hash", new[] { region });
    }

    /// <summary>Future hook for merging staged regions prior to presentation.</summary>
    public DiffRegion MergeRegions()
    {
        throw new NotImplementedException("Region merging is not wired up for the diff skeleton yet.");
    }
}
