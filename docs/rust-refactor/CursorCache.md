**Cursor Cache 重构方案（修订版）**

## 背景
- `Xi.Editor` C# 移植在阶段 M2 需要补齐 `NodeCursor` 与 `find`/`movement` 等遍历管线（参见 `docs/architecture/port-blueprint.md` §5.2、§9）。
- `docs/architecture/rope-port-mapping.md` 将 `tree.rs` 游标列为“仅骨架”，等待 Rust 端提供无生命周期的缓存策略以便 C# 对齐。
- 现有 Rust `Cursor<'a, N, L>`（`xi-editor-ph7/rust/rope/src/tree.rs:788-1188`）依赖生命周期缓存 `[Option<(&'a Node, usize)>; 4]` 与 `Option<&'a L>`，这是 C# 难以直接复刻的核心阻塞。

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
	- 为 `Cursor` 增加调试守卫（命令行 feature）记录缓存命中率、重新下钻次数，为后续性能比对提供基线。

2. **Phase 1 – Descriptor API**
	- 在 Rust `tree.rs` 中新增 `CursorDescriptor<N, L>`（持有 `position`、`offset_of_leaf`、有效位、`SmallVec<PathFrame>`，每个 `PathFrame` 包含 `Arc<NodeBody<N, L>>`、子索引、子偏移；可选叶节点 `Arc<L>`）。
	- 实现 `Cursor::to_descriptor(&self) -> CursorDescriptor` 与 `CursorDescriptor::restore(&self, root: &Node<N, L>) -> Option<Cursor>`，使用 `Arc::ptr_eq` 验证路径，失败时返回 `None`。
	- 添加 `Cursor::apply_descriptor(&mut self, &CursorDescriptor) -> bool`，在成功时重建缓存并保持零分配；否则回退到 `descend`。
	- 在 `cursor_next_triangle` 等测试中增加 round-trip 验证，构建 `tests/cursor_descriptor.rs` 聚焦深树、多 Metric、编辑后失效场景。
	- 文档更新：`Cursor` Rustdoc、`docs/architecture/rope-port-mapping.md` 状态栏转为“Rust 重构中 → 实施中”，`AGENTS.md` 记录阶段成果。

3. **Phase 2 – CursorState 内核**
	- 引入内部 `CursorState<N, L>`（拥有根 `Arc<NodeBody>`，记录 `position`、`offset_of_leaf`、路径 `SmallVec<PathFrame>`、`leaf_handle`）。
	- 将 `Cursor<'a>` 作为轻量 wrapper：持有 `&'a Node` 与 `CursorState` 的借用视图；原方法转调 `CursorState`。
	- 替换 `next_leaf`/`prev_leaf`/`descend_metric` 等内部方法为状态版本；确认 `CURSOR_CACHE_SIZE` 仍可固定为 4，路径超过 4 时 `CursorState` 自动保留完整 `SmallVec`（防止 Descriptor 信息不足）。
	- 新增 `#[cfg(feature = "cursor_state")]` gate，初期通过 feature flag 供测试验证；性能稳定后默认启用并保留旧实现 behind feature fallback。
	- 将核心模块（`find.rs`, `compare.rs`, `spans.rs`, `breaks.rs`, `core-lib` 游标调用）引导到 `CursorState` API，确保后续可直接镜像至 C#。

4. **Phase 3 – C# NodeCursor 落地**
	- 在 C# `NodeCursor` 中引入 `PathFrame` 结构（`SharedNode`, `ChildIndex`, `OffsetAtParent`）与 `LeafHandle`（指向 `SharedNode` 的叶子包装器）。
	- 支持 `NodeCursorDescriptor` 与 Rust 同名 JSON 表达（便于调试/测试）。
	- 实现恢复流程：基于 `SharedNode` 引用比较（`ReferenceEquals`）或 rope epoch（`Rope.Version`）判断是否需要回退。
	- 将 `MoveToNext`/`MoveToPrevious` 等方法改写为状态机，与 Rust `CursorState` 共用算法（初期可直接翻译到 C#，后续考虑共享测试脚本）。
	- 为 `xi.Core.Tests` 添加游标回归测试：
	  - 深度树遍历（构造 8+ 层树，验证缓存超出情况）。
	  - Delta 应用后缓存失效检测（使用 Stage A/B/C 序列化夹具生成的编辑序列）。
	  - 与 Rust `cursor_descriptor_roundtrip` 测试共享 JSON fixture，确保路径重建一致。

## Rust 实施细节
- `CursorDescriptor` 的 `PathFrame` 需同时存储 `child_offset`，避免恢复时重新累计长度。
- `Cursor::descend()` 改造为可接受 `PathFrame` 迭代器，使恢复流程与首次定位复用代码。
- 通过 `SmallVec<[PathFrame; CURSOR_CACHE_SIZE]>` 默认容纳 4 层缓存；超过时自动分配堆内存，但即便在最坏情况下也仅复制深度条目。
- 提供 `descriptor.depth()` 与 `descriptor.valid()` 方法，方便 C# 端调试与日志。
- 性能验证建议：
  - `cargo bench -p xi-rope -- benches::cursor_scan`（新增）比较启用/禁用 descriptor 恢复的耗时。
  - 在 `rope::find` 快速扫描中加入可选统计（`debug_assert!(state.cache_hits >= ...)`）验证命中率。

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
- **双端测试**：Rust 加入 `cursor_descriptor_roundtrip`、`cursor_state_randomized`；C# 添加镜像 `NodeCursorDescriptorTests`、`NodeCursorTraversalTests`。
- **随机编辑序列**：复用现有 Rope 随机测试框架（81 项）生成编辑操作，序列化为共享 JSON，在 Rust/C# 之间交叉验证。
- **性能监控**：收集 `Cursor::to_descriptor` 与 `apply_descriptor` 的 `Arc` 克隆次数、缓存命中率（调试日志或 `metrics` feature）。
- **文档同步**：完成阶段后刷新 `docs/skeleton/rope.md`、`docs/skeleton/xi.Core.Rope.cs`，并在 `AGENTS.md` “当前聚焦事项”/“下一步行动”更新状态。

## 风险与缓解
- **过期描述符导致错误恢复**：通过 `Arc::ptr_eq` + rope 版本号验证，并在失败时强制 `descend`。同时在 C# 端对 `SharedNode` 引用做引用相等检查。
- **`Arc` 克隆导致内存保持时间延长**：Descriptor 需文档化“短期持有”预期；提供 `drop_leaf()` 帮助器或调试计数以分析泄漏。
- **性能回退**：保持 Descriptor API 为 opt-in，并在默认路径继续使用原缓存；待 `CursorState` 稳定后再切换默认实现。
- **双实现漂移**：共享测试 + skeleton 文档 + `runSubAgent` 自动对照（计划中）防止 Rust/C# 行为偏差。

## 下一步行动
1. Rust：实现 `CursorDescriptor` 与基础测试（Phase 1），在 `rope-port-mapping.md` 将游标条目标记为“实现中”。
2. Rust：使用 feature flag 引入 `CursorState` 草案，并邀请性能测试验证（Phase 2）。
3. C#：扩展 `NodeCursor` 数据结构以接收 Descriptor，编写最小 round-trip 测试。
4. 文档：更新 `cursor-lifetime-refactor.md` 说明组合策略，并在 `AGENTS.md` 记录阶段性里程碑。
5. 工具链：考虑在 `scripts/refresh_skeleton_docs.py` 中追加游标相关结构的自动摘要，保持跨语言骨架同步。

## 达成标准
- Descriptor API 已在 Rust 主分支落地并通过 cursor 相关测试；`CursorState` feature 在 CI 中启用试跑，无性能回退超 5%。
- C# `NodeCursor` 能够在 81 项 Rope 测试基础上新增游标回归用例并全部通过。
- `docs/architecture/rope-port-mapping.md`、`docs/architecture/port-blueprint.md` 与 `AGENTS.md` 均同步描述新的游标策略。
- 双端共享的 Descriptor 示例（JSON）可 round-trip 并被测试覆盖，确保跨语言恢复行为一致。
