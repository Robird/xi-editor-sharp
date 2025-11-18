# 类型系统移植阻塞追踪

> **Scope**: Track cross-language type-system blockers for the Xi.Editor port and document the agreed mitigations.
> **Owner**: Architecture Mapper
> **Update Frequency**: After any blocker opens/closes or a Stage D asset lands.
> **Reviewers**: AI Architect · C# Implementer · Rust Porter · QA Engineer
> **Anchor Prefix**: TS
> **Last Synced Goal Tree**: 2025-11-18 (mirrors `[BP-GoalTree]`)

---

## [TS-Blockers] Blocker Cards
<a id="TS-Blockers"></a>

### [TS-B1] Cursor 生命周期与 Descriptor 管道
<a id="TS-B1"></a>
- **Problem**: `_editVersion` parity + 11/11 `CursorDescriptorParityTests` pass, yet Stage D artifacts still describe the legacy `cursor_descriptors@1.1.0` schema and never document `NodeCursorState`, so `[RPM-Matrix]`/`[RPM-ParityAssets]` overstate readiness (`[Chat-2025-11-20]`).
- **Rust Plan**: Ship `cursor_state`-aware exporters by 11/22 (Owner: Rust Porter) via `cargo run -p xi-rope --features serde,cursor_state --bin export-serde-fixtures -- --cursor-descriptors --emit-manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`, bump the schema to `cursor_descriptors@1.2.0`, and attach Inspector digests for `[QA-IngestionSmoke]`.
- **C# Plan**: Document `NodeCursorState` layout inside `[StageD::ParityAssets]`/`[StageD::FixtureFlow]`, keep `_editVersion` parity tests green, and teach `StageDDescriptorLoader` to tolerate the new gate so QA can replay manifests without patching code.
- **QA/Doc dependencies**: `[QA-IngestionSmoke]` must embed the manifest + Inspector hashes, `[Fixture-Manifest]` must reflect the new `feature_gates=["serde","cursor_state"]`, and `docs/architecture/rope-port-mapping.md#[RPM-Matrix]` + `m3-implementation-plan.md#[MP-T1]` must cite the Stage D evidence once available.
- **Status**: ⚠️ Watch — Schema text + doc hooks are still missing; the 2025-11-19 manifest is classified as skeleton even though the loader/hydrator tests pass.
- **Next Steps**:
  1. **11/22 – Rust Porter**: land the `cursor_state` exporter + manifest bump, share the CLI/Inspector logs, and note the change in `[StageD::ParityAssets]` (`[Chat-2025-11-20]`).
  2. **11/22 – C# Implementer**: publish the `NodeCursorState` write-up + loader changes, then rerun `dotnet test Xi.Editor.sln -v m --filter CursorDescriptorParityTests` to prove nothing regressed.
  3. **11/27 – Architecture Mapper**: once payload + docs exist, flip the `[RPM-ParityAssets]` cursor row back to ✅ and close the Goal Tree dependency.
### [TS-B2] Metric 互操作与泛型节点桥接
<a id="TS-B2"></a>
- **Problem**: C# 仍通过 `IMetric` 动态分派遍历整棵树，`Breaks`/`Diff` 依赖的 shim 缺席；泛型 `Node<TInfo, TLeaf, TLeafOps>` 尚未进入主实现。虽已拿到 `tree_builder_slice_trace@1.0.0` manifest 资产（`basic_slice_plan.json`），但 `TreeBuilderTracer` 仍未注入 `TreeBuilder`，Rust CLI 也尚未导出长 trace 供 replay。 
- **Rust Plan**: 依托 `convert_lines_from_bytes` 等 shim 以及 `docs/rust-refactor/breaks-metrics-templating.md` 的模板输出度量 helper，逐步补齐 `edit_*`/`Breaks` shim。
- **C# Plan**: 在 `node-generic-refactor-plan.md` 定下 `MetricAdapter` 结构，落地 smoke tests（`MetricAdapterTests`），并利用 `TreeBuilderSliceTraceLoader` + `TreeBuilderTracer` 骨架记录调试事件，写入 `[StageD::FixtureFlow]` 以便 CLI/fixture 同步。
- **Status**: 🟡 Implementation (loader ready) — `_editVersion` 与 `TreeBuilderTracer` 骨架齐备，`TreeBuilderSliceTraceLoader`/`TreeBuilderSliceTraceEvent` + tests 现直接消费 manifest 里的 `tree_builder_slice/basic_slice_plan.json`，但仍需 Rust CLI 注入 + C# tracer wiring 才能 replay 长 trace 并落地 `MetricAdapter`。
- **Links**:
	- `docs/rust-refactor/breaks-metrics-templating.md`
	- `docs/csharp-refactor/node-generic-refactor-plan.md`
	- `src/xi.Core/Rope/Diagnostics/TreeBuilder/TreeBuilderSliceTraceLoader.cs`
	- `tests/xi.Core.Tests/Diagnostics/TreeBuilderSliceTraceLoaderTests.cs`
	- `tests/xi.Core.Tests/GenericNodeInterfaceTests.cs`
	- `docs/architecture/rope-port-mapping.md#rpm-matrix`
- **Next**:
	1. 于 2025-11-24 前提交 `MetricAdapter` 草案 + smoke 测试，并在 `rope-port-mapping.md`、`m3-implementation-plan.md` 标记 G6 进度。
	2. 将 `TreeBuilderTracer` 注入 `TreeBuilder`，并把 Rust CLI 导出的 slice trace 与 C# tracer 对拍；完成后更新 `[StageD::FixtureFlow]`/`[StageD::ParityAssets]`。
	3. Online 更新 `[StageD::FeatureGates]` 与 `[Fixture-Manifest]`（tree builder gate 已登记；MetricAdapter 若引入新 gate 需同步），确保 CLI/manifest 可复现实验。
	4. 在 Stage D 文档中补上 `_editVersion` → tracer → adapter 依赖图，避免 G6 merge 时追溯困难。
### [TS-B3] Chunk/Line 迭代器与 Telemetry
<a id="TS-B3"></a>
- **Problem**: Loader + DTOs exist, but QA still consumes copy-on-read enumerators; `[QA-ChunkBench]` lacks the 1 MB baseline + alloc stats and `[QA-IngestionSmoke]` never records Inspector output for chunk/grapheme assets, so documentation overstates Stage D coverage (`[Chat-2025-11-20]`).
- **Rust Plan**: Keep exporting chunk/line/grapheme descriptors via `export-serde-fixtures` and update hashes whenever schema drifts, then share CLI logs so QA can diff manifest vs Inspector counts.
- **C# Plan**: Run `StageDDescriptorLoader`/`Hydrator` inside refresh scripts, feed the materialized descriptors into `RopeChunkEnumeratorBenchmarks`, and publish the telemetry counters/alloc stats demanded by `[MP-R10]`.
- **QA/Doc dependencies**: `[QA-IngestionSmoke]` must capture loader → hydrator → manifest verifier → Inspector output, `[QA-ChunkBench]` must store throughput + alloc, and `docs/architecture/rope-port-mapping.md#[RPM-Matrix]`/`[RPM-ParityAssets]` must highlight the QA evidence once present.
- **Status**: 🟡 Implementation — code exists but Stage D + QA anchors still show skeleton due to missing ingestion evidence.
- **Next Steps**:
  1. **11/24 – C# Implementer**: update `python scripts/refresh_all_assets.py --only stage-d-fixtures` to always execute loader/hydrator + Inspector, dump the manifest summary, and surface failures in CI.
  2. **11/24 – QA Engineer**: record the resulting hashes + `RopeChunkEnumeratorBenchmarks` telemetry in `[QA-IngestionSmoke]`/`[QA-ChunkBench]`, including alloc stats and CLI commands.
  3. **11/27 – Architecture Mapper**: close the documentation gap by flipping the `[RPM-Matrix]` chunk row back to parity-ready only after QA artifacts are linked.

### [TS-B4] Grapheme 降级策略
<a id="TS-B4"></a>
- **Problem**: C# 仅保证 surrogate-safe + 单邻叶补片，未复刻 Rust `GraphemeCursor`；遥测阈值（0.5%）仍待裁决。
- **Rust Plan**: 维持 `unicode_segmentation::GraphemeCursor` 输出，并在需要时暴露 `cursor_state` trace 供未来追平。
- **C# Plan**: 使用 `DegradedGraphemeNavigator` + `GraphemeNavigationMetrics`，在 `[QA-Telemetry]` 记录 fallback 命中率；必要时升级至 ICU4N 或 Rust trace。
- **Status**: ✅ Degraded — 行为受控、测试完备，但需持续监控。
- **Links**:
	- `docs/architecture/design-divergence-log.md#div-active`
	- `tests/xi.Core.Tests/GraphemeNavigatorSmokeTests.cs`
- **Next**: 2025-11-20 前由架构师裁决遥测阈值，Architecture Mapper 负责在 `design-divergence-log.md` 与 `m3-implementation-plan.md` 同步监控结论。

### [TS-B5] Breaks/Diff/Search 骨架缺口
<a id="TS-B5"></a>
- **Problem**: `[RPM-Matrix]` shows Breaks/Diff/Search as “hydrator wired”, yet `[Chat-2025-11-20]` confirmed the Stage D ledger still carries placeholder JSON; QA cannot verify Goal Tree G3/G4 because no real payload or Inspector log exists.
- **Rust Plan**: Between 11/22–11/24, implement `export-serde-fixtures --breaks-descriptors --diff-regions --search-spans` using the schemas in `docs/rust-refactor/breaks-metrics-templating.md` & `iterator-facade-export.md`, emit live payloads + hashes into `fixtures.manifest.json`, and update `[StageD::ParityAssets]`/`[Fixture-Manifest]` with the new feature gates.
- **C# Plan**: Skeleton DTOs/tests are already in `Rope/Breaks|Diff|Search`; next is to run the hydrator against the real manifest, pipe outputs into QA scripts, and light up Breaks metrics + Finder parity smoke so `[QA-IngestionSmoke]` has typed evidence.
- **QA/Doc dependencies**: `[QA-IngestionSmoke]` must store Inspector digests for the new payloads, `[QA-ChunkBench]`/`[QA-Telemetry]` track any perf regressions, and Architecture docs (`rope-port-mapping.md`, `design-divergence-log.md`, `system-overview.md`) need to flip the Breaks/Diff/Search rows back to parity only after evidence lands.
- **Status**: 🟠 Partial — schemas + C# skeleton are done, exporter + QA evidence are not.
- **Next Steps**:
  1. **11/22 – Rust Porter**: cut the exporter branch, land CLI flags + schema text, and attach the cargo/Inspector logs referenced by `[RPM-Actions]`.
  2. **11/24 – QA Engineer + C# Implementer**: rerun `python scripts/refresh_all_assets.py --only stage-d-fixtures` with the new assets, update `[QA-IngestionSmoke]` with hashes/counts, and add hydrator smoke filters for `BreaksSkeletonTests|DiffSkeletonTests|SearchSkeletonTests`.
  3. **11/27 – Architecture Mapper**: if QA delivers evidence, update `[RPM-ParityAssets]` + `[Div-Active]` + `[SO-Map]` to remove the skeleton warnings; otherwise escalate via `[MP-R9]`/AI Architect.

## [TS-Retired] Retired Blockers
<a id="TS-Retired"></a>
尚无完成项；所有历史阻塞仍在跟踪列表中。

## [TS-ChangeLog] Change Log
<a id="TS-ChangeLog"></a>
- **2025-11-18 – Template rollout**：重写为 blocker 卡片格式，新增 `[TS-B5]`（Breaks/Diff/Search 骨架缺口），并把每张卡映射到 Goal Tree / Stage D / QA anchors；旧段落被折叠进 Links/Next 字段。

[StageD::ParityAssets]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::ParityAssets
[StageD::FeatureGates]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::FeatureGates
[StageD::FixtureFlow]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::FixtureFlow
[QA-ChunkBench]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-ChunkBench
[QA-Telemetry]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-Telemetry
[Fixture-Manifest]: fixtures/parity-fixture-schema.md#fixture-manifest
[MP-R9]: m3-implementation-plan.md#r9
[MP-R10]: m3-implementation-plan.md#r10
[Chat-2025-11-20]: ../meetings/2025-11-20-type-mapping-sync-chat.md#architecture-mapper
