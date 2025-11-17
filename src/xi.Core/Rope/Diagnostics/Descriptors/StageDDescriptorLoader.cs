using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Xi.Core.Rope.Diagnostics.Descriptors;

/// <summary>
/// Loads Stage D descriptor fixtures (manifest + chunk/line/grapheme payloads) from disk so
/// QA tooling can reason about <c>tests/xi.Core.Tests/Fixtures</c> without duplicating JSON parsing logic.
/// </summary>
internal static class StageDDescriptorLoader
{
    private const string ManifestFileName = "fixtures.manifest.json";
    private const string ChunkDirectoryName = "chunk_descriptors";
    private const string ChunkFileName = "chunk_descriptors.json";
    private const string GraphemeDirectoryName = "grapheme_descriptors";
    private const string GraphemeFileName = "grapheme_descriptors.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Reads the latest Stage D manifest + descriptor payloads from the provided fixture root.
    /// </summary>
    /// <param name="fixtureDirectory">Absolute or relative path to <c>tests/xi.Core.Tests/Fixtures</c>.</param>
    /// <exception cref="ArgumentException">Thrown when the directory argument is null/empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
    /// <exception cref="FileNotFoundException">Thrown when a required JSON payload is missing.</exception>
    /// <exception cref="InvalidDataException">Thrown when payload metadata is missing or inconsistent.</exception>
    public static StageDDescriptorManifest LoadFromFixtureDirectory(string fixtureDirectory)
    {
        if (string.IsNullOrWhiteSpace(fixtureDirectory))
        {
            throw new ArgumentException("Fixture directory path cannot be null or whitespace.", nameof(fixtureDirectory));
        }

        if (!Directory.Exists(fixtureDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Stage D fixture directory '{fixtureDirectory}' was not found. " +
                "Run 'python scripts/refresh_all_assets.py --only stage-d-fixtures' to regenerate the assets.");
        }

        var manifest = Deserialize<FixtureManifest>(Path.Combine(fixtureDirectory, ManifestFileName), "manifest");
        var chunkPayload = Deserialize<ChunkDescriptorPayload>(
            Path.Combine(fixtureDirectory, ChunkDirectoryName, ChunkFileName),
            "chunk descriptor payload");
        var graphemePayload = Deserialize<GraphemeDescriptorPayload>(
            Path.Combine(fixtureDirectory, GraphemeDirectoryName, GraphemeFileName),
            "grapheme descriptor payload");

        if (chunkPayload.Metadata is null)
        {
            throw new InvalidDataException("Chunk descriptor payload is missing the required 'metadata' object.");
        }

        if (graphemePayload.Metadata is null)
        {
            throw new InvalidDataException("Grapheme descriptor payload is missing the required 'metadata' object.");
        }

        var chunkDescriptors = MaterializeList(chunkPayload.ChunkDescriptors);
        var lineDescriptors = MaterializeList(chunkPayload.LineDescriptors);
        var graphemeDescriptors = MaterializeList(graphemePayload.GraphemeDescriptors);

        var ledgerEntries = manifest.Fixtures
            .Select(entry => new StageDFixtureLedgerEntry
            {
                Name = entry.Name,
                Path = entry.Path,
                Count = entry.Count,
                SchemaHash = entry.SchemaHash,
                PayloadHash = entry.PayloadHash
            })
            .ToList();

        ValidateCount(
            chunkDescriptors.Count,
            chunkPayload.Metadata.ChunkDescriptorCount,
            "chunk_descriptors",
            "chunk descriptor payload");
        ValidateCount(
            lineDescriptors.Count,
            chunkPayload.Metadata.LineDescriptorCount,
            "line_descriptors",
            "chunk descriptor payload");
        ValidateCount(
            graphemeDescriptors.Count,
            graphemePayload.Metadata.DescriptorCount,
            "grapheme_descriptors",
            "grapheme descriptor payload");

        ValidateLedgerEntry(
            ledgerEntries,
            ChunkFileName,
            chunkDescriptors.Count + lineDescriptors.Count,
            chunkPayload.Metadata.SchemaVersion);
        ValidateLedgerEntry(
            ledgerEntries,
            GraphemeFileName,
            graphemeDescriptors.Count,
            graphemePayload.Metadata.SchemaVersion);

        var metadata = new StageDDescriptorManifestMetadata
        {
            SchemaVersion = chunkPayload.Metadata.SchemaVersion,
            RustCommit = FirstValue(
                manifest.RustCommit,
                chunkPayload.Metadata.RustCommit,
                graphemePayload.Metadata.RustCommit) ?? string.Empty,
            CliRevision = NullIfEmpty(manifest.CliRevision),
            GeneratedAtUnixMillis = chunkPayload.Metadata.GeneratedAtUnixMillis,
            ChunkDescriptorCount = chunkPayload.Metadata.ChunkDescriptorCount,
            LineDescriptorCount = chunkPayload.Metadata.LineDescriptorCount,
            GraphemeDescriptorCount = graphemePayload.Metadata.DescriptorCount,
            FeatureGates = MaterializeList(manifest.FeatureGates)
        };

        return new StageDDescriptorManifest
        {
            Metadata = metadata,
            ChunkDescriptors = chunkDescriptors,
            LineDescriptors = lineDescriptors,
            GraphemeDescriptors = graphemeDescriptors,
            Fixtures = ledgerEntries
        };
    }

    private static T Deserialize<T>(string path, string label)
        where T : class
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Missing Stage D {label} at '{path}'. Re-run 'python scripts/refresh_all_assets.py --only stage-d-fixtures' so QA fixtures stay in sync.",
                path);
        }

        using var stream = File.OpenRead(path);
        var result = JsonSerializer.Deserialize<T>(stream, SerializerOptions);
        if (result is null)
        {
            throw new InvalidDataException(
                $"Unable to parse Stage D {label} at '{path}'. Ensure the schema matches docs/architecture/fixtures/parity-fixture-schema.md.");
        }

        return result;
    }

    private static IList<T> MaterializeList<T>(IList<T>? source)
    {
        return source ?? new List<T>();
    }

    private static void ValidateLedgerEntry(
        IList<StageDFixtureLedgerEntry> fixtures,
        string requiredName,
        int expectedCount,
        string? expectedSchemaVersion)
    {
        var entry = fixtures.FirstOrDefault(f => string.Equals(f.Name, requiredName, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            throw new InvalidDataException(
                $"Stage D manifest is missing the '{requiredName}' ledger entry. Re-run 'python scripts/refresh_all_assets.py --only stage-d-fixtures' to hydrate manifest metadata.");
        }

        if (entry.Count != expectedCount)
        {
            throw new InvalidDataException(
                $"Stage D manifest entry '{requiredName}' reports {entry.Count} records but the JSON payload exposed {expectedCount}. Run the manifest verifier script to regenerate canonical hashes.");
        }

        if (!string.IsNullOrWhiteSpace(expectedSchemaVersion) &&
            !entry.SchemaHash.Contains(expectedSchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Stage D manifest entry '{requiredName}' recorded schema hash '{entry.SchemaHash}' which does not reference schema version '{expectedSchemaVersion}'.");
        }
    }

    private static void ValidateCount(int actual, int expected, string fieldName, string payloadLabel)
    {
        if (expected == 0 && actual == 0)
        {
            return;
        }

        if (expected != actual)
        {
            throw new InvalidDataException(
                $"Stage D {payloadLabel} reported {expected} entries for '{fieldName}' but the JSON contained {actual} records.");
        }
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? FirstValue(params string?[] candidates)
    {
        return candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
    }

    private sealed class FixtureManifest
    {
        [JsonPropertyName("rust_commit")]
        public string? RustCommit { get; set; }

        [JsonPropertyName("cli_rev")]
        public string? CliRevision { get; set; }

        [JsonPropertyName("feature_gates")]
        public IList<string> FeatureGates { get; set; } = new List<string>();

        [JsonPropertyName("fixtures")]
        public IList<FixtureManifestEntry> Fixtures { get; set; } = new List<FixtureManifestEntry>();
    }

    private sealed class FixtureManifestEntry
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("schema_hash")]
        public string SchemaHash { get; set; } = string.Empty;

        [JsonPropertyName("payload_hash")]
        public string PayloadHash { get; set; } = string.Empty;
    }

    private sealed class ChunkDescriptorPayload
    {
        [JsonPropertyName("metadata")]
        public ChunkDescriptorMetadata Metadata { get; set; } = new();

        [JsonPropertyName("chunk_descriptors")]
        public IList<ChunkDescriptor> ChunkDescriptors { get; set; } = new List<ChunkDescriptor>();

        [JsonPropertyName("line_descriptors")]
        public IList<LineDescriptor>? LineDescriptors { get; set; }
    }

    private sealed class ChunkDescriptorMetadata
    {
        [JsonPropertyName("schema_version")]
        public string SchemaVersion { get; set; } = string.Empty;

        [JsonPropertyName("rust_commit")]
        public string? RustCommit { get; set; }

        [JsonPropertyName("generated_at_unix_millis")]
        public long GeneratedAtUnixMillis { get; set; }

        [JsonPropertyName("chunk_descriptor_count")]
        public int ChunkDescriptorCount { get; set; }

        [JsonPropertyName("line_descriptor_count")]
        public int LineDescriptorCount { get; set; }
    }

    private sealed class GraphemeDescriptorPayload
    {
        [JsonPropertyName("metadata")]
        public GraphemeDescriptorMetadata Metadata { get; set; } = new();

        [JsonPropertyName("grapheme_descriptors")]
        public IList<GraphemeDescriptor> GraphemeDescriptors { get; set; } = new List<GraphemeDescriptor>();
    }

    private sealed class GraphemeDescriptorMetadata
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
}
