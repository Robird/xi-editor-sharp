# Rope 文件级映射与类型翻译计划

## 目标
建立 `reference/rust/rope` 与 `src/xi.Core/Rope` 之间的一一映射，记录每个模块的移植状态，并沉淀 Rust → C# 在接口与类型层面的翻译范式，支撑“先契约后实现 + Rust 侧迁移友好化”的移植流程。

## 状态标签
- `未开始`：尚未在 C# 侧创建对应文件或类型。
- `仅骨架`：已建立目录/类型壳子但缺少真实逻辑。
- `实现中`：核心逻辑正在移植或调试，测试逐步补齐。
- `Rust 重构中`：C# 侧暂缓，等待 `xi-editor-ph7` 提供迁移友好 helper 或结构调整。
- `已实现`：关键能力与诊断均已移植，后续仅保留优化或性能工作。

## 路径映射约定
- `reference/rust/rope/` ↔ `src/xi.Core/Rope/`
- `reference/rust/rope/tree.rs` ↔ `src/xi.Core/Rope/Tree/` （当前包含 `Node.cs`、`TreeBuilder.cs`、`LeafSplitter.cs`，后续 Tree 相关类型统一进入该子目录与命名空间 `Xi.Core.Rope.Tree`）
- `reference/rust/rope/rope.rs` ↔ `src/xi.Core/Rope/` 根目录（`Rope.cs`、`RopeInfo.cs`、`Metrics.cs`、`IMetric.cs` 等）
- 其余 Rust 模块按子目录映射：例如 `delta.rs` 计划对齐至 `src/xi.Core/Rope/Delta/`，`interval.rs` 对齐至 `src/xi.Core/Rope/Intervals/`

## 文件级映射表
| Rust 模块 | 关键类型/职责 | C# 目标文件/目录 | 当前状态 | 备注 |
|-----------|---------------|-------------------|----------|------|
| `tree.rs` | `Node`, `TreeBuilder`, 节点借用/合并、再平衡、结构共享 | `Tree/Node.cs`, `Tree/TreeBuilder.cs`, `Tree/LeafSplitter.cs` | 实现中 | 叶片借用/合并与欠载修复已实现；Rust/C# `SharedNode` 封装已对齐，仅经 `EnsureUnique/CloneWithChildren/ReplaceChildRange` 触碰 COW；内部节点再平衡、聚合刷新与 SharedNode 诊断待补齐。 |
| `tree.rs`（后续类型） | `Cursor`, `BalanceIter`, 内部辅助结构 | `Tree/`（待补充） | Rust 重构中 | 等待 Rust 将生命周期改写为索引/Arc 模式后再引入 C# 骨架。 |
| `rope.rs` | `Rope`, `RopeInfo`, Metric 适配、Buffer API | `Rope.cs`, `RopeInfo.cs`, `Metrics.cs`, `IMetric.cs` | 实现中 | 缺少多 Metric 组合测试与聚合增量刷新；需补充 `Cursor`/`Metric` 交互。 |
| `delta.rs` | `Delta`, `Subset`, `Transformer` 协作算法 | `Rope/Delta.cs`, `Rope/DeltaJson.cs`（C# `Delta` / JSON helper）；`Subset`/`Transformer` 仍规划中 | 实现中 | Stage B 已完成 `Delta<TInfo, TLeaf>`/`DeltaJson` 与回归测试，`factor()` 留作后续；`Transformer` 等高级 helper 待 Stage C/D 引入。 |
| `interval.rs` | 区间集合、`IntervalTree` | 规划为 `Intervals/IntervalSet.cs`, `Intervals/IntervalTree.cs` | 未开始 | 与 Delta/Subset 共用，需预留 Span/Memory 友好实现。 |
| `multiset.rs` | `Subset`/`SubsetBuilder` 多重子集 helper | `Rope/Subset.cs`, `Rope/SubsetJson.cs` | 已实现 | Stage A 引入 `SegmentTriples`/`FromSegmentTriples`/`SegmentCount` 映射与 JSON 回归测试；`SubsetJson` 对齐 Rust serde 输出。 |
| `engine.rs` | 编辑命令应用、Undo/Redo 入口 | 规划为 `Engine/Engine.cs` | 未开始 | 依赖 Rope 与 Delta 实现完成后启动；Rust 正拆除宏以便移植。 |
| `diff.rs` | 文本 diff 逻辑 | 规划为 `Diff/DiffEngine.cs` | 未开始 | 评估复用现有 diff 库或移植 Rust 算法。 |
| `compare.rs` | Rope 比较工具 | 规划为 `Diff/Compare.cs` | 未开始 | 与 `diff.rs` 共享目录，落地后补测试。 |
| `breaks.rs` | 换行符/段落切分逻辑 | 规划为 `Tree/Breaks.cs` | 未开始 | 与 `LeafSplitter` 结合，提供界面供 Rope/Delta 使用。 |
| `find.rs` | Rope 搜索功能 | 规划为 `Search/Find.cs` | 未开始 | 依赖 Metric, Interval；需设计 Span 友好 API。 |
| `spans.rs` | 高亮范围管理 | 规划为 `Search/Spans.cs` | 未开始 | 与 `find.rs` 同目录，提供 Span/Style 聚合。 |
| `serde_impls.rs` | 序列化支持 | 规划为 `Serialization/RopeJsonConverters.cs` | 未开始 | 依据 JSON-RPC 宿主方案决定实现。 |
| `lib.rs` | 模块导出、测试入口 | Solution 顶层 | 已实现 | 通过 `Xi.Editor.sln` 管理，对应 C# 项目已经建立。 |
| `test_helpers.rs` | Rope 测试工具 | `tests/xi.Core.Tests/RopeTestHelpers.cs` | 已实现 | 已封装不变量断言与调试 API；Rust 侧需输出同步 fixture。 |

> 注：表格只列出首批重点模块，可在实际推进中扩充行或拆分更细粒度的子文件（例如 `tree/node.rs`、`tree/edit.rs` 等）。

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

## 规划动作
1. **补齐空壳**：按照映射表对 `delta.rs`、`interval.rs` 等模块创建对应 C# 文件，声明类型但暂不实现逻辑，并在 Rust 端预留迁移友好 helper。
2. **同步测试骨架**：在 `xi.Core.Tests` 下新增与 Rust 测试同名的测试类/方法，标记 `Skip` 或 `TODO`；Rust 端将关键测试转换为共享 fixture。
3. **持续更新表格**：每完成一次接口/实现迭代，更新本表状态列与备注列，同时同步 Rust 改造状态。
4. **范式扩展**：遇到新的语言差异（如迭代器、闭包、宏）时，将翻译策略追加到范式表中，并评估是否需在 Rust 端提供替代写法。
5. **特性同步**：记录 Rust 工作区新增的 `serde` 特性层级（`xi-core` → `xi-core-lib/serde` → `xi-rope/serde`），确保 C# 侧在需要访问 JSON helper 时显式启用对应开关并同步文档。

## 主要缺口
先对照 rope.md 走了一圈，整体感觉这份 C# skeleton 已经把“Rope + Tree + Metric”主干都列出来了，但和 Rust 原版相比仍有几块明显缺口，需要补上才能支撑后续的类型对齐和实现规划。

- **Delta/Subset/Transformer 整块缺席**  
  Rust `delta.rs`、`multiset.rs` 的类型（`Delta<T>`, `InsertDelta`, `Subset`, `SubsetBuilder`, `Transformer` 等）在 C# skeleton 中完全没有。`core-lib` 与 `engine.rs` 强依赖这套接口，缺了它们我们很难往上游推进；当前等待 Rust 拆分宏与 helper 后再同步实现。

- **辅助模块（Breaks/Compare/Diff/Find）未体现**  
  Rust 的 `breaks.rs`、`compare.rs`、`diff.rs`、`find.rs` 在 C# 侧没有任何骨架，意味着行号、diff、搜索等能力还没映射，后续要补的类型和接口会不少。

- **Tree 模块泛型差异未记录**  
  Rust 的 `Node<N>`、`TreeBuilder<N>` 是泛型化设计；我们 C# 目前直接特化成 `Node`（string 叶片）。Skeleton 里最好显式备注“暂时特化 string”或“未来计划恢复泛型”，免得在设计阶段忽略这一差异；Rust 端正在将关联类型替换为显式泛型，完成后需同步更新。

- **Cursor 细节空白**  
  Rust `Cursor<'a, N>` 包含位置缓存、固定大小数组等优化。C# 的 `NodeCursor` 目前只是方法签名。Skeleton 可以添加字段/注释（例如 cache、当前 leaf 引用、偏移量等），否则后续实现时还要回头从 Rust 文档再找一次。

- **RopeInfo 与 Metric 贯穿全局的契约说明不足**  
  Rust 中 `NodeInfo::interval` 等默认方法在 C# 已抽象出来，但 skeleton 里没有标明这些契约如何被 `Node`、`TreeBuilder` 使用，也没有点出“IntervalForPrefix 仍未覆盖非默认行为”。

- **命名空间层级和模块边界未凸显**  
  Rust 按模块分层。Skeleton 里所有类型几乎都堆在一个文件里，很难看出“这是 Tree”“这是 Delta 模块”。对照时会增加认知负担。

## 改进思路

1. **补齐缺失模块骨架**  
  - 按照 `rope-port-mapping.md` 建的映射表，把 `Delta`, `Subset`, `Transformer`, `CountMatcher`, `BreakBuilder`, `LineHashDiff`, `FindResult` 等关键类型都先放进 skeleton，并写上职责摘要。
  - 即便暂时未实现，也能在文档中清楚列出 TODO/待对齐信息，方便设定下一阶段的编码任务；若 Rust 端已有 helper 改造，需同步方法签名。

2. **强化 Tree 泛型/特化的设计说明**  
   - 在 skeleton 的 `Node`/`TreeBuilder` 注释里写明“C# 当前特化 string 叶片，后续评估泛型化方案”，必要时给出拓展接口（例如 `ILeafOperations`）的使用方式。
   - 如果有计划引入泛型版本，可在 skeleton 里预先定义 `Node<TInfo>`、`TreeBuilder<TInfo>` 的草稿或备注，让设计取向更明确。

3. **补全 Cursor 内部状态骨架**  
   - 给 `NodeCursor` 增加与 Rust 一致的字段：`root`, `position`, `cache`（例如固定大小数组）、`leaf`, `offsetOfLeaf`。并在注释里标出缓存策略/无分配前提。
   - 将 Rust 中的核心方法（`descend`, `measure_leaf`, `descend_metric`, `next_leaf`, `prev_leaf` 等）以 stub 形式加进去，避免遗漏。

4. **模块化视图与文档互通**  
  - 在 skeleton 文件里按模块加标题或分段注释（例如 `// ==== Delta ====`, `// ==== Engine ==== `），对应 Rust 中的 `mod`。这样和 rope.md 对照时更直观。
  - 新增关联文档链接注释，例如在 `Delta` 段落写“// 参考 docs/architecture/rope-delta-notes.md”，并在 Rust 端注释中标明对齐目标，方便双端定位。

5. **记录跨文件依赖**  
  - 例如在 `Rope`、`Engine` 骨架位置注明它们依赖的模块（Delta/Subset/Tree），以及任何计划使用的辅助结构（Breaks、Compare、Diff）。帮助在规划实现顺序时横向串联。
  - 若 Rust 端在 helper 拆分后新增模块/函数，也要在此处标注，以免遗漏迁移。

  ### Metric Helper 对齐记录

  - Rust `rope::metrics::codepoint::{is_codepoint_boundary, prev_codepoint_boundary, next_codepoint_boundary}` ↔ C# `Utf16BoundaryHelper.{IsBoundary, GetPreviousBoundary, GetNextBoundary}`。命名风格遵循各语言惯例（snake_case vs. PascalCase），但语义保持一致；新增 helper 后，任何 API 变更需要同步更新两侧命名对照。
  - Rust `rope::metrics::lines::{count_newlines_bytes, find_next_newline, find_prev_newline}` ↔ C# `LinesMetric` 内部的 `CountNewlines`, `GetNextBoundary`, `GetPreviousBoundary` 逻辑。Rust 侧仍提供 `pub fn count_newlines(&str)` 作为 shim 以兼容其他 crate。
  - Rust `rope::metrics::break_indices::{nth_break_offset, count_breaks_up_to, find_prev_break, find_next_break, is_break_boundary}` ↔ C# `BreaksMetricHelper`（`src/xi.Core/Rope/BreaksMetricHelper.cs`）。Helper 已实现并复刻零分配查找语义；`BreaksMetric`/`BreaksBaseMetric` 后续对齐。
  - Rust `rope::metrics::identity::BaseUnitsIdentity` ↔ C# `BaseMetricIdentity`（TODO：待引入），用于生成“与 base 单位一致”的 metric 包装，减少重复实现。
  - 以上 helper 列表纳入 review checklist：每当 Rust/C# 端新增或改名 helper，必须更新此对照表，并执行 `scripts/refresh_skeleton_docs.py` 刷新骨架文档。
