Robird: 这是一个实验git分支，我正在尝试对位机械性翻译rust为近似C#的路线。请帮我先整体评估一下，以语句为单位，哪些是能“机械翻译”的，哪些需要做设计调整。

GitHub Copilot: 读取 [](file:///e%3A/repos/Atelia-org/xi-editor-sharp/src/xi.Core/Rope/Tree/Node.cs)

先帮你过一遍 `tree.rs` 的主要语句，标记出可以直接机械翻译的部分，以及需要在 C# 侧重新设计的部分。

## 评估范围与基准

- 以 Rust 源文件 tree.rs 为基准，按结构块（常量、trait、结构体、impl 块、方法体语句）评估。
- “机械翻译”指可无脑映射到等价 C# 语法/库（语义保持一致，复杂度基本为语法替换）。
- “需要设计调整”指 C# 缺少等价概念、或现有 xi.Core 计划（如静态抽象接口、写时复制策略）需要格外处理的语句。

## 语句分类总览

| 位置/类别 | 示例语句 | 机械翻译 | 需要调整 | 备注 |
|-----------|---------|---------|---------|------|
| 模块导入 | `use std::cmp::{min, Ordering};` | ✅ |  | 直接对应 `using System;` + `System.Math.Min` 等。 |
| 常量定义 | `const MIN_CHILDREN: usize = 4;` | ✅ |  | 映射为 `private const int MIN_CHILDREN = 4;`。 |
| trait 静态常量 | `const CURSOR_CACHE_SIZE: usize = 4;` | ✅ |  | 同上。 |
| trait 定义（无关联类型） | `pub trait Metric<N: NodeInfo> { ... }` 中普通方法签名 | ⚠️ | ✅ | C# 接口无法表达关联类型；需通过泛型/静态抽象成员重构。 |
| trait 关联类型 | `type L: Leaf;` |  | ✅ | 必须改写为额外的泛型参数或静态抽象接口；当前 `ILeafOperations<T>` 方向。 |
| trait 默认实现 | `fn identity() -> Self { ... }` | ⚠️ | ✅ | C# 接口默认实现可行，但若依赖关联类型（`Self::L::default()`）则仍需设计 `default` 机制。 |
| `#[derive(Clone)]` | |  | ✅ | C# 缺省无自动 Clone；需手写 `ICloneable` 或深拷贝逻辑。 |
| tuple struct | `pub struct Node<N: NodeInfo>(Arc<NodeBody<N>>);` |  | ✅ | 需展开为 class +字段；`Arc`→`Arc<NodeBody>`? C# 需 `RefCounted`/`Shared<T>` 自定义。 |
| `enum NodeVal` |  | ⚠️ | ✅ | C# 可用 `sealed class` + 子类或 `struct` + `DiscriminatedUnion`；需手工实现 `Leaf`/`Internal` 判别。 |
| `impl` 块静态函数 | `pub fn from_leaf(...)` | ✅ |  | 转成 `public static Node FromLeaf(...)`（前提内层已设计好）。 |
| `impl` 块普通函数 | `pub fn len(&self) -> usize` | ✅ |  | getter 映射简单。 |
| 模式匹配 | `match {...}` / `if let` | ⚠️ | ✅ | 需改写为 `switch`/`if` + 手写解构。 |
| `debug_assert!` | | ⚠️ | ✅ | C# 没有同名宏；可用 `Debug.Assert` 或条件编译。 |
| `Arc::make_mut` | |  | ✅ | 需引入写时复制容器；现有 C# 没等价标准库。 |
| 切片 `&nodes[1..]` | | ⚠️ | ✅ | 需使用 `for (var child in nodes.Skip(1))` 等；注意分配。 |
| 数组连接 | `[children1, children2].concat()` | ⚠️ | ✅ | 需 `children1.Concat(children2).ToList()` 或 `Span`；性能考量。 |
| 泛型约束 | `<N: NodeInfo>` | ⚠️ | ✅ | C# 需 `where N : INodeInfo<NLeaf, NMetrics>` 等方案。 |
| `Option` 处理 | `Option<Self>` | ⚠️ | ✅ | 用 `T?`/自定义 Option；涉及模式匹配。 |
| `Vec` 操作 | `vec![...]`, `push` | ✅ |  | 映射为 `List<T>`. |
| `interval` 相关方法 | `iv.into_interval(self.len())` | ✅ |  | 若 Interval API 已复刻，可直接调用。 |
| `panic!` | | ⚠️ | ✅ | 对应 `throw new InvalidOperationException(...)`. |
| `Ordering` 比较 | `match h1.cmp(&h2)` | ⚠️ | ✅ | C# 转换为 `if/else` 或 `switch` 枚举。 |
| `while let` / `loop` | | ⚠️ | ✅ | 需用 C# `while(true)` + `break`. |
| `Iterator` 实现 | `impl Iterator for CursorIter` | ⚠️ | ✅ | C# 需 `IEnumerable<int>` + `yield return`. |
| 生命周期 `'a` | `Cursor<'a, N>` |  | ✅ | C# 无生命周期；需约束读写语义，可能导致额外拷贝或 Span-like 设计。 |
| 测试模块 | `#[cfg(test)]` | ⚠️ | ✅ | C# 需迁移到 xUnit 类；逻辑可机械化但要调整框架。 |

## 详细说明

### 1. 顶部导入、常量

- `use`、`const` 基本可机械翻译；唯一注意 `Ordering` 枚举与 `min` 属于 `std::cmp`，C# 对应 `System.Math.Min` 和自定义枚举映射。

### 2. trait 与接口

- `NodeInfo`/`Leaf`/`Metric` 均依赖关联类型与默认实现，C# 无直接等价。你当前在 `ILeafOperations<T>` 与静态抽象接口方向已经是必须的设计调整。
- 语句如 `type L: Leaf;`、`fn compute_info(_: &Self::L) -> Self;` 均需要重新建模（泛型参数 + 静态抽象方法 + 值语义）。

### 3. 结构体/枚举

- `Node<N: NodeInfo>(Arc<NodeBody<N>>)`：Rust 的 tuple struct + `Arc`（共享所有权）。C# 需 `class Node<TInfo>` + 内部 `_inner`，同时实现 `Copy-on-write`（需自建引用计数或借用 `System.Threading.Interlocked` 维护）。语句层面不能机械搬运。
- `enum NodeVal` -> discriminated union。C# 缺内建 `enum` 携带数据，需要设计 `NodeKind` + `LeafValue`/`Children`，或者 `OneOf`/`Union` 模式。

### 4. `impl Node`

- 纯 getter (`len`, `is_empty`, `height`) 可机械翻译。
- 依赖 `Arc::make_mut`、`match` 的函数（如 `with_leaf_mut`, `merge_leaves`）需重写 COW 逻辑：必须设计 `EnsureWritable` 等 API，不是机械。
- `concat` 中递归 + `match Ordering` + 切片语法 -> 需重写控制流，可视为“结构保持但手动翻译”，即语义能保留，不过 `Arc::make_mut`/`clone`/`get_children` 仍要先有设计。

### 5. `TreeBuilder`

- `push` 中 `loop` + `match` + `Vec` 操作需改写为 C# `while (true)` + `switch`/`if`。
- 使用 `Interval`、`push_maybe_split` 的语句若 C# 端已有等价类型则可机械翻译；否则需先把 Interval 契约补齐。
- `push_slice` 的递归 match 在 C# 中可翻译为 `if (node.IsLeaf)` 等，属于中等复杂度但可机械；关键阻碍是 `Arc`/COW 设计。

### 6. `Cursor` 与生命周期

- 结构体字段 + 构造函数基本可机械翻译（改为 class/struct +数组）。
- 但依赖 `Option`、`match`、生命周期 `'a`（借用 tree 内部引用）——C# 没法安全返回叶子引用而不复制；可能需要 `Span<char>` 或直接返回 `NodeRef`，属于设计调整。
- Methods `prev`, `next`, `iter` 里的 `Option`、`Metric` 泛型 trait 调用都需要 C# 静态抽象接口支持；否则无法机械搬运。

### 7. `CursorIter` 与 `Iterator`

- Rust `impl Iterator` → C# 需实现 `IEnumerable<int>` 和 `IEnumerator<int>` 或 `yield return`。语句可机械翻译成 `while(cursor.Next()) yield return cursor.Position;`，但 `Metric` 接口仍是阻塞点。

### 8. 测试模块

- `#[cfg(test)]` 与 `#[test]` 块必须迁移到 xUnit/NUnit；断言 `assert_eq!` → `Assert.Equal` 可机械，但需改写 `build_triangle`、测试结构。
- 语句中 `cursor.next::<LinesMetric>()` 依赖泛型常量；C# 版本要用 `cursor.Next<LinesMetric>()` + 约束。

## 汇总

- **完全可机械翻译**：常量、简单 getter、List/Vec push、`TreeBuilder.build` 的 while 循环等，只需语法替换。
- **需要设计调整**：
  - 所有 trait 关联类型、默认泛型方法。
  - `Arc` 写时复制、`Option`/`Result` 模式匹配、`enum` 带数据。
  - 生命周期 `'a`、`Cursor` 返回借用引用。
  - `impl Iterator`、`debug_assert!` 宏、`panic!`。
  - `Node`/`TreeBuilder` 内部对 `Vec` 切片、连接的高阶语法（需考虑性能和分配）。

总体来说，上层控制流（if/while/for）多可以保持语序机械翻译；真正需要重新设计的是语言特性差异处：关联类型 + 写时复制容器 + 生命周期语义。只要先在 C# 侧抽象出 `ILeafOperations`、`SharedNode<T>` 等契约，再回头搬运语句，很多 `match`/`Option` 就能按“模板化”翻译。当前分支主攻泛型 Node/叶操作已经瞄准最大痛点，建议先把这些设计块定型，再分函数机械迁移。

Robird: 我有了一个新思路，原版rust repo不是有完善的单元测试嘛！那些不易翻译为C#的语言特性，不一定完全依赖在C#端重新建模，我们还可以评估在rust端是否必须那样写，把rust版本身重构成跟更易于移植为其他语言的形式。请沿着这个思路，帮我再分析一遍rust源码中难以机械翻译为C#的那些部分，其使用rust语言特性哪些不是必须的。

GitHub Copilot: 马上基于 `tree.rs` 重新审视那些阻碍机械翻译的 Rust 特性，看看哪些其实可以在 Rust 端重构成更“跨语言友好”的写法。

## Rust 特性复用 vs 可重构性🧭

| 特性/位置 | 当前写法 & 目的 | 是否必须 | 可在 Rust 端改写的思路 | 对测试/行为的影响 |
|-----------|----------------|-----------|--------------------------|--------------------|
| **`NodeInfo` / `Leaf` / `Metric` 的关联类型** | 通过 `type L` / `type DefaultMetric` 把叶节点与聚合信息绑定，静态确保一致 | 否：目标主要是编译期保障 | 1. 改为显式泛型：`trait NodeInfo<L>`，`Node<L, Info>`，把叶类型作为参数;<br>2. 若继续使用 trait，可拆成“信息”与“叶”两组 trait，通过约束组合 | 编译期约束略松，但可用单元测试兜底；接口变化较大，需要同步调所有实现与测试 |
| **trait 默认实现 (`fn identity` 等)** | 复用 `compute_info` 默认逻辑，减少重复代码 | 否：只是便利性 | 1. 将默认方法拆到自由函数或 helper 模块；<br>2. 或保持 trait，但用更基本的函数（移除关联类型依赖） | 影响极小；测试可保持原样 |
| **`Arc<NodeBody>` + `Arc::make_mut`** | 提供写时复制与跨线程共享 | 是：如果仍需 COW 语义 | 可替换为自定义 `SharedNode`（`Arc` 包装 + 手动 `try_unwrap`），接口用普通方法调用而非 `Arc::make_mut`；在 Rust 端实现更简单的 `ensure_unique`/`clone_with` API 后，C# 更容易模拟 | 行为不变；不过要验证 COW 的引用语义，通过现有 Rope 测试即可 |
| **`enum NodeVal { Leaf, Internal }`** | 区分叶节点与内部节点 | 否：枚举只是表达方式 | 1. 用结构体 + 布尔标记;<br>2. 或拆成 `LeafNode`/`InternalNode` 两个 struct，通过 `NodeBody::Leaf`/`NodeBody::Internal` wrapping；<br>3. 甚至直接在 `NodeBody` 使用 `Option<Vec<Node>>` | 结构拆分后，需要更新 `match`/`if let` 逻辑，但测试可自动验证 |
| **大量 `match` / `if let`** | 控制流表达 | 否：语言风格 | 可统一替换为 `if {...} else {...}`，或提取方法让模式匹配集中在一处 | 只是语法调整，测试不受影响 |
| **`Ordering` 枚举匹配** | 清晰表达三向比较 | 否 | 改成 `if h1 < h2 { ... } else if h1 == h2 { ... }` | 无风险 |
| **切片语法 (`nodes[1..]`)** | 快速遍历子数组 | 否 | 改成 `for child in &nodes[1..]` → `for child in nodes.iter().skip(1)` 等 | 仅语法变化 |
| **`Option` / `None` 返回** | 表示可能缺失 | 否 | 仍可使用 `Option`; 但若想便于 C#，可在 Rust 端改用返回 `(bool, T)` 或自定义 struct；甚至将“无值”视为特殊 sentinel | API 会变化，需调整调用和测试断言 |
| **生命周期 `'a` (Cursor)** | 避免分配，直接借用树中叶片 | 否：可换成引用计数或 ID | 1. Cursor 改用 `Arc<Node>` 或节点索引路径，内部每次 `get_leaf` 重新 `Arc::clone`；<br>2. 或返回 `Cow<'a, Leaf>` 兼容借用与所有权 | 需要补充测试验证性能/行为；可能牺牲零分配优势，但功能保持 |
| **`CursorIter` 实现 `Iterator`** | 提供迭代接口 | 否：可手写 `next`/`prev` 方法 | 若更易翻译，可取消 `impl Iterator`，直接提供 `Cursor::next_boundary`，并在测试中用 while 循环；Rust 端新增简单封装即可 | 现有迭代行为测试要调整为显式循环 |
| **`debug_assert!` / `panic!`** | 开发期断言 | 否 | 用常规函数 `ensure_children_ok` 返回 `Result`，或统一通过 `assert!`/`Result` 传播 | 测试需覆盖预期失败分支 |
| **`#[cfg(test)]` 内联单测** | 保持多文件共享私有结构 | 否 | 可拆到独立测试模块/公共 helper，减少 `#[cfg]` 的侵入；便于生成更通用的测试数据 | 迁移工作量不大；测试照跑即可 |

## 推荐改造优先级

1. **接口层（NodeInfo/Leaf/Metric）**  
   - **短期**：先在 Rust 端引入泛型版 alias（例如 `type RopeNode = Node<RopeInfo, RopeLeaf>`），逐步去除关联类型依赖。  
   - **中期**：将 `compute_info`/`push_maybe_split` 的调用从静态函数改为显式 helper，以便未来映射到 C# 的 `StringLeafOperations`。

2. **结构表现形式**  
   - 把 `NodeVal` 改写成“扁平结构 + bool/enum 标记”，同时提供 `is_leaf()`、`as_leaf_mut()` 等辅助函数，使得源码更接近面向对象风格。

3. **COW 封装**  
   - 用 Rust 新增 `NodeBody::ensure_unique()` 等方法隐藏 `Arc::make_mut`，这样迁移时只需复制方法语义，不必在 C# 端重新模拟 `Arc`.

4. **Cursor 生命周期**  
   - 研究把 cursor 改成“存索引 + 每次按需取叶”的实现（即便性能稍降），以换取更通用的对象模型；现有测试套可验证行为一致。

5. **迭代器包装**  
   - 将 `CursorIter` 简化成 `pub fn collect_boundaries<M>(&self) -> Vec<usize>` 等更显式的 API，再用 tests 比对结果；方便 C# 侧直接移植。

## 结论与下一步

很多“难以机械翻译”的语句，其实是出于 Rust 风格/性能取舍，并非语义所迫。只要在 Rust 仓库里提前引入一层抽象（泛型参数、helper 函数、独立 COW 管理、显式控制流），就能让原测试继续覆盖，同时把源码变成“语言中立骨架”。建议按优先级分批重构，再运行原有单测确保回归，之后 C# 端同步迁移会顺畅许多。

## Plan: 双向移植路线规划

以 Rust/C# 双向收敛为目标，先裁剪 scope、统一核心抽象，再按阶段对齐节点/叶、Cursor、Delta/Subset 等模块，依托现有测试基线滚动验证；Rust 端通过辅助 API、泛型重构、移除强语言特性的写法来降低翻译难度，C# 端配合静态抽象/写时复制骨架，最终形成可脚本化的分段迁移流水线。

### Steps
1. 归档并冻结核心范围：Rust `core-lib/rope` + C# `xi.Core.Rope`；暂停 RPC/插件/UTF-8 等非目标功能。  
2. 在 Rust 侧引入迁移友好层：封装 `Arc::make_mut`、拆 `NodeVal`、用显式泛型取代关联类型，保持现有测试全绿。  
3. 对齐节点/叶抽象：完成 Rust/C# 双方 `Node<TInfo, TLeaf, TLeafOps>`、`ILeafOperations` 等接口，共享写时复制/容量策略测试。  
4. 重建遍历与迭代：在 Rust 中改造 Cursor/迭代器到可索引模式；C# 侧实现等价 API 并覆盖 invariants/基准。  
5. 迁移 Delta/Subset 与增量更新：先在 Rust 做可翻译重构，再在 .NET 端按映射实现并复用测试文件。  
6. 建立分段迁移流水线：脚本按模块比对骨架、复制实现、运行双端测试/基准，逐步补充文档与基线。

### Further Considerations
1. 是否要先提升 Rust MSRV（如 1.70+）以便使用现代 API 与更易翻译的语法？  
2. 工具链协同建议？Option A：统一 Criterion/BenchmarkDotNet；Option B：先跳过性能测试。

## Plan: MSRV & Core Refocus

将 xi-editor-ph7 定位为“核心 Rope 库”方案：先同步升级工具链，再清理性能基准与非核心 crate，最后产出实施计划文档，交付给后续实现阶段。

### Steps
1. 统一目标版本  
   - Rust：更新 rust/Cargo.toml、docs/building-core.md、rust/metadata.json，锁定 ≥1.70；检查 CI/脚本对旧版本假设。  
   - .NET：确认 net9.0 作为最小版本，评估是否同步迁移到 net10.0（可选）。

2. 构建“纯核心”工作区  
   - 修订 Cargo.toml `[workspace]` 成员，只保留 `core-lib`, `rope`, `unicode`（看是否留下 `trace`）；移除 `[patch]` onig/`syntect` 相关。  
   - 梳理 `core-lib` 对 RPC/插件模块的依赖，若暂时保留需在文档中标明未来拆除步骤。

3. 移除性能基准资产  
   - 删除/归档各 crate `benches/` 目录；移除 Criterion `dev-dependencies` 与 `features`（`benchmarks`）。  
   - 更新 scripts/run_all_checks、docs 中的 bench 说明；清理 nightly-only API (`#[cfg(feature="benchmarks")]` 等)。

4. 制定后续清单  
   - 记录仍引用被裁剪功能的 API/测试；列出需重构的调用点（如 core-lib 中对 rpc、trace 的耦合）。  
   - 标注 C# 侧配套调整（例如删去 BenchmarkDotNet 计划、保持 net9.0）。

5. 草拟实施方案文档  
   - 新建 docs/architecture/msrv-core-plan.md（在实现阶段完成）。  
   - 章节建议：背景、目标、范围（保留/删除模块列表）、详细步骤（版本升级、依赖裁剪、脚本更新、测试口径）、风险与回滚策略、待办事项。

**现状确认**
- `rust/Cargo.toml` 已设置 `rust-version = "1.75"`，仓库目标版本统一完成。
- `cargo test --workspace` 全量运行 328 项单元/文档测试全部通过；目前只有硬链接受限、`PluginLoadError` 未读字段、`serde_test` future-incompat 等警告待清理。
- 当前清理重点已转向移除 Criterion 基准与非必要 crate，为后续 C# 迁移缩小 Rust 侧活动面。

**后续行动计划**
- **阶段 1｜Rust 工作区瘦身**：逐 crate 移除 `benches/` 与 `[[bench]]`、Criterion dev-deps，刷新 `run_all_checks` 与 README；同时生成一份保留 crate 清单（rope/core-lib/xi-core）并标注 C# 侧对应责任。
- **阶段 2｜契约与骨架固化**：同步更新 `docs/skeleton/*.md` 与 architecture 计划，冻结即将移植的 API 签名；对照 Rust ↔ C# 的 Rope/Delta/Tree 合约，补充缺口与假设。
- **阶段 3｜核心模块移植**：依次完成 Rope COW 阶段收尾、Delta/Subset 最小实现、TextBuffer 替换；每完成一块即在 `xi.Core.Tests` 添加对照用例并记录与 Rust 行为差异。
- **阶段 4｜系统集成**：按优先级迁移 RPC/LSP/Plugin 管线，使用精简后的 Rust 仓库作为参考；建立 .NET 宿主原型和跨语言集成测试基线。
- **阶段 5｜收口与发布**：收集性能回归（BenchmarkDotNet vs. Rust benches遗留数据）、关闭 remaining warnings/future-incompat，整理迁移文档与发布 checklist。

**建议下一步**
1. 启动阶段 1：先在 `rope`, `core-lib`, `unicode`, `trace` 等 crate 移除基准并提交脚本/文档更新。
2. 跑 `cargo report future-incompatibilities --id 1` 核实 `serde_test` 未来不兼容根因，决定是升级或移除。
3. 将阶段 2/3 的责任拆解写入 `module-migration-plan.md`，方便后续迭代时跟踪完成度。

**阶段 1 瘦身计划**
- 核心保留：`xi-core`、`xi-core-lib`、`xi-rope`、`xi-unicode`、`xi-rpc`、`xi-trace`（暂缺替代的诊断支撑）。
- 近端参考：`xi-plugin-lib`（供 C# 插件接口对照，但后续可迁出默认构建）、`metadata.json`、run_all_checks。
- 候选下线：`experimental/lang`、`lsp-lib`、`sample-plugin`、`syntect-plugin`（纯插件/LSP 示例，已不在近期移植范围）。
- 基准残留：`rope/benches/*`、`trace/benches/bench_trace.rs`、`core-lib/benches/wrap.rs`、`experimental/lang/benches/peg_tests.rs` 等仍引入 Criterion。

1. 现状快照：记录一次 `cargo check --workspace` 与 `cargo test --workspace` 输出，确认 1.75 下基线，更新 AGENTS.md/计划文档，便于回溯。
2. Bench 迁出（按依赖从叶到根）：  
   a. 依次处理 `experimental/lang` → `sample-plugin` → `syntect-plugin` → `trace` → `core-lib` → `rope`（无被依赖方先移除风险）；  
   b. 将 `benches/` 重命名为 `benches.disabled/`（环境禁止直接删除），删除同目录 Cargo.toml 中的 Criterion dev-deps/`[[bench]]`；  
   c. 复核 run_all_checks，确保不再引用 `cargo bench` 或 Criterion；  
   d. 每完成两个 crate 执行 `cargo check -p <crate>` 保障局部健康，再在批处理结束时跑一次全量测试。
3. Crate 精简（按依赖拓扑）：  
   a. `experimental/lang`：从根 `Cargo.toml [workspace.members]` 移除并将目录改名为 `experimental/_lang_parked`，`Cargo.lock` 运行 `cargo metadata` 触发行清理；  
   b. `sample-plugin`、`syntect-plugin`：同样移出 workspace，确认 `xi-core` 不再通过 bins 依赖它们；  
   c. `lsp-lib`：移出后检查 `xi-core` 与 run_all_checks 是否仍引用 `xi-lsp-plugin`；  
   d. 评估是否将 `plugin-lib` 与以上归档到 `legacy/` 目录，仅在文档中保留引用；若保留，至少在根 Cargo.toml 设置 `default-members` 只包含六个核心 crate，避免常规构建扫描插件链。
4. 依赖梳理：  
   a. 在 `core-lib` 内临时引入 `cfg(feature = "trace")` 宏，为后续替换 `xi-trace` 打基础（本阶段只标记，不关断）；  
   b. 删除 `Cargo.lock` 中不再引用的 Criterion/插件依赖，`cargo check` 验证无悬挂特性；  
   c. 在 module-migration-plan.md 补写“Rust 子集”章节，列出保留 crate 与 C# 映射关系。
5. 文档与脚本收尾：  
   a. 更新 `README.md`、AGENTS.md 说明 Rust 子仓库仅用于 C# 对照、bench 已迁除；  
   b. 在 rope-port-mapping.md 记录仍保留 crate 的对照用途；  
   c. 若引入 `legacy/` 目录，为其添加 `README` 提示仅作参考，不参与默认构建。

输出物：瘦身后的 Cargo.toml（含 `default-members`）、精简 `Cargo.lock`、归档的 bench/crate 目录、更新的文档与脚本。完成后即可进入阶段 2 的接口骨架对齐与 Rope 迁移。