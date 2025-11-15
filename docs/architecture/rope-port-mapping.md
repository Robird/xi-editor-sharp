# Rope 文件级映射与类型翻译计划

## 目标
建立 `reference/rust/rope` 与 `src/xi.Core/Rope` 之间的一一映射，记录每个模块的移植状态，并沉淀 Rust → C# 在接口与类型层面的翻译范式，支撑“先契约后实现 + Rust 侧迁移友好化”的移植流程。Stage D 负责依托 `docs/architecture/rope-serialization-fixture-playbook.md` 维护共享 JSON 夹具与 CI 钩子，确保映射表与黄金资产保持同步。

## 状态标签
- `未开始`：尚未在 C# 侧创建对应文件或类型。
- `仅骨架`：已建立目录/类型壳子但缺少真实逻辑。
- `实现中`：核心逻辑正在移植或调试，测试逐步补齐。
- `Rust 重构中`：C# 侧暂缓，等待 `xi-editor-ph7` 提供迁移友好 helper 或结构调整。
- `已实现`：关键能力与诊断均已移植，后续仅保留优化或性能工作。

## 最新进展（2025-11-15）

## 路径映射约定
- `reference/rust/rope/` ↔ `src/xi.Core/Rope/`
- `reference/rust/rope/tree.rs` ↔ `src/xi.Core/Rope/Tree/` （当前包含 `Node.cs`、`TreeBuilder.cs`、`LeafSplitter.cs`，后续 Tree 相关类型统一进入该子目录与命名空间 `Xi.Core.Rope.Tree`）
- `reference/rust/rope/rope.rs` ↔ `src/xi.Core/Rope/` 根目录（`Rope.cs`、`RopeInfo.cs`、`Metrics.cs`、`IMetric.cs` 等）
- 其余 Rust 模块按子目录映射：例如 `delta.rs` 计划对齐至 `src/xi.Core/Rope/Delta/`，`interval.rs` 对齐至 `src/xi.Core/Rope/Intervals/`

## 文件级映射表
| Rust 模块 | 关键类型/职责 | C# 目标文件/目录 | 当前状态 | 备注 |
|-----------|---------------|-------------------|----------|------|
| `tree.rs` | `Node`, `SharedNode`, `TreeBuilder`，负责节点借用/合并、再平衡骨架 | `Tree/Node.cs`, `Tree/Node.Generic.cs`, `Tree/TreeBuilder.cs`, `Tree/LeafSplitter.cs` | 实现中 | 写时复制 helper 已对齐；内部再平衡与诊断计数器尚未接入，`Node.Generic.cs` 仍待并入主实现。 |
| `tree.rs`（游标相关） | `Cursor`, `CursorIter`, `BalanceIter` 等遍历结构 | `Tree/NodeCursor.cs`（骨架） | 实现中 | `CursorDescriptor` 为移植必需能力，C# 需实现等效的拥有型描述符；`cursor_state` feature gate 仅在 Rust 侧提供可选持久化快照（`Cursor::state()`/`CursorState`），可作为 C# 设计参考但无需一比一复刻 gate。 |
- **Grapheme 降级实现确认**：Rust 通过 `unicode_segmentation::GraphemeCursor` 完成字素粒度移动，C# 初版仅保证不拆分 surrogate，对上下文最多补一片并在不足时退回 code point；该策略已在 2025-11-15 设计分歧日志登记，后续若需要更完整行为，再评估引入 `StringInfo`/ICU4N 或 Rust trace。 
| `helpers/string_leaf.rs` | 字符串叶片容量常量、拆分策略与 UTF-16 计数 helper | `Tree/StringLeafOperations.cs` | 已实现 | 注意记录 UTF-8/UTF-16 单位差异，继续扩充对拍样本。 |
| `delta.rs` | `Delta`, `InsertDelta`, `Transformer` 协作算法 | `Rope/Delta.cs`, `Rope/DeltaJson.cs` | 实现中 | Stage B 镜像完成；`Transformer` 与 `factor()` 仍为 TODO。 |
| `interval.rs` | 区间结构 | `Rope/Interval.cs` | 已实现 | 后续若新增 `IntervalTree` 需更新目录映射。 |
| `multiset.rs` | `Subset`/`SubsetBuilder` | `Rope/Subset.cs`, `Rope/SubsetJson.cs` | 已实现 | Stage A 序列化回归已经纳入基线。 |
| `engine.rs` | 编辑引擎、Undo/Redo | `Rope/Engine.cs`, `Rope/EngineJson.cs` | 已实现 | Stage C 完成镜像与黄金夹具。 |
| `diff.rs` | 文本 diff 逻辑 | 规划为 `Diff/` 目录 | 未开始 | 待建立骨架，评估直接移植或复用 .NET 库。 |
| `compare.rs` | Rope 比较工具 | 规划为 `Diff/Compare.cs` | 未开始 | 涉及 SIMD 指令，C# 需选中性实现或 `System.Runtime.Intrinsics`。 |
| `breaks.rs` | 换行/断点索引 | 规划为 `Tree/Breaks.cs` | 未开始 | 与 `BreaksMetricHelper` 配合，需在树层打通。 |
| `find.rs` | Rope 搜索 | 规划为 `Search/Find.cs` | 未开始 | 依赖 `Cursor` 与 `Regex`，需引入 Span 友好实现。 |
| `spans.rs` | 样式跨度 | 规划为 `Search/Spans.cs` | 未开始 | 与搜索/高亮管线绑定。 |
| `serde_impls.rs` | Rope serde 支撑 | 规划为 `Serialization/` | 未开始 | 根据宿主需求决定是否拆分为多个 converter。 |
| `lib.rs` | 模块导出 | Solution 顶层 | 已实现 | 由 `Xi.Editor.sln` 统一管理。 |
| `test_helpers.rs` | 测试支撑 | `tests/xi.Core.Tests/RopeTestHelpers.cs` | 已实现 | 不变量断言/诊断输出已对齐。 |

## 树/绳映射准备清单（2025-11-15）

### 已可直接建立 C# 骨架的符号

| Rust 符号/片段 | 职责摘要 | C# 映射现状 | 后续动作 |
|----------------|-----------|---------------|-----------|
| `NodeInfo<L>`, `DefaultMetricProvider<L>`, `Leaf` | 约束节点聚合信息与叶片接口 | `Tree/TreeContracts.cs`, `Tree/StringLeafOperations.cs` 已提供 `ILeafOperations<string>` 与静态 helper | 补充默认实现注释与契约说明，避免泛型化时再命名 |
| `SharedNode`, `Node`, `NodeBody`, `NodeVal` | 树节点写时复制核心 | `Tree/Node.cs`, `Tree/Node.Generic.cs` 已接入 `SharedNode` 包装 | 在主实现补齐诊断计数器与 COW instrumentation |
| `TreeBuilder<N, L>` | 批量构建/再平衡入口 | `Tree/TreeBuilder.cs` 已落地 | 校对 `push_slice` 与 Rust 栈策略，补多 Metric 插入测试 |
| `TreeBuilderEventKind`, `TreeBuilderEvent`, `TreeBuilderTracer<N, L>` | 构建阶段事件追踪与可选 tracer | C# 未实现 tracer | 直接映射为枚举 + 接口，并用调试开关模拟 feature gate |
| `PathFrame`, `CursorDescriptor`, `CursorState`（feature gate） | 游标缓存的无借用快照入口 | `Tree/NodeCursor.cs` 尚未引入描述符 | 首要任务是对齐 `CursorDescriptor`/`PathFrame`；`cursor_state` 属 Rust 端可选持久化实现，C# 可自定义拥有型缓存结构，无需实现同名 feature gate |
| `Metric<N, L>` 及 `helpers/string_leaf.rs` 提供的度量 helper | 度量与边界查询统一依赖 helper 常量 | `IMetric.cs`, `Metrics.cs`、`StringLeafOperations.cs` 已引用常量 | 对齐常量命名并增加共享文档，避免重复实现 |
| `RopeInfo`, `Leaf for String`, `MIN_LEAF`/`MAX_LEAF`/`NEWLINE_WINDOW` | 聚合信息与字符串叶片拆分策略 | `RopeInfo.cs`, `StringLeafOperations.cs` 已同步常量 | 将 Rust 新增 helper（UTF-16 计数、bulk/merge 拆分）函数签名补全记录 |
| `Node::count`, `Node::count_base_units`, `convert_metrics` | Metric 间互相转换 | `Node.cs` 中已有占位实现 | 接入 `IMetric` 桥梁方法并补单元测试 |

### 暂需额外设计或信息的符号

| Rust 符号/片段 | 当前阻碍 | 影响 | 计划 |
|----------------|-----------|------|------|
| `Cursor<'a, N, L>`, `CursorIter<'a, …>` | 仍依赖借用生命周期与 `Option<&'a L>` 缓存叶片；`CursorDescriptor` 已对外稳定，`cursor_state` 作为可选 gate 扩展持久化状态 | 游标遍历、查找、Diff/Find 等都取决于游标语义 | C# 必须实现 `CursorDescriptor` 等效路径，并根据需要引入拥有型缓存（不要求复刻 feature gate）；通过 `SharedNode` + 索引缓存替代生命周期 |
| `Rope::prev_grapheme_offset`, `next_grapheme_offset`, `Cursor::prev_grapheme`, `Cursor::next_grapheme` | 用 `unicode_segmentation::GraphemeCursor` 及 `GraphemeIncomplete` 驱动跨叶状态机，C# 无直接库可复刻 | 多语言光标、删除、插件同步需精准 grapheme 语义 | 降级实现已在 2025-11-15 设计分歧中确认（单片 + 相邻片 + code point备用），视为当前既定策略；若真实需求出现，再评估 ICU4N 或 Rust trace 挂钩 |
| `Rope::iter_chunks`, `LinesRaw`, `Lines`, `ChunkIter` | 迭代器返回借用的 `&str`/`Cow<str>`，依赖 Rust 借用与 `SmallVec` | Rope diff、序列化、插件 API 需要零拷贝块访问 | 设计 `ReadOnlyMemory<char>` 或 `ChunkDescriptor` 返回类型，并评估是否落地 `Span`-based 枚举器 |
| `Tree::convert_metrics`, `Node::edit` 的 `Into<Node>` 管道 | Rust 静态分发支持零拷贝转换，C# 需静态抽象接口配合泛型节点 | 影响 Delta/Subset、Rope 编辑互转路径 | 在 `Node.Generic.cs` 明确静态抽象成员与 helper 入口，必要时提供专用 shim |
| `helpers/string_leaf.rs::find_leaf_split_for_bulk`/`for_merge` | 返回值仍为 UTF-8 字节偏移，C# 叶片以 UTF-16 计量 | Bulk 构建与合并路径仍需额外扫描二次换算 | 评估 Rust 侧能否返回 byte + utf16 双指标；短期依赖 `leaf_split_parity_samples.json` 做对拍 |
| `TreeBuilder::push_slice`, `Node::subseq` | 大量使用临时 `Vec<Node>` 及栈式拆分，需在 C# 中重建批量拼接策略 | Rope 编辑与 Delta 应用的性能关键路径 | 整理 Rust slice trace（`tree_builder_slice_trace`）为对照，C# 侧考虑 `ArrayPool<Node>` 或结构体栈缓冲 |

> 注：表格只列出首批重点模块，可在实际推进中扩充行或拆分更细粒度的子文件（例如 `tree/node.rs`、`tree/edit.rs` 等）。

> 偏移单位提示：Rust `helpers/string_leaf.rs` 中的拆分函数始终返回 UTF-8 字节偏移；C# `StringLeafOperations` 与测试则以 UTF-16 `char` 计数。跨语言对拍或文档引用拆分位置时，需显式标注单位并避免混用。

### Rust 侧可移植性改造建议（2025-11-15）

- **`Cursor<'a, N, L>` 缓存**：`CursorDescriptor` 已常驻导出并应视为移植基线，负责将缓存路径存成 `Arc<NodeBody>` + 子节点索引集合，供其他语言无生命周期约束地重建游标；`cursor_state` feature gate 额外提供拥有型快照，可供 C# 设计参考但不要求同步 gate。 
- **字素导航（已记录设计分歧）**：C# 已确认采用 surrogate 安全的降级策略（见 2025-11-15 设计分歧日志），Rust 侧短期无需额外导出 `GraphemeCursor` trace；仅在后续确有追平需求时，再评估 `GraphemeStep` 助手的公开形态。
- **Chunk 元数据**：为现有 `iter_chunks` 补充 `iter_chunk_descriptors` 之类伴随 API，输出 `(byte_len, utf16_len)` 元信息，避免在移植侧重复 UTF-8 → UTF-16 统计。
- **Metric/Into<Node>` 抽象**：提供面向 `RopeInfo` 的非泛型 shim（如 `rope::ops::count_lines`, `rope::ops::edit_str`），将复杂的 `Into<Node>`/静态 trait 成员留在内部，实现跨语言访问的稳定入口。
- **叶片拆分回传**：让 `helpers/string_leaf.rs` 的 `find_leaf_split_*` 返回同时包含字节与 UTF-16 长度的结构体，减少移植侧的重复扫描。
- **切片计划诊断**：在 `TreeBuilder::push_slice`/`Node::subseq` 增设守护特性（例如 `collect_slice_plan`），记录节点 push/pop 序列和区间变换，帮助 C# 复刻栈化策略并在测试中比对。

#### 阻塞项的 Rust 端重构评估（2025-11-15）

| Rust 符号/模块 | 当前阻塞模式 | 建议的 Rust 重构/Helper | 可行性评估 | 风险与依赖 |
|----------------|--------------|--------------------------|-------------|-------------|
| `Cursor<'a, N, L>`, `CursorIter<'a, …>` | 游标缓存依赖生命周期与 `Option<&'a L>`，绑定侧难以镜像 | 强化 `CursorDescriptor`（必需）并在需要时提供拥有型 `CursorState` 辅助：导出 `Cursor::from_descriptor_owned`, `CursorState::restore` 等 helper，统一返回 `Arc<NodeBody>` + 子索引路径 | 中等：核心逻辑已存在于 `to_descriptor`/`apply_descriptor`，`cursor_state` 已以 feature gate 形式提供可选实现 | 需保证 `Arc::ptr_eq` 的稳定语义；公开额外状态时注意与 feature gate 默认策略的一致性 |
| `Rope::prev_grapheme_offset`、`Cursor::prev_grapheme` 等 | 依赖 `unicode_segmentation::GraphemeCursor` 驱动跨叶状态机 | （已撤回）C# 侧通过 2025-11-15 设计分歧采用降级策略，Rust 暂不需新增 helper；仅保留未来可选的 trace 研究方向 | 不适用（当前策略已定） | 后续若恢复追平需求，再重新评估 helper 发布与兼容性 |
| `Rope::iter_chunks`、`LinesRaw`、`Lines`、`ChunkIter` | 迭代器返回借用 `&str`/`Cow<str>`，移植方需复制或重写 | 提供 `ChunkDescriptorIter`/`LineDescriptorIter` 返回 `(byte_range, utf16_units, newline_flag)` 等元信息，并附 `to_cow_owned` 辅助 | 较易：可复用现有游标与 helper，只需包装层 | 需关注 UTF-16 计数性能；API 需与现有 iterator 共存避免破坏现有调用 |
| `Tree::convert_metrics`、`Node::edit` + `Into<Node>` | 泛型 trait 约束难以在 C# 表达 | 发布 `Rope` 专用 shim（`convert_bytes_to_lines`, `convert_lines_to_bytes`, `edit_interval_str` 等）隐藏泛型 | 容易：已有类似 `convert_utf16_from_bytes` 先例，新增函数即可 | 必须与泛型实现保持同步；若未来泛型更换叶类型需更新文档 |
| `helpers/string_leaf.rs::find_leaf_split_for_*` | 仅返回 UTF-8 偏移，C# 需重新计算 UTF-16 | 返回 `LeafSplit { byte_offset, utf16_units }` 或新增双指标版本，内部复用一次扫描 | 容易：变更范围局限 helper 与调用点；可保留旧 API 兼容 | 需同步更新测试/fixture，若对 FFI 暴露需考虑 `repr` 和兼容性 |
| `TreeBuilder::push_slice`、`Node::subseq` | 栈式 `Vec<Node>` 与复用策略黑箱，难以做等价实现 | 把 `tree_builder_slice_trace` 提升为运行时开关或提供 `plan_slice` helper 输出确定性事件流 | 中等：trace 框架已存在，主要工作是整理为稳定 API | 需验证 trace 开销；若默认启用需评估性能与二进制尺寸影响 |

### C# 侧创造性重新设计建议（2025-11-15）

| C# 对应问题 | 设计思路 | 测试 / 依赖 | 风险 / 权衡 |
|---------------|-----------|--------------|--------------|
| `NodeCursor` 与遍历迭代器 | 将游标实现为基于 `ValueListBuilder<int>`（或等效栈结构）的 `struct` 状态机，复用 `SharedNode` 与 `Node.Generic` 聚合信息，通过 `SpanStack`/池化数组缓存父链；对外提供 `CursorBoundaryIterator`/`CursorMetricEnumerator`，以 `ReadOnlyMemory<char>` 暴露叶片并与现有 `ILeafOperations` 对齐 | 依赖 `NodeCursor` 新增的单元测试与 parity fixture，对比 Rust `CursorDescriptor` 输出；若参考 `CursorState` 思路，可在 C# 侧实现拥有型缓存而不必提供 feature gate | 增加实现复杂度并限制异步使用场景；池化缓冲若滞留可能造成泄露，需约束 API 生命周期与调试工具 |
| Grapheme 导航 API | 维持 surrogate 安全的降级实现，并在 `NodeCursor` 暴露可观测钩子记录跨叶补片次数；预留可插拔接口以便未来切换到 ICU4N 或 Rust trace | 扩展降级版单元测试与 parity fixture（确认不会拆分 surrogate 对），并收集 telemetry 统计降级命中率 | 与 Rust 语义仍存在差异，需要在文档与 API 注释中强调；过度补片可能影响极端文本下性能 |
| `Rope` 块/行迭代器 | 构建 `RopeChunkEnumerator`/`RopeLineEnumerator` 结构体，直接迭代叶节点并返回 `ReadOnlyMemory<char>` 或轻量 `ChunkView`，`Lines` 接口在其上完成换行拆分并复用 `StringLeafOperations` 的 newline helper | 扩展现有 Rope 测试，确保拼接 chunk/line 结果还原原始文本；添加诊断计数（chunk 数量、最大长度）辅助性能回归分析 | 结构化枚举器实现较复杂，消费方若依赖 `string` 仍需转换；若不慎使用 `yield` 将引入额外分配 |
| Metric 转换与 `Node::edit` | 引入 `MetricAdapter` 与 `NodeEditor` 辅助类型，围绕 `Node<TInfo,TLeaf,TLeafOps>` 将 `convert_metrics`、`count`、`edit` 封装成声明式 API，并通过 `INodeConvertible` 接口统一 string/span/node 输入 | 需复用 Stage A-C 的 JSON fixture 与新增单元测试比较 Adapter 输出；Instrumentation 记录编辑路径与 COW 命中率，确保复用 `SharedNode` | 增加接口层可能带来委托分配；若未来泛型节点并入主实现，需谨慎避免重复封装导致栈深增长 |
| 叶片拆分策略 | 扩展 `StringLeafOperations` 引入 `LeafSplitStrategy`/`FindSplitResult`（含 UTF-8 与 UTF-16 偏移），由 `TreeBuilder` 与 `Node` 编辑路径选择策略（bulk/merge/diagnostic）以掩蔽 Rust 偏移差异 | 依赖 `leaf_split_parity_samples.json` 与新增 bulk/merge 覆盖测试记录拆分结果；可在调试模式下记录策略选择频次 | 策略枚举若不断扩张需维护一致性；额外分支可能影响热路径性能，需结合基准确认影响可控 |
| `TreeBuilder::push_slice` / `Node::subseq` | 引入 `TreeSlice` 视图结构，延迟物化 subseq 结果并允许使用 `ArrayPool<char>` 聚合片段；`TreeBuilder` 针对 slice 操作产出 `SlicePlan`（事件流）供诊断与 parity 使用 | 扩展 `TreeBuilder` 单元测试验证 `TreeSlice` 在编辑后的有效性；结合现有 slice trace fixture 检查 `SlicePlan` 输出 | `TreeSlice` 持有引用可能延长节点生命周期导致内存占用上涨；池化缓冲需要严格释放策略，避免泄露或多线程争用 |

## 接口与类型系统翻译范式

| Rust 概念 | 常用代码形态 | C# 对应形态 | 说明 |
|-----------|---------------|--------------|------|
| `struct`（不可变字段） | `struct Node { ... }` | `sealed class` / `record` | C# 侧倾向使用不可变 `sealed class` + 私有 body；必要时提供工厂方法。 |
| `struct`（可变字段） | `struct Builder { ... mut ... }` | `class` + 私有字段 | 将内部可变状态封装在类中，公开只读视图。 |
| `enum` | `enum Metric { Base, Lines, Utf16 }` | `enum` 或 `sealed hierarchy` | 简单枚举直接映射，带 payload 的枚举通过 `abstract record` 或策略接口实现。 |
| `trait` | `trait Metric { ... }` | `interface` | 接口方法默认 `NotImplementedException`，待实现阶段补齐。 |
| 泛型 + trait bound | `impl<T: Metric>` | C# 泛型 + 约束接口 | 若涉及生命周期，可通过只读接口或 Span/ReadOnlySpan 表达。 |
| `Option<T>` | `Option<Node>` | `Node?`（可空引用）或 `struct?` | 注意区分值类型与引用类型；需要显式空检查。 |
| `Result<T, E>` | `Result<Node, Error>` | `bool` + out 参数 / `OneOf` / 例外 | 将错误路径翻译为异常或 `Try` 前缀方法。 |
| `Rc<T>` + `RefCell<T>` | 共享可变节点 | 不可变节点 + 写时复制 helper | C# 采用不可变对象，编辑时创建新节点。 |
| 切片/借用 | `&str`, `&[u8]` | `ReadOnlySpan<char>`/`Span<byte>` | `LeafSplitter` 等类使用 `Span`，防止额外分配。 |
| 模块路径 | `crate::tree::Node` | `Xi.Core.Rope.Tree.Node` | 命名空间按模块层级划分（Tree 模块统一放入 `Xi.Core.Rope.Tree`）。 |

### 类型命名映射范式

为减少“翻译”成本，优先保持 Rust 与 C# 类型在语义上的同名对应，仅按照语言风格调整大小写或命名空间。遇到新类型时，按照以下步骤执行，并同步评估 Rust 端是否需要改名或补 alias：

1. **识别来源上下文**：记录 Rust 模块路径与原始类型名，例如 `tree::Node`, `rope::RopeInfo`。
2. **选择命名空间**：将顶层模块映射为 `Xi.Core.Rope` 子命名空间；子模块可通过子文件夹或局部命名空间体现，如 `Xi.Core.Rope.Tree`。
3. **保持核心名**：除非存在语义差异或泛型化计划，保持核心名不变，仅转换为 C# PascalCase。例如 `rope::base_metric` → `BaseMetric`。
4. **为特化类型加前缀/后缀**：当 Rust 类型在 C# 中转化为领域特化版本（如 Rust 泛型 `Node<N>` 被具体化为 `String` 叶片实现）时，可在名称中加入领域限定词，例如 `Node`，并在映射表中标注该差异，后续若回归泛型可统一回原名。
5. **别名与 type alias**：Rust 的 `type Rope = Node<RopeInfo>` 建议直接映射为同名顶层公开类型 `Rope`。若 C# 需要额外封装（例如实现接口），优先使用同名 `partial` 或包装类，避免新增后缀；若 Rust 出现 alias 改名，需在此文档中同步。
6. **测试/辅助类型**：`test_helpers.rs` 等测试支撑类型保持 `*TestHelper`、`*Assertions` 等约定，便于搜索与复用。

以下表格提供常见类别的命名参考：

| Rust 类型类别 | 原始示例 | 推荐 C# 名称 | 说明 |
|----------------|-----------|--------------|------|
| 顶层结构体 | `struct RopeInfo` | `RopeInfo` | 保留同名，位于 `Xi.Core.Rope` 命名空间。 |
| 顶层别名 | `type Rope = Node<RopeInfo>` | `Rope` | 建议公开类/record 与 `ITextBuffer` 等接口实现相结合。 |
| 泛型节点 | `struct Node<N>` | `Node` | 若未来回归泛型，保持同名并在命名空间区分具体 Info/叶片实现。 |
| Builder/编辑器 | `TreeBuilder`, `RopeBuilder` | `TreeBuilder`, `RopeBuilder` | 直接同名，必要时加命名空间区隔。 |
| Metric 类型 | `BaseMetric`, `LinesMetric`, `Utf16Metric` | `BaseMetric`, `LinesMetric`, `Utf16Metric` | 同名 + 单例模式。 |
| 枚举/状态机 | `enum CursorMode` | `CursorMode` | 使用 PascalCase 枚举，成员名同 Rust variant。 |
| 辅助模块类型 | `LeafSplit`, `Interval`, `Delta` | `LeafSplitter`, `Interval`, `Delta` | 若 Rust 使用动词/函数，可转化为 `*er` 后缀的类，强调职责。 |

> **命名冲突决策**：若 C# 名称已在 BCL 或项目中占用，优先添加领域限定词（例如 `RopeInterval`），同时在映射表备注列记录原始名称，防止日后回归时产生歧义。

### Delta Helper 对齐记录

- `Delta::base_len()` ↔ `Delta<TInfo, TLeaf>.BaseLength`
- `Delta::iter_elements()` ↔ `Delta<TInfo, TLeaf>.EnumerateElements()`
- `Delta::element_triples()` ↔ `Delta<TInfo, TLeaf>.EnumerateElementTriples()`
- Rust serde DTO `els` / `base_len` ↔ C# `DeltaJson` 输出的 `els` 数组与 `base_len` 属性（使用 `System.Text.Json`）。

### Engine Helper 对齐记录

- `Engine::revision_log()` ↔ `Engine.RevisionLog()`（返回 `Revision` 只读视图，对应 Rust `RevisionRef` 迭代器）。
- `Engine::text_snapshot()` ↔ `Engine.TextSnapshot()`。
- `Engine::tombstones_snapshot()` ↔ `Engine.TombstonesSnapshot()`。
- `Engine::deletes_from_union_snapshot()` ↔ `Engine.DeletesFromUnionSnapshot()`。
- `Engine::undone_groups_snapshot()` ↔ `Engine.UndoneGroupsSnapshot()`（返回不可变组列表）。
- `Engine::from_serialized_state()` ↔ `Engine.FromSerializedState(...)`（执行防御性拷贝并复用 `Subset` 镜像）。

## 规划动作
1. **补齐空壳**：按照映射表对 `delta.rs`、`interval.rs` 等模块创建对应 C# 文件，声明类型但暂不实现逻辑，并在 Rust 端预留迁移友好 helper。
2. **同步测试骨架**：在 `xi.Core.Tests` 下新增与 Rust 测试同名的测试类/方法，标记 `Skip` 或 `TODO`；Rust 端将关键测试转换为共享 fixture。
3. **持续更新表格**：每完成一次接口/实现迭代，更新本表状态列与备注列，同时同步 Rust 改造状态。
4. **范式扩展**：遇到新的语言差异（如迭代器、闭包、宏）时，将翻译策略追加到范式表中，并评估是否需在 Rust 端提供替代写法。
5. **特性同步**：记录 Rust 工作区新增的 `serde` 特性层级（`xi-core` → `xi-core-lib/serde` → `xi-rope/serde`），确保 C# 侧在需要访问 JSON helper 时显式启用对应开关并同步文档。
6. **夹具维护**：按照 `docs/architecture/rope-serialization-fixture-playbook.md` 执行 Stage D 刷新流程，在完成复制与验证后回填本表与相关文档的状态备注。

## 主要缺口

- **游标缓存与生命周期策略更新**：Rust `CursorDescriptor` 已发布并作为移植基线，C# `Tree/NodeCursor.cs` 需基于此实现拥有型描述符；`cursor_state` feature gate 提供的 `CursorState` 属可选增强，可在评估成本后决定是否在 C# 侧提供等效持久化缓存。
- **Rope 块/行/字素迭代器尚无 C# 映射**：`ChunkIter`、`LinesRaw`、`Lines` 以及相关 `Rope::lines*` API 在 C# 中缺位，导致高层遍历、Diff/查找等功能无法接线。
- **Grapheme 降级策略的监控**：降级实现已确认为短期方案，需持续收集跨叶补片与 code point 回退频次，供未来是否追平 Rust 版本决策参考。
- **辅助模块仍为空白**：`breaks.rs`、`compare.rs`、`diff.rs`、`find.rs` 等仍在规划阶段，无法支撑视图层和插件所需的断点、差异和搜索能力。
- **Cursor Parity 资产出入口缺失**：`export-serde-fixtures` 尚未提供 `--cursor-descriptors` 子命令，`tests/xi.Core.Tests/Fixtures/CursorDescriptors/` 目录为空，无法支撑文档 §3.2.4 所要求的 10 份 JSON fixture。Rust Porter 需在 Stage D 资产表登记该 CLI，Architecture Mapper 在本表追踪其落地状态。

## 改进思路

1. **补齐 Rope 迭代器与游标骨架**：按照 Rust 定义扩展 `NodeCursor` 字段与内部辅助方法，同时在 `Rope/Iterators` 新增 `ChunkIterator`, `LineIterator` 等类型，确保接口签名与 Rust 对齐。
2. **监控 Grapheme 降级命中情况**：保留当前降级实现并通过 telemetry/测试样本记录触发频率，待数据表明需追平时再评估 ICU4N 或 Rust trace 方案。
3. **拉通 Metric 与 Node 泛型桥接**：在 `Node.Generic.cs` 补充静态抽象成员使用范式，明确 `convert_metrics`、`count` 等方法如何复用 `IMetric`，同时更新测试覆盖。
4. **规划 Breaks/Diff/Search 子系统落点**：为 `Tree/Breaks.cs`, `Diff/`, `Search/` 目录生成最小骨架和 TODO，结合 Stage D 夹具制定迭代顺序。

### Metric Helper 对齐记录

`docs/rust-refactor/breaks-metrics-templating.md` 定义的模板现为 Rust/C# 度量 helper 的唯一真源，新 helper 需先在模板中登记，再落地到 `StringLeafOperations` 与 `BreaksMetricHelper` 复用点。

- Rust `rope::metrics::codepoint::{is_codepoint_boundary, prev_codepoint_boundary, next_codepoint_boundary}` ↔ C# `Utf16BoundaryHelper.{IsBoundary, GetPreviousBoundary, GetNextBoundary}`；所有 boundary 逻辑通过模板生成的 `StringLeafOperations` 入口复用。
- Rust `rope::metrics::lines::{count_newlines_bytes, find_next_newline, find_prev_newline}` ↔ C# `LinesMetric` 内部的 `CountNewlines`, `GetNextBoundary`, `GetPreviousBoundary`，共用 `StringLeafOperations` 的逐字节遍历实现。
- Rust `rope::metrics::break_indices::{nth_break_offset, count_breaks_up_to, find_prev_break, find_next_break, is_break_boundary}` ↔ C# `BreaksMetricHelper`（`src/xi.Core/Rope/BreaksMetricHelper.cs`），模板提供共享的段落断点扫描；`BreaksMetric`/`BreaksBaseMetric` 将直接消费该 helper。
- Rust `rope::metrics::identity::BaseUnitsIdentity` ↔ C# 待引入的 `BaseMetricIdentity` 包装器，确保 base 单位 metric 不重复实现。
- 模板新增或重命名 helper 时，需同步更新本表并运行 Stage D 夹具刷新（`scripts/refresh_serialization_fixtures.ps1`），确保度量输出与黄金资产保持一致。

### Delta Helper 对齐记录

- `Delta::base_len()` ↔ `Delta<TInfo, TLeaf>.BaseLength`
- `Delta::iter_elements()` ↔ `Delta<TInfo, TLeaf>.EnumerateElements()`
- `Delta::element_triples()` ↔ `Delta<TInfo, TLeaf>.EnumerateElementTriples()`
- Rust serde DTO `els` / `base_len` ↔ C# `DeltaJson` 输出的 `els` 数组与 `base_len` 属性（使用 `System.Text.Json`）。

### Engine Helper 对齐记录

- `Engine::revision_log()` ↔ `Engine.RevisionLog()`（返回 `Revision` 只读视图，对应 Rust `RevisionRef` 迭代器）。
- `Engine::text_snapshot()` ↔ `Engine.TextSnapshot()`。
- `Engine::tombstones_snapshot()` ↔ `Engine.TombstonesSnapshot()`。
- `Engine::deletes_from_union_snapshot()` ↔ `Engine.DeletesFromUnionSnapshot()`。
- `Engine::undone_groups_snapshot()` ↔ `Engine.UndoneGroupsSnapshot()`（返回不可变组列表）。
- `Engine::from_serialized_state()` ↔ `Engine.FromSerializedState(...)`（执行防御性拷贝并复用 `Subset` 镜像）。
