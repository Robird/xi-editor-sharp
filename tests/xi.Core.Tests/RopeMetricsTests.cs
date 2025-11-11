using System.Linq;
using Xi.Core.Rope;

namespace Xi.Core.Tests;

public class RopeMetricsTests
{
    [Fact]
    public void RopeInfoFromLeafCountsLinesAndUtf16()
    {
        var text = "line1\nline2\U0001F600"; // includes surrogate pair
        var info = RopeInfo.FromLeaf(text.AsSpan());

    Assert.Equal(1, info.LineCount); // single newline separator
        Assert.Equal(text.EnumerateRunes().Sum(r => r.Utf16SequenceLength), info.Utf16Length);
    }

    [Theory]
    [InlineData("abc", 0, true)]
    [InlineData("abc", 1, true)]
    [InlineData("abc", 3, true)]
    public void BaseMetricRecognisesSimpleBoundaries(string text, int offset, bool expected)
    {
        var span = text.AsSpan();
        Assert.Equal(expected, BaseMetric.Instance.IsBoundary(span, offset));
    }

    [Fact]
    public void BaseMetricSkipsInsideSurrogate()
    {
        var text = "\U0001F600"; // 😀 -> surrogate pair
        var span = text.AsSpan();
        Assert.False(BaseMetric.Instance.IsBoundary(span, 1));
    }

    [Fact]
    public void LinesMetricConvertsBetweenUnits()
    {
        var span = "a\nb\nc".AsSpan();
        var metric = LinesMetric.Instance;

    Assert.Equal(4, metric.ToBaseUnits(span, 2));
        Assert.Equal(2, metric.FromBaseUnits(span, 4));
    }
}
