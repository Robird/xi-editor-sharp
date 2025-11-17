using System;
using System.IO;
using Xi.Core.Rope.Diagnostics.Descriptors;
using Xunit;

namespace Xi.Core.Tests.Diagnostics;

public sealed class StageDDescriptorLoaderTests
{
    private static readonly string FixtureRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));

    private static readonly Lazy<StageDDescriptorManifest> Manifest = new(
        () => StageDDescriptorLoader.LoadFromFixtureDirectory(FixtureRoot));

    [Fact]
    public void LoadFromFixtureDirectory_populates_manifest_metadata()
    {
        var manifest = Manifest.Value;

        Assert.Equal("7ac917a05be4bb526844d5cdaa842030411800e5", manifest.Metadata.RustCommit);
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
            "52aa448cf565df821bd9798ead94caee73a0e4798e73fade542951d10fa6632e",
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
            "109d57d39b83618d0da2d091144ebd9ebe5e3fa53e4d1c00f720c7f721ab5e2a",
            graphemeEntry.PayloadHash);
    }
}
