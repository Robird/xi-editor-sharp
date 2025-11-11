using System;
using Xi.Core;
using Xi.Core.Rope;

namespace Xi.Core.Tests;

public class RopeTextBufferTests
{
    private static ITextBuffer Create() => new RopeTextBuffer();

    [Fact]
    public void Append_AppendsStringsInOrder()
    {
        var buffer = Create();

        buffer.Append("hello");
        buffer.Append(", ");
        buffer.Append("world");

        Assert.Equal("hello, world", buffer.Snapshot());
        Assert.Equal(12, buffer.Length);
    }

    [Fact]
    public void Append_SpanOverloadMatchesStringAppend()
    {
        var buffer = Create();
        buffer.Append("xi");

        buffer.Append(" editor".AsSpan(1));

        Assert.Equal("xieditor", buffer.Snapshot());
    }

    [Fact]
    public void Clear_ResetsState()
    {
        var buffer = Create();
        buffer.Append("content");

        buffer.Clear();

        Assert.Equal(0, buffer.Length);
        Assert.Equal(string.Empty, buffer.Snapshot());
    }

    [Fact]
    public void GetSlice_ReturnsExpectedSegment()
    {
        var buffer = Create();
        buffer.Append("abcdef");

        Assert.Equal("cd", buffer.GetSlice(2, 2));
    }

    [Fact]
    public void GetSlice_HandlesSegmentsAcrossLeaves()
    {
        var buffer = Create();
        buffer.Append(new string('a', RopeNode.MaxLeafSize));
        buffer.Append("middle");
        buffer.Append(new string('b', RopeNode.MaxLeafSize));

        var slice = buffer.GetSlice(RopeNode.MaxLeafSize - 3, 10);

    Assert.Equal(new string('a', 3) + "middle" + "b", slice);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    [InlineData(3, 10)]
    public void GetSlice_ThrowsOnInvalidRange(int start, int length)
    {
        var buffer = Create();
        buffer.Append("abc");

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetSlice(start, length));
    }
}
