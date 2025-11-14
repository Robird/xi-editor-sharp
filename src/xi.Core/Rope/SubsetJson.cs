using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Xi.Core.Rope;

internal static class SubsetJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
    };

    internal static string Serialize(Subset subset)
    {
        if (subset == null)
        {
            throw new ArgumentNullException(nameof(subset));
        }

        var segmentDtos = new SegmentDto[subset.SegmentCount];
        var index = 0;
        foreach (var triple in subset.SegmentTriples())
        {
            segmentDtos[index++] = new SegmentDto
            {
                Length = triple.Length,
                Count = triple.Count
            };
        }

        var dto = new SubsetDto
        {
            Segments = segmentDtos
        };

        return JsonSerializer.Serialize(dto, SerializerOptions);
    }

    internal static Subset Deserialize(string json)
    {
        if (json == null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        var dto = JsonSerializer.Deserialize<SubsetDto>(json, SerializerOptions)
                  ?? throw new InvalidOperationException("Subset JSON payload could not be deserialized.");

        if (dto.Segments == null || dto.Segments.Length == 0)
        {
            return Subset.Empty;
        }

        var triples = new List<(int Start, int Length, int Count)>(dto.Segments.Length);
        var offset = 0;
        foreach (var segment in dto.Segments)
        {
            var length = segment.Length;
            var count = segment.Count;

            if (length <= 0)
            {
                throw new InvalidOperationException("Segment length must be positive.");
            }

            if (count < 0)
            {
                throw new InvalidOperationException("Segment count must be non-negative.");
            }

            triples.Add((offset, length, count));
            offset += length;
        }

        return Subset.FromSegmentTriples(triples);
    }

    private sealed class SubsetDto
    {
        [JsonPropertyName("segments")]
        public SegmentDto[] Segments { get; set; } = Array.Empty<SegmentDto>();
    }

    private sealed class SegmentDto
    {
        [JsonPropertyName("len")]
        public int Length { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
}
