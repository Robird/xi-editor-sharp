---
role: QA Engineer
project: xi-editor-sharp
reports_to: AI 架构师
partners:
	- C# Implementer
	- Rust Porter
	- Architecture Mapper
	- Information Researcher
start_date: 2025-11-17
responsibilities:
	- 维护回归/benchmark 基线并对接 Stage D 验收
	- 消费/校验 Rust parity 夹具与 manifest
	- 监控 Grapheme/Chunk 遥测与 1 MB 基准
interfaces:
	- Rust Porter: parity fixtures / CLI schema / Stage D manifest
	- C# Implementer: dotnet tests, diagnostics, ingest scripts
	- Architecture Mapper: Stage D anchors、Goal Tree、风险登记
cadence:
	status_update: 任务完成 ≤24h 内刷新档案 + Goal Tree
	baseline_run: `dotnet test -v m` 每日班次结束前一次
	fixture_refresh: Stage D 请求或 manifest/hash 漂移时立刻重跑
---

## 最近完成
- **2025-11-19**: 将 QA 专属 runSubAgent 条目 #5 “Stage D ingestion smoke + manifest ledger audit” 与 #6 “Release chunk bench + Grapheme telemetry ingestion (QA verification)” 登记到 `docs/sprints/sptrint-1.md#Ready Queue`，为 Sprint 1 对应的 `[QA-IngestionSmoke]`、`[QA-ChunkBench]`、`[QA-Telemetry]` 路线提供标准化触发点。
- **2025-11-19**: Stage D ingestion smoke reran via `python3 scripts/refresh_all_assets.py --only stage-d-fixtures --continue-on-error | tee tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118T162433Z.log`; exporter -> loader/hydrator -> `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` (reported "All 10 fixtures match the manifest hashes.") -> `dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures` all passed, and `dotnet test Xi.Editor.sln -v m` recorded 199/199 plus 5/5 loader + 3/3 hydrator. `stage-d-inspector-latest.txt` now holds the authoritative ledger (chunk 69ba7f2536876ad3a76296a215ac163fa82629934a97a82e49faa25423f9415a, grapheme eb0c7c66069ca33a3626ed3909754da72223f6e0e283d6c7b2b3182a0a35182c, breaks 5d37a7313730dd0ae9c292ca45daa442c11fe63d45f9ad21bad664f919c13f86, diff fe76ed31cff4549ffd3f32bd3847dda15ced7538c4165b4566f782e715572562, search 7eac7ecf3bb0bdbf5369b6b0dcb17bf5241d8c914af791440e2c5a4582ee7d57, tree trace 22724af7fe8b22e4e1dbd01f86ba1b3902a0ccf777944b5b29081259927c3a6e).
- **2025-11-19**: Pruned QA dossier + anchors to drop stale 11/17-11/18 paragraphs, re-linked `[QA-IngestionSmoke]`/`[QA-ChunkBench]`/`[QA-Telemetry]` to the 2025-11-19 artefacts, and mirrored the same references inside `docs/meetings/2025-11-19-knowledge-refresh-chat.md` and `docs/csharp-refactor/rope-serialization-fixture-playbook.md`.
- **2025-11-18**: Release `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj --configuration Release -- --stage-d --include-alloc-stats --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` produced chunk 4.25 ms (235.12 MB/s) / line 3.25 ms (307.97 MB/s) with thread alloc 37,944 bytes and GC 0/0/0, satisfying `[QA-ChunkBench]`. Immediately reran `dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter GraphemeNavigatorSmokeTests --logger "trx;LogFileName=tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx"` (total=3/executed=3/passed=3) to refresh `[QA-Telemetry]`.

## 当前监控
- **Sprint 1 Ready Queue (#5/#6)**: 等待 C# Implementer 提供 loader/hydrator + bench/telemetry harness 和 Rust Porter 提供 CLI/manifest/hash drop，一旦窗口开启即触发 Stage D ingestion smoke + Release chunk bench/≥10k Grapheme telemetry reruns并回填 `[QA-*]` anchors。
- **11/23-11/24 anchor refresh**: need another Stage D orchestrator + chunk bench + grapheme telemetry run before the 11/27 readiness sync so `[QA-IngestionSmoke]`, `[QA-ChunkBench]`, `[QA-Telemetry]` stay <7 days old.
- **Manifest drift response**: Rust Porter is preparing the next exporter drop (adds metric_windows, extended tree trace). C# Implementer must update `StageDDescriptorLoader/Hydrator` assertions the same day or the Stage D pipeline will halt on hash mismatches.
- **Chunk bench guardrail**: keep Release + `--include-alloc-stats` as the only accepted evidence; alert C# Implementer if throughput falls below 200 MB/s or allocations exceed 5 MB.
- **Telemetry coverage gap**: Grapheme TRX currently reports only three smoke tests; Architecture Mapper expects >=10k operation coverage once C# Implementer delivers scripted replay and Rust Porter ships a larger dataset.

## 测试基线 / 工具链
| 项 | 命令 | 状态 | 证据 |
| --- | --- | --- | --- |
| Stage D full solution tests | `dotnet test Xi.Editor.sln -v m` | ✅ 199/199 (2025-11-19, Stage D refresh) | `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118T162433Z.log` |
| Loader/Hydrator smoke | `dotnet test Xi.Editor.sln --filter "StageDDescriptorLoaderTests|StageDDescriptorHydratorTests"` | ✅ 5/5 + 3/3 (2025-11-19) | 同上 |
| Manifest verifier | `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` | ✅ "All 10 fixtures match the manifest hashes." (2025-11-19) | 同上 |
| Release chunk bench | `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj --configuration Release -- --stage-d --include-alloc-stats --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` | ✅ 4.25 ms / 3.25 ms, 37,944 bytes alloc, GC 0/0/0 (2025-11-19) | `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` |
| Grapheme telemetry | `dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter GraphemeNavigatorSmokeTests --logger "trx;LogFileName=tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx"` | ✅ total=3/executed=3/passed=3 (2025-11-19T00:25:41+08:00) | `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx` |
| Rust serde sweep | `./xi-editor-ph7/rust/run_all_checks --filter serde-fixtures` | ✅ 2025-11-19 run embedded in Stage D log | `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118T162433Z.log` |

## Stage D & QA Anchors
| Anchor | 目标 | 当前状态 | 关联文档 |
| --- | --- | --- | --- |
| `[QA-IngestionSmoke]` | Stage D exporter -> loader -> hydrator -> manifest verifier -> inspector | ✅ `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118T162433Z.log` + `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt` (rust_commit=3799d2be9db0ef040517ed69df1b717e96a8958e; chunk 69ba7f25..., grapheme eb0c7c66..., breaks 5d37a731..., diff fe76ed31..., search 7eac7ecf, tree trace 22724af7...) | `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[QA-IngestionSmoke]` |
| `[QA-ChunkBench]` | 1 MB chunk/line replay, Release + alloc stats | ✅ `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` (chunk 235.12 MB/s, line 307.97 MB/s, alloc 37,944 bytes) | `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[QA-ChunkBench]`, `docs/architecture/m3-implementation-plan.md §5.3` |
| `[QA-Telemetry]` | Grapheme fallback ratio <=0.5% with TRX evidence | ✅ `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx` (Counters total=3/executed=3/passed=3, fallback ratio 0%) | `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[QA-Telemetry]`, `docs/architecture/design-divergence-log.md` |

## Automation & Scripts
- **`scripts/refresh_serialization_fixtures.ps1`**: Stage D orchestrator chaining `run_all_checks`, `cargo export-serde-fixtures`, dotnet loader/hydrator tests, manifest verifier, and inspector. Defaults force `-SkipStageDLoaderTest:$false -SkipStageDHydratorTest:$false -SkipManifestVerification:$false -SkipStageDInspector:$false`.
- **`scripts/refresh_all_assets.py`**: use `--only stage-d-fixtures --continue-on-error` for focused reruns; `--mode stage-d --check-anchors QA-IngestionSmoke QA-ChunkBench QA-Telemetry` keeps docs, Goal Tree, and anchors consistent.
- **`scripts/verify_fixture_manifest.py`**: canonical JSON SHA256 verifier; run without `--update` after every exporter change so logs contain an explicit "All N fixtures match" line.
- **Benchmarks & telemetry**: Release `tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj` with `--include-alloc-stats` is the only valid `[QA-ChunkBench]` entrypoint; `GraphemeNavigatorSmokeTests` with TRX logging underpins `[QA-Telemetry]` until the high-volume replay lands.

## 待办 / 风险
- [ ] **Metric windows + tree trace schema**: waiting on Rust Porter to land manifest fields and exporter defaults; once delivered we must rerun Stage D pipeline and refresh `[QA-IngestionSmoke]` hashes the same day.
- [ ] **Loader/Hydrator assertions**: need C# Implementer to expose configurable ledger sources so QA can pick up new payload hashes without code patches whenever manifest drifts.
- [ ] **High-volume telemetry capture**: C# Implementer + Architecture Mapper must finalize the >=10k operation replay harness so `[QA-Telemetry]` reports aggregate fallback rate rather than a 3-test smoke.
- [ ] **Bench regression surfacing**: integrate Release `RopeChunkEnumeratorBenchmarks` (alloc stats) into nightly automation; Architecture Mapper owns Goal Tree wiring, QA owns alerting.

## 文档索引
- `docs/csharp-refactor/rope-serialization-fixture-playbook.md`: Stage D QA playbook with `[QA-*]` anchors.
- `docs/architecture/m3-implementation-plan.md`: Chunk/telemetry metrics plus risk items R8/R9/R10.
- `docs/architecture/design-divergence-log.md`: telemetry and benchmark variance tracking.
- `docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]`: manifest ledger and schema mapping.
- `docs/meetings/2025-11-19-knowledge-refresh-chat.md`: current sync notes referencing this dossier.

---

> ⚠️ 除非架构师要求，不要另建 markdown 文档；所有状态写入本档案与 Playbook/Goal Tree/Stage D 文档并保持锚点对齐。
