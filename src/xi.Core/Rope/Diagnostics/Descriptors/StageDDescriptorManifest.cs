using System.Collections.Generic;
using Xi.Core.Diff;
using Xi.Core.Rope.Breaks;
using Xi.Core.Search;

namespace Xi.Core.Rope.Diagnostics.Descriptors;

/// <summary>
/// Captures the shared metadata emitted by chunk/grapheme descriptor exports and the
/// fixtures.manifest.json ledger referenced by `[StageD::ParityAssets]`.
/// TODO(TS-B3): hydrate this from the JSON payload instead of leaving it as a pure DTO.
/// </summary>
internal sealed class StageDDescriptorManifestMetadata
{
    public string SchemaVersion { get; set; } = string.Empty;

    public string RustCommit { get; set; } = string.Empty;

    public string? CliRevision { get; set; }

    public long? GeneratedAtUnixMillis { get; set; }

    public int? ChunkDescriptorCount { get; set; }

    public int? LineDescriptorCount { get; set; }

    public int? GraphemeDescriptorCount { get; set; }

    public int? BreaksDescriptorCount { get; set; }

    public int? DiffCaseCount { get; set; }

    public int? SearchCaseCount { get; set; }

    public IList<string> FeatureGates { get; set; } = new List<string>();
}

/// <summary>
/// Aggregates descriptor collections so Stage D ingestion code can bind manifest metadata
/// and JSON samples without scattering temporary structs across the Rope assembly.
/// </summary>
internal sealed class StageDDescriptorManifest
{
    public StageDDescriptorManifestMetadata Metadata { get; set; } = new();

    public IList<ChunkDescriptor> ChunkDescriptors { get; set; } = new List<ChunkDescriptor>();

    public IList<LineDescriptor> LineDescriptors { get; set; } = new List<LineDescriptor>();

    public IList<GraphemeDescriptor> GraphemeDescriptors { get; set; } = new List<GraphemeDescriptor>();

    public IList<BreakSetDescriptorView> BreaksDescriptors { get; set; } = new List<BreakSetDescriptorView>();

    public IList<DiffCaseDescriptorView> DiffRegions { get; set; } = new List<DiffCaseDescriptorView>();

    public IList<SearchCaseDescriptorView> SearchSpans { get; set; } = new List<SearchCaseDescriptorView>();

    public IList<StageDFixtureLedgerEntry> Fixtures { get; set; } = new List<StageDFixtureLedgerEntry>();
}

/// <summary>
/// Snapshot of a fixtures.manifest.json entry so QA tooling can cross-check payload counts & hashes.
/// </summary>
internal sealed class StageDFixtureLedgerEntry
{
    public string Name { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public int Count { get; set; }

    public string SchemaHash { get; set; } = string.Empty;

    public string PayloadHash { get; set; } = string.Empty;
}
