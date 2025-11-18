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
- **2025-11-19**: 重构 `scripts/refresh_all_assets.py` 流水线，让 `dotnet build` 提前、Stage D 全套（PowerShell exporter -> StageDDescriptorInspector -> Release chunk bench + Grapheme telemetry harness + StageDTelemetry 烟雾）与 Goal Tree 串行接力，并让 Step 命令在 `--list/--dry-run` 时按仓库根动态注入 manifest/inspector/chunk 报告；`python scripts/refresh_all_assets.py --list` 现展示顺序 `rust-skeletons → dotnet-build → stage-d-fixtures → verify-stage-d → goal-tree → ilspy → csharp-skeleton`，`--dry-run` 输出新增 `stage-d-telemetry-harness` 与 `goal_tree_sync.py --manifest … --inspector … --chunk-report …`，确保 CI/QA 无参数即可覆盖日常刷新。
- **2025-11-19**: Ready Queue #6 Release chunk bench + ≥10k Grapheme telemetry QA 验证完成：`dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj --configuration Release -- --stage-d --include-alloc-stats --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`（mtime `2025-11-19 03:18:52 +08:00`, sha256 `6e9a617c2a5dd755b5bb60d1e4be0938a47cd06369efce272c46f27ef17ad469`）产出 chunk 3.91 ms（255.93 MB/s） / line 2.88 ms（347.07 MB/s），thread alloc 37,944 bytes、GC 0/0/0；随后执行 `dotnet run --project tests/xi.Core.Tests/Benchmarks/GraphemeTelemetryHarness/GraphemeTelemetryHarness.csproj --configuration Release -- --fixture-dir tests/xi.Core.Tests/Fixtures --target-ops 12000 --report tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry-20251118T191914Z.txt`（mtime `2025-11-19 03:19:18 +08:00`, sha256 `33460c5055247cf6bed05540f0a4ef429d39be42c25eeced2d08d1df86fa09bd`）得到 12,000 ops、fallback ratio 0%。`dotnet test Xi.Editor.sln --filter Category=StageDTelemetry --logger "trx;LogFileName=tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx"`（mtime `2025-11-19 03:13:13 +08:00`, total=3/executed=3/passed=3）同步 TRX 证据。上述 artefact 已更新 `[QA-ChunkBench]`、`[QA-Telemetry]`、`docs/sprints/sptrint-1.md` Ready Queue #6。
- **2025-11-19**: 完成 Ready Queue #5 Stage D ingestion smoke + manifest ledger audit，串行执行 `python scripts/refresh_all_assets.py --only stage-d-fixtures --continue-on-error` → `dotnet test Xi.Editor.sln --filter StageDDescriptor` → `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` → `dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures | tee tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt`；`tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-191129.log` 捕获 exporter→loader/hydrator smoke、manifest verifier “All 10 fixtures match the manifest hashes.” 与 inspector ledger（chunk 69ba7f25 / grapheme eb0c7c66 / breaks 5d37a731 / diff fe76ed31 / search 7eac7ecf / tree trace 22724af7 / cursor 9b46bd8e / delta 59c45336 / engine 8707d5de / subset 28fa3c80），同批 Release chunk bench（261.73 MB/s / 312.99 MB/s，thread alloc 37,944 bytes）和 `grapheme-telemetry.trx` 也刷新；StageDDescriptorInspector 尚无 `--report` flag，当前以 `tee` 写入 `stage-d-inspector-latest.txt` 并在 Ready Queue #7 跟踪。
- **2025-11-19**: 将 QA 专属 runSubAgent 条目 #5 “Stage D ingestion smoke + manifest ledger audit” 与 #6 “Release chunk bench + Grapheme telemetry ingestion (QA verification)” 登记到 `docs/sprints/sptrint-1.md#Ready Queue`，为 Sprint 1 对应的 `[QA-IngestionSmoke]`、`[QA-ChunkBench]`、`[QA-Telemetry]` 路线提供标准化触发点。
- **2025-11-19**: Stage D ingestion smoke reran via `python3 scripts/refresh_all_assets.py --only stage-d-fixtures --continue-on-error | tee tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118T162433Z.log`; exporter -> loader/hydrator -> `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` (reported "All 10 fixtures match the manifest hashes.") -> `dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures` all passed, and `dotnet test Xi.Editor.sln -v m` recorded 199/199 plus 5/5 loader + 3/3 hydrator. `stage-d-inspector-latest.txt` now holds the authoritative ledger (chunk 69ba7f2536876ad3a76296a215ac163fa82629934a97a82e49faa25423f9415a, grapheme eb0c7c66069ca33a3626ed3909754da72223f6e0e283d6c7b2b3182a0a35182c, breaks 5d37a7313730dd0ae9c292ca45daa442c11fe63d45f9ad21bad664f919c13f86, diff fe76ed31cff4549ffd3f32bd3847dda15ced7538c4165b4566f782e715572562, search 7eac7ecf3bb0bdbf5369b6b0dcb17bf5241d8c914af791440e2c5a4582ee7d57, tree trace 22724af7fe8b22e4e1dbd01f86ba1b3902a0ccf777944b5b29081259927c3a6e).
- **2025-11-19**: Pruned QA dossier + anchors to drop stale 11/17-11/18 paragraphs, re-linked `[QA-IngestionSmoke]`/`[QA-ChunkBench]`/`[QA-Telemetry]` to the 2025-11-19 artefacts, and mirrored the same references inside `docs/meetings/2025-11-19-knowledge-refresh-chat.md` and `docs/csharp-refactor/rope-serialization-fixture-playbook.md`.
- **2025-11-18**: Release `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj --configuration Release -- --stage-d --include-alloc-stats --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` produced chunk 4.25 ms (235.12 MB/s) / line 3.25 ms (307.97 MB/s) with thread alloc 37,944 bytes and GC 0/0/0, satisfying `[QA-ChunkBench]`. Immediately reran `dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter GraphemeNavigatorSmokeTests --logger "trx;LogFileName=tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx"` (total=3/executed=3/passed=3) to refresh `[QA-Telemetry]`.

## 当前监控
- `scripts/refresh_all_assets.py` 现默认串行 `dotnet build → stage-d-fixtures → StageDDescriptorInspector → Release chunk bench → Grapheme telemetry harness → StageDTelemetry 烟雾 → goal-tree`，每次运行都覆盖固定报告：`stage-d-inspector-latest.txt`、`chunk-bench-latest.txt`、`grapheme-telemetry-latest.txt`、`grapheme-telemetry.trx`。CI/本地若发现其中任一文件缺失或 mtime 落后，即触发脚本无参刷新；`--list`/`--dry-run` 可快速审计命令和参数。

## 测试基线 / 工具链
| 项 | 命令 | 状态 | 证据 |
| --- | --- | --- | --- |
| Stage D full solution tests | `dotnet test Xi.Editor.sln -v m` | ✅ 200/200 (2025-11-19 Stage D refresh) | `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-191129.log` |
| Loader/Hydrator smoke | `dotnet test Xi.Editor.sln --filter "StageDDescriptorLoaderTests|StageDDescriptorHydratorTests"` | ✅ 6/6 + 3/3 (2025-11-19) | 同上 |
| Manifest verifier | `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` | ✅ "All 10 fixtures match the manifest hashes." (2025-11-19) | 同上 |
| Release chunk bench | `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj --configuration Release -- --stage-d --include-alloc-stats --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` | ✅ 3.91 ms / 2.88 ms ⇒ 255.93 MB/s / 347.07 MB/s，thread alloc 37,944 bytes，GC 0/0/0（2025-11-19 03:18:52 +08:00，sha256 6e9a617c…） | `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` |
| Grapheme telemetry | `dotnet run --project tests/xi.Core.Tests/Benchmarks/GraphemeTelemetryHarness/GraphemeTelemetryHarness.csproj --configuration Release -- --fixture-dir tests/xi.Core.Tests/Fixtures --target-ops 12000 --report tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry-20251118T191914Z.txt` + `dotnet test Xi.Editor.sln --filter Category=StageDTelemetry --logger "trx;LogFileName=tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx"` | ✅ harness 12,000 ops（fallback 0%，ops/sec 35,635）+ TRX total=3/executed=3/passed=3（2025-11-19） | `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry-20251118T191914Z.txt`, `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx` |
| Rust serde sweep | `./xi-editor-ph7/rust/run_all_checks --filter serde-fixtures` | ✅ 2025-11-19 run embedded in Stage D log | `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-191129.log` |

## Stage D & QA Anchors
| Anchor | 目标 | 当前状态 | 关联文档 |
| --- | --- | --- | --- |
| `[QA-IngestionSmoke]` | Stage D exporter -> loader -> hydrator -> manifest verifier -> inspector | ✅ `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-191129.log` + `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt`（rust_commit=3799d2be9db0ef040517ed69df1b717e96a8958e；chunk 69ba7f25..., grapheme eb0c7c66..., breaks 5d37a731..., diff fe76ed31..., search 7eac7ecf..., tree trace 22724af7..., cursor 9b46bd8e..., delta 59c45336..., engine 8707d5de..., subset 28fa3c80...） | `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[QA-IngestionSmoke]` |
| `[QA-ChunkBench]` | 1 MB chunk/line replay, Release + alloc stats | ✅ `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`（chunk 3.91 ms / 255.93 MB/s，line 2.88 ms / 347.07 MB/s，alloc 37,944 bytes，sha256 6e9a617c…） | `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[QA-ChunkBench]`, `docs/architecture/m3-implementation-plan.md §5.3` |
| `[QA-Telemetry]` | Grapheme fallback ratio <=0.5% with TRX evidence | ✅ `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry-20251118T191914Z.txt`（12,000 ops，fallback 0%，ops/sec 35,635，sha256 33460c50…）+ `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx`（total=3/executed=3/passed=3） | `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[QA-Telemetry]`, `docs/architecture/design-divergence-log.md` |

## Automation & Scripts
- **`scripts/refresh_serialization_fixtures.ps1`**: Stage D orchestrator chaining `run_all_checks`, `cargo export-serde-fixtures`, dotnet loader/hydrator tests, manifest verifier, and inspector. Defaults force `-SkipStageDLoaderTest:$false -SkipStageDHydratorTest:$false -SkipManifestVerification:$false -SkipStageDInspector:$false`.
- **`scripts/refresh_all_assets.py`**: use `--only stage-d-fixtures --continue-on-error` for focused reruns; `--mode stage-d --check-anchors QA-IngestionSmoke QA-ChunkBench QA-Telemetry` keeps docs, Goal Tree, and anchors consistent.
- **`scripts/verify_fixture_manifest.py`**: canonical JSON SHA256 verifier; run without `--update` after every exporter change so logs contain an explicit "All N fixtures match" line.
- **Benchmarks & telemetry**: Release `tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj` with `--include-alloc-stats` remains `[QA-ChunkBench]` 入口；`tests/xi.Core.Tests/Benchmarks/GraphemeTelemetryHarness/GraphemeTelemetryHarness.csproj --configuration Release -- --fixture-dir tests/xi.Core.Tests/Fixtures --target-ops >=12000 --report <Reports/...>` 现为 `[QA-Telemetry]` 主体，`dotnet test ... --filter Category=StageDTelemetry` TRX 作为辅助烟雾证据。

## 待办 / 风险
- [ ] **Metric windows + tree trace schema**: waiting on Rust Porter to land manifest fields and exporter defaults; once delivered we must rerun Stage D pipeline and refresh `[QA-IngestionSmoke]` hashes the same day.
- [ ] **Loader/Hydrator assertions**: need C# Implementer to expose configurable ledger sources so QA can pick up new payload hashes without code patches whenever manifest drifts.
- [ ] **High-volume telemetry capture**: Harness（12k ops）现可手动运行，但仍需 C# Implementer + Architecture Mapper 把命令纳入自动化并扩展 dataset，以便 `[QA-Telemetry]` 在无人值守状态下持续监测 fallback >0.5% 的异常。
- [ ] **Bench regression surfacing**: integrate Release `RopeChunkEnumeratorBenchmarks` (alloc stats) into nightly automation; Architecture Mapper owns Goal Tree wiring, QA owns alerting.

## 文档索引
- `docs/csharp-refactor/rope-serialization-fixture-playbook.md`: Stage D QA playbook with `[QA-*]` anchors.
- `docs/architecture/m3-implementation-plan.md`: Chunk/telemetry metrics plus risk items R8/R9/R10.
- `docs/architecture/design-divergence-log.md`: telemetry and benchmark variance tracking.
- `docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]`: manifest ledger and schema mapping.
- `docs/meetings/2025-11-19-knowledge-refresh-chat.md`: current sync notes referencing this dossier.

---

> ⚠️ 除非架构师要求，不要另建 markdown 文档；所有状态写入本档案与 Playbook/Goal Tree/Stage D 文档并保持锚点对齐。
