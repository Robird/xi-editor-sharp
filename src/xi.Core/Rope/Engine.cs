using System;
using System.Collections.Generic;

namespace Xi.Core.Rope;

internal readonly struct RevId
{
    public RevId(long session1, int session2, int number)
    {
        if (session1 < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(session1), "Session identifier must be non-negative.");
        }

        if (session2 < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(session2), "Session identifier must be non-negative.");
        }

        if (number < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(number), "Revision number must be non-negative.");
        }

        Session1 = session1;
        Session2 = session2;
        Number = number;
    }

    public long Session1 { get; }

    public int Session2 { get; }

    public int Number { get; }
}

internal enum RevisionOperationKind
{
    Edit,
    Undo
}

internal abstract class RevisionOperation
{
    internal abstract RevisionOperationKind Kind { get; }

    internal bool IsEdit => Kind == RevisionOperationKind.Edit;

    internal bool IsUndo => Kind == RevisionOperationKind.Undo;

    internal RevisionEdit AsEdit()
    {
        if (!IsEdit)
        {
            throw new InvalidOperationException("Revision operation is not an edit payload.");
        }

        return (RevisionEdit)this;
    }

    internal RevisionUndo AsUndo()
    {
        if (!IsUndo)
        {
            throw new InvalidOperationException("Revision operation is not an undo payload.");
        }

        return (RevisionUndo)this;
    }
}

internal sealed class RevisionEdit : RevisionOperation
{
    internal RevisionEdit(int priority, int undoGroup, Subset inserts, Subset deletes)
    {
        if (priority < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be non-negative.");
        }

        if (undoGroup < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(undoGroup), "Undo group must be non-negative.");
        }

        Inserts = inserts ?? throw new ArgumentNullException(nameof(inserts));
        Deletes = deletes ?? throw new ArgumentNullException(nameof(deletes));
        Priority = priority;
        UndoGroup = undoGroup;
    }

    internal override RevisionOperationKind Kind => RevisionOperationKind.Edit;

    internal int Priority { get; }

    internal int UndoGroup { get; }

    internal Subset Inserts { get; }

    internal Subset Deletes { get; }
}

internal sealed class RevisionUndo : RevisionOperation
{
    private readonly int[] _toggledGroups;
    private readonly IReadOnlyList<int> _toggledGroupsView;

    internal RevisionUndo(IEnumerable<int> toggledGroups, Subset deletesBitxor)
    {
        if (toggledGroups == null)
        {
            throw new ArgumentNullException(nameof(toggledGroups));
        }

        _toggledGroups = CopyGroups(toggledGroups);
        _toggledGroupsView = Array.AsReadOnly(_toggledGroups);
        DeletesBitxor = deletesBitxor ?? throw new ArgumentNullException(nameof(deletesBitxor));
    }

    internal override RevisionOperationKind Kind => RevisionOperationKind.Undo;

    internal IReadOnlyList<int> ToggledGroups => _toggledGroupsView;

    internal Subset DeletesBitxor { get; }

    private static int[] CopyGroups(IEnumerable<int> groups)
    {
        var list = new List<int>();
        foreach (var group in groups)
        {
            if (group < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(groups), "Group identifiers must be non-negative.");
            }

            list.Add(group);
        }

        if (list.Count == 0)
        {
            return Array.Empty<int>();
        }

        return list.ToArray();
    }
}

internal sealed class Revision
{
    internal Revision(RevId revId, int maxUndoSoFar, RevisionOperation operation)
    {
        if (maxUndoSoFar < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxUndoSoFar), "Maximum undo depth must be non-negative.");
        }

        RevId = revId;
        MaxUndoSoFar = maxUndoSoFar;
        Operation = operation ?? throw new ArgumentNullException(nameof(operation));
    }

    internal RevId RevId { get; }

    internal int MaxUndoSoFar { get; }

    internal RevisionOperation Operation { get; }
}

internal sealed class Engine
{
    private readonly string _text;
    private readonly string _tombstones;
    private readonly Subset _deletesFromUnion;
    private readonly int[] _undoneGroups;
    private readonly Revision[] _revisions;
    private readonly IReadOnlyList<int> _undoneGroupsView;
    private readonly IReadOnlyList<Revision> _revisionLogView;

    private Engine(string text, string tombstones, Subset deletesFromUnion, int[] undoneGroups, Revision[] revisions)
    {
        _text = text;
        _tombstones = tombstones;
        _deletesFromUnion = deletesFromUnion;
        _undoneGroups = undoneGroups;
        _revisions = revisions;
        _undoneGroupsView = Array.AsReadOnly(_undoneGroups);
        _revisionLogView = Array.AsReadOnly(_revisions);
    }

    internal static Engine FromSerializedState(
        string text,
        string tombstones,
        Subset deletesFromUnion,
        IEnumerable<int> undoneGroups,
        IEnumerable<Revision> revisions)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (tombstones == null)
        {
            throw new ArgumentNullException(nameof(tombstones));
        }

        if (deletesFromUnion == null)
        {
            throw new ArgumentNullException(nameof(deletesFromUnion));
        }

        if (undoneGroups == null)
        {
            throw new ArgumentNullException(nameof(undoneGroups));
        }

        if (revisions == null)
        {
            throw new ArgumentNullException(nameof(revisions));
        }

        var undoneGroupsArray = CopyGroups(undoneGroups);
        var revisionsArray = CopyRevisions(revisions);

        return new Engine(text, tombstones, deletesFromUnion, undoneGroupsArray, revisionsArray);
    }

    private static int[] CopyGroups(IEnumerable<int> source)
    {
        var list = new List<int>();
        foreach (var group in source)
        {
            if (group < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(source), "Group identifiers must be non-negative.");
            }

            list.Add(group);
        }

        if (list.Count == 0)
        {
            return Array.Empty<int>();
        }

        return list.ToArray();
    }

    private static Revision[] CopyRevisions(IEnumerable<Revision> source)
    {
        var list = new List<Revision>();
        foreach (var revision in source)
        {
            if (revision == null)
            {
                throw new ArgumentException("Revision collection must not contain null entries.", nameof(source));
            }

            list.Add(revision);
        }

        if (list.Count == 0)
        {
            return Array.Empty<Revision>();
        }

        return list.ToArray();
    }

    internal string TextSnapshot()
    {
        return _text;
    }

    internal string TombstonesSnapshot()
    {
        return _tombstones;
    }

    internal Subset DeletesFromUnionSnapshot()
    {
        return _deletesFromUnion;
    }

    internal IReadOnlyList<int> UndoneGroupsSnapshot()
    {
        return _undoneGroupsView;
    }

    internal IReadOnlyList<Revision> RevisionLog()
    {
        return _revisionLogView;
    }
}
