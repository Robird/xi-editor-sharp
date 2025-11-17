# QA Chunk Window Benchmark Log

> **Anchor**：`[QA-ChunkWindowLog]`
> **Owner**：QA Engineer
> **Purpose**：记录 Rope Chunk/Line Benchmarks（含 Span/ArrayPool 诊断、Grapheme 遥测）的命令、环境、结果与 Stage D 依赖，支撑 `[QA-ChunkBench]`、`[MP-R10]` 与 `design-divergence-log.md#[Div-ChunkWindow]` 的可追溯性。

## [QA-ChunkWindowLog] 基线结果
| 记录 ID | 日期 | 机器/资源 | 输入场景 | 命令 | 结果摘要 |
| --- | --- | --- | --- | --- | --- |
| CW-2025-11-17-A | 2025-11-17 | Linux · 16 GB RAM · `dotnet 8.0.100` | Rope 长度 1,048,625 UTF-16 chars（1 MB 名义），Leaf Max 1,000 | `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj -c Release --no-build` | Chunk：12.62 ms（≈79 MiB/s 名义，≈159 MiB/s UTF-16），Line：14.99 ms（≈67/133 MiB/s）。Alloc 指标未采集，已在 `[MP-R10]` 标为 ⚠️。 |

> 说明：本表需随着 BenchmarkDotNet/QA 运行更新；若命令或输入发生变化，应追加新行而非覆盖旧记录。

## [QA-ChunkWindowPlan] 待执行事项
1. **Windows 32 GB Benchmark（CW-2025-11-19-W）**
   - 资源：Windows 11 Pro · 32 GB RAM · `dotnet 8.0.100` · BenchmarkDotNet 0.13+。
   - 输入：1 MB & 32 MB 文本（含 emoji/CRLF），`ArrayPool<char>` Span 实现。
   - 命令：
     ```bash
     dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/ChunkWindowBenchmarks.csproj \
       -c Release -- \
       --scenario chunk-1mb --scenario chunk-32mb --artifacts artifacts/chunk-bench
     ```
   - 输出：吞吐 ≥200 MB/s 或记录改进计划；alloc <5 MB 计数；遥测文件写入 `artifacts/chunk-bench/telemetry.json`。
2. **Telemetry Merge（Cursor/Chunk/Grapheme）**
   - 等待 `CursorEditTelemetry.json`、`ChunkWindowMetrics.json`、`GraphemeFallbackMetrics.json` 并落入 Stage D manifest。
   - QA 命令：
     ```bash
     pwsh ./scripts/refresh_serialization_fixtures.ps1 -ExportTelemetry Cursor,Chunk,Grapheme
     dotnet test tests/xi.Core.Tests/Telemetry/TelemetryIngestionTests.cs -c Release
     ```
   - 结果写入本文件和 `docs/architecture/m3-implementation-plan.md §5.3`。
3. **Coyote 并发基准**
   - 依赖 `CursorEditSession` instrumentation。
   - 命令：`dotnet coyote test tests/xi.Core.Tests/Coyote/CursorLifecycle.coyote --iterations 200 --timeout 00:20:00`。
   - 输出 `_editVersion` 命中率与假失效率，记录于 `[QA-ChunkWindowLog]` 附注并同步 `type-system-migration-log.md#[TS-O2-Metrics]`。

## [QA-ChunkWindowChangeLog]
| 日期 | 版本 | 修改者 | 说明 |
| --- | --- | --- | --- |
| 2025-11-18 | 0.1 | QA Engineer | 初始化日志，移植 11/17 Linux 基线，并列出 Windows 32 GB/Telemetry/Coyote 三项待办。 |
