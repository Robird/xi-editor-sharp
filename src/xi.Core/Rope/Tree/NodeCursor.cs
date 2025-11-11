using System;
using Xi.Core.Rope;

namespace Xi.Core.Rope.Tree;

/// <summary>
/// Cursor for traversing rope nodes. Mirrors xi-editor's <c>Cursor</c> type but focuses on structural wiring for now.
/// The implementation is intentionally left as skeleton while the surrounding infrastructure is being solidified.
/// </summary>
public sealed class NodeCursor
{
    /// <summary>Creates a cursor positioned at <paramref name="position"/>.</summary>
    public NodeCursor(Node root, int position)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        Position = position;
        throw new NotImplementedException("NodeCursor is a skeleton placeholder pending full implementation.");
    }

    /// <summary>The rope node this cursor traverses.</summary>
    public Node Root { get; }

    /// <summary>The current position in base units.</summary>
    public int Position { get; private set; }

    /// <summary>Returns the total length of the underlying rope.</summary>
    public int TotalLength => Root.Length;

    /// <summary>Returns the current leaf and offset when the cursor is valid.</summary>
    public (string Leaf, int Offset)? GetLeaf() => throw new NotImplementedException();

    /// <summary>Sets the cursor to the supplied absolute position.</summary>
    public void SetPosition(int position) => throw new NotImplementedException();

    /// <summary>Determines whether the current position is a boundary for the supplied metric.</summary>
    public bool IsBoundary(IMetric metric) => throw new NotImplementedException();

    /// <summary>Moves to the previous boundary defined by <paramref name="metric"/>.</summary>
    public int? MoveToPrevious(IMetric metric) => throw new NotImplementedException();

    /// <summary>Moves to the next boundary defined by <paramref name="metric"/>.</summary>
    public int? MoveToNext(IMetric metric) => throw new NotImplementedException();

    /// <summary>Returns the current position if it's a boundary; otherwise advances to the next one.</summary>
    public int? AtOrNext(IMetric metric) => throw new NotImplementedException();

    /// <summary>Returns the current position if it's a boundary; otherwise rewinds to the previous one.</summary>
    public int? AtOrPrevious(IMetric metric) => throw new NotImplementedException();
}
