# Rope Generic Simplification Blueprint

_Last updated: 2025-11-14_

## 1. Why This Exists
- 目前 Rust 侧 `rope` 模块的核心类型（`Node<N, L>`、`Delta<N, L>`、`Cursor<'a, N, L>` 等）层层传递 `NodeInfo`、`Leaf` 泛型，使得结构分析、移植与测试成本偏高。
- C# 端正推进泛型 `Node<TInfo, TLeaf, TLeafOps>`，但在落地层面逐步暴露 Rust 侧泛型树的复杂性，阻塞了再平衡、Delta/Engine 协同等后续任务。
- 文档目标：提出一套 Rust 端可执行的“泛型瘦身”方案，使核心概念在 Rust/C# 两端均能以更精简的 API 呈现，降低跨语言映射难度并提供迁移路线图。

### 成功判据
1. `tree`/`rope`/`delta`/`subset`/`engine` 等模块的公共 API 参数表减少冗余泛型（至少消除 `DefaultMetricProvider` 额外泛型，`Delta`/`Cursor` 端仅对必要类型开放）。
2. `Rope`、`RopeDelta`、`Subset`、`Engine` 等上层模块能通过具名包装隐藏内部泛型，对外暴露稳定的 concrete 类型。
3. 提供清晰的阶段性实施计划，包含 Rust 端代码调整、测试策略与 C# 侧联动要求。

## 2. 现状速览（来自 skeleton/rope.md）
- `NodeInfo<L>` & `DefaultMetricProvider<L>`：同属节点摘要与 Metric 转换能力，却以两个 trait 存在，导致 `Node` 方法泛型陡增。
- `Leaf`：仅关注叶片拼接/拆分，但实际调用点还需要字符串特化逻辑（C# 侧已抽象为 `ILeafOperations`）。
- `Metric<N, L>`：完全基于静态方法的泛型 trait，每个实现都要反复指定 `N`/`L`。
- `Delta<N, L>`、`InsertDelta<N, L>`、`Transformer<'a, N, L>`：所有编辑管线都继承树的泛型参数。
- `Cursor<'a, N, L>`：生命周期 + 双泛型，在 find/compare/diff/engine 等模块引入复杂签名。
- `Subset` 本身无泛型，但在方法层广泛引用 `Node<N, L>` 与 `Delta<N, L>`，形成传导。

## 3. 目标架构概览
1. **合并节点摘要接口**：
   - 定义 `trait NodeSummary<L>: Clone { ... }`，整合现有 `NodeInfo` 与 `DefaultMetricProvider` 能力。
   - `NodeSummary` 直接提供默认 metric 转换入口（或返回策略对象），避免额外泛型。
   - `Node<N, L>` 调整为 `Node<S, L, Ops>`（S = summary, Ops = leaf helper，可选）。
2. **显式叶操作策略**：
   - 引入 `LeafOps<L>` trait（Rust 侧对应 C# `ILeafOperations<T>`），封装 `push_maybe_split` 前后处理、容量策略、诊断等。
   - `Leaf` trait 退化为 `LeafPayload`（仅约束 Clone/Default/len/is_ok_child），具体操作委派给 `LeafOps`。
   - `StringLeafOps`/`BreaksLeafOps`/`SpansLeafOps` 提供特化实现但共享统一接口。
3. **度量对象化**：
   - 不再要求 `Metric<N, L>` 静态实现；改为 `struct Metric<M>` 或枚举 `enum RopeMetric { Utf8, Lines, Utf16 }`，由 `Node`/`Cursor` 内部 switch 处理。
   - 若仍需扩展型 Metric，提供 trait `MetricStrategy` 与小型 vtable（dyn trait）供少数高级场景使用。
4. **专项化 Delta 管线**：
   - Rust 侧保留 `Delta<N, L>` 但添加专用类型 `struct RopeDeltaCore`，内部固定 `NodeSummary = RopeSummary`、`LeafOps = RopeLeafOps`。
   - `InsertDelta` 替换为 `enum DeltaKind { General(DeltaCore), InsertOnly(InsertSegment) }`，减少 wrapper。
   - `Transformer` 专门服务 rope：`RopeTransformer`，对其他树类型暂不公开。
5. **Cursor 简化**：
   - 将生命周期仅留给借用 rope（`Cursor<'a>`），内部通过 `&'a RopeNode` 工作。
   - Metric 选择在构造时固定（`Cursor::for_metric(RopeMetric::Lines)`），后续遍历无需泛型。
6. **模块封装层**：
   - `Breaks`, `Spans` 等维持泛型树内部实现，但对外暴露 `struct BreakTree`/`struct SpanTree<T>`，隐藏泛型参数。
   - 统一 builder 接口：`TreeBuilder` 调整为非泛型外观（`BreakTree::builder()` 返回封装），避免调用方处理泛型栈。
7. **序列化映射去重**：
   - 将 `RevisionRef/Owned`、`RevisionContents*` 等 serde helper 收敛为 `RevisionSnapshot<'a>` + `RevisionRecord` 两个结构，分别用于遍历和重建。

## 4. 分阶段实施计划

### Phase 0 — 准备 & 验证
- 编写快照测试（Rust）确保 `Node`、`Delta`、`Subset` 核心 API 行为固定。
- 同步刷新 `docs/skeleton/*.md` 与 `docs/csharp-refactor/node-generic-refactor-plan.md`，记录起点状态。

### Phase 1 — 核心 trait 改造
- 引入 `NodeSummary`/`LeafOps`/`LeafPayload` 新接口，逐步替换原有 trait。
- `Node`、`SharedNode`、`TreeBuilder` 改签名；提供 `type RopeNode = Node<RopeSummary, String, RopeLeafOps>` 别名。
- 迁移现有 `RopeInfo` -> `RopeSummary`，将 metric 转换逻辑内联。
- 运行 `cargo test -p xi-rope`、`dotnet test`（确认 C# 测试仍编译），更新黄金结果。

### Phase 2 — Metric 系统瘦身
- 将 `Metric` trait 替换为 `RopeMetric` 枚举 + 内部 helper；`Cursor`/`Node::measure`/`Node::count` 接收该枚举。
- 提供临时 adapter 保留旧 API（deprecate），确保外部调用逐步迁移。
- 更新 `metrics/` 模块，拆分 `*_helper.rs` 供新结构引用。

### Phase 3 — Delta/Subset 重构
- 引入 `RopeDeltaCore`，迁移 `Delta<RopeSummary, String>` 功能至新结构，保留旧别名作 shim。
- 替换 `InsertDelta`/`Transformer` 为新枚举与变换器；更新 `engine`、`spans`、`diff` 等模块的调用。
- `Subset` API 改为以 `RopeNode` 为默认目标；涉及其他树的场景引入 trait bound adapter。

### Phase 4 — Cursor & Builder 封装
- 实现 `RopeCursor`，替换 `Cursor<'a, RopeInfo, String>` 用例。
- 为 `Breaks`、`Spans`、`TreeBuilder` 引入专用 builder struct，原泛型 builder 标记为 internal。
- 确认 `find`、`compare`、`diff`、`spans` 测试通过。

### Phase 5 — 序列化与 Engine 收敛
- 重写 serde helper 结构体，减少重复视图。
- 更新 `engine` 逻辑以使用 `RopeDeltaCore` 和新 `Subset` API。
- 刷新 `serde_fixtures` 与 C# 夹具，执行 `scripts/refresh_serialization_fixtures.ps1` 全量流程。

### Phase 6 — 清理 & 文档
- 移除旧 trait/shim，更新所有引用。
- 刷新 `docs/skeleton/rope.md`、`docs/architecture/rope-port-mapping.md` 与本文件进度。
- 在 C# 侧同步最终签名，确保双端保持一致。

## 5. 风险与缓解
- **Trait 替换导致编译风暴**：采用 `#[cfg(feature = "legacy-sig")]` 或 type alias shim 支撑过渡；阶段内保持全量测试频繁执行。
- **Metric 对象化引发性能回退**：为 `RopeMetric` 提供微基准，对比旧实现；必要时保留内联函数避免动态 dispatch。
- **Delta/Subset 调整破坏 serde 黄金**：每阶段完成后运行导出脚本 + `dotnet test`，并更新夹具。
- **游标重写影响 find/diff 语义**：增加 property test 或 corpus 测试比较旧/新游标输出。
- **C# 同步压力**：阶段交付后在 `docs/csharp-refactor/node-generic-refactor-plan.md` 标记对应调整，明确需要的 .NET 改动窗口。

## 6. 任务清单（按优先顺序）
1. **定义 NodeSummary & LeafOps 接口**：完成 trait/struct 初稿，提供 `RopeSummary`、`RopeLeafOps` 实现。
2. **重构 Node/SharedNode 签名**：落地新的泛型排列并保持现有 API 兼容层。
3. **替换 Metric trait**：实现 `RopeMetric` 枚举与 helper，迁移 `Node::measure`/`Cursor` 使用。
4. **实现 RopeDeltaCore + Transformer 替代方案**：更新 `delta`, `engine`, `spans` 调用链。
5. **Cursor 简化为 RopeCursor**：重写游标缓存结构，更新相关模块。
6. **Builder 封装层**：新增 `RopeBuilder`、`BreakBuilder`、`SpanBuilder` 等友好 API。
7. **Serde 视图归一化**：统一 `Revision` 相关 snapshot/record 类型并更新导出脚本。
8. **文档巡检与 C# 对齐**：刷新 skeleton、port-blueprint、C# 规划文档。

## 7. 协同指引
- 每个阶段完成后刷新 `docs/skeleton/rope.md` 及 C# skeleton，保持跨语言视图同步。
- 在 `AGENTS.md` 与 `docs/architecture/port-blueprint.md` 中登记阶段进度和风险更新。
- C# 端可先行对齐 `LeafOps` 思路（已经有 `ILeafOperations<T>`），后续等待 Rust 提供更稳定的泛型接口再做深度合并。
- 若阶段中需要大量符号替换，优先使用自动化脚本（如 `runSubAgent` 驱动批量重命名）减少人工出错。

---

此方案旨在通过模块化的泛型瘦身，压缩跨语言迁移的心智与维护成本。执行过程中如出现与现有测试/文档冲突，应以最终统一架构为优先，必要时调整 C# 实验实现以配合新的 Rust 设计。

**主要问题**
- `NodeSummary & LeafOps` 方案会让 `Node` 从当前的 `Node<N, L>` 膨胀成 `Node<S, L, Ops>`，同时 `TreeBuilder`、`SharedNode`、`Cursor` 等整棵树的实现都要同步改写；这一改动不仅没能“瘦身”泛型，反而新增一个类型参数，并要求所有叶子调用点改用外部 helper 执行 `push_maybe_split`/`subseq` 等操作。现有实现大量依赖直接对 `L` 调用方法（例如 `Node::merge_leaves`、`TreeBuilder::push` 内的 `with_leaf_mut`），要彻底抽离需要把整个树编辑路径重写一遍，风险远高于文档估计。
- 将 `NodeInfo` 与 `DefaultMetricProvider` 合并为统一的 `NodeSummary` 不符合当前使用面。像 spans.rs 的 `SpansInfo<T>`、`interval.rs` 的区间摘要都只需要聚合信息而不需要度量换算，强行让它们实现 `convert_from_default/convert_to_default` 会引入“无意义默认实现”或大量样板，同时这些类型常被泛型 `TreeBuilder`/`Subset` 复用，接口变化会造成级联爆炸。
- 把 `Metric` trait 换成 `RopeMetric` 枚举会丢失泛型静态派发带来的优化，并且无法覆盖 `BreaksMetric`、`SpansMetric` 等类型专属逻辑；`Cursor<'a, N, L>` 目前完全依赖 `Metric<N, L>` 的关联类型语义，改成运行时枚举需要所有调用者分支匹配，性能和可维护性都会显著下降。
- 计划中的 `RopeDeltaCore`/`RopeTransformer` 专用化忽视了 `delta.rs` 在 `spans.rs::Spans<T>::transform` 等场景下的通用用途（`Transformer<'_, N, L>` 目前允许任意树类型参与 OT）；把 Delta/Transformer 绑死在 Rope 会迫使 `spans`、`engine`、`diff` 等模块再次实现一套平行管线，违背“减负”初衷。
- Phase 分解中多处假定“提供 shim 即可”但缺乏可执行细节：例如 Phase 1 要同时替换 `Node`、`TreeBuilder`、`SharedNode`、`Cursor` 的泛型签名，却未说明如何在编译器一次性通过；Phase 2/3 又要求 serde/引擎全部覆盖，工作量与回归风险被严重低估。

**沟通点**
- 文档是否只针对 Rope 单一用途？如果仍需支持 `Breaks`/`Spans` 等结构，需要重新定义“瘦身”边界，否则那些模块必须另起一套实现。
- 有没有评估过在 Rust 端保留泛型、只在对外 API（`pub type Rope = Node<…>`）层做包装即可满足 C# 映射？这能避免破坏底层 trait 网。

**后续建议**
- 先在北向文档里缩小目标：例如只探讨“如何减少对外暴露的泛型”，而不是重写核心 trait。
- 若确有需要抽离叶助手，可先在 C# 侧总结需求，试着为 Rust `String` 叶子单独引入 helper struct，验证一条编辑路径后再评估推广的代价。

我看过整体架构后，觉得“彻底改写 trait 网络”风险太高；可以换个方向，从**收敛对外 API、局部抽离重复能力**入手。下面这版“温和瘦身”在当前代码上可落地，且方便逐步评估。

**可行方案**

- **外观统一**：在 `rope::`, `breaks::`, `spans::` 等模块内部新增 `pub struct RopeNode(Node<RopeInfo, String>);` 这一类的薄包装，加 `Deref`/`From`/`Into` 实现，把泛型隐藏在模块边界之内。既能维持内部共享实现，也能对外显露稳定 concrete 类型。

- **Rope 专属 helper 层**：提取现有 `Rope` 相关自由函数（比如 `count_newlines`, `find_leaf_split_for_merge`）到 `rope::helpers::{…}`，收束泛型调用点；C# 迁移时可以直接映射这些 helper，无须理解 `Node<N,L>` 细节。

- **Metrics “模板化”**：保留 `Metric<N,L>` trait，但在 `metrics/` 下集中放共通逻辑（`base_units.rs`, `lines.rs` 等），对其他模块提供纯函数 API（如 `measure_lines(node: &RopeNode) -> usize`）。这样既避免运行时分派，又减少重复泛型栈。

- **Leaf 操作抽象分层**：不强制所有叶实现 `LeafOps`；先为 `String` 叶单独写一个 helper（类比 C# `StringLeafOperations`），只把 `push_maybe_split` 附带的复杂流程搬出去。节点内部依旧直接调用 leaf 方法，修改范围相对聚焦。

- **Delta/Transformer 专用别名**：继续使用通用 `Delta<N,L>`，但在 `rope` 模块中定义 `type RopeDeltaCore = Delta<RopeInfo, String>`、`struct RopeTransformer<'a>(Transformer<'a, RopeInfo, String>);` 等别名/封装，让上层只面对固定类型。

**推进节奏**

1. **Phase A：包装层**  
   - 为 Rope/Breaks/Spans 定义新 wrapper 与 type alias，补充 `impl From<Node<…>>`、`impl Deref`.  
   - 调整 rope.rs 等模块暴露 wrapper，而内部仍复用泛型函数。  
   - 验证：`cargo test -p xi-rope`。

2. **Phase B：Rope helper 与 leaf 拆分**  
   - 迁移 `find_leaf_split_for_merge`、`count_newlines_bytes` 等函数到 `rope::helpers`.  
   - 添加 `rope::leaf_ops::string_leaf_push_maybe_split` 等轻量 helper，并在节点合并路径调用。  
   - 验证：现有并发/serde 回归都跑。

3. **Phase C：Metric 模块整理**  
   - 把 `count_utf16_code_units` 等散落函数集中到 `metrics/helpers`.  
  - 对外提供 `RopeMetrics::{base_len, line_count}` 一类 API，内部仍依赖 `Metric` trait。  
   - 引入基准脚本快速对比性能，确保未退化。

4. **Phase D：文档与对外同步**  
   - 更新 rope.md 反映 wrapper/模块划分。  
   - 在 rope-generic-simplification.md 重写方案描述，明确“不改 trait 核心，只在边界遮蔽泛型”的策略。

这样做的好处是：

- 不触动 Node/Tree 核心 trait，因此改动编译面和回归成本可控。
- 对外消费方（无论是 Rust 其它 crate 还是 C# port）看到的是 concrete 类型+小型 helper，心智复杂度下降。
- 后续若仍想进一步泛型瘦身，可在 wrapper 基础上继续实验（例如只对 Rope 路径实现定制 TreeBuilder），不会一次性推倒重来。

建议先着手 Phase A/B——改动范围小，但很快能衡量收益。如果后续希望在泛型层继续探索，可再讨论更激进的变体。
