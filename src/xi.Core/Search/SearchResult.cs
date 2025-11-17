using System;
using System.Collections.Generic;
using System.Linq;

namespace Xi.Core.Search;

/// <summary>Represents the projected result for <c>search_spans@1.0.0</c>.</summary>
public sealed class SearchResult
{
    public SearchResult(
        string sample,
        string query,
        bool isRegex,
        string caseMatching,
        IReadOnlyList<SearchHit> hits,
        IReadOnlyList<SearchSpanWindow> spanWindows,
        string? notes)
    {
        Sample = sample ?? throw new ArgumentNullException(nameof(sample));
        Query = query ?? throw new ArgumentNullException(nameof(query));
        IsRegex = isRegex;
        CaseMatching = caseMatching ?? throw new ArgumentNullException(nameof(caseMatching));
        Hits = hits ?? Array.Empty<SearchHit>();
        SpanWindows = spanWindows ?? Array.Empty<SearchSpanWindow>();
        Notes = notes;
    }

    public string Sample { get; }

    public string Query { get; }

    public bool IsRegex { get; }

    public string CaseMatching { get; }

    public IReadOnlyList<SearchHit> Hits { get; }

    public IReadOnlyList<SearchSpanWindow> SpanWindows { get; }

    public string? Notes { get; }

    public static SearchResult FromDescriptor(SearchCaseDescriptorView descriptor)
    {
        var hits = descriptor.Hits.Select(SearchHit.FromDescriptor).ToArray();
        var spans = descriptor.SpanWindows.Select(SearchSpanWindow.FromDescriptor).ToArray();
        return new SearchResult(
            descriptor.Sample,
            descriptor.Query,
            descriptor.IsRegex,
            descriptor.CaseMatching,
            hits,
            spans,
            descriptor.Notes);
    }
}
