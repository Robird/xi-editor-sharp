using System.Collections.Generic;
using System.Linq;
using Xi.Core.Rope.Tree;
using Xunit;
using RopeBuffer = Xi.Core.Rope.Rope;

namespace Xi.Core.Tests;

public sealed class RopeChunkEnumeratorTests
{
    [Fact]
    public void EmptyRopeReturnsSingleEmptyChunk()
    {
        var rope = new RopeBuffer();
        var chunks = CollectChunks(rope);

        Assert.Single(chunks);
        Assert.Equal(string.Empty, chunks[0]);
    }

    [Fact]
    public void SingleLeafChunkMatchesSource()
    {
        var rope = new RopeBuffer();
        rope.Append("hello world");

        var chunks = CollectChunks(rope);
        Assert.Single(chunks);
        Assert.Equal("hello world", chunks[0]);
    }

    [Fact]
    public void MultiLeafRopeProducesAllContent()
    {
        var rope = new RopeBuffer();
        var input = new string('a', Node.MaxLeafSize + 64);
        rope.Append(input);

        var actual = string.Concat(CollectChunks(rope));
        Assert.Equal(input, actual);
    }

    [Fact]
    public void ChunksPreserveCrLfPairs()
    {
        var rope = new RopeBuffer();
        var text = string.Join("\r\n", new[] { "one", "two", "three" });
        rope.Append(text);

        var concatenated = string.Concat(CollectChunks(rope));
        Assert.Equal(text, concatenated);
    }

    private static List<string> CollectChunks(RopeBuffer rope)
    {
        var list = new List<string>();
        foreach (var chunk in rope.EnumerateChunks())
        {
            list.Add(new string(chunk.Span));
        }

        return list;
    }
}
