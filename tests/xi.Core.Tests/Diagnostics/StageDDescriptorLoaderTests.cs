using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xi.Core.Rope.Diagnostics.Descriptors;
using Xunit;

namespace Xi.Core.Tests.Diagnostics;

public sealed class StageDDescriptorLoaderTests
{
    private static readonly string FixtureRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));

    private static readonly string StageDParityRoot = Path.Combine(FixtureRoot, "ParityFixtures", "StageD");
    private static readonly string BreaksSamplePath = Path.Combine(StageDParityRoot, "breaks_descriptors.sample.json");
    private static readonly string DiffSamplePath = Path.Combine(StageDParityRoot, "diff_regions.sample.json");
    private static readonly string SearchSamplePath = Path.Combine(StageDParityRoot, "search_spans.sample.json");
    private const string BreaksManifestName = "breaks_descriptors.json";
    private const string DiffManifestName = "diff_regions.json";
    private const string SearchManifestName = "search_spans.json";

    private static readonly Lazy<StageDDescriptorManifest> Manifest = new(
        () => StageDDescriptorLoader.LoadFromFixtureDirectory(FixtureRoot));

    [Fact]
    public void LoadFromFixtureDirectory_populates_manifest_metadata()
    {
        var manifest = Manifest.Value;

        Assert.Equal("f740a440eacb14b59a1ed26388ebc137827e621d", manifest.Metadata.RustCommit);
        Assert.Equal("0.3.0", manifest.Metadata.CliRevision);
        Assert.Equal("1.0.0", manifest.Metadata.SchemaVersion);
        Assert.Equal(9, manifest.Metadata.ChunkDescriptorCount);
        Assert.Equal(11, manifest.Metadata.LineDescriptorCount);
        Assert.Equal(668, manifest.Metadata.GraphemeDescriptorCount);
        Assert.Contains("serde", manifest.Metadata.FeatureGates);
    }

    [Fact]
    public void LoadFromFixtureDirectory_projects_chunk_descriptor()
    {
        var manifest = Manifest.Value;
        var chunk = Assert.Single(manifest.ChunkDescriptors, d => d.Sample == "emoji_cluster_block");

        Assert.Equal(37, chunk.Utf16Range.End);
        Assert.False(chunk.ContainsCrlf);
        Assert.Contains("emoji", chunk.Tags);
    }

    [Fact]
    public void LoadFromFixtureDirectory_projects_grapheme_descriptor()
    {
        var manifest = Manifest.Value;
        var grapheme = Assert.Single(
            manifest.GraphemeDescriptors,
            g => g.Sample == "zwj_family" && g.Cluster == "👩‍👩‍👧‍👦");

        Assert.True(grapheme.ContainsZwj);
        Assert.True(grapheme.RequiresFallback);
        Assert.Equal(7, grapheme.ScalarCount);
    }

    [Fact]
    public void LoadFromFixtureDirectory_projects_manifest_ledger()
    {
        var manifest = Manifest.Value;

        var chunkEntry = Assert.Single(
            manifest.Fixtures,
            f => f.Name == "chunk_descriptors.json");
        Assert.Equal(20, chunkEntry.Count);
        Assert.Equal(
            "tests/xi.Core.Tests/Fixtures/chunk_descriptors/chunk_descriptors.json",
            chunkEntry.Path);
        Assert.Equal("chunk_descriptors@1.0.0", chunkEntry.SchemaHash);
        Assert.Equal(
            "4c13cbf9f750f205f9d0e7857fb60ed552f032aebc7833cc19276b26539b81dd",
            chunkEntry.PayloadHash);

        var graphemeEntry = Assert.Single(
            manifest.Fixtures,
            f => f.Name == "grapheme_descriptors.json");
        Assert.Equal(668, graphemeEntry.Count);
        Assert.Equal(
            "tests/xi.Core.Tests/Fixtures/grapheme_descriptors/grapheme_descriptors.json",
            graphemeEntry.Path);
        Assert.Equal("grapheme_descriptors@1.0.0", graphemeEntry.SchemaHash);
        Assert.Equal(
            "45c01de9d036e402f3a5f7a00b67fd7974a82427a31646da5a70483122c5ccd3",
            graphemeEntry.PayloadHash);
    }

    [Fact]
    public void LoadFromFixtureDirectory_projects_optional_stage_d_assets()
    {
        var manifest = LoadAugmentedManifest();

        var breaks = Assert.Single(manifest.BreaksDescriptors, d => d.Sample == "tiny_soft_wrap");
        Assert.Equal("tiny_soft_wrap", breaks.Sample);
        Assert.Equal(3, breaks.BreakCount);

        var diff = Assert.Single(manifest.DiffRegions, d => d.Sample == "tiny_diff_case");
        Assert.Equal("tiny_diff_case", diff.Sample);
        Assert.Collection(
            diff.Ops,
            op => Assert.Equal("copy", op.Kind),
            op => Assert.Equal("delete", op.Kind),
            op => Assert.Equal("insert", op.Kind));

        var search = Assert.Single(manifest.SearchSpans, s => s.Sample == "simple_query");
        Assert.Equal("simple_query", search.Sample);
        Assert.Equal("rope", search.Query);
    }

    private static StageDDescriptorManifest LoadAugmentedManifest()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var manifestPath = Path.Combine(tempDirectory, "fixtures.manifest.json");
            File.WriteAllText(manifestPath, BuildAugmentedManifestJson());
            return StageDDescriptorLoader.LoadFromFixtureDirectory(tempDirectory);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors during test shutdown.
            }
        }
    }

    private static string BuildAugmentedManifestJson()
    {
        var baselineManifestPath = Path.Combine(FixtureRoot, "fixtures.manifest.json");
        var baselineContent = File.ReadAllText(baselineManifestPath);
        var manifest = JsonSerializer.Deserialize<FixtureManifestDocument>(baselineContent)
                       ?? throw new InvalidOperationException("Unable to hydrate baseline manifest");

        manifest.rust_commit = "stage-d-test";
        manifest.cli_rev = "0.0-test";
        manifest.feature_gates = manifest.feature_gates
            .Concat(new[] { "breaks_diagnostics", "diff_regions", "search_spans" })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        manifest.fixtures = manifest.fixtures
            .Where(entry => !string.Equals(entry.name, BreaksManifestName, StringComparison.OrdinalIgnoreCase)
                             && !string.Equals(entry.name, DiffManifestName, StringComparison.OrdinalIgnoreCase)
                             && !string.Equals(entry.name, SearchManifestName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var entry in manifest.fixtures)
        {
            if (Path.IsPathRooted(entry.path))
            {
                continue;
            }

            var normalized = entry.path
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            var marker = $"tests{Path.DirectorySeparatorChar}xi.Core.Tests{Path.DirectorySeparatorChar}Fixtures";
            var markerIndex = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                var relative = normalized
                    .Substring(markerIndex + marker.Length)
                    .TrimStart(Path.DirectorySeparatorChar);
                entry.path = Path.Combine(FixtureRoot, relative);
            }
            else
            {
                entry.path = Path.Combine(FixtureRoot, normalized);
            }
        }

        manifest.fixtures.AddRange(new[]
        {
            new FixtureManifestEntryDocument
            {
                name = BreaksManifestName,
                path = BreaksSamplePath,
                count = 1,
                schema_hash = "breaks_descriptors@1.0.0",
                payload_hash = "sample"
            },
            new FixtureManifestEntryDocument
            {
                name = DiffManifestName,
                path = DiffSamplePath,
                count = 1,
                schema_hash = "diff_regions@1.0.0",
                payload_hash = "sample"
            },
            new FixtureManifestEntryDocument
            {
                name = SearchManifestName,
                path = SearchSamplePath,
                count = 1,
                schema_hash = "search_spans@1.0.0",
                payload_hash = "sample"
            }
        });

        return JsonSerializer.Serialize(
            manifest,
            new JsonSerializerOptions { WriteIndented = true });
    }

    private sealed class FixtureManifestDocument
    {
        public string? rust_commit { get; set; }

        public string? cli_rev { get; set; }

        public List<string> feature_gates { get; set; } = new();

        public List<FixtureManifestEntryDocument> fixtures { get; set; } = new();
    }

    private sealed class FixtureManifestEntryDocument
    {
        public string name { get; set; } = string.Empty;

        public string path { get; set; } = string.Empty;

        public int count { get; set; }

        public string schema_hash { get; set; } = string.Empty;

        public string payload_hash { get; set; } = string.Empty;
    }
}
