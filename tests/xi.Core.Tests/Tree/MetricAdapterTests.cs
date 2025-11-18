using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Xi.Core.Rope;
using Xi.Core.Rope.Diagnostics.TreeBuilder;
using Xi.Core.Rope.Tree;
using Xunit;

namespace Xi.Core.Tests.Tree
{
    public sealed class MetricAdapterTests
    {
    private static readonly string FixtureDirectory = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));

    [Fact]
    public void MeasureAggregatesLineCountsAcrossInternalNodes()
    {
        var adapter = MetricAdapter.Default;
        var left = Node.FromLeaf("line1\nline2\n");
        var right = Node.FromLeaf("tail");
        var rope = Node.Concat(left, right);

        var snapshot = adapter.Measure(rope);

        Assert.Equal(rope.Length, snapshot.BaseLength);
        Assert.Equal(rope.Info.Utf16Length, snapshot.Utf16Length);
        Assert.Equal(2, snapshot.LineCount);
        Assert.Equal(2, snapshot.BreakCount);
    }

    [Fact]
    public void ConvertIntervalUtf16RejectsPartialSurrogates()
    {
        var adapter = MetricAdapter.Default;
        var node = Node.FromLeaf("a😀b");

        var converted = adapter.ConvertInterval(
            node,
            new Interval(0, 3),
            MetricAdapter.MetricKind.Base,
            MetricAdapter.MetricKind.Utf16CodeUnits);

        Assert.Equal(new Interval(0, 3), converted);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => adapter.ConvertInterval(
                node,
                new Interval(1, 2),
                MetricAdapter.MetricKind.Utf16CodeUnits,
                MetricAdapter.MetricKind.Base));
    }

    [Fact]
    public void BreaksMetricSurfacesLeafOffsetsInTracerEvents()
    {
        var tracer = new CapturingTreeBuilderTracer();
        var builder = new TreeBuilder
        {
            Tracer = tracer,
        };

        var payload = "alpha\n" + new string('x', 32);
        builder.PushString(payload);
        _ = builder.Build();

        var leafEvent = Assert.Single(tracer.Events, evt => evt.Kind == TreeBuilderEventKind.PushLeaf);
        Assert.Equal(1, leafEvent.LineCount);
        Assert.Equal(1, leafEvent.BreakCount);
        Assert.Equal(new[] { payload.IndexOf('\n') + 1 }, leafEvent.BreakOffsets.ToArray());
    }

    [Fact]
    public void TreeBuilderTraceRoundTripsViaManifest()
    {
        var trace = TreeBuilderSliceTraceLoader.LoadFromManifest(FixtureDirectory).Single();
        var originalNode = JsonNode.Parse(File.ReadAllText(trace.SourceFile));
        Assert.NotNull(originalNode);

        JsonNode regenerated = originalNode switch
        {
            JsonArray => SerializeTraceAsEventArray(trace),
            JsonObject => SerializeTrace(trace),
            _ => throw new InvalidOperationException("Unsupported tree builder trace shape.")
        };

        Assert.True(JsonNode.DeepEquals(originalNode, regenerated));
    }

    private static JsonNode SerializeTrace(TreeBuilderSliceTrace trace)
    {
        var metadata = new JsonObject
        {
            ["sample"] = trace.Metadata.SampleName,
            ["rust_commit"] = trace.Metadata.RustCommit,
            ["generated_at_unix_millis"] = trace.Metadata.TimestampUnixMillis,
        };

        var events = new JsonArray();
        foreach (var evt in trace.Events)
        {
            var evtNode = new JsonObject
            {
                ["kind"] = evt.Kind.ToString(),
                ["depth"] = evt.Depth,
                ["node_height"] = evt.NodeHeight,
                ["node_len"] = evt.NodeLength,
                ["node_id"] = evt.NodeId,
                ["reuse"] = evt.Reuse,
            };

            if (evt.MergedChildren is int mergedChildren)
            {
                evtNode["merged_children"] = mergedChildren;
            }

            if (evt.Interval is not null)
            {
                evtNode["interval"] = new JsonObject
                {
                    ["start"] = evt.Interval.Start,
                    ["end"] = evt.Interval.End,
                };
            }

            if (evt.Requested is not null)
            {
                evtNode["requested"] = new JsonObject
                {
                    ["start"] = evt.Requested.Start,
                    ["end"] = evt.Requested.End,
                };
            }

            if (evt.Translated is not null)
            {
                evtNode["translated"] = new JsonObject
                {
                    ["start"] = evt.Translated.Start,
                    ["end"] = evt.Translated.End,
                };
            }

            events.Add(evtNode);
        }

        return new JsonObject
        {
            ["metadata"] = metadata,
            ["events"] = events,
        };
    }

    private static JsonNode SerializeTraceAsEventArray(TreeBuilderSliceTrace trace)
    {
        var events = new JsonArray();
        foreach (var evt in trace.Events)
        {
            var evtNode = new JsonObject
            {
                ["kind"] = SerializeKindPayload(evt),
                ["depth"] = evt.Depth,
                ["node_height"] = evt.NodeHeight,
                ["node_len"] = evt.NodeLength,
                ["node_id"] = evt.NodeId,
                ["reuse"] = evt.Reuse,
            };

            events.Add(evtNode);
        }

        return events;
    }

    private static JsonObject SerializeKindPayload(TreeBuilderSliceTraceEvent evt)
    {
        var payload = new JsonObject
        {
            ["kind"] = evt.Kind.ToString(),
        };

        if (evt.Interval is not null)
        {
            payload["interval"] = new JsonObject
            {
                ["start"] = evt.Interval.Start,
                ["end"] = evt.Interval.End,
            };
        }

        if (evt.MergedChildren is int mergedChildren)
        {
            payload["merged_children"] = mergedChildren;
        }

        if (evt.Requested is not null)
        {
            payload["requested"] = new JsonObject
            {
                ["start"] = evt.Requested.Start,
                ["end"] = evt.Requested.End,
            };
        }

        if (evt.Translated is not null)
        {
            payload["translated"] = new JsonObject
            {
                ["start"] = evt.Translated.Start,
                ["end"] = evt.Translated.End,
            };
        }

        return payload;
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
}
