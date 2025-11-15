
# Metric Conversion & Edit Into `Node`

## 调研结论（2025-11-15 实地核对）
- `tree.rs` 保留了核心入口：`Node::convert_metrics`（行 540-576）、`Node::count`（行 584-600）、`Node::count_base_units`（行 604-619），以及泛型 `Node::edit`（行 525-538），全部位于 `xi-editor-ph7/rust/rope/src/tree.rs` 并复用 `DefaultMetricProvider` 的静态方法调度。
- 当前仅有两组 `DefaultMetricProvider` 实现：`RopeInfo`（`rope.rs` 行 138-173）与 `BreaksInfo`（`breaks.rs` 行 74-117），均直接调用 `Node::convert_metrics`，没有额外逻辑。
- 主要运行时调用集中在：`Rope::line_of_offset`/`offset_of_line`（`rope.rs` 401-438）、`Breaks` 软换行管线（`core-lib/src/linewrap.rs` 158-417, 768-781）以及 `LineOffset for Breaks`（`core-lib/src/line_offset.rs` 96-107）。测试覆盖来自 `rope.rs` 1030-1085 与 `breaks.rs` 270-285 的默认度量对拍。
- 仓库未发现第三方 `NodeInfo`/`DefaultMetricProvider` 实现；外部模块都通过 `Rope` 或 `Breaks` 别名持有 `Node`。

## 迁移痛点
- C# 虽已引入 `IDefaultMetricProvider`/`ITreeNodeInfo` 等 *static abstract* 契约，但尚无与 `Node::count`/`count_base_units` 等价的节点级包装；直接移植 Rust 泛型层需要额外类型参数与约束，样板成本高。
- `Node::edit<T: Into<Node>>` 的多态在 C# 中没有一手镜像方案；目前的实现只需要消费已构建的 `Node`，但缺少等价的具象入口。
- 软换行依赖的 `Breaks::count::<BreaksMetric>` 与 `count_base_units::<BreaksMetric>` 没有非泛型替代品，阻塞了 `Breaks` 在 C# 侧的规划。

## 重构目标
1. 在 Rust 侧提供面向 `Rope` 与 `Breaks` 的“互操作 shim”，让 C# 可以直接调用具象方法（如 `Rope::convert_lines_from_bytes`），而无需复刻泛型调度。
2. 在 C# 侧对接 shim，暴露 `Rope.ConvertLinesFromBytes`、`Rope.ConvertBytesFromLines`、`Rope.ConvertUtf16FromBytes` 等 API，并为即将落地的 `Breaks` 骨架保留同名接口。
3. 通过 parity 测试与黄金夹具，确保 shim 与泛型实现结果一致，防止未来漂移。

## 交付蓝图

### Phase 0：防错与基线
- Rust：为 `Node::convert_metrics`、`Node::count`、`Node::count_base_units` 添加单元测试注释链接，以利后续 shim 对齐；校验现有测试覆盖是否涉及 UTF-16、换行、软换行等关键场景。
- 文档：在 `docs/architecture/rope-port-mapping.md` 标记当前阻塞点。

### Phase 1：Rope Shim（Rust）
- 新增 `impl Rope` 方法：
	- `convert_lines_from_bytes(offset: usize) -> usize`
	- `convert_bytes_from_lines(line: usize) -> usize`
	- `convert_utf16_from_bytes(offset: usize) -> usize`
	- `convert_bytes_from_utf16(units: usize) -> usize`
- 每个方法内部仅调用 `self.count::<Metric>` 或 `self.count_base_units::<Metric>`，并以 `#[inline]` 暴露。
- 为避免 API 膨胀，可选通过 `#[cfg(feature = "portability_shims")]` 暴露；默认开启以支持 C# 镜像。
- 在 `rope/src/tests` 增加 parity 用例：对比 shim 与泛型调用、覆盖 surrogate、混合换行、空文本等情形。
- _Status update (2025-11-15)_: `Rope::convert_lines_from_bytes`, `Rope::convert_bytes_from_lines`, `Rope::convert_utf16_from_bytes`, and `Rope::convert_bytes_from_utf16` now live in `xi-editor-ph7/rust/rope/src/rope.rs` as portability shims for downstream bindings。

### Phase 2：Breaks Shim（Rust）
- 在 `impl Breaks` 中新增：
	- `count_breaks_up_to(offset: usize) -> usize`
	- `offset_of_break(index: usize) -> usize`
	- `count_breaks_in_range(range: Range<usize>) -> usize`
- 与 Phase 1 同样走 `count::<BreaksMetric>` / `count_base_units::<BreaksMetric>`。
- 增补 `breaks.rs` 现有测试，验证 shim/泛型一致；在 `core-lib` wrap 流程中追加最小黄金案例（如 80 列软换行）。
- _Status update (2025-11-15)_: `Breaks::count_breaks_up_to`, `Breaks::offset_of_break`, and `Breaks::count_breaks_in_range` now wrap the generic metric helpers with rustdoc-shim documentation, with parity tests living in `xi-editor-ph7/rust/rope/src/breaks.rs` and `Lines::visual_line_of_offset` updated to consume the new shim.

### Phase 3：C# 对接
- 在 `Rope` 与后续的 `Breaks` 封装上添加同名方法，内部调用现有 `Node` 泛型助手或直接委派至 Rust shim 生成的黄金数据。
- 为 `Rope` 引入针对 `ConvertLinesFromBytes` 等方法的单元测试，复用 serde 黄金串与 parity fixture（如 `leaf_split_parity_samples.json`）。
- 规划 `Breaks` 类型骨架：`Node<BreaksInfo, BreakLeaf>` + `BreaksMetricHelper`，并绑定 Phase 2 shim 约定的 API。

### Phase 4：文档 & 自动化
- 更新 `docs/architecture/rope-port-mapping.md`、`docs/csharp-refactor/rope-cow-rebalance-plan.md` 反映 shim 状态。
- 在 Stage D 共享夹具脚本中加入 shim 验证步骤（Rust `cargo test -p xi-rope portability_shims` + C# `dotnet test`）。
- 将 shim 行为纳入 `design-divergence-log.md`，明确其“仅为互操作提供”的定位。

## 验证策略
- **Rust**：`cargo test -p xi-rope`（默认 + `--features portability_shims`）；`cargo test -p xi-core-lib --lib` 覆盖 wrap 管线。
- **C#**：`dotnet test tests/xi.Core.Tests --filter RopeMetricInterops`（新增分类）以及 `BreaksMetricHelperTests` 扩展。
- **差异检测**：为关键测试记录黄金输出（如 UTF-16 ↔ UTF-8 偏移）并在脚本中比对。

## 风险与缓解
- **API 漫延**：限定 shim 在文档与命名上标注为“interop helper”；任何新 `NodeInfo` 需求需走评审流程。
- **测试缺口**：在 parity 用例中覆盖 surrogate、mixed newline、wrap 边缘等场景；一旦 shim 行为偏离泛型实现，测试将立即失败。
- **特性开关复杂度**：若引入 `portability_shims` 特性，CI 需同时运行启用/禁用模式；脚本中明确执行命令，避免遗漏。

## 未决问题
- 是否需要额外 shim（如 `Subset`/`Delta`）以匹配后续 C# 阶段需求？
- `Breaks` 在 C# 侧的叶片类型与存储策略（字符串 vs 索引列表）尚未定稿，可能影响 shim 的最终签名。
- 若未来出现第三方 `NodeInfo` 扩展，如何向外部消费者说明 shim 的可用性范围？

## 下一步建议
1. 在 Rust 原仓执行 Phase 1，实现 `Rope` shim 并补充 parity 测试。
2. 并行准备 Phase 2 设计草稿，确认 `Breaks` shim 需要的最小 API。
3. 在 C# 侧预留 `RopeMetricInteropTests` 测试骨架，待 Rust shim 落地后快速接入。
