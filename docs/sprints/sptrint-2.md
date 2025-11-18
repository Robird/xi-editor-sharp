---
Title: Sprint 2 Execution Intake
Summary: Draft backlog and Ready Queue items derived from the 2025-11-20 doc-sync chat, scoped for runSubAgent-sized tasks.
Status: In Draft – awaiting role contributions
Last-Updated: 2025-11-20T00:00Z
Owners:
  - AI Architect
  - Architecture Mapper
Related-Docs:
  - docs/meetings/2025-11-20-doc-sync-chat.md
  - docs/sprints/sptrint-1.md
  - AGENTS.md
---

## Sprint Goal & Window
- **Goal**: Convert the open coordination items (Stage D rerun unblock, metric windows ingestion, QA automation, Goal Tree guard rails) into runSubAgent-ready tasks with clear owners, evidence, and success signals.
- **Window**: 2025-11-20 → 2025-11-27 (aligns with Ready Queue #1-#7 completion recap and the next Goal Tree/Stage D checkpoints).
- **Success Metrics**:
  1. Every task cites upstream Goal Tree / `[StageD::*]` / `[QA-*]` anchors before kickoff.
  2. Ready Queue only contains fully specified runSubAgent missions (single owner, <= 1 day).
  3. Coordination Notes updated within 24h when dependencies change, keeping `goal_tree_sync.py --check` noise-free.

## Intake Notes
- Prefer referencing artefacts under `tests/xi.Core.Tests/Fixtures/Reports/` for reproducibility.
- Mention expected commands/scripts (e.g., `python scripts/refresh_all_assets.py --only stage-d-fixtures`) so hand-offs are automation-ready.
- Include evidence drop locations so Information Researcher can refresh indices without extra clarification.

## Backlog Candidates
| # | Candidate Task | Proposed Owner | Scope Snapshot | Source / Anchor |
| --- | --- | --- | --- | --- |
| RP1 | `cursor_descriptors@1.3.0` schema drop | Rust Porter SubAgent | Finalize new Breaks metric + UTF16 invalidation fields, emit updated CLI hashes + manifest rows, and produce a migration note for `[StageD::ParityAssets]` / `CursorCache.md`. | `docs/meetings/2025-11-20-doc-sync-chat.md#Rust Porter`; `agents/rust-porter.md#待办 / 风险` |
| CI1 | MetricAdapter ingestion design | C# Implementer SubAgent | Define how `StageDDescriptorHydrator` emits `metric_windows[]` DTOs and how `MetricAdapter` consumes them, including smoke tests + doc updates for `[RPM-Matrix]`. | `docs/meetings/2025-11-20-doc-sync-chat.md#C# Implementer`; `agents/csharp-implementer.md#当前聚焦` |

## Ready Queue (Draft)
| # | Task Name | runSubAgent Owner | Scope | Dependencies | Acceptance Criteria | Status | Evidence Plan |
| :-: | --- | --- | --- | --- | --- | :-: | --- |
| 1 | Stage D rerun unblock | C# Implementer SubAgent | Patch `python scripts/refresh_all_assets.py --only stage-d-fixtures` failure, rerun exporter + loader + hydrator + manifest verifier + inspector, and refresh `[QA-IngestionSmoke]` references. | Rust Porter (CLI defaults & schema), QA Engineer (log acceptance), Information Researcher (evidence index). | Script runs to completion; new `stage-d-refresh-YYYYMMDD-*.log`, `stage-d-inspector-latest.txt`, and manifest hashes recorded in `[StageD::FixtureFlow]` / `[QA-IngestionSmoke]`. | Planned | Drop logs under `tests/xi.Core.Tests/Fixtures/Reports/` with UTC timestamps + mention in meeting notes. |
| 2 | `metric_windows[]` hydrator wiring | C# Implementer SubAgent | Extend `StageDDescriptorHydrator` + DTOs so chunk/grapheme/breaks/diff/search/tree trace windows hydrate into MetricAdapter-ready structures, with tests verifying manifest counts. | Rust Porter (manifest schema / hashes), QA Engineer (smoke logging), Architecture Mapper (`[RPM-Matrix]` / `[StageD::ParityAssets]`). | Hydrator + tests merged; `[QA-IngestionSmoke]` references new counts; `[RPM-Matrix]` lists DTO coverage; `MetricAdapter` design doc updated. | Planned | `dotnet test Xi.Editor.sln --filter StageDDescriptorHydratorTests`; updated docs referencing manifest snapshot. |
| 3 | Stage D ingestion refresh (QA) | QA Engineer SubAgent | Re-execute ingestion smoke (loader/hydrator/inspector + manifest verifier) after rerun, push artefacts + TRX into `[QA-IngestionSmoke]`, `[QA-ChunkBench]`, `[QA-Telemetry]`, ensuring <7 天窗口。 | C# Implementer (new logs), Information Researcher (indexing), Architecture Mapper (Goal Tree anchors)。 | `[QA-*]` anchors updated with 2025-11-2x artefacts, manifest hash matches ledger, chunk bench + telemetry logs refreshed。 | Planned | `tests/xi.Core.Tests/Fixtures/Reports/{stage-d-refresh-*,chunk-bench-latest.txt,grapheme-telemetry-*.txt,.trx}` + QA logbook。 |
| 4 | Goal Tree & Stage D anchor guard | Architecture Mapper SubAgent | Wire `python scripts/goal_tree_sync.py --check` (and nightly `--update` when needed) into `scripts/refresh_all_assets.py`, document process, and capture drift alerts in `[BP-GoalTree]`/`[MP-GoalTree]`. | AI Architect (approval), Information Researcher (log storage), Tooling scripts。 | Nightly job or documented command ensures Goal Tree + Stage D anchors share the latest manifest snapshot; Coordination Notes updated with alert workflow。 | Planned | `goal-tree-sync-YYYYMMDD-HHMMSS.log` stored under `scripts/logs/` + references in Blueprint / Implementation Plan。 |
| 5 | Evidence index refresh & distribution | Information Researcher SubAgent | Once new Stage D/QA artefacts land, propagate log paths into `agents/information-researcher.md#证据快照`、`docs/operations/do-check-stage-d.md`、`docs/architecture/system-overview.md#[SO-Map]`，并 append sprint notes。 | C# Implementer / QA Engineer (provide artefacts), Architecture Mapper (anchors), AI Architect (coordination)。 | Evidence tables reference the same 2025-11-2x artefacts across all documents; sprint log records timestamps + owners。 | Planned | Link each artefact path + sha256 into the evidence sections + meeting file。 |

## Coordination Notes (Draft)
- **Pipeline Order**: #1 _(C# Implementer)_ must land before #3 _(QA Engineer)_ and #5 _(Information Researcher)_; Architecture Mapper (#4) watches Goal Tree drift during/after rerun.
- **Schema & Design Sync**: Rust Porter backlog `RP1` feeds Ready Queue #2/#3 once field names + hashes are confirmed; Architecture Mapper collects the schema note for `[StageD::ParityAssets]`.
- **Evidence Windows**: QA keeps artefact freshness <7 天; if rerun slips past 11/25, AI Architect to trigger contingency runSubAgent requests.
