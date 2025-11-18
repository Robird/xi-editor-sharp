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
- **Problem**: The 2025-11-21 Stage D run already delivers `cursor_descriptors@1.2.0` (count=12, `payload_hash=9b46bd8e29042e38c36556afc4054a5a2c4a6cfa405b5bbd738ca1909f8a4d38`, `feature_gates=["cursor_state","serde"]`) and `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt` mirrors the same ledger, but `[QA-IngestionSmoke]`, `[StageD::ParityAssets]`, and `[RPM-Matrix]` still describe the cursor stack as “skeleton”, so consumers lack a single documented proof for NodeCursorState ↔ `_editVersion` parity.
- **Rust Plan**: Keep the exporter pinned to `rust_commit=3799d2be9db0ef040517ed69df1b717e96a8958e`, continue invoking `cargo run -p xi-rope --features serde,cursor_state --bin export-serde-fixtures -- --cursor-descriptors --emit-manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`, and re-run `python scripts/refresh_all_assets.py --only stage-d-fixtures` whenever schema fields drift so the manifest + inspector log stay authoritative.
- **C# Plan**: Fold the NodeCursorState layout + hash into `[StageD::ParityAssets]` and `[StageD::FixtureFlow]`, ensure `StageDDescriptorLoader`/`CursorDescriptorParityTests` explicitly validate the gate, and surface the same manifest snippet in `rope-port-mapping.md#[RPM-Matrix]` so `_editVersion` parity has a documented Stage D anchor.
- **QA/Doc dependencies**: `[QA-IngestionSmoke]` must link the latest manifest + inspector output (stored at `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt`), `[Fixture-Manifest]` needs the `feature_gates=["cursor_state","serde"]` note, and `[QA-ChunkBench]` should reference the same refresh command to avoid drift.
- **Status**: 🟡 Implementation (QA ingestion pending) — exporter + manifest + inspector evidence exist; missing work is stitching the proof into QA anchors and doc tables (log: `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt`).
- **Next Steps**:
	1. **11/22 – QA Engineer**: update `[QA-IngestionSmoke]` with the manifest diff + inspector snippet from the latest `python scripts/refresh_all_assets.py --only stage-d-fixtures` run and note the cursor counts/hashes.
	2. **11/22 – C# Implementer**: re-run `dotnet test Xi.Editor.sln -v m --filter CursorDescriptorParityTests` against the manifest-backed payload, paste the command/log into `[StageD::ParityAssets]`, and document NodeCursorState fields.
	3. **11/24 – Architecture Mapper**: propagate the same evidence into `[RPM-Matrix]`, `[RPM-ParityAssets]`, and `m3-implementation-plan.md#[MP-T1]`, then mark the Goal Tree dependency as unblocked.
### [TS-B2] Metric 互操作与泛型节点桥接
<a id="TS-B2"></a>
- **Problem**: C# 仍通过 `IMetric` 动态分派遍历整棵树，`Breaks`/`Diff` 依赖的 shim 缺席；泛型 `Node<TInfo, TLeaf, TLeafOps>` 尚未进入主实现。虽已拿到 `tree_builder_slice_trace@1.0.0` manifest 资产（`basic_slice_plan.json`），但 `TreeBuilderTracer` 仍未注入 `TreeBuilder`，Rust CLI 也尚未导出长 trace 供 replay。`scripts/refresh_serialization_fixtures.ps1` 现默认在 parity refresh 中携带 `--tree-builder-trace`（可用 `-SkipTreeTrace` 临时关闭），因此缺口聚焦在 tracer wiring 与更长 trace 的生成。 
- **Rust Plan**: 依托 `convert_lines_from_bytes` 等 shim 以及 `docs/rust-refactor/breaks-metrics-templating.md` 的模板输出度量 helper，逐步补齐 `edit_*`/`Breaks` shim，并继续把 `--tree-builder-trace` 作为默认输出（除非明确传入 `-SkipTreeTrace`），确保 C# 在接入 tracer 时始终有最新样本。
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
- **Problem**: The exporter now emits manifest-backed Breaks/Diff/Search payloads (`breaks_descriptors@1.0.0` hash `5d37a7313730dd0ae9c292ca45daa442c11fe63d45f9ad21bad664f919c13f86`, `diff_regions@1.0.0` hash `fe76ed31cff4549ffd3f32bd3847dda15ced7538c4165b4566f782e715572562`, `search_spans@1.0.0` hash `7eac7ecf3bb0bdbf5369b6b0dcb17bf5241d8c914af791440e2c5a4582ee7d57`), and the inspector log enumerates wrap width spans, per-case ops, and regex queries. `scripts/refresh_serialization_fixtures.ps1` 现在会在默认 parity refresh 中携带 `--breaks-descriptors --diff-regions --search-spans`（除非显式 `-SkipBreaksDiffSearch`），但 `[QA-IngestionSmoke]`、`[QA-ChunkBench]`、`[StageD::ParityAssets]` 仍描述它们为 placeholder，导致 QA 缺少 canonical anchor，尽管 `StageDDescriptorHydrator` 消费的是实数据信息。
- **Rust Plan**: Keep `export-serde-fixtures --breaks-descriptors --diff-regions --search-spans` (now wired via script defaults unless `-SkipBreaksDiffSearch` is provided) in the `python scripts/refresh_all_assets.py --only stage-d-fixtures` pipeline, pin `rust_commit=3799d2be9db0ef040517ed69df1b717e96a8958e`, and attach every refresh’s inspector output to `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt` for traceability.
- **C# Plan**: Extend `StageDDescriptorHydratorTests`/`BreaksSkeletonTests|DiffSkeletonTests|SearchSkeletonTests` to assert the manifest counts/hashes, surface the inspector metadata (wrap span, regex queries) in doc notes, and reuse the hydrated payload in QA smoke + benchmarks.
- **QA/Doc dependencies**: `[QA-IngestionSmoke]` must embed the inspector snippet + manifest diff (`tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt`), `[QA-ChunkBench]` should explicitly cite the chunk/line stats from the same run (`tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`), and docs (`rope-port-mapping.md`, `system-overview.md`, `design-divergence-log.md`) need to reflect the manifest-backed status so Goal Tree G3/G4 references a single fact source.
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
