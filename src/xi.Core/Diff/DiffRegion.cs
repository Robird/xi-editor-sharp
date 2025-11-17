using System;
using System.Collections.Generic;
using System.Linq;

namespace Xi.Core.Diff;

/// <summary>
/// Represents a single diff case emitted by <c>diff_regions@1.0.0</c> after Stage D projection.
/// </summary>
public sealed class DiffRegion
{
    public DiffRegion(
        string sample,
        string basePath,
        string targetPath,
        IReadOnlyList<DiffOperation> operations,
        DiffCaseStats? stats,
        string? notes)
    {
        Sample = sample ?? throw new ArgumentNullException(nameof(sample));
        BasePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        TargetPath = targetPath ?? throw new ArgumentNullException(nameof(targetPath));
        Operations = operations ?? Array.Empty<DiffOperation>();
        Stats = stats;
        Notes = notes;
    }

    public string Sample { get; }

    public string BasePath { get; }

    public string TargetPath { get; }

    public IReadOnlyList<DiffOperation> Operations { get; }

    public DiffCaseStats? Stats { get; }

    public string? Notes { get; }

    public static DiffRegion FromDescriptor(DiffCaseDescriptorView descriptor)
    {
        var operations = descriptor.Ops.Select(DiffOperation.FromDescriptor).ToArray();
        return new DiffRegion(
            descriptor.Sample,
            descriptor.BasePath,
            descriptor.TargetPath,
            operations,
            descriptor.Stats,
            descriptor.Notes);
    }
}
