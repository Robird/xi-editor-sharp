# Rope 文件级映射与类型翻译计划

## 目标
建立 `reference/rust/rope` 与 `src/xi.Core/Rope` 之间的一一映射，记录每个模块的移植状态，并沉淀 Rust → C# 在接口与类型层面的翻译范式，支撑“先契约后实现”的移植流程。

## 状态标签
- `未开始`：尚未在 C# 侧创建对应文件或类型。
- `仅骨架`：已建立目录/类型壳子但缺少真实逻辑。
- `实现中`：核心逻辑正在移植或调试，测试逐步补齐。
- `已实现`：关键能力与诊断均已移植，后续仅保留优化或性能工作。

## 路径映射约定
- `reference/rust/rope/` ↔ `src/xi.Core/Rope/`
- `reference/rust/rope/tree.rs` ↔ `src/xi.Core/Rope/Tree/` （当前包含 `Node.cs`、`TreeBuilder.cs`、`LeafSplitter.cs`，后续 Tree 相关类型统一进入该子目录与命名空间 `Xi.Core.Rope.Tree`）
- `reference/rust/rope/rope.rs` ↔ `src/xi.Core/Rope/` 根目录（`Rope.cs`、`RopeInfo.cs`、`Metrics.cs`、`IMetric.cs` 等）
- 其余 Rust 模块按子目录映射：例如 `delta.rs` 计划对齐至 `src/xi.Core/Rope/Delta/`，`interval.rs` 对齐至 `src/xi.Core/Rope/Intervals/`

## 文件级映射表
| Rust 模块 | 关键类型/职责 | C# 目标文件/目录 | 当前状态 | 备注 |
|-----------|---------------|-------------------|----------|------|
| `tree.rs` | `Node`, `TreeBuilder`, 节点借用/合并、再平衡、结构共享 | `Tree/Node.cs`, `Tree/TreeBuilder.cs`, `Tree/LeafSplitter.cs` | 实现中 | 叶片借用/合并与欠载修复已实现；内部节点再平衡、聚合刷新待补齐。 |
| `tree.rs`（后续类型） | `Cursor`, `BalanceIter`, 内部辅助结构 | `Tree/`（待补充） | 未开始 | 迁移阶段 C/D 时引入，对应子类型先列入目录后续补充。 |
| `rope.rs` | `Rope`, `RopeInfo`, Metric 适配、Buffer API | `Rope.cs`, `RopeInfo.cs`, `Metrics.cs`, `IMetric.cs` | 实现中 | 缺少多 Metric 组合测试与聚合增量刷新；需补充 `Cursor`/`Metric` 交互。 |
| `delta.rs` | `Delta`, `Subset`, `Transformer` 协作算法 | 规划为 `Delta/Delta.cs`, `Delta/Subset.cs`, `Delta/Transformer.cs` | 未开始 | 先创建骨架并对齐类型命名，再补实现与测试。 |
| `interval.rs` | 区间集合、`IntervalTree` | 规划为 `Intervals/IntervalSet.cs`, `Intervals/IntervalTree.cs` | 未开始 | 与 Delta/Subset 共用，需预留 Span/Memory 友好实现。 |
| `multiset.rs` | Rope 统计聚合辅助 | 规划为 `Stats/MultiSet.cs` | 未开始 | 可结合 .NET `Dictionary` 或自定义结构。 |
| `engine.rs` | 编辑命令应用、Undo/Redo 入口 | 规划为 `Engine/Engine.cs` | 未开始 | 依赖 Rope 与 Delta 实现完成后启动。 |
| `diff.rs` | 文本 diff 逻辑 | 规划为 `Diff/DiffEngine.cs` | 未开始 | 评估复用现有 diff 库或移植 Rust 算法。 |
| `compare.rs` | Rope 比较工具 | 规划为 `Diff/Compare.cs` | 未开始 | 与 `diff.rs` 共享目录，落地后补测试。 |
| `breaks.rs` | 换行符/段落切分逻辑 | 规划为 `Tree/Breaks.cs` | 未开始 | 与 `LeafSplitter` 结合，提供界面供 Rope/Delta 使用。 |
| `find.rs` | Rope 搜索功能 | 规划为 `Search/Find.cs` | 未开始 | 依赖 Metric, Interval；需设计 Span 友好 API。 |
| `spans.rs` | 高亮范围管理 | 规划为 `Search/Spans.cs` | 未开始 | 与 `find.rs` 同目录，提供 Span/Style 聚合。 |
| `serde_impls.rs` | 序列化支持 | 规划为 `Serialization/RopeJsonConverters.cs` | 未开始 | 依据 JSON-RPC 宿主方案决定实现。 |
| `lib.rs` | 模块导出、测试入口 | Solution 顶层 | 已实现 | 通过 `Xi.Editor.sln` 管理，对应 C# 项目已经建立。 |
| `test_helpers.rs` | Rope 测试工具 | `tests/xi.Core.Tests/RopeTestHelpers.cs` | 已实现 | 已封装不变量断言与调试 API。 |

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

为减少“翻译”成本，优先保持 Rust 与 C# 类型在语义上的同名对应，仅按照语言风格调整大小写或命名空间。遇到新类型时，按照以下步骤执行：

1. **识别来源上下文**：记录 Rust 模块路径与原始类型名，例如 `tree::Node`, `rope::RopeInfo`。
2. **选择命名空间**：将顶层模块映射为 `Xi.Core.Rope` 子命名空间；子模块可通过子文件夹或局部命名空间体现，如 `Xi.Core.Rope.Tree`。
3. **保持核心名**：除非存在语义差异或泛型化计划，保持核心名不变，仅转换为 C# PascalCase。例如 `rope::base_metric` → `BaseMetric`。
4. **为特化类型加前缀/后缀**：当 Rust 类型在 C# 中转化为领域特化版本（如 Rust 泛型 `Node<N>` 被具体化为 `String` 叶片实现）时，可在名称中加入领域限定词，例如 `Node`，并在映射表中标注该差异，后续若回归泛型可统一回原名。
5. **别名与 type alias**：Rust 的 `type Rope = Node<RopeInfo>` 建议直接映射为同名顶层公开类型 `Rope`。若 C# 需要额外封装（例如实现接口），优先使用同名 `partial` 或包装类，避免新增后缀。
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

## 规划动作
1. **补齐空壳**：按照映射表对 `delta.rs`、`interval.rs` 等模块创建对应 C# 文件，声明类型但暂不实现逻辑。
2. **同步测试骨架**：在 `xi.Core.Tests` 下新增与 Rust 测试同名的测试类/方法，标记 `Skip` 或 `TODO`。
3. **持续更新表格**：每完成一次接口/实现迭代，更新本表状态列与备注列。
4. **范式扩展**：遇到新的语言差异（如迭代器、闭包、宏）时，将翻译策略追加到范式表中。
