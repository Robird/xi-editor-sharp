using System.Linq;
using Xi.Core.Rope;

namespace Xi.Core.Tests;

public class RopeNodeTests
{
    [Fact]
    public void FromLeaf_ComputesMetadata()
    {
        var node = RopeNode.FromLeaf("hello\nworld");

        Assert.True(node.IsLeaf);
        Assert.Equal(11, node.Length);
        Assert.Equal(1, node.Info.LineCount);
        Assert.Equal("hello\nworld", node.ToString());
    }

    [Fact]
    public void Concat_AggregatesInfoAndText()
    {
        var left = RopeNode.FromLeaf("hello");
        var right = RopeNode.FromLeaf("世界");

        var combined = RopeNode.Concat(left, right);

        Assert.False(combined.IsLeaf);
        Assert.Equal(left.Length + right.Length, combined.Length);
        Assert.Equal(left.Info.Utf16Length + right.Info.Utf16Length, combined.Info.Utf16Length);
        Assert.Equal("hello世界", combined.ToString());
    }

    [Fact]
    public void Concat_MultipleLeavesPreservesOrder()
    {
        var result = RopeNode.Concat(RopeNode.Concat(RopeNode.FromLeaf("a"), RopeNode.FromLeaf("b")), RopeNode.FromLeaf("c"));

        Assert.Equal("abc", result.ToString());
        Assert.Equal(3, result.TraverseLeaves().Count());
    }

    [Fact]
    public void Slice_SpansMultipleLeaves()
    {
        var builder = new TreeBuilder();
        builder.PushString("hello");
        builder.PushString(" world");
        builder.PushString("!");

        var node = builder.Build();
        var slice = node.Slice(3, 5);

        Assert.Equal("lo wo", slice.ToString());
    }

    [Fact]
    public void SplitAt_SplitsAcrossInternalNodes()
    {
        var builder = new TreeBuilder();
        builder.PushString("hello");
        builder.PushString(" ");
        builder.PushString("world");

        var node = builder.Build();
        var (left, right) = node.SplitAt(6);

        Assert.Equal("hello ", left.ToString());
        Assert.Equal("world", right.ToString());
        Assert.Equal(node.Length, left.Length + right.Length);
    }

    [Fact]
    public void Insert_PreservesStructureAndContent()
    {
        var builder = new TreeBuilder();
        builder.PushString("hello");
        builder.PushString("world");

        var node = builder.Build();
        var inserted = node.Insert(5, ", ");

        Assert.Equal("hello, world", inserted.ToString());
    }

    [Fact]
    public void Delete_RemovesSpecifiedSegment()
    {
        var builder = new TreeBuilder();
        builder.PushString("hello, world");

        var node = builder.Build();
        var deleted = node.Delete(5, 2);

        Assert.Equal("helloworld", deleted.ToString());
    }

    [Fact]
    public void WithChildReplaced_RecomputesAggregates()
    {
        var builder = new TreeBuilder();
        builder.PushString("aaa\n");
        builder.PushString("bbb");
        builder.PushString("ccc");

        var node = builder.Build();
        Assert.False(node.IsLeaf);

        var originalChild = node.Children[1];
        var replacement = RopeNode.FromLeaf("xyz\n");

        var updated = node.WithChildReplaced(1, replacement);

        Assert.Equal("aaa\nxyz\nccc", updated.ToString());
        Assert.Equal(
            node.Length - originalChild.Length + replacement.Length,
            updated.Length);
        Assert.Equal(
            node.Info.LineCount - originalChild.Info.LineCount + replacement.Info.LineCount,
            updated.Info.LineCount);
        Assert.Equal("aaa\nbbbccc", node.ToString());
    }
}
