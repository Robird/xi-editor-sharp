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

        Assert.Equal("bd28ebdf83d2dd2fa6d4bd7c857d2471a21b4999", manifest.Metadata.RustCommit);
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
        // Hash recorded from fixtures.manifest.json in the 2025-11-19 Stage D refresh.
        Assert.Equal(
            "f0c9ada5a8fa6e405b2de207225a0874ca6d62925b9ae3fa92cbe68a5f06785c",
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
            "53701cb66ca1407ff636bd40d96cc5fa3b27d1dd450d89959edb42acf9c3c04f",
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
            "0a82a69f83ae8abd98933af159f59bd01dbd5cf98ce4aadaa4e6452031e147a6",
            breaksLedger.PayloadHash);

        var diffLedger = Assert.Single(manifest.Fixtures, f => f.Name == DiffManifestName);
        Assert.Equal(3, diffLedger.Count);
            Assert.Equal(
                "2c4c366ce1070d56988f1adca2048a701b5b6b273b34e1d8a0c06665d2cce1b9",
            diffLedger.PayloadHash);

        var searchLedger = Assert.Single(manifest.Fixtures, f => f.Name == SearchManifestName);
        Assert.Equal(3, searchLedger.Count);
        Assert.Equal(
            "e7de44d74af62d44fcdfe4be554f88937bb45c93194bc4a5b62a0750e36bb467",
            searchLedger.PayloadHash);
    }
}
