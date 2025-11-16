using System;
using Xi.Core.Rope;
using Xi.Core.Rope.Tree;
using Xunit;

namespace Xi.Core.Tests;

public sealed class RopeChunkEnumeratorDiagnosticsTests
{
    [Fact]
    public void DiagnosticsCaptureEmptyRope()
    {
        var rope = new Rope.Rope();
        var diagnostics = new RopeChunkEnumeratorDiagnostics();
        int iterations = 0;

        foreach (var chunk in rope.EnumerateChunks(diagnostics))
        {
            iterations++;
            Assert.Equal(0, chunk.Length);
        }

        Assert.Equal(1, iterations);
        Assert.Equal(1, diagnostics.ChunkCount);
        Assert.Equal(0, diagnostics.MaxChunkLength);
        Assert.Equal(0, diagnostics.TotalUtf16CharCount);
    }

    [Fact]
    public void DiagnosticsCaptureSingleLeaf()
    {
        var rope = new Rope.Rope();
        rope.Append("hello world");

        var diagnostics = new RopeChunkEnumeratorDiagnostics();
        int totalChars = 0;

        foreach (var chunk in rope.EnumerateChunks(diagnostics))
        {
            totalChars += chunk.Length;
        }

        Assert.Equal(1, diagnostics.ChunkCount);
        Assert.Equal("hello world".Length, diagnostics.MaxChunkLength);
        Assert.Equal(totalChars, diagnostics.TotalUtf16CharCount);
        Assert.Equal("hello world".Length, totalChars);
    }

    [Fact]
    public void DiagnosticsCaptureMultipleLeaves()
    {
        var rope = new Rope.Rope();
        var payload = new string('x', Node.MaxLeafSize + 256);
        rope.Append(payload);

        var diagnostics = new RopeChunkEnumeratorDiagnostics();
        int totalChars = 0;
        int observedMax = 0;
        int observedChunks = 0;

        foreach (var chunk in rope.EnumerateChunks(diagnostics))
        {
            totalChars += chunk.Length;
            observedMax = Math.Max(observedMax, chunk.Length);
            observedChunks++;
        }

        Assert.Equal(payload.Length, totalChars);
        Assert.Equal(totalChars, diagnostics.TotalUtf16CharCount);
        Assert.Equal(observedMax, diagnostics.MaxChunkLength);
        Assert.Equal(observedChunks, diagnostics.ChunkCount);
        Assert.True(diagnostics.ChunkCount >= 2);
    }
}
