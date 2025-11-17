using System.Text.Json.Serialization;

namespace Xi.Core.Rope.StageD;

/// <summary>
/// Mirrors <c>RangeSnapshot</c> from docs/architecture/fixtures/parity-fixture-schema.md so Stage D
/// ingestion code can reason about Base/UTF-16 ranges without duplicating schema structs.
/// </summary>
public sealed class RangeSnapshot
{
    [JsonPropertyName("start")]
    public int Start { get; set; }

    [JsonPropertyName("end")]
    public int End { get; set; }
}

/// <summary>
/// Mirrors <c>PathFrameSnapshot</c> to preserve the route from the rope root to a sampled leaf.
/// </summary>
public sealed class PathFrameSnapshot
{
    [JsonPropertyName("node_height")]
    public int NodeHeight { get; set; }

    [JsonPropertyName("node_len")]
    public int NodeLength { get; set; }

    [JsonPropertyName("child_index")]
    public int ChildIndex { get; set; }

    [JsonPropertyName("child_offset")]
    public int ChildOffset { get; set; }
}
