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
| Node / SharedNode / TreeBuilder | `rope/tree.rs` | `Tree/Node*.cs`, `TreeBuilder.cs`, `Tree/LeafSplitter.cs`, `Tree/StringLeafOperations.cs`, `Tree/TreeBuilderTracer.cs`, `Diagnostics/TreeBuilder/TreeBuilderSliceTraceLoader.cs` | Implementation (slice trace loader ready) | Rust skeleton exposes `Node<N,L>`, `SharedNode`, `TreeBuilderTracer`, `TreeBuilderEventKind`; C# ships `TreeBuilderTracer/EventKind/Event/ITreeBuilderTracer` + `NoOpTreeBuilderTracer` and now hydrates manifest-managed traces via `TreeBuilderSliceTraceLoader` + tests. | SharedNode helpers + `_editVersion` + tracer scaffolding exist, and `TreeBuilderSliceTraceLoaderTests` prove we can read the Stage D exporter output at `tree_builder_slice/basic_slice_plan.json`; remaining work is injecting the tracer through `TreeBuilder` + `MetricAdapter` (`[TS-B2]`) and replaying full CLI traces once `[StageD::FixtureFlow]` emits production samples alongside the manifest. |
| Cursor stack | `rope/tree.rs::Cursor` | `Tree/NodeCursor.cs`, `tests/.../CursorDescriptorParityTests.cs` | Active | Rust skeleton lists `Cursor`, `CursorState`, `CursorDescriptorFixture`, `DescriptorMetric`; C# has `NodeCursor`, parity tests, yet no descriptor DTO/exporter equivalents. | `_editVersion` + invalidation probes remain covered by `CursorDescriptorParityTests` (`dotnet test Xi.Editor.sln -v m` -> 169/169); Stage D manifest (11 entries, payload hash `fe963d909d5c…`) was minted on 2025-11-17 via `python scripts/refresh_all_assets.py --only stage-d-fixtures`, so `[QA-IngestionSmoke]` ✅ and focus shifts to canonical hash automation before the CLI schema evolves (`[Fixture-Manifest]`). |
| Rope core & metrics | `rope/rope.rs` | `Rope.cs`, `RopeInfo.cs`, `Metrics.cs`, `IMetric.cs` | Implementing | Rust keeps `helpers/string_leaf.rs`, `metrics/*`, `BreaksLeaf/BreaksInfo`; C# only ships `StringLeafOperations` + `BreaksMetricHelper`, missing `Breaks` node skeleton (`BreakBuilder`, `BreaksLeaf`). | `StringLeafOperations` + `BreaksMetricHelper` landed; conversion shim still mirrors Rust via `[MP-T1]`. |
| Delta / Transformer | `rope/delta.rs` | `Rope/Delta*.cs` | Implementing | Rust skeleton includes `Delta`, `DeltaElement`, `Transformer`, `SubsetFactor`; C# has `Delta<TInfo,TLeaf>`/`DeltaJson` but no `Transformer`/`Factor` chain or subset builder parity. | Stage B JSON parity lives in `[StageD::ParityAssets]`; `Transformer`/`factor` tracked under `[MP-T1]`. |
| Engine / Undo ledger | `rope/engine.rs` | `Rope/Engine*.cs` | Done | Rust engine skeleton exposes `Engine`, `Rev`, `Contents`, `UndoGroup`, `gc` helpers; C# implements `Engine`, `Revision*`, `EngineJson` but still lacks merging/gc APIs beyond serialization playback. | Stage C fixtures frozen; no open TODO beyond perf tuning. |
| Chunk + Line enumerators | `rope/rope.rs::ChunkIter`, `Lines*` | `RopeChunkEnumerator*.cs`, `RopeLineEnumerator*.cs`, `Rope/Diagnostics/Descriptors/{ChunkDescriptor,LineDescriptor,StageDDescriptorLoader}.cs` | Implementation (loader ready) | Rust skeleton covers `ChunkIter`, `LinesRaw`, `ChunkDescriptor`, `LineDescriptor` exporter structs; `export-serde-fixtures` keeps manifest-backed parity（chunk hash `52aa448cf565…`, grapheme hash `109d57d39b83…`, feature gates `serde + tree_builder_slice_trace`）。C# now mirrors the descriptor DTOs *and* ships `StageDDescriptorLoader` + manifest DTOs so chunk/line/grapheme descriptors hydrate directly from `fixtures.manifest.json`. | Copy-on-read enumerators + telemetry counters remain the downgrade path; loader coverage lives in `tests/xi.Core.Tests/Diagnostics/StageDDescriptorLoaderTests.cs` and proves metadata/chunk/grapheme ingestion, but `[TS-B3]` still tracks wiring the loader into `[QA-IngestionSmoke]`/Stage D CLI before `[QA-ChunkBench]` can log the 1 MB baseline. |
| Grapheme navigation | `rope/rope.rs::GraphemeCursor` | `Navigation/DegradedGraphemeNavigator.cs`, `GraphemeNavigationMetrics.cs`, `Rope/Diagnostics/Descriptors/GraphemeDescriptor.cs` | Degraded (monitor) | Rust exposes `unicode_segmentation::GraphemeCursor`, `GraphemeDescriptor`, exporter structs; C# now mirrors the descriptor DTOs but still lacks iterator trace ingestion + loader wiring. | Surrogate-safe fallback accepted (see `[Div-Active]`); Stage D manifest lists 668 grapheme windows（hash `109d57d39b83…`）并由 C# DTO 映射——下一步是启用 loader ingestion + CLI trace capture 让 `[QA-Telemetry]` 能对比 Rust traces 与降级实现（`[TS-B3]`, `[TS-B4]`）。 |
| Breaks tree / soft line metrics | `rope/breaks.rs`, `rope/metrics/break_indices.rs`, `core-lib/src/linewrap.rs` | `Rope/BreaksMetricHelper.cs`, planned `src/xi.Core/Rope/Breaks/{BreaksTree.cs,BreakBuilder.cs,BreaksDescriptor.cs}` | Rust-only (helper only) | Rust publishes `BreaksLeaf`, `BreaksInfo`, `BreaksMetric`, `BreaksBaseMetric`, `BreakBuilder`, and façade shims (`Breaks::count_breaks_*`). C# only carries `BreaksMetricHelper`—no tree/backing nodes, descriptor DTO, or loader wiring. Stage D needs a `export-serde-fixtures --breaks-descriptors` flag emitting `breaks_descriptors@1.0.0` into `fixtures.manifest.json` and listed under `[StageD::ParityAssets]`. | `[TS-B5]` owns the CLI + schema spec; until manifest-backed descriptors exist (tracked via `[StageD::FixtureFlow]`), Breaks consumers must stay on Rust iterator façades and QA cannot sample soft-break coverage. |
| Diff / Compare | `rope/diff.rs`, `rope/compare.rs`, `rust/core-lib/src/find.rs` | Planned `src/xi.Core/Diff/{LineHashDiff.cs,DiffBuilder.cs,DiffRegionDescriptor.cs}` | Rust-only (blocked) | Rust exposes the `Diff` trait, `LineHashDiff`, `DiffBuilder`, `DiffOp`, `RopeScanner`, plus helper pipelines from `delta.rs`. No C# namespace, DTO, or test fixture exists. Stage D needs deterministic `diff_regions.json` snapshots (hashing `DiffBuilder::ops`) so QA can replay `[StageD::ParityAssets]` runs. | Blocked on `[TS-B5]`: Rust Porter must add `export-serde-fixtures --diff-regions` (documented in `[StageD::FixtureFlow]`) and C# must add skeletons/tests before Goal G4 unblocks. |
| Search / Spans | `rope/find.rs`, `rope/spans.rs` | Planned `src/xi.Core/Search/{Finder.cs,SearchOptions.cs,Spans.cs}` | Rust-only (blocked) | Rust currently owns `FindResult`, `CaseMatching`, `find_progress`, regex-aware scanners, plus `Spans<T>`, `SpansBuilder`, `SpanIter`. C# lacks cursor/search pipelines, span trees, or telemetry hooks. Stage D requires CLI-exported `search_hits.json` + `span_windows.json` to capture regex/substring runs for QA. | `[TS-B5]` couples the finder/spans skeletons with the upcoming Stage D CLI (documented in `[StageD::FixtureFlow]`); until manifest hashes land, these features must stay Rust-only. |

> Remaining helper/details (Subset, Interval, serde DTOs, CLI commands) live in `docs/csharp-refactor/rope-serialization-fixture-playbook.md` and `[StageD::ParityAssets]`.

## [RPM-ParityAssets] Parity Assets & Stage D Hooks
<a id="RPM-ParityAssets"></a>
| Asset | Path | Status | Source / Notes |
| --- | --- | --- | --- |
| Serde baseline (Subset/Delta/Engine) | `tests/xi.Core.Tests/Fixtures/{subset,delta,engine}_serialization/` | ✅ Current | Refreshed via `scripts/refresh_serialization_fixtures.ps1`; `dotnet test Xi.Editor.sln -v m` -> 169/169 gets attached to `[QA-IngestionSmoke]` whenever these change. |
| Cursor descriptors | `tests/xi.Core.Tests/Fixtures/cursor_descriptors/` | Manifest-backed ✅ (2025-11-17) | Refreshed via `python scripts/refresh_all_assets.py --only stage-d-fixtures`; manifest records `count=11`, `payload_hash=fe963d909d5c4e225bfd6e0c7085def006dccfadde7da8f5ea15a574a1483375`, `schema_hash=cursor_descriptors@1.1.0`, `rust_commit=7ac917a05be4bb526844d5cdaa842030411800e5`, `cli_rev=0.3.0`, `feature_gates=["serde","tree_builder_slice_trace"]`. `[QA-IngestionSmoke]` ✅，`[TS-B1]` 现聚焦 schema forward-compat。 |
| Chunk descriptors | `tests/xi.Core.Tests/Fixtures/chunk_descriptors/` | Manifest-backed ✅ (2025-11-19) | 最新刷新产出 20 条 chunk windows（`payload_hash=52aa448cf565df821bd9798ead94caee73a0e4798e73fade542951d10fa6632e`, `schema_hash=chunk_descriptors@1.0.0`，feature gates 同步包含 `tree_builder_slice_trace`）。下一步把 `[QA-ChunkBench]` ingestion + telemetry gating 绑到 manifest entry（`[TS-B3]`）。 |
| Grapheme windows | `tests/xi.Core.Tests/Fixtures/grapheme_descriptors/` | Manifest-backed ✅ (2025-11-19) | 668 grapheme windows（`payload_hash=109d57d39b83618d0da2d091144ebd9ebe5e3fa53e4d1c00f720c7f721ab5e2a`, `schema_hash=grapheme_descriptors@1.0.0`）已写入 manifest；待 CLI iterator trace 接好后即可喂给 `[QA-Telemetry]` (`[TS-B4]`)。 |
| Tree builder slice trace | `tests/xi.Core.Tests/Fixtures/tree_builder_slice/` | Manifest-backed ✅ (2025-11-19) | `basic_slice_plan.json`（3 events，hash `22724af7fe8b22e4e1dbd01f86ba1b3902a0ccf777944b5b29081259927c3a6e`, schema `tree_builder_slice_trace@1.0.0`）由 `--tree-builder-trace` + `-ExportTreeTrace` 导出并记录在 manifest 中；`TreeBuilderSliceTraceLoader` + tests 现直接消费该路径，后续等待更长 CLI traces（`[TS-B2]`）。 |
| Fixture manifest ledger | `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` | Manifest-backed ✅ (2025-11-19) | Canonical ledger（`rust_commit=7ac917a05be4bb526844d5cdaa842030411800e5`, `cli_rev=0.3.0`, `feature_gates=["serde","tree_builder_slice_trace"]`），含 tree builder/chunk/cursor/grapheme/serde 基线；接下来将 canonical hash diff 自动化（`[StageD::ParityAssets]` ↔ `[Fixture-Manifest]`）。 |
| Stage D scripts | `scripts/refresh_serialization_fixtures.ps1` | ⏳ Updates planned | CLI path works today through `scripts/refresh_all_assets.py --only stage-d-fixtures`; need to fold the manifest flag + canonical hash check into the PowerShell wrapper before we bless the Stage D instructions. |
| Breaks descriptors (soft line metrics) | `tests/xi.Core.Tests/Fixtures/breaks_descriptors/` | ⏳ Planned (`[TS-B5]`) | Rust exporter must grow `--breaks-descriptors` to emit `breaks_descriptors@1.0.0` (mirroring chunk/grapheme rows) and append the manifest entry; QA then ingests via `StageDDescriptorLoader` once C# DTOs exist. |
| Diff region snapshots | `tests/xi.Core.Tests/Fixtures/diff_regions/` | ⏳ Planned (`[TS-B5]`) | CLI hook (tentative `--diff-regions`) should capture `LineHashDiff` ops against curated fixture pairs; manifest rows unblock Diff replay tests + `[QA-IngestionSmoke]`. |
| Search hits & span windows | `tests/xi.Core.Tests/Fixtures/search_spans/` | ⏳ Planned (`[TS-B5]`) | Needs a finder CLI bolt-on that logs regex/text hits plus `Spans<T>` state; ingestion path mirrors chunk/grapheme descriptors and must be wired into `[StageD::FixtureFlow]`. |

## [RPM-Actions] Open Actions
<a id="RPM-Actions"></a>
1. **Automate canonical hash generation + diffing** – Extend `scripts/refresh_all_assets.py` + `scripts/refresh_serialization_fixtures.ps1` to emit/verify hashes per `[Fixture-Manifest]` and bind them to the new descriptor DTOs so the C# side can fail fast when manifests diverge (`[TS-B3]`).
2. **Hook StageDDescriptorLoader into QA smoke + Stage D CLI** – Update `[QA-IngestionSmoke]` and the Stage D CLI wrapper so they call `StageDDescriptorLoader` / `StageDDescriptorLoaderTests` after each refresh, cache the hydrated manifest for `ChunkBench`, and block merges if counts/hashes drift (`[TS-B3]`).
3. **Capture Grapheme iterator CLI trace + loader hook** – Add CLI logging to the Rust exporter and land a C# loader that hydrates `GraphemeDescriptor` DTOs, letting `[QA-Telemetry]` compare Rust traces against the degraded navigator (`[TS-B3]`, `[TS-B4]`).
4. **Publish MetricAdapter + TreeBuilder tracer design** – `[TS-B2]` now tracks injecting the new `TreeBuilderTracer` into `TreeBuilder` and capturing Stage D slice traces; document the adapter plan so M4 can switch safely.
5. **Spec Breaks/Diff/Search CLI + manifest rows** – Work with Rust Porter to extend `export-serde-fixtures` (or a sibling CLI) with `--breaks-descriptors`, `--diff-regions`, `--search-spans`, document schema in `[StageD::FixtureFlow]`, and add placeholder entries in `[StageD::ParityAssets]`/manifest so `[QA-IngestionSmoke]` can track hashes (`[TS-B5]`).
6. **Stand up C# Breaks/Diff/Search skeletons** – Create `Rope/Breaks/*`, `Diff/*`, `Search/*` namespaces plus descriptor DTOs/tests that hydrate the new Stage D assets via `StageDDescriptorLoader`, then link those smoke tests back into `[RPM-Matrix]`/`[TS-B5]` before enabling feature flags.

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
