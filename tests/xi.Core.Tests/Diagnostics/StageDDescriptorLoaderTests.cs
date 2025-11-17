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

        Assert.Equal("96ce8ddff31f368b52ca930b3930f1cf8ecd909a", manifest.Metadata.RustCommit);
        Assert.Equal("0.3.0", manifest.Metadata.CliRevision);
        Assert.Equal("1.0.0", manifest.Metadata.SchemaVersion);
        Assert.Equal(20, manifest.Metadata.ChunkDescriptorCount);
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
        // Hash recorded from fixtures.manifest.json in the 2025-11-18 Stage D refresh.
        Assert.Equal(
            "e62a4faa936a20b261b167ddbd2be3b4d3566f3099149b2be215fbfafeecd756",
            chunkEntry.PayloadHash);

        var graphemeEntry = Assert.Single(
            manifest.Fixtures,
            f => f.Name == "grapheme_descriptors.json");
        Assert.Equal(668, graphemeEntry.Count);
        Assert.Equal(
            "tests/xi.Core.Tests/Fixtures/grapheme_descriptors/grapheme_descriptors.json",
            graphemeEntry.Path);
        Assert.Equal("grapheme_descriptors@1.0.0", graphemeEntry.SchemaHash);
        // Hash recorded from fixtures.manifest.json in the 2025-11-18 Stage D refresh.
        Assert.Equal(
            "c6b1721d29286f01e67d2b7491361c2636a6a6affcc9e15206fb31c2fa5d1f0e",
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
            "ab2f746e2bdd945e69b0acf9cd275c068a5ba6546f52a820144244f3b0e6e22e",
            breaksLedger.PayloadHash);

        var diffLedger = Assert.Single(manifest.Fixtures, f => f.Name == DiffManifestName);
        Assert.Equal(3, diffLedger.Count);
        Assert.Equal(
            "8c400ea433b77d9aa4f7b0cb57cbbcd6d1b935d52ff7e61101ad7a8babac5076",
            diffLedger.PayloadHash);

        var searchLedger = Assert.Single(manifest.Fixtures, f => f.Name == SearchManifestName);
        Assert.Equal(3, searchLedger.Count);
        Assert.Equal(
            "ce068c5217d2c45d213609d538e5a30280d10559894ef81bb9692d75d23c3502",
            searchLedger.PayloadHash);
    }
}
