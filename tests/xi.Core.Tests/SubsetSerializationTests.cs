using System;
using System.IO;
using System.Linq;
using Xi.Core.Rope;
using Xunit;

namespace Xi.Core.Tests;

public sealed class SubsetSerializationTests
{
    [Fact]
    public void SerializeMatchesFixture()
    {
        var builder = new SubsetBuilder();
        builder.PadToLength(2);
        builder.AddRange(2, 5, 3);
        builder.AddRange(6, 7, 1);
        builder.PadToLength(9);
        var subset = builder.Build();

        var json = SubsetJson.Serialize(subset);
        var fixture = LoadFixture("subset_regression.json");

        Assert.Equal(fixture, json);
    }

    [Fact]
    public void DeserializeRoundTripsFixture()
    {
        var fixture = LoadFixture("subset_regression.json");
        var subset = SubsetJson.Deserialize(fixture);
        var roundTrip = SubsetJson.Serialize(subset);

        Assert.Equal(fixture, roundTrip);
    }

    [Fact]
    public void FromSegmentTriplesRoundTrip()
    {
        var triples = new (int Start, int Length, int Count)[]
        {
            (0, 4, 0),
            (4, 2, 2),
            (6, 3, 0),
            (9, 1, 1)
        };

        var subset = Subset.FromSegmentTriples(triples);
        var roundTrip = subset.SegmentTriples().ToArray();

        Assert.Equal(triples, roundTrip);
        Assert.False(subset.IsEmpty);
        Assert.Equal(10, subset.Length);
        Assert.Equal(7, subset.LengthAfterDelete());
        Assert.Equal(triples.Length, subset.SegmentCount);
    }

    private static string LoadFixture(string fixtureName)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var path = Path.Combine(baseDirectory, "Fixtures", fixtureName);
        return File.ReadAllText(path).Trim();
    }
}
