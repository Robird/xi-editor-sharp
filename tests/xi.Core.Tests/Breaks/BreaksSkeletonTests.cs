using System;
using Xi.Core.Rope.Breaks;
using Xi.Core.Rope.StageD;
using Xunit;

namespace Xi.Core.Tests.Breaks;

public sealed class BreaksSkeletonTests
{
    [Fact]
    public void BreakPlan_from_descriptor_captures_sample_data()
    {
        var descriptor = new BreakSetDescriptorView(
            "sample_breaks",
            128,
            40,
            "unicode-width",
            new[] { 4, 16, 32 },
            3,
            new[]
            {
                new LeafRunSnapshotView(
                    new RangeSnapshot { Start = 0, End = 64 },
                    3,
                    Array.Empty<PathFrameSnapshot>())
            },
            "lorem ipsum",
            new[] { "breaks", "stage-d" });

        var plan = BreakPlan.FromDescriptor(descriptor);

        Assert.Equal("sample_breaks", plan.SampleName);
        Assert.Equal(40, plan.WrapWidthUnits);
        Assert.Equal("unicode-width", plan.Metric);
        Assert.Equal(3, plan.Breaks.Count);
        Assert.Single(plan.Leaves);
        Assert.Equal(3, plan.Leaves[0].BreakCount);
    }
}
