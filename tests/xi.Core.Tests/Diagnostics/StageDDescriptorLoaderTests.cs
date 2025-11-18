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

        Assert.Equal("bea8a3360131a0840d26aa75daba9184b70d50b7", manifest.Metadata.RustCommit);
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
        // Hash recorded from fixtures.manifest.json in the 2025-11-19 Stage D refresh.
        Assert.Equal(
            "f61d10167b65f159a44e88c56ee286db964108afd1496d5a942cc5ee8a48c385",
            chunkEntry.PayloadHash);

        var graphemeEntry = Assert.Single(
            manifest.Fixtures,
            f => f.Name == "grapheme_descriptors.json");
        Assert.Equal(668, graphemeEntry.Count);
        Assert.Equal(
            "tests/xi.Core.Tests/Fixtures/grapheme_descriptors/grapheme_descriptors.json",
            graphemeEntry.Path);
        Assert.Equal("grapheme_descriptors@1.0.0", graphemeEntry.SchemaHash);
        // Hash recorded from fixtures.manifest.json in the 2025-11-19 Stage D refresh.
        Assert.Equal(
            "aa5600996bcad5c37227d1731847b1c9404916c4cc6bca9ed4414c917d585667",
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
            "7b948fc6e433482cf531ec43dc74fb943612927d2fe89ba85b47658a21610dbe",
            breaksLedger.PayloadHash);

        var diffLedger = Assert.Single(manifest.Fixtures, f => f.Name == DiffManifestName);
        Assert.Equal(3, diffLedger.Count);
        Assert.Equal(
            "a309f2f1306b6a1892da40ef90fb21d44deabe5758297e2474f51d1a5c999cf9",
            diffLedger.PayloadHash);

        var searchLedger = Assert.Single(manifest.Fixtures, f => f.Name == SearchManifestName);
        Assert.Equal(3, searchLedger.Count);
        Assert.Equal(
            "2fb14e0ca5c7b103a9ad050351547ccff3e1ba46095f9e111d66814c7415d9c5",
            searchLedger.PayloadHash);
    }
}
