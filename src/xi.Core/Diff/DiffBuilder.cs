using System;

namespace Xi.Core.Diff;

/// <summary>Placeholder builder for diff computation.</summary>
public sealed class DiffBuilder
{
    /// <summary>Computes a diff using the requested options.</summary>
    public LineHashDiff ComputeDiff(ReadOnlySpan<char> left, ReadOnlySpan<char> right, DiffOptions options)
    {
        throw new NotImplementedException("DiffBuilder.ComputeDiff will be implemented once the Stage D diff plan is ready.");
    }
}

/// <summary>Options that map Stage D diff metadata into the managed builder.</summary>
public readonly record struct DiffOptions(bool IgnoreWhitespace, bool PreferLineHashes);
