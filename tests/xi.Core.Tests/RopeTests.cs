using System;
using Xi.Core.Rope.Tree;
using static Xi.Core.Tests.RopeTestHelpers;

namespace Xi.Core.Tests;

public class RopeTests
{
    private static Rope.Rope Create() => new Rope.Rope();

    private static void AssertBufferInvariants(Rope.Rope buffer, bool enforceLeafMinimum = false)
    {
        AssertInvariants(GetRoot(buffer), enforceLeafMinimum);
    }

    [Fact]
    public void Append_AppendsStringsInOrder()
    {
        var buffer = Create();

        buffer.Append("hello");
        buffer.Append(", ");
        buffer.Append("world");

        Assert.Equal("hello, world", buffer.Snapshot());
        Assert.Equal(12, buffer.Length);
        AssertBufferInvariants(buffer);
    }

    [Fact]
    public void Append_SpanOverloadMatchesStringAppend()
    {
        var buffer = Create();
        buffer.Append("xi");

        buffer.Append(" editor".AsSpan(1));

        Assert.Equal("xieditor", buffer.Snapshot());
        AssertBufferInvariants(buffer);
    }

    [Fact]
    public void Clear_ResetsState()
    {
        var buffer = Create();
        buffer.Append("content");

        buffer.Clear();

        Assert.Equal(0, buffer.Length);
        Assert.Equal(string.Empty, buffer.Snapshot());
        AssertBufferInvariants(buffer);
    }

    [Fact]
    public void GetSlice_ReturnsExpectedSegment()
    {
        var buffer = Create();
        buffer.Append("abcdef");

        Assert.Equal("cd", buffer.GetSlice(2, 2));
        AssertBufferInvariants(buffer);
    }

    [Fact]
    public void GetSlice_HandlesSegmentsAcrossLeaves()
    {
        var buffer = Create();
        buffer.Append(new string('a', Node.MaxLeafSize));
        buffer.Append("middle");
        buffer.Append(new string('b', Node.MaxLeafSize));

        var slice = buffer.GetSlice(Node.MaxLeafSize - 3, 10);

        Assert.Equal(new string('a', 3) + "middle" + "b", slice);
    AssertBufferInvariants(buffer);
    }

    [Fact]
    public void Replace_InsertsTextAcrossLeaves()
    {
        var buffer = Create();
        buffer.Append(new string('a', Node.MaxLeafSize));
        buffer.Append(new string('b', Node.MaxLeafSize));

        var insertPosition = Node.MaxLeafSize / 2;
        buffer.Replace(insertPosition, 0, "XYZ");

        var expected = new string('a', insertPosition) + "XYZ" + new string('a', Node.MaxLeafSize - insertPosition) + new string('b', Node.MaxLeafSize);
        Assert.Equal(expected, buffer.Snapshot());
    AssertBufferInvariants(buffer);
    }

    [Fact]
    public void Replace_DeletesRangeAcrossLeaves()
    {
        var buffer = Create();
        buffer.Append(new string('a', Node.MaxLeafSize));
        buffer.Append("middle");
        buffer.Append(new string('b', Node.MaxLeafSize));

        buffer.Replace(Node.MaxLeafSize - 2, 6, null);

        var expected = new string('a', Node.MaxLeafSize - 2) + "le" + new string('b', Node.MaxLeafSize);
        Assert.Equal(expected, buffer.Snapshot());
    AssertBufferInvariants(buffer);
    }

    [Fact]
    public void Replace_PerformsInPlaceLeafEdit()
    {
        var buffer = Create();
        buffer.Append("hello brave world");

        buffer.Replace(6, 5, "rope");

        Assert.Equal("hello rope world", buffer.Snapshot());
        AssertBufferInvariants(buffer);
    }

    [Fact]
    public void SequenceOfEdits_MaintainsInvariants()
    {
        var buffer = Create();
        buffer.Append(new string('a', Node.MaxLeafSize));
        buffer.Append(new string('b', Node.MaxLeafSize));
        buffer.Append(new string('c', Node.MaxLeafSize));

    AssertBufferInvariants(buffer);

        buffer.Replace(Node.MaxLeafSize - 10, 20, new string('x', 40));
    AssertBufferInvariants(buffer);

        buffer.Replace(Node.MaxLeafSize + 100, 150, new string('y', 30));
    AssertBufferInvariants(buffer);

        buffer.Replace(50, 25, "hello world");
        AssertBufferInvariants(buffer);

    buffer.Replace(buffer.Length - 200, 150, new string('z', 80));
    AssertBufferInvariants(buffer);

    var snapshot = buffer.Snapshot();
    Assert.Contains("hello world", snapshot);
    Assert.Contains("zzzz", snapshot);
    Assert.True(snapshot.Length > Node.MaxLeafSize * 2);
    }

    [Fact]
    public void EditVersion_IncrementsOnStructuralMutation()
    {
        var buffer = Create();
        var initial = buffer.EditVersion;

        buffer.Append("abc");
        var afterAppend = buffer.EditVersion;
        Assert.True(afterAppend > initial);

        buffer.Replace(0, 3, "xyz");
        var afterReplace = buffer.EditVersion;
        Assert.True(afterReplace > afterAppend);

        buffer.Clear();
        var afterClear = buffer.EditVersion;
        Assert.True(afterClear > afterReplace);

        buffer.Replace(0, 0, string.Empty);
        Assert.Equal(afterClear, buffer.EditVersion);
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
