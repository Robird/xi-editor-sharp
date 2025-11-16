using System;
using System.Linq;
using System.Reflection;
using Xi.Core.Rope.Tree;
using static Xi.Core.Tests.RopeTestHelpers;

namespace Xi.Core.Tests;

public class NodeTests
{
    [Fact]
    public void FromLeaf_ComputesMetadata()
    {
        var node = Node.FromLeaf("hello\nworld");

        Assert.True(node.IsLeaf);
        Assert.Equal(11, node.Length);
        Assert.Equal(1, node.Info.LineCount);
        Assert.Equal("hello\nworld", node.ToString());
        AssertInvariants(node);
    }

    [Fact]
    public void Concat_AggregatesInfoAndText()
    {
        var left = Node.FromLeaf("hello");
        var right = Node.FromLeaf("世界");

        var combined = Node.Concat(left, right);

        Assert.False(combined.IsLeaf);
        Assert.Equal(left.Length + right.Length, combined.Length);
        Assert.Equal(left.Info.Utf16Length + right.Info.Utf16Length, combined.Info.Utf16Length);
        Assert.Equal("hello世界", combined.ToString());
        AssertInvariants(combined);
    }

    [Fact]
    public void Concat_MultipleLeavesPreservesOrder()
    {
        var result = Node.Concat(Node.Concat(Node.FromLeaf("a"), Node.FromLeaf("b")), Node.FromLeaf("c"));

        Assert.Equal("abc", result.ToString());
        Assert.Equal(3, result.TraverseLeaves().Count());
        AssertInvariants(result);
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
        AssertInvariants(slice);
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
        AssertInvariants(left);
        AssertInvariants(right);
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
        AssertInvariants(inserted);
    }

    [Fact]
    public void Delete_RemovesSpecifiedSegment()
    {
        var builder = new TreeBuilder();
        builder.PushString("hello, world");

        var node = builder.Build();
        var deleted = node.Delete(5, 2);

        Assert.Equal("helloworld", deleted.ToString());
        AssertInvariants(deleted);
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
        var replacement = Node.FromLeaf("xyz\n");

        var updated = node.WithChildReplaced(1, replacement);

        Assert.Equal("aaa\nxyz\nccc", updated.ToString());
        Assert.Equal(
            node.Length - originalChild.Length + replacement.Length,
            updated.Length);
        Assert.Equal(
            node.Info.LineCount - originalChild.Info.LineCount + replacement.Info.LineCount,
            updated.Info.LineCount);
        Assert.Equal("aaa\nbbbccc", node.ToString());
        AssertInvariants(updated);
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
        var replacement = Node.FromLeaf("MID");
        children[1] = replacement;

        var cloned = node.CloneWithChildren(children);

        Assert.Equal("leftMIDright", cloned.ToString());
        Assert.Equal(
            node.Info.LineCount - node.Children[1].Info.LineCount + replacement.Info.LineCount,
            cloned.Info.LineCount);
        Assert.Equal(node.Length - node.Children[1].Length + replacement.Length, cloned.Length);
        Assert.Equal("leftmiddleright", node.ToString());
        AssertInvariants(cloned);
    }

    [Fact]
    public void CloneWithChildren_ThrowsOnLeafNode()
    {
        var leaf = Node.FromLeaf("abc");

        Assert.Throws<InvalidOperationException>(() => leaf.CloneWithChildren(new[] { leaf }));
    }

    [Fact]
    public void EnsureWritableLeaf_ReturnsNewLeafInstance()
    {
        var leaf = Node.FromLeaf("hello");
        var cloned = leaf.EnsureWritableLeaf();

        Assert.Equal("hello", cloned.ToString());
        Assert.NotSame(leaf, cloned);
        Assert.Equal(leaf.Info.LineCount, cloned.Info.LineCount);
        AssertInvariants(cloned);
    }

    [Fact]
    public void SplitLeafByBounds_RespectsMaxSizeAndNewlinePreference()
    {
        var longText = new string('x', Node.MaxLeafSize - 8) + "\n" + new string('y', Node.MaxLeafSize + 20);
        var leaf = Node.FromLeaf(longText);

        var segments = leaf.SplitLeafByBounds();

    Assert.True(segments.Count >= 2);
    Assert.EndsWith("\n", segments[0].ToString());
        Assert.All(segments, segment => Assert.True(segment.Length <= Node.MaxLeafSize));
    Assert.All(segments, segment => AssertInvariants(segment));
    }

    [Fact]
    public void SplitLeafByBounds_AvoidsBreakingSurrogatePairs()
    {
        var emoji = char.ConvertFromUtf32(0x1F603);
        var text = new string('a', Node.MaxLeafSize - 1) + emoji + new string('b', Node.MaxLeafSize);
        var leaf = Node.FromLeaf(text);

        var segments = leaf.SplitLeafByBounds();

        for (var i = 0; i < segments.Count - 1; i++)
        {
            var left = segments[i].ToString();
            var right = segments[i + 1].ToString();
            Assert.False(char.IsHighSurrogate(left[^1]) && char.IsLowSurrogate(right[0]));
        }
    Assert.All(segments, segment => AssertInvariants(segment));
    }

    [Fact]
    public void Insert_WithinLeafUsesLeafOptimization()
    {
        var initial = new string('x', Node.MaxLeafSize - 10);
        var node = Node.FromLeaf(initial);

        var inserted = node.Insert(5, "hello");

        Assert.True(inserted.IsLeaf);
        Assert.Equal(initial.Insert(5, "hello"), inserted.ToString());
        Assert.Equal(node.Info.LineCount, inserted.Info.LineCount);
        AssertInvariants(inserted);
    }

    [Fact]
    public void Insert_LeafOverflowFallsBackToTreeBuilder()
    {
        var initial = new string('x', Node.MaxLeafSize - 1);
        var node = Node.FromLeaf(initial);

        var inserted = node.Insert(initial.Length, new string('y', 16));

        Assert.False(inserted.IsLeaf);
        Assert.Equal(initial + new string('y', 16), inserted.ToString());
        Assert.True(inserted.TraverseLeaves().All(leaf => leaf.Length <= Node.MaxLeafSize));
        AssertInvariants(inserted);
    }

    [Fact]
    public void Delete_WithinLeafUsesLeafOptimization()
    {
        var node = Node.FromLeaf("abcdefghij");

        var deleted = node.Delete(3, 4);

        Assert.True(deleted.IsLeaf);
    Assert.Equal("abchij", deleted.ToString());
        Assert.Equal(node.Info.LineCount, deleted.Info.LineCount);
        AssertInvariants(deleted);
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
        AssertInvariants(deleted);
    }

    [Fact]
    public void Replace_WithinLeafUsesSingleSegment()
    {
        var node = Node.FromLeaf("abcdef");

        var replaced = node.Replace(2, 2, "XYZ");

        Assert.True(replaced.IsLeaf);
        Assert.Equal("abXYZef", replaced.ToString());
        Assert.Equal(node.Info.LineCount, replaced.Info.LineCount);
        AssertInvariants(replaced);
    }

    [Fact]
    public void Replace_LeafOverflowFallsBackToGeneralPath()
    {
        var initial = Node.FromLeaf(new string('a', Node.MaxLeafSize));
        var replacement = new string('b', 32);

        var replaced = initial.Replace(Node.MaxLeafSize - 4, 2, replacement);

        Assert.False(replaced.IsLeaf);
        var expected = new string('a', Node.MaxLeafSize - 4) + replacement + new string('a', 2);
        Assert.Equal(expected, replaced.ToString());
        Assert.All(replaced.TraverseLeaves(), leaf => Assert.True(leaf.Length <= Node.MaxLeafSize));
        AssertInvariants(replaced);
    }

    [Fact]
    public void Insert_LeafOverflowSplitsIntoMultipleSegments()
    {
        var baseText = new string('a', Node.MaxLeafSize - 2);
        var insertText = "hello world";
        var insertIndex = Node.MaxLeafSize / 3;

        var node = Node.FromLeaf(baseText);
        var inserted = node.Insert(insertIndex, insertText);

        var leaves = inserted.TraverseLeaves().ToArray();

        Assert.Equal(baseText[..insertIndex] + insertText + baseText[insertIndex..], inserted.ToString());
        Assert.True(leaves.Length >= 2);
        Assert.All(leaves, leaf => Assert.True(leaf.Length <= Node.MaxLeafSize));
        AssertInvariants(inserted);
    }

    [Fact]
    public void Insert_SplitWithinInternalNodeKeepsSiblingContent()
    {
        var builder = new TreeBuilder();
        builder.PushString(new string('a', Node.MaxLeafSize - 10));
        builder.PushString(new string('b', Node.MaxLeafSize));

        var node = builder.Build();

        var insertText = new string('x', 32);
        var modified = node.Insert(20, insertText);

        var leaves = modified.TraverseLeaves().ToArray();

        Assert.True(leaves.Length >= 3);
        Assert.All(leaves, leaf => Assert.True(leaf.Length <= Node.MaxLeafSize));
        Assert.EndsWith(new string('b', Node.MaxLeafSize), modified.ToString());
        AssertInvariants(modified);
    }

    [Fact]
    public void Delete_ShrinkingLeafMergesWithSibling()
    {
        var initialFirst = Node.MinLeafSize + 200;
        var builder = new TreeBuilder();
        builder.PushString(new string('a', initialFirst));
        builder.PushString(new string('b', Node.MinLeafSize));

        var node = builder.Build();
        var deleted = node.Delete(0, initialFirst - 100);

        var leaves = deleted.TraverseLeaves().ToArray();

        Assert.Single(leaves);
        Assert.True(deleted.IsLeaf);
        Assert.Equal(100 + Node.MinLeafSize, deleted.Length);
        Assert.Equal(new string('a', 100) + new string('b', Node.MinLeafSize), deleted.ToString());
        AssertInvariants(deleted);
    }

    [Fact]
    public void Delete_ShrinkingLeafDoesNotMergeWhenExceedingMax()
    {
        var builder = new TreeBuilder();
        builder.PushString(new string('a', Node.MaxLeafSize));
        builder.PushString(new string('b', Node.MaxLeafSize));

        var node = builder.Build();
        var deleted = node.Delete(0, Node.MaxLeafSize - 74);

        var leaves = deleted.TraverseLeaves().ToArray();

        Assert.Equal(2, leaves.Length);
        Assert.All(leaves, leaf => Assert.InRange(leaf.Length, Node.MinLeafSize, Node.MaxLeafSize));
        Assert.Equal(new string('a', 74) + new string('b', Node.MaxLeafSize), deleted.ToString());
    }

    [Fact]
    public void Replace_ShrinkingLeafMergesWithSibling()
    {
        var firstLength = Node.MinLeafSize + 200;
        var builder = new TreeBuilder();
        builder.PushString(new string('a', firstLength));
        builder.PushString(new string('b', Node.MinLeafSize));

        var node = builder.Build();
        var replaced = node.Replace(0, firstLength - 71, new string('x', 10));

        var leaves = replaced.TraverseLeaves().ToArray();

        Assert.Single(leaves);
        Assert.True(replaced.IsLeaf);
        var expected = new string('x', 10) + new string('a', 71) + new string('b', Node.MinLeafSize);
        Assert.Equal(expected, replaced.ToString());
        AssertInvariants(replaced);
    }

    [Fact]
    public void Replace_ShrinkingLeafDoesNotMergeWhenExceedingMax()
    {
        var builder = new TreeBuilder();
        builder.PushString(new string('a', Node.MaxLeafSize));
        builder.PushString(new string('b', Node.MaxLeafSize));

        var node = builder.Build();
        var replaced = node.Replace(0, Node.MaxLeafSize - 400, new string('x', 10));

        var leaves = replaced.TraverseLeaves().ToArray();

        Assert.Equal(2, leaves.Length);
        Assert.All(leaves, leaf => Assert.InRange(leaf.Length, Node.MinLeafSize, Node.MaxLeafSize));
        var expected = new string('x', 10) + new string('a', 400) + new string('b', Node.MaxLeafSize);
        Assert.Equal(expected, replaced.ToString());
    }

    [Fact]
    public void Delete_ShrinkingLeafBorrowsFromLeftSibling()
    {
        var builder = new TreeBuilder();
        builder.PushString(new string('a', Node.MaxLeafSize));
        builder.PushString(new string('b', Node.MaxLeafSize));

        var node = builder.Build();
        var deleted = node.Delete(Node.MaxLeafSize, Node.MaxLeafSize - 50);

        var leaves = deleted.TraverseLeaves().ToArray();

        Assert.Equal(2, leaves.Length);
        Assert.All(leaves, leaf => Assert.InRange(leaf.Length, Node.MinLeafSize, Node.MaxLeafSize));
        Assert.Equal(Node.MaxLeafSize + 50, leaves.Sum(l => l.Length));
        Assert.Equal(new string('a', Node.MaxLeafSize) + new string('b', 50), deleted.ToString());
        AssertInvariants(deleted, enforceLeafMinimum: true);
    }

    [Fact]
    public void Delete_ShrinkingLeafBorrowsFromRightSibling()
    {
        var builder = new TreeBuilder();
        builder.PushString(new string('a', Node.MaxLeafSize));
        builder.PushString(new string('b', Node.MaxLeafSize));

        var node = builder.Build();
        var deleted = node.Delete(50, Node.MaxLeafSize - 50);

        var leaves = deleted.TraverseLeaves().ToArray();

        Assert.Equal(2, leaves.Length);
        Assert.All(leaves, leaf => Assert.InRange(leaf.Length, Node.MinLeafSize, Node.MaxLeafSize));
        Assert.Equal(Node.MaxLeafSize + 50, leaves.Sum(l => l.Length));
        Assert.Equal(new string('a', 50) + new string('b', Node.MaxLeafSize), deleted.ToString());
        AssertInvariants(deleted, enforceLeafMinimum: true);
    }

    [Fact]
    public void Replace_ShrinkingLeafBorrowsFromSibling()
    {
        var builder = new TreeBuilder();
        builder.PushString(new string('a', Node.MaxLeafSize));
        builder.PushString(new string('b', Node.MaxLeafSize));

        var node = builder.Build();
        var replaced = node.Replace(Node.MaxLeafSize, Node.MaxLeafSize - 60, new string('x', 20));

        var leaves = replaced.TraverseLeaves().ToArray();

        Assert.Equal(2, leaves.Length);
        Assert.All(leaves, leaf => Assert.InRange(leaf.Length, Node.MinLeafSize, Node.MaxLeafSize));
        Assert.Equal(Node.MaxLeafSize + 80, leaves.Sum(l => l.Length));
        var expected = new string('a', Node.MaxLeafSize) + new string('x', 20) + new string('b', 60);
        Assert.Equal(expected, replaced.ToString());
        AssertInvariants(replaced, enforceLeafMinimum: true);
    }

    [Fact]
    public void Rebalance_PreservesSurrogatePairs()
    {
        var emoji = char.ConvertFromUtf32(0x1F603);
        var leftText = new string('a', Node.MaxLeafSize - 3) + emoji;
        var rightText = emoji + new string('b', Node.MaxLeafSize - 3);

        var node = Node.Concat(Node.FromLeaf(leftText), Node.FromLeaf(rightText));

        var removalLength = rightText.Length - 80;
        var deleted = node.Delete(leftText.Length, removalLength);

        var leaves = deleted.TraverseLeaves().Select(l => l.ToString()).ToArray();

        Assert.Equal(2, leaves.Length);
        Assert.All(leaves, leaf => Assert.InRange(leaf.Length, Node.MinLeafSize, Node.MaxLeafSize));

        var left = leaves[0];
        var right = leaves[1];
        Assert.False(char.IsHighSurrogate(left[^1]) && char.IsLowSurrogate(right[0]));

        var expectedTail = rightText[^80..];
        var expected = leftText + expectedTail;
        Assert.Equal(expected, deleted.ToString());
    AssertInvariants(deleted, enforceLeafMinimum: true);
    }

    [Fact]
    public void ValidateInvariants_PassesAfterMixedEdits()
    {
        var builder = new TreeBuilder();
        builder.PushString(new string('a', Node.MaxLeafSize));
        builder.PushString(new string('b', Node.MaxLeafSize));
        builder.PushString(new string('c', Node.MaxLeafSize - 50));

        var node = builder.Build();
        AssertInvariants(node);

    node = node.Insert(Node.MaxLeafSize / 2, new string('x', 200));
        AssertInvariants(node);

    node = node.Delete(Node.MaxLeafSize - 100, 300);
        AssertInvariants(node);

        node = node.Replace(node.Length - 120, 60, new string('y', 75));

        AssertInvariants(node);
    }

    [Fact]
    public void Delete_AcrossMultipleLevelsMaintainsLeafConstraints()
    {
        var builder = new TreeBuilder();
        const int leafCount = 12; // exceed Node.MaxChildCount (8) to ensure multi-level tree
        for (var i = 0; i < leafCount; i++)
        {
            builder.PushString(new string((char)('a' + i), Node.MaxLeafSize));
        }

        var node = builder.Build();
        Assert.True(node.Height >= 2, $"Expected multi-level tree but height={node.Height}");

        node = node.Delete(Node.MaxLeafSize + 128, Node.MaxLeafSize + 400);

        AssertInvariants(node, enforceLeafMinimum: true);
    }

    [Fact]
    public void Replace_AcrossMultipleLevelsRespectsSurrogateBoundaries()
    {
        var builder = new TreeBuilder();
        var emoji = char.ConvertFromUtf32(0x1F9D1);

        for (var i = 0; i < 5; i++)
        {
            var prefix = new string((char)('a' + i), Node.MaxLeafSize - 8);
            var suffix = new string((char)('f' + i), 12);
            builder.PushString(prefix + emoji + suffix);
        }

        var node = builder.Build();
        Assert.True(node.Height >= 2);

        var replacement = new string('z', Node.MinLeafSize + 64) + emoji;
        var start = Node.MaxLeafSize - 32;
        var length = Node.MaxLeafSize + 96;

        node = node.Replace(start, length, replacement);

        var leaves = node.TraverseLeaves().Select(l => l.ToString()).ToArray();
        Assert.NotEmpty(leaves);

        for (var i = 0; i < leaves.Length - 1; i++)
        {
            var left = leaves[i];
            var right = leaves[i + 1];
            Assert.False(char.IsHighSurrogate(left[^1]) && char.IsLowSurrogate(right[0]));
        }

        Assert.Contains(replacement, node.ToString());
        AssertInvariants(node, enforceLeafMinimum: true);
    }

    [Fact]
    public void CollectInvariantIssues_ReturnsLeafPreviewAndPath()
    {
        var smallLeft = Node.FromLeaf(new string('a', Node.MinLeafSize - 120));
        var smallRight = Node.FromLeaf(new string('b', Node.MinLeafSize - 80));

        var createInternal = typeof(Node).GetMethod("CreateInternal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(createInternal);

        var node = (Node)createInternal!.Invoke(null, new object[] { 1, new[] { smallLeft, smallRight } })!;
        var issues = node.CollectInvariantIssues(enforceLeafMinimum: true);

        Assert.NotEmpty(issues);
        Assert.All(issues, entry =>
        {
            Assert.Contains("Leaf below MinLeafSize", entry);
            Assert.Contains("[root/", entry);
            Assert.Contains("preview=\"", entry);
        });
    }

    [Fact]
    public void CollectInvariantIssues_ReturnsEmptyWhenNodeIsValid()
    {
        var builder = new TreeBuilder();
        builder.PushString(new string('a', Node.MaxLeafSize));
        builder.PushString(new string('b', Node.MaxLeafSize));

        var node = builder.Build();
        var normalized = node.NormalizeLeafMinimum();
        var issues = normalized.CollectInvariantIssues(enforceLeafMinimum: true);

        Assert.Empty(issues);
    }
}
