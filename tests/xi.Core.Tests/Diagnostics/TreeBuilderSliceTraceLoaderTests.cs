using System;
using System.IO;
using System.Linq;
using Xi.Core.Rope.Diagnostics.TreeBuilder;
using Xunit;

namespace Xi.Core.Tests.Diagnostics;

public sealed class TreeBuilderSliceTraceLoaderTests
{
    private static readonly string FixtureDirectory = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));

    private static readonly string TraceDirectory = Path.Combine(FixtureDirectory, "tree_builder_slice");

    [Fact]
    public void LoadFromDirectory_discovers_and_hydrates_every_trace()
    {
        var traces = TreeBuilderSliceTraceLoader.LoadFromDirectory(TraceDirectory);
        var expectedCount = Directory.EnumerateFiles(TraceDirectory, "*.json", SearchOption.TopDirectoryOnly).Count();

        Assert.Equal(expectedCount, traces.Count);

        var trace = Assert.Single(traces, t => t.Metadata.SampleName == "basic_slice_plan");
        Assert.Equal(string.Empty, trace.Metadata.RustCommit);
        Assert.Equal(0, trace.Metadata.TimestampUnixMillis);

        var firstEvent = trace.Events.First();
        Assert.Equal(TreeBuilderSliceTraceEventKind.PushFrame, firstEvent.Kind);
        Assert.Equal(1, firstEvent.Depth);
        Assert.Equal(0, firstEvent.NodeHeight);
        Assert.Equal(3, firstEvent.NodeLength);
        Assert.Equal((ulong)1, firstEvent.NodeId);
        Assert.False(firstEvent.Reuse);
    }

    [Fact]
    public void LoadFromDirectory_projects_interval_specific_fields()
    {
        var trace = Assert.Single(TreeBuilderSliceTraceLoader.LoadFromDirectory(TraceDirectory));

                var leafSlice = Assert.Single(trace.Events, evt => evt.Kind == TreeBuilderSliceTraceEventKind.LeafSlice);
                Assert.NotNull(leafSlice.Interval);
                Assert.Equal(1, leafSlice.Interval!.Start);
                Assert.Equal(4, leafSlice.Interval.End);
                Assert.Null(leafSlice.MergedChildren);

                var mergePop = Assert.Single(trace.Events, evt => evt.Kind == TreeBuilderSliceTraceEventKind.MergePop);
                Assert.Equal(1, mergePop.MergedChildren);
                Assert.Null(mergePop.Interval);
    }

        [Fact]
        public void LoadFromDirectory_flattens_nested_kind_payload_fields()
        {
                var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDirectory);
                var tracePath = Path.Combine(tempDirectory, "payload.json");

                File.WriteAllText(tracePath, """
[
    {
        "kind": {
            "kind": "EnterChild",
            "requested": { "start": 2, "end": 6 },
            "translated": { "start": 0, "end": 4 }
        },
        "depth": 2,
        "node_height": 1,
        "node_len": 8,
        "node_id": 42,
        "reuse": true
    }
]
""");

                try
                {
                        var trace = Assert.Single(TreeBuilderSliceTraceLoader.LoadFromDirectory(tempDirectory));
                        var enterChild = Assert.Single(trace.Events);

                        Assert.Equal(TreeBuilderSliceTraceEventKind.EnterChild, enterChild.Kind);
                        Assert.NotNull(enterChild.Requested);
                        Assert.Equal(2, enterChild.Requested!.Start);
                        Assert.Equal(6, enterChild.Requested.End);
                        Assert.NotNull(enterChild.Translated);
                        Assert.Equal(0, enterChild.Translated!.Start);
                        Assert.Equal(4, enterChild.Translated.End);
                }
                finally
                {
                        Directory.Delete(tempDirectory, recursive: true);
                }
        }

    [Fact]
    public void LoadFromDirectory_missing_directory_throws()
    {
        var missingPath = Path.Combine(TraceDirectory, Guid.NewGuid().ToString("N"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => TreeBuilderSliceTraceLoader.LoadFromDirectory(missingPath));

        Assert.Contains("tree builder slice trace directory", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFromManifest_discovers_tree_builder_entries()
    {
        var traces = TreeBuilderSliceTraceLoader.LoadFromManifest(FixtureDirectory);
        var trace = Assert.Single(traces);
        Assert.EndsWith(
            Path.Combine("tree_builder_slice", "basic_slice_plan.json"),
            trace.SourceFile,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFromManifest_missing_manifest_throws()
    {
        var missingRoot = Path.Combine(FixtureDirectory, Guid.NewGuid().ToString("N"));
        var exception = Assert.Throws<FileNotFoundException>(
            () => TreeBuilderSliceTraceLoader.LoadFromManifest(missingRoot));
        Assert.Contains("manifest", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
