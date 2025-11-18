using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xi.Core.Diff;
using Xi.Core.Rope.Breaks;
using Xi.Core.Search;

namespace Xi.Core.Rope.Diagnostics.Descriptors;

/// <summary>
/// Provides a stable bridge between Stage D descriptor fixtures and the Breaks/Diff/Search skeletons
/// so QA/CLI tooling can consume typed structures without duplicating JSON parsing logic.
/// </summary>
public static class StageDDescriptorHydrator
{
    /// <summary>Hydrates BreakPlan instances from the Breaks descriptor payload.</summary>
    public static IReadOnlyList<BreakPlan> LoadBreakPlans(string fixtureDirectory)
    {
        var manifest = LoadManifest(fixtureDirectory);
        if (manifest.BreaksDescriptors.Count == 0)
        {
            return Array.Empty<BreakPlan>();
        }

        return manifest.BreaksDescriptors
            .Select(BreakPlan.FromDescriptor)
            .ToArray();
    }

    /// <summary>Hydrates diff regions (LineHashDiff + DiffRegion/DiffOperation) from the descriptor payload.</summary>
    public static IReadOnlyList<LineHashDiff> LoadDiffRegions(string fixtureDirectory)
    {
        var manifest = LoadManifest(fixtureDirectory);
        if (manifest.DiffRegions.Count == 0)
        {
            return Array.Empty<LineHashDiff>();
        }

        return manifest.DiffRegions
            .Select(LineHashDiff.FromDescriptor)
            .ToArray();
    }

    /// <summary>Hydrates search span samples (hits + span windows) from the descriptor payload.</summary>
    public static IReadOnlyList<SearchResult> LoadSearchSpans(string fixtureDirectory)
    {
        var manifest = LoadManifest(fixtureDirectory);
        if (manifest.SearchSpans.Count == 0)
        {
            return Array.Empty<SearchResult>();
        }

        return manifest.SearchSpans
            .Select(SearchResult.FromDescriptor)
            .ToArray();
    }

    private static StageDDescriptorManifest LoadManifest(string fixtureDirectory)
    {
        var resolvedDirectory = ResolveFixtureDirectory(fixtureDirectory);
        return StageDDescriptorLoader.LoadFromFixtureDirectory(resolvedDirectory);
    }

    private static string ResolveFixtureDirectory(string fixtureDirectory)
    {
        if (string.IsNullOrWhiteSpace(fixtureDirectory))
        {
            throw new ArgumentException("Fixture directory cannot be null or whitespace.", nameof(fixtureDirectory));
        }

        var resolvedPath = Path.GetFullPath(fixtureDirectory);
        if (!Directory.Exists(resolvedPath))
        {
            throw new DirectoryNotFoundException(
                $"Stage D fixture directory '{resolvedPath}' was not found. " +
                "Run 'python scripts/refresh_all_assets.py --only stage-d-fixtures' to regenerate the assets.");
        }

        return resolvedPath;
    }
}
