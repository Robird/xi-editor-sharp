using System;
using System.Collections.Generic;
using System.IO;

namespace StageDDescriptorInspector;

internal sealed class StageDIngestionOptions
{
    private StageDIngestionOptions(
        string fixtureDirectory,
        StageDCategory categories,
        StageDOutputFormat outputFormat,
        bool showHelp)
    {
        FixtureDirectory = fixtureDirectory;
        Categories = categories;
        OutputFormat = outputFormat;
        ShowHelp = showHelp;
    }

    public string FixtureDirectory { get; }

    public StageDCategory Categories { get; }

    public StageDOutputFormat OutputFormat { get; }

    public bool ShowHelp { get; }

    public bool ShouldLoad(StageDCategory category)
    {
        return (Categories & category) == category;
    }

    public static StageDIngestionOptions Parse(string[] args)
    {
        string? fixtureDirectory = null;
        var categories = StageDCategory.All;
        var format = StageDOutputFormat.Text;
        var showHelp = false;

        for (var i = 0; i < args.Length; i++)
        {
            var current = args[i];
            switch (current)
            {
                case "--fixtures":
                case "-f":
                    fixtureDirectory = RequireValue(args, ++i, current);
                    break;
                case "--categories":
                    categories = ParseCategories(RequireValue(args, ++i, current));
                    break;
                case "--format":
                    format = ParseFormat(RequireValue(args, ++i, current));
                    break;
                case "--help":
                case "-h":
                case "/?":
                    showHelp = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{current}'. Use --help for usage instructions.");
            }
        }

        var resolvedDirectory = ResolveFixtureDirectory(fixtureDirectory);

        return new StageDIngestionOptions(resolvedDirectory, categories, format, showHelp);
    }

    private static StageDCategory ParseCategories(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || string.Equals(raw, "all", StringComparison.OrdinalIgnoreCase))
        {
            return StageDCategory.All;
        }

        var result = StageDCategory.None;
        foreach (var token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            result |= token.ToLowerInvariant() switch
            {
                "breaks" => StageDCategory.Breaks,
                "diff" => StageDCategory.Diff,
                "search" => StageDCategory.Search,
                _ => throw new ArgumentException($"Unknown category '{token}'. Expected breaks|diff|search|all.")
            };
        }

        return result == StageDCategory.None ? StageDCategory.All : result;
    }

    private static StageDOutputFormat ParseFormat(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return StageDOutputFormat.Text;
        }

        return raw.ToLowerInvariant() switch
        {
            "text" => StageDOutputFormat.Text,
            "json" => StageDOutputFormat.Json,
            _ => throw new ArgumentException($"Unknown output format '{raw}'. Expected text or json.")
        };
    }

    private static string ResolveFixtureDirectory(string? supplied)
    {
        if (!string.IsNullOrWhiteSpace(supplied))
        {
            var expanded = Environment.ExpandEnvironmentVariables(supplied);
            return Path.GetFullPath(expanded);
        }

        var searchRoot = new DirectoryInfo(Environment.CurrentDirectory);
        while (searchRoot is not null)
        {
            var candidate = Path.Combine(searchRoot.FullName, "tests", "xi.Core.Tests", "Fixtures");
            if (Directory.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }

            searchRoot = searchRoot.Parent;
        }

        throw new DirectoryNotFoundException(
            "Unable to locate tests/xi.Core.Tests/Fixtures. Provide --fixtures <path>.");
    }

    private static string RequireValue(string[] args, int index, string option)
    {
        if (index >= args.Length)
        {
            throw new ArgumentException($"Missing value for '{option}'.");
        }

        return args[index];
    }

    public static void WriteUsage(TextWriter writer)
    {
        writer.WriteLine("StageDDescriptorInspector");
        writer.WriteLine("Usage: dotnet run --project tools/StageDDescriptorInspector -- [options]");
        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  --fixtures <path>     Path to tests/xi.Core.Tests/Fixtures (default: auto-discover).");
        writer.WriteLine("  --categories <list>   Categories to hydrate (breaks,diff,search,all). Default: all.");
        writer.WriteLine("  --format <text|json>  Output format. Default: text.");
        writer.WriteLine("  --help                Show this help message.");
    }
}

[Flags]
internal enum StageDCategory
{
    None = 0,
    Breaks = 1,
    Diff = 2,
    Search = 4,
    All = Breaks | Diff | Search
}

internal enum StageDOutputFormat
{
    Text,
    Json
}
