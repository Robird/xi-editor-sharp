**Cursor Cache 重构方案（修订版）**

## 背景
`Cursor Cache` 重构隶属于 `Xi.Editor` 移植蓝图的 M2/M3 游标生命周期工作包，需与 `docs/architecture/port-blueprint.md`（尤其 §5.2、§9）以及 `docs/architecture/rope-port-mapping.md` 的映射表保持同步。
- `port-blueprint` 将 `NodeCursor` 拓展视作 Rust/C# 协同交付，当前标记为等待 Rust 端 Descriptor/State 能力解锁。
- `rope-port-mapping` 将 `Tree/NodeCursor.cs` 标记为“仅骨架”，明确依赖 Rust 侧游标缓存去生命周期化后才能推进。
- 现有 Rust `Cursor<'a, N, L>`（`xi-editor-ph7/rust/rope/src/tree.rs:788-1188`）依赖生命周期缓存 `[Option<(&'a Node, usize)>; 4]` 与 `Option<&'a L>`，这是 C# 难以直接复刻的核心阻塞，也是上述两份文档列出的主路径风险。

## 阶段进度追踪
| Phase | Deliverable | Status | Dependencies | Next Checkpoint |
|-------|-------------|--------|--------------|-----------------|
| Phase 0 | 游标调用基线与缓存诊断报告 | 进行中 | `port-blueprint` §5.2（诊断基线） | 将命中率指标回填到 `port-blueprint` 附录 |
| Phase 1 | `CursorDescriptor` API 与往返测试 | 已完成 | Rust `tree.rs`、`rope-port-mapping` 状态列 | 推动 `rope-port-mapping` 将 `Tree/NodeCursor.cs` 标记为“实现中”并准备 Phase 2 `CursorState` 草案 |
| Phase 2 | `CursorState` 内核与 Feature Gate | 进行中 | Phase 1 稳定报告、`port-blueprint` §9 | 在启用/禁用模式下确认语义一致性，按需通过轻量 instrumentation 观察热点路径，无需额外基准 |
| Phase 3 | C# `NodeCursor` 落地与共享夹具 | 规划中 | Phase 2 主分支可用、`rope-port-mapping` C# 行 | 宣告 `Tree/NodeCursor.cs` 从“仅骨架”晋级到“实现中/已实现” 并刷新 `AGENTS.md` |

> **2025-11-15 更新**：`xi-rope` 新增可选 `cursor_state` feature gate，提供借用-free `CursorState` (`Cursor::state()`, `CursorState::from_cursor`/`restore`/`to_descriptor`) 并在启用时保持与 `Cursor` 同步；未启用时沿用原缓存路径。新增 `cursor_state_round_trip_basic`、`cursor_state_handles_deep_paths`、`cursor_state_invalidates_after_edit` 及 `cursor_state_preserves_navigation_*`（Base/Lines/Utf16）系列测试，确保在默认/feature 模式下均可往返、穿越深树并在多 Metric 场景维持语义一致。

## 问题现状
- 游标缓存使用固定长度数组保存自底向上的父链，命中时可零分配前进/后退；树高度超过缓存时会自动回退到 `descend`，行为正确但有额外扫描成本。
- Copy-on-write 编辑通过 `Arc<NodeBody>` 替换子树；借用语义确保游标在编辑后要么重新定位、要么被标记为无效（`leaf = None`）。
- 上层模块（`find.rs`、`compare.rs`、`spans.rs`、`breaks.rs`、`core-lib` 中的 `movement`/`word_boundaries` 等）大量依赖游标迭代并假设构造/复位无分配。
- C# `NodeCursor` 目前仅有骨架 (`src/xi.Core/Rope/Tree/NodeCursor.cs`)，缺少缓存结构与状态恢复入口；如果直接维护节点引用，将缺乏 Rust 生命周期提供的并发/一致性保障。

## 设计目标
1. **跨语言一致性**：保持现有游标 API 语义（`get_leaf`、`next_leaf`、`prev_leaf`、Metric 驱动的 `prev`/`next`）。
2. **搬运友好**：提供无生命周期、可序列化的缓存描述（Descriptor），使 C# 能重建游标而不依赖堆栈上借用。
3. **性能守恒**：保持热门路径零分配；新能力需 opt-in，不影响现有 Rust 调用者。
4. **安全恢复**：在结构发生写时复制后，应能检测并拒绝过期缓存，避免恢复到错误的节点。
5. **文档可追踪**：关键改动需同步到 `port-blueprint`、`rope-port-mapping`，并在 `AGENTS.md` 登记跨会话进度。

## 方案对比

| 方案 | 描述 | 优点 | 缺点 |
|------|------|------|------|
| A. 仅提供 `CursorDescriptor` | 保留现有 `Cursor<'a>`，新增 `to_descriptor` / `apply_descriptor`，在需要时克隆路径 `Arc` | 对现有调用者零侵入；便于 C# `NodeCursor` 存储共享节点 | Descriptor 仍需回落到借用 `Cursor` 才能工作，Rust 仍背负生命周期实现，后续迭代器很难改写 |
| B. 新增借用-free `CursorState` 并以其为默认实现 | 构建拥有路径 `Arc` + 子索引的状态对象，`Cursor<'a>` 仅作轻量适配层 | 彻底消除生命周期依赖，后续 Rust/C# 共享逻辑可直接基于 `CursorState` | 若一步切换为默认实现，风险较大，需要足够测试与性能验证 |
| C. **组合策略（推荐）** | 第一阶段先引入 Descriptor（A），并实现内部 `CursorState` 原型；验证稳定后将游标内部改写为 `CursorState`，保留原 API 适配（B） | 渐进式迁移，Rust 调用者可按需试用 Descriptor；验证通过后统一到借用-free 状态以支撑 C# | 需要维护双实现过渡期；必须建设共享测试确保行为一致 |

## 推荐路线（组合策略）
1. **Phase 0 – 基线与诊断**
	- 审核现有游标调用点（`list_code_usages` 结果约 95 处）并按模块归档，确认必须保持零分配的路径。
	- 如需记录缓存命中率、重新下钻次数，可为 `Cursor` 增加调试守卫（命令行 feature），数据仅用于迁移过程中的可移植性验证，无需构建长期性能基线。

	**跨文档对齐**
	- 在 `docs/architecture/port-blueprint.md` §5.2 记录缓存命中率基线，标记“Cursor 生命周期诊断”为“已完成”。
	- 在 `docs/architecture/rope-port-mapping.md` 中保留 `Tree/NodeCursor.cs` 为“仅骨架”，附注“Phase 0 基线完成，等待 Descriptor 支持”。

2. **Phase 1 – Descriptor API**
	- 在 Rust `tree.rs` 中新增 `CursorDescriptor<N, L>`（持有 `position`、`offset_of_leaf`、有效位、`SmallVec<PathFrame>`，每个 `PathFrame` 包含 `Arc<NodeBody<N, L>>`、子索引、子偏移；可选叶节点 `Arc<L>`）。
	- 实现 `Cursor::to_descriptor(&self) -> CursorDescriptor` 与 `CursorDescriptor::restore(&self, root: &Node<N, L>) -> Option<Cursor>`，使用 `Arc::ptr_eq` 验证路径，失败时返回 `None`。
	- 添加 `Cursor::apply_descriptor(&mut self, &CursorDescriptor) -> bool`，在成功时重建缓存并保持零分配；否则回退到 `descend`。
	- 在 `cursor_next_triangle` 等测试中增加 round-trip 验证，构建 `tests/cursor_descriptor.rs` 聚焦深树、多 Metric、编辑后失效场景。
	- **当前成果（2025-11-15）**：`CursorDescriptor`, `PathFrame` 已合入 `tree.rs`，并配套 `xi-editor-ph7/rust/rope/tests/cursor_descriptor.rs` 完成 round-trip、失效检测与超过缓存深度的覆盖；`Cursor::apply_descriptor`/`restore` 存在零拷贝成功路径，验证失败时保持游标状态不变。
	- 文档更新：`Cursor` Rustdoc、`docs/architecture/rope-port-mapping.md` 状态栏转为“Rust 重构中 → 实施中”，`AGENTS.md` 记录阶段成果。

	**跨文档对齐**
	- Phase 1 完成后，在 `docs/architecture/port-blueprint.md` §5.2/§9 将 Descriptor 能力状态从“规划中”推进为“已完成”，并链接往返测试清单。
	- 将 `docs/architecture/rope-port-mapping.md` 中 `Tree/NodeCursor.cs` 状态升级为“实现中”，记录 Descriptor JSON 夹具路径。

3. **Phase 2 – CursorState 内核**
	- 引入内部 `CursorState<N, L>`（拥有根 `Arc<NodeBody>`，记录 `position`、`offset_of_leaf`、路径 `SmallVec<PathFrame>`、`leaf_handle`）。
	- 将 `Cursor<'a>` 作为轻量 wrapper：持有 `&'a Node` 与 `CursorState` 的借用视图；原方法转调 `CursorState`。
	- 替换 `next_leaf`/`prev_leaf`/`descend_metric` 等内部方法为状态版本；确认 `CURSOR_CACHE_SIZE` 仍可固定为 4，路径超过 4 时 `CursorState` 自动保留完整 `SmallVec`（防止 Descriptor 信息不足）。
	- 新增 `#[cfg(feature = "cursor_state")]` gate，初期通过 feature flag 聚焦语义与移植验证；在确认行为与现有实现一致后，再评估是否切换默认开关。
	- 将核心模块（`find.rs`, `compare.rs`, `spans.rs`, `breaks.rs`, `core-lib` 游标调用）引导到 `CursorState` API，确保后续可直接镜像至 C#；如需观察热点，可临时开启 instrumentation，而非构建专门基准。

	**跨文档对齐**
	- 在 `docs/architecture/port-blueprint.md` §9 更新“CursorState 内核”任务的状态，从“规划中”提升为“进行中/已完成”，同时标注 C# 依赖解除时间点。
	- 将 `docs/architecture/rope-port-mapping.md` 中 Rust 侧游标条目从“Rust 重构中”推进到“已实现”，并记录 feature gate 退出标准。

4. **Phase 3 – C# NodeCursor 落地**
	- 在 C# `NodeCursor` 中引入 `PathFrame` 结构（`SharedNode`, `ChildIndex`, `OffsetAtParent`）与 `LeafHandle`（指向 `SharedNode` 的叶子包装器）。
	- 支持 `NodeCursorDescriptor` 与 Rust 同名 JSON 表达（便于调试/测试）。
	- 实现恢复流程：基于 `SharedNode` 引用比较（`ReferenceEquals`）或 rope epoch（`Rope.Version`）判断是否需要回退。
	- 将 `MoveToNext`/`MoveToPrevious` 等方法改写为状态机，与 Rust `CursorState` 共用算法（初期可直接翻译到 C#，后续考虑共享测试脚本）。
	- 为 `xi.Core.Tests` 添加游标回归测试：
	  - 深度树遍历（构造 8+ 层树，验证缓存超出情况）。
	  - Delta 应用后缓存失效检测（使用 Stage A/B/C 序列化夹具生成的编辑序列）。
	  - 与 Rust `cursor_descriptor_roundtrip` 测试共享 JSON fixture，确保路径重建一致。

	**跨文档对齐**
	- 在 `docs/architecture/port-blueprint.md` §5.2/§9 更新 M3 游标交付状态为“已实现”，并链接 C# 端回归测试结果。
	- 将 `docs/architecture/rope-port-mapping.md` 中 `Tree/NodeCursor.cs` 状态从“实现中”最终标记为“已实现”，新增 Descriptor/State 夹具引用与测试清单。

## Rust 实施细节
- `CursorDescriptor` 的 `PathFrame` 需同时存储 `child_offset`，避免恢复时重新累计长度。
- `Cursor::descend()` 改造为可接受 `PathFrame` 迭代器，使恢复流程与首次定位复用代码。
- 通过 `SmallVec<[PathFrame; CURSOR_CACHE_SIZE]>` 默认容纳 4 层缓存；超过时自动分配堆内存，但即便在最坏情况下也仅复制深度条目。
- 提供 `descriptor.depth()` 与 `descriptor.valid()` 方法，方便 C# 端调试与日志。
- 性能观察策略：保持编译与现有测试即可验证功能；若需了解热点路径，可通过可选 instrumentation（临时计数器、调试日志）收集粗粒度数据，采集后立即移除，避免引入额外依赖或跨语言难以复刻的基准。

## C# 实施细节
- `NodeCursor` 需依赖 `SharedNode`/`Node.CloneWithChildren` 已完成的写时复制封装（参见 `docs/architecture/port-blueprint.md` §5.3）。
- 推荐引入内部结构：
  ```csharp
  internal readonly record struct PathFrame(SharedNode Node, int ChildIndex, int OffsetOfChild);
  internal sealed class LeafHandle { public SharedNode Node; public string Leaf; public int Offset; }
  ```
- `NodeCursorDescriptor` 可实现 `IEquatable<>`，序列化字段与 Rust 保持同名（`position`, `offset_of_leaf`, `frames`, `leaf`）。
- Rope 需维护 `uint Version`（每次结构写时复制自增），`NodeCursor` 恢复时检查 `descriptor.RootVersion == rope.Version`。
- Metric 辅助：将 `IMetric` 扩展为 `bool CanFragment`/`bool IsBoundary(string leaf, int leafOffset)`，配合 `StringLeafOperations` 与 `BreaksMetricHelper` 的现有 helper。

## 验证策略
- **双端测试**：Rust 侧覆盖 `cursor_descriptor_roundtrip`、`cursor_state_randomized`、`cursor_state_preserves_navigation_*`（Base/Lines/Utf16）；C# 添加镜像 `NodeCursorDescriptorTests`、`NodeCursorTraversalTests`。
- **随机编辑序列**：复用现有 Rope 随机测试框架（81 项）生成编辑操作，序列化为共享 JSON，在 Rust/C# 之间交叉验证。
- **性能监控**：通过 opt-in 的调试计数器或日志临时记录 `Cursor::to_descriptor`/`apply_descriptor` 的 `Arc` 克隆次数、缓存命中率；验证完成后移除 instrumentation，维持编译与单元测试作为主要验证手段。
- **文档同步**：完成阶段后刷新 `docs/skeleton/rope.md`、`docs/skeleton/xi.Core.Rope.cs`，并在 `AGENTS.md` “当前聚焦事项”/“下一步行动”更新状态。

## 风险与缓解
- **过期描述符导致错误恢复**：通过 `Arc::ptr_eq` + rope 版本号验证，并在失败时强制 `descend`。同时在 C# 端对 `SharedNode` 引用做引用相等检查。
- **`Arc` 克隆导致内存保持时间延长**：Descriptor 需文档化“短期持有”预期；提供 `drop_leaf()` 帮助器或调试计数以分析泄漏。
- **性能回退**：保持 Descriptor API 为 opt-in，并在默认路径继续使用原缓存；如需评估趋势，使用短期 instrumentation 收集指标即可；待 `CursorState` 稳定后再考虑调整默认实现。
- **双实现漂移**：共享测试 + skeleton 文档 + `runSubAgent` 自动对照（计划中）防止 Rust/C# 行为偏差。

## 下一步行动
1. Rust：实现 `CursorDescriptor` 与基础测试（Phase 1），在 `rope-port-mapping.md` 将游标条目标记为“实现中”。
2. Rust：使用 feature flag 引入 `CursorState` 草案，优先确认语义对齐；如需观测热点，采用轻量 instrumentation，而非单独的性能测试流程（Phase 2）。
3. C#：扩展 `NodeCursor` 数据结构以接收 Descriptor，编写最小 round-trip 测试。
4. 文档：更新 `cursor-lifetime-refactor.md` 说明组合策略，并在 `AGENTS.md` 记录阶段性里程碑。
5. 工具链：考虑在 `scripts/refresh_skeleton_docs.py` 中追加游标相关结构的自动摘要，保持跨语言骨架同步。

## 达成标准
- Descriptor API 已在 Rust 主分支落地并通过 cursor 相关测试；`CursorState` feature 在 CI 中启用试跑，与默认实现语义一致且全部单元测试通过。
- C# `NodeCursor` 能够在 81 项 Rope 测试基础上新增游标回归用例并全部通过。
- `docs/architecture/rope-port-mapping.md`、`docs/architecture/port-blueprint.md` 与 `AGENTS.md` 均同步描述新的游标策略。
- 双端共享的 Descriptor 示例（JSON）可 round-trip 并被测试覆盖，确保跨语言恢复行为一致。

## 文档与协同更新
- [ ] Phase 0：在 `docs/architecture/port-blueprint.md` §5.2 回填缓存命中率基线，同时在 `docs/architecture/rope-port-mapping.md` 备注“Phase 0 基线完成，等待 Descriptor 支持”。
- [x] Phase 1：更新 `docs/architecture/port-blueprint.md` §5.2/§9 的 Descriptor 任务为“已完成”，并将 `docs/architecture/rope-port-mapping.md` 中 `Tree/NodeCursor.cs` 调整为“实现中”；同步在 `AGENTS.md` 记录往返测试链接（新增 `xi-editor-ph7/rust/rope/tests/cursor_descriptor.rs`）。
- [ ] Phase 2：`cursor_state` feature gate 已上线（Rust 侧进行中），待语义与测试对齐确认后，再将 `docs/architecture/port-blueprint.md` §9 与 `docs/architecture/rope-port-mapping.md` 标记为“已完成”；若需要补充数据，可记录一次轻量 instrumentation 结果并在 `AGENTS.md` 留存。 
- [ ] Phase 3：把 `docs/architecture/port-blueprint.md` M3 游标里程碑与 `docs/architecture/rope-port-mapping.md` C# 映射表同时标记为“已实现”，并在 `AGENTS.md` 与 `docs/skeleton/rope.md` 发布最终实现说明。
