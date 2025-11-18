---
Title: Sprint 1 Alignment Skeleton
Summary: Scaffold for aligning runSubAgent-ready work after the 2025-11-19 knowledge refresh checkpoint.
Status: Draft - awaiting role-specific population
Last-Updated: 2025-11-19
Owners:
  - Architecture Mapper
  - AI Architect
Related-Docs:
  - docs/meetings/2025-11-19-knowledge-refresh-chat.md
  - docs/architecture/document-structure-template.md
  - agents/architecture-mapper.md
---

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
| # | Task Name | runSubAgent Owner | Scope | Dependencies | Acceptance Criteria | Status |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Stage D rerun + `_editVersion ↔ NodeCursorState` doc alignment | C# Implementer SubAgent | Rerun `python scripts/refresh_all_assets.py --only stage-d-fixtures`, `dotnet test Xi.Editor.sln --filter StageDDescriptor`, and `StageDDescriptorInspector` to capture latest fixture + inspector logs; record refreshed descriptor hash into `docs/architecture/m3-implementation-plan.md#[MP-T1]` and `docs/architecture/rope-port-mapping.md#[RPM-Matrix]` | QA Engineer (Stage D fixture ledger + `[QA-IngestionSmoke]` hooks); Rust Porter (freeze cursor descriptor CLI schema); Automation scripts (`refresh_all_assets.py`, StageDDescriptorInspector) | Stage D rerun logs + inspector report attached under `tests/xi.Core.Tests/Fixtures/Reports/`; `[MP-T1]` and `[RPM-Matrix]` show new manifest hash + `_editVersion ↔ NodeCursorState` notes; Ready Queue comment links into `[StageD::ParityAssets]`/`[StageD::FixtureFlow]`; goal tree sync clean | Planned |
| 2 | Release chunk bench + ≥10k Grapheme telemetry drop | C# Implementer SubAgent | Execute `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj --configuration Release -- --stage-d --include-alloc-stats --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`, run Grapheme telemetry harness for ≥10k operations, persist artifacts under `tests/xi.Core.Tests/Fixtures/Reports/`, and sync `[QA-ChunkBench]`/`[QA-Telemetry]` anchors | QA Engineer (accept Release bench + telemetry bundle); Rust Porter (provide Grapheme trace/`--grapheme-windows` parity data); Benchmark harness + telemetry scripts (ensure alloc stats enabled) | `chunk-bench-latest.txt` Release report with allocation stats + Grapheme telemetry log stored under Reports; `[QA-ChunkBench]`/`[QA-Telemetry]` anchors updated with log paths + metrics; TRX or benchmark stdout captured for QA ingestion; goal tree references G2/G4/StageD::FeatureGates | Planned |
| 3 | Stage D exporter 默认开启 + metric_windows manifest 字段 | Rust Porter SubAgent | Update `xi-editor-ph7/rust/export-serde-fixtures` so cursor/chunk/grapheme/breaks/diff/search/tree trace descriptors default on, emit `metric_windows[]` (with schema version) into the manifest ledger, run `./run_all_checks --filter serde-fixtures`, then land the refreshed manifest in `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` and capture the new hash | QA Engineer (loader/hydrator verification), Architecture Mapper (refresh `[RPM-ParityAssets]` + Goal Tree nodes), C# Implementer (StageDDescriptorHydrator + MetricAdapter to read the new field) | Rust CLI + manifest changes committed, `stage-d-refresh-*.log` and `run_all_checks` logs show green, `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::ParityAssets]` updated with `metric_windows[]` hash reference | Planned |
| 4 | Stage D CLI schema & feature gate spec drop | Rust Porter SubAgent | Capture the canonical CLI flags and JSON schema, document them under `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::FeatureGates]` and `docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]`, and provide sample `metric_windows`/`cursor_state`/`tree_builder_slice_trace` snippets for QA + C# consumers | Architecture Mapper (align references), Information Researcher (index logs + schema snippets), C# Implementer (hydrator parsing adjustments) | Docs updated with the new paragraphs, schema snippets link back to the manifest, and `goal_tree_sync.py --check` passes with the refreshed references | Planned |
| 5 | Stage D ingestion smoke + manifest ledger audit | QA Engineer SubAgent | Execute `python scripts/refresh_all_assets.py --only stage-d-fixtures --continue-on-error`, `dotnet test Xi.Editor.sln --filter StageDDescriptor`, `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`, and `dotnet run --project tools/StageDDescriptorInspector`; capture every log/inspector output under `tests/xi.Core.Tests/Fixtures/Reports/` and confirm Rust Porter’s manifest hash matches the `[QA-IngestionSmoke]` ledger + `[StageD::FixtureFlow]` notes before closing the task | C# Implementer (loader/hydrator code + StageDDescriptorInspector), Rust Porter (CLI + manifest drop), Architecture Mapper (Goal Tree anchor alignment) | All Stage D commands succeed, logs + inspector ledger archived under Reports, Playbook anchors `[StageD::FixtureFlow]` & `[QA-IngestionSmoke]` refreshed with the new hash + ledger diff, Status set to `Planned` until QA sign-off | Planned |
| 6 | Release chunk bench + Grapheme telemetry ingestion (QA verification) | QA Engineer SubAgent | Replay `tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj` in Release with `--stage-d --include-alloc-stats`, drive ≥10k Grapheme telemetry operations using the C#/Rust harness instructions, persist bench/stdout/TRX artefacts inside `tests/xi.Core.Tests/Fixtures/Reports/`, and append the metrics to `[QA-ChunkBench]` / `[QA-Telemetry]` while recording fallback ratio + throughput gates | C# Implementer (bench + telemetry harness), Rust Porter (Grapheme traces/CLI windows), Information Researcher (log indexing for QA anchors) | QA anchors reflect the new Release data, TRX + bench logs paths documented and accepted by gating scripts, fallback ratio and throughput stay within thresholds, Status remains `Planned` until automation green | Planned |
| 7 | Stage D evidence index refresh | Information Researcher SubAgent | After C#/Rust/QA drops land, gather `tests/xi.Core.Tests/Fixtures/Reports/{stage-d-refresh-*.log,stage-d-inspector-latest.txt,chunk-bench-latest.txt,grapheme-telemetry.trx}`, propagate the references into `agents/information-researcher.md#证据快照`, `docs/operations/do-check-stage-d.md`, and `docs/architecture/system-overview.md#[SO-Map]`, and align the `goal_tree_sync.py --check` log id with each doc’s `generated-at` metadata | Architecture Mapper (Goal Tree diff + `generated-at` stamps), QA Engineer (log bundle), C# Implementer & Rust Porter (command/script write-ups) | All named docs anchor the refreshed artefacts, hyperlinks resolve, and `python scripts/goal_tree_sync.py --check` emits a clean report whose log id matches the documented `generated-at` values | Planned |

## Coordination Notes
**Execution Path & Triggers (Ready Queue #1-#7)**
- `#3` (Rust Porter) executes first so the exporter defaults/`metric_windows[]` manifest field land before any rerun; trigger: `export-serde-fixtures` CLI + `./run_all_checks --filter serde-fixtures` green and manifest hash recorded.
- `#4` (Rust Porter) immediately follows #3 to drop the CLI schema + gate spec snippets that C#/QA need; trigger: schema text + sample payloads pushed to `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::FeatureGates]`.
- `#1` (C# Implementer) starts once #3+#4 logs are available; rerun `python scripts/refresh_all_assets.py --only stage-d-fixtures` + Stage D descriptors, then backfill `[MP-T1]`/`[RPM-Matrix]`.
- `#2` (C# Implementer) can begin after #1 confirms pipelines are clean; run Release chunk bench + ≥10k Grapheme telemetry and stash artefacts for QA.
- `#5` (QA Engineer) kicks off once #1+#3 close, reusing the refreshed manifest and descriptor hydrator to perform ingestion smoke + ledger audit.
- `#6` (QA Engineer) depends on #2 outputs; replay Release bench + Grapheme telemetry to validate QA anchors and gate automation.
- `#7` (Information Researcher) is last; only start after #5+#6 deposit every log so the evidence index + Goal Tree references stay in sync.

**Dependency Ledger & Drop Locations**
- `[MP-T1]` / `[RPM-Matrix]` / `[BP-GoalTree]`: #1 owners must push `_editVersion ↔ NodeCursorState` + Stage D hydrator deltas into `docs/architecture/m3-implementation-plan.md#[MP-T1]` and `docs/architecture/rope-port-mapping.md#[RPM-Matrix]`, then notify Architecture Mapper to rerun `goal_tree_sync.py` so `<!-- goal-tree:meta ... -->` stays consistent.
- `[StageD::ParityAssets]` / `[StageD::FixtureFlow]` / `[StageD::FeatureGates]`: #3+#4+#5 must update `docs/csharp-refactor/rope-serialization-fixture-playbook.md` with the new manifest hash, CLI defaults, and loader/hydrator/inspector steps; CLI + inspector logs land under `tests/xi.Core.Tests/Fixtures/Reports/stage-d-*.log`.
- `[QA-IngestionSmoke]` / `[QA-ChunkBench]` / `[QA-Telemetry]`: #2+#5+#6 archive `stage-d-refresh-*.log`, `chunk-bench-latest.txt`, `grapheme-telemetry.trx`, and reference them inside the QA anchors plus `agents/qa-engineer.md`.
- `[RPM-ParityAssets]` / `[SO-Map]` / `agents/information-researcher.md#证据快照`: After #5+#6, #7 collects every artefact into the evidence index, mirrors the paths inside `docs/architecture/system-overview.md#[SO-Map]`, and stores the `goal_tree_sync.py --check` log id for audit.

**Completion Notifications (send via AGENTS.md log or direct ping)**
- `#1` → ping Architecture Mapper + QA Engineer so ledger/doc updates can be reviewed and QA smoke (#5) can start.
- `#2` → ping QA Engineer + Information Researcher because QA needs the bench artefacts for #6 and Info Researcher must plan indexing.
- `#3` → ping Architecture Mapper + QA Engineer to signal new manifest + metric windows landed (QA reuses the hash; Architecture Mapper updates `[RPM-ParityAssets]`).
- `#4` → ping Architecture Mapper + C# Implementer since schema snippets drive hydrator adjustments.
- `#5` → ping Architecture Mapper + Rust Porter; Mapper confirms `[StageD::FixtureFlow]` entries, Rust Porter verifies exporter assumptions.
- `#6` → ping Architecture Mapper + Information Researcher so QA anchors and evidence index capture throughput/fallback gates.
- `#7` → ping Architecture Mapper + AI Architect once the evidence roll-up + `goal_tree_sync.py --check` summary is committed, closing the Sprint 1 coordination loop.

