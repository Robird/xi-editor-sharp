using System;
using System.Collections.Generic;
using System.Linq;
using Xi.Core.Rope.Tree;
using Xunit;

namespace Xi.Core.Tests;

public class TreeBuilderTracerTests
{
    [Fact]
    public void PushStringAndBuildEmitLeafPopAndBuildEvents()
    {
        var tracer = new CapturingTreeBuilderTracer();
        var builder = new TreeBuilder
        {
            Tracer = tracer,
        };

        builder.PushString("abc");
        var rope = builder.Build();

        AssertKindsInOrder(tracer.Events,
            TreeBuilderEventKind.PushLeaf,
            TreeBuilderEventKind.PopFrame,
            TreeBuilderEventKind.BuildCompleted);

        var leafEvent = tracer.Events.First(e => e.Kind == TreeBuilderEventKind.PushLeaf);
        Assert.Equal("abc", leafEvent.LeafPreview);
        Assert.Equal(3, leafEvent.Utf16Length);
        Assert.Equal(3, leafEvent.ByteLength);

        var buildEvent = tracer.Events.First(e => e.Kind == TreeBuilderEventKind.BuildCompleted);
        Assert.Equal(rope.Info.Utf16Length, buildEvent.Utf16Length);
    }

    [Fact]
    public void ResetEmitsEventWithClearedStackDepth()
    {
        var tracer = new CapturingTreeBuilderTracer();
        var builder = new TreeBuilder
        {
            Tracer = tracer,
        };

        builder.PushNode(Node.FromLeaf("trace me"));
        builder.Reset();

        var resetEvent = tracer.Events.Last(e => e.Kind == TreeBuilderEventKind.Reset);
        Assert.Equal(TreeBuilderEventKind.Reset, resetEvent.Kind);
        Assert.Equal(0, resetEvent.StackDepth);
    }

    private static void AssertKindsInOrder(IEnumerable<TreeBuilderEvent> events, params TreeBuilderEventKind[] expected)
    {
        var kinds = events.Select(e => e.Kind).ToArray();
        var searchStart = 0;
        foreach (var expectedKind in expected)
        {
            var index = Array.IndexOf(kinds, expectedKind, searchStart);
            Assert.True(index >= 0, $"Missing expected event kind {expectedKind} after index {searchStart}.");
            searchStart = index + 1;
        }
    }

    private sealed class CapturingTreeBuilderTracer : ITreeBuilderTracer
    {
        public List<TreeBuilderEvent> Events { get; } = new();

        public bool IsEnabled => true;

        public void Trace(in TreeBuilderEvent builderEvent)
        {
            Events.Add(builderEvent);
        }
    }
}
