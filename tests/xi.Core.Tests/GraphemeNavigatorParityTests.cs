using System;
using System.Collections.Generic;
using System.Linq;
using Xi.Core.Rope.Navigation;
using Xi.Core.Rope.Tree;
using Xi.Core.Tests.Fixtures.ParityFixtures;
using Xunit;

using RopeBuffer = Xi.Core.Rope.Rope;

namespace Xi.Core.Tests;

public sealed class GraphemeNavigatorParityTests
{
    [Fact]
    public void DegradedNavigatorMatchesGraphemeFixtures()
    {
        var fixture = ParityFixtureLoader.GraphemeFixtures;
        var grouped = fixture.GraphemeDescriptors
            .GroupBy(d => d.Sample, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(d => d.ClusterIndex).ToList(),
                StringComparer.Ordinal);

        foreach (var kvp in grouped)
        {
            ValidateSample(kvp.Key, kvp.Value);
        }
    }

    private static void ValidateSample(string sample, IReadOnlyList<GraphemeDescriptor> descriptors)
    {
        var text = BuildSampleText(descriptors);
        var rope = BuildRope(text, descriptors);
        var navigator = new DegradedGraphemeNavigator();
        var metrics = navigator.Metrics;

        for (int i = 0; i < descriptors.Count; i++)
        {
            var descriptor = descriptors[i];
            ValidateDescriptor(rope, navigator, metrics, descriptor);
        }
    }

    private static void ValidateDescriptor(RopeBuffer rope, DegradedGraphemeNavigator navigator, GraphemeNavigationMetrics metrics, GraphemeDescriptor descriptor)
    {
        var start = descriptor.Utf16Range.Start;
        var end = descriptor.Utf16Range.End;
        var expectedLength = Math.Max(0, end - start);

        metrics.Reset();
        var forwardCursor = new NodeCursor(rope, start);
        var next = navigator.MoveNext(forwardCursor);

        Assert.Equal(end, next);
        Assert.Equal(end, forwardCursor.Position);
        Assert.Equal(descriptor.Cluster, rope.GetSlice(start, expectedLength));

        var snapshot = metrics.GetSnapshot();
        Assert.Equal(1, snapshot.MoveNextCalls);
        if (!descriptor.RequiresFallback)
        {
            Assert.Equal(0, snapshot.ScalarFallbacks);
        }

        metrics.Reset();
        var backwardCursor = new NodeCursor(rope, end);
        var previous = navigator.MovePrevious(backwardCursor);

        Assert.Equal(start, previous);
        Assert.Equal(start, backwardCursor.Position);

        snapshot = metrics.GetSnapshot();
        Assert.Equal(1, snapshot.MovePreviousCalls);
        if (!descriptor.RequiresFallback)
        {
            Assert.Equal(0, snapshot.ScalarFallbacks);
        }

        var crossesLeaf = CrossesLeaf(rope, start, end);
        if (!descriptor.CrossesLeaf)
        {
            Assert.False(crossesLeaf,
                $"Sample '{descriptor.Sample}' cluster {descriptor.ClusterIndex} was expected to stay within a single leaf but crossed one in C#.");
        }
    }

    private static string BuildSampleText(IEnumerable<GraphemeDescriptor> descriptors)
    {
        return string.Concat(descriptors.Select(d => d.Cluster));
    }

    private static RopeBuffer BuildRope(string text, IReadOnlyList<GraphemeDescriptor> descriptors)
    {
        var rope = new RopeBuffer();
        if (!string.IsNullOrEmpty(text))
        {
            var leafRanges = descriptors
                .Select(d => d.Leaf?.Range)
                .Where(r => r is not null)
                .Select(r => (Start: r!.Start, End: r!.End))
                .Distinct()
                .OrderBy(r => r.Start)
                .ToList();

            if (leafRanges.Count <= 1)
            {
                rope.Append(text);
            }
            else
            {
                var covered = 0;
                foreach (var range in leafRanges)
                {
                    var clampedStart = Math.Clamp(range.Start, 0, text.Length);
                    var clampedEnd = Math.Clamp(range.End, clampedStart, text.Length);
                    if (clampedEnd <= clampedStart)
                    {
                        continue;
                    }

                    var segment = text.Substring(clampedStart, clampedEnd - clampedStart);
                    rope.Append(segment);
                    covered = Math.Max(covered, clampedEnd);
                }

                if (covered < text.Length)
                {
                    rope.Append(text.Substring(covered));
                }
            }
        }

        return rope;
    }

    private static bool CrossesLeaf(RopeBuffer rope, int start, int end)
    {
        if (end <= start)
        {
            return false;
        }

        var cursor = new NodeCursor(rope, start);
        var leaf = cursor.GetLeaf();
        if (!leaf.HasValue || leaf.Value.Leaf is null)
        {
            return false;
        }

        var leafStart = cursor.Position - leaf.Value.Offset;
        var leafEnd = leafStart + leaf.Value.Leaf.Length;
        return end > leafEnd;
    }
}
