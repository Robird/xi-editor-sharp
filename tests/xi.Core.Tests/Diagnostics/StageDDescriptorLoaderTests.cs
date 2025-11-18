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

        Assert.Equal("3799d2be9db0ef040517ed69df1b717e96a8958e", manifest.Metadata.RustCommit);
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
        // Hash copied from tests/xi.Core.Tests/Fixtures/fixtures.manifest.json (Stage D refresh 2025-11-20).
        Assert.Equal(
            "69ba7f2536876ad3a76296a215ac163fa82629934a97a82e49faa25423f9415a",
            chunkEntry.PayloadHash);

        var graphemeEntry = Assert.Single(
            manifest.Fixtures,
            f => f.Name == "grapheme_descriptors.json");
        Assert.Equal(668, graphemeEntry.Count);
        Assert.Equal(
            "tests/xi.Core.Tests/Fixtures/grapheme_descriptors/grapheme_descriptors.json",
            graphemeEntry.Path);
        Assert.Equal("grapheme_descriptors@1.0.0", graphemeEntry.SchemaHash);
        // Hash copied from tests/xi.Core.Tests/Fixtures/fixtures.manifest.json (Stage D refresh 2025-11-20).
        Assert.Equal(
            "eb0c7c66069ca33a3626ed3909754da72223f6e0e283d6c7b2b3182a0a35182c",
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
            "5d37a7313730dd0ae9c292ca45daa442c11fe63d45f9ad21bad664f919c13f86",
            breaksLedger.PayloadHash);

        var diffLedger = Assert.Single(manifest.Fixtures, f => f.Name == DiffManifestName);
        Assert.Equal(3, diffLedger.Count);
        Assert.Equal(
            "fe76ed31cff4549ffd3f32bd3847dda15ced7538c4165b4566f782e715572562",
            diffLedger.PayloadHash);

        var searchLedger = Assert.Single(manifest.Fixtures, f => f.Name == SearchManifestName);
        Assert.Equal(3, searchLedger.Count);
        Assert.Equal(
            "7eac7ecf3bb0bdbf5369b6b0dcb17bf5241d8c914af791440e2c5a4582ee7d57",
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
