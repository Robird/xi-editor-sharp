# Node 泛型化重构调查

> 目标：对齐 Rust 版 `Node<N>`，将当前 `Node` 从 `string` 特化改造成基于静态多态的 `Node<TInfo, TLeaf, TLeafOps>`。本文记录实际引用面、需要触达的 C# 文件以及预期的改动风险。

## Progress Snapshot
- [x] Rust 侧 `NodeInfo`、`TreeBuilder`、`Delta` 已全面改造为对叶类型显式泛型，`Rope` 等别名通过特化绑定到字符串实现。
- [ ] C# 侧主线仍使用字符串特化 `Node`；实验性 `Node<TInfo,TLeaf,TLeafOps>` 骨架已在 `Tree/Node.Generic.cs` 落地，但尚未接入 Builder/Delta 等路径。
- [ ] 集成泛型节点、游标重构与共享节点契约规划见短期路线图 1-3 项。
- [x] Rust 字符串叶片 helper 已抽离至 `helpers/string_leaf.rs`，统一暴露 `MIN_LEAF`/`MAX_LEAF`/`NEWLINE_WINDOW` 与拆分函数；C# `StringLeafOperations` 仍维持 UTF-16 `char` 计数，需要文档与测试明确偏移单位差异。

## 1. 原版参考
- Rust 源（见 `docs/skeleton/rope.md` `tree.rs` 部分）：`Node<N>` 通过 `N::L` 访问叶类型；`NodeInfo`/`TreeBuilder`/`Delta` 现已全部以叶类型为泛型参数，在 Rust 端形成自洽链路。
- 迁移规划文档：`docs/architecture/rope-port-mapping.md` 中已将 `tree.rs` → `Tree/Node.cs` 对应关系列出；`docs/architecture/static-polymorphism-assessment.md` 评估了使用 `struct` Helper 的静态多态可行性。

## 2. 使用面盘点
使用 `Node` 的主要文件可归类如下（依据 usage 工具结果）：

| 区域 | 具体文件 | 当前依赖点 | 泛型化时的重点工作 |
|---|---|---|---|
| 核心实现 | `src/xi.Core/Rope/Tree/Node.cs` | 直接持有 `string` 文本、`RopeInfo`、静态常量等 | 引入泛型参数；将所有叶操作改走 `TLeafOps`；把 `Min/MaxLeafSize` 等常量转由 Helper 提供；重新设计 `ToString()`、`LeafSpan` 等字符串特化 API；保证 `NodeBody`、`Split/Concat/Insert/Delete/Replace` 等流程均使用泛型叶片。 |
| 构建器/拆分器 | `TreeBuilder.cs`、`LeafSplitter.cs` | 仅支持 `string` 输入，直接 new `Node.FromLeaf`，依赖 `Node.MaxLeafSize` | 让 Builder/拆分器接受泛型参数或策略；考虑将字符串专用拆分逻辑迁入 `StringLeafOperations`；为泛型 Builder 提供 `PushLeaf(TLeaf)` 等接口。 |
| Rope 表层 | `Rope.cs`、`RopeInfo.cs`、`Metrics.cs`, `IMetric.cs` | Rope 直接持有 `Node`；Metrics/Info 假设叶为 `string` | `Rope` 可通过类型别名绑定泛型 `Node<RopeInfo,string,StringLeafOperations>`；`RopeInfo`/Metrics 保持在该特化上工作（必要时新增泛型版本或静态抽象接口成员）。 |
| 游标/契约 | `NodeCursor.cs`、`TreeContracts.cs` | Cursor 还未实现；契约接口缺少静态抽象成员 | 先完善契约（例如为 `ILeafOperations` 添加静态属性 `MinLeafSize`/`MaxLeafSize` 等）；Cursor 改为基于泛型节点，以便后续实现 metric 相关逻辑。 |
| 架构常量 | `LeafSplitter.cs`、`NodeTests.cs` 等使用 `Node.MinLeafSize/MaxLeafSize` | 常量绑定在具体类型上 | 改为通过泛型 Helper 暴露（`TLeafOps.MinLeafSize`）；测试使用 `RopeNode.MinLeafSize` 等别名。 |
| 测试集 | `tests/xi.Core.Tests/*.cs` | 大量调用 `Node.FromLeaf`、`ToString`、`TraverseLeaves()` | 引入 `using RopeNode = Node<RopeInfo, string, StringLeafOperations>;` 或更新断言，确保测试仍验证字符串特化行为；必要时提供工厂函数。 |

## 3. 关键改动详解

### 3.1 `Tree/Node.cs`
- **泛型签名**：建议定义 `public sealed class Node<TInfo, TLeaf, TLeafOps>`，约束 `TInfo : struct, ITreeNodeInfo<TInfo, TLeaf>` 与 `TLeafOps : struct, ILeafOperations<TLeaf>`。
- **节点数据**：`NodeBody` 应持有 `TLeaf? Leaf` 与 `Node<TInfo, TLeaf, TLeafOps>[]? Children`。
- **常量迁移**：将 `MinLeafSize/MaxLeafSize` 替换为 `TLeafOps.MinLeafSize/MaxLeafSize`（静态抽象属性），或在节点类内缓存 Helper 实例后通过属性访问。
- **叶操作**：`Insert/Delete/Replace/SplitLeafByBounds/MergeLeaves/TryPushMaybeSplit` 等所有 `string` 操作改由 `TLeafOps` 暴露的方法完成；若现有接口不足，需要扩充 `ILeafOperations`（例如增加 `InsertRange`, `RemoveRange`, `Concat`, `Clone`, `EnumerateSegments` 等）。
- **调试输出**：`ToString()`、`FormatLeafPreview` 目前依赖 `string`；可以
  1. 在泛型节点中保留虚/委托形式，仅在 `string` 特化中注入；
  2. 或者提供 `LeafDisplayAdapter` 接口专门用于测试。
- **结构共享方法**：如 `CloneWithChildren`, `ReplaceChildWithSegments`, `BuildFromSegments` 需要泛型化，确保在不重建叶片的前提下操作 `Node<T...>`。
- **兼容层**：为了渐进迁移，可先创建 `internal sealed class RopeNode : Node<RopeInfo, string, StringLeafOperations>` 并保留现有 API，随后逐步替换使用点；当前实验性骨架存于 `Tree/Node.Generic.cs`，尚未与主 `Node` partial 类和 Builder/Delta 管线连通。

> **操作映射速览**
> 为了落地泛型版 `Node`，需要在 `TLeafOps` 中补齐以下能力，以承载当前 `Node.cs` 中的叶操作：
> - **创建/克隆**：`Create(ReadOnlySpan<char>)`、`Clone(TLeaf)`、`Empty` 常量，用于 `FromLeaf`、`EnsureWritableLeaf` 等路径；
> - **切分/重组**：`SplitByBounds(TLeaf)`（返回分段枚举）、`TryMerge(TLeaf lhs, TLeaf rhs)`（合并 + 可选拆分），支撑 `SplitLeafByBounds`、`NormalizeLeafMinimum`、`MergeLeaves`；
> - **片段编辑**：`InsertRange`, `RemoveRange`, `ReplaceRange`，对应 `TryInsertInSingleLeaf`、`TryDeleteInSingleSegment`、`TryReplaceInSingleSegment`；
> - **容量/诊断**：`MinLeafSize`、`MaxLeafSize`、`Length(TLeaf)`、`IsOkChild(TLeaf)`，用于构建和校验；
> - **显示/调试（可选）**：`ToString(TLeaf)` 或通过委托注入字符串预览，保证测试输出稳定。
> 这些 API 可以拆分为两个接口：运行期实例方法聚焦数据操作，静态抽象成员提供常量/工厂，避免在泛型上下文频繁构造 Helper 实例。

### 3.2 `TreeContracts.cs`
- 将 `ILeafOperations<TLeaf>` 拆分为**静态抽象常量/工厂**与**实例方法**两部分：静态成员暴露 `MinLeafSize/MaxLeafSize`、`Create(ReadOnlySpan<char>)` 等工厂；实例方法负责对现有叶片执行插入/删除/拆分。
- 为满足现有 `Node.cs` 全部调用，必须补充 `InsertRange`, `RemoveRange`, `ReplaceRange`, `Clone`, `EnumerateSegments`, `TryMergeWith` 等方法，返回类型尽量使用 `ReadOnlyMemory<T>`/`IReadOnlyList<TLeaf>` 避免装箱。
- `ITreeNodeInfo` 需同步评估：若现有 `Identity`/`FromLeaf`/`Accumulate` 难以覆盖局部重算，可新增 `static` 辅助（例如 `UpdateForLeaf(ref TInfo info, TLeaf leaf)`）或约定 `Accumulate`/`FromLeaf` 的组合使用模式，确保写时复制后无需整棵树回算聚合信息。

### 3.3 `LeafSplitter.cs`
- 当前使用 `Node.MaxLeafSize` 和字符串 API。为了泛型化：
  - 可将 `LeafSplitter` 抽象成策略 `ILeafSplitter<TLeaf, TLeafOps>`；字符串特化保留现有逻辑。
  - 或在 `StringLeafOperations` 内提供 `Split(string text)`，`LeafSplitter` 调用 Helper 静态方法。
- 其他叶类型（例如基于 `ArrayPool<char>`）可能需要重新设计拆分方式，这是泛型化后的主要风险点之一。
- Rust `helpers/string_leaf.rs` 已集中声明 `MIN_LEAF`/`MAX_LEAF`/`NEWLINE_WINDOW` 与 `find_leaf_split_*`，输出仍以 UTF-8 字节偏移计量；C# `StringLeafOperations` 的拆分窗口与返回值继续使用 UTF-16 `char` 单位，跨语言文档与测试需显式注记单位差异。

### 3.4 `TreeBuilder.cs`
- 需要引入泛型参数并存储 `List<Node<TInfo,TLeaf,TLeafOps>>`。
- `PushString/PushSpan` 在泛型场景下未必适用；可以：
  - 保留字符串专用入口仅用于 `StringLeafOperations`；
  - 增加 `PushLeaf(TLeaf leaf)` / `PushNode(Node<T...>)` API，供泛型调用。
- `AppendNode` 中的高度比较、合并逻辑与现有 Node 代码相同，但要保证类型匹配。

### 3.5 `Rope.cs`、`RopeInfo.cs`、`Metrics.cs`
- `Rope` 本身不必泛型化（仍作为字符串缓冲区），但内部字段 `_root` 应改为 `Node<RopeInfo, string, StringLeafOperations>`（可通过 `using RopeNode = ...` 简化）。
- `RopeInfo` 已实现 `ITreeNodeInfo<RopeInfo,string>`，无需大改，但要确保新增的 `ILeafOperations` 接口满足 Node 的调用需求。
- `BaseMetric` 等度量类型若保持字符串特化，应更新其接口签名为 `IMetric<string, RopeInfo>`（或继续使用 `IMetric` 别名，但需确认别名定义随泛型调整）。

### 3.6 `NodeCursor.cs`
- 游标骨架尚未实现，但要预留泛型签名，避免日后重写时再次大规模改动。
- Cursor 应能接受 `Node<TInfo, TLeaf, TLeafOps>`，并在内部通过 `TLeafOps/IMetric` 绕过特定叶实现。

### 3.7 测试代码
- 现有测试大量引用 `Xi.Core.Rope.Tree.Node` 静态方法和常量。重构后需：
  - 提供 `using RopeNode = Node<RopeInfo, string, StringLeafOperations>;`
  - 或新增工厂 `RopeNodeFactory.FromString(...)`，统一包装。
- 需要更新断言中对 `Node.MaxLeafSize` 等常量的引用方式。
- 若 `ToString()` 等方法仅在字符串特化提供，确认测试仍可访问。

## 4. 可能遇到的难点
1. **叶操作接口缺口**：`Insert/Delete/Replace` 目前直接操作字符串，泛型化后需要在 Helper 中实现等价功能，可能导致接口膨胀。建议先梳理 Node 内部调用点，再统一补全接口。
2. **常量/配置注入**：`MinLeafSize/MaxLeafSize` 目前为 `const`，并被 `LeafSplitter`、测试等多处直接引用。需在 Helper 上提供静态抽象成员，并在消费端替换使用；编译器支持（C# 11+）已验证可行（参见 `static-polymorphism-assessment.md`）。
3. **字符串特化的调试/日志**：测试和诊断高度依赖 `ToString()` 输出；若泛型化后 `Node` 不再普适提供字符串化，需要保留字符串特化的 `partial` 类或扩展方法。
4. **额外叶类型的需求**：若未来要支持基于 `char[]`/`ArrayPool<char>` 的实现，Helper 结构体无法通过构造注入资源；需要提前决定是否允许 `Node` 构造时显式传入 Helper 实例。
5. **过渡阶段的二义性**：重构过程中测试与业务同时存在旧、新 Node；建议采用别名或 `partial class` 手段，保证在一次合并中完成 API 切换，避免调用点混淆。

## 5. 短期路线图

**已完成**
- [x] 接口准备：扩展 `ILeafOperations`/`ITreeNodeInfo` 为 static abstract 契约，并以 `StringLeafOperations` 结构体实现；81 项单元测试锁定字符串 Helper 行为。
- [x] 泛型 Node 骨架：`Tree/Node.Generic.cs` 中提供 `Node<TInfo,TLeaf,TLeafOps>` 原型，并通过 `GenericNodeSmokeTests` 验证聚合与遍历路径。

**待办（当前冲刺）**
1. [ ] 集成泛型 `Node<TInfo,TLeaf,TLeafOps>` 至现有 C# 主实现（`TreeBuilder`、`LeafSplitter`、`Rope`、`Delta`），并确保 81 项基线测试在新路径下通过。
2. [ ] 调研并设计基于泛型节点的 Cursor 生命周期/索引方案，澄清父缓存、借用与高度访问信约。
3. [ ] 定义 `SharedNode` 辅助契约（或包装类型），覆盖 clone/borrow/调试视图需求，为跨模块共享节点打好接口基础。

**后续待排期**
- [ ] 泛型化 `TreeBuilder`/`LeafSplitter` 余项将在步骤 1 落地后拆分提交，完善策略注入与特化逻辑。
- [ ] 清理 `Rope`、`Delta`、`Subset`、`Cursor` 等上层 API 的别名与回归测试，并同步更新相关文档（`AGENTS.md`、`rope-port-mapping.md`）。

## 6. 可行性结论
- **可行**：从语言特性和静态多态评估来看，C# 端完全可以实现与 Rust 类似的泛型 `Node`。
- **主要工作量**：集中在叶片操作抽象、常量迁移与 Builder/Splitter 的适配，涉及 `Tree` 目录内四个核心文件以及 `Rope.cs`/测试套件。
- **建议**：在正式动手前，先完成 `StringLeafOperations` 原型和接口扩展，以便明确 Node 内部真正需要的 Helper 能力，降低后续返工。

## 7. 原版 `Node<N>` 使用场景总览
结合 `reference/rust/rope` 中的完整实现，`Node<N>` 的落地场景可按职责分为以下几类，每一类都提示了 C# 泛型化后需要守住的契约与能力：

### 7.1 树内核与通用基础设施（`reference/rust/rope/src/tree.rs`）
- **结构定义**：`Node<N>` 以 `Arc<NodeBody<N>>` 持有叶或子节点，`NodeBody` 在叶/内部节点间切换仍复用同一泛型约束（`N: NodeInfo`）。
- **写时复制路径**：`from_leaf`、`from_nodes`、`merge_nodes`、`merge_leaves`、`concat` 等方法在保持树平衡的同时复用已有子树，需要访问 `N::L`、`Leaf::is_ok_child`、`push_maybe_split` 与聚合信息累加。
- **局部编辑**：`subseq`、`edit`、`convert_metrics`、`count`、`measure` 依赖 `TreeBuilder::push_slice`、`TreeBuilder::push` 构造新节点，隐含要求泛型版 `TreeBuilder` 能按任意间隔拼接 `Node<T>`。
- **遍历能力**：`Cursor<'a, N>`、`CursorCache`、`Cursor::next_leaf/prev_leaf` 等通过 `&Node<N>` + Metric 实现跨叶导航，对叶访问（`get_leaf`）和父节点高度、子节点缓存有直接依赖。
- **调试与诊断**：`fmt::Debug`、`interval()`、`ptr_eq` 等方法暴露了内部状态，泛型版需要保留最少的可视化/比较 API 以供测试与日志使用。

### 7.2 字符串 Rope 特化（`reference/rust/rope/src/rope.rs`）
- **类型别名**：`pub type Rope = Node<RopeInfo>` 将 `String` 作为叶类型，`RopeInfo` 在 `NodeInfo` 中累积 `lines`、`utf16_size`。
- **叶面操作**：`impl Leaf for String` 自定义 `push_maybe_split`、`is_ok_child`，并依赖常量 `MIN_LEAF/MAX_LEAF` 控制叶尺寸。C# 侧需通过 `TLeafOps` 暴露等价逻辑。
- **高级 API**：文本编辑/查询（`edit`、`slice`、`insert`、`remove`）、行列度量（`count::<LinesMetric>`、`measure::<Metric>`）、游标操作（`lines_raw`、`graphemes` 等）全部复用 `Node` 的度量与切片能力。
- **构建路径**：`TreeBuilder::push_str`、`push_leaf`、`build` 等在 `FromStr` / `Add` / `Concat` 中频繁调用，要求泛型版 `TreeBuilder` 支持从原生叶数据构建节点，并维护叶容量策略。
- **别名传递**：`RopeDelta = Delta<RopeInfo>`、`RopeScanner`、`LinesMetric` 等通过 `Rope` 间接约束 `Node`，提示我们必须确保泛型版 `Node` 与指标体系协同工作。

### 7.3 Breaks / Spans 等派生结构
- **Breaks（`reference/rust/rope/src/breaks.rs`）**：`Breaks = Node<BreaksInfo>` 使用 `BreaksLeaf`（持有行断点 Vec）、专属 Metric。`BreakBuilder` 借助 `TreeBuilder`/`Node::from_leaf` 构建树，要求泛型化后叶操作能够处理自定义数据结构而非字符串。
- **Spans（`reference/rust/rope/src/spans.rs`）**：`Spans<T> = Node<SpansInfo<T>>` 提供富文本区间存储，`SpansBuilder`/`SpanIter`/`Spans::transform` 结合 `Cursor`、`Delta` 完成插入、合并与 OT。这里强调两点：
  - Leaf 中可能保存泛型 `T`，因此 `Node` 不应该预设叶片可序列化或可显示。
  - `TreeBuilder`、`Node::from_leaf`、`push` 在不同叶类型下必须仍然可用，且 `Cursor` 需要在泛型 Info/Leaf 上保持 metric 正确性。

### 7.4 增量编辑与变换管线
- **Delta（`reference/rust/rope/src/delta.rs`）**：`DeltaElement::Insert(Node<N>)`、`Delta::apply/simple_edit/synthesize/replace` 全程将 `Node<N>` 作为不可变片段拼接，依赖 `Node::len`、`clone`、`TreeBuilder::push_slice`、`push`。泛型化需保证克隆、切片、插入在任意叶类型上成立。
- **Diff（`reference/rust/rope/src/diff.rs`）**：`Diff<N>` trait 的输入/输出都是 `Node<N>` 与 `Delta<N>`，提示我们未来若需要为 C# 引擎扩展 diff 策略，泛型 `Node` 必须作为公共契约暴露。
- **Subset/Multiset（`reference/rust/rope/src/multiset.rs`）**：`Subset::delete_from` 使用 `TreeBuilder::push_slice` 将选区复制到新 `Node<N>`；对应 C# 版需要让 Subset/Delta 等组合直接消费泛型节点。
- **CRDT/Engine（`reference/rust/rope/src/engine.rs`）**：虽然文件中更多使用 `Rope` 别名，但底层操作（`shuffle`、`compute_deltas` 等）依赖 `Delta` 与 `Subset` 的 `Node<N>` 能力，该链路要求泛型化后保持 API 兼容。

### 7.5 序列化与外部接口（`reference/rust/rope/src/serde_impls.rs`）
- `serde` 为 `DeltaElement::Insert(Node<RopeInfo>)`、`Delta<RopeInfo>` 提供序列化/反序列化，意味着泛型 `Node` 最终仍需能被具体特化序列化（在 C# 中体现为 JSON/消息传输层的转换入口）。
- 同一文件也通过 `Rope::from_str`/`Serialize` 说明 Leaf 特化需要暴露从原生文本构建节点的能力。

### 7.6 小结：对 C# 泛型化的启示
- **能力闭环**：`Node` 必须提供（1）结构共享 + 写时复制、（2）基于 Metric 的切片/遍历、（3）与 `TreeBuilder`、`Cursor` 的配套 API，才能覆盖所有上层模块需求。
- **扩展性**：Breaks/Spans 展示了 Leaf/Info 自定义的可能性，因此泛型接口不能绑定到 `string`，并需允许额外的 Metric、Builder、Iterator 行为在外部实现。
- **生态兼容**：Delta/Diff/Subset/Engine/Serde 形成的“增量编辑 → 协作同步 → 序列化”链路均以 `Node<N>` 为数据载体，C# 版必须保证泛型节点在这些场景下可复制、可比较、可序列化。

## 8. 方案优化建议与风险缓释

### 8.1 接口设计：控制抽象复杂度
- **分层 Helper**：将 Leaf 操作拆成“静态常量/工厂 + 实例数据操作”两层，有助于避免在泛型上下文频繁构造 Helper；可通过 `struct TLeafOps : ILeafOperations<TLeaf>` + `static abstract` 实现达成。
- **最小必要集**：先以单元测试锁定 `TryInsertInSingleLeaf`、`TryDeleteInSingleSegment`、`NormalizeLeafMinimum` 等路径所需的最小操作集合，再评估是否需要额外扩展，防止接口膨胀。
- **聚合更新**：若 `ITreeNodeInfo` 的 `Accumulate`/`FromLeaf` 难以支撑局部更新，可增设 `WithLeafRecomputed(TLeaf leaf)` 或静态 `UpdateInfo(ref TInfo info, TLeaf newLeaf)`，避免退化到整树重算。

### 8.2 迁移节奏：保持回退通道
- **包装层保留旧 API**：先让现有 `Node` 调用泛型内核（或反过来），并保留 `Node.MinLeafSize` 等静态成员作为别名，确保回归定位时可以快速切换。
- **特化别名统一调用面**：在 `Rope`、`Delta`、`TreeBuilder` 等模块尽早改成 `RopeNode` 别名，即使泛型内核尚未完成，也能提前消化调用面变化。
- **开关式发布**：考虑加入编译期常量或环境变量控制泛型节点启用，便于在 CI 上做 A/B 对比与回退。

### 8.3 验证策略：覆盖高风险路径
- **结构不变量增强**：让 `RopeTestHelpers.AssertInvariants` 输出叶类型与 Helper 名称，便于对比泛型/特化实现的差异。
- **性能基线**：在关键节点（接口完成、TreeBuilder 泛型化、Rope 接入）采集一次 `dotnet test` 运行时间或轻量基准，避免泛型引入额外装箱/复制。
- **跨模块回归**：编写“编辑 → Delta.factor → apply → 不变量”全链路测试，确认泛型节点贯穿 Delta/Subset/TreeBuilder 时行为一致。

### 8.4 长期演进：面向多叶类型
- 为潜在 `ArrayPool<char>`、`ReadOnlyMemory<char>` 或 Breaks/Spans 叶类型预留接口（如 `BorrowSpan`/`ReturnSpan` 钩子），避免后续再度拆改。
- 在文档中跟踪每种叶类型的容量门槛、复制策略与调试输出需求，确保泛型 Node 能支撑多实现共存。
- 结合 `static-polymorphism-assessment.md` 的评估，视情况引入默认实现或泛型数学接口，减少在多个叶类型间重复代码。
