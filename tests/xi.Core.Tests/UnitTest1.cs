using System;
using Xi.Core;

namespace Xi.Core.Tests;

public class TextBufferTests
{
    [Fact]
    public void Append_AppendsStringsInOrder()
    {
        ITextBuffer buffer = new TextBuffer();

        buffer.Append("hello");
        buffer.Append(", ");
        buffer.Append("world");

        Assert.Equal("hello, world", buffer.Snapshot());
    }

    [Fact]
    public void Append_SpanOverloadMatchesStringAppend()
    {
        ITextBuffer buffer = new TextBuffer();
        buffer.Append("xi");

        buffer.Append(" editor".AsSpan(1)); // "editor"

        Assert.Equal("xieditor", buffer.Snapshot());
    }

    [Fact]
    public void LengthReflectsAppends()
    {
        ITextBuffer buffer = new TextBuffer();

        buffer.Append("abc");
        buffer.Append("de".AsSpan());

        Assert.Equal(5, buffer.Length);
    }

    [Fact]
    public void GetSlice_ReturnsExpectedSegment()
    {
        ITextBuffer buffer = new TextBuffer();
        buffer.Append("abcdef");

        Assert.Equal("cd", buffer.GetSlice(2, 2));
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    [InlineData(3, 10)]
    public void GetSlice_ThrowsOnOutOfRange(int start, int length)
    {
        ITextBuffer buffer = new TextBuffer();
        buffer.Append("abc");

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetSlice(start, length));
    }
}
