using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Xi.Core.Rope.StageD;

namespace Xi.Core.Rope.Breaks;

/// <summary>
/// Represents the <c>breaks_descriptors@1.0.0</c> payload described in
/// docs/architecture/fixtures/parity-fixture-schema.md.
/// </summary>
public sealed class BreaksDescriptorDocument
{
    [JsonPropertyName("metadata")]
    public BreaksDescriptorMetadata Metadata { get; set; } = new();

    [JsonPropertyName("break_sets")]
    public IList<BreakSetDescriptor> BreakSets { get; set; } = new List<BreakSetDescriptor>();
}

/// <summary>Metadata emitted by the Rust exporter for the Breaks descriptor set.</summary>
public sealed class BreaksDescriptorMetadata
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = string.Empty;

    [JsonPropertyName("rust_commit")]
    public string? RustCommit { get; set; }

    [JsonPropertyName("generated_at_unix_millis")]
    public long GeneratedAtUnixMillis { get; set; }

    [JsonPropertyName("descriptor_count")]
    public int DescriptorCount { get; set; }
}

/// <summary>Describes the soft-wrap breakpoints for a given sample.</summary>
public sealed class BreakSetDescriptor
{
    [JsonPropertyName("sample")]
    public string Sample { get; set; } = string.Empty;

    [JsonPropertyName("rope_len")]
    public int RopeLength { get; set; }

    [JsonPropertyName("wrap_width_units")]
    public int WrapWidthUnits { get; set; }

    [JsonPropertyName("metric")]
    public string Metric { get; set; } = string.Empty;

    [JsonPropertyName("break_offsets")]
    public IList<int> BreakOffsets { get; set; } = new List<int>();

    [JsonPropertyName("break_count")]
    public int BreakCount { get; set; }

    [JsonPropertyName("leaf_runs")]
    public IList<LeafRunSnapshot> LeafRuns { get; set; } = new List<LeafRunSnapshot>();

    [JsonPropertyName("text_excerpt")]
    public string? TextExcerpt { get; set; }

    [JsonPropertyName("tags")]
    public IList<string> Tags { get; set; } = new List<string>();
}

/// <summary>Summarises the number of breakpoints that fall inside a specific leaf.</summary>
public sealed class LeafRunSnapshot
{
    [JsonPropertyName("range")]
    public RangeSnapshot Range { get; set; } = new();

    [JsonPropertyName("break_count")]
    public int BreakCount { get; set; }

    [JsonPropertyName("path")]
    public IList<PathFrameSnapshot> Path { get; set; } = new List<PathFrameSnapshot>();
}

/// <summary>Read-only view projected from <see cref="BreakSetDescriptor"/> for Stage D loaders.</summary>
public readonly record struct BreakSetDescriptorView(
    string Sample,
    int RopeLength,
    int WrapWidthUnits,
    string Metric,
    IReadOnlyList<int> BreakOffsets,
    int BreakCount,
    IReadOnlyList<LeafRunSnapshotView> LeafRuns,
    string? TextExcerpt,
    IReadOnlyList<string> Tags)
{
    public static BreakSetDescriptorView FromDescriptor(BreakSetDescriptor descriptor)
    {
        return new BreakSetDescriptorView(
            descriptor.Sample,
            descriptor.RopeLength,
            descriptor.WrapWidthUnits,
            descriptor.Metric,
            Materialize(descriptor.BreakOffsets),
            descriptor.BreakCount,
            descriptor.LeafRuns.Select(leaf => LeafRunSnapshotView.FromSnapshot(leaf)).ToArray(),
            descriptor.TextExcerpt,
            Materialize(descriptor.Tags));
    }

    private static IReadOnlyList<T> Materialize<T>(IList<T>? source)
    {
        return source is { Count: > 0 } ? source.ToArray() : Array.Empty<T>();
    }
}

/// <summary>Projection that freezes the mutable leaf-run DTO.</summary>
public readonly record struct LeafRunSnapshotView(RangeSnapshot Range, int BreakCount, IReadOnlyList<PathFrameSnapshot> Path)
{
    public static LeafRunSnapshotView FromSnapshot(LeafRunSnapshot snapshot)
    {
        return new LeafRunSnapshotView(
            snapshot.Range,
            snapshot.BreakCount,
            snapshot.Path is { Count: > 0 } ? snapshot.Path.ToArray() : Array.Empty<PathFrameSnapshot>());
    }
}
