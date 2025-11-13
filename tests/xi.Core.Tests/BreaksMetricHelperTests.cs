using System;
using Xi.Core.Rope;
using Xunit;

namespace Xi.Core.Tests;

public class BreaksMetricHelperTests
{
    [Fact]
    public void EmptyBreaks_Semantics()
    {
        const int leafLength = 10;
        var empty = ReadOnlySpan<int>.Empty;

        Assert.Equal(0, BreaksMetricHelper.GetNthBreakOffset(empty, leafLength, 0));
        Assert.Equal(leafLength + 1, BreaksMetricHelper.GetNthBreakOffset(empty, leafLength, 1));
        Assert.Equal(0, BreaksMetricHelper.CountBreaksUpTo(empty, 0));
        Assert.Null(BreaksMetricHelper.FindPreviousBreak(empty, 5));
        Assert.Null(BreaksMetricHelper.FindNextBreak(empty, 5));
        Assert.False(BreaksMetricHelper.IsBreakBoundary(empty, 0));
    }

    [Fact]
    public void BreaksMetricHelper_WithSampleBreaks()
    {
        var breaks = new[] { 3, 7, 10, 12 };
        const int leafLength = 15;

        Assert.Equal(0, BreaksMetricHelper.GetNthBreakOffset(breaks, leafLength, 0));
        Assert.Equal(3, BreaksMetricHelper.GetNthBreakOffset(breaks, leafLength, 1));
        Assert.Equal(12, BreaksMetricHelper.GetNthBreakOffset(breaks, leafLength, 4));
        Assert.Equal(leafLength + 1, BreaksMetricHelper.GetNthBreakOffset(breaks, leafLength, 5));

        Assert.Equal(0, BreaksMetricHelper.CountBreaksUpTo(breaks, 2));
        Assert.Equal(1, BreaksMetricHelper.CountBreaksUpTo(breaks, 3));
        Assert.Equal(2, BreaksMetricHelper.CountBreaksUpTo(breaks, 7));
        Assert.Equal(4, BreaksMetricHelper.CountBreaksUpTo(breaks, 12));
        Assert.Equal(4, BreaksMetricHelper.CountBreaksUpTo(breaks, 20));

        Assert.Null(BreaksMetricHelper.FindPreviousBreak(breaks, 3));
        Assert.Equal(3, BreaksMetricHelper.FindPreviousBreak(breaks, 4));
        Assert.Equal(7, BreaksMetricHelper.FindPreviousBreak(breaks, 8));
        Assert.Equal(12, BreaksMetricHelper.FindPreviousBreak(breaks, 20));

        Assert.Equal(3, BreaksMetricHelper.FindNextBreak(breaks, 0));
        Assert.Equal(7, BreaksMetricHelper.FindNextBreak(breaks, 3));
        Assert.Equal(10, BreaksMetricHelper.FindNextBreak(breaks, 7));
        Assert.Null(BreaksMetricHelper.FindNextBreak(breaks, 12));

        Assert.True(BreaksMetricHelper.IsBreakBoundary(breaks, 7));
        Assert.False(BreaksMetricHelper.IsBreakBoundary(breaks, 11));
    }

    [Fact]
    public void BreaksMetricHelper_HandlesDuplicates()
    {
        var breaks = new[] { 5, 5, 8 };

        Assert.Equal(0, BreaksMetricHelper.CountBreaksUpTo(breaks, 4));
        Assert.Equal(2, BreaksMetricHelper.CountBreaksUpTo(breaks, 5));
        Assert.Equal(3, BreaksMetricHelper.CountBreaksUpTo(breaks, 8));

        Assert.Null(BreaksMetricHelper.FindPreviousBreak(breaks, 5));
        Assert.Equal(5, BreaksMetricHelper.FindPreviousBreak(breaks, 6));

        Assert.Equal(5, BreaksMetricHelper.FindNextBreak(breaks, 4));
        Assert.Equal(8, BreaksMetricHelper.FindNextBreak(breaks, 5));

        Assert.True(BreaksMetricHelper.IsBreakBoundary(breaks, 5));
        Assert.False(BreaksMetricHelper.IsBreakBoundary(breaks, 6));
    }

    [Fact]
    public void CountBreaksUpTo_ThrowsOnNegativeOffset()
    {
        var breaks = Array.Empty<int>();

        Assert.Throws<ArgumentOutOfRangeException>(() => BreaksMetricHelper.CountBreaksUpTo(breaks, -1));
    }

    [Fact]
    public void GetNthBreakOffset_ThrowsOnInvalidArguments()
    {
        var breaks = new[] { 1 };

        Assert.Throws<ArgumentOutOfRangeException>(() => BreaksMetricHelper.GetNthBreakOffset(breaks, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => BreaksMetricHelper.GetNthBreakOffset(breaks, 0, -1));
    }
}
