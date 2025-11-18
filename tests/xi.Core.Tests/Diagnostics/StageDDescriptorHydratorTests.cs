using System;
using System.IO;
using System.Linq;
using Xi.Core.Rope.Diagnostics.Descriptors;
using Xunit;

namespace Xi.Core.Tests.Diagnostics;

public sealed class StageDDescriptorHydratorTests
{
    private static readonly string FixtureRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));

    [Fact]
    public void LoadBreakPlans_projects_first_plan()
    {
        var plans = StageDDescriptorHydrator.LoadBreakPlans(FixtureRoot);

        Assert.NotEmpty(plans);
        var first = plans[0];
        Assert.Equal("ascii_guidance", first.SampleName);
        Assert.Equal(6, first.Breaks.Count);
    }

    [Fact]
    public void LoadDiffRegions_projects_diff_operations()
    {
        var diffs = StageDDescriptorHydrator.LoadDiffRegions(FixtureRoot);

        var diff = Assert.Single(diffs, d => d.Regions.Any(r => r.Sample == "ascii_minimal_ops"));
        var region = Assert.Single(diff.Regions, r => r.Sample == "ascii_minimal_ops");
        Assert.Collection(
            region.Operations,
            op => Assert.Equal("copy", op.Kind),
            op => Assert.Equal("insert", op.Kind),
            op => Assert.Equal("delete", op.Kind),
            op => Assert.Equal("copy", op.Kind));
    }

    [Fact]
    public void LoadSearchSpans_projects_hits_with_context()
    {
        var results = StageDDescriptorHydrator.LoadSearchSpans(FixtureRoot);

        var sample = Assert.Single(results, r => r.Sample == "literal_case_insensitive");
        Assert.Contains(
            sample.Hits,
            hit => hit.ContextAfter?.Contains("parity", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Equal(2, sample.SpanWindows.Count);
    }
}
