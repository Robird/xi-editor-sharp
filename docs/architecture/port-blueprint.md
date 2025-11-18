# Xi.Editor Port Blueprint

> **Scope**: Single source of truth for Xi.Editor rope-port goals, cross-role dependencies, and risk posture.
> **Owner**: Architecture Mapper
> **Update Frequency**: After each milestone kickoff or goal-tree edit.
> **Reviewers**: AI Architect · C# Implementer · Rust Porter · QA Engineer
> **Anchor Prefix**: BP
> **Last Synced Goal Tree**: 2025-11-18 (manual sync with `m3-implementation-plan.md`)

---

## [BP-GoalTree] Goal Tree Snapshot
<a id="BP-GoalTree"></a>
<!-- goal-tree:start -->
<!-- goal-tree:meta generated-at="2025-11-18T02:24:44.386912+00:00" source="docs/architecture/templates/goal-tree.yaml" checksum="054fa092b7533028d0a255c653a0ad585fb9a9b35a76d2fee81c604b6a55afd6" -->
| ID | Title | Status | Due | Owner | Next | QA / Stage D |
| --- | --- | --- | --- | --- | --- | --- |
| G1 | Cursor descriptors + version tickets | ⚠️ Watch | 2025-11-22 | C# Implementer | Freeze CLI schema + rerun [MP-T1] parity ingestion | [QA-IngestionSmoke] (Stage D smoke must ingest cursor_descriptors manifest once CLI export lands) · [StageD::ParityAssets] |
| G2 | Chunk/Line diagnostics + fixtures | ⚠️ Watch | 2025-11-23 | C# Implementer · QA Engineer | Import chunk JSON + record [MP-T3] baseline | [QA-ChunkBench] (Capture 1 MB baseline with diagnostics counters) · [StageD::ParityAssets] |
| G3 | Breaks tree bridge | ⚠️ Watch | 2025-11-26 | C# Implementer · Rust Porter | Publish shim draft + refresh [TS-B5] card | [StageD::FeatureGates] |
| G4 | Diff/Search plan handoff | ⏳ Pending | 2025-11-28 | AI Architect · C# Implementer | Ship doc skeleton + stub directories | [QA-StageDManual] (Manual validation plan required before fixtures ship) · [StageD::FixtureFlow] |
| G5 | Iterator façade + CLI alignment | ⏳ Pending | 2025-11-27 | Rust Porter · Architecture Mapper | Approve export matrix + update Stage D script | [QA-IngestionSmoke] (Smoke scripts must pass once CLI flags converge) · [StageD::FixtureFlow] |
| G6 | Metric adapter bridge | ⚠️ Watch | 2025-11-24 | C# Implementer · Architecture Mapper | Land adapter tests + document [TS-B2] dependency | [QA-StageDManual] (QA to confirm adapter instrumentation via manual Stage D checklist) · [StageD::FeatureGates] |
<!-- goal-tree:end -->
> Snippet mirrors `m3-implementation-plan.md#[MP-GoalTree]`; update both blocks together until `goal_tree_sync.py` lands.

## [BP-Milestones] Milestones & Dependencies
<a id="BP-Milestones"></a>
- **Cursor system runway** (`[MP-T1]` ↔ `[TS-B1]`): code complete behind version tickets; only missing the `--cursor-descriptors` CLI + Stage D hooks. Blocked items must reference `[StageD::ParityAssets]` instead of copying commands.
- **Chunk + Line diagnostics** (`[MP-T3]` ↔ `[TS-B3]`): enumerators ship with telemetry, but ingestion and 1 MB baseline stay open until `[QA-ChunkBench]` captures the new assets. Keep dependency chain explicit in `rope-port-mapping.md` to avoid silent drift.
- **Metric & Generic bridge** (`[MP-T1]` maintenance ↔ `[TS-B2]`): `MetricAdapter` is the gating artifact for switching to `RopeNode` in M4; doc all breaking changes under `[StageD::FeatureGates]` to keep CLI expectations clear.
- **Diff/Search runway** (G4 ↔ `[TS-B5]`): publish the design doc + skeleton directories before 11/28 so QA can plan future fixtures. No duplicate CLI descriptions—link back to `[StageD::FixtureFlow]` once the iterator façade spec is approved.

## [BP-RiskTable] Risk & Watchlist
<a id="BP-RiskTable"></a>
| Risk | Trigger | Status | Mitigation |
| --- | --- | --- | --- |
| `[MP-R8]` Rope version ticket not captured by Stage D | Stage D scripts omit `_editVersion` metadata | Watch | Document the hook in `[StageD::FixtureFlow]` and block Goal G1 sign-off until QA confirms ingestion smoke.
| `[MP-R9]` Export CLI flags slip past 11/19 | `export-serde-fixtures` lacks cursor/chunk/grapheme switches | Watch | Mirror Rust PR status inside `[TS-B1]`/`[TS-B3]` and escalate to Architect if CLI isnt demoed by the checkpoint.
| `[MP-R10]` Chunk/Line telemetry never baselined | No `[QA-ChunkBench]` entry after ingest | Watch | Require QA to log 1 MB benchmark + telemetry screenshot before closing G2, otherwise keep T3 tasks open.

## [BP-ChangeLog] Change Log
<a id="BP-ChangeLog"></a>
- **2025-11-18 – Template rollout**: added shared front matter, goal-tree snippet, milestone/risk anchors, and Stage D/QA cross-links; superseded narrative sections with references to `rope-port-mapping.md` + `m3-implementation-plan.md`.
- **2025-11-17 – Diagnostic sync reminder**: carried forward G1-G3 dependency notes while waiting for CLI schema demos (pre-template).

[MP-GoalTree]: m3-implementation-plan.md#mp-goaltree
[MP-T1]: m3-implementation-plan.md#21-任务-1游标系统实现
[MP-T3]: m3-implementation-plan.md#23-任务-3chunk-迭代器骨架
[MP-R8]: m3-implementation-plan.md#r8
[MP-R9]: m3-implementation-plan.md#r9
[MP-R10]: m3-implementation-plan.md#r10
[TS-B1]: type-system-migration-log.md#ts-b1
[TS-B2]: type-system-migration-log.md#ts-b2
[TS-B3]: type-system-migration-log.md#ts-b3
[TS-B5]: type-system-migration-log.md#ts-b5
[QA-IngestionSmoke]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-IngestionSmoke
[QA-ChunkBench]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-ChunkBench
[QA-StageDManual]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-StageDManual
[StageD::ParityAssets]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::ParityAssets
[StageD::FeatureGates]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::FeatureGates
[StageD::FixtureFlow]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::FixtureFlow
