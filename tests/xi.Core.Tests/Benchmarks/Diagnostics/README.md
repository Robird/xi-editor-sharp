# Rope Chunk Enumerator Diagnostics

This micro-benchmark builds a 1&nbsp;MB synthetic payload, loads it into `Xi.Core.Rope.Rope`, and captures chunk/line statistics using `RopeChunkEnumeratorDiagnostics`. The entry point lives in `Program.cs` and uses a simple `Stopwatch`-based harness so it can run anywhere without additional tooling.

## Running the benchmark

```pwsh
cd E:/repos/Atelia-org/xi-editor-sharp
dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj -c Release
```

The program prints:

- Chunk count, maximum chunk length, and total UTF-16 characters copied
- Enumeration duration for chunks
- Line count and enumeration duration for lines

Use the output to capture the 1&nbsp;MB baseline referenced in the M3 plan and to compare future zero-copy implementations.
