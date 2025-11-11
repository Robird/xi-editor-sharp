# Rope 与 Delta 迁移要点备忘

> 目的：收敛 xi-editor 核心 Rope/Delta 能力在 .NET 迁移时的关键技术点，为后续实现提供结构化参考。

## 1. Rust 版 Rope 数据结构回顾

### 1.1 树形骨架
- `xi_rope::tree::Node<N>` 是一个泛型 B-树，内部采用 ARC + COW；叶子存放具体片段，内部节点缓存聚合信息。
- 树高自适应，常量 `MIN_CHILDREN = 4`、`MAX_CHILDREN = 8` 控制分支数，叶子大小受 `MIN_LEAF = 511`、`MAX_LEAF = 1024` 约束，保证平衡与缓存友好。
- `NodeInfo` 为节点聚合信息（单调半群），`RopeInfo` 实现存储：
  - `lines`：子树中换行符数量。
  - `utf16_size`：子树 UTF-16 code units，总用于前端协议坐标。
- `Leaf for String` 负责拼接与分裂逻辑，通过 `push_maybe_split` 在合并时维护叶节点大小上下界。

### 1.2 Metric 抽象
- `Metric` trait 让同一棵树支持多种坐标体系（字节、UTF-16、行等）。
- 核心 Metric：
  - `BaseMetric`：按 UTF-8 code unit 计数，保证所有偏移对齐到字符边界。
  - `LinesMetric`：基于换行符的分段计数，用于光标行号与可视范围判断。
  - `Utf16CodeUnitsMetric`：服务于与前端/插件协议的 UTF-16 坐标转换。
- Metric 保证 `prev/next`、`to_base_units`、`from_base_units` 等操作 O(log n)，并能跨叶片段处理断裂场景（`can_fragment`）。

### 1.3 核心操作流程
- `TreeBuilder` 用于增量构建树（文件加载、应用增量更新）。
- `Node::concat`、`Node::subseq`、`Node::edit` 等方法在保持平衡的同时更新 `NodeInfo`。
- `Cursor` 与遍历 API 为查找偏移、定位节点的高阶工具，未来需要一并迁移。

### 1.4 性能假设
- 叶子采用 1KB 左右大小可以兼顾缓存命中与树高；.NET 版本需验证 GC/LOH 开销。
- Rust 版本通过 ARC + Cow 减少复制；C# 需复刻“写时复制”语义以支撑多视图共享。

## 2. Delta/Subset 机制

### 2.1 Delta 表达
- `Delta<RopeInfo>` 将编辑操作表示为 `Copy(begin, end)` 与 `Insert(Node)` 序列，并记录 `base_len`。
- `Builder` 支持 `replace`/`delete` 等高层操作，保证输出 Delta 满足最小化和排序约束。
- `apply` 将 Delta 应用到 `Node`，内部复用 `TreeBuilder`。

### 2.2 组合与转换
- `factor()` 将 Delta 拆为纯插入部分 `InsertDelta` 与删除集合 `Subset`，便于与插件、同步模块复用。
- `transform_expand`、`synthesize` 等操作处理并发编辑、撤销/重做所需的坐标变换。
- `summary()` 提供快速估算受影响区间，减少通知范围。

### 2.3 Subset/Multiset
- `Subset` 用稀疏区间集合表示删除位置，依赖 `transform_*` 方法在多视图场景下重映射坐标。
- 迁移时需同步移植 `multiset` 模块，提供 `CountMatcher` 等辅助枚举。

## 3. C# 迁移设计要点

### 3.1 类型映射建议
- `RopeNode`（类/struct）：对应 Rust `Node`，内部持有 `RopeNodeBody`（高度、长度、`RopeInfo`、叶或子节点集合）。
- `RopeInfo`（readonly struct）：
  - `int LineCount`
  - `int Utf16Length`
  - 静态方法 `Accumulate`、`FromLeaf`。
- 叶子候选：
  - 初期使用不可变 `string`（与 Rust `String` 对齐），写时复制时替换整片字符串。
  - 中期考虑 `char[]` + `ArraySegment<char>` 或 `ReadOnlyMemory<char>` 承载，以便 span 化访问和池化。
- `IRopeMetric<TMetric>` 接口：等价于 Rust `Metric` trait，约束测量、边界判断及前后游标操作。
- `TreeBuilder`、`RopeCursor` 等单独类型维持与 Rust 相同责任划分。

### 3.2 内存与并发策略
- 使用 `ReferenceCounted<RopeNodeBody>`（可基于 `System.Threading.Interlocked` + 自定义 COW）模拟 ARC 行为。
- 叶子扩容/分裂流程：
  1. 检查写入者是否唯一持有节点；
  2. 若共享则复制叶片；
  3. 在超出 `MAX_LEAF` 时调用 `FindLeafSplit()`（优先换行、退化到 UTF-16 边界）。
- 预研 `ArrayPool<char>` 降低重复分配成本，并评估在 .NET 9 中的 `Span<T>` 与 `Memory<T>` 支持。

### 3.3 Metric API 迁移
- 定义 `BaseMetric`, `LineMetric`, `Utf16Metric` 三个静态类或单例，实现：
  - `Measure(RopeInfo info, int length)`
  - `ToBaseUnits(ReadOnlySpan<char> leaf, int measured)`
  - `FromBaseUnits(ReadOnlySpan<char> leaf, int baseUnits)`
  - `TryFindPrev/NextBoundary`
- 提供静态注册表以便未来扩展（例如 grapheme cluster、display width）。

### 3.4 Delta 与 Subset
- `RopeDelta` 类封装：
  - `List<DeltaElement>`，元素为 `Copy(int start, int end)` 或 `Insert(RopeNode)`。
  - `int BaseLength`。
- `DeltaBuilder` 提供 Fluent API（`Replace`, `Delete`, `Build`），与 Rust 语义保持一致。
- `Subset` 使用压缩区间表（`List<(int start, int end, int count)>`）实现 `transform_expand` 等操作。
- 确保 `Apply`, `Factor`, `Compose` 等操作在 O(k log n) 内完成，其中 k 为 Delta 元素数量。

### 3.5 与现有 `TextBuffer` 的衔接
- 定义接口 `ITextBuffer`：
  - `int Length { get; }`
  - `ReadOnlySpan<char> GetSlice(int start, int length)`
  - `void Apply(RopeDelta delta)`
- 当前 `TextBuffer` 以 `StringBuilder` 实现；短期复用此接口以建立测试基线，然后无缝替换为 Rope 实现。
- 新增的 Rope 需实现上述接口并通过现有单元测试，再扩展覆盖 delta 应用、行/UTF-16 坐标转换等案例。

## 4. 渐进式落地计划

1. **抽象层搭建**：
   - 引入 `ITextBuffer` 与 Metric/Delta 基础类型的接口定义。
   - 将现有测试改造为接口基准测试（插入、删除、Span 读取）。
2. **最小 Rope 骨架**：
   - 实现 `RopeInfo`、`RopeNode`、`TreeBuilder` 基础操作（构建、拼接、子串）。
   - 替换 `TextBuffer` 底层为单叶 Rope，确保单元测试通过。
3. **增量操作**：
   - 移植 `Edit`, `Cursor`, `Metric` 系列方法，补充针对空文档、跨叶边界、UTF-16 代理对等边界测试。
4. **Delta/Subset 迁移**：
   - 移植 `RopeDelta` 与 `Subset`，构建针对多种编辑模式的测试组合。
   - 验证 `factor`、`transform_expand` 在并发编辑/撤销场景下的正确性（引入参考 trace）。
5. **性能与内存验证**：
   - 构建基准，覆盖大文件加载、随机编辑、重复撤销/重做。
   - 探索 `ArrayPool<char>` 等优化策略，记录 GC 影响。

## 5. 风险与开放问题

- **写时复制语义**：.NET 缺乏 ARC，需要自定义引用计数或 `WeakReference` + `Interlocked` 方案，需特别关注多线程安全。
- **叶节点内存布局**：使用 `string` 虽简洁但在频繁编辑下会触发大量新字符串生成，可能影响性能；需对 `char[]` 方案提前预研。
- **Metric 扩展性**：Rust 中的 trait 对泛型友好，C# 需要考虑泛型接口、静态多态与性能之间的权衡。
- **并发编辑**：`transform_expand` 等算法涉及大量区间映射，需确保整数溢出、安全检查符合 .NET 约束。
- **测试资产导入**：Rust 版有海量 property-based 测试，C# 需决定等效的 QuickCheck/ FsCheck 引入策略。

---

> 下一步：根据上述计划在代码层引入接口骨架与初版 Rope 节点，实现最小替换并扩展测试覆盖。
