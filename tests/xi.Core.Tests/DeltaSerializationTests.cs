using System;
using System.IO;
using System.Linq;
using Xi.Core.Rope;
using Xunit;

namespace Xi.Core.Tests;

public sealed class DeltaSerializationTests
{
    private const int BaseLength = 62;

    [Fact]
    public void SerializeMatchesFixture()
    {
        var delta = BuildSampleDelta();
        var json = DeltaJson.Serialize(delta);
        var fixture = LoadFixture("delta_regression.json");

        Assert.Equal(fixture, json);
    }

    [Fact]
    public void DeserializeRoundTripsFixture()
    {
        var fixture = LoadFixture("delta_regression.json");
        var delta = DeltaJson.Deserialize<RopeInfo>(fixture);
        var roundTrip = DeltaJson.Serialize(delta);

        Assert.Equal(fixture, roundTrip);
    }

    [Fact]
    public void FromElementsRoundTrip()
    {
        var tuples = SampleTuples();
        var delta = Delta<RopeInfo, string>.FromElements(BaseLength, tuples);

        var elements = delta.EnumerateElements().ToArray();
        Assert.Equal(5, elements.Length);

        Assert.True(elements[0].IsCopy);
        Assert.Equal((0, 3), (elements[0].AsCopy().Start, elements[0].AsCopy().End));

        Assert.True(elements[1].IsInsert);
        Assert.Equal("[ins]", elements[1].AsInsert().Value);

        Assert.True(elements[2].IsCopy);
        Assert.Equal((8, 10), (elements[2].AsCopy().Start, elements[2].AsCopy().End));

        Assert.True(elements[3].IsInsert);
        Assert.Equal("!", elements[3].AsInsert().Value);

        Assert.True(elements[4].IsCopy);
        Assert.Equal((15, 62), (elements[4].AsCopy().Start, elements[4].AsCopy().End));

        Assert.Equal(BaseLength, delta.BaseLength);
        Assert.Equal(elements.Length, delta.ElementCount);

        var triples = delta.EnumerateElementTriples().ToArray();
        Assert.Equal((false, 0, 3), triples[0]);
        Assert.Equal((true, 3, 8), triples[1]);
        Assert.Equal((false, 8, 10), triples[2]);
        Assert.Equal((true, 10, 11), triples[3]);
        Assert.Equal((false, 15, 62), triples[4]);
    }

    [Fact]
    public void FactorPlaceholderThrows()
    {
        var delta = BuildSampleDelta();
        Assert.Throws<NotImplementedException>(() => delta.Factor());
    }

    private static Delta<RopeInfo, string> BuildSampleDelta()
    {
        var tuples = SampleTuples();
        return Delta<RopeInfo, string>.FromElements(BaseLength, tuples);
    }

    private static (int? CopyStart, int? CopyEnd, string? Insert)[] SampleTuples()
    {
        return new (int? CopyStart, int? CopyEnd, string? Insert)[]
        {
            (0, 3, null),
            (null, null, "[ins]"),
            (8, 10, null),
            (null, null, "!"),
            (15, 62, null)
        };
    }

    private static string LoadFixture(string fixtureName)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var path = Path.Combine(baseDirectory, "Fixtures", fixtureName);
        return File.ReadAllText(path).Trim();
    }
}
