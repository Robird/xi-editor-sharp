using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Xi.Core.Rope;

internal static class DeltaJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static string Serialize<TInfo>(Delta<TInfo, string> delta)
    {
        if (delta == null)
        {
            throw new ArgumentNullException(nameof(delta));
        }

        var elementDtos = new List<ElementDto>(delta.ElementCount);
        foreach (var element in delta.EnumerateElements())
        {
            if (element.IsCopy)
            {
                var copy = element.AsCopy();
                elementDtos.Add(new ElementDto
                {
                    Copy = new[] { copy.Start, copy.End }
                });
            }
            else
            {
                var insert = element.AsInsert().Value;
                elementDtos.Add(new ElementDto
                {
                    Insert = insert
                });
            }
        }

        var dto = new DeltaDto
        {
            Elements = elementDtos.ToArray(),
            BaseLength = delta.BaseLength
        };

        return JsonSerializer.Serialize(dto, SerializerOptions);
    }

    internal static Delta<TInfo, string> Deserialize<TInfo>(string json)
    {
        if (json == null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        var dto = JsonSerializer.Deserialize<DeltaDto>(json, SerializerOptions)
                  ?? throw new InvalidOperationException("Delta JSON payload could not be deserialized.");

        if (dto.Elements == null)
        {
            throw new InvalidOperationException("Delta JSON payload must include an elements array.");
        }

        if (dto.BaseLength < 0)
        {
            throw new InvalidOperationException("Delta base length must be non-negative.");
        }

        var elements = new List<DeltaElement<string>>(dto.Elements.Length);
        foreach (var elementDto in dto.Elements)
        {
            var hasCopy = elementDto.Copy != null;
            var hasInsert = elementDto.Insert != null;

            if (hasCopy == hasInsert)
            {
                throw new InvalidOperationException("Each delta element must specify exactly one of copy or insert.");
            }

            if (hasCopy)
            {
                var copy = elementDto.Copy!;
                if (copy.Length != 2)
                {
                    throw new InvalidOperationException("Copy elements must provide exactly two offsets.");
                }

                elements.Add(DeltaElement<string>.Copy(copy[0], copy[1]));
            }
            else
            {
                var insertValue = elementDto.Insert ?? throw new InvalidOperationException("Insert value cannot be null.");
                elements.Add(DeltaElement<string>.Insert(insertValue));
            }
        }

        return Delta<TInfo, string>.FromElements(dto.BaseLength, elements);
    }

    private sealed class DeltaDto
    {
        [JsonPropertyName("els")]
        public ElementDto[] Elements { get; set; } = Array.Empty<ElementDto>();

        [JsonPropertyName("base_len")]
        public int BaseLength { get; set; }
    }

    private sealed class ElementDto
    {
        [JsonPropertyName("copy")]
        public int[]? Copy { get; set; }

        [JsonPropertyName("insert")]
        public string? Insert { get; set; }
    }
}
