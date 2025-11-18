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
- **2025-11-20**：清理 `[QA-IngestionSmoke]` / `[QA-ChunkBench]` / `[QA-Telemetry]` 记录，全部指向 2025-11-18 20:30Z Stage D orchestrator（`tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-203039.log` + rerun `stage-d-refresh-20251118-203341.log`），核对 `stage-d-inspector-latest.txt`、`chunk-bench-latest.txt`、`grapheme-telemetry-latest.txt`、`grapheme-telemetry.trx` 与 manifest（`rust_commit=b6fb5999288946daeeadc4b7f9f9756f6dda1f50`, feature gates `cursor_state,serde,tree_builder_slice_trace`），并在本档案标注 <7 天证据窗口（下一次刷新最迟 2025-11-25）。
- **2025-11-18 20:30Z**：`python scripts/refresh_all_assets.py --only stage-d-fixtures --continue-on-error` + `dotnet test Xi.Editor.sln --filter StageDDescriptor` + `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` + `dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures` 产出 canonical Stage D log/ledger；同批 Release chunk bench（5.51 ms / 4.21 ms ⇒ 181.45 / 237.40 MB/s，alloc 37,944 B）与 Grapheme telemetry harness（12,000 ops，fallback ratio 0.0000%，Requires fallback 18/668=0.3000%）写入 Reports 目录，满足 Ready Queue #5/#6/#7 的证据要求。

## 当前监控 / 风险
- **Stage D ingestion 链（<7 天窗口）**：`stage-d-refresh-20251118-203039.log`（原始 orchestrator）+ `stage-d-refresh-20251118-203341.log`（tests 更新后 rerun）及 `stage-d-inspector-latest.txt` 记录的 `rust_commit=b6fb5999…` 仍是 Goal Tree / `[QA-IngestionSmoke]` 唯一事实来源；若 2025-11-25 前未 rerun 或 manifest hash 漂移，必须立即执行 orchestrator 并写回日志。
- **Chunk bench & Grapheme telemetry 证据**：`chunk-bench-latest.txt`、`grapheme-telemetry-latest.txt`、`grapheme-telemetry.trx` 与上次 Stage D run 同步（20:35Z）；任何 Rope/Metric 改动或超过 7 天需重跑，否则 `[QA-ChunkBench]` / `[QA-Telemetry]` 会失效。（Owner：QA）
- **Metric windows 消费缺口（Needs C# Implementer + Architecture Mapper）**：manifest `metric_windows[]` 仅由 Rust exporter写入，C# Hydrator/MetricAdapter 仍未投产，`[TS-B5]` 与 `[RPM-ParityAssets]` 无法引用该字段；需要 C# Implementer 暴露 DTO + Architecture Mapper 更新映射后，再由 QA 接线到 `[QA-IngestionSmoke]`。

## 测试基线 / 工具链
| 项 | 命令 | 状态 | 证据 |
| --- | --- | --- | --- |
| Stage D full solution tests | `dotnet test Xi.Editor.sln -v m` | ✅ 200/200（2025-11-18 20:33Z rerun，哈希更新后通过） | `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-203341.log` |
| Loader/Hydrator smoke | `dotnet test Xi.Editor.sln --filter "StageDDescriptorLoaderTests|StageDDescriptorHydratorTests"` | ✅ 6/6 + 3/3（同批 rerun） | 同上 |
| Manifest verifier | `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` | ✅ “All 10 fixtures match the manifest hashes.”（2025-11-18 20:34Z） | 同上 |
| Release chunk bench | `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj --configuration Release -- --stage-d --include-alloc-stats --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` | ✅ 5.51 ms / 4.21 ms ⇒ 181.45 MB/s / 237.40 MB/s，thread alloc 37,944 bytes，GC 0/0/0（2025-11-18 20:35:14Z，sha256 f3a6a0ef…） | `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` |
| Grapheme telemetry | `dotnet run --project tests/xi.Core.Tests/Benchmarks/GraphemeTelemetryHarness/GraphemeTelemetryHarness.csproj --configuration Release -- --fixture-dir tests/xi.Core.Tests/Fixtures --target-ops 12000 --report tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry-latest.txt` + `dotnet test Xi.Editor.sln --filter Category=StageDTelemetry --logger "trx;LogFileName=tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx"` | ✅ harness 12,000 ops（Requires fallback 18/668=0.3000%，fallback ratio 0.0000%，≈35.8k ops/s）+ TRX total=3/executed=3/passed=3（2025-11-18 20:35Z，sha256 42534786…） | `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry-latest.txt`, `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx` |
| Rust serde sweep | `./xi-editor-ph7/rust/run_all_checks --filter serde-fixtures` | ✅ 2025-11-18 20:30Z run embedded in Stage D log | `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-203039.log` |

## Stage D & QA Anchors
| Anchor | 目标 | 当前状态 | 关联文档 |
| --- | --- | --- | --- |
| `[QA-IngestionSmoke]` | Stage D exporter → loader → hydrator → manifest verifier → inspector（<7 天内需新证据） | ✅ `stage-d-refresh-20251118-203039.log`（orchestrator）+ `stage-d-refresh-20251118-203341.log`（hash 修复后 rerun）+ `stage-d-inspector-latest.txt`（`rust_commit=b6fb5999288946daeeadc4b7f9f9756f6dda1f50`, feature gates `cursor_state,serde,tree_builder_slice_trace`, payload：chunk `9c7163eb…`, grapheme `b3484eab…`, breaks `527ca4f2…`, diff `afc04b22…`, search `599f25b0…`, tree trace `22724af7…`, cursor `9b46bd8e…`, delta `59c45336…`, engine `8707d5de…`, subset `28fa3c80…`)；manifest verifier同批输出 “All 10 fixtures match the manifest hashes.”，有效期至 2025-11-25。 | `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[QA-IngestionSmoke]` |
| `[QA-ChunkBench]` | 1 MB chunk/line replay（Release + alloc stats，<7 天窗口） | ✅ `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`（2025-11-18 20:35Z，chunk 5.51 ms / 181.45 MB/s，line 4.21 ms / 237.40 MB/s，alloc 37,944 bytes，sha256 f3a6a0ef…）；Inspector note字段引用同一 manifest ledger。 | `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[QA-ChunkBench]`, `docs/architecture/m3-implementation-plan.md §5.3` |
| `[QA-Telemetry]` | Grapheme harness + TRX（fallback ≤0.5%，<7 天窗口） | ✅ `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry-latest.txt`（12,000 ops，Requires fallback 18/668=0.3000%，fallback ratio 0.0000%，≈35.8k ops/s，sha256 42534786…）+ `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx`（total=3/executed=3/passed=3）；更新截止 2025-11-25 或 Grahpeme 路径变更。 | `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[QA-Telemetry]`, `docs/architecture/design-divergence-log.md` |

## Automation & Scripts
- **`scripts/refresh_serialization_fixtures.ps1`**: Stage D orchestrator chaining `run_all_checks`, `cargo export-serde-fixtures`, dotnet loader/hydrator tests, manifest verifier, and inspector. Defaults force `-SkipStageDLoaderTest:$false -SkipStageDHydratorTest:$false -SkipManifestVerification:$false -SkipStageDInspector:$false`.
- **`scripts/refresh_all_assets.py`**: use `--only stage-d-fixtures --continue-on-error` for focused reruns；`--list`/`--dry-run` 可先列出/模拟 `rust-skeletons → dotnet-build → stage-d-fixtures → verify-stage-d → goal-tree → ilspy → csharp-skeleton` 的执行顺序，避免误触。
- **`scripts/verify_fixture_manifest.py`**: canonical JSON SHA256 verifier; run without `--update` after every exporter change so logs contain an explicit "All N fixtures match" line.
- **Benchmarks & telemetry**: Release `tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj` with `--include-alloc-stats` remains `[QA-ChunkBench]` 入口；`tests/xi.Core.Tests/Benchmarks/GraphemeTelemetryHarness/GraphemeTelemetryHarness.csproj --configuration Release -- --fixture-dir tests/xi.Core.Tests/Fixtures --target-ops >=12000 --report <Reports/...>` 现为 `[QA-Telemetry]` 主体，`dotnet test ... --filter Category=StageDTelemetry` TRX 作为辅助烟雾证据。

## 待办 / 风险
- [ ] **Stage D rerun截止 2025-11-25（Owner：QA）**：若 `stage-d-refresh-*`、`chunk-bench-latest.txt`、`grapheme-telemetry-latest.txt` 未在 7 天内刷新，`[QA-*]` anchors 将失效并阻塞 Ready Queue #1/#2；需在 11/23-11/24 之间预定一次 orchestrator rerun。
- [ ] **Metric windows 消费（Needs C# Implementer + Architecture Mapper）**：`metric_windows[]` 仍未注入 `StageDDescriptorHydrator`/`MetricAdapter`，Goal Tree `[TS-B5]` 缺乏可验证证据；等待 C# 实现 DTO + Architecture Mapper 回写 `[RPM-ParityAssets]`，QA 才能在 `[QA-IngestionSmoke]` 记录窗口数据。
- [ ] **Chunk bench / Telemetry 自动化（QA + Architecture Mapper）**：Release harness 仍靠手动触发；需把命令挂到 `scripts/refresh_all_assets.py` 或 nightly job，输出稳定 mtime/sha，以免 <7 天窗口被错过。
- [ ] **Loader/Hydrator 参数化（Needs C# Implementer）**：希望把 manifest/ledger 路径与 hash 设为配置项，避免下次 payload drift 时必须改动测试源码；需求已在 Ready Queue #5 追踪。

## 文档索引
- `docs/csharp-refactor/rope-serialization-fixture-playbook.md`: Stage D QA playbook with `[QA-*]` anchors.
- `docs/architecture/m3-implementation-plan.md`: Chunk/telemetry metrics plus risk items R8/R9/R10.
- `docs/architecture/design-divergence-log.md`: telemetry and benchmark variance tracking.
- `docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]`: manifest ledger and schema mapping.
- `docs/meetings/2025-11-19-knowledge-refresh-chat.md`: current sync notes referencing this dossier.

---

> ⚠️ 除非架构师要求，不要另建 markdown 文档；所有状态写入本档案与 Playbook/Goal Tree/Stage D 文档并保持锚点对齐。
