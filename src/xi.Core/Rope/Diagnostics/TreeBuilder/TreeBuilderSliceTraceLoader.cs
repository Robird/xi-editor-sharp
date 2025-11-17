using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Xi.Core.Rope.Diagnostics.TreeBuilder;

/// <summary>
/// Loads Rust-emitted tree builder slice traces so Stage&nbsp;D diagnostics can inspect stack activity.
/// </summary>
internal static class TreeBuilderSliceTraceLoader
{
    /// <summary>Reads every <c>.json</c> trace under <paramref name="traceDirectory"/>.</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="traceDirectory"/> is null/empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the directory is missing.</exception>
    /// <exception cref="InvalidDataException">Thrown when an individual trace cannot be parsed.</exception>
    public static IReadOnlyList<TreeBuilderSliceTrace> LoadFromDirectory(string traceDirectory)
    {
        if (string.IsNullOrWhiteSpace(traceDirectory))
        {
            throw new ArgumentException("Trace directory cannot be null or whitespace.", nameof(traceDirectory));
        }

        var absolutePath = Path.GetFullPath(traceDirectory);
        if (!Directory.Exists(absolutePath))
        {
            throw new InvalidOperationException(
                $"Tree builder slice trace directory '{absolutePath}' was not found. " +
                "Re-run the Rust exporter with '--features serde,tree_builder_slice_trace --tree-builder-trace tests/xi.Core.Tests/Fixtures/ParityFixtures/tree_builder_trace' so the assets exist.");
        }

        var files = Directory.EnumerateFiles(absolutePath, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(static path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count == 0)
        {
            return Array.Empty<TreeBuilderSliceTrace>();
        }

        var traces = new List<TreeBuilderSliceTrace>(files.Count);
        foreach (var file in files)
        {
            traces.Add(ParseTrace(file));
        }

        return traces;
    }

    private static TreeBuilderSliceTrace ParseTrace(string filePath)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(filePath));
            var root = document.RootElement;

            JsonElement eventsElement;
            JsonElement? metadataElement = null;

            switch (root.ValueKind)
            {
                case JsonValueKind.Array:
                    eventsElement = root;
                    break;
                case JsonValueKind.Object:
                    if (!root.TryGetProperty("events", out eventsElement))
                    {
                        throw new InvalidDataException($"Trace '{filePath}' does not contain an 'events' array.");
                    }

                    if (eventsElement.ValueKind != JsonValueKind.Array)
                    {
                        throw new InvalidDataException($"Trace '{filePath}' has a non-array 'events' payload.");
                    }

                    if (root.TryGetProperty("metadata", out var metadata) && metadata.ValueKind == JsonValueKind.Object)
                    {
                        metadataElement = metadata;
                    }

                    break;
                default:
                    throw new InvalidDataException(
                        $"Trace '{filePath}' must be either a JSON array (events only) or an object with 'metadata'/'events'.");
            }

            var traceMetadata = BuildMetadata(metadataElement, Path.GetFileNameWithoutExtension(filePath));
            var events = ParseEvents(eventsElement);

            return new TreeBuilderSliceTrace
            {
                SourceFile = filePath,
                Metadata = traceMetadata,
                Events = events
            };
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Unable to parse tree builder slice trace '{filePath}'.", ex);
        }
    }

    private static TreeBuilderSliceTraceMetadata BuildMetadata(JsonElement? metadataElement, string fallbackSample)
    {
        var sampleName = fallbackSample;
        var rustCommit = string.Empty;
        var timestamp = 0L;

        if (metadataElement.HasValue)
        {
            sampleName = FirstNonEmptyString(metadataElement.Value, fallbackSample, "sample", "sample_name", "name", "fixture");
            rustCommit = FirstNonEmptyString(metadataElement.Value, string.Empty, "rust_commit", "commit");
            timestamp = FirstInteger(metadataElement.Value, "generated_at_unix_millis", "timestamp", "recorded_at");
        }

        return new TreeBuilderSliceTraceMetadata
        {
            SampleName = string.IsNullOrWhiteSpace(sampleName) ? fallbackSample : sampleName,
            RustCommit = rustCommit ?? string.Empty,
            TimestampUnixMillis = timestamp
        };
    }

    private static IReadOnlyList<TreeBuilderSliceTraceEvent> ParseEvents(JsonElement arrayElement)
    {
        if (arrayElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("Tree builder slice trace events payload must be an array.");
        }

        var events = new List<TreeBuilderSliceTraceEvent>(arrayElement.GetArrayLength());
        foreach (var eventElement in arrayElement.EnumerateArray())
        {
            events.Add(ParseEvent(eventElement));
        }

        return events;
    }

    private static TreeBuilderSliceTraceEvent ParseEvent(JsonElement element)
    {
        if (!element.TryGetProperty("kind", out var kindProperty) ||
            kindProperty.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(kindProperty.GetString()))
        {
            throw new InvalidDataException("Tree builder slice trace event is missing a 'kind' string field.");
        }

        var kindValue = kindProperty.GetString() ?? string.Empty;
        if (!Enum.TryParse(kindValue, ignoreCase: false, out TreeBuilderSliceTraceEventKind kind))
        {
            throw new InvalidDataException($"Tree builder slice trace event kind '{kindValue}' is not recognized.");
        }

        return new TreeBuilderSliceTraceEvent
        {
            Kind = kind,
            Depth = GetInt32(element, "depth"),
            NodeHeight = GetInt32(element, "node_height"),
            NodeLength = GetInt32(element, "node_len"),
            NodeId = GetUInt64(element, "node_id"),
            Reuse = GetBoolean(element, "reuse"),
            MergedChildren = GetNullableInt32(element, "merged_children"),
            Interval = ReadInterval(element, "interval"),
            Requested = ReadInterval(element, "requested"),
            Translated = ReadInterval(element, "translated")
        };
    }

    private static TreeBuilderSliceTraceInterval? ReadInterval(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var intervalElement) || intervalElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new TreeBuilderSliceTraceInterval
        {
            Start = GetInt32(intervalElement, "start"),
            End = GetInt32(intervalElement, "end")
        };
    }

    private static string FirstNonEmptyString(JsonElement element, string fallback, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                var candidate = value.GetString();
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    return candidate!;
                }
            }
        }

        return fallback;
    }

    private static long FirstInteger(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return 0;
    }

    private static int GetInt32(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return 0;
    }

    private static int? GetNullableInt32(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static ulong GetUInt64(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetUInt64(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String && ulong.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return 0;
    }

    private static bool GetBoolean(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (value.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return false;
    }
}

internal sealed record class TreeBuilderSliceTrace
{
    public string SourceFile { get; init; } = string.Empty;

    public TreeBuilderSliceTraceMetadata Metadata { get; init; } = new();

    public IReadOnlyList<TreeBuilderSliceTraceEvent> Events { get; init; } = Array.Empty<TreeBuilderSliceTraceEvent>();
}

internal sealed record class TreeBuilderSliceTraceMetadata
{
    public string SampleName { get; init; } = string.Empty;

    public string RustCommit { get; init; } = string.Empty;

    public long TimestampUnixMillis { get; init; }
}

internal sealed record class TreeBuilderSliceTraceEvent
{
    public TreeBuilderSliceTraceEventKind Kind { get; init; }

    public int Depth { get; init; }

    public int NodeHeight { get; init; }

    public int NodeLength { get; init; }

    public ulong NodeId { get; init; }

    public bool Reuse { get; init; }

    public int? MergedChildren { get; init; }

    public TreeBuilderSliceTraceInterval? Interval { get; init; }

    public TreeBuilderSliceTraceInterval? Requested { get; init; }

    public TreeBuilderSliceTraceInterval? Translated { get; init; }
}

internal sealed record class TreeBuilderSliceTraceInterval
{
    public int Start { get; init; }

    public int End { get; init; }
}

internal enum TreeBuilderSliceTraceEventKind
{
    PushFrame,
    ExtendFrame,
    MergePop,
    LeafSlice,
    EnterChild
}
