using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using StageDDescriptorInspector;
using Xunit;

namespace Xi.Core.Tests.Diagnostics;

public sealed class StageDDescriptorInspectorTests
{
    private static readonly string FixtureRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures"));

    [Fact]
    public void Inspector_outputs_text_summary()
    {
        var args = new[]
        {
            "--fixtures",
            FixtureRoot,
            "--categories",
            "breaks,diff,search"
        };

        var exitCode = ExecuteInspector(args, out var output, out var errorOutput);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(errorOutput), $"Inspector wrote to stderr: {errorOutput}");

        Assert.Contains("Stage D ingestion summary", output);
        Assert.Contains("Break plan samples", output);
        Assert.Contains("bea8a3360131a0840d26aa75daba9184b70d50b7", output);
        Assert.Contains("Feature gates", output);
        Assert.Contains("serde", output);
    }

    [Fact]
    public void Inspector_outputs_json_when_requested()
    {
        var args = new[]
        {
            "--fixtures",
            FixtureRoot,
            "--format",
            "json",
            "--categories",
            "breaks"
        };

        var exitCode = ExecuteInspector(args, out var output, out var errorOutput);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(errorOutput), $"Inspector wrote to stderr: {errorOutput}");

        using var document = JsonDocument.Parse(output);
        var root = document.RootElement;

        var manifest = root.GetProperty("Manifest");
        Assert.Equal("bea8a3360131a0840d26aa75daba9184b70d50b7", manifest.GetProperty("RustCommit").GetString());
        var featureGates = manifest
            .GetProperty("FeatureGates")
            .EnumerateArray()
            .Select(property => property.GetString())
            .Where(value => value is not null)
            .Select(value => value!);
        Assert.Contains("serde", featureGates);

        var breaks = root.GetProperty("Breaks");
        Assert.Equal(3, breaks.GetProperty("HydratedCount").GetInt32());

        Assert.Equal(JsonValueKind.Null, root.GetProperty("Diff").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("Search").ValueKind);
    }

    private static int ExecuteInspector(string[] args, out string standardOutput, out string standardError)
    {
        var originalOut = Console.Out;
        var originalErr = Console.Error;
        var outputWriter = new StringWriter();
        var errorWriter = new StringWriter();

        try
        {
            Console.SetOut(outputWriter);
            Console.SetError(errorWriter);
            return Program.Main(args);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
            standardOutput = outputWriter.ToString();
            standardError = errorWriter.ToString();
        }
    }
}
