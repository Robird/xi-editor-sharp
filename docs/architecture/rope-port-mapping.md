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
- SharedNode helper 重构完成，Rust `Arc::make_mut` 现由 `docs/rust-refactor/shared-node-api.md` 规范的包装层统一暴露，C# 同步采用。
- Metric 模板化计划依 `docs/rust-refactor/breaks-metrics-templating.md` 落地，`StringLeafOperations` 与 `BreaksMetricHelper` 已承载共享字符串度量逻辑。
- Delta/Subset 序列化经 `docs/rust-refactor/delta-subset-serialization.md` 清理，Stage A-C 镜像与 Stage D 黄金夹具流程现已打通。
- Rust 与 C# skeleton bird’s-eye 文档同步刷新，结构与模块边界标注对齐当前实现。
- Rust 侧字符串叶片 helper 已拆分至 `rope/src/helpers/string_leaf.rs`，统一暴露 `MIN_LEAF`/`MAX_LEAF`/`NEWLINE_WINDOW` 与拆分策略；C# `StringLeafOperations` 继续作为对应实现，跨语言对拍需注意偏移单位差异。

## 路径映射约定
- `reference/rust/rope/` ↔ `src/xi.Core/Rope/`
- `reference/rust/rope/tree.rs` ↔ `src/xi.Core/Rope/Tree/` （当前包含 `Node.cs`、`TreeBuilder.cs`、`LeafSplitter.cs`，后续 Tree 相关类型统一进入该子目录与命名空间 `Xi.Core.Rope.Tree`）
- `reference/rust/rope/rope.rs` ↔ `src/xi.Core/Rope/` 根目录（`Rope.cs`、`RopeInfo.cs`、`Metrics.cs`、`IMetric.cs` 等）
- 其余 Rust 模块按子目录映射：例如 `delta.rs` 计划对齐至 `src/xi.Core/Rope/Delta/`，`interval.rs` 对齐至 `src/xi.Core/Rope/Intervals/`

## 文件级映射表
| Rust 模块 | 关键类型/职责 | C# 目标文件/目录 | 当前状态 | 备注 |
|-----------|---------------|-------------------|----------|------|
| `tree.rs` | `Node`, `SharedNode`, `TreeBuilder`，负责节点借用/合并、再平衡骨架 | `Tree/Node.cs`, `Tree/Node.Generic.cs`, `Tree/TreeBuilder.cs`, `Tree/LeafSplitter.cs` | 实现中 | 写时复制 helper 已对齐；内部再平衡与诊断计数器尚未接入，`Node.Generic.cs` 仍待并入主实现。 |
| `tree.rs`（游标相关） | `Cursor`, `CursorIter`, `BalanceIter` 等遍历结构 | `Tree/NodeCursor.cs`（骨架） | 仅骨架 | Rust 端 API 已稳定，可在 C# 侧补齐缓存字段与 Metric 钩子；需要为 lifetime → 索引的映射设计落地方案。 |
| `rope.rs` | `Rope`, `RopeInfo`, Metric 适配、文本 API | `Rope.cs`, `RopeInfo.cs`, `Metrics.cs`, `IMetric.cs` | 实现中 | `RopeInfo`/`Metrics` 已实现；`Rope` 仍是最小占位，缺少 chunk/line 迭代与 grapheme 接口。 |
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
| `NodeInfo<L>`, `DefaultMetricProvider<L>`, `Leaf` | 约束节点聚合信息与叶片接口 | `Tree/TreeContracts.cs`, `Tree/StringLeafOperations.cs` 已提供 `ILeafOperations<string>` 与静态 helper | 继续补充文档注释阐明默认实现语义，确保后续泛型化时无需重命名 |
| `SharedNode`, `Node`, `NodeBody`, `NodeVal` | 树节点写时复制核心 | `Tree/Node.cs`, `Tree/Node.Generic.cs` 完成封装 | 结合 `Node.Generic` 为主实现补足共享入口，并预留诊断计数器挂载点 |
| `TreeBuilder<N, L>` | 批量构建/再平衡入口 | `Tree/TreeBuilder.cs` 现已映射 | 核对 `push_slice` 与 Rust 行为，补充多 Metric 插入测试 |
| `Metric<N, L>` | 度量与边界查询 | `IMetric.cs`, `Metrics.cs` | 将 `LinesMetric`/`Utf16Metric` 的 helper 调用对齐 Rust `helpers/string_leaf.rs` 常量命名 |
| `RopeInfo` 及 `Leaf for String` 实现 | 基础聚合字段与字符串叶片实现 | `RopeInfo.cs`, `StringLeafOperations.cs` | 将 Rust `count_utf16_code_units`、`find_leaf_split_for_*` 的新 helper 签名同步进 C# 记录 |
| `Node::count`, `Node::count_base_units`, `convert_metrics` | Metric 间转换 | `Node.cs` 中已有对应占位 | 需要补上调用 `IMetric` 的桥梁方法并新增单元测试 |
| `ChunkIter`, `LinesRaw`, `Lines`、`iter_chunks`/`lines` API | 文本块与行遍历 | C# 尚未创建对应类型 | 现可根据 Rust 定义生成骨架（建议放入 `Rope/Iterators/`），接口返回 `ReadOnlyMemory<char>` 以规避多余分配 |
| `Cursor`, `CursorIter` | Metric 驱动的遍历游标 | `Tree/NodeCursor.cs` 已存在骨架 | 依据 Rust 缓存结构增加字段：固定大小父链缓存、当前叶引用与偏移 |

### 暂需额外设计或信息的符号

| Rust 符号/片段 | 当前阻碍 | 影响 | 计划 |
|----------------|-----------|------|------|
| `Cursor<'a, N, L>` 的缓存模型 | Rust 依赖生命周期与 `Option<&'a L>` 持久化叶引用；C# 需以索引或共享节点替代 | 游标迭代、查找与 `find.rs` 依赖 | 在 `docs/rust-refactor/cursor-lifetime-refactor.md` 基础上细化索引化方案，随后扩充 `NodeCursor` 字段与构造逻辑 |
| `Rope::next_grapheme_offset`、`prev_grapheme_offset` 等 | Rust 借助 `unicode_segmentation::GraphemeCursor`，.NET 标准库缺乏等价实现 | 影响多语言光标、选择扩展、插件同步 | 评估引入 ICU（`System.Globalization.StringInfo` / `ICU4N`）或嵌入 Rust 预处理表，决定 C# 骨架返回类型及依赖 |
| `Rope::iter_chunks` 返回的 `Cow<str>` | Rust 通过借用避免分配；C# 需在 `string`、`ReadOnlyMemory<char>`、`ReadOnlySpan<char>` 之间取舍 | Buffer diff、序列化与插件接口的遍历性能 | 制定跨语言块枚举协议，可能以 `ReadOnlyMemory<char>` + 池化 string 替代，骨架中需先定义抽象返回类型 |
| `Tree::convert_metrics` 与 `Node::edit` 中的 `Into<Node>` | Rust 泛型允许零拷贝地在不同 Metric 间转换；C# 需显式限定泛型与 `ILeafOperations` | 影响 Delta/Subset 与 Rope API 的泛型一致性 | 在 `Node.Generic.cs` 引入受约束的静态抽象成员，并对 `Node` 特化实现重定向 |
| `helpers/string_leaf.rs::find_leaf_split_for_bulk`/`for_merge` | Rust 基于 UTF-8 窗口；C# 当前仅暴露 UTF-16 版本 | 当叶片超限或 bulk 构建时会出现拆分偏差 | 扩充 `StringLeafOperations`，对拍 `leaf_split_parity_samples.json` 以验证拆分窗口 |
| `TreeBuilder::push_slice` & `Node::subseq` | Rust 使用 `Interval` 和栈化拆分策略，涉及临时 `Vec<Node>` | 影响 Rope 编辑与 Delta 应用的性能 | 记录临时节点池策略，考虑在 C# 中使用 `ArrayPool<Node>` 以降低分配 |

> 注：表格只列出首批重点模块，可在实际推进中扩充行或拆分更细粒度的子文件（例如 `tree/node.rs`、`tree/edit.rs` 等）。

> 偏移单位提示：Rust `helpers/string_leaf.rs` 中的拆分函数始终返回 UTF-8 字节偏移；C# `StringLeafOperations` 与测试则以 UTF-16 `char` 计数。跨语言对拍或文档引用拆分位置时，需显式标注单位并避免混用。

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

- **游标缓存与生命周期策略仍待定**：尽管 Rust `Cursor` 接口已经稳定，但其依赖的 `Option<&'a L>`、固定大小缓存数组需要在 C# 中重新建模，目前 `Tree/NodeCursor.cs` 仅包含最小骨架。
- **Rope 块/行/字素迭代器尚无 C# 映射**：`ChunkIter`、`LinesRaw`、`Lines` 以及相关 `Rope::lines*` API 在 C# 中缺位，导致高层遍历、Diff/查找等功能无法接线。
- **Grapheme 与 ICU 依赖策略未决**：Rust 通过 `unicode_segmentation::GraphemeCursor` 完成字素粒度移动，C# 需选择 `StringInfo`/ICU4N 等替代并评估性能差异。
- **辅助模块仍为空白**：`breaks.rs`、`compare.rs`、`diff.rs`、`find.rs` 等仍在规划阶段，无法支撑视图层和插件所需的断点、差异和搜索能力。

## 改进思路

1. **补齐 Rope 迭代器与游标骨架**：按照 Rust 定义扩展 `NodeCursor` 字段与内部辅助方法，同时在 `Rope/Iterators` 新增 `ChunkIterator`, `LineIterator` 等类型，确保接口签名与 Rust 对齐。
2. **确定 Grapheme 处理方案**：对比 `System.Globalization.StringInfo`, `Rune` API 与 ICU4N 实现，撰写设计备忘并在骨架中选定返回类型，避免后续 API 反复改动。
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
