# Xi.Editor Port Blueprint

> **Scope**: Single source of truth for Xi.Editor rope-port goals, cross-role dependencies, and risk posture.
> **Owner**: Architecture Mapper
> **Update Frequency**: After each milestone kickoff or goal-tree edit.
> **Reviewers**: AI Architect · C# Implementer · Rust Porter · QA Engineer
> **Anchor Prefix**: BP
> **Last Synced Goal Tree**: 2025-11-18 19:26 UTC (`goal_tree_sync.py` · log `tests/xi.Core.Tests/Fixtures/Reports/goal-tree-sync-20251118-192655.log`)

---

## [BP-GoalTree] Goal Tree Snapshot
<a id="BP-GoalTree"></a>
<!-- goal-tree:start -->
<!-- goal-tree:meta generated-at="2025-11-18T20:35:37.473959+00:00" source="docs/architecture/templates/goal-tree.yaml" checksum="7a47156ffd7e47c1f5e0a2a6f2b489b40ed9f687200e1479aab8194dde1b7df4" payload-hash="9c7163ebd6ceb7f9e7e4ec566682470e96e27424499c4a6f5804898eca48f1f0" inspector-report="tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt" chunk-report="tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt" -->
| ID | Title | Status | Due | Owner | Next | QA / Stage D |
| --- | --- | --- | --- | --- | --- | --- |
| G1 | Cursor descriptors + version tickets | ⚠️ Watch | 2025-11-22 | C# Implementer | Freeze CLI schema + rerun [MP-T1] parity ingestion | [QA-IngestionSmoke] (Inspector log tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt (rust_commit=3799d2be)) · [StageD::ParityAssets] (fixtures.manifest.json#cursor payload hash 9b46bd8e29042e38c36556afc4054a5a2c4a6cfa405b5bbd738ca1909f8a4d38) |
| G2 | Chunk/Line diagnostics + fixtures | ⚠️ Watch | 2025-11-23 | C# Implementer · QA Engineer | Import chunk JSON + record [MP-T3] baseline | [QA-ChunkBench] (Report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt (2025-11-18 replay: 4.30 ms chunk / 3.33 ms line, hash 69ba7f25…, alloc 37,944 B)) · [QA-Telemetry] (Telemetry trx tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx references grapheme hash eb0c7c66…) · [StageD::ParityAssets] (fixtures.manifest.json#chunk + #grapheme entries share inspector log evidence) |
| G3 | Breaks tree bridge | ⚠️ Watch | 2025-11-26 | C# Implementer · Rust Porter | Publish shim draft + refresh [TS-B5] card | [QA-IngestionSmoke] (Inspector log now records Breaks ledger + wrap span metrics) · [StageD::FeatureGates] (serde export emits breaks_descriptors with hash 5d37a7313730dd0ae9c292ca45daa442c11fe63d45f9ad21bad664f919c13f86) · [StageD::ParityAssets] (fixtures.manifest.json#breaks backs the hydrator smoke) |
| G4 | Diff/Search plan handoff | ⏳ Pending | 2025-11-28 | AI Architect · C# Implementer | Ship doc skeleton + stub directories | [QA-StageDManual] (Manual validation must cite inspector log diff/search ledger) · [StageD::FixtureFlow] (export-serde-fixtures --diff-regions --search-spans --emit-manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json) |
| G5 | Iterator façade + CLI alignment | ⏳ Pending | 2025-11-27 | Rust Porter · Architecture Mapper | Approve export matrix + update Stage D script | [QA-IngestionSmoke] (Single exporter command now emits manifest + chunk hash 69ba7f2536876ad3a76296a215ac163fa82629934a97a82e49faa25423f9415a) · [StageD::FixtureFlow] (refresh_all_assets.py --only stage-d-fixtures stitches exporter + loader + inspector) |
| G6 | Metric adapter bridge | ⚠️ Watch | 2025-11-24 | C# Implementer · Architecture Mapper | Land adapter tests + document [TS-B2] dependency | [QA-StageDManual] (QA to confirm adapter instrumentation via manual Stage D checklist) · [StageD::FeatureGates] (serde + cursor_state gates stay enabled while metric adapter wiring lands) |
<!-- goal-tree:end -->
> Snippet mirrors `m3-implementation-plan.md#[MP-GoalTree]`; update both blocks together until `goal_tree_sync.py` lands。Stage D 证据链默认由 `python scripts/refresh_all_assets.py`（无参数）生成：脚本会依次运行 `rust-skeletons → dotnet-build → stage-d-fixtures → verify-stage-d → goal-tree → ilspy → Skeletonizer`，并在 `tests/xi.Core.Tests/Fixtures/Reports/` 下覆盖 `stage-d-inspector-latest.txt`、`chunk-bench-latest.txt`、`grapheme-telemetry-*.txt/.trx`，这些 artefact 再被 `goal_tree_sync.py` 写入本表的 QA/Stage D 列。

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
