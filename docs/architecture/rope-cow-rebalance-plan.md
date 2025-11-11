# Rope 写时复制与再平衡实施方案草案

> 目的：在当前 `Node`/`Rope` 已具备的结构共享能力基础上，定义写时复制（COW）与再平衡策略的落地路径，指导后续编码、测试与性能验证。

## 1. 背景与现状
- `SplitAt`、`Slice`、`Insert`、`Delete` 已重写为走结构共享路径，但仍会在编辑过程中重建大量中间节点。
- 树结构暂未设置叶节点/内部节点的容量上下界，顺序插入可能导致树高增长或局部失衡。
- 聚合信息（`RopeInfo`）在编辑过程中整体重算，缺乏局部更新优化。
- 现有测试覆盖基础编辑语义，但尚未验证共享节点、边界合并与再平衡行为。

## 2. 设计目标
1. **结构共享最大化**：对未修改子树引用复用，保证常见编辑操作的内存分配与拷贝数量控制在 O(log n)。
2. **平衡性约束**：遵循 B-Tree 风格的不变式，确保树高 $o(\log n)$，避免线性退化。
3. **聚合信息增量更新**：在 COW 路径中局部刷新 `RopeInfo`，保持 Metric 精度并减少重复计算。
4. **测试与基准**：补齐 COW 与再平衡相关的单元测试/属性测试，并建立性能基线以验证优化效果。

## 3. 不变式设定
### 3.1 叶节点
- 约束：`MIN_LEAF = 512`、`TARGET_LEAF = 768`、`MAX_LEAF = 1024`（初始值，可根据基准调整）。
- 断点选择：优先在换行符或 UTF-16 代理对边界分裂；无合适边界时退化为中点。
- 共享策略：当叶节点被多个父节点引用时，编辑前需复制；否则可原地修改。

### 3.2 内部节点
- 子节点个数：`MIN_CHILDREN = 4`、`MAX_CHILDREN = 8`。
- 聚合信息：`RopeInfo` 包含 `Length`、`LineCount`、`Utf16Count`，必须在节点更新时自底向上重算。
- 平衡规则：
  - 合并：若相邻节点的子节点数总和 <= `MAX_CHILDREN`，可直接合并。
  - 借用：若某节点子节点数 < `MIN_CHILDREN`，需向兄弟节点借用一个子节点。
  - 分裂：若子节点数 > `MAX_CHILDREN`，在中位附近拆分为两节点并向上插入。

### 3.3 根节点
- 允许特殊情况：根节点子节点数可低于 `MIN_CHILDREN`，但大于 1 时需满足其他约束。
- 当根节点只剩单个子节点时，可将该子节点提升为根以降低树高。

## 4. 工作拆解

| 阶段 | 子任务 | 关键交付物 | 依赖 | 验收标准 |
| --- | --- | --- | --- | --- |
| A | **节点所有权与引用管理** | `NodeBody` 引用计数或复制策略说明与初版实现 | `SplitAt`/`Concat` 现有行为 | 插入/删除流程中未触碰分支的节点引用保持不变（通过调试断言或测试验证）。 |
| B | **叶节点策略** | `EnsureWritableLeaf`、`SplitLeaf`, `MergeLeaf` 实现 | 阶段 A | 叶节点长度在编辑后保持约束，同步更新 `RopeInfo`，通过跨叶编辑测试。 |
| C | **内部节点再平衡** | `RebalanceAfterEdit` 框架、借用/合并逻辑 | 阶段 B | 顺序插入/删除 10^5 字符后树高度上限保持在 `ceil(log_{MIN_CHILDREN}(n)) + 1`。 |
| D | **聚合信息增量更新** | 上行更新函数 `RefreshInfoUpwards` | 阶段 C | 编辑操作仅重新计算沿途节点的 `RopeInfo`，测试验证行/UTF-16 计数无回归。 |
| E | **测试与诊断** | 新增 xUnit 测试（共享引用、借用、合并、分裂、性能守护） | 阶段 A-D | 新增 8+ 测试场景；基准记录 v0（当前）与 v1（COW+Rebalance）差异。 |
| F | **性能基线** | BenchmarkDotNet harness 与初次运行报告 | 阶段 E | 提供 3 个典型场景（顺序插入、随机编辑、批量删除）指标并记录至文档。 |

## 5. 关键 API 变更草案
| 类/方法 | 新增/调整 | 说明 |
| --- | --- | --- |
| `Node` | `WithChildReplaced(int index, Node newChild)` | 在 COW 路径中创建共享节点的新实例。 |
| `Node` | `EnsureWritableLeaf()` | 当叶节点引用计数 > 1 时复制 leaf 内容。 |
| `Node` | `SplitLeaf(int splitIndex)` | 按不变式分裂叶节点，返回 `(left, right)`。 |
| `Node` | `BorrowFromLeft/Right(...)` | 从兄弟节点借用子节点，更新 `RopeInfo`。 |
| `Rope` | `EditCore(...)` | 拆分为调整叶节点、更新父链、触发再平衡三个步骤。 |
| `TreeBuilder` | `BuildBalanced(IEnumerable<string> chunks)` | 构建满足新不变式的初始树，用于文件加载与测试准备。 |

## 6. 测试计划
1. **单元测试**
      - `NodeTests`
     - `InsertMaintainsLeafConstraints`
     - `DeleteTriggersLeafMerge`
     - `BalanceRestoresHeight`
     - `CowPreservesSharedSubtrees`
  - `RopeTests`
     - 顺序插入 / 顺序删除 / 随机编辑验证长度与行计数。
     - 与 `StringBuilder` 基线比对，确保结果一致。
2. **属性测试（可选，FsCheck）**
   - 随机编辑序列，确保最终文本与朴素实现匹配。
   - 验证树高度与叶节点大小区间。
3. **性能基线**
   - `BenchmarkDotNet` 方案：10^5 字符文档上插入/删除 1000 次编辑。
   - 指标：执行时间、GC 分配、最大内存。

## 7. 实施节奏建议
- **Day 1**：完成阶段 A~B 草案实现与基础测试。
- **Day 2**：实现内部节点再平衡与聚合信息增量更新（阶段 C~D），对现有测试回归。
- **Day 3**：补充测试与 Benchmark（阶段 E~F），记录性能数据并回写文档。
- **持续**：根据 Benchmark 结果迭代叶节点大小/分支度参数，并评估 `ArrayPool<char>` 的引入时机。

## 8. 未决问题与后续调研
- 是否需要自定义引用计数（如 `RefCountBox`）或采用 `WeakReference` + 只读节点策略。
- 在多线程场景下的安全性边界：当前策略假设单写多读，后续是否需要同步原语。
- 叶节点采用 `string` 与 `char[]` 的性能对比：需在基线完成后安排实验。
- 与 Delta/Subset 的集成：COW/再平衡完成后，Delta 应当复用新的 API；需确保两者契合。

## 9. 治理与文档
- 本方案确认后，需同步更新 `docs/architecture/module-migration-plan.md` 中的阶段里程碑与交付物列表。
- 实施过程中记录关键决策（如参数调整、引用管理策略）至 `AGENTS.md` 的“决策 & 假设日志”。
- 性能基线完成后，形成独立报告或补充到 `docs/architecture/rope-performance.md`（待建）。

---
> 下一动作：按照阶段 A 启动写时复制辅助 API 的编码，先在 `Node`引入必要的引用管理骨架并补充最小测试。