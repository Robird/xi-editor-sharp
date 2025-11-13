## Rope 双向移植评估

> 目标：明确 C# 与 Rust 双侧协同的移植策略，既保持 C# 端贴近原版实现，也在 `xi-editor-ph7` fork 中逐步减少 Rust 特有语言特性的依赖，让两端最终围绕同一套骨架对齐。

## 1. 评估范围
- 以 `reference/rust/rope/tree.rs` 为样本，按语句/结构块拆分可“机械翻译”的部分与需重新设计的部分。
- 将结果反向映射到 Rust 端，判断当前写法是否必须依赖特定语言特性，若非必要则列入“迁移友好”改造清单。
- 输出双侧协同计划：C# 端保留语义一致的结构，Rust 端主动调整以便复用现有测试资产。

## 2. 语句与结构分类概览

| 分类 | 典型示例 | 对 C# 的处理 | Rust 端是否需改写 | 说明 |
|------|----------|--------------|--------------------|------|
| 基础语法 | `use`, `const`, 基本 `impl` getter | ✅ 直接语法映射 | 否 | 仅需将 `min`, `Ordering` 映射到 BCL。 |
| 容器操作 | `Vec::push`, `collect`, while/for | ✅ 改写为 `List<T>`/LINQ | 否 | 控制流结构保持一致。 |
| pattern matching | `match`, `if let`, `while let` | ⚠️ 转写为 `switch`/`if` | 可选 | 大多是表达风格，Rust 可留用也可替换。 |
| 枚举携带数据 | `enum NodeVal { Leaf, Internal }` | ⚠️ 需要 `sealed` 层次或标记字段 | ✅ | Rust 可拆成 struct + 标记，减少模式匹配压力。 |
| 关联类型 | `trait NodeInfo { type L; }` | ❌ 静态抽象接口/显式泛型 | ✅ | Rust 可改写为 `trait NodeInfo<L>` 及辅助别名。 |
| 默认实现 | `fn identity() -> Self` | ⚠️ 可转 C# 默认接口实现 | 可选 | 若依赖关联类型建议改为 helper 函数。 |
| COW 接口 | `Arc::make_mut`, `NodeRef` | ❌ 需重写 `SharedNode` | ✅ | Rust 端提供 `ensure_unique()` 等 helper 以屏蔽 `Arc::make_mut`。 |
| 生命周期 | `Cursor<'a>` 直接借用叶片 | ❌ C# 无等价 | ✅ | Rust 可切换到索引/Arc 引用模型，牺牲部分零拷贝。 |
| Iterator trait | `impl Iterator for CursorIter` | ⚠️ `IEnumerable<T>` + `yield` | 可选 | Rust 可保留 iterator，但可补 `collect_*` helper 便于跨语言重用。 |
| 宏断言 | `debug_assert!`, `panic!` | ⚠️ `Debug.Assert`, `throw` | 可选 | Rust 若转为显式 `Result` 更易脚本测试。 |

> ✅：C# 侧几乎可按部就班翻译；⚠️：需轻微语法重写；❌：需要额外设计或 Rust 协同调整。

## 3. Rust 端迁移友好改造建议

| 主题 | 建议改造 | 预期收益 | 验证方式 |
|------|----------|----------|----------|
| 关联类型退场 | 将 `NodeInfo`, `Metric`, `Leaf` 等 trait 改为显式泛型或组合接口；提供 `type RopeNode = Node<RopeInfo, RopeLeaf>` 别名以兼容现有代码 | 与 C# 泛型约束对齐，脚本化生成骨架 | `cargo test -p xi-rope` 全量通过；查无 orphan impl | 
| COW helper 抽象 | 在 `NodeBody`/`Arc` 外包裹 `SharedNode` 并提供 `ensure_unique`, `clone_with_children` API | C# 端可按函数签名模拟，避免直接翻译 `Arc::make_mut` | Rope 单测验证编辑后节点引用计数稳定 |
| NodeVal 扁平化 | 用 `struct NodeBody { kind: NodeKind, leaf: RopeLeaf, children: SmallVec }` + 辅助方法替代 match-heavy 枚举 | 减少模式匹配差异，便于 C# `class` 表达 | `cargo fmt` + `cargo test`，审查生成代码差异 |
| Cursor 抽象 | 将 `'a` 借用模式替换为节点索引 + `Arc<Node>` 捕获；提供 `CursorCache` 结构说明 | C# 无需实现生命周期可直接共享逻辑 | Cursor 相关单测 (`cursor_next_prev`) 行为保持一致 |
| 迭代器封装 | 在 Rust 端提供 `collect_boundaries(metric)` 等纯函数 | C# 可以直接调用同名 helper，简化 iterator 翻译 | 复用现有 `Iterator` 测试，新增 helper 覆盖 |
| 测试模块外置 | 将 `#[cfg(test)]` 模块拆入 `tests/` 目录或公共 helper | 便于提取共享测试数据，双端同步执行 | `cargo test` 结构调整后仍全绿 |

## 4. 双向协同计划

1. **冻结核心范围**：Rust 侧聚焦 `xi-rope`、`xi-core-lib`；C# 侧集中在 `Xi.Core.Rope`，暂缓视图/插件等模块。
2. **建立骨架对照**：使用脚本从 Rust 生成精简骨架（已输出 `docs/reference/rust-skeleton.md`），确保每次 Rust 改造后可自动刷新；C# 侧维护同步 skeleton 并标注差异。
3. **同步 Helper 契约**：先在 Rust 端落地 `SharedNode::ensure_unique()`、`LeafOps::split_at()` 等 helper，再在 C# 端实现等价静态方法，保持命名一致。
4. **迭代式回放**：每完成一轮 Rust 改造，立即在 C# 侧更新骨架或实现；C# 端新增测试需在 Rust 仓库寻找等价覆盖，反之亦然。
5. **双端回归基线**：
	- Rust：`cargo test --workspace`（尤其 `xi-rope`）。
	- C#：`dotnet test Xi.Editor.sln`，验证 81 项 Rope 测试保持通过。
6. **脚本化对齐**：在 `scripts/` 增加对照脚本，输出“Rust helper 列表 vs C# 实际实现”差异并写入 CI 报告。

## 5. 近期行动（更新 2025-11-13）

### 5.1 进展快照
- Rust 端已完成 `NodeInfo`、`TreeBuilder`、`Delta` 及依赖模块的显式叶泛型化，`cargo test -p xi-rope`（149 项）全部通过，现行实现以 `Node<RopeInfo, String>` 等别名维持兼容。
- `scripts/refresh_skeleton_docs.py` 已刷新 `docs/skeleton/rope.md`，C# 骨架仍以实验版泛型节点为准，尚未迁入主实现。
- C# 与文档侧对 `Cursor<'a>` 生命周期、`Arc::make_mut` 相关 helper 的映射尚无定案，阻滞后续双向移植。

### 5.2 差距与短期计划
1. **Node 泛型双向同步**：在 `node-generic-refactor-plan.md` 标注已完成的 Rust 泛型化内容，更新 C# 迁移任务清单，并将 `Node<TInfo, TLeaf, TLeafOps>` 包装层接入主实现后串联 81 项 Rope 测试。
2. **Cursor 生命周期削薄预研**：梳理 `Cursor<'a, N, L>` 生命周期依赖，评估以节点索引 + 共享指针实现的方案，输出设计权衡与最小 POC 验证路径。
3. **SharedNode/COW Helper 抽象**：归纳 `Arc::make_mut` 触点并设计语言无关的 helper 契约，为 C# 端静态 helper 提供对照实现与测试要求。
4. **文档与骨架对齐**：在上述调整完成后，刷新 `docs/skeleton/rope.md`、`docs/skeleton/xi.Core.Rope.cs` 与 `rope-port-mapping.md` 对应段落，确保 helper 名称与泛型签名的一致性，并将进展同步至 `AGENTS.md`。

## 6. 风险与监控

| 风险 | 描述 | 监控与缓解 |
|------|------|------------|
| Rust 改造破坏上游兼容 | `xi-editor-ph7` 仍需编译/运行 | 每次重构后运行全量测试；对外合入时保留 feature flag / 版本分支 |
| C# 端实现偏离 | 未及时吸收 Rust 改造，导致分叉 | 建立每周对照清单；必要时生成“缺口报告”放入 `AGENTS.md` |
| 双端语义不一致 | Helper 名称/参数不匹配 | 所有 helper/方法命名统一在 `rope-port-mapping.md` 维护，改动需同步 PR |
| 测试资产难以共享 | Rust/C# 输出格式差异大 | 引入中立格式（JSON/Markdown）存储 golden data，并提供转换脚本 |

---

*维护人：GitHub Copilot Agent（2025-11-13 更新）*
