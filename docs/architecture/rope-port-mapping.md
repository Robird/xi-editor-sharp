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
| Module | Rust Source | C# Target | Status | Skeleton Coverage | Notes |
| --- | --- | --- | --- | --- | --- |
| Node / SharedNode / TreeBuilder | `rope/tree.rs` | `Tree/Node*.cs`, `TreeBuilder.cs`, `Tree/LeafSplitter.cs`, `Tree/StringLeafOperations.cs`, `Tree/TreeBuilderTracer.cs`, `Diagnostics/TreeBuilder/TreeBuilderSliceTraceLoader.cs` | Implementation (slice trace loader ready) | Rust skeleton exposes `Node<N,L>`, `SharedNode`, `TreeBuilderTracer`, `TreeBuilderEventKind`; C# ships `TreeBuilderTracer/EventKind/Event/ITreeBuilderTracer` + `NoOpTreeBuilderTracer` and now hydrates parity traces via `TreeBuilderSliceTraceLoader` + tests. | SharedNode helpers + `_editVersion` + tracer scaffolding exist, and `TreeBuilderSliceTraceLoaderTests` prove we can read `ParityFixtures/tree_builder_trace/basic_slice_plan.json`; remaining work is injecting the tracer through `TreeBuilder` + `MetricAdapter` (`[TS-B2]`) and replaying real Rust CLI traces once `[StageD::FixtureFlow]` exports them. |
| Cursor stack | `rope/tree.rs::Cursor` | `Tree/NodeCursor.cs`, `tests/.../CursorDescriptorParityTests.cs` | Active | Rust skeleton lists `Cursor`, `CursorState`, `CursorDescriptorFixture`, `DescriptorMetric`; C# has `NodeCursor`, parity tests, yet no descriptor DTO/exporter equivalents. | `_editVersion` + invalidation probes remain covered by `CursorDescriptorParityTests` (`dotnet test Xi.Editor.sln -v m` -> 169/169); Stage D manifest (11 entries, payload hash `fe963d909d5c…`) was minted on 2025-11-17 via `python scripts/refresh_all_assets.py --only stage-d-fixtures`, so `[QA-IngestionSmoke]` ✅ and focus shifts to canonical hash automation before the CLI schema evolves (`[Fixture-Manifest]`). |
| Rope core & metrics | `rope/rope.rs` | `Rope.cs`, `RopeInfo.cs`, `Metrics.cs`, `IMetric.cs` | Implementing | Rust keeps `helpers/string_leaf.rs`, `metrics/*`, `BreaksLeaf/BreaksInfo`; C# only ships `StringLeafOperations` + `BreaksMetricHelper`, missing `Breaks` node skeleton (`BreakBuilder`, `BreaksLeaf`). | `StringLeafOperations` + `BreaksMetricHelper` landed; conversion shim still mirrors Rust via `[MP-T1]`. |
| Delta / Transformer | `rope/delta.rs` | `Rope/Delta*.cs` | Implementing | Rust skeleton includes `Delta`, `DeltaElement`, `Transformer`, `SubsetFactor`; C# has `Delta<TInfo,TLeaf>`/`DeltaJson` but no `Transformer`/`Factor` chain or subset builder parity. | Stage B JSON parity lives in `[StageD::ParityAssets]`; `Transformer`/`factor` tracked under `[MP-T1]`. |
| Engine / Undo ledger | `rope/engine.rs` | `Rope/Engine*.cs` | Done | Rust engine skeleton exposes `Engine`, `Rev`, `Contents`, `UndoGroup`, `gc` helpers; C# implements `Engine`, `Revision*`, `EngineJson` but still lacks merging/gc APIs beyond serialization playback. | Stage C fixtures frozen; no open TODO beyond perf tuning. |
| Chunk + Line enumerators | `rope/rope.rs::ChunkIter`, `Lines*` | `RopeChunkEnumerator*.cs`, `RopeLineEnumerator*.cs`, `Rope/Diagnostics/Descriptors/{ChunkDescriptor,LineDescriptor,StageDDescriptorLoader}.cs` | Implementation (loader ready) | Rust skeleton covers `ChunkIter`, `LinesRaw`, `ChunkDescriptor`, `LineDescriptor` exporter structs; `export-serde-fixtures` keeps manifest-backed parity (chunk hash `bd863f2237dd…`, grapheme hash `a2b84031c5aa…`). C# now mirrors the descriptor DTOs *and* ships `StageDDescriptorLoader` + manifest DTOs so chunk/line/grapheme descriptors hydrate directly from `fixtures.manifest.json`. | Copy-on-read enumerators + telemetry counters remain the downgrade path; loader coverage lives in `tests/xi.Core.Tests/Diagnostics/StageDDescriptorLoaderTests.cs` and proves metadata/chunk/grapheme ingestion, but `[TS-B3]` still tracks wiring the loader into `[QA-IngestionSmoke]`/Stage D CLI before `[QA-ChunkBench]` can log the 1 MB baseline. |
| Grapheme navigation | `rope/rope.rs::GraphemeCursor` | `Navigation/DegradedGraphemeNavigator.cs`, `GraphemeNavigationMetrics.cs`, `Rope/Diagnostics/Descriptors/GraphemeDescriptor.cs` | Degraded (monitor) | Rust exposes `unicode_segmentation::GraphemeCursor`, `GraphemeDescriptor`, exporter structs; C# now mirrors the descriptor DTOs but still lacks iterator trace ingestion + loader wiring. | Surrogate-safe fallback accepted (see `[Div-Active]`); Stage D manifest lists 668 grapheme windows (hash `a2b84031c5aa…`) and the C# DTOs mirror them—next up is enabling loader ingestion + CLI trace capture so `[QA-Telemetry]` can diff Rust vs degraded mode (`[TS-B3]`, `[TS-B4]`). |
| Breaks tree bridge | `rope/breaks.rs` | `Rope/BreaksMetricHelper.cs` + planned `Rope/Breaks/` | TODO | Rust skeleton includes `BreaksLeaf`, `BreaksInfo`, `BreaksMetric`, `BreakBuilder`; C# currently exposes only helpers (no tree types). | Helper exists but tree integration + CLI schema live in `[TS-B5]`; no fixtures exported yet. |
| Diff / Compare | `rope/diff.rs`, `rope/compare.rs` | Planned `src/xi.Core/Diff/` | Planned | Rust skeleton ready (`DiffBuilder`, `CompareConfig`); C# has no directories yet—mapping tracked for Goal G4. | Will unlock once G4 plan publishes; currently referenced only in `m3-implementation-plan.md` §1.7. |
| Search / Spans | `rope/find.rs`, `rope/spans.rs` | Planned `src/xi.Core/Search/` | Planned | Rust skeleton provides `Finder`, `SearchOptions`, `Spans`, `SpanBuilder`; C# lacks any counterpart. | Depends on Diff+Cursor maturity; until then point consumers to Rust helpers. |

> Remaining helper/details (Subset, Interval, serde DTOs, CLI commands) live in `docs/csharp-refactor/rope-serialization-fixture-playbook.md` and `[StageD::ParityAssets]`.

## [RPM-ParityAssets] Parity Assets & Stage D Hooks
<a id="RPM-ParityAssets"></a>
| Asset | Path | Status | Source / Notes |
| --- | --- | --- | --- |
| Serde baseline (Subset/Delta/Engine) | `tests/xi.Core.Tests/Fixtures/{subset,delta,engine}_serialization/` | ✅ Current | Refreshed via `scripts/refresh_serialization_fixtures.ps1`; `dotnet test Xi.Editor.sln -v m` -> 169/169 gets attached to `[QA-IngestionSmoke]` whenever these change. |
| Cursor descriptors | `tests/xi.Core.Tests/Fixtures/cursor_descriptors/` | Manifest-backed ✅ (2025-11-17) | Refreshed via `python scripts/refresh_all_assets.py --only stage-d-fixtures`; manifest records `count=11`, `payload_hash=fe963d909d5c4e225bfd6e0c7085def006dccfadde7da8f5ea15a574a1483375`, `schema_hash=cursor_descriptors@1.1.0`, `rust_commit=7ac917a05be4bb526844d5cdaa842030411800e5`, `cli_rev=0.3.0`, `feature_gates=["serde"]`. `[QA-IngestionSmoke]` ✅ and `[TS-B1]` can now focus on schema forward-compat. |
| Chunk descriptors | `tests/xi.Core.Tests/Fixtures/chunk_descriptors/` | Manifest-backed ✅ (2025-11-17) | Same refresh produced 20 chunk windows (`payload_hash=bd863f2237ddede207a234318a785271a975604da299fc19247aae179d08dccb`, `schema_hash=chunk_descriptors@1.0.0`); action item is wiring `[QA-ChunkBench]` ingestion + telemetry gating to the manifest entry (`[TS-B3]`). |
| Grapheme windows | `tests/xi.Core.Tests/Fixtures/grapheme_descriptors/` | Manifest-backed ✅ (2025-11-17) | 668 grapheme windows now sit in the manifest (`payload_hash=a2b84031c5aa8d11f5f3a2a0d3c3537cf93f4bc0bb7bfb68bfe29fdf4f37f98d`, `schema_hash=grapheme_descriptors@1.0.0`); `[QA-Telemetry]` can replay Rust traces once iterator CLI logging is enabled (`[TS-B4]`). |
| Tree builder slice trace | `tests/xi.Core.Tests/Fixtures/ParityFixtures/tree_builder_trace/` | Placeholder sample ✅ (2025-11-18) | `basic_slice_plan.json` exercises `PushFrame`/`LeafSlice`/`EnterChild`/`MergePop`; `TreeBuilderSliceTraceLoader` + `TreeBuilderSliceTraceLoaderTests` hydrate it so C# can validate schema drift. Awaiting Rust CLI `--tree-builder-trace` runs to drop real traces + manifest entries before `[StageD::FixtureFlow]` is considered closed. |
| Fixture manifest ledger | `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` | Manifest-backed ✅ (2025-11-17) | Serves as the canonical ledger for the refresh (`python scripts/refresh_all_assets.py --only stage-d-fixtures`); contains `rust_commit=7ac917a05be4bb526844d5cdaa842030411800e5`, `cli_rev=0.3.0`, `feature_gates=["serde"]` plus the hashes above. Next step is to automate canonical hash diffs + alerting via `[StageD::ParityAssets]`/`[Fixture-Manifest]`. |
| Stage D scripts | `scripts/refresh_serialization_fixtures.ps1` | ⏳ Updates planned | CLI path works today through `scripts/refresh_all_assets.py --only stage-d-fixtures`; need to fold the manifest flag + canonical hash check into the PowerShell wrapper before we bless the Stage D instructions. |

## [RPM-Actions] Open Actions
<a id="RPM-Actions"></a>
1. **Automate canonical hash generation + diffing** – Extend `scripts/refresh_all_assets.py` + `scripts/refresh_serialization_fixtures.ps1` to emit/verify hashes per `[Fixture-Manifest]` and bind them to the new descriptor DTOs so the C# side can fail fast when manifests diverge (`[TS-B3]`).
2. **Hook StageDDescriptorLoader into QA smoke + Stage D CLI** – Update `[QA-IngestionSmoke]` and the Stage D CLI wrapper so they call `StageDDescriptorLoader` / `StageDDescriptorLoaderTests` after each refresh, cache the hydrated manifest for `ChunkBench`, and block merges if counts/hashes drift (`[TS-B3]`).
3. **Capture Grapheme iterator CLI trace + loader hook** – Add CLI logging to the Rust exporter and land a C# loader that hydrates `GraphemeDescriptor` DTOs, letting `[QA-Telemetry]` compare Rust traces against the degraded navigator (`[TS-B3]`, `[TS-B4]`).
4. **Publish MetricAdapter + TreeBuilder tracer design** – `[TS-B2]` now tracks injecting the new `TreeBuilderTracer` into `TreeBuilder` and capturing Stage D slice traces; document the adapter plan so M4 can switch safely.
5. **Document Breaks/Diff/Search skeletons** – Architecture Mapper + AI Architect to outline directories + CLI expectations (Goal G3/G4, `[TS-B5]`), referencing `[StageD::FixtureFlow]` for execution order.

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
[QA-IngestionSmoke]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-IngestionSmoke
[StageD::ParityAssets]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::ParityAssets
[StageD::FeatureGates]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::FeatureGates
[StageD::FixtureFlow]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::FixtureFlow
[Fixture-Manifest]: fixtures/parity-fixture-schema.md#fixture-manifest
