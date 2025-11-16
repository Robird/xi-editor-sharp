# Rope 文件级映射与类型翻译计划

> **Scope**: Keep a compressed Rust↔C# file/type matrix plus the parity-assets ledger for Xi.Editor Rope.
> **Owner**: Architecture Mapper
> **Update Frequency**: Whenever a module state changes or a new Stage D asset lands.
> **Reviewers**: AI Architect · C# Implementer · Rust Porter · QA Engineer
> **Anchor Prefix**: RPM
> **Last Synced Goal Tree**: 2025-11-18 (aligned with `[BP-GoalTree]`)

---

## [RPM-Matrix] File & Type Matrix
<a id="RPM-Matrix"></a>
| Module | Rust Source | C# Target | Status | Notes |
| --- | --- | --- | --- | --- |
| Node / SharedNode / TreeBuilder | `rope/tree.rs` | `Tree/Node*.cs`, `TreeBuilder.cs`, `Tree/LeafSplitter.cs`, `Tree/StringLeafOperations.cs` | Implementing | SharedNode helpers + `_editVersion` shipped; remaining work is hooking `MetricAdapter` (`[TS-B2]`) before the M4 generic switch. |
| Cursor stack | `rope/tree.rs::Cursor` | `Tree/NodeCursor.cs`, `tests/.../CursorDescriptorParityTests.cs` | Active | Version tickets + 11/11 JSON parity green; CLI schema + Stage D manifest pending `[TS-B1]`. |
| Rope core & metrics | `rope/rope.rs` | `Rope.cs`, `RopeInfo.cs`, `Metrics.cs`, `IMetric.cs` | Implementing | `StringLeafOperations` + `BreaksMetricHelper` landed; conversion shim still mirrors Rust via `[MP-T1]`. |
| Delta / Transformer | `rope/delta.rs` | `Rope/Delta*.cs` | Implementing | Stage B JSON parity lives in `[StageD::ParityAssets]`; `Transformer`/`factor` tracked under `[MP-T1]`. |
| Engine / Undo ledger | `rope/engine.rs` | `Rope/Engine*.cs` | Done | Stage C fixtures frozen; no open TODO beyond perf tuning. |
| Chunk + Line enumerators | `rope/rope.rs::ChunkIter`, `Lines*` | `RopeChunkEnumerator*.cs`, `RopeLineEnumerator*.cs`, diagnostics harness | Diagnostics only | Copy-on-read enumerators emit telemetry; awaiting `--chunk-descriptors` ingestion + `[QA-ChunkBench]` baseline (`[TS-B3]`). |
| Grapheme navigation | `rope/rope.rs::GraphemeCursor` | `Navigation/DegradedGraphemeNavigator.cs`, `GraphemeNavigationMetrics.cs` | Degraded (monitor) | Surrogate-safe fallback accepted (see `[Div-Active]`); telemetry piped into `[QA-Telemetry]`. |
| Breaks tree bridge | `rope/breaks.rs` | `Rope/BreaksMetricHelper.cs` + planned `Rope/Breaks/` | TODO | Helper exists but tree integration + CLI schema live in `[TS-B5]`; no fixtures exported yet. |
| Diff / Compare | `rope/diff.rs`, `rope/compare.rs` | Planned `src/xi.Core/Diff/` | Planned | Will unlock once G4 plan publishes; currently referenced only in `m3-implementation-plan.md` §1.7. |
| Search / Spans | `rope/find.rs`, `rope/spans.rs` | Planned `src/xi.Core/Search/` | Planned | Depends on Diff+Cursor maturity; until then point consumers to Rust helpers.

> Remaining helper/details (Subset, Interval, serde DTOs, CLI commands) live in `docs/csharp-refactor/rope-serialization-fixture-playbook.md` and `[StageD::ParityAssets]`.

## [RPM-ParityAssets] Parity Assets & Stage D Hooks
<a id="RPM-ParityAssets"></a>
| Asset | Path | Status | Source / Notes |
| --- | --- | --- | --- |
| Serde baseline (Subset/Delta/Engine) | `tests/xi.Core.Tests/Fixtures/{subset,delta,engine}_serialization/` | ✅ Current | Refreshed via `scripts/refresh_serialization_fixtures.ps1`; manifests tracked under `[StageD::ParityAssets]`. |
| Cursor descriptors | `tests/xi.Core.Tests/Fixtures/cursor_descriptors/` | ⚠️ Awaiting CLI schema | Hand-authored JSON unblocks tests; replace with `export-serde-fixtures --cursor-descriptors` output once `[TS-B1]` closes. |
| Chunk descriptors | `tests/xi.Core.Tests/Fixtures/chunk_descriptors/` | ⚠️ Diagnostics only | Requires Rust CLI demo + `[QA-ChunkBench]` ingestion per `[TS-B3]`. |
| Grapheme windows | `tests/xi.Core.Tests/Fixtures/grapheme_descriptors/` | ⚠️ Pending export | Stub directory only; telemetry relies on `GraphemeNavigationMetrics` until `[TS-B4]` finishes CLI work. |
| Stage D scripts | `scripts/refresh_serialization_fixtures.ps1` | ⏳ Updates planned | Needs iterator façade flag wiring per G5 / `[StageD::FixtureFlow]`. |

## [RPM-Actions] Open Actions
<a id="RPM-Actions"></a>
1. **Lock cursor CLI schema** – `[TS-B1]` owner to land the Rust exporter, then update Stage D playbook and link manifests from `[StageD::ParityAssets]`.
2. **Import chunk descriptors + log benchmark** – Once Rust Porter provides JSON, QA must append a `[QA-ChunkBench]` entry and C# tests should consume the same manifest (ties to `[MP-T3]`).
3. **Publish MetricAdapter design** – `[TS-B2]` requires a concrete adapter test plus documentation so `RopeNode` can merge in M4; blueprint milestone G6 stays blocked otherwise.
4. **Document Breaks/Diff/Search skeletons** – Architecture Mapper + AI Architect to outline directories + CLI expectations (Goal G3/G4, `[TS-B5]`), referencing `[StageD::FixtureFlow]` for execution order.
5. **Update design-divergence anchors** – As chunk/grapheme assets land, revisit `[Div-Active]` to either close or restate the degradation limits.

## [RPM-ChangeLog] Change Log
<a id="RPM-ChangeLog"></a>
- **2025-11-18 – Template rollout**: collapsed the full matrix to the ten critical modules, pushed the rest into Stage D references, added `[RPM-ParityAssets]`/`[RPM-Actions]` anchors, and recorded cursor/chunk/grapheme asset status per `[TS-B1]`/`[TS-B3]`/`[TS-B4]`.
- **2025-11-17 – Diagnostics reminder**: captured `_editVersion` + telemetry notes (legacy entry, retained for context).

[BP-GoalTree]: port-blueprint.md#bp-goaltree
[TS-B1]: type-system-migration-log.md#ts-b1
[TS-B2]: type-system-migration-log.md#ts-b2
[TS-B3]: type-system-migration-log.md#ts-b3
[TS-B4]: type-system-migration-log.md#ts-b4
[TS-B5]: type-system-migration-log.md#ts-b5
[MP-T3]: m3-implementation-plan.md#23-任务-3chunk-迭代器骨架
[Div-Active]: design-divergence-log.md#div-active
[QA-ChunkBench]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-ChunkBench
[QA-Telemetry]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-Telemetry
[StageD::ParityAssets]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::ParityAssets
[StageD::FeatureGates]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::FeatureGates
[StageD::FixtureFlow]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::FixtureFlow
