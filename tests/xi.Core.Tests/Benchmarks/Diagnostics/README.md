````markdown
# Rope Chunk Enumerator Diagnostics

The benchmark entry point in `Program.cs` now supports two modes so QA can either keep the historical 1 MB baseline or replay the latest Stage D fixtures.

- **Synthetic baseline (default)** – builds a 1 MB lorem ipsum payload, emits the classic chunk/line stats, and mirrors the pre-existing console format so historical baselines stay comparable.
- **Stage D replay (`--stage-d`)** – loads `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` through `StageDDescriptorLoader`, reconstructs a `Rope` from real chunk descriptors, and writes a manifest-aware report to both the console and `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`.

> `python scripts/refresh_all_assets.py --only stage-d-fixtures` now runs the Stage D replay automatically (Release build) immediately after the loader/hydrator/inspector smoke so QA always has a fresh `chunk-bench-latest.txt` alongside the manifest and inspector logs.

## Commands

```bash
# Synthetic 1 MB run (legacy baseline)
dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj

# Stage D replay with report mirroring
dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj -c Release -- --stage-d --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt
```

Optional arguments:

- `--manifest <path>` – override the fixtures manifest path; defaults to `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`.
- `--report <path>` – override the report destination; defaults to `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`.
- `-c/--configuration` – pass `Release` to get throughput numbers that match `[QA-ChunkBench]` expectations (the automation in `scripts/refresh_all_assets.py` already does this).

## Output expectations

- Synthetic mode prints the legacy chunk/line statistics verbatim.
- Stage D mode adds manifest metadata (Rust commit, feature gates, ledger hashes), chunk/line throughput, and per-sample summaries, then mirrors the exact text into the report file so QA can drop it into `[QA-ChunkBench]`/`[RPM-Actions]#2` without additional tooling.
````
