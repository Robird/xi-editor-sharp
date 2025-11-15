# 类型系统移植阻塞追踪

> 共享缓冲：记录 C#/Rust 双端协同梳理类型系统骨架过程中发现的阻塞项，以及对应的解法思路、降级策略与当前进度。所有参与者（人类、AI 主代理与 SubAgent）均可在此文档中增量记录结论。

## 任务约定
- 按阻塞点开设顶级小节，每项至少包含 Rust 侧缓解思路、C# 侧实现路径与可接受的降级策略三栏。
- 若阻塞解决，保留条目并在标题中标注状态（例如 ✅ 已解决）。
- 重要参考资料、设计分歧与测试资产需同步链接至相关文档。
- 每轮调研或方案迭代后附上“最新进展”子章节便于溯源。

## 待办清单快照
- [ ] 初始化阻塞列表（游标生命周期、泛型节点、Metric 互操作、Chunk 迭代器等）。
- [ ] 针对每个阻塞点，明确 Rust 改造预期与 C# 侧落地策略。
- [ ] 判定需要降级实现的范围并记录监测指标。

---

## 游标生命周期约束

### Rust 侧策略
- `xi-editor-ph7/rust/rope/src/tree.rs` 中的 `Cursor<'a, N, L>` 仍以 `Option<(&'a L, usize)>` 缓存叶节点并用 `SmallVec` 维护父链，依赖借用生命周期保障缓存正确性。
- `Cursor::to_descriptor`/`apply_descriptor` 无需 feature gate，提供拥有型 `CursorDescriptor`，这是跨语言移植的必备能力；`#[cfg(feature = "cursor_state")]` 额外引入 `CursorState`/`Cursor::state()` 等 API 以便持久化缓存，但该 gate 默认关闭，主要服务调试或未来性能实验。
- Descriptor 往返测试已存在，而 `cursor_state` 在启用时通过专门测试验证语义一致；跨语言 parity 仍需补充 descriptor 级别的共享夹具。

### C# 侧策略
- `src/xi.Core/Rope/Tree/NodeCursor.cs` 仍是占位实现，尚未复刻 Rust 的路径缓存与借用语义；需基于 `SharedNode`/`Node.Generic` 构建拥有型状态并导出 C# 版 `CursorDescriptor`。
- 结合 `docs/csharp-refactor/node-generic-refactor-plan.md`，Cursor 实现应直接消费泛型节点，利用 `ValueListBuilder<int>` 或池化数组维护父链，并与 `StringLeafOperations` 协调叶片视图。
- 测试计划：在 `tests/xi.Core.Tests` 新增 `CursorMetricParityTests`，对齐 Rust 端 `Lines`/`Base`/`Utf16` 的 `next/prev` 行为并验证 descriptor 恢复。

### 降级方案
- 游标落地之前，C# 高层 API 继续以 `Node.TraverseLeaves()` + metric helper 实现导航，必要时退化为 `Rope.Snapshot()` + `string` 扫描，牺牲性能换取正确性。
- 对外暂不暴露 Cursor 接口，提供 `Rope` 封装的便捷方法（如 `Convert*`、`GetSlice`）满足现有需求；调试场景可临时公开 `GetLeafSegments()` 返回 `List<(string Leaf, int Offset)>`。

### 最新进展
- Rust 侧 `CursorDescriptor` 已稳定可用，C# 需要基于该能力实现自身的拥有型游标；`cursor_state` feature gate 作为可选增强，不要求在移植第一阶段同步实现。
- 需评估 C# Cursor 复用 `SharedNode` 后的 GC 压力与缓存命中率，建议在实现阶段引入 instrumentation 并回写 `AGENTS.md`。
- Open question: 是否需要在 Cursor 状态中记录版本号以检测编辑后的失效？Rust 端依赖 `Arc::ptr_eq`，C# 需确认等效策略；如未来复刻 `CursorState`，仍可延续版本号/指针比较混合方案。

## Metric 泛型与 Node 编辑互通

### Rust 侧策略
- `xi-editor-ph7/rust/rope/src/tree.rs` 继续以 `DefaultMetricProvider`/`Metric` trait 约束泛型 `Node<N, L>`，核心入口是 `Node::convert_metrics`/`convert_metrics_full` 与 `Node::edit`，均以静态分发连接 `Into<Node>`，目前尚无非泛型导出。
- `xi-editor-ph7/rust/rope/src/rope.rs` 新增的 `convert_lines_from_bytes`、`convert_bytes_from_lines`、`convert_utf16_from_bytes`、`convert_bytes_from_utf16` 四个 shim（404-452 行）直接包装上述泛型实现并始终编译（未绑定 `portability_shims`），向绑定语言暴露 `usize` 计量互换。
- 仍缺失的部分：无 `edit_*` 级别的 shim，`Breaks` 度量转换也未对外导出；文档中提到的 `portability_shims` feature 尚未真正落地，需在后续补齐或调整描述以免混淆。

### C# 侧策略
- `src/xi.Core/Rope/Tree/Node.cs` 目前通过 `IMetric` 接口的动态分派实现 `ConvertFromDefaultMetric`/`ConvertToDefaultMetric`，遍历所有子节点手动累计指标，逻辑与 Rust 重复且存在性能/一致性风险。
- `Tree/Node.Generic.cs` 的泛型骨架与 `Tree/TreeContracts.cs` 的静态抽象（`IDefaultMetricProvider`, `ITreeMetric`）尚未接入主实现，`Rope.cs` 仍依赖字符串特化路径；`Node.Generic` 仅在 `GenericNodeSmokeTests` 中使用。
- 短期方案可继续依赖动态 API 或调用 Rust 暴露的 `convert_*` shim；中期需引入 `MetricAdapter`/泛型节点别名，将静态抽象接入生产路径，并评估是否需要调用 Rust 侧 helper 以避免双端漂移。

### 降级方案
- 当前可继续依赖 `IMetric` 接口 + 树遍历实现，`tests/xi.Core.Tests/RopeMetricInteropTests.cs` 已覆盖行数/UTF-16 对拍；如需性能缓解，可在 C# 中直接 P/Invoke Rust shim 作为临时方案。
- 字符串特化的 `Node.Replace`/`Rope.Replace` 暂不泛型化，待 Rust 提供 `edit_*` shim 或 C# 泛型节点成熟后再统一改造。

### 最新进展
- Rust 侧四个 `convert_*` shim 已合入并可直接消费，C# 现有测试间接验证其正确性；`Node::edit`/`Breaks` 仍待 Shim 化，需在文档中保持显式 TODO。
- 待办：在泛型切换前补充基准或性能守护，确认静态调用收益；同时规划 `Breaks` shim 与 `edit_*` helper 的对接策略。
- Open question: 泛型节点合入后是否保留 `IMetric` 作为扩展点？需在切换前明确，并同步更新 `docs/architecture/rope-port-mapping.md` 与 C# 规划；另需确认未来是否启用 `portability_shims` feature gate。
- **M3 阶段策略（2025-11-16 会议决策）**：NodeCursor 基于字符串特化 `Node.cs` 实现，通过 `IMetric` 接口访问度量；泛型节点接口验证完成（`TreeBuilder.Generic.cs` + 8 项测试）但不接入主实现。
- **M4 切换计划**：将 TreeBuilder/Delta/Rope 内部调用迁移到 `Node<RopeInfo, string, StringLeafOperations>`，通过 `src/xi.Core/Rope/TypeAliases.cs` 的 `global using RopeNode` 别名统一管理，81 项测试最小化改动后迁移。 

## Chunk/行 迭代器借用差异

### Rust 侧策略
- `xi-editor-ph7/rust/rope/src/rope.rs` 中的 `iter_chunks`/`chunks` 通过 `ChunkIter<'a>` 直接返回借用的 `&'a str`，以 `MAX_LEAF` 为上限，将叶片跨界/换行段拆分后零拷贝交给调用者；`LinesRaw`/`Lines` 复用同一游标机制，区别在于是否保留尾部换行。
- `ChunkIter<'a>`、`LinesRaw<'a>` 等迭代器均绑定在借用生命周期上，依赖 `Cursor<'a, RopeInfo, String>` 的缓存 (`leaf`, `path_cache`) 保持零分配；返回类型为 `Cow<'a, str>` 或 `&'a str`，同时暴露绝对偏移。
- `Cursor::Descriptor` 可为这些迭代器提供拥有型快照，但目前尚未存在专门的 chunk/line descriptor；`docs/rust-refactor/iterator-facade-export.md` 建议新增 façade API（如 owned descriptor/visitor）供绑定语言消费。

### C# 侧策略
- `src/xi.Core/Rope/Rope.cs` 暂无 `Chunk`/`Lines` 方法，上层调用一律退化到 `Snapshot()` 或 `EnumerateLeaves()`，导致 O(n) 字符串复制；`Tree/NodeCursor.cs` 仍是占位，无法提供逐叶遍历支持。
- 需设计 `RopeChunkEnumerator`/`RopeLineEnumerator`（返回 `ReadOnlyMemory<char>` 或轻量 `ChunkView`）并配套 `ChunkDescriptor` 拟合 Rust 的 `(offset, len)` 语义；实现依赖 `NodeCursor` 拥有型快照与 `StringLeafOperations` 的 UTF-16 计量。
- 测试计划：新增 `RopeChunkIteratorTests`/`RopeLineIteratorTests`，引入共享 trace（可参考 `tree_builder_slice_trace` 或未来 façade 输出）验证空文本、跨叶换行、CRLF 等情况，并统计分配次数。

### 降级方案
- 当前继续使用 `Snapshot()` 或 `EnumerateLeaves()` 作为遍历入口，并在文档中标注性能风险；必要时提供 `IEnumerable<string>`/`ReadOnlyMemory<char>` 的包装 API 作为临时替代。
- 分块差异检测等性能敏感场景，可在 C# 侧调用 Rust façade（待提供）或通过临时 P/Invoke 获取 chunk 描述，确保功能可用但接受跨语言调用成本。

### 最新进展
- `iterator-facade-export.md` 给出了 owned descriptor/visitor 风格指引，但尚未有具体实现；需跟进 Rust 侧是否计划导出 `(byte_len, utf16_len)` 元数据或 `visit_chunks` façade。
- Open question: C# 是否采用 `ArrayPool<char>`/`MemoryOwner<char>` 来减少桥接成本？需在原型阶段收集基准后决策。
- 待办：协调 Rust 提供 chunk/line façade 或共享 trace，C# 侧同步规划 `ChunkDescriptor` 结构与测试夹具，避免长期依赖 `Snapshot()`。 

## 字素导航与跨叶上下文

### Rust 侧策略
- `xi-editor-ph7/rust/rope/src/rope.rs` 中的 `prev_grapheme_offset`、`next_grapheme_offset` 通过 `Cursor::prev_grapheme`/`next_grapheme` 调用 `unicode_segmentation` 提供的状态机实现跨叶字素遍历。
- 相关实现依赖 `Cursor` 持有的借用缓存；`docs/rust-refactor/cursor-lifetime-refactor.md` 中的 `CursorState` 计划将该状态封装为拥有型结构，供绑定语言复用。
- Rust 端尚未导出字素级 trace 或简化 helper，无法直接在 C# 中对拍状态转换。

### C# 侧策略
- `docs/architecture/design-divergence-log.md`（2025-11-15）确认暂采降级策略：保证 UTF-16 surrogate 对不拆分，遇到跨叶组合时最多补齐一个相邻叶片，仍不足则退回 code point。
- 需为 `Rope`/`NodeCursor` 设计可插拔 `IGraphemeNavigator`，便于未来导入 ICU4N 或 Rust trace；同时在公共 API 注释中明确当前语义并暴露遥测指标。
- 测试计划：补充 `GraphemeNavigationTests`，结合遥测计数（补片命中次数、code point 回退次数）记录降级触发频率。

### 降级方案
- 在完整字素导航落地前，延续 surrogate 安全 + 单叶/邻叶补片方案；对需要严格 UAX #29 语义的功能保持禁用或回退到 Rust 结果。
- 文档中强调“字素支持暂不完全”，并要求上层调用在遇到失败时提供备用路径或明确错误提示。

### 最新进展
- 设计分歧日志已登记降级策略，但遥测与测试仍缺失；后续实现需同步更新 `AGENTS.md` 与本文档。
- Open question: 是否需要 Rust 端输出 `GraphemeDescriptor` fixture 以支撑未来追平？如需，请与 `iterator-facade-export` 一并规划。
- 待办：评估 ICU4N 依赖体积与许可，作为后续替换 `unicode_segmentation` 行为的可行方案。

---

## 📋 2025-11-16 架构会议决策：坚持骨架映射

### 会议结论
**✅ 坚持骨架映射策略，采用方案 B（M3 接口验证 + M4 完整切换）**

### 阻塞点分类
- **游标生命周期**：✅ 必须解决，M3 基于字符串特化实现（5-7 天）
- **Metric 互操作**：⚠️ 可部分降级，M3 保留动态 `IMetric` + 泛型接口验证，M4 切换
- **Chunk 迭代器**：⚠️ 可降级，M3 临时返回 `ReadOnlyMemory<char>`（3-4 天）
- **字素导航**：✅ 已确认降级，M3 实现 surrogate 安全 + 遥测（2 天）

### M3 核心交付（2-3 周）
1. `NodeCursor` 基于字符串特化 `Node.cs`
2. `TreeBuilder.Generic.cs` + 8 项接口验证测试
3. `TypeAliases.cs`：`global using RopeNode = Node;`（M4 一键切换泛型）
4. `Node.cs` 警告注释：禁止新增字符串特化 API
5. `RopeChunkEnumerator` 骨架（非零拷贝）
6. 字素降级 + 遥测

### M4 延后项
- 泛型节点全面接入（TreeBuilder/Delta/Rope 切换）
- 81 项测试迁移到 `RopeNode` 别名
- 删除/标记 Obsolete 字符串特化方法

### 测试验收
- M3 前：106 项 ✅
- 新增：8 项泛型接口测试 ✅
- **总计：114 项全部通过** ✅

详细会议记录与 10 项架构管控措施见上述各阻塞点的"M3 阶段策略"小节。
