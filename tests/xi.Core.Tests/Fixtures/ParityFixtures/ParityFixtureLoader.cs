using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Xi.Core.Tests.Fixtures.ParityFixtures;

internal static class ParityFixtureLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Lazy<ChunkDescriptorFixture> ChunkFixture = new(LoadChunkFixture);
    private static readonly Lazy<GraphemeDescriptorFixture> GraphemeFixture = new(LoadGraphemeFixture);

    public static ChunkDescriptorFixture ChunkFixtures => ChunkFixture.Value;

    public static GraphemeDescriptorFixture GraphemeFixtures => GraphemeFixture.Value;

    private static ChunkDescriptorFixture LoadChunkFixture()
    {
        const string fileName = "chunk_descriptors.json";
        var path = ResolveFixturePath("chunk_descriptors", fileName);
        return Deserialize<ChunkDescriptorFixture>(path, fileName);
    }

    private static GraphemeDescriptorFixture LoadGraphemeFixture()
    {
        const string fileName = "grapheme_descriptors.json";
        var path = ResolveFixturePath("grapheme_descriptors", fileName);
        return Deserialize<GraphemeDescriptorFixture>(path, fileName);
    }

    private static T Deserialize<T>(string path, string label)
        where T : class
    {
        Assert.True(File.Exists(path),
            $"Missing parity fixture '{label}'. Refresh fixtures via the Rust exporter (cargo test -p xi-rope --features serde) before running parity tests.");

        using var stream = File.OpenRead(path);
        var result = JsonSerializer.Deserialize<T>(stream, SerializerOptions);
        Assert.NotNull(result);
        return result!;
    }

    private static string ResolveFixturePath(params string[] segments)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var parts = new List<string> { projectRoot, "Fixtures" };
        parts.AddRange(segments);
        return Path.Combine(parts.ToArray());
    }
}

internal sealed record ChunkDescriptorFixture
{
    [JsonPropertyName("metadata")]
    public ChunkDescriptorMetadata Metadata { get; init; } = new();

    [JsonPropertyName("chunk_descriptors")]
    public List<ChunkDescriptor> ChunkDescriptors { get; init; } = new();

    [JsonPropertyName("line_descriptors")]
    public List<LineDescriptor> LineDescriptors { get; init; } = new();
}

internal sealed record ChunkDescriptorMetadata
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("rust_commit")]
    public string RustCommit { get; init; } = string.Empty;

    [JsonPropertyName("generated_at_unix_millis")]
    public long GeneratedAtUnixMillis { get; init; }

    [JsonPropertyName("chunk_descriptor_count")]
    public int ChunkDescriptorCount { get; init; }

    [JsonPropertyName("line_descriptor_count")]
    public int LineDescriptorCount { get; init; }
}

internal sealed record ChunkDescriptor
{
    [JsonPropertyName("sample")]
    public string Sample { get; init; } = string.Empty;

    [JsonPropertyName("chunk_index")]
    public int ChunkIndex { get; init; }

    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("byte_range")]
    public RangeDescriptor ByteRange { get; init; } = new();

    [JsonPropertyName("utf16_range")]
    public RangeDescriptor Utf16Range { get; init; } = new();

    [JsonPropertyName("leaf_range")]
    public RangeDescriptor LeafRange { get; init; } = new();

    [JsonPropertyName("contains_crlf")]
    public bool ContainsCrlf { get; init; }

    [JsonPropertyName("is_empty")]
    public bool IsEmpty { get; init; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = new();

    [JsonPropertyName("path")]
    public List<ChunkPathEntry> Path { get; init; } = new();

    [JsonPropertyName("context")]
    public ChunkContext Context { get; init; } = new();
}

internal sealed record ChunkPathEntry
{
    [JsonPropertyName("node_height")]
    public int NodeHeight { get; init; }

    [JsonPropertyName("node_len")]
    public int NodeLength { get; init; }

    [JsonPropertyName("child_index")]
    public int ChildIndex { get; init; }

    [JsonPropertyName("child_offset")]
    public int ChildOffset { get; init; }
}

internal sealed record ChunkContext
{
    [JsonPropertyName("before")]
    public string Before { get; init; } = string.Empty;

    [JsonPropertyName("after")]
    public string After { get; init; } = string.Empty;
}

internal sealed record LineDescriptor
{
    [JsonPropertyName("sample")]
    public string Sample { get; init; } = string.Empty;

    [JsonPropertyName("line_index")]
    public int LineIndex { get; init; }

    [JsonPropertyName("raw")]
    public string Raw { get; init; } = string.Empty;

    [JsonPropertyName("logical")]
    public string Logical { get; init; } = string.Empty;

    [JsonPropertyName("byte_range")]
    public RangeDescriptor ByteRange { get; init; } = new();

    [JsonPropertyName("utf16_range")]
    public RangeDescriptor Utf16Range { get; init; } = new();

    [JsonPropertyName("newline_kind")]
    public string NewlineKind { get; init; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = new();
}

internal sealed record GraphemeDescriptorFixture
{
    [JsonPropertyName("metadata")]
    public GraphemeDescriptorMetadata Metadata { get; init; } = new();

    [JsonPropertyName("grapheme_descriptors")]
    public List<GraphemeDescriptor> GraphemeDescriptors { get; init; } = new();
}

internal sealed record GraphemeDescriptorMetadata
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("rust_commit")]
    public string RustCommit { get; init; } = string.Empty;

    [JsonPropertyName("generated_at_unix_millis")]
    public long GeneratedAtUnixMillis { get; init; }

    [JsonPropertyName("descriptor_count")]
    public int DescriptorCount { get; init; }
}

internal sealed record GraphemeDescriptor
{
    [JsonPropertyName("sample")]
    public string Sample { get; init; } = string.Empty;

    [JsonPropertyName("cluster_index")]
    public int ClusterIndex { get; init; }

    [JsonPropertyName("cluster")]
    public string Cluster { get; init; } = string.Empty;

    [JsonPropertyName("byte_range")]
    public RangeDescriptor ByteRange { get; init; } = new();

    [JsonPropertyName("utf16_range")]
    public RangeDescriptor Utf16Range { get; init; } = new();

    [JsonPropertyName("scalar_count")]
    public int ScalarCount { get; init; }

    [JsonPropertyName("contains_zwj")]
    public bool ContainsZwj { get; init; }

    [JsonPropertyName("is_ascii")]
    public bool IsAscii { get; init; }

    [JsonPropertyName("crosses_leaf")]
    public bool CrossesLeaf { get; init; }

    [JsonPropertyName("requires_fallback")]
    public bool RequiresFallback { get; init; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = new();

    [JsonPropertyName("context")]
    public GraphemeContext Context { get; init; } = new();

    [JsonPropertyName("leaf")]
    public GraphemeLeaf Leaf { get; init; } = new();
}

internal sealed record GraphemeContext
{
    [JsonPropertyName("before")]
    public string Before { get; init; } = string.Empty;

    [JsonPropertyName("after")]
    public string After { get; init; } = string.Empty;
}

internal sealed record GraphemeLeaf
{
    [JsonPropertyName("range")]
    public RangeDescriptor Range { get; init; } = new();

    [JsonPropertyName("path")]
    public List<GraphemeLeafPathEntry> Path { get; init; } = new();
}

internal sealed record GraphemeLeafPathEntry
{
    [JsonPropertyName("node_height")]
    public int NodeHeight { get; init; }

    [JsonPropertyName("node_len")]
    public int NodeLength { get; init; }

    [JsonPropertyName("child_index")]
    public int ChildIndex { get; init; }

    [JsonPropertyName("child_offset")]
    public int ChildOffset { get; init; }
}

internal sealed record RangeDescriptor
{
    [JsonPropertyName("start")]
    public int Start { get; init; }

    [JsonPropertyName("end")]
    public int End { get; init; }
}
