using System;
using System.IO;
using System.Linq;
using Xi.Core.Rope.Diagnostics.TreeBuilder;
using Xunit;

namespace Xi.Core.Tests.Diagnostics;

public sealed class TreeBuilderSliceTraceLoaderTests
{
    private static readonly string TraceDirectory = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures", "ParityFixtures", "tree_builder_trace"));

    [Fact]
    public void LoadFromDirectory_discovers_and_hydrates_every_trace()
    {
        var traces = TreeBuilderSliceTraceLoader.LoadFromDirectory(TraceDirectory);
        var expectedCount = Directory.EnumerateFiles(TraceDirectory, "*.json", SearchOption.TopDirectoryOnly).Count();

        Assert.Equal(expectedCount, traces.Count);

        var trace = Assert.Single(traces, t => t.Metadata.SampleName == "basic_slice_plan");
        Assert.Equal("tests-only", trace.Metadata.RustCommit);
        Assert.Equal(1731907200000, trace.Metadata.TimestampUnixMillis);

        var firstEvent = trace.Events.First();
        Assert.Equal(TreeBuilderSliceTraceEventKind.PushFrame, firstEvent.Kind);
        Assert.Equal(0, firstEvent.Depth);
        Assert.Equal(2, firstEvent.NodeHeight);
        Assert.Equal(48, firstEvent.NodeLength);
        Assert.Equal((ulong)1, firstEvent.NodeId);
        Assert.False(firstEvent.Reuse);
    }

    [Fact]
    public void LoadFromDirectory_projects_interval_specific_fields()
    {
        var trace = Assert.Single(TreeBuilderSliceTraceLoader.LoadFromDirectory(TraceDirectory));

        var leafSlice = Assert.Single(trace.Events, evt => evt.Kind == TreeBuilderSliceTraceEventKind.LeafSlice);
        Assert.NotNull(leafSlice.Interval);
        Assert.Equal(0, leafSlice.Interval!.Start);
        Assert.Equal(16, leafSlice.Interval.End);
        Assert.Null(leafSlice.MergedChildren);

        var enterChild = Assert.Single(trace.Events, evt => evt.Kind == TreeBuilderSliceTraceEventKind.EnterChild);
        Assert.NotNull(enterChild.Requested);
        Assert.Equal(12, enterChild.Requested!.Start);
        Assert.Equal(20, enterChild.Requested.End);
        Assert.NotNull(enterChild.Translated);
        Assert.Equal(0, enterChild.Translated!.Start);
        Assert.Equal(8, enterChild.Translated.End);

        var mergePop = Assert.Single(trace.Events, evt => evt.Kind == TreeBuilderSliceTraceEventKind.MergePop);
        Assert.Equal(2, mergePop.MergedChildren);
        Assert.Null(mergePop.Interval);
    }

    [Fact]
    public void LoadFromDirectory_missing_directory_throws()
    {
        var missingPath = Path.Combine(TraceDirectory, Guid.NewGuid().ToString("N"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => TreeBuilderSliceTraceLoader.LoadFromDirectory(missingPath));

        Assert.Contains("tree builder slice trace directory", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
