using System;

namespace Xi.Core.Search;

/// <summary>
/// Placeholder search entry point that mirrors Rust's Finder interface for Stage D search spans.
/// </summary>
public sealed class Finder
{
    /// <summary>Executes a search over the provided text using the given options.</summary>
    public SearchResult Find(ReadOnlySpan<char> text, SearchOptions options)
    {
        throw new NotImplementedException("Finder.Find is not ready until the Stage D search pipeline lands.");
    }

    /// <summary>Convenience helper to surface Stage D descriptors during smoke tests.</summary>
    public SearchResult FromDescriptor(SearchCaseDescriptorView descriptor)
    {
        return SearchResult.FromDescriptor(descriptor);
    }
}
