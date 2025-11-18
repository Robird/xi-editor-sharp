using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Xi.Core.Rope.Diagnostics.Descriptors;

/// <summary>
/// Captures fields emitted by export-serde-fixtures --grapheme-descriptors so
/// `[StageD::ParityAssets]` can assert parity between Rust traces and the degraded navigator.
/// </summary>
public sealed class GraphemeDescriptor
{
    public string Sample { get; set; } = string.Empty;

    [JsonPropertyName("cluster_index")]
    public int ClusterIndex { get; set; }

    public string Cluster { get; set; } = string.Empty;

    [JsonPropertyName("byte_range")]
    public DescriptorRange ByteRange { get; set; } = new();

    [JsonPropertyName("utf16_range")]
    public DescriptorRange Utf16Range { get; set; } = new();

    [JsonPropertyName("scalar_count")]
    public int ScalarCount { get; set; }

    [JsonPropertyName("contains_zwj")]
    public bool ContainsZwj { get; set; }

    [JsonPropertyName("is_ascii")]
    public bool IsAscii { get; set; }

    [JsonPropertyName("crosses_leaf")]
    public bool CrossesLeaf { get; set; }

    [JsonPropertyName("requires_fallback")]
    public bool RequiresFallback { get; set; }

    public IList<string> Tags { get; set; } = new List<string>();

    public DescriptorContext Context { get; set; } = new();

    public DescriptorLeafInfo Leaf { get; set; } = new();
}
