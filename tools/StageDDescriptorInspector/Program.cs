using System;
using System.Collections.Generic;
using Xi.Core.Diff;
using Xi.Core.Rope.Breaks;
using Xi.Core.Rope.Diagnostics.Descriptors;
using Xi.Core.Search;

namespace StageDDescriptorInspector;

internal static class Program
{
    public static int Main(string[] args)
    {
        StageDIngestionOptions options;
        try
        {
            options = StageDIngestionOptions.Parse(args);
        }
        catch (Exception ex) when (ex is ArgumentException or DirectoryNotFoundException)
        {
            Console.Error.WriteLine($"[stage-d] {ex.Message}");
            StageDIngestionOptions.WriteUsage(Console.Error);
            return 1;
        }

        if (options.ShowHelp)
        {
            StageDIngestionOptions.WriteUsage(Console.Out);
            return 0;
        }

        StageDDescriptorManifest manifest;
        try
        {
            manifest = StageDDescriptorLoader.LoadFromFixtureDirectory(options.FixtureDirectory);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[stage-d] {ex.Message}");
            return 1;
        }

        IReadOnlyList<BreakPlan>? breakPlans = null;
        IReadOnlyList<LineHashDiff>? diffRegions = null;
        IReadOnlyList<SearchResult>? searchSpans = null;

        try
        {
            if (options.ShouldLoad(StageDCategory.Breaks))
            {
                breakPlans = StageDDescriptorHydrator.LoadBreakPlans(options.FixtureDirectory);
            }

            if (options.ShouldLoad(StageDCategory.Diff))
            {
                diffRegions = StageDDescriptorHydrator.LoadDiffRegions(options.FixtureDirectory);
            }

            if (options.ShouldLoad(StageDCategory.Search))
            {
                searchSpans = StageDDescriptorHydrator.LoadSearchSpans(options.FixtureDirectory);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[stage-d] Failed to hydrate descriptor payloads: {ex.Message}");
            return 1;
        }

        var summary = StageDIngestionSummary.Create(
            options.FixtureDirectory,
            manifest,
            options.Categories,
            breakPlans,
            diffRegions,
            searchSpans);

        if (options.OutputFormat == StageDOutputFormat.Json)
        {
            Console.WriteLine(summary.ToJson());
        }
        else
        {
            summary.WriteText(Console.Out);
        }

        return 0;
    }
}
