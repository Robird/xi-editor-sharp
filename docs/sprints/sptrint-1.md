---
Title: Sprint 1 Alignment Skeleton
Summary: Scaffold for aligning runSubAgent-ready work after the 2025-11-19 knowledge refresh checkpoint.
Status: Ready Queue #1-#7 Completed – awaiting Sprint 2 intake
Last-Updated: 2025-11-19T19:45Z
Owners:
  - Architecture Mapper
  - AI Architect
Related-Docs:
  - docs/meetings/2025-11-19-knowledge-refresh-chat.md
  - docs/architecture/document-structure-template.md
  - agents/architecture-mapper.md
---

## Summary
Sprint 1 锁定四条证据链：Rust Porter 让 Stage D exporter 默认产出 `metric_windows[]` 并写入 `[StageD::ParityAssets]` / `[Fixture-MetricWindows]`；C# Implementer rerun Stage D + `_editVersion ↔ NodeCursorState`，把结果扎进 `[MP-T1]` 与 `[RPM-Matrix]`；Release chunk bench 与 ≥10k Grapheme telemetry 现稳定落在 `[QA-ChunkBench]` / `[QA-Telemetry]`，支持 `[StageD::FeatureGates]` 回溯；QA Engineer + Information Researcher 把 ingestion smoke 与证据索引写回 `[QA-IngestionSmoke]`、`[SO-Map]`、`agents/information-researcher.md#证据快照`，形成 Stage D → QA → Evidence 闭环。

## Sprint Overview
**Background**: Captures the normalized follow-ups from `docs/meetings/2025-11-19-knowledge-refresh-chat.md`, focusing on runSubAgent tasks that must respect the architecture template and Goal Tree guardrails.

**Time Window**: 2025-11-19 -> 2025-11-26 (syncing with the Stage D rerun and Type System checkpoints highlighted during the knowledge refresh chat).

**Primary Outcome Metrics**:
- Ready Queue contains only runSubAgent-formatted tasks with referenced evidence (fixtures, benchmarks, telemetry).
- Each accepted task cites upstream Goal Tree IDs and `[QA-*]` / `[StageD::*]` anchors before execution.
- Coordination Notes reflect cross-role dependencies within 24 hours of any scope change, keeping `goal_tree_sync.py --check` clean.

## Task Intake Rules
1. Every candidate must trace back to a Goal Tree node or blocker anchor and link to the evidence artifact (logs, manifests, telemetry) that triggered the work.
2. Only runSubAgent-compatible requests (single-owner, CLI-ready step, <=1 day of effort) enter the Ready Queue; larger efforts stay in Backlog Candidates until broken down.
3. Intake submissions must specify the desired owner role (C# Implementer, Rust Porter, QA Engineer, Information Researcher) plus success signals, so reviewers can fast-track gating tasks.
4. QA and Stage D related requests must include the latest manifest hash or benchmark log path to keep the ledger consistent with `docs/csharp-refactor/rope-serialization-fixture-playbook.md`.

## Backlog Candidates
| # | Candidate Task | Intake Source | Notes |
| --- | --- | --- | --- |

> Pending contributions: C# Implementer, Rust Porter, QA Engineer, and Information Researcher should add their first groomed items to the Ready Queue below and log blockers inside Coordination Notes as soon as they have evidence from the knowledge refresh action items.

## Ready Queue
| # | Task Name | runSubAgent Owner | Scope | Dependencies | Acceptance Criteria | Status | Evidence (Date + Artefacts) |
| :-: | --- | --- | --- | --- | --- | :-: | --- |
| 1 | Stage D rerun + `_editVersion ↔ NodeCursorState` doc alignment | C# Implementer SubAgent | Rerun `python scripts/refresh_all_assets.py`（必要时加 `--only stage-d-fixtures`）、`dotnet test Xi.Editor.sln --filter StageDDescriptor`、`StageDDescriptorInspector`; update descriptor hashes in `[MP-T1]` + `[RPM-Matrix]`. | QA Engineer（Stage D ledger + `[QA-IngestionSmoke]` hooks）；Rust Porter（cursor descriptor schema）；automation scripts（`refresh_all_assets.py`、StageDDescriptorInspector）。 | `_editVersion ↔ NodeCursorState` facts now live in `[MP-T1]`/`[RPM-Matrix]`; Ready Queue entry stays linked to `[StageD::ParityAssets]`、`[StageD::FixtureFlow]`; Goal Tree metadata remains unchanged. | Complete | 2025-11-19: `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-184351.log`<br>2025-11-19: `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt` |
| 2 | Release chunk bench + ≥10k Grapheme telemetry drop | C# Implementer SubAgent | Run Release chunk bench with `--stage-d --include-alloc-stats`, drive ≥10k Grapheme telemetry, and persist artefacts under `tests/xi.Core.Tests/Fixtures/Reports/`. | QA Engineer（accept Release bench + telemetry bundle）；Rust Porter（Grapheme trace / `--grapheme-windows` data）；benchmark + telemetry scripts。 | `[QA-ChunkBench]` / `[QA-Telemetry]` now carry the Release metrics and TRX bundle; G2/G4 Goal Tree anchors + `[StageD::FeatureGates]` cite the same throughput + fallback story. | Complete | 2025-11-19: `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`<br>2025-11-19: `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry-20251118T185753Z.txt`<br>2025-11-19: `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx` |
| 3 | Stage D exporter 默认开启 + `metric_windows[]` manifest 字段 | Rust Porter SubAgent | Update exporter defaults across cursor/chunk/grapheme/breaks/diff/search/tree trace; emit `metric_windows[]` into `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`; rerun `./run_all_checks --filter serde-fixtures`. | `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-230526-porter.log`（porter drop）；Architecture Mapper + QA acknowledgements记录在 `AGENTS.md`；`[StageD::FixtureFlow]` 指向默认 bundle。 | Manifest ledger lists `metric_windows[]` counts; `[StageD::FeatureGates]`、`[RPM-ParityAssets]`、`[Fixture-MetricWindows]` reference the same schema for reruns. | Complete | 2025-11-19: `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-230526-porter.log`<br>2025-11-19: `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` |
| 4 | Stage D CLI schema & feature gate spec drop | Rust Porter SubAgent | Capture canonical CLI flags + JSON schema, document under `[StageD::FeatureGates]` and `[RPM-ParityAssets]`, and provide sample snippets for QA/C#. | `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::FeatureGates]`（默认 flag 表）；`docs/architecture/fixtures/parity-fixture-schema.md#[Fixture-MetricWindows]`；Information Researcher evidence index（2025-11-19）。 | CLI flag matrix、feature-gate strategy、`metric_windows`/`cursor_state`/`tree_builder_slice_trace` samples now live in `[StageD::FeatureGates]` + `[StageD::ParityAssets]` and cross-link to `[RPM-ParityAssets]` / `[SO-Map]`; `goal_tree_sync.py` metadata remains synchronized. | Complete | 2025-11-19: `docs/csharp-refactor/rope-serialization-fixture-playbook.md#StageD::FeatureGates`<br>2025-11-19: `docs/architecture/fixtures/parity-fixture-schema.md#Fixture-MetricWindows` |
| 5 | Stage D ingestion smoke + manifest ledger audit | QA Engineer SubAgent | Execute `python scripts/refresh_all_assets.py --continue-on-error`（可加 `--only stage-d-fixtures` 做局部 rerun）、`dotnet test Xi.Editor.sln --filter StageDDescriptor`, `python scripts/verify_fixture_manifest.py --manifest ...`, and StageDDescriptorInspector; store every report under `tests/xi.Core.Tests/Fixtures/Reports/`. | C# Implementer（loader/hydrator + StageDDescriptorInspector）；Rust Porter（CLI + manifest drop）；Architecture Mapper（Goal Tree anchor alignment）。 | `[QA-IngestionSmoke]` documents the rerun + hash list; `[StageD::FixtureFlow]` captures the inspector gap + manifest verdict so QA can replay the run without missing artefacts. | Complete | 2025-11-19: `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-191129.log`<br>2025-11-19: `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt` |
| 6 | Release chunk bench + Grapheme telemetry ingestion（QA verification） | QA Engineer SubAgent | Replay Release chunk bench + Grapheme telemetry harness, persist bench/stdout/TRX artefacts, and append metrics to `[QA-ChunkBench]` / `[QA-Telemetry]`。 | C# Implementer（bench + telemetry harness）；Rust Porter（Grapheme traces / CLI windows）；Information Researcher（log indexing）。 | QA anchors re-state throughput + fallback thresholds; automation gating now references the same artefact bundle as `[QA-IngestionSmoke]` for cross-checks. | Complete | 2025-11-19: `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`<br>2025-11-19: `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry-20251118T191914Z.txt`<br>2025-11-19: `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx` |
| 7 | Stage D evidence index refresh | Information Researcher SubAgent | Gather `tests/xi.Core.Tests/Fixtures/Reports/{stage-d-refresh-*.log,stage-d-inspector-latest.txt,chunk-bench-latest.txt,grapheme-telemetry.trx}` and propagate them to `agents/information-researcher.md#证据快照`、`docs/operations/do-check-stage-d.md`、`docs/architecture/system-overview.md#[SO-Map]`；align `goal_tree_sync.py --check` metadata across docs。 | Architecture Mapper（Goal Tree diff + `generated-at` stamps）；QA Engineer（log bundle）；C# Implementer & Rust Porter（command write-ups）。 | All referenced docs anchor the refreshed artefacts, `goal_tree_sync.py --check` stays clean, and the Ready Queue entry closes the Stage D → QA → Evidence loop. | Complete | 2025-11-19: `agents/information-researcher.md#证据快照`<br>2025-11-19: `docs/operations/do-check-stage-d.md`<br>2025-11-19: `docs/architecture/system-overview.md#SO-Map` |

## Coordination Notes
**Execution Ledger（Ready Queue #1-#7）**
- **#1 – Status: Completed** (`[StageD::FixtureFlow]` · `[StageD::ParityAssets]` · `[MP-T1]` · `[RPM-Matrix]`): C# Implementer reran `python scripts/refresh_all_assets.py`（当时为 `--only stage-d-fixtures` 模式） producing `stage-d-refresh-20251118-184351.log` 与 `stage-d-inspector-latest.txt`，并把 `_editVersion ↔ NodeCursorState` 事实写回 `[MP-T1]` / `[RPM-Matrix]`，同步解锁 QA 的 #5。
- **#2 – Status: Completed** (`[QA-ChunkBench]` · `[QA-Telemetry]` · `[StageD::FeatureGates]`): Release chunk bench + ≥10k Grapheme telemetry artefact（`chunk-bench-latest.txt`、`grapheme-telemetry-20251118T191914Z.txt`、`.trx`）现作为 `[QA-ChunkBench]` / `[QA-Telemetry]` 的最新记录，支撑 Stage D feature gate 复查。
- **#3 – Status: Completed** (`[StageD::FixtureFlow]` · `[StageD::ParityAssets]` · `[Fixture-MetricWindows]`): Rust Porter 默认开启 exporter 并写入 `metric_windows[]` ledger（`stage-d-refresh-20251118-230526-porter.log`、`fixtures.manifest.json`），Architecture Mapper 与 QA Engineer 均已在 `AGENTS.md` 记账。
- **#4 – Status: Completed** (`[StageD::FeatureGates]` · `[StageD::ParityAssets]` · `[RPM-ParityAssets]`): CLI flag matrix + schema 片段已落位 `docs/csharp-refactor/rope-serialization-fixture-playbook.md` 与 `[Fixture-MetricWindows]`，`goal_tree_sync.py` (`goal-tree-sync-20251118-193607.log`) 重新盖章 anchors。
- **#5 – Status: Completed** (`[QA-IngestionSmoke]` · `[StageD::FixtureFlow]`): QA Engineer 依照 Stage D 手册执行 ingestion smoke（`stage-d-refresh-20251118-191129.log`）+ manifest 审计，并把 hash 列表写入 `[QA-IngestionSmoke]`，提醒 Rust Porter/Architecture Mapper 更新 ledger。
- **#6 – Status: Completed** (`[QA-ChunkBench]` · `[QA-Telemetry]` · `[SO-StageDQAEvidence]`): QA 回放 Release bench/telemetry，确认吞吐+fallback 阈值未漂移，并把 artefact 转交给 Architecture Mapper / Information Researcher 供系统图消费。
- **#7 – Status: Completed** (`[SO-Map]` · `agents/information-researcher.md#证据快照` · `docs/operations/do-check-stage-d.md`): Information Researcher 汇总 Stage D、QA、Telemetry artefact 成为统一 evidence index，关闭 Ready Queue #7 并向 AI Architect 广播。

**Ledger Cross-links & Follow-ups**
- `[MP-T1]` / `[RPM-Matrix]` / `[BP-GoalTree]` 现引用 Ready Queue #1 artefact，并由 `goal-tree-sync-20251118-193607.log` 记录生成时间；后续 NodeCursor/MetricAdapter 更新需继续沿用该 log。
- `[StageD::ParityAssets]` / `[StageD::FixtureFlow]` / `[StageD::FeatureGates]` 已吸收 Ready Queue #3+#4 的 CLI/ledger 描述；`metric_windows[]` schema 作为 `Fixture-MetricWindows` anchor 的唯一事实源。
- `[QA-IngestionSmoke]` / `[QA-ChunkBench]` / `[QA-Telemetry]` 捕获 #2+#5+#6 artefact 并在 QA 档案落地；信息研究员通过 #7 把同一套 artefact 拓展到系统图与操作手册。
- All Ready Queue items are now closed；下一步聚焦于 `StageDDescriptorManifest.MetricWindows` → `MetricAdapter` / QA anchor 的消费（详见 `docs/architecture/rope-port-mapping.md#[RPM-Actions]` 与 `agents/architecture-mapper.md#待办-/-风险`）。

**Completion Broadcast**
- #1 → QA Engineer、Architecture Mapper：在 `AGENTS.md#工作日志`（2025-11-19 Stage D manifest + inspector doc sync）登记录得 rerun 结果，并于 `docs/meetings/2025-11-19-knowledge-refresh-chat.md` 更新指令，提示 QA 承接 #5。
- #2 → QA Engineer、Information Researcher：C# Implementer 将 Release 基准落盘后，通过 `AGENTS.md#工作日志`（2025-11-19 Breaks/Diff/Search skeleton smoke ✅）与 `docs/meetings/2025-11-19-knowledge-refresh-chat.md` 通知两位角色复用 `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` / `grapheme-telemetry-*.txt`。
- #3 → Architecture Mapper、QA Engineer：Rust Porter 在 `AGENTS.md#工作日志`（2025-11-18 Stage D exporter 稳定化 + refresh_all_assets 全绿）广播 exporter 更新，并在 `agents/rust-porter.md` “最近完成”中抄送，让 QA 立即更新 `[StageD::FixtureFlow]`。
- #4 → AI Architect、Information Researcher：CLI schema 公布后，Architecture Mapper 把链接写回 `AGENTS.md#当前聚焦` 以及 `agents/information-researcher.md#证据快照`，由 AI Architect 在下一轮审阅中确认。
- #5 → Rust Porter、Architecture Mapper：QA Engineer 在 `AGENTS.md#工作日志`（2025-11-19 Stage D manifest + inspector doc sync）与 `docs/operations/do-check-stage-d.md` 中同步 ingestion smoke，提醒 Rust Porter manifest ledger 已与 `[QA-IngestionSmoke]` 对齐。
- #6 → C# Implementer、AI Architect：QA Engineer 于 `AGENTS.md#工作日志`（2025-11-20 Stage D refresh unblock via clippy fix）旁注入 Release telemetry 摘要，并在 `agents/qa-engineer.md` “最近完成”中 mention C# Implementer / AI Architect，确认门槛达标。
- #7 → Architecture Mapper、AI Architect、Information Researcher：证据索引落地后，通过 `AGENTS.md#工作日志`（2025-11-19 System Overview Map Launch）与 `agents/information-researcher.md#证据快照` 宣布完成，同时在 `docs/architecture/system-overview.md#[SO-Map]` 留下引用，方便 AI Architect 关闭 Sprint 1 循环。

### Risk & Next Cycle
- Ready Queue #1-#7 已全部完成，但 `StageDDescriptorManifest.MetricWindows` 目前只在 Rust manifest 记录，C# Hydrator / MetricAdapter 与 QA dashboards 尚未消费该 ledger；若 11/25 前仍未接入，`[TS-B2]` / `[QA-IngestionSmoke]` 将难以验证窗口阈值，且 `[RPM-Actions]` 难以自动化证据链。
- QA pipeline 依旧手动触发 `python scripts/refresh_all_assets.py`（偶尔附 `--only stage-d-fixtures`）与 Stage D CLI flag 组合，缺乏 CI 守卫与 `goal_tree_sync.py --check` 钩子；Sprint 2 需让 QA Engineer + Tooling 将 exporter flag 表与 StageDDescriptorInspector 报告纳入 nightly run，并把 artefact 上传到 `[StageD::FixtureFlow]`。
- 下一迭代聚焦三项：1) C# Hydrator 解析 `metric_windows[]` 并把计数暴露给 MetricAdapter/QA；2) QA automation 将 Stage D CLI / Inspector 纳入 `refresh_all_assets.py --check` 流程；3) AI Architect 对接 Stage D Metric Adapter + QA automation 的 owner/due（2025-11-21 例会确认）。

