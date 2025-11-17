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
- **Problem**: C# `NodeCursor` 现已改为 `_editVersion` + `ReferenceEquals` 判定，但缺少可被 Stage D 工具消费的 `CursorDescriptor` schema与 `NodeCursorState` 文档，CLI 亦未产出 manifest。
- **Rust Plan**: 继续以 `CursorDescriptor` 为跨语言基线，并在需要时启用 `cursor_state` feature；由 `export-serde-fixtures --cursor-descriptors` 导出 10+ JSON 资产（见 `xi-editor-ph7` `cursor_descriptor.rs`）。
- **C# Plan**: 复用 `_editVersion` 票据 + `CursorDescriptorParityTests`，把结果写入 `[RPM-Matrix]` 并持续运行 `dotnet test Xi.Editor.sln -v m`（169/169）；同时将 schema/manifest 迁移到 `[StageD::ParityAssets]` 并以 `[Fixture-Manifest]` 作为唯一事实。
- **Status**: ⚠️ Watch — NodeCursor 可以测试且 parity 绿，但 CLI manifest 与 Stage D 文档尚未锁定。
- **Links**:
	- `docs/rust-refactor/CursorCache.md`
	- `docs/architecture/rope-port-mapping.md#rpm-matrix`
	- `tests/xi.Core.Tests/CursorDescriptorParityTests.cs`
	- `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` (`[Fixture-Manifest]`)
- **Next**:
	1. 与 Rust Porter 在 2025-11-19 前冻结 CLI schema，随后更新 `[StageD::ParityAssets]`、`rope-port-mapping.md` 与 `m3-implementation-plan.md` 风险表，并 rerun `dotnet test -v m` 确认 11/11 parity 仍绿。
	2. Document `NodeCursorState` 在 `[StageD::FixtureFlow]` 中的触发点，使 CLI -> manifest 路径可复制。
	3. Script `scripts/refresh_serialization_fixtures.ps1` to always pass `--emit-manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`。
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
- **Problem**: `RopeChunkEnumerator`/`RopeLineEnumerator` 仍以复制方式提供数据；虽然 Rust `export-serde-fixtures` 已通过 `fixtures.manifest.json` 输出 chunk/line/grapheme 描述符，但 QA/Stage D 自动化尚未把这些资产回灌到 dotnet 流水线，1 MB `ChunkBench` 仍缺 baseline。 
- **Rust Plan**: 继续依赖 manifest-backed exporter（见 `docs/rust-refactor/iterator-facade-export.md`）维持 chunk/line/grapheme 样本，并在 schema 变动时更新 `fixtures.manifest.json` 哈希供 C# loader 校验。
- **C# Plan**: 新增 `src/xi.Core/Rope/Diagnostics/Descriptors/StageDDescriptorLoader.cs`，使用 manifest (`tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`) + `chunk_descriptors/grapheme_descriptors` JSON hydrate `StageDDescriptorManifest`，并由 `tests/xi.Core.Tests/Diagnostics/StageDDescriptorLoaderTests.cs` 覆盖 metadata/chunk/grapheme 映射；下一步是把 loader 输出暴露给 `[QA-ChunkBench]`、`[QA-IngestionSmoke]` 与 Stage D CLI 流程。
- **Status**: 🟡 Implementation (awaiting QA wiring) — Loader + tests landed（chunk hash `52aa448cf565…`, grapheme hash `109d57d39b83…`, manifest `feature_gates=["serde","tree_builder_slice_trace"]`），但 QA smoke、Stage D CLI 仍未调用 loader，基准仍缺口。
- **Links**:
	- `docs/rust-refactor/iterator-facade-export.md`
	- `docs/architecture/rope-port-mapping.md#rpm-matrix`
	- `tests/xi.Core.Tests/RopeChunkEnumeratorTests.cs`
	- `src/xi.Core/Rope/Diagnostics/Descriptors/StageDDescriptorLoader.cs`
	- `tests/xi.Core.Tests/Diagnostics/StageDDescriptorLoaderTests.cs`
- **Next**:
	1. 将 `StageDDescriptorLoader` 接入 `[QA-IngestionSmoke]`（可直接运行 `dotnet test --filter StageDDescriptorLoaderTests` 或通过 QA 工具调用），并把结果回写到 `agents/qa-engineer.md`。
	2. 在 Stage D CLI 流程中引用 loader（或其 manifest DTO）验证 chunk/line/grapheme 计数及哈希，然后再运行 `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj -c Release` 记录 1 MB baseline 至 `[QA-ChunkBench]`。
	3. 挂钩 chunk/grapheme diagnostics，使 Stage D manifest ingestion 结果写入 `[RPM-ParityAssets]`、`[Div-Active]` 和 `design-divergence-log.md`，并在 CLI schema 变更时加 schema 版本守卫。
	4. 将 loader 作为 Stage D CLI/Litmus 的输入源，确保 `scripts/refresh_all_assets.py --only stage-d-fixtures` 产物每次刷新后自动运行 canonical hash diff并提醒 QA。

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
- **Problem**: `breaks.rs`, `diff.rs`, `find.rs` 对应的 C# 目录、CLI schema、Stage D 资产均缺席，导致 G3/G4/G5 依赖无法落地。Skeleton Coverage 列表也证实：Rust 已有 `BreaksLeaf`、`BreaksInfo`、`BreakBuilder`、`DiffBuilder`、`Finder` 等类型，而 C# 连占位命名空间都未创建。
- **Rust Plan**: 基于 `breaks-metrics-templating.md`, `iterator-facade-export.md`, `delta-subset-serialization.md` 输出新的 shim 与 CLI flag，确保 `export-serde-fixtures` 可导出 Breaks/Diff/Search 描述符。
- **C# Plan**: 在 `src/xi.Core/Rope/Breaks`, `src/xi.Core/Diff`, `src/xi.Core/Search` 建骨架；把 CLI/fixture 规划写入 `[StageD::FixtureFlow]`，并在 `rope-port-mapping.md`、`m3-implementation-plan.md` G3/G4 行追踪依赖。
- **Status**: 🟥 Risk — 无骨架、无资产，已阻塞 G3/G4。
- **Links**:
	- `docs/rust-refactor/breaks-metrics-templating.md`
	- `docs/rust-refactor/delta-subset-serialization.md`
	- `docs/architecture/port-blueprint.md#bp-goaltree`
- **Next**: 2025-11-26 前提交 Breaks shim 草案与 CLI 参数矩阵，并在 Stage D playbook 添加 refresh 步骤；若 CLI 延迟，升级 `[MP-R9]` 风险等级。同时在 `src/xi.Core/Rope/Breaks`、`src/xi.Core/Diff`、`src/xi.Core/Search` 建立骨架（namespace + TODO 类），并把“Rust-only”状态登记到 `[StageD::FixtureFlow]`/`[Design-Div]`，避免后续遗忘。

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
