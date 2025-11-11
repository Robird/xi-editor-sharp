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
}
