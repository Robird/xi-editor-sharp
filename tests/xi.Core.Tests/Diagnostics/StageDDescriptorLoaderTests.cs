using System;
using System.IO;
using Xi.Core.Rope.Diagnostics.Descriptors;
using Xunit;

namespace Xi.Core.Tests.Diagnostics;

public sealed class StageDDescriptorLoaderTests
{
    private static readonly string FixtureRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));

    private const string BreaksManifestName = "breaks_descriptors.json";
    private const string DiffManifestName = "diff_regions.json";
    private const string SearchManifestName = "search_spans.json";

    private static readonly Lazy<StageDDescriptorManifest> Manifest = new(
        () => StageDDescriptorLoader.LoadFromFixtureDirectory(FixtureRoot));

    [Fact]
    public void LoadFromFixtureDirectory_populates_manifest_metadata()
    {
        var manifest = Manifest.Value;

        Assert.Equal("b6fb5999288946daeeadc4b7f9f9756f6dda1f50", manifest.Metadata.RustCommit);
        Assert.Equal("0.3.0", manifest.Metadata.CliRevision);
        Assert.Equal("1.0.0", manifest.Metadata.SchemaVersion);
        Assert.Equal(20, manifest.Metadata.ChunkDescriptorCount);
        Assert.Equal(11, manifest.Metadata.LineDescriptorCount);
        Assert.Equal(668, manifest.Metadata.GraphemeDescriptorCount);
        Assert.Contains("serde", manifest.Metadata.FeatureGates);
        Assert.Contains("cursor_state", manifest.Metadata.FeatureGates);
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
        // Hash copied from tests/xi.Core.Tests/Fixtures/fixtures.manifest.json (Stage D refresh 2025-11-18 20:30 UTC).
        Assert.Equal(
            "9c7163ebd6ceb7f9e7e4ec566682470e96e27424499c4a6f5804898eca48f1f0",
            chunkEntry.PayloadHash);

        var graphemeEntry = Assert.Single(
            manifest.Fixtures,
            f => f.Name == "grapheme_descriptors.json");
        Assert.Equal(668, graphemeEntry.Count);
        Assert.Equal(
            "tests/xi.Core.Tests/Fixtures/grapheme_descriptors/grapheme_descriptors.json",
            graphemeEntry.Path);
        Assert.Equal("grapheme_descriptors@1.0.0", graphemeEntry.SchemaHash);
        // Hash copied from tests/xi.Core.Tests/Fixtures/fixtures.manifest.json (Stage D refresh 2025-11-18 20:30 UTC).
        Assert.Equal(
            "b3484eab43303735c754fa4fb6928255ef7dbe053d2cf41ce60a7c63a7a59095",
            graphemeEntry.PayloadHash);
    }

    [Fact]
    public void LoadFromFixtureDirectory_projects_optional_stage_d_assets()
    {
        var manifest = Manifest.Value;

        Assert.Equal(3, manifest.BreaksDescriptors.Count);
        Assert.Contains(
            manifest.BreaksDescriptors,
            d => d.Sample == "ascii_guidance" && d.BreakCount == 6 && d.Tags.Contains("breaks"));
        Assert.Contains(
            manifest.BreaksDescriptors,
            d => d.Sample == "crlf_emoji_mix" && d.BreakCount == 4 && d.Tags.Contains("emoji"));
        Assert.Contains(
            manifest.BreaksDescriptors,
            d => d.Sample == "delimited_tables" && d.BreakCount == 6 && d.Tags.Contains("tab"));

        var diff = Assert.Single(manifest.DiffRegions, d => d.Sample == "ascii_minimal_ops");
        Assert.Equal(3, manifest.DiffRegions.Count);
        Assert.Collection(
            diff.Ops,
            op => Assert.Equal("copy", op.Kind),
            op => Assert.Equal("insert", op.Kind),
            op => Assert.Equal("delete", op.Kind),
            op => Assert.Equal("copy", op.Kind));

        var search = Assert.Single(manifest.SearchSpans, s => s.Sample == "literal_case_insensitive");
        Assert.Equal(3, manifest.SearchSpans.Count);
        Assert.Equal("stage", search.Query);
        Assert.Contains(search.Hits, h => h.ContextAfter?.Contains("parity", StringComparison.OrdinalIgnoreCase) == true);

        var breaksLedger = Assert.Single(manifest.Fixtures, f => f.Name == BreaksManifestName);
        Assert.Equal(3, breaksLedger.Count);
        Assert.Equal(
            "527ca4f2000b7548a10fe94cf51f6e758f23fa73e7f1ce82347ce92ab61a1f83",
            breaksLedger.PayloadHash);

        var diffLedger = Assert.Single(manifest.Fixtures, f => f.Name == DiffManifestName);
        Assert.Equal(3, diffLedger.Count);
        Assert.Equal(
            "afc04b228f19c7663f1d43597973c0afc90a34159b64ccaf7b9407e6613811a6",
            diffLedger.PayloadHash);

        var searchLedger = Assert.Single(manifest.Fixtures, f => f.Name == SearchManifestName);
        Assert.Equal(3, searchLedger.Count);
        Assert.Equal(
            "599f25b06a36d417cd1ca51a818c0ac5dfcf162c9530368d62cd7075c840df51",
            searchLedger.PayloadHash);
    }

    [Fact]
    public void LoadFromFixtureDirectory_projects_metric_window_ledger()
    {
        var manifest = Manifest.Value;

        var chunkWindows = Assert.Single(
            manifest.MetricWindows,
            entry => entry.Fixture == "chunk_descriptors.json");

        Assert.Equal("metric_windows@1.0.0", chunkWindows.WindowSchema);
        Assert.Contains(chunkWindows.Windows, window => window.Kind == "chunk_windows" && window.Count == 9);
        Assert.Contains(chunkWindows.Windows, window => window.Kind == "line_windows" && window.Count == 11);

        var graphemeWindows = Assert.Single(
            manifest.MetricWindows,
            entry => entry.Fixture == "grapheme_descriptors.json");
        var graphemeMetrics = Assert.Single(graphemeWindows.Windows);
        Assert.Equal("grapheme_windows", graphemeMetrics.Kind);
        Assert.Equal(668, graphemeMetrics.Count);
    }
}
