# Xi Rope 移植方法论

## 背景
为了在 .NET 平台上复刻 `xi-editor` 的高性能 Rope 与周边增量编辑能力，我们决定采用“**先契约后实现**”的分层移植策略，并与 `xi-editor-ph7` fork 上的 Rust 重构形成“双向奔赴”：即 C# 侧贴近原实现的同时，Rust 端也主动减少语言特性束缚，形成共享骨架。

## 核心原则
- **类型优先**：先对齐公共 API、结构体/枚举和内部数据承载类型，再逐步补齐函数逻辑。
- **文件一一映射**：Rust 原始文件与 C# 目标文件建立显式关系，方便定位实现差距与追踪进度。
- **最小增量**：每一轮实现只聚焦一个函数或小型 helper，配套引入/改写对应测试，防止大范围回归。
- **持续校验**：在 C# 版本中保留 `NormalizeLeafMinimum()`、`ValidateInvariants()` 等诊断入口，确保逐步移植过程中结构不变量始终被约束。
- **先 Rust 后 C#**：遇到语言特性差异（如关联类型、`Arc::make_mut`、生命周期）时，优先在 Rust fork 内提供迁移友好 helper，再将新语义拷贝至 C#。

## 工作流拆解
1. **建立映射清单**
   - 新建文件级映射文档，记录 Rust → C# 的计划关系与当前状态。
   - 标记每个条目的阶段：`未开始`、`接口对齐中`、`实现中`、`已完成`。

2. **对齐接口与类型系统**
   - 将 Rust 中的公开 `trait`、`struct`、`enum`、`type alias` 翻译为对应的 C# `interface`/`record`/`class`。
   - 对内部帮助器（如 `NodeInfo`、`TreeBuilder`）保留空实现或 `NotImplementedException`，确保编译通过。
   - 记录差异化处理策略（如 Rust 的 `Borrow`/`Rc<RefCell<T>>` 对应 C# 的不可变写时复制模式）。

3. **测试骨架占位**
   - 在 `xi.Core.Tests` 中同步创建与 Rust 测试等价的测试方法/类，仅留 `TODO` 或 `Skip` 标记。
   - 在 Rust 端将原 `#[cfg(test)]` 测试迁移到独立 helper（便于导出数据），并在进入函数实现前确保双侧测试骨架就绪。

4. **函数级增量实现**
   - 逐条迁移：选定 Rust 函数 → 复制伪码/注释 → 实现 C# 版本 → 补充或激活测试 → 运行 `dotnet test`。
   - 如遇到 `Arc::make_mut`/生命周期等差异，先在 Rust 端引入 `SharedNode::ensure_unique`、`CursorCache` 等辅助方法，再在 C# 中引用相同签名，保持语义一致。
   - 必要时把复杂流程拆成多个 helper，以便配合 C# 的不可变数据结构。

5. **文档与进度同步**
   - 每完成一个阶段或关键节点，更新映射文档和 `AGENTS.md` 的“当前关键认知”与“下一步行动”。
   - `bi-direction-port.md` 记录 Rust ↔ C# 双端改造清单；`rope-port-mapping.md` 标注最新 helper 对齐情况。
   - 记录新的约束、设计决策或风险，避免重复分析。

## Rust 端配套重构

为降低语言差异带来的摩擦，Rust fork 将配合执行以下动作：

1. **封装 COW 语义**：新增 `SharedNode` 或 `NodeBody::ensure_unique()`，隐藏 `Arc::make_mut` 调用，并在测试中验证引用计数行为。
2. **拆分枚举与宏**：将 `NodeVal`, `Cursor<'a>` 等使用模式匹配/生命周期的结构改写为显式 `struct` + helper，使其更贴近面向对象模型。
3. **关联类型改造**：把 `NodeInfo`, `Metric`, `Leaf` 等 trait 改为显式泛型参数或组合接口，并提供类型别名保证现有代码编译。
4. **测试资产归档**：将关键 `#[cfg(test)]` 块抽离成公共 helper 或 JSON fixture，便于 C# 端直接使用。
5. **脚本化骨架**：持续更新 `docs/reference/rust-skeleton.md`，保证在 Rust 改造后可立即导出最新骨架，与 C# skeleton 对照。

## 预期收益
- **可分解执行**：任务粒度固定在“定义契约”与“填充函数”两个层级，便于并行或交接。
- **降低返工**：接口阶段即暴露出语言差异问题（如生命周期、借用模型），提前设计替代方案。
- **上下文友好**：工具调用只需围绕当前函数/类型展开，不需频繁回溯整个模块。

## 下一步
- 依据此方法论，编写并维护文件级映射与翻译范式文档。
- 在 Tree/Rope 树结构模块先行试点：完成 `tree.rs`、`rope.rs` 的类型对齐与测试骨架设置，并同步 Rust 端 helper 改造计划。
