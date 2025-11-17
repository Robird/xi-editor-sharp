using System.Collections.Generic;

namespace Xi.Core.Rope.Diagnostics.Descriptors;

/// <summary>
/// Captures fields emitted by export-serde-fixtures --grapheme-descriptors so
/// `[StageD::ParityAssets]` can assert parity between Rust traces and the degraded navigator.
/// </summary>
internal sealed class GraphemeDescriptor
{
    public string Sample { get; set; } = string.Empty;

    public int ClusterIndex { get; set; }

    public string Cluster { get; set; } = string.Empty;

    public DescriptorRange ByteRange { get; set; } = new();

    public DescriptorRange Utf16Range { get; set; } = new();

    public int ScalarCount { get; set; }

    public bool ContainsZwj { get; set; }

    public bool IsAscii { get; set; }

    public bool CrossesLeaf { get; set; }

    public bool RequiresFallback { get; set; }

    public IList<string> Tags { get; set; } = new List<string>();

    public DescriptorContext Context { get; set; } = new();

    public DescriptorLeafInfo Leaf { get; set; } = new();
}
