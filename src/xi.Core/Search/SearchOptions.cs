namespace Xi.Core.Search;

/// <summary>Options shared between Stage D search spans and the future Finder implementation.</summary>
public readonly record struct SearchOptions(
    string Query,
    bool IsRegex,
    string CaseMatching,
    string? RegexOptions);
