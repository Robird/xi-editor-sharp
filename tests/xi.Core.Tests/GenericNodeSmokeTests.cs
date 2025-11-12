using System.Linq;
using Xi.Core.Rope;
using Xi.Core.Rope.Tree;

namespace Xi.Core.Tests;

public class GenericNodeSmokeTests
{
    private static Node<RopeInfo, string, StringLeafOperations> Leaf(string text)
        => Node<RopeInfo, string, StringLeafOperations>.FromLeaf(text);

    [Fact]
    public void Empty_Node_Is_Leaf_With_Identity_Info()
    {
        var empty = Node<RopeInfo, string, StringLeafOperations>.Empty;

        Assert.True(empty.IsLeaf);
        Assert.True(empty.IsEmpty);
        Assert.Equal(0, empty.Length);
        Assert.Equal(RopeInfo.Identity.LineCount, empty.Info.LineCount);
        Assert.Equal(RopeInfo.Identity.Utf16Length, empty.Info.Utf16Length);
    }

    [Fact]
    public void FromLeaf_Computes_Length_And_Info()
    {
        const string text = "Hello\n🌍";

        var node = Leaf(text);

        Assert.True(node.IsLeaf);
        Assert.Equal(text.Length, node.Length);
        Assert.Equal(RopeInfo.FromLeaf(text).Utf16Length, node.Info.Utf16Length);
        Assert.Equal(text, node.Leaf);
    }

    [Fact]
    public void CreateInternal_Aggregates_Length_And_Info()
    {
        var left = Leaf(new string('a', 10));
        var right = Leaf(new string('b', 5));

        var parent = Node<RopeInfo, string, StringLeafOperations>.CreateInternal(new[] { left, right });

        Assert.False(parent.IsLeaf);
        Assert.Equal(1, parent.Height);
        Assert.Equal(left.Length + right.Length, parent.Length);
        Assert.Equal(left.Info.Accumulate(right.Info).Utf16Length, parent.Info.Utf16Length);

        var leaves = parent.TraverseLeaves().ToList();
        Assert.Equal(new[] { left, right }, leaves);
    }
}
