using Xi.Core.Rope.StageD;
using Xi.Core.Search;
using Xunit;

namespace Xi.Core.Tests.Search;

public sealed class SearchSkeletonTests
{
    [Fact]
    public void SearchResult_from_descriptor_captures_hits_and_spans()
    {
        var descriptor = new SearchCaseDescriptorView(
            "sample_search",
            "stage",
            false,
            null,
            "insensitive",
            256,
            new[]
            {
                new SearchHitView(
                    0,
                    new RangeSnapshot { Start = 10, End = 15 },
                    2,
                    "st",
                    "ge")
            },
            new[]
            {
                new SearchSpanSegmentView(
                    new RangeSnapshot { Start = 8, End = 20 },
                    7,
                    "match",
                    1)
            },
            "stage-d search smoke");

        var result = SearchResult.FromDescriptor(descriptor);

        Assert.Equal("sample_search", result.Sample);
        Assert.Equal("stage", result.Query);
        Assert.False(result.IsRegex);
        Assert.Single(result.Hits);
        Assert.Single(result.SpanWindows);
        Assert.Equal(7, result.SpanWindows[0].StyleId);
    }
}
