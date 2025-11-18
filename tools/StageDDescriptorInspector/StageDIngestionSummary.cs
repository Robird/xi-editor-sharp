using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xi.Core.Diff;
using Xi.Core.Rope.Breaks;
using Xi.Core.Rope.Diagnostics.Descriptors;
using Xi.Core.Search;

namespace StageDDescriptorInspector;

internal sealed class StageDIngestionSummary
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public string FixtureDirectory { get; init; } = string.Empty;

    public StageDManifestSummary Manifest { get; init; } = new();

    public IReadOnlyList<StageDFixtureLedgerSummary> Ledger { get; init; } = Array.Empty<StageDFixtureLedgerSummary>();

    public BreakPlanSummary? Breaks { get; init; }

    public DiffCaseSummary? Diff { get; init; }

    public SearchCaseSummary? Search { get; init; }

    public static StageDIngestionSummary Create(
        string fixtureDirectory,
        StageDDescriptorManifest manifest,
        StageDCategory categories,
        IReadOnlyList<BreakPlan>? breakPlans,
        IReadOnlyList<LineHashDiff>? diffs,
        IReadOnlyList<SearchResult>? searches)
    {
        return new StageDIngestionSummary
        {
            FixtureDirectory = fixtureDirectory,
            Manifest = StageDManifestSummary.FromMetadata(manifest.Metadata),
            Ledger = manifest.Fixtures.Select(StageDFixtureLedgerSummary.FromEntry).ToArray(),
            Breaks = (categories & StageDCategory.Breaks) != 0 && manifest.BreaksDescriptors.Count > 0
                ? BreakPlanSummary.From(manifest, breakPlans)
                : null,
            Diff = (categories & StageDCategory.Diff) != 0 && manifest.DiffRegions.Count > 0
                ? DiffCaseSummary.From(manifest, diffs)
                : null,
            Search = (categories & StageDCategory.Search) != 0 && manifest.SearchSpans.Count > 0
                ? SearchCaseSummary.From(manifest, searches)
                : null
        };
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, SerializerOptions);
    }

    public void WriteText(TextWriter writer)
    {
        writer.WriteLine("Stage D ingestion summary");
        writer.WriteLine($"Fixture directory : {FixtureDirectory}");
        writer.WriteLine($"Rust commit       : {Manifest.RustCommit ?? "(unknown)"}");
        writer.WriteLine($"CLI revision      : {Manifest.CliRevision ?? "(n/a)"}");
        writer.WriteLine($"Schema version    : {Manifest.SchemaVersion ?? "(n/a)"}");
        writer.WriteLine($"Generated (unix)  : {Manifest.GeneratedAtUnixMillis?.ToString() ?? "(n/a)"}");
        writer.WriteLine($"Feature gates     : {FormatList(Manifest.FeatureGates)}");
        writer.WriteLine($"Chunk descriptors : {Manifest.ChunkDescriptorCount?.ToString() ?? "(n/a)"}");
        writer.WriteLine($"Line descriptors  : {Manifest.LineDescriptorCount?.ToString() ?? "(n/a)"}");
        writer.WriteLine($"Grapheme samples  : {Manifest.GraphemeDescriptorCount?.ToString() ?? "(n/a)"}");
        writer.WriteLine($"Breaks cases      : {Manifest.BreaksDescriptorCount?.ToString() ?? "(n/a)"}");
        writer.WriteLine($"Diff cases        : {Manifest.DiffCaseCount?.ToString() ?? "(n/a)"}");
        writer.WriteLine($"Search cases      : {Manifest.SearchCaseCount?.ToString() ?? "(n/a)"}");

        writer.WriteLine();
        writer.WriteLine("Ledger entries:");
        foreach (var entry in Ledger)
        {
            writer.WriteLine($"- {entry.Name}: count={entry.Count}, schema={entry.SchemaHash}, payload={entry.PayloadHash}");
        }

        if (Breaks is not null)
        {
            writer.WriteLine();
            writer.WriteLine("Break plan samples");
            Breaks.WriteText(writer);
        }

        if (Diff is not null)
        {
            writer.WriteLine();
            writer.WriteLine("Diff region samples");
            Diff.WriteText(writer);
        }

        if (Search is not null)
        {
            writer.WriteLine();
            writer.WriteLine("Search span samples");
            Search.WriteText(writer);
        }
    }

    internal static string FormatList(IReadOnlyList<string>? values)
    {
        return values is { Count: > 0 } ? string.Join(", ", values) : "(none)";
    }
}

internal sealed class StageDManifestSummary
{
    public string? SchemaVersion { get; init; }

    public string? RustCommit { get; init; }

    public string? CliRevision { get; init; }

    public long? GeneratedAtUnixMillis { get; init; }

    public int? ChunkDescriptorCount { get; init; }

    public int? LineDescriptorCount { get; init; }

    public int? GraphemeDescriptorCount { get; init; }

    public int? BreaksDescriptorCount { get; init; }

    public int? DiffCaseCount { get; init; }

    public int? SearchCaseCount { get; init; }

    public IReadOnlyList<string> FeatureGates { get; init; } = Array.Empty<string>();

    public static StageDManifestSummary FromMetadata(StageDDescriptorManifestMetadata metadata)
    {
        return new StageDManifestSummary
        {
            SchemaVersion = metadata.SchemaVersion,
            RustCommit = metadata.RustCommit,
            CliRevision = metadata.CliRevision,
            GeneratedAtUnixMillis = metadata.GeneratedAtUnixMillis,
            ChunkDescriptorCount = metadata.ChunkDescriptorCount,
            LineDescriptorCount = metadata.LineDescriptorCount,
            GraphemeDescriptorCount = metadata.GraphemeDescriptorCount,
            BreaksDescriptorCount = metadata.BreaksDescriptorCount,
            DiffCaseCount = metadata.DiffCaseCount,
            SearchCaseCount = metadata.SearchCaseCount,
            FeatureGates = metadata.FeatureGates is { Count: > 0 }
                ? metadata.FeatureGates.ToArray()
                : Array.Empty<string>()
        };
    }
}

internal sealed record StageDFixtureLedgerSummary(
    string Name,
    string? Path,
    int Count,
    string SchemaHash,
    string PayloadHash)
{
    public static StageDFixtureLedgerSummary FromEntry(StageDFixtureLedgerEntry entry)
    {
        return new StageDFixtureLedgerSummary(
            entry.Name,
            entry.Path,
            entry.Count,
            entry.SchemaHash,
            entry.PayloadHash);
    }
}

internal sealed class BreakPlanSummary
{
    public int DescriptorCount { get; init; }

    public int? ManifestCount { get; init; }

    public int HydratedCount { get; init; }

    public IReadOnlyList<string> SampleNames { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Metrics { get; init; } = Array.Empty<string>();

    public int? MinWrapWidth { get; init; }

    public int? MaxWrapWidth { get; init; }

    public static BreakPlanSummary From(StageDDescriptorManifest manifest, IReadOnlyList<BreakPlan>? plans)
    {
        var samples = manifest.BreaksDescriptors
            .Select(descriptor => descriptor.Sample)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var metrics = manifest.BreaksDescriptors
            .Select(descriptor => descriptor.Metric)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(metric => metric, StringComparer.Ordinal)
            .ToArray();
        var wrapWidths = manifest.BreaksDescriptors
            .Select(descriptor => descriptor.WrapWidthUnits)
            .OrderBy(width => width)
            .ToArray();

        return new BreakPlanSummary
        {
            DescriptorCount = manifest.BreaksDescriptors.Count,
            ManifestCount = manifest.Metadata.BreaksDescriptorCount,
            HydratedCount = plans?.Count ?? 0,
            SampleNames = samples,
            Metrics = metrics,
            MinWrapWidth = wrapWidths.Length > 0 ? wrapWidths.First() : null,
            MaxWrapWidth = wrapWidths.Length > 0 ? wrapWidths.Last() : null
        };
    }

    public void WriteText(TextWriter writer)
    {
        writer.WriteLine($"  Manifest count  : {ManifestCount?.ToString() ?? "(n/a)"}");
        writer.WriteLine($"  Descriptor load : {DescriptorCount}");
        writer.WriteLine($"  Hydrated count  : {HydratedCount}");
        if (MinWrapWidth.HasValue && MaxWrapWidth.HasValue)
        {
            writer.WriteLine($"  Wrap width span : {MinWrapWidth}–{MaxWrapWidth}");
        }

        writer.WriteLine($"  Metrics         : {StageDIngestionSummary.FormatList(Metrics)}");
        writer.WriteLine($"  Samples         : {StageDIngestionSummary.FormatList(SampleNames)}");
    }
}

internal sealed class DiffCaseSummary
{
    public int DescriptorCount { get; init; }

    public int? ManifestCount { get; init; }

    public int HydratedCount { get; init; }

    public IReadOnlyList<string> SampleNames { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Algorithms { get; init; } = Array.Empty<string>();

    public IReadOnlyList<int> OperationCounts { get; init; } = Array.Empty<int>();

    public static DiffCaseSummary From(StageDDescriptorManifest manifest, IReadOnlyList<LineHashDiff>? diffs)
    {
        var samples = manifest.DiffRegions
            .Select(region => region.Sample)
            .OrderBy(sample => sample, StringComparer.Ordinal)
            .ToArray();
        var algorithms = diffs is { Count: > 0 }
            ? diffs.Select(diff => diff.Algorithm)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : Array.Empty<string>();
        var opCounts = manifest.DiffRegions
            .Select(region => region.Ops.Count)
            .ToArray();

        return new DiffCaseSummary
        {
            DescriptorCount = manifest.DiffRegions.Count,
            ManifestCount = manifest.Metadata.DiffCaseCount,
            HydratedCount = diffs?.Count ?? 0,
            SampleNames = samples,
            Algorithms = algorithms,
            OperationCounts = opCounts
        };
    }

    public void WriteText(TextWriter writer)
    {
        writer.WriteLine($"  Manifest count  : {ManifestCount?.ToString() ?? "(n/a)"}");
        writer.WriteLine($"  Descriptor load : {DescriptorCount}");
        writer.WriteLine($"  Hydrated count  : {HydratedCount}");
        writer.WriteLine($"  Algorithms      : {StageDIngestionSummary.FormatList(Algorithms)}");
        if (OperationCounts.Count > 0)
        {
            var summary = string.Join(", ", OperationCounts.Select((count, index) => $"case{index + 1}={count}"));
            writer.WriteLine($"  Ops per case    : {summary}");
        }
        writer.WriteLine($"  Samples         : {StageDIngestionSummary.FormatList(SampleNames)}");
    }
}

internal sealed class SearchCaseSummary
{
    public int DescriptorCount { get; init; }

    public int? ManifestCount { get; init; }

    public int HydratedCount { get; init; }

    public IReadOnlyList<string> SampleNames { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Queries { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> CaseModes { get; init; } = Array.Empty<string>();

    public int RegexCaseCount { get; init; }

    public static SearchCaseSummary From(StageDDescriptorManifest manifest, IReadOnlyList<SearchResult>? searches)
    {
        var sampleNames = manifest.SearchSpans
            .Select(span => span.Sample)
            .OrderBy(sample => sample, StringComparer.Ordinal)
            .ToArray();
        var queries = manifest.SearchSpans
            .Select(span => span.Query)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(query => query, StringComparer.Ordinal)
            .ToArray();
        var caseModes = manifest.SearchSpans
            .Select(span => span.CaseMatching)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(mode => mode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var regexCount = manifest.SearchSpans.Count(span => span.IsRegex);

        return new SearchCaseSummary
        {
            DescriptorCount = manifest.SearchSpans.Count,
            ManifestCount = manifest.Metadata.SearchCaseCount,
            HydratedCount = searches?.Count ?? 0,
            SampleNames = sampleNames,
            Queries = queries,
            CaseModes = caseModes,
            RegexCaseCount = regexCount
        };
    }

    public void WriteText(TextWriter writer)
    {
        writer.WriteLine($"  Manifest count  : {ManifestCount?.ToString() ?? "(n/a)"}");
        writer.WriteLine($"  Descriptor load : {DescriptorCount}");
        writer.WriteLine($"  Hydrated count  : {HydratedCount}");
        writer.WriteLine($"  Regex samples   : {RegexCaseCount}");
        writer.WriteLine($"  Case modes      : {StageDIngestionSummary.FormatList(CaseModes)}");
        writer.WriteLine($"  Queries         : {StageDIngestionSummary.FormatList(Queries)}");
        writer.WriteLine($"  Samples         : {StageDIngestionSummary.FormatList(SampleNames)}");
    }
}
