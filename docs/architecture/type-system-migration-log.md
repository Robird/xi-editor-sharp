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
- **Problem**: The 2025-11-19 Stage D run already delivers `cursor_descriptors@1.2.0` (count=12, `payload_hash=9b46bd8e29042e38c36556afc4054a5a2c4a6cfa405b5bbd738ca1909f8a4d38`, `feature_gates=["cursor_state","serde"]`) and `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt` mirrors the same ledger, but `[QA-IngestionSmoke]`, `[StageD::ParityAssets]`, and `[RPM-Matrix]` still describe the cursor stack as “skeleton”, so consumers lack a single documented proof for NodeCursorState ↔ `_editVersion` parity.
- **Rust Plan**: Keep the exporter pinned to `rust_commit=bd28ebdf83d2dd2fa6d4bd7c857d2471a21b4999`, continue invoking `cargo run -p xi-rope --features serde,cursor_state --bin export-serde-fixtures -- --cursor-descriptors --emit-manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`, and re-run `python scripts/refresh_all_assets.py --only stage-d-fixtures` whenever schema fields drift so the manifest + inspector log stay authoritative.
- **C# Plan**: Fold the NodeCursorState layout + hash into `[StageD::ParityAssets]` and `[StageD::FixtureFlow]`, ensure `StageDDescriptorLoader`/`CursorDescriptorParityTests` explicitly validate the gate, and surface the same manifest snippet in `rope-port-mapping.md#[RPM-Matrix]` so `_editVersion` parity has a documented Stage D anchor.
- **QA/Doc dependencies**: `[QA-IngestionSmoke]` must link the latest manifest + inspector output (stored at `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt`), `[Fixture-Manifest]` needs the `feature_gates=["cursor_state","serde"]` note, and `[QA-ChunkBench]` should reference the same refresh command to avoid drift.
- **Status**: 🟡 Implementation (QA ingestion pending) — exporter + manifest + inspector evidence exist; missing work is stitching the proof into QA anchors and doc tables.
- **Next Steps**:
	1. **11/22 – QA Engineer**: update `[QA-IngestionSmoke]` with the manifest diff + inspector snippet from the latest `python scripts/refresh_all_assets.py --only stage-d-fixtures` run and note the cursor counts/hashes.
	2. **11/22 – C# Implementer**: re-run `dotnet test Xi.Editor.sln -v m --filter CursorDescriptorParityTests` against the manifest-backed payload, paste the command/log into `[StageD::ParityAssets]`, and document NodeCursorState fields.
	3. **11/24 – Architecture Mapper**: propagate the same evidence into `[RPM-Matrix]`, `[RPM-ParityAssets]`, and `m3-implementation-plan.md#[MP-T1]`, then mark the Goal Tree dependency as unblocked.
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
- **Problem**: The exporter now emits manifest-backed Breaks/Diff/Search payloads (`breaks_descriptors@1.0.0` hash `0a82a69f83ae8abd98933af159f59bd01dbd5cf98ce4aadaa4e6452031e147a6`, `diff_regions@1.0.0` hash `2c4c366ce1070d56988f1adca2048a701b5b6b273b34e1d8a0c06665d2cce1b9`, `search_spans@1.0.0` hash `e7de44d74af62d44fcdfe4be554f88937bb45c93194bc4a5b62a0750e36bb467`), and the inspector log enumerates wrap width spans, per-case ops, and regex queries. However `[QA-IngestionSmoke]`, `[QA-ChunkBench]`, and `[StageD::ParityAssets]` still describe them as placeholders, so QA lacks a canonical anchor even though `StageDDescriptorHydrator` consumes real data.
- **Rust Plan**: Keep `export-serde-fixtures --breaks-descriptors --diff-regions --search-spans` in the `python scripts/refresh_all_assets.py --only stage-d-fixtures` pipeline, pin `rust_commit=bd28ebdf83d2dd2fa6d4bd7c857d2471a21b4999`, and attach every refresh’s inspector output to `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt` for traceability.
- **C# Plan**: Extend `StageDDescriptorHydratorTests`/`BreaksSkeletonTests|DiffSkeletonTests|SearchSkeletonTests` to assert the manifest counts/hashes, surface the inspector metadata (wrap span, regex queries) in doc notes, and reuse the hydrated payload in QA smoke + benchmarks.
- **QA/Doc dependencies**: `[QA-IngestionSmoke]` must embed the inspector snippet + manifest diff, `[QA-ChunkBench]` should explicitly cite the chunk/line stats from the same run, and docs (`rope-port-mapping.md`, `system-overview.md`, `design-divergence-log.md`) need to reflect the manifest-backed status so Goal Tree G3/G4 references a single fact source.
- **Status**: 🟡 Implementation (QA ingestion pending) — exporter + manifest + inspector proof exist; migration is blocked only on QA/doc ingestion.
- **Next Steps**:
	1. **11/22 – QA Engineer**: paste the Breaks/Diff/Search ledger (counts + hashes + sample lists) from `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt` into `[QA-IngestionSmoke]`, referencing the `python scripts/refresh_all_assets.py --only stage-d-fixtures` command used to generate it.
	2. **11/23 – C# Implementer**: rerun `dotnet test Xi.Editor.sln -v m --filter "BreaksSkeletonTests|DiffSkeletonTests|SearchSkeletonTests|StageDDescriptorHydratorTests"`, confirm hydrator assertions hit the manifest-backed payload, and document any follow-up telemetry hooks needed for `[QA-ChunkBench]`/`[QA-Telemetry]`.
	3. **11/27 – Architecture Mapper**: update `[RPM-Matrix]`, `[RPM-ParityAssets]`, `[SO-Map]`, and `[StageD::ParityAssets]` change logs to record the hashes above and remove the “skeleton” wording; escalate via `[MP-R9]` if QA evidence drifts.

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
