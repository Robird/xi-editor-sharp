using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xi.Core.Rope;
using Xi.Core.Tests.Fixtures.ParityFixtures;
using Xunit;

using RopeBuffer = Xi.Core.Rope.Rope;

namespace Xi.Core.Tests;

public sealed class RopeChunkParityTests
{
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly HashSet<string> LargeSamples = new(StringComparer.Ordinal)
    {
        "deep_tree_payload"
    };

    [Fact]
    public void ChunkDescriptorsMatchParitySamples()
    {
        var fixture = ParityFixtureLoader.ChunkFixtures;
        var samples = BuildSampleTexts(fixture.ChunkDescriptors);

        foreach (var kvp in samples)
        {
            var descriptors = fixture.ChunkDescriptors
                .Where(d => d.Sample == kvp.Key)
                .OrderBy(d => d.ChunkIndex)
                .ToList();

            var rope = BuildRopeFromChunks(descriptors);
            var actualChunks = CollectChunks(rope);

            Assert.Equal(descriptors.Count, actualChunks.Count);

            if (LargeSamples.Contains(kvp.Key))
            {
                continue;
            }

            var byteOffset = 0;
            var utf16Offset = 0;

            for (int i = 0; i < descriptors.Count; i++)
            {
                var descriptor = descriptors[i];
                var chunk = actualChunks[i];

                Assert.Equal(descriptor.Text, chunk);
                Assert.Equal(descriptor.IsEmpty, chunk.Length == 0);
                Assert.Equal(descriptor.ContainsCrlf, chunk.Contains("\r\n", StringComparison.Ordinal));

                Assert.Equal(descriptor.ByteRange.Start, byteOffset);
                byteOffset += Utf8.GetByteCount(chunk);
                Assert.Equal(descriptor.ByteRange.End, byteOffset);

                Assert.Equal(descriptor.Utf16Range.Start, utf16Offset);
                utf16Offset += chunk.Length;
                Assert.Equal(descriptor.Utf16Range.End, utf16Offset);
            }
        }
    }

    [Fact]
    public void LineDescriptorsMatchParitySamples()
    {
        var fixture = ParityFixtureLoader.ChunkFixtures;
        var samples = BuildSampleTexts(fixture.ChunkDescriptors);

        foreach (var grouping in fixture.LineDescriptors.GroupBy(l => l.Sample))
        {
            Assert.True(samples.TryGetValue(grouping.Key, out var sampleText),
                $"Missing chunk descriptors for sample '{grouping.Key}'.");

            var descriptors = grouping.OrderBy(l => l.LineIndex).ToList();
            var rope = BuildRopeFromText(sampleText);
            var actualLines = CollectLines(rope);

            Assert.Equal(descriptors.Count, actualLines.Count);

            var byteOffset = 0;
            var utf16Offset = 0;

            for (int i = 0; i < descriptors.Count; i++)
            {
                var descriptor = descriptors[i];
                var line = actualLines[i];

                Assert.Equal(descriptor.Raw, line);
                Assert.Equal(descriptor.NewlineKind, InferNewlineKind(line));

                Assert.Equal(descriptor.ByteRange.Start, byteOffset);
                byteOffset += Utf8.GetByteCount(line);
                Assert.Equal(descriptor.ByteRange.End, byteOffset);

                Assert.Equal(descriptor.Utf16Range.Start, utf16Offset);
                utf16Offset += line.Length;
                Assert.Equal(descriptor.Utf16Range.End, utf16Offset);
            }
        }
    }

    private static Dictionary<string, string> BuildSampleTexts(IEnumerable<ChunkDescriptor> descriptors)
    {
        return descriptors
            .GroupBy(d => d.Sample, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => string.Concat(g.OrderBy(d => d.ChunkIndex).Select(d => d.Text)),
                StringComparer.Ordinal);
    }

    private static RopeBuffer BuildRopeFromChunks(IReadOnlyList<ChunkDescriptor> descriptors)
    {
        var rope = new RopeBuffer();
        for (int i = 0; i < descriptors.Count; i++)
        {
            var segment = descriptors[i].Text;
            if (!string.IsNullOrEmpty(segment))
            {
                rope.Append(segment);
            }
        }

        return rope;
    }

    private static RopeBuffer BuildRopeFromText(string text)
    {
        var rope = new RopeBuffer();
        if (!string.IsNullOrEmpty(text))
        {
            rope.Append(text);
        }

        return rope;
    }

    private static List<string> CollectChunks(RopeBuffer rope)
    {
        var results = new List<string>();
        foreach (var chunk in rope.EnumerateChunks())
        {
            results.Add(new string(chunk.Span));
        }

        return results;
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

    private static string InferNewlineKind(string line)
    {
        if (line.EndsWith("\r\n", StringComparison.Ordinal))
        {
            return "cr_lf";
        }

        if (line.EndsWith("\n", StringComparison.Ordinal))
        {
            return "lf";
        }

        if (line.EndsWith("\r", StringComparison.Ordinal))
        {
            return "cr";
        }

        return "none";
    }
}
