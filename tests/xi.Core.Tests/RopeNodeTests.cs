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

    [Fact]
    public void CloneWithChildren_AllowsBulkReplacement()
    {
        var builder = new TreeBuilder();
        builder.PushString("left");
        builder.PushString("middle");
        builder.PushString("right");

        var node = builder.Build();
        var children = node.Children.ToArray();
        var replacement = RopeNode.FromLeaf("MID");
        children[1] = replacement;

        var cloned = node.CloneWithChildren(children);

        Assert.Equal("leftMIDright", cloned.ToString());
        Assert.Equal(
            node.Info.LineCount - node.Children[1].Info.LineCount + replacement.Info.LineCount,
            cloned.Info.LineCount);
        Assert.Equal(node.Length - node.Children[1].Length + replacement.Length, cloned.Length);
        Assert.Equal("leftmiddleright", node.ToString());
    }

    [Fact]
    public void CloneWithChildren_ThrowsOnLeafNode()
    {
        var leaf = RopeNode.FromLeaf("abc");

        Assert.Throws<InvalidOperationException>(() => leaf.CloneWithChildren(new[] { leaf }));
    }

    [Fact]
    public void EnsureWritableLeaf_ReturnsNewLeafInstance()
    {
        var leaf = RopeNode.FromLeaf("hello");
        var cloned = leaf.EnsureWritableLeaf();

        Assert.Equal("hello", cloned.ToString());
        Assert.NotSame(leaf, cloned);
        Assert.Equal(leaf.Info.LineCount, cloned.Info.LineCount);
    }

    [Fact]
    public void SplitLeafByBounds_RespectsMaxSizeAndNewlinePreference()
    {
        var longText = new string('x', RopeNode.MaxLeafSize - 8) + "\n" + new string('y', RopeNode.MaxLeafSize + 20);
        var leaf = RopeNode.FromLeaf(longText);

        var segments = leaf.SplitLeafByBounds();

    Assert.True(segments.Count >= 2);
    Assert.EndsWith("\n", segments[0].ToString());
        Assert.All(segments, segment => Assert.True(segment.Length <= RopeNode.MaxLeafSize));
    }

    [Fact]
    public void SplitLeafByBounds_AvoidsBreakingSurrogatePairs()
    {
        var emoji = char.ConvertFromUtf32(0x1F603);
        var text = new string('a', RopeNode.MaxLeafSize - 1) + emoji + new string('b', RopeNode.MaxLeafSize);
        var leaf = RopeNode.FromLeaf(text);

        var segments = leaf.SplitLeafByBounds();

        for (var i = 0; i < segments.Count - 1; i++)
        {
            var left = segments[i].ToString();
            var right = segments[i + 1].ToString();
            Assert.False(char.IsHighSurrogate(left[^1]) && char.IsLowSurrogate(right[0]));
        }
    }

    [Fact]
    public void Insert_WithinLeafUsesLeafOptimization()
    {
        var initial = new string('x', RopeNode.MaxLeafSize - 10);
        var node = RopeNode.FromLeaf(initial);

        var inserted = node.Insert(5, "hello");

        Assert.True(inserted.IsLeaf);
        Assert.Equal(initial.Insert(5, "hello"), inserted.ToString());
        Assert.Equal(node.Info.LineCount, inserted.Info.LineCount);
    }

    [Fact]
    public void Insert_LeafOverflowFallsBackToTreeBuilder()
    {
        var initial = new string('x', RopeNode.MaxLeafSize - 1);
        var node = RopeNode.FromLeaf(initial);

        var inserted = node.Insert(initial.Length, new string('y', 16));

        Assert.False(inserted.IsLeaf);
        Assert.Equal(initial + new string('y', 16), inserted.ToString());
        Assert.True(inserted.TraverseLeaves().All(leaf => leaf.Length <= RopeNode.MaxLeafSize));
    }

    [Fact]
    public void Delete_WithinLeafUsesLeafOptimization()
    {
        var node = RopeNode.FromLeaf("abcdefghij");

        var deleted = node.Delete(3, 4);

        Assert.True(deleted.IsLeaf);
    Assert.Equal("abchij", deleted.ToString());
        Assert.Equal(node.Info.LineCount, deleted.Info.LineCount);
    }

    [Fact]
    public void Delete_RemovesEntireLeafFromInternalNode()
    {
        var builder = new TreeBuilder();
        builder.PushString("hello");
        builder.PushString("world");

        var node = builder.Build();
        var deleted = node.Delete(5, 5);

        Assert.Equal("hello", deleted.ToString());
        Assert.True(deleted.IsLeaf);
        Assert.True(deleted.TraverseLeaves().Count() == 1);
    }

    [Fact]
    public void Replace_WithinLeafUsesSingleSegment()
    {
        var node = RopeNode.FromLeaf("abcdef");

        var replaced = node.Replace(2, 2, "XYZ");

        Assert.True(replaced.IsLeaf);
        Assert.Equal("abXYZef", replaced.ToString());
        Assert.Equal(node.Info.LineCount, replaced.Info.LineCount);
    }

    [Fact]
    public void Replace_LeafOverflowFallsBackToGeneralPath()
    {
        var initial = RopeNode.FromLeaf(new string('a', RopeNode.MaxLeafSize));
        var replacement = new string('b', 32);

        var replaced = initial.Replace(RopeNode.MaxLeafSize - 4, 2, replacement);

        Assert.False(replaced.IsLeaf);
        var expected = new string('a', RopeNode.MaxLeafSize - 4) + replacement + new string('a', 2);
        Assert.Equal(expected, replaced.ToString());
        Assert.All(replaced.TraverseLeaves(), leaf => Assert.True(leaf.Length <= RopeNode.MaxLeafSize));
    }

    [Fact]
    public void Insert_LeafOverflowSplitsIntoMultipleSegments()
    {
        var baseText = new string('a', RopeNode.MaxLeafSize - 2);
        var insertText = "hello world";
        var insertIndex = RopeNode.MaxLeafSize / 3;

        var node = RopeNode.FromLeaf(baseText);
        var inserted = node.Insert(insertIndex, insertText);

        var leaves = inserted.TraverseLeaves().ToArray();

        Assert.Equal(baseText[..insertIndex] + insertText + baseText[insertIndex..], inserted.ToString());
        Assert.True(leaves.Length >= 2);
        Assert.All(leaves, leaf => Assert.True(leaf.Length <= RopeNode.MaxLeafSize));
    }

    [Fact]
    public void Insert_SplitWithinInternalNodeKeepsSiblingContent()
    {
        var builder = new TreeBuilder();
        builder.PushString(new string('a', RopeNode.MaxLeafSize - 10));
        builder.PushString(new string('b', RopeNode.MaxLeafSize));

        var node = builder.Build();

        var insertText = new string('x', 32);
        var modified = node.Insert(20, insertText);

        var leaves = modified.TraverseLeaves().ToArray();

        Assert.True(leaves.Length >= 3);
        Assert.All(leaves, leaf => Assert.True(leaf.Length <= RopeNode.MaxLeafSize));
        Assert.EndsWith(new string('b', RopeNode.MaxLeafSize), modified.ToString());
    }

    [Fact]
    public void Delete_ShrinkingLeafMergesWithSibling()
    {
        var initialFirst = RopeNode.MinLeafSize + 200;
        var builder = new TreeBuilder();
        builder.PushString(new string('a', initialFirst));
        builder.PushString(new string('b', RopeNode.MinLeafSize));

        var node = builder.Build();
        var deleted = node.Delete(0, initialFirst - 100);

        var leaves = deleted.TraverseLeaves().ToArray();

        Assert.Single(leaves);
        Assert.True(deleted.IsLeaf);
        Assert.Equal(100 + RopeNode.MinLeafSize, deleted.Length);
        Assert.Equal(new string('a', 100) + new string('b', RopeNode.MinLeafSize), deleted.ToString());
    }

    [Fact]
    public void Delete_ShrinkingLeafDoesNotMergeWhenExceedingMax()
    {
        var builder = new TreeBuilder();
        builder.PushString(new string('a', RopeNode.MaxLeafSize));
        builder.PushString(new string('b', RopeNode.MaxLeafSize));

        var node = builder.Build();
        var deleted = node.Delete(0, RopeNode.MaxLeafSize - 74);

        var leaves = deleted.TraverseLeaves().ToArray();

        Assert.Equal(2, leaves.Length);
        Assert.Equal(74, leaves[0].Length);
        Assert.Equal(RopeNode.MaxLeafSize, leaves[1].Length);
        Assert.Equal(new string('a', 74) + new string('b', RopeNode.MaxLeafSize), deleted.ToString());
    }
}
