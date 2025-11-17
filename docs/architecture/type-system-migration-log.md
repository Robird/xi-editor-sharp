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
- **Problem**: C# 仍通过 `IMetric` 动态分派遍历整棵树，`Breaks`/`Diff` 依赖的 shim 缺席；泛型 `Node<TInfo, TLeaf, TLeafOps>` 尚未进入主实现。`[RPM-Matrix]` Skeleton Coverage 进一步指出：Rust 侧仍暴露 `TreeBuilderTracer`、`TreeBuilderEventKind` 与 `helpers/string_leaf.rs` 中的分裂 helper，而 C# 端连占位类型都没有，导致 Stage D slice trace 只能依赖 Rust CLI，MetricAdapter 也缺少调试信号。
- **Rust Plan**: 依托 `convert_lines_from_bytes` 等 shim 以及 `docs/rust-refactor/breaks-metrics-templating.md` 的模板输出度量 helper，逐步补齐 `edit_*`/`Breaks` shim。
- **C# Plan**: 在 `node-generic-refactor-plan.md` 定下 `MetricAdapter` 结构，落地 smoke tests（`MetricAdapterTests`），并记录到 `[StageD::FeatureGates]` 以便 CLI/fixture 同步。
- **Status**: ⚠️ Watch — 设计已定稿且 `_editVersion` 为 adapter 铺路，但实现/文档尚未绑定 Stage D manifest，阻碍泛型切换。
- **Links**:
	- `docs/rust-refactor/breaks-metrics-templating.md`
	- `docs/csharp-refactor/node-generic-refactor-plan.md`
	- `tests/xi.Core.Tests/GenericNodeInterfaceTests.cs`
	- `docs/architecture/rope-port-mapping.md#rpm-matrix`
- **Next**:
	1. 于 2025-11-24 前提交 `MetricAdapter` 草案 + smoke 测试，并在 `rope-port-mapping.md`、`m3-implementation-plan.md` 标记 G6 进度。
	2. Online 更新 `[StageD::FeatureGates]` 与 `[Fixture-Manifest]`（添加 `metric_adapter`/`cursor_state` 记录），确保 CLI/manifest 可复现实验。
	3. Call out `_editVersion` -> adapter 依赖图于 Stage D 文档，避免 G6 merge 时追溯困难。
	4. 在 `src/xi.Core/Rope/Tree/` 建立 `TreeBuilderTracer`/`TreeBuilderEventKind`/`TreeBuilderEvent` 骨架（哪怕暂时抛出 `NotImplementedException`），并在 `[StageD::FixtureFlow]` 标注“Rust-only slice trace”降级，待 CLI trace 需要时可接线。

### [TS-B3] Chunk/Line 迭代器与 Telemetry
<a id="TS-B3"></a>
- **Problem**: `RopeChunkEnumerator`/`RopeLineEnumerator` 仍以复制方式提供数据，CLI `--chunk-descriptors` 输出尚未写入 `fixtures.manifest.json`，1 MB `ChunkBench` 基线缺席。Skeleton Coverage 显示 Rust 侧已具备 `ChunkIter`、`LinesRaw`、`ChunkDescriptor`/`LineDescriptor` 导出结构，而 C# 仍只有 diagnostics，没有 DTO/CLI 入口，导致 Stage D manifest 只能依赖 Rust。
- **Rust Plan**: 通过 `iterator-facade-export.md` 评估 owned descriptor/visitor 输出，并在 `export-serde-fixtures` 添加 chunk/line flags。
- **C# Plan**: 维持 diagnostics（`RopeChunkEnumeratorDiagnostics`）并把 Rust JSON + manifest (`[Fixture-Manifest]`) 作为单一事实，一旦 CLI 稳定即把 1 MB 微基准挂到 `[QA-ChunkBench]`。
- **Status**: ⚠️ Watch — 诊断与 telemetry 有数据，但 manifest 与 1 MB baseline 双双缺口。
- **Links**:
	- `docs/rust-refactor/iterator-facade-export.md`
	- `docs/architecture/rope-port-mapping.md#rpm-matrix`
	- `tests/xi.Core.Tests/RopeChunkEnumeratorTests.cs`
- **Next**:
	1. Rust Porter 11/19 demo CLI 输出 -> Architecture Mapper 11/20 前将 `chunk_descriptors.json` 与 `fixtures.manifest.json` 写入 `[RPM-ParityAssets]`。
	2. QA 复跑 `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj -c Release`，把 1 MB 指标写进 `[QA-ChunkBench]` 并同步到 `design-divergence-log.md`。
	3. Once manifest + baseline exist, C# 实现把 copy-on-read 降级策略更新到 `[Div-Active]` exit criteria。
	4. 在 C# 侧补建 `ChunkDescriptor`/`LineDescriptor` DTO（可先放于 `Xi.Core.Rope.Diagnostics` 命名空间）并写明如何消费 Rust manifest，以免 CLI schema 更新时缺少编译期守卫。

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
