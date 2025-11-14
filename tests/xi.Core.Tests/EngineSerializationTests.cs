using System;
using System.IO;
using System.Linq;
using Xi.Core.Rope;
using Xunit;

namespace Xi.Core.Tests;

public sealed class EngineSerializationTests
{
    [Fact]
    public void SerializeMatchesFixture()
    {
        var engine = BuildSampleEngine();
        var json = EngineJson.Serialize(engine);
        var fixture = LoadFixture("engine_regression.json");

        Assert.Equal(fixture, json);
    }

    [Fact]
    public void DeserializeRoundTripsFixture()
    {
        var fixture = LoadFixture("engine_regression.json");
        var engine = EngineJson.Deserialize(fixture);
        var roundTrip = EngineJson.Serialize(engine);

        Assert.Equal(fixture, roundTrip);
    }

    [Fact]
    public void RevisionLogMatchesExpectedSequence()
    {
        var fixture = LoadFixture("engine_regression.json");
        var engine = EngineJson.Deserialize(fixture);
        var revisions = engine.RevisionLog().ToArray();

        Assert.Equal(5, revisions.Length);

        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, revisions.Select(r => r.RevId.Number).ToArray());
        Assert.Equal(new[] { 0, 0, 1, 2, 2 }, revisions.Select(r => r.MaxUndoSoFar).ToArray());

        Assert.Equal(
            new[]
            {
                RevisionOperationKind.Undo,
                RevisionOperationKind.Edit,
                RevisionOperationKind.Edit,
                RevisionOperationKind.Edit,
                RevisionOperationKind.Undo
            },
            revisions.Select(r => r.Operation.Kind).ToArray());

        var firstUndo = revisions[0].Operation.AsUndo();
        Assert.Empty(firstUndo.ToggledGroups);
        Assert.Equal(0, firstUndo.DeletesBitxor.SegmentCount);

        var firstEdit = revisions[1].Operation.AsEdit();
        Assert.Equal(0, firstEdit.Priority);
        Assert.Equal(0, firstEdit.UndoGroup);
        Assert.Equal(1, firstEdit.Inserts.SegmentCount);
        Assert.Equal(1, firstEdit.Deletes.SegmentCount);

        var thirdEdit = revisions[2].Operation.AsEdit();
        Assert.Equal(2, thirdEdit.Inserts.SegmentCount);
        Assert.Equal(1, thirdEdit.Deletes.SegmentCount);

        var finalUndo = revisions[4].Operation.AsUndo();
        Assert.Single(finalUndo.ToggledGroups, 2);
        Assert.Equal(2, finalUndo.DeletesBitxor.SegmentCount);
    }

    private static Engine BuildSampleEngine()
    {
        var deletesFromUnion = CreateSubset((6, 1), (8, 0));
        var revisions = new Revision[]
        {
            new(new RevId(0, 0, 0), 0, new RevisionUndo(Array.Empty<int>(), Subset.Empty)),
            new(new RevId(1, 0, 1), 0, new RevisionEdit(0, 0, CreateSubset((2, 1)), CreateSubset((2, 0)))),
            new(new RevId(1, 0, 2), 1, new RevisionEdit(1, 1, CreateSubset((2, 0), (6, 1)), CreateSubset((8, 0)))),
            new(new RevId(1, 0, 3), 2, new RevisionEdit(0, 2, CreateSubset((6, 1), (8, 0)), CreateSubset((14, 0)))),
            new(new RevId(1, 0, 4), 2, new RevisionUndo(new[] { 2 }, CreateSubset((6, 1), (8, 0))))
        };

        return Engine.FromSerializedState("Hi there", "Well, ", deletesFromUnion, new[] { 2 }, revisions);
    }

    private static Subset CreateSubset(params (int Length, int Count)[] segments)
    {
        if (segments.Length == 0)
        {
            return Subset.Empty;
        }

        var triples = new (int Start, int Length, int Count)[segments.Length];
        var offset = 0;
        for (var i = 0; i < segments.Length; i++)
        {
            var (length, count) = segments[i];
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(segments), "Segment length must be positive.");
            }

            triples[i] = (offset, length, count);
            offset += length;
        }

        return Subset.FromSegmentTriples(triples);
    }

    private static string LoadFixture(string fixtureName)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var path = Path.Combine(baseDirectory, "Fixtures", fixtureName);
        return File.ReadAllText(path).Trim();
    }
}
