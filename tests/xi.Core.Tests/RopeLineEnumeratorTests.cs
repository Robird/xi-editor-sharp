using System.Collections.Generic;
using Xi.Core.Rope.Tree;
using Xunit;
using RopeBuffer = Xi.Core.Rope.Rope;

namespace Xi.Core.Tests;

public sealed class RopeLineEnumeratorTests
{
    [Fact]
    public void EnumerateLines_UnixNewlines()
    {
        var rope = new RopeBuffer();
        rope.Append("alpha\nbeta\ngamma");

        var lines = CollectLines(rope);
        Assert.Equal(new[] { "alpha\n", "beta\n", "gamma" }, lines);
    }

    [Fact]
    public void EnumerateLines_WindowsNewlines()
    {
        var rope = new RopeBuffer();
        rope.Append("alpha\r\nbeta\r\ngamma");

        var lines = CollectLines(rope);
        Assert.Equal(new[] { "alpha\r\n", "beta\r\n", "gamma" }, lines);
    }

    [Fact]
    public void EnumerateLines_NoTrailingNewline()
    {
        var rope = new RopeBuffer();
        rope.Append("solo line");

        var lines = CollectLines(rope);
        Assert.Single(lines);
        Assert.Equal("solo line", lines[0]);
    }

    [Fact]
    public void EnumerateLines_ConsecutiveEmptyLines()
    {
        var rope = new RopeBuffer();
        rope.Append("\n\n");

        var lines = CollectLines(rope);
        Assert.Equal(new[] { "\n", "\n" }, lines);
    }

    [Fact]
    public void EnumerateLines_CrLfAcrossChunkBoundary()
    {
        var rope = new RopeBuffer();
        var prefix = new string('x', Node.MaxLeafSize - 1);
        rope.Append(prefix + "\r\n" + "tail");

        var lines = CollectLines(rope);
        Assert.Equal(new[] { prefix + "\r\n", "tail" }, lines);
    }

    private static List<string> CollectLines(RopeBuffer rope)
    {
        var results = new List<string>();
        foreach (var line in rope.EnumerateLines())
        {
            results.Add(new string(line.Span));
        }

        return results;
    }
}
