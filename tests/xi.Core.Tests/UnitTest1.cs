using Xi.Core;

namespace Xi.Core.Tests;

public class TextBufferTests
{
    [Fact]
    public void Append_AppendsStringsInOrder()
    {
        var buffer = new TextBuffer();

        buffer.Append("hello");
        buffer.Append(", ");
        buffer.Append("world");

        Assert.Equal("hello, world", buffer.ToString());
    }

    [Fact]
    public void Append_SpanOverloadMatchesStringAppend()
    {
        var buffer = new TextBuffer();
        buffer.Append("xi");

        buffer.Append(" editor".AsSpan(1)); // "editor"

        Assert.Equal("xieditor", buffer.ToString());
    }
}
