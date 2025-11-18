using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Xi.Core.Rope;
using Xi.Core.Rope.Diagnostics.Descriptors;

const int TargetBytes = 1_048_576; // 1 MB baseline
const string ChunkLedgerName = "chunk_descriptors.json";

var options = ChunkBenchOptions.Parse(args);

if (options.UseStageD)
{
    RunStageDMode(options);
}
else
{
    RunSyntheticMode();
}

static void RunSyntheticMode()
{
    Console.WriteLine($"Building synthetic {TargetBytes:N0} byte payload...");
    var payload = BuildPayload(TargetBytes);
    var rope = new Rope();
    rope.Append(payload);

    var chunkDiagnostics = new RopeChunkEnumeratorDiagnostics();
    var chunkResult = Measure(() => EnumerateChunks(rope, chunkDiagnostics));
    var lineResult = Measure(() => EnumerateLines(rope));

    Console.WriteLine();
    Console.WriteLine("Chunk enumeration diagnostics:");
    Console.WriteLine($"  Chunks     : {chunkDiagnostics.ChunkCount}");
    Console.WriteLine($"  Max length : {chunkDiagnostics.MaxChunkLength}");
    Console.WriteLine($"  Total chars: {chunkDiagnostics.TotalUtf16CharCount:N0}");
    Console.WriteLine($"  Duration   : {chunkResult.Elapsed.TotalMilliseconds:F2} ms");
    Console.WriteLine();
    Console.WriteLine("Line enumeration diagnostics:");
    Console.WriteLine($"  Lines      : {lineResult.Result:N0}");
    Console.WriteLine($"  Duration   : {lineResult.Elapsed.TotalMilliseconds:F2} ms");
}

static void RunStageDMode(ChunkBenchOptions options)
{
    var manifestFullPath = Path.GetFullPath(options.ManifestPath);
    var fixtureDirectory = Directory.Exists(manifestFullPath)
        ? manifestFullPath
        : Path.GetDirectoryName(manifestFullPath)
            ?? throw new InvalidOperationException(
                $"Unable to determine Stage D fixture directory from '{options.ManifestPath}'.");

    var manifestPath = Directory.Exists(manifestFullPath)
        ? Path.Combine(fixtureDirectory, Path.GetFileName(ChunkBenchDefaults.ManifestPath))
        : manifestFullPath;

    if (!File.Exists(manifestPath))
    {
        throw new FileNotFoundException(
            $"Stage D manifest was not found at '{manifestPath}'. " +
            "Ensure the fixtures directory contains fixtures.manifest.json.",
            manifestPath);
    }

    var manifest = StageDDescriptorLoader.LoadFromFixtureDirectory(fixtureDirectory);
    var reportWriter = new ReportWriter(Path.GetFullPath(options.ReportPath));

    reportWriter.WriteLine("Stage D chunk benchmark (chunk/line replay)");
    reportWriter.WriteLine($"Timestamp      : {DateTimeOffset.UtcNow:O}");
    reportWriter.WriteLine($"Fixture folder : {fixtureDirectory}");
    reportWriter.WriteLine($"Manifest path  : {manifestPath}");
    reportWriter.WriteLine($"Rust commit    : {manifest.Metadata.RustCommit}");
    reportWriter.WriteLine($"Schema version : {manifest.Metadata.SchemaVersion}");
    reportWriter.WriteLine($"CLI revision   : {manifest.Metadata.CliRevision ?? "(n/a)"}");
    reportWriter.WriteLine($"Feature gates  : {FormatFeatureGates(manifest.Metadata.FeatureGates)}");
    reportWriter.WriteLine($"Generated at   : {FormatGeneratedTimestamp(manifest.Metadata.GeneratedAtUnixMillis)}");

    var chunkLedger = manifest.Fixtures
        .FirstOrDefault(entry => string.Equals(entry.Name, ChunkLedgerName, StringComparison.OrdinalIgnoreCase));
    if (chunkLedger is not null)
    {
        reportWriter.WriteLine(
            $"Chunk ledger   : count={chunkLedger.Count}, schema_hash={chunkLedger.SchemaHash}, payload_hash={chunkLedger.PayloadHash}");
    }
    else
    {
        reportWriter.WriteLine("Chunk ledger   : (missing in manifest)");
    }

    reportWriter.WriteLine("Inspector note : tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt");
    reportWriter.WriteLine();

    AllocationSnapshot? startSnapshot = null;
    if (options.IncludeAllocationStats)
    {
        startSnapshot = AllocationSnapshot.Capture();
    }

    var rope = BuildRopeFromStageD(manifest.ChunkDescriptors);
    var chunkDiagnostics = new RopeChunkEnumeratorDiagnostics();
    var chunkResult = Measure(() => EnumerateChunks(rope, chunkDiagnostics));
    var lineResult = Measure(() => EnumerateLines(rope));

    reportWriter.WriteLine("Chunk enumeration diagnostics:");
    reportWriter.WriteLine($"  Rope length  : {rope.Length:N0} UTF-16 chars");
    reportWriter.WriteLine($"  Chunk samples: {manifest.ChunkDescriptors.Count:N0}");
    reportWriter.WriteLine($"  Chunks       : {chunkDiagnostics.ChunkCount:N0}");
    reportWriter.WriteLine($"  Max length   : {chunkDiagnostics.MaxChunkLength:N0}");
    reportWriter.WriteLine($"  Total chars  : {chunkDiagnostics.TotalUtf16CharCount:N0}");
    reportWriter.WriteLine($"  Duration     : {chunkResult.Elapsed.TotalMilliseconds:F2} ms");
    var chunkThroughput = ComputeThroughputMbPerSec(TargetBytes, chunkResult.Elapsed);
    reportWriter.WriteLine($"  Throughput   : {chunkThroughput:F2} MB/s");
    reportWriter.WriteLine();
    reportWriter.WriteLine("Line enumeration diagnostics:");
    reportWriter.WriteLine($"  Manifest lines: {manifest.LineDescriptors.Count:N0}");
    reportWriter.WriteLine($"  Lines visited : {lineResult.Result:N0}");
    reportWriter.WriteLine($"  Duration      : {lineResult.Elapsed.TotalMilliseconds:F2} ms");
    var lineThroughput = ComputeThroughputMbPerSec(TargetBytes, lineResult.Elapsed);
    reportWriter.WriteLine($"  Throughput    : {lineThroughput:F2} MB/s");
    reportWriter.WriteLine();

    if (options.IncludeAllocationStats && startSnapshot is AllocationSnapshot baseline)
    {
        var delta = AllocationSnapshot.Capture().DeltaFrom(baseline);
        reportWriter.WriteLine("Allocation statistics:");
        reportWriter.WriteLine($"  Thread alloc : {delta.AllocatedBytes:N0} bytes");
        reportWriter.WriteLine($"  GC gen0      : {delta.Gen0Collections}");
        reportWriter.WriteLine($"  GC gen1      : {delta.Gen1Collections}");
        reportWriter.WriteLine($"  GC gen2      : {delta.Gen2Collections}");
        reportWriter.WriteLine();
    }

    WriteTopSamples(reportWriter, manifest.ChunkDescriptors);
    reportWriter.WriteLine();
    WriteSampleSummaries(reportWriter, manifest.ChunkDescriptors);

    reportWriter.Flush();
}

static void WriteTopSamples(ReportWriter writer, IList<ChunkDescriptor> descriptors)
{
    writer.WriteLine("Top 3 chunk samples by UTF-16 length:");
    var topThree = descriptors
        .OrderByDescending(d => d.Text?.Length ?? 0)
        .ThenBy(d => d.Sample, StringComparer.Ordinal)
        .ThenBy(d => d.ChunkIndex)
        .Take(3)
        .ToList();

    if (topThree.Count == 0)
    {
        writer.WriteLine("  (no samples)");
        return;
    }

    for (var i = 0; i < topThree.Count; i++)
    {
        var descriptor = topThree[i];
        writer.WriteLine(
            $"  {i + 1}. {FormatSampleHeader(descriptor)} len={descriptor.Text.Length} utf16={FormatRange(descriptor.Utf16Range)} crlf={(descriptor.ContainsCrlf ? "Y" : "N")} tags={FormatTags(descriptor.Tags)}");
    }
}

static void WriteSampleSummaries(ReportWriter writer, IList<ChunkDescriptor> descriptors)
{
    writer.WriteLine("Per-sample chunk summary:");
    if (descriptors.Count == 0)
    {
        writer.WriteLine("  (no samples in manifest)");
        return;
    }

    foreach (var descriptor in descriptors)
    {
        writer.WriteLine(
            $"  {FormatSampleHeader(descriptor)} len={descriptor.Text.Length} utf16={FormatRange(descriptor.Utf16Range)} crlf={(descriptor.ContainsCrlf ? "Y" : "N")} empty={(descriptor.IsEmpty ? "Y" : "N")} path={descriptor.Path.Count} tags={FormatTags(descriptor.Tags)}");
    }
}

static string FormatSampleHeader(ChunkDescriptor descriptor)
{
    return $"{descriptor.Sample}#chunk_{descriptor.ChunkIndex}";
}

static string FormatRange(DescriptorRange range)
{
    return $"[{range.Start}, {range.End})";
}

static string FormatTags(IList<string> tags)
{
    return tags is { Count: > 0 }
        ? string.Join(',', tags)
        : "(none)";
}

static string FormatFeatureGates(IList<string> gates)
{
    return gates is { Count: > 0 }
        ? string.Join(',', gates)
        : "(none)";
}

static string FormatGeneratedTimestamp(long? unixMillis)
{
    if (unixMillis is null)
    {
        return "(n/a)";
    }

    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(unixMillis.Value).ToUniversalTime();
    return $"{timestamp:O} ({unixMillis.Value})";
}

static Rope BuildRopeFromStageD(IList<ChunkDescriptor> descriptors)
{
    var rope = new Rope();
    foreach (var descriptor in descriptors)
    {
        rope.Append(descriptor.Text);
    }

    return rope;
}

static BenchmarkResult<long> Measure(Func<long> action)
{
    var stopwatch = Stopwatch.StartNew();
    var result = action();
    stopwatch.Stop();
    return new BenchmarkResult<long>(result, stopwatch.Elapsed);
}

static long EnumerateChunks(Rope rope, RopeChunkEnumeratorDiagnostics diagnostics)
{
    long consumed = 0;
    foreach (var chunk in rope.EnumerateChunks(diagnostics))
    {
        consumed += chunk.Length;
    }

    return consumed;
}

static long EnumerateLines(Rope rope)
{
    long lines = 0;
    foreach (var line in rope.EnumerateLines())
    {
        if (line.Length == 0 && rope.Length == 0)
        {
            continue;
        }

        lines++;
    }

    return lines;
}

static double ComputeThroughputMbPerSec(long bytesProcessed, TimeSpan elapsed)
{
    if (bytesProcessed <= 0 || elapsed.TotalSeconds <= 0)
    {
        return 0;
    }

    const double BytesPerMegabyte = 1024d * 1024d;
    var megabytes = bytesProcessed / BytesPerMegabyte;
    return megabytes / elapsed.TotalSeconds;
}

static string BuildPayload(int minimumBytes)
{
    const string Seed = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.\r\n";
    var builder = new StringBuilder(minimumBytes + Seed.Length);
    while (builder.Length < minimumBytes)
    {
        builder.Append(Seed);
    }

    return builder.ToString();
}

internal readonly record struct BenchmarkResult<T>(T Result, TimeSpan Elapsed);

internal sealed class ChunkBenchOptions
{
    public bool UseStageD { get; private set; }

    public string ManifestPath { get; private set; } = ChunkBenchDefaults.ManifestPath;

    public string ReportPath { get; private set; } = ChunkBenchDefaults.ReportPath;

    public bool IncludeAllocationStats { get; private set; }

    public static ChunkBenchOptions Parse(string[] args)
    {
        var options = new ChunkBenchOptions();
        for (var i = 0; i < args.Length; i++)
        {
            var argument = args[i];
            if (MatchesFlag(argument, "--stage-d", out var flagValue))
            {
                if (flagValue is not null)
                {
                    options.UseStageD = ParseBoolean(flagValue);
                }
                else if (TryReadValue(i, args, out var nextValue) && IsBooleanLiteral(nextValue))
                {
                    options.UseStageD = ParseBoolean(nextValue);
                    i++;
                }
                else
                {
                    options.UseStageD = true;
                }

                continue;
            }

            if (MatchesFlag(argument, "--manifest", out var manifestValue))
            {
                options.ManifestPath = manifestValue ?? ReadRequiredValue("--manifest", ref i, args);
                continue;
            }

            if (MatchesFlag(argument, "--report", out var reportValue))
            {
                options.ReportPath = reportValue ?? ReadRequiredValue("--report", ref i, args);
                continue;
            }

            if (MatchesFlag(argument, "--include-alloc-stats", out var allocValue))
            {
                if (allocValue is not null)
                {
                    options.IncludeAllocationStats = ParseBoolean(allocValue);
                }
                else if (TryReadValue(i, args, out var nextValue) && IsBooleanLiteral(nextValue))
                {
                    options.IncludeAllocationStats = ParseBoolean(nextValue);
                    i++;
                }
                else
                {
                    options.IncludeAllocationStats = true;
                }

                continue;
            }
        }

        return options;
    }

    private static bool MatchesFlag(string argument, string flag, out string? value)
    {
        if (string.Equals(argument, flag, StringComparison.OrdinalIgnoreCase))
        {
            value = null;
            return true;
        }

        if (argument.StartsWith(flag + "=", StringComparison.OrdinalIgnoreCase))
        {
            value = argument.Substring(flag.Length + 1);
            return true;
        }

        value = null;
        return false;
    }

    private static string ReadRequiredValue(string flag, ref int index, string[] args)
    {
        if (!TryReadValue(index, args, out var value))
        {
            throw new ArgumentException($"The option '{flag}' requires a value.");
        }

        index++;
        return value;
    }

    private static bool TryReadValue(int index, string[] args, out string value)
    {
        if (index + 1 < args.Length)
        {
            value = args[index + 1];
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool IsBooleanLiteral(string value)
    {
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ParseBoolean(string value)
    {
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}

internal static class ChunkBenchDefaults
{
    public const string ManifestPath = "tests/xi.Core.Tests/Fixtures/fixtures.manifest.json";

    public const string ReportPath = "tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt";
}

internal sealed class ReportWriter
{
    private readonly StringBuilder _buffer = new();
    private readonly string _path;

    public ReportWriter(string path)
    {
        _path = path;
    }

    public void WriteLine(string value = "")
    {
        Console.WriteLine(value);
        _buffer.AppendLine(value);
    }

    public void Flush()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_path, _buffer.ToString(), new UTF8Encoding(false));
    }
}

internal readonly record struct AllocationSnapshot(long AllocatedBytes, int Gen0Collections, int Gen1Collections, int Gen2Collections)
{
    public static AllocationSnapshot Capture()
    {
        return new AllocationSnapshot(
            GC.GetAllocatedBytesForCurrentThread(),
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2));
    }

    public AllocationSnapshot DeltaFrom(AllocationSnapshot baseline)
    {
        return new AllocationSnapshot(
            AllocatedBytes - baseline.AllocatedBytes,
            Gen0Collections - baseline.Gen0Collections,
            Gen1Collections - baseline.Gen1Collections,
            Gen2Collections - baseline.Gen2Collections);
    }
}
