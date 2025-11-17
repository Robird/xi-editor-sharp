using System.Collections.Generic;

namespace Xi.Core.Rope.Diagnostics.Descriptors;

/// <summary>
/// Represents a byte/UTF-16/leaf range captured inside Stage D descriptor fixtures
/// (see tests/xi.Core.Tests/Fixtures/fixtures.manifest.json and `[StageD::ParityAssets]`).
/// </summary>
internal sealed class DescriptorRange
{
    /// <summary>Inclusive start offset within the descriptor's coordinate system.</summary>
    public int Start { get; set; }

    /// <summary>Exclusive end offset.</summary>
    public int End { get; set; }
}

/// <summary>
/// Stores the text immediately before or after a descriptor target so QA can
/// diff rope slices when replaying `[StageD::ParityAssets]` fixtures.
/// </summary>
internal sealed class DescriptorContext
{
    public string? Before { get; set; }

    public string? After { get; set; }
}

/// <summary>
/// Identifies a node on the TreeBuilder path that produced the descriptor sample.
/// TODO(TS-B3): Align with Rust's path payload once JSON ingestion is implemented.
/// </summary>
internal sealed class DescriptorNodePathEntry
{
    public int NodeHeight { get; set; }

    public int NodeLength { get; set; }

    public int ChildIndex { get; set; }

    public int ChildOffset { get; set; }
}

/// <summary>
/// Provides leaf-level metadata for grapheme descriptors, mirroring the Rust exporter schema.
/// </summary>
internal sealed class DescriptorLeafInfo
{
    public DescriptorRange Range { get; set; } = new();

    public IList<DescriptorNodePathEntry> Path { get; set; } = new List<DescriptorNodePathEntry>();
}
