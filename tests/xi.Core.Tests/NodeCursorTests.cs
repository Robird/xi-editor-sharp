using System;
using Xunit;
using Xi.Core.Rope;
using Xi.Core.Rope.Tree;

namespace Xi.Core.Tests;

/// <summary>
/// Tests for NodeCursor implementation (M3 T1.1-T1.5).
/// Verifies basic navigation, metric support, and invalidation behavior.
/// </summary>
public sealed class NodeCursorTests
{
    [Fact]
    public void Constructor_WithNullRoot_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new NodeCursor((Node)null!, 0));
    }

    [Fact]
    public void Constructor_WithNegativePosition_ThrowsArgumentOutOfRangeException()
    {
        var root = Node.FromLeaf("hello");
        Assert.Throws<ArgumentOutOfRangeException>(() => new NodeCursor(root, -1));
    }

    [Fact]
    public void Constructor_WithPositionBeyondLength_ThrowsArgumentOutOfRangeException()
    {
        var root = Node.FromLeaf("hello");
        Assert.Throws<ArgumentOutOfRangeException>(() => new NodeCursor(root, 10));
    }

    [Fact]
    public void Constructor_WithEmptyRope_CreatesInvalidCursor()
    {
        var root = Node.Empty;
        var cursor = new NodeCursor(root, 0);
        
        Assert.Equal(0, cursor.Position);
        Assert.Equal(0, cursor.TotalLength);
        // Empty rope: cursor is valid at position 0
        Assert.True(cursor.IsValid);
    }

    [Fact]
    public void Constructor_WithSingleLeaf_CreatesValidCursor()
    {
        var root = Node.FromLeaf("hello");
        var cursor = new NodeCursor(root, 0);
        
        Assert.Equal(0, cursor.Position);
        Assert.Equal(5, cursor.TotalLength);
        Assert.True(cursor.IsValid);
    }

    [Fact]
    public void GetLeaf_OnEmptyRope_ReturnsEmptyLeaf()
    {
        var root = Node.Empty;
        var cursor = new NodeCursor(root, 0);
        
        var leaf = cursor.GetLeaf();
        Assert.NotNull(leaf);
        Assert.Equal("", leaf.Value.Leaf);
        Assert.Equal(0, leaf.Value.Offset);
    }

    [Fact]
    public void GetLeaf_OnSingleLeaf_ReturnsCorrectLeafAndOffset()
    {
        var root = Node.FromLeaf("hello");
        var cursor = new NodeCursor(root, 2);
        
        var leaf = cursor.GetLeaf();
        Assert.NotNull(leaf);
        Assert.Equal("hello", leaf.Value.Leaf);
        Assert.Equal(2, leaf.Value.Offset);
    }

    [Fact]
    public void SetPosition_WithinSameLeaf_PreservesLeafCache()
    {
        var root = Node.FromLeaf("hello");
        var cursor = new NodeCursor(root, 0);
        
        cursor.SetPosition(3);
        Assert.Equal(3, cursor.Position);
        Assert.True(cursor.IsValid);
        
        var leaf = cursor.GetLeaf();
        Assert.NotNull(leaf);
        Assert.Equal("hello", leaf.Value.Leaf);
        Assert.Equal(3, leaf.Value.Offset);
    }

    [Fact]
    public void SetPosition_ToEOF_CreatesValidCursor()
    {
        var root = Node.FromLeaf("hello");
        var cursor = new NodeCursor(root, 0);
        
        cursor.SetPosition(5); // EOF
        Assert.Equal(5, cursor.Position);
        Assert.True(cursor.IsValid);
    }

    [Fact]
    public void IsBoundary_WithBaseMetric_AtBOF_ReturnsTrue()
    {
        var root = Node.FromLeaf("hello");
        var cursor = new NodeCursor(root, 0);
        
        Assert.True(cursor.IsBoundary(BaseMetric.Instance));
    }

    [Fact]
    public void IsBoundary_WithBaseMetric_AtEOF_ReturnsTrue()
    {
        var root = Node.FromLeaf("hello");
        var cursor = new NodeCursor(root, 5);
        
        Assert.True(cursor.IsBoundary(BaseMetric.Instance));
    }

    [Fact]
    public void IsBoundary_WithBaseMetric_InMiddle_ReturnsTrue()
    {
        var root = Node.FromLeaf("hello");
        var cursor = new NodeCursor(root, 2);
        
        // BaseMetric: every position is a boundary (surrogate-safe)
        Assert.True(cursor.IsBoundary(BaseMetric.Instance));
    }

    [Fact]
    public void IsBoundary_WithLinesMetric_AtNewline_ReturnsTrue()
    {
        var root = Node.FromLeaf("hello\nworld");
        var cursor = new NodeCursor(root, 6); // After '\n'
        
        Assert.True(cursor.IsBoundary(LinesMetric.Instance));
    }

    [Fact]
    public void IsBoundary_WithLinesMetric_NotAtNewline_ReturnsFalse()
    {
        var root = Node.FromLeaf("hello\nworld");
        var cursor = new NodeCursor(root, 2); // 'l'
        
        Assert.False(cursor.IsBoundary(LinesMetric.Instance));
    }

    [Fact]
    public void MoveToNext_WithBaseMetric_AdvancesOneCodeUnit()
    {
        var root = Node.FromLeaf("hello");
        var cursor = new NodeCursor(root, 0);
        
        var next = cursor.MoveToNext(BaseMetric.Instance);
        Assert.NotNull(next);
        Assert.Equal(1, next.Value);
        Assert.Equal(1, cursor.Position);
    }

    [Fact]
    public void MoveToNext_WithLinesMetric_AdvancesToNextNewline()
    {
        var root = Node.FromLeaf("hello\nworld\n");
        var cursor = new NodeCursor(root, 0);
        
        var next = cursor.MoveToNext(LinesMetric.Instance);
        Assert.NotNull(next);
        Assert.Equal(6, next.Value); // After first '\n'
        Assert.Equal(6, cursor.Position);
    }

    [Fact]
    public void MoveToNext_AtEOF_ReturnsNull()
    {
        var root = Node.FromLeaf("hello");
        var cursor = new NodeCursor(root, 5);
        
        var next = cursor.MoveToNext(BaseMetric.Instance);
        Assert.Null(next);
        Assert.False(cursor.IsValid);
    }

    [Fact]
    public void MoveToPrevious_WithBaseMetric_RewindsOneCodeUnit()
    {
        var root = Node.FromLeaf("hello");
        var cursor = new NodeCursor(root, 3);
        
        var prev = cursor.MoveToPrevious(BaseMetric.Instance);
        Assert.NotNull(prev);
        Assert.Equal(2, prev.Value);
        Assert.Equal(2, cursor.Position);
    }

    [Fact]
    public void MoveToPrevious_WithLinesMetric_RewindsToPreviousNewline()
    {
        var root = Node.FromLeaf("hello\nworld\n");
        var cursor = new NodeCursor(root, 12); // After second '\n'
        
        var prev = cursor.MoveToPrevious(LinesMetric.Instance);
        Assert.NotNull(prev);
        Assert.Equal(6, prev.Value); // After first '\n'
        Assert.Equal(6, cursor.Position);
    }

    [Fact]
    public void MoveToPrevious_AtBOF_ReturnsNull()
    {
        var root = Node.FromLeaf("hello");
        var cursor = new NodeCursor(root, 0);
        
        var prev = cursor.MoveToPrevious(BaseMetric.Instance);
        Assert.Null(prev);
        Assert.False(cursor.IsValid);
    }

    [Fact]
    public void AtOrNext_WhenAtBoundary_ReturnsCurrentPosition()
    {
        var root = Node.FromLeaf("hello\nworld");
        var cursor = new NodeCursor(root, 6); // At newline boundary
        
        var pos = cursor.AtOrNext(LinesMetric.Instance);
        Assert.NotNull(pos);
        Assert.Equal(6, pos.Value);
        Assert.Equal(6, cursor.Position);
    }

    [Fact]
    public void AtOrNext_WhenNotAtBoundary_AdvancesToNext()
    {
        var root = Node.FromLeaf("hello\nworld");
        var cursor = new NodeCursor(root, 2); // Not at boundary
        
        var pos = cursor.AtOrNext(LinesMetric.Instance);
        Assert.NotNull(pos);
        Assert.Equal(6, pos.Value);
        Assert.Equal(6, cursor.Position);
    }

    [Fact]
    public void AtOrPrevious_WhenAtBoundary_ReturnsCurrentPosition()
    {
        var root = Node.FromLeaf("hello\nworld");
        var cursor = new NodeCursor(root, 6); // At newline boundary
        
        var pos = cursor.AtOrPrevious(LinesMetric.Instance);
        Assert.NotNull(pos);
        Assert.Equal(6, pos.Value);
        Assert.Equal(6, cursor.Position);
    }

    [Fact]
    public void AtOrPrevious_WhenNotAtBoundary_RewindsToPrevious()
    {
        var root = Node.FromLeaf("hello\nworld\n");
        var cursor = new NodeCursor(root, 8); // 'o' in "world"
        
        var pos = cursor.AtOrPrevious(LinesMetric.Instance);
        Assert.NotNull(pos);
        Assert.Equal(6, pos.Value); // After first '\n'
        Assert.Equal(6, cursor.Position);
    }

    [Fact]
    public void RoundTrip_BaseMetric_IteratesThroughAllPositions()
    {
        var root = Node.FromLeaf("hello");
        var cursor = new NodeCursor(root, 0);
        
        int count = 0;
        while (cursor.IsValid && count < 10) // Safety limit
        {
            count++;
            var next = cursor.MoveToNext(BaseMetric.Instance);
            if (next == null)
            {
                break;
            }
        }
        
        Assert.Equal(5, count); // 0→1→2→3→4→5(EOF)
    }

    [Fact]
    public void RoundTrip_LinesMetric_IteratesThroughAllNewlines()
    {
        var root = Node.FromLeaf("line1\nline2\nline3\n");
        var cursor = new NodeCursor(root, 0);
        
        int lineCount = 0;
        while (cursor.IsValid && lineCount < 10) // Safety limit
        {
            var next = cursor.MoveToNext(LinesMetric.Instance);
            if (next == null)
            {
                break;
            }
            lineCount++;
        }
        
        Assert.Equal(3, lineCount); // 3 newlines
    }

    [Fact]
    public void CursorBoundToRope_InvalidatesAfterEdit()
    {
        var rope = new Rope.Rope();
        rope.Append("abc");
        var cursor = new NodeCursor(rope, 0);

        var first = cursor.MoveToNext(BaseMetric.Instance);
        Assert.NotNull(first);
        Assert.Equal(1, first.Value);

        rope.Append("def");

        var afterEdit = cursor.MoveToNext(BaseMetric.Instance);
        Assert.Null(afterEdit);
        Assert.False(cursor.IsValid);
    }

    [Fact]
    public void CursorBoundToRope_SetPositionAfterEditThrows()
    {
        var rope = new Rope.Rope();
        rope.Append("abc");
        var cursor = new NodeCursor(rope, 1);

        rope.Replace(0, 1, "z");

        Assert.Throws<InvalidOperationException>(() => cursor.SetPosition(0));
    }
}
