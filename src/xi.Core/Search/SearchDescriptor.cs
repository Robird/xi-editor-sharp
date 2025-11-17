using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Xi.Core.Rope.StageD;

namespace Xi.Core.Search;

/// <summary>
/// Represents the payload exported by <c>--search-spans</c> for Stage D parity.
/// </summary>
public sealed class SearchSpansDocument
{
    [JsonPropertyName("metadata")]
    public SearchSpansMetadata Metadata { get; set; } = new();

    [JsonPropertyName("search_cases")]
    public IList<SearchCaseDescriptor> SearchCases { get; set; } = new List<SearchCaseDescriptor>();
}

/// <summary>Metadata describing the batch of search spans.</summary>
public sealed class SearchSpansMetadata
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = string.Empty;

    [JsonPropertyName("rust_commit")]
    public string? RustCommit { get; set; }

    [JsonPropertyName("generated_at_unix_millis")]
    public long GeneratedAtUnixMillis { get; set; }

    [JsonPropertyName("case_count")]
    public int CaseCount { get; set; }
}

/// <summary>Single search sample with hits + span windows.</summary>
public sealed class SearchCaseDescriptor
{
    [JsonPropertyName("sample")]
    public string Sample { get; set; } = string.Empty;

    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("is_regex")]
    public bool IsRegex { get; set; }

    [JsonPropertyName("regex_options")]
    public string? RegexOptions { get; set; }

    [JsonPropertyName("case_matching")]
    public string CaseMatching { get; set; } = string.Empty;

    [JsonPropertyName("text_len")]
    public int TextLength { get; set; }

    [JsonPropertyName("hits")]
    public IList<SearchHitDescriptor> Hits { get; set; } = new List<SearchHitDescriptor>();

    [JsonPropertyName("span_windows")]
    public IList<SearchSpanSegment> SpanWindows { get; set; } = new List<SearchSpanSegment>();

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

/// <summary>Represents a single search hit.</summary>
public sealed class SearchHitDescriptor
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("range")]
    public RangeSnapshot Range { get; set; } = new();

    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("context_before")]
    public string? ContextBefore { get; set; }

    [JsonPropertyName("context_after")]
    public string? ContextAfter { get; set; }
}

/// <summary>Span window emitted by <c>SpansBuilder</c>.</summary>
public sealed class SearchSpanSegment
{
    [JsonPropertyName("range")]
    public RangeSnapshot Range { get; set; } = new();

    [JsonPropertyName("style_id")]
    public int StyleId { get; set; }

    [JsonPropertyName("style_tag")]
    public string StyleTag { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public int Priority { get; set; }
}

/// <summary>Immutable projection for ingestion.</summary>
public readonly record struct SearchCaseDescriptorView(
    string Sample,
    string Query,
    bool IsRegex,
    string? RegexOptions,
    string CaseMatching,
    int TextLength,
    IReadOnlyList<SearchHitView> Hits,
    IReadOnlyList<SearchSpanSegmentView> SpanWindows,
    string? Notes)
{
    public static SearchCaseDescriptorView FromDescriptor(SearchCaseDescriptor descriptor)
    {
        return new SearchCaseDescriptorView(
            descriptor.Sample,
            descriptor.Query,
            descriptor.IsRegex,
            descriptor.RegexOptions,
            descriptor.CaseMatching,
            descriptor.TextLength,
            descriptor.Hits.Select(SearchHitView.FromDescriptor).ToArray(),
            descriptor.SpanWindows.Select(SearchSpanSegmentView.FromDescriptor).ToArray(),
            descriptor.Notes);
    }
}

/// <summary>View for <see cref="SearchHitDescriptor"/>.</summary>
public readonly record struct SearchHitView(
    int Index,
    RangeSnapshot Range,
    int Line,
    string? ContextBefore,
    string? ContextAfter)
{
    public static SearchHitView FromDescriptor(SearchHitDescriptor descriptor)
    {
        return new SearchHitView(
            descriptor.Index,
            descriptor.Range,
            descriptor.Line,
            descriptor.ContextBefore,
            descriptor.ContextAfter);
    }
}

/// <summary>View for <see cref="SearchSpanSegment"/>.</summary>
public readonly record struct SearchSpanSegmentView(RangeSnapshot Range, int StyleId, string StyleTag, int Priority)
{
    public static SearchSpanSegmentView FromDescriptor(SearchSpanSegment segment)
    {
        return new SearchSpanSegmentView(segment.Range, segment.StyleId, segment.StyleTag, segment.Priority);
    }
}
