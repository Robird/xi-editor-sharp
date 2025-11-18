using Xi.Core.Rope.Navigation;
using Xi.Core.Rope.Tree;
using Xunit;

namespace Xi.Core.Tests;

public sealed class GraphemeNavigatorSmokeTests
{
    [Fact]
    [Trait("Category", "StageDTelemetry")]
    public void MoveNextAcrossSurrogatePairAdvancesTwoCodeUnits()
    {
        var metrics = new GraphemeNavigationMetrics();
        var navigator = new DegradedGraphemeNavigator(metrics);
        var node = Node.FromLeaf("\uD83D\uDE00");
        var cursor = new NodeCursor(node, 0);

        var next = navigator.MoveNext(cursor);

        Assert.Equal(2, next);
        Assert.Equal(2, cursor.Position);
        Assert.True(navigator.IsBoundary(cursor));

        var snapshot = metrics.GetSnapshot();
        Assert.Equal(1, snapshot.MoveNextCalls);
        Assert.Equal(0, snapshot.ForwardNeighborRequests);
    }

    [Fact]
    [Trait("Category", "StageDTelemetry")]
    public void MovePreviousAcrossEmojiSequenceRewindsEntireCluster()
    {
        var metrics = new GraphemeNavigationMetrics();
        var navigator = new DegradedGraphemeNavigator(metrics);
        var text = "👩‍👧";
        var node = Node.FromLeaf(text);
        var cursor = new NodeCursor(node, text.Length);

        var previous = navigator.MovePrevious(cursor);

        Assert.Equal(0, previous);
        Assert.Equal(0, cursor.Position);
        var snapshot = metrics.GetSnapshot();
        Assert.Equal(1, snapshot.MovePreviousCalls);
    }

    [Fact]
    [Trait("Category", "StageDTelemetry")]
    public void CrossLeafGraphemeRequestsNeighborTelemetry()
    {
        var metrics = new GraphemeNavigationMetrics();
        var navigator = new DegradedGraphemeNavigator(metrics);
        var left = Node.FromLeaf("\uD83D");
        var right = Node.FromLeaf("\uDE00");
        var root = Node.Concat(left, right);
        var cursor = new NodeCursor(root, 0);

        var next = navigator.MoveNext(cursor);
        Assert.Equal(2, next);

        var snapshot = metrics.GetSnapshot();
        Assert.Equal(1, snapshot.MoveNextCalls);
        Assert.Equal(1, snapshot.ForwardNeighborRequests);

        var previous = navigator.MovePrevious(cursor);
        Assert.Equal(0, previous);

        snapshot = metrics.GetSnapshot();
        Assert.Equal(1, snapshot.BackwardNeighborRequests);
        Assert.Equal(1, snapshot.MovePreviousCalls);
    }
}
