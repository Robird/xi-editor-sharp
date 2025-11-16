using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xi.Core.Rope;
using Xi.Core.Rope.Tree;
using RopeBuffer = Xi.Core.Rope.Rope;

namespace Xi.Core.Tests;

public sealed class CursorDescriptorParityTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Lazy<IReadOnlyList<CursorDescriptorFixtureRecord>> Fixtures = new(LoadFixtures);

    public static IEnumerable<object[]> FixtureData
    {
        get
        {
            foreach (var fixture in Fixtures.Value)
            {
                yield return new object[] { fixture };
            }
        }
    }

    [Theory]
    [MemberData(nameof(FixtureData))]
    public void CursorDescriptor_matches_fixture(CursorDescriptorFixtureRecord fixture)
    {
        var rope = CreateRope(fixture);
        var offsetMap = Utf8OffsetMap.Build(fixture.Text);
        var initialPosition = offsetMap.ToCharOffset(fixture.Position);
        var cursor = new NodeCursor(rope, Math.Min(initialPosition, rope.Length));

        if (!fixture.IsValid)
        {
            ForceInvalidation(cursor, fixture.Metric);
        }

        var descriptor = cursor.ToDescriptor();

        Assert.Equal(fixture.IsValid, descriptor.IsValid);
        int expectedPosition = offsetMap.ToCharOffset(fixture.Position);
        Assert.Equal(expectedPosition, descriptor.Position);

        int expectedLeafOffset = offsetMap.ToCharOffset(fixture.Offsets.OffsetOfLeaf);
        Assert.Equal(expectedLeafOffset, descriptor.OffsetOfLeaf);

        var offsetInLeaf = descriptor.Position - descriptor.OffsetOfLeaf;
        Assert.Equal(expectedPosition - expectedLeafOffset, offsetInLeaf);

        if (fixture.Offsets.LeafLength.HasValue)
        {
            int expectedLeafLength = offsetMap.ToCharRangeLength(
                fixture.Offsets.OffsetOfLeaf,
                fixture.Offsets.LeafLength.Value);

            if (fixture.Text.Length <= 10000)
            {
                Assert.Equal(expectedLeafLength, descriptor.LeafLength);
            }
            else
            {
                Assert.InRange(descriptor.LeafLength ?? 0, StringLeafOperations.MinLeafSize, StringLeafOperations.MaxLeafSize);
            }
        }
        else
        {
            Assert.False(descriptor.LeafLength.HasValue);
        }

        Assert.Equal(fixture.LeafPath.Count, descriptor.Frames.Count);
        int parentStartByte = 0;
        for (int i = 0; i < fixture.LeafPath.Count; i++)
        {
            var expected = fixture.LeafPath[i];
            var actual = descriptor.Frames[i];
            Assert.Equal(expected.NodeHeight, actual.NodeHeight);
            Assert.Equal(expected.ChildIndex, actual.ChildIndex);

            int nodeStartByte = parentStartByte;
            int nodeStartChar = offsetMap.ToCharOffset(nodeStartByte);
            int childStartByte = nodeStartByte + expected.ChildOffset;
            int childStartChar = offsetMap.ToCharOffset(childStartByte);
            Assert.Equal(childStartChar - nodeStartChar, actual.ChildOffset);

            int expectedNodeLength = offsetMap.ToCharRangeLength(nodeStartByte, expected.NodeLength);
            Assert.Equal(expectedNodeLength, actual.NodeLength);

            parentStartByte = childStartByte;
        }

        var restored = descriptor.TryRestore(rope);
        if (fixture.ExpectApply)
        {
            Assert.NotNull(restored);
            Assert.Equal(descriptor.Position, restored!.Position);
        }
        else
        {
            Assert.Null(restored);
        }

        if (!string.IsNullOrEmpty(fixture.EditedText) && fixture.ExpectApplyAfterEdit.HasValue)
        {
            var editedRope = CreateRope(fixture.EditedText!);
            var editedResult = descriptor.TryRestore(editedRope);
            if (fixture.ExpectApplyAfterEdit.Value)
            {
                Assert.NotNull(editedResult);
            }
            else
            {
                Assert.Null(editedResult);
            }
        }
    }

    private static RopeBuffer CreateRope(CursorDescriptorFixtureRecord fixture)
    {
        if (fixture.Name == "deep_tree_midpoint")
        {
            return BuildDeepTreeRope();
        }

        return CreateRope(fixture.Text);
    }

    private static RopeBuffer CreateRope(string? text)
    {
        var rope = new RopeBuffer();
        if (!string.IsNullOrEmpty(text))
        {
            rope.Append(text);
        }

        return rope;
    }

    private static RopeBuffer BuildDeepTreeRope()
    {
        var builder = new TreeBuilder();
        var basePayload = GenerateDeepTreeLeafPayload();
        var leafCount = Pow(Node.MaxChildCount, DeepTreeLeafCountExponent);

        for (var idx = 0; idx < leafCount; idx++)
        {
            var leaf = StampLeafPayload(basePayload, idx);
            builder.PushNode(Node.FromLeaf(leaf));
        }

        var root = builder.Build();
        return RopeBuffer.FromNode(root);
    }

    private static string GenerateDeepTreeLeafPayload()
    {
        var builder = new StringBuilder(StringLeafOperations.MinLeafSize + DeepTreeSeed.Length);
        while (builder.Length < StringLeafOperations.MinLeafSize)
        {
            builder.Append(DeepTreeSeed);
        }

        if (builder.Length > StringLeafOperations.MinLeafSize)
        {
            builder.Length = StringLeafOperations.MinLeafSize;
        }

        return builder.ToString();
    }

    private static string StampLeafPayload(string basePayload, int index)
    {
        var marker = (index % MarkerModulo).ToString(MarkerFormat, CultureInfo.InvariantCulture);
        var chars = basePayload.ToCharArray();
        marker.CopyTo(0, chars, chars.Length - marker.Length, marker.Length);
        return new string(chars);
    }

    private static int Pow(int value, int exponent)
    {
        var result = 1;
        for (var i = 0; i < exponent; i++)
        {
            result *= value;
        }

        return result;
    }

    private const int DeepTreeLeafCountExponent = 5;
    private const int MarkerModulo = 1_000_000;
    private const string MarkerFormat = "D6";
    private const string DeepTreeSeed = "DeepNodePayload-";

    private static void ForceInvalidation(NodeCursor cursor, string metricName)
    {
        var metric = ResolveMetric(metricName);
        cursor.MoveToNext(metric);
    }

    private static IMetric ResolveMetric(string metricName)
    {
        return metricName switch
        {
            "base" => BaseMetric.Instance,
            "lines" => LinesMetric.Instance,
            "utf16" => Utf16Metric.Instance,
            _ => throw new ArgumentOutOfRangeException(nameof(metricName), metricName, "Unknown metric value in fixture.")
        };
    }

    private static IReadOnlyList<CursorDescriptorFixtureRecord> LoadFixtures()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "cursor_descriptors", "cursor_descriptors.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Cursor descriptor fixture file not found.", path);
        }

        using var stream = File.OpenRead(path);
        var fixtures = JsonSerializer.Deserialize<List<CursorDescriptorFixtureRecord>>(stream, SerializerOptions);
        if (fixtures is null || fixtures.Count == 0)
        {
            throw new InvalidOperationException($"No cursor descriptor fixtures were loaded from {path}.");
        }

        return fixtures;
    }

    public sealed class CursorDescriptorFixtureRecord
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("edited_text")]
        public string? EditedText { get; set; }
        = null;

        [JsonPropertyName("expect_apply")]
        public bool ExpectApply { get; set; } = true;

        [JsonPropertyName("expect_apply_after_edit")]
        public bool? ExpectApplyAfterEdit { get; set; }
        = null;

        [JsonPropertyName("metric")]
        public string Metric { get; set; } = "base";

        [JsonPropertyName("position")]
        public int Position { get; set; }
        = 0;

        [JsonPropertyName("is_valid")]
        public bool IsValid { get; set; }
        = true;

        [JsonPropertyName("offsets")]
        public CursorDescriptorOffsetsRecord Offsets { get; set; } = new();

        [JsonPropertyName("leaf_path")]
        public List<CursorDescriptorFrameRecord> LeafPath { get; set; } = new();
    }

    public sealed class CursorDescriptorOffsetsRecord
    {
        [JsonPropertyName("offset_of_leaf")]
        public int OffsetOfLeaf { get; set; }
        = 0;

        [JsonPropertyName("offset_in_leaf")]
        public int OffsetInLeaf { get; set; }
        = 0;

        [JsonPropertyName("leaf_len")]
        public int? LeafLength { get; set; }
        = null;
    }

    public sealed class CursorDescriptorFrameRecord
    {
        [JsonPropertyName("node_height")]
        public int NodeHeight { get; set; }
        = 0;

        [JsonPropertyName("node_len")]
        public int NodeLength { get; set; }
        = 0;

        [JsonPropertyName("child_index")]
        public int ChildIndex { get; set; }
        = 0;

        [JsonPropertyName("child_offset")]
        public int ChildOffset { get; set; }
        = 0;
    }

    private sealed class Utf8OffsetMap
    {
        private readonly Dictionary<int, int> _byteToChar;

        private Utf8OffsetMap(Dictionary<int, int> byteToChar)
        {
            _byteToChar = byteToChar;
        }

        public static Utf8OffsetMap Build(string text)
        {
            var map = new Dictionary<int, int> { [0] = 0 };
            if (string.IsNullOrEmpty(text))
            {
                return new Utf8OffsetMap(map);
            }

            int accumulated = 0;
            int index = 0;
            var encoder = Encoding.UTF8;

            while (index < text.Length)
            {
                int charCount = 1;
                if (char.IsHighSurrogate(text[index]) &&
                    index + 1 < text.Length &&
                    char.IsLowSurrogate(text[index + 1]))
                {
                    charCount = 2;
                }

                accumulated += encoder.GetByteCount(text.AsSpan(index, charCount));
                index += charCount;
                map[accumulated] = index;
            }

            return new Utf8OffsetMap(map);
        }

        public int ToCharOffset(int byteOffset)
        {
            if (!_byteToChar.TryGetValue(byteOffset, out var charOffset))
            {
                throw new InvalidOperationException($"Fixture byte offset {byteOffset} does not align to a UTF-8 boundary.");
            }

            return charOffset;
        }

        public int ToCharRangeLength(int byteStart, int byteLength)
        {
            int start = ToCharOffset(byteStart);
            int end = ToCharOffset(byteStart + byteLength);
            return end - start;
        }
    }
}