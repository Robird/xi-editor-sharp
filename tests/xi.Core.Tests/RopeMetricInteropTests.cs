using System;
using System.Collections.Generic;
using System.Text;
using RopeBuffer = Xi.Core.Rope.Rope;
using Xunit;

namespace Xi.Core.Tests;

public sealed class RopeMetricInteropTests
{
    [Fact]
    public void ConvertLinesFromBytesMatchesManualCount()
    {
        var rope = new RopeBuffer();
        rope.Append("a\nb\nc");
        var snapshot = rope.Snapshot();

        foreach (var offset in GetUtf16Boundaries(snapshot))
        {
            var expected = CountLines(snapshot, offset);
            Assert.Equal(expected, rope.ConvertLinesFromBytes(offset));
        }
    }

    [Fact]
    public void ConvertBytesFromLinesMatchesLineStarts()
    {
        var rope = new RopeBuffer();
        rope.Append("alpha\nbeta\n\nomega");
        var snapshot = rope.Snapshot();
        var lineOffsets = GetLineOffsets(snapshot);

        for (var line = 0; line < lineOffsets.Count; line++)
        {
            Assert.Equal(lineOffsets[line], rope.ConvertBytesFromLines(line));
        }
    }

    [Fact]
    public void ConvertUtf16FromBytesAccountsForSurrogates()
    {
        var rope = new RopeBuffer();
        rope.Append("a😀b💖c");
        var snapshot = rope.Snapshot();

        foreach (var offset in GetUtf16Boundaries(snapshot))
        {
            var expected = CountUtf16Units(snapshot, offset);
            Assert.Equal(expected, rope.ConvertUtf16FromBytes(offset));
        }
    }

    [Fact]
    public void ConvertBytesFromUtf16MatchesOffsets()
    {
        var rope = new RopeBuffer();
        rope.Append("😀multi\nline💖");
        var snapshot = rope.Snapshot();

        foreach (var units in GetUtf16Boundaries(snapshot))
        {
            var expectedOffset = GetOffsetFromUtf16Units(snapshot, units);
            Assert.Equal(expectedOffset, rope.ConvertBytesFromUtf16(units));
        }
    }

    [Fact]
    public void ConvertThrowsOnOutOfRangeInput()
    {
        var rope = new RopeBuffer();
        rope.Append("hi");

        Assert.Throws<ArgumentOutOfRangeException>(() => rope.ConvertLinesFromBytes(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => rope.ConvertLinesFromBytes(rope.Length + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => rope.ConvertUtf16FromBytes(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => rope.ConvertUtf16FromBytes(rope.Length + 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => rope.ConvertBytesFromLines(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => rope.ConvertBytesFromLines(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => rope.ConvertBytesFromUtf16(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => rope.ConvertBytesFromUtf16(rope.Length + 1));
    }

    private static int CountLines(string text, int length)
    {
        var span = text.AsSpan(0, length);
        var count = 0;
        foreach (var ch in span)
        {
            if (ch == '\n')
            {
                count++;
            }
        }

        return count;
    }

    private static int CountUtf16Units(string text, int length)
    {
        var total = 0;
        foreach (var rune in text.AsSpan(0, length).EnumerateRunes())
        {
            total += rune.Utf16SequenceLength;
        }

        return total;
    }

    private static IReadOnlyList<int> GetUtf16Boundaries(string text)
    {
        var boundaries = new List<int> { 0 };
        var offset = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            offset += rune.Utf16SequenceLength;
            boundaries.Add(offset);
        }

        return boundaries;
    }

    private static IReadOnlyList<int> GetLineOffsets(string text)
    {
        var offsets = new List<int> { 0 };
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '\n')
            {
                offsets.Add(index + 1);
            }
        }

        offsets.Add(text.Length);
        return offsets;
    }

    private static int GetOffsetFromUtf16Units(string text, int units)
    {
        if (units < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(units));
        }

        if (units == 0)
        {
            return 0;
        }

        var collected = 0;
        var offset = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            var length = rune.Utf16SequenceLength;
            if (collected + length > units)
            {
                break;
            }

            collected += length;
            offset += length;

            if (collected == units)
            {
                return offset;
            }
        }

        if (collected == units)
        {
            return offset;
        }

        throw new ArgumentOutOfRangeException(nameof(units));
    }
}
