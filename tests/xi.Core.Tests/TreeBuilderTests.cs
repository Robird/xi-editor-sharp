using System;
using System.Linq;
using Xi.Core.Rope;

namespace Xi.Core.Tests;

public class TreeBuilderTests
{
    [Fact]
    public void BuildFromStringsProducesConcatenatedText()
    {
        var builder = new TreeBuilder();
        builder.PushString("hello");
        builder.PushString(", ");
        builder.PushString("world");

        var rope = builder.Build();

        Assert.Equal("hello, world", rope.ToString());
        Assert.Equal(3, rope.TraverseLeaves().Count());
    }

    [Fact]
    public void BuildSplitsLongInputIntoMultipleLeaves()
    {
        var builder = new TreeBuilder();
        var longText = new string('a', RopeNode.MaxLeafSize * 2 + 100);
        builder.PushString(longText);

        var rope = builder.Build();

        Assert.Equal(longText.Length, rope.Length);
        Assert.True(rope.TraverseLeaves().Count() >= 3);
    }

    [Fact]
    public void ResetClearsPendingState()
    {
        var builder = new TreeBuilder();
        builder.PushString("test");
        Assert.NotEqual(0, builder.Build().Length);

        builder.Reset();
        var rope = builder.Build();

        Assert.True(rope.IsEmpty);
    }

    [Fact]
    public void SplitPrefersNewlineNearBoundary()
    {
        var builder = new TreeBuilder();
        var segment = new string('a', RopeNode.MaxLeafSize - 10) + "\n" + new string('b', RopeNode.MaxLeafSize);

        builder.PushString(segment);

        var leaves = builder.Build().TraverseLeaves().Select(n => n.ToString()).ToArray();

        Assert.True(leaves.Length >= 2);
        Assert.EndsWith("\n", leaves[0]);
        Assert.True(leaves[0].Length <= RopeNode.MaxLeafSize);
    }

    [Fact]
    public void SplitAvoidsBreakingSurrogatePairs()
    {
        var builder = new TreeBuilder();
        var emoji = char.ConvertFromUtf32(0x1F600);
        var text = new string('x', RopeNode.MaxLeafSize - 1) + emoji + new string('y', RopeNode.MaxLeafSize);

        builder.PushString(text);

        var leaves = builder.Build().TraverseLeaves().Select(n => n.ToString()).ToArray();

        Assert.All(leaves, leaf => Assert.NotEmpty(leaf));

        for (var i = 0; i < leaves.Length - 1; i++)
        {
            var left = leaves[i];
            var right = leaves[i + 1];
            var leftLast = left[^1];
            var rightFirst = right[0];

            Assert.False(char.IsHighSurrogate(leftLast) && char.IsLowSurrogate(rightFirst));
        }
    }
}
