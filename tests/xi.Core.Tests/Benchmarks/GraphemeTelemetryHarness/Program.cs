using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Xi.Core.Rope;
using Xi.Core.Rope.Diagnostics.Descriptors;
using Xi.Core.Rope.Navigation;
using Xi.Core.Rope.Tree;

var options = GraphemeTelemetryOptions.Parse(args);
var fixtureDirectory = Path.GetFullPath(options.FixtureDirectory);
var manifest = StageDDescriptorLoader.LoadFromFixtureDirectory(fixtureDirectory);
if (manifest.GraphemeDescriptors.Count == 0)
{
    throw new InvalidOperationException("Stage D manifest does not expose any grapheme descriptors. Run 'python scripts/refresh_all_assets.py --only stage-d-fixtures' first.");
}

var playbacks = GraphemeSamplePlayback.Build(manifest.GraphemeDescriptors);
var descriptorsPerPass = playbacks.Sum(sample => sample.Descriptors.Count);
if (descriptorsPerPass == 0)
{
    throw new InvalidOperationException("Unable to build a grapheme playback set from manifest descriptors.");
}

var metrics = new GraphemeNavigationMetrics();
var navigator = new DegradedGraphemeNavigator(metrics);
var aggregator = new TelemetryAggregator(descriptorsPerPass, playbacks.Count);

var stopwatch = Stopwatch.StartNew();
while (aggregator.TotalOperations < options.TargetOperations)
{
    aggregator.RecordPassAttempt();
    foreach (var sample in playbacks)
    {
        foreach (var descriptor in sample.Descriptors)
        {
            ReplayDescriptor(sample.Rope, navigator, descriptor, aggregator);
            if (aggregator.TotalOperations >= options.TargetOperations)
            {
                break;
            }
        }

        if (aggregator.TotalOperations >= options.TargetOperations)
        {
            break;
        }
    }
}
stopwatch.Stop();
aggregator.MarkComplete(stopwatch.Elapsed);

var snapshot = metrics.GetSnapshot();
var reportWriter = new ReportWriter(options.ReportPath);
var manifestPath = Path.Combine(fixtureDirectory, GraphemeTelemetryDefaults.ManifestFileName);
var graphemeLedger = manifest.Fixtures.FirstOrDefault(
    entry => string.Equals(entry.Name, GraphemeTelemetryDefaults.GraphemeLedgerName, StringComparison.OrdinalIgnoreCase));
var graphemeWindowCount = ResolveMetricWindowCount(manifest, GraphemeTelemetryDefaults.GraphemeWindowKind);

reportWriter.WriteLine("Stage D grapheme telemetry harness");
reportWriter.WriteLine($"Timestamp      : {DateTimeOffset.UtcNow:O}");
reportWriter.WriteLine($"Fixture folder : {fixtureDirectory}");
reportWriter.WriteLine($"Manifest path  : {manifestPath}");
reportWriter.WriteLine($"Rust commit    : {manifest.Metadata.RustCommit}");
reportWriter.WriteLine($"Schema version : {manifest.Metadata.SchemaVersion}");
reportWriter.WriteLine($"CLI revision   : {manifest.Metadata.CliRevision ?? "(n/a)"}");
reportWriter.WriteLine($"Feature gates  : {FormatFeatureGates(manifest.Metadata.FeatureGates)}");
reportWriter.WriteLine($"Generated at   : {FormatGeneratedTimestamp(manifest.Metadata.GeneratedAtUnixMillis)}");
if (graphemeLedger is not null)
{
    reportWriter.WriteLine(
        $"Grapheme ledger: count={graphemeLedger.Count}, schema_hash={graphemeLedger.SchemaHash}, payload_hash={graphemeLedger.PayloadHash}");
}
else
{
    reportWriter.WriteLine("Grapheme ledger: (missing in manifest)");
}
if (graphemeWindowCount is not null)
{
    reportWriter.WriteLine($"Metric windows : grapheme_windows={graphemeWindowCount}");
}
reportWriter.WriteLine($"Inspector note : {GraphemeTelemetryDefaults.InspectorReport}");
reportWriter.WriteLine();

var totalOperations = snapshot.MoveNextCalls + snapshot.MovePreviousCalls;
var fallbackRatio = totalOperations > 0
    ? snapshot.ScalarFallbacks / (double)totalOperations
    : 0d;
var opsPerSecond = aggregator.Duration.TotalSeconds > 0
    ? totalOperations / aggregator.Duration.TotalSeconds
    : 0d;

reportWriter.WriteLine("Telemetry replay summary:");
reportWriter.WriteLine($"  Target operations : {options.TargetOperations:N0}");
reportWriter.WriteLine($"  Actual operations : {totalOperations:N0}");
reportWriter.WriteLine($"  Descriptor replays: {aggregator.DescriptorReplays:N0}");
reportWriter.WriteLine($"  Pass attempts     : {aggregator.PassAttempts:N0}");
reportWriter.WriteLine($"  Samples per pass  : {aggregator.DescriptorsPerPass:N0} descriptors across {aggregator.UniqueSamples:N0} samples");
reportWriter.WriteLine($"  Requires fallback : {aggregator.RequiresFallbackDescriptors:N0} descriptors ({aggregator.RequiresFallbackDescriptors / (double)Math.Max(1, aggregator.DescriptorReplays):P4})");
reportWriter.WriteLine($"  Cross-leaf cases  : {aggregator.CrossLeafDescriptors:N0}");
reportWriter.WriteLine($"  Duration          : {aggregator.Duration.TotalMilliseconds:F2} ms");
reportWriter.WriteLine($"  Ops / second      : {opsPerSecond:F2}");
reportWriter.WriteLine();

reportWriter.WriteLine("Grapheme navigator metrics:");
reportWriter.WriteLine($"  MoveNext calls          : {snapshot.MoveNextCalls:N0}");
reportWriter.WriteLine($"  MovePrevious calls      : {snapshot.MovePreviousCalls:N0}");
reportWriter.WriteLine($"  Forward neighbor reqs   : {snapshot.ForwardNeighborRequests:N0}");
reportWriter.WriteLine($"  Backward neighbor reqs  : {snapshot.BackwardNeighborRequests:N0}");
reportWriter.WriteLine($"  Scalar fallback count   : {snapshot.ScalarFallbacks:N0}");
reportWriter.WriteLine($"  Fallback ratio          : {fallbackRatio:P4}");
reportWriter.WriteLine();

reportWriter.WriteLine("Validation gates:");
reportWriter.WriteLine($"  Rope slices verified    : {aggregator.DescriptorReplays:N0}");
reportWriter.WriteLine($"  Stage D samples visited : {aggregator.UniqueSamples:N0}");
reportWriter.WriteLine($"  Report path             : {Path.GetFullPath(options.ReportPath)}");

reportWriter.Flush();

static void ReplayDescriptor(
    Rope rope,
    DegradedGraphemeNavigator navigator,
    GraphemeDescriptor descriptor,
    TelemetryAggregator aggregator)
{
    aggregator.RecordDescriptor(descriptor);

    var start = descriptor.Utf16Range.Start;
    var end = descriptor.Utf16Range.End;
    var expectedLength = Math.Max(0, end - start);

    var forwardCursor = new NodeCursor(rope, start);
    var next = navigator.MoveNext(forwardCursor);
    if (next != end)
    {
        throw new InvalidOperationException(
            $"MoveNext mismatch for sample '{descriptor.Sample}' cluster {descriptor.ClusterIndex}: expected {end}, observed {next}.");
    }

    if (forwardCursor.Position != end)
    {
        throw new InvalidOperationException(
            $"Cursor position mismatch after MoveNext for sample '{descriptor.Sample}' cluster {descriptor.ClusterIndex}.");
    }

    var cluster = rope.GetSlice(start, expectedLength);
    if (!string.Equals(cluster, descriptor.Cluster, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"Cluster text mismatch for sample '{descriptor.Sample}' cluster {descriptor.ClusterIndex}.");
    }

    var backwardCursor = new NodeCursor(rope, end);
    var previous = navigator.MovePrevious(backwardCursor);
    if (previous != start)
    {
        throw new InvalidOperationException(
            $"MovePrevious mismatch for sample '{descriptor.Sample}' cluster {descriptor.ClusterIndex}: expected {start}, observed {previous}.");
    }

    if (backwardCursor.Position != start)
    {
        throw new InvalidOperationException(
            $"Cursor position mismatch after MovePrevious for sample '{descriptor.Sample}' cluster {descriptor.ClusterIndex}.");
    }
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

static int? ResolveMetricWindowCount(StageDDescriptorManifest manifest, string kind)
{
    foreach (var entry in manifest.MetricWindows)
    {
        foreach (var window in entry.Windows)
        {
            if (string.Equals(window.Kind, kind, StringComparison.OrdinalIgnoreCase))
            {
                return window.Count;
            }
        }
    }

    return null;
}

internal sealed class TelemetryAggregator
{
    public TelemetryAggregator(int descriptorsPerPass, int uniqueSamples)
    {
        DescriptorsPerPass = descriptorsPerPass;
        UniqueSamples = uniqueSamples;
    }

    public int DescriptorsPerPass { get; }

    public int UniqueSamples { get; }

    public long DescriptorReplays { get; private set; }

    public long RequiresFallbackDescriptors { get; private set; }

    public long CrossLeafDescriptors { get; private set; }

    public long PassAttempts { get; private set; }

    public TimeSpan Duration { get; private set; }

    public long TotalOperations => DescriptorReplays * 2;

    public void RecordPassAttempt() => PassAttempts++;

    public void RecordDescriptor(GraphemeDescriptor descriptor)
    {
        DescriptorReplays++;
        if (descriptor.RequiresFallback)
        {
            RequiresFallbackDescriptors++;
        }

        if (descriptor.CrossesLeaf)
        {
            CrossLeafDescriptors++;
        }
    }

    public void MarkComplete(TimeSpan duration)
    {
        Duration = duration;
    }
}

internal sealed class GraphemeSamplePlayback
{
    private GraphemeSamplePlayback(string sample, Rope rope, IReadOnlyList<GraphemeDescriptor> descriptors)
    {
        Sample = sample;
        Rope = rope;
        Descriptors = descriptors;
    }

    public string Sample { get; }

    public Rope Rope { get; }

    public IReadOnlyList<GraphemeDescriptor> Descriptors { get; }

    public static IReadOnlyList<GraphemeSamplePlayback> Build(IList<GraphemeDescriptor> descriptors)
    {
        return descriptors
            .GroupBy(d => d.Sample, StringComparer.Ordinal)
            .Select(group =>
            {
                var ordered = group
                    .OrderBy(descriptor => descriptor.ClusterIndex)
                    .ToList();
                var text = string.Concat(ordered.Select(descriptor => descriptor.Cluster));
                var rope = BuildRope(text, ordered);
                return new GraphemeSamplePlayback(group.Key, rope, ordered);
            })
            .ToList();
    }

    private static Rope BuildRope(string text, IReadOnlyList<GraphemeDescriptor> descriptors)
    {
        var rope = new Rope();
        if (string.IsNullOrEmpty(text))
        {
            return rope;
        }

        var leafRanges = descriptors
            .Select(descriptor => descriptor.Leaf?.Range)
            .Where(range => range is not null)
            .Select(range => (Start: range!.Start, End: range.End))
            .Distinct()
            .OrderBy(range => range.Start)
            .ToList();

        if (leafRanges.Count <= 1)
        {
            rope.Append(text);
            return rope;
        }

        var covered = 0;
        foreach (var range in leafRanges)
        {
            var clampedStart = Math.Clamp(range.Start, 0, text.Length);
            var clampedEnd = Math.Clamp(range.End, clampedStart, text.Length);
            if (clampedEnd <= clampedStart)
            {
                continue;
            }

            var length = clampedEnd - clampedStart;
            var segment = text.Substring(clampedStart, length);
            rope.Append(segment);
            covered = Math.Max(covered, clampedEnd);
        }

        if (covered < text.Length)
        {
            rope.Append(text.Substring(covered));
        }

        return rope;
    }
}

internal sealed class GraphemeTelemetryOptions
{
    private GraphemeTelemetryOptions()
    {
        FixtureDirectory = GraphemeTelemetryDefaults.FixtureDirectory;
        ReportPath = GraphemeTelemetryDefaults.BuildDefaultReportPath();
    }

    public string FixtureDirectory { get; private set; }

    public string ReportPath { get; private set; }

    public int TargetOperations { get; private set; } = GraphemeTelemetryDefaults.DefaultTargetOperations;

    public static GraphemeTelemetryOptions Parse(string[] args)
    {
        var options = new GraphemeTelemetryOptions();
        for (var i = 0; i < args.Length; i++)
        {
            var argument = args[i];
            if (MatchesFlag(argument, "--fixture-dir", out var fixtureValue))
            {
                options.FixtureDirectory = fixtureValue ?? ReadRequiredValue("--fixture-dir", ref i, args);
                continue;
            }

            if (MatchesFlag(argument, "--report", out var reportValue))
            {
                options.ReportPath = reportValue ?? ReadRequiredValue("--report", ref i, args);
                continue;
            }

            if (MatchesFlag(argument, "--target-ops", out var targetValue))
            {
                var value = targetValue ?? ReadRequiredValue("--target-ops", ref i, args);
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
                {
                    throw new ArgumentException("--target-ops must be a positive integer", nameof(args));
                }

                options.TargetOperations = parsed;
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
            value = argument[(flag.Length + 1)..];
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
}

internal static class GraphemeTelemetryDefaults
{
    public const string FixtureDirectory = "tests/xi.Core.Tests/Fixtures";

    public const string ReportDirectory = "tests/xi.Core.Tests/Fixtures/Reports";

    public const string ManifestFileName = "fixtures.manifest.json";

    public const string GraphemeLedgerName = "grapheme_descriptors.json";

    public const string GraphemeWindowKind = "grapheme_windows";

    public const string InspectorReport = "tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt";

    public const int DefaultTargetOperations = 10_000;

    public static string BuildDefaultReportPath()
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        return Path.Combine(ReportDirectory, $"grapheme-telemetry-{timestamp}.txt");
    }
}

internal sealed class ReportWriter
{
    private readonly StringBuilder _buffer = new();
    private readonly string _path;

    public ReportWriter(string path)
    {
        _path = Path.GetFullPath(path);
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
