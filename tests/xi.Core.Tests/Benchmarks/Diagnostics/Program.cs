using System.Diagnostics;
using System.Text;
using Xi.Core.Rope;

const int TargetBytes = 1_048_576; // 1 MB baseline

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
