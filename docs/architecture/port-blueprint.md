# Xi.Editor C# 迁移蓝图（统一草案）

> 版本：2025-11-14；本文件汇总并重整原 `docs/architecture` 与阶段性规划文档中的关键信息，供当前会话及后续协作者快速恢复上下文。内容随实现推进滚动更新。

---

## 1. 文档目的与阅读指引

- **统一视图**：将原分散于 `docs/architecture`、`docs/csharp-refactor` 等目录的草案集中整理，确保架构、API、移植策略与进度规划在单文档内自洽呈现。
- **跨团队协同基线**：同步 C# 端移植路线与 `xi-editor-ph7` Rust fork 的协同约定，减少语言差异造成的信息偏差。
- **任务导航**：每节末尾标注当前状态与下一步任务，便于在迭代中定位优先事项。

---

## 2. 背景与愿景

### 2.1 项目定位
- 目标构建兼容 .NET 生态的高性能文本编辑内核，为 Agent/LLM 应用提供**增量、可验证**的编辑体验。
- 结合 Rust `xi-editor` 的 Rope/CRDT 优势与 AvalonEdit 在行缓存、锚点管理方面的经验，面向嵌入式（in-process）与 JSON-RPC 双模式。

### 2.2 目标使用场景
- 单个 Agent 进程内托管多个 Buffer/View，通过 Tool Calling 向 LLM 暴露结构化命令。
- 渲染层生成“文档样式”帧（正文 + 光标/选区/折叠标记），每次响应仅投递最新帧，缓解上下文膨胀。
- 支持虚拟行号、软换行、区域折叠、viewport 裁剪，减少误操作与重排开销。

### 2.3 成功判据
- Rope/Delta/编辑命令行为与 Rust 基线一致，核心测试与黄金 trace 通过。
- 百万字符级操作延迟 < 50 ms，GC 压力可控，关键指标可观测。
- Tool Calling API 与核心内核解耦，宿主/插件能够稳定交换增量数据。

**状态（2025-11-14）**：M1 架构骨架已建立（`Xi.Editor.sln`、`xi.Core.Tests` 基线 96 项通过）；Rope 写时复制阶段 B 完成，Stage C（Engine 镜像）与 Stage D（夹具刷新流程）推进中。

---

## 3. 系统分层与数据流

```
┌──────────────────┐
│ 前端 UI / Agent 宿主 │ ⇄ JSON-RPC / 嵌入式 API ⇄ Core 服务接口
└──────────────────┘
                     │
        ┌────────────┴────────────┐
        │                         │
   编辑命令调度                视图与通知层            插件桥接层
        │                         │                     │
        └─────────────── Rope / CRDT 引擎 ──────────────┘
                             │
                     持久化 / Undo / 诊断
```

- **Core 服务接口**：暴露命令、查询与事件，协调 Buffer/View/插件生命周期。
- **Rope / CRDT 引擎**：`xi-rope` 等价实现，是性能与正确性的核心。
- **视图层**：维护行缓存、样式增量、viewport 裁剪。
- **插件层**：管理生命周期与消息桥接，兼容 JSON-RPC 与进程内调用。

**待办**：M3 进入前需确定 idle token/事件循环在 .NET 中的映射策略（`TaskScheduler` 或自定义队列）。

---

## 4. 对外 API 契约摘要

### 4.1 命名空间与核心类型草案

| 命名空间 | 职责 | 代表类型（草案） |
|----------|------|----------------|
| `Xi.Core.Abstractions` | 宿主交互契约、DTO、事件 | `IEditorHost`, `IEditorSession`, `BufferId`, `ViewId`, `TextDelta`, `SelectionRange`, `ViewUpdate` |
| `Xi.Core.Text` | 文本存储 & Delta | `Rope`, `RopeSlice`, `RopeDelta`, `DeltaBuilder` |
| `Xi.Core.Editing` | 命令与撤销栈 | `EditorCommand`, `CommandMetadata`, `UndoToken` |
| `Xi.Core.Views` | 行缓存与增量通知 | `LineFragment`, `StyleSpan`, `ViewDiff` |
| `Xi.Core.Plugins` | 插件宿主与桥接 | `PluginDescriptor`, `IPluginTransport`, `PluginUpdate`, `PluginCommand` |

宿主通过 `IEditorHost` 管理 `IEditorSession` 实例；`IEditorSession` 近似 Rust `CoreState` 的单 Buffer 投影，负责命令执行与视图事件推送。

### 4.2 命令分类与顺序保障

| 类别 | 特性 | 示例 | 顺序保证 |
|------|------|------|----------|
| 同步请求 | 立即返回，无副作用或只读 | `GetBufferSnapshot`, `QueryStyleSpans` | 调用线程直接返回 |
| 异步命令 | 入队执行，完成后事件反馈 | `ApplyEdit`, `Save`, `StartPlugin` | `CommandToken` 跟踪完成顺序，内部串行处理 |
| 事件/通知 | Core → 宿主/插件 | `ViewUpdate`, `PluginDiagnostics`, `BufferStatusChanged` | 含 `BaseRevision`，确保乱序可检测 |

- `IEditorSession` 按提交顺序执行命令，外部可并行发起调用。
- `ViewUpdate` 事件与命令因果顺序一致，包含 `Revision` 与 `InvalidatedRanges`。
- 插件更新要求 ACK 最新 `RevisionToken`，保持与 Rust `Engine` 语义同步。

### 4.3 关键命令一览

| 命令 | 参数摘要 | 返回值 | 语义说明 |
|------|----------|--------|----------|
| `OpenBuffer` | `OpenBufferOptions { Path?, InitialText? }` | `BufferId` | 打开或创建缓冲区，支持内存缓冲 |
| `CloseBuffer` | `BufferId` | `void` | 释放缓冲与视图，触发 `BufferClosed` |
| `ApplyDelta` | `BufferId`, `TextDelta`, `CommandMetadata` | `UndoToken` | 应用 Rope delta，写入 CRDT |
| `SetSelections` | `BufferId`, `SelectionRange[]`, `SelectionAffinity` | `void` | 更新多选区 |
| `MoveCursor` | `BufferId`, `MovementKind`, `MovementModifiers` | `SelectionRange[]` | 返回新选区位置 |
| `Undo`/`Redo` | `BufferId`, `UndoScope?` | `UndoToken` | 管理 undo group，与 Rust `undo`/`redo` 对齐 |
| `Save` | `BufferId`, `SaveOptions` | `SaveResult` | 写入磁盘并刷新 pristine revision |
| `SetLanguage` | `BufferId`, `LanguageId` | `void` | 更新语法配置，触发插件刷新 |
| `RequestFind` | `BufferId`, `FindQuery` | `FindResultToken` | 异步查找，结果走事件返回 |

> `EditorCommand` 建议使用 C# 12 `required` record 或判别联合表达，便于 JSON-RPC 序列化。

### 4.4 事件模型

| 事件 | 数据成员 | 说明 |
|------|----------|------|
| `ViewUpdate` | `ViewId`, `Revision`, `LineFragments[]`, `InvalidatedRanges[]`, `StyleSpans[]`, `CursorPositions`, `ScrollHint?` | 对应 Rust `update` 通知，含 diff 信息 |
| `BufferStatusChanged` | `BufferId`, `IsDirty`, `Diagnostics[]` | 用于状态栏与错误提示 |
| `IdleTokenFired` | `TokenId`, `Timestamp` | 对齐 Rust idle 机制（重建缓存、自动保存等） |
| `FindResults` | `FindResultToken`, `MatchRanges[]` | 查找任务完成通知 |

### 4.5 插件契约

- 生命周期 API：`RegisterPlugin`, `StartPlugin`, `StopPlugin`, `ListActivePlugins`。
- 消息方向：
  - Core → 插件：`PluginUpdate { Revision, RopeDelta, ExtendedState? }`，插件需返回 `Ack(RevisionToken)`。
  - 插件 → Core：`PluginCommand { ViewId, PluginId, Payload }`，涵盖 `PluginEdit`、`Diagnostics`、自定义 RPC。
- 传输层：嵌入式实现 `IPluginTransport.SendAsync`；JSON-RPC 模式按原协议序列化。
- 版本协商：`ProtocolVersion` + capability 标志，保持向后兼容。

### 4.6 辅助接口

| 范畴 | API | 备注 |
|------|-----|------|
| 配置 | `ApplyConfig(BufferId, ConfigDelta)`, `QueryConfig(BufferId)` | 区分用户/语言/项目层级，需事件通知 |
| 诊断 | `PublishDiagnostics(BufferId, DiagnosticEntry[])` | 插件或核心均可发送 |
| 遥测 | `TraceEvent(EventKind, EventPayload)` | 结合 `System.Diagnostics.Tracing` 或 ILogger |

**下一步**：完成 API 草图后，将契约转写为 `Xi.Core.Abstractions` 项目接口定义，并编写 in-memory 宿主用于契约测试。

---

## 5. Rope 移植策略

### 5.1 方法论
- **类型优先**：先对齐 `struct`/`enum`/trait 接口，再填充算法实现。
- **文件一一映射**：维护 Rust `rope` 模块与 `Xi.Core.Rope` 目录的映射表，跟踪状态标签（`未开始`、`仅骨架`、`实现中`、`已实现`、`Rust 重构中`）。
- **最小增量**：按函数粒度迁移，配套 C#/Rust 双端测试；保留 `ValidateInvariants` 等诊断以约束结构正确性。
- **先 Rust 后 C#**：遇到 `Arc::make_mut`、生命周期、模式匹配等难以翻译的特性，优先在 Rust fork 中提供 helper（如 `SharedNode::ensure_unique`、泛型 trait 调整），再同步到 C#。
- **测试骨架同步**：在 `xi.Core.Tests` 建立与 Rust 同名的测试壳子，必要时使用共享 JSON fixture。

### 5.2 文件映射概览

| Rust 模块 | 关键职责 | C# 目标 | 状态 | 备注 |
|-----------|----------|---------|------|------|
| `tree.rs` | 节点借用、合并、再平衡 | `Tree/Node.cs`, `Tree/Node.Generic.cs`, `Tree/TreeBuilder.cs`, `Tree/LeafSplitter.cs`, `Tree/StringLeafOperations.cs`, `Tree/TreeContracts.cs` | 实现中 | 叶片借用/合并与 SharedNode helper 已对齐；泛型包装层尚未接入主线，内部节点再平衡与诊断待补 |
| `tree.rs`（Cursor 等） | `Cursor`, `BalanceIter` | `Tree/NodeCursor.cs`（骨架） | 进行中 | Rust 侧 `CursorDescriptor` 已就绪并新增可选 `cursor_state` feature gate（`Cursor::state()`, `CursorState::restore` 等借用-free API），后续需采集性能数据并引导 C# 游标实现对齐 |
| `rope.rs` | Rope API、Metric 聚合 | `Rope.cs`, `RopeInfo.cs`, `Metrics.cs`, `IMetric.cs` | 实现中 | `StringLeafOperations`、`BreaksMetricHelper` 已落地；聚合增量刷新与泛型 Node 接入待完成 |
| `delta.rs` | `Delta`, `Transformer` | `Rope/Delta.cs`, `Rope/DeltaJson.cs` | 实现中 | Stage B 完成泛型 `Delta` 与 JSON；`factor`/`Transformer` 待实现 |
| `multiset.rs` | `Subset`, `SubsetBuilder` | `Rope/Subset.cs`, `Rope/SubsetJson.cs` | 已实现 | Stage A 完成 JSON 回归与 triple helper |
| `engine.rs` | CRDT Engine、Undo | `Rope/Engine.cs`, `Rope/EngineJson.cs` | 已实现 | Stage C 完成不可变镜像与黄金 fixture |
| `interval.rs` | 区间结构 | `Rope/Interval.cs` | 已实现 | 基础半开区间 [Start, End) 结构已落地，后续可扩展 Span 友好 helper |
| `diff.rs` / `compare.rs` | Diff 与比较 | `Diff/` 模块（规划中） | 未开始 | 待建立 `src/xi.Core/Diff/` 骨架，评估复用现成库或逐步移植 |
| `breaks.rs` | 段落切分 | `Rope/BreaksMetricHelper.cs` | 已实现 | 零分配断点 helper 与测试已到位；与树结构的再平衡入口仍需整合 |
| `find.rs` / `spans.rs` | 搜索与高亮 | `Search/` 模块（规划中） | 未开始 | 需先建 `src/xi.Core/Search/` 骨架，依赖 Metric、Interval |
| `serde_impls.rs` | 序列化支持 | `Rope/SubsetJson.cs`、`Rope/DeltaJson.cs`、`Rope/EngineJson.cs` | 进行中 | Stage A-C JSON 镜像完成；Stage D 夹具刷新与自动化尚在推进 |
| `test_helpers.rs` | 测试工具 | `tests/xi.Core.Tests/RopeTestHelpers.cs` | 已实现 | 提供不变量断言与诊断输出 |

**脚注**：映射表需在每次落地或 Rust helper 改造后刷新，并同步 `docs/architecture/rope-port-mapping.md`。

> 2025-11-14 自查：上述路径已对照仓库现状修订；`Diff/` 与 `Search/` 模块尚未建立，后续任务需先补齐骨架再对外引用。

### 5.3 Helper 对齐记录

- **SharedNode**：`ensure_unique`, `clone_with_children`, `replace_child_range` 在 Rust/C# 双端封装完成；后续将扩充诊断 instrumentation。
- **Metric 模板**：`docs/rust-refactor/breaks-metrics-templating.md` 为唯一真源；Rust `codepoint`/`lines`/`break_indices` helper 对应 C# `StringLeafOperations` 与 `BreaksMetricHelper`。
- **Delta Helper**：`Delta::base_len()` ↔ `Delta<TInfo, TLeaf>.BaseLength`；`iter_elements` ↔ `EnumerateElements()`；JSON 字段 `els`/`base_len` 与 `DeltaJson` 保持一致。
- **Engine Helper**：`Engine::revision_log()` ↔ `Engine.RevisionLog()`；`from_serialized_state()` ↔ `Engine.FromSerializedState` 等。

### 5.4 缺口与改进
- **Delta/Transformer**：C# 侧尚缺 `factor()`、`summary()`、`transform_expand()` 等核心算法；需等待 Rust helper 拆分完成。
- **Cursor 架构**：Rust `Cursor<'a>` 需改写为索引/Arc 模式，C# 才能实现无生命周期版本。
- **Breaks 整合 / Diff / Search**：`BreaksMetricHelper` 已实现并通过测试，但尚未与树的再平衡入口联动；Diff/Search 模块仍需先落地骨架再逐步移植。
- **泛型节点记录**：`Node.Generic.cs` 骨架已存在但尚未接入主实现，需在注释中持续标注与 Rust 泛型化成果的对齐计划。

### 5.5 下一步
1. 在 `Xi.Core.Rope` 补全 `Transformer` 骨架并标注与 Rust helper 的对齐 TODO（`Delta.Factor` 仍保留 stub）。
2. 刷新 `docs/skeleton/rope.md` 与 `docs/skeleton/xi.Core.Rope.cs`，确保骨架反映最新 helper 与泛型签名。
3. 在 `xi.Core.Tests` 添加与 Rust 测试同名的 `Skip` 用例，为 `Transformer`/`factor` 等特性预留验证入口。

---

## 6. 里程碑与任务拆解

### 6.1 阶段路线图

| 阶段 | 目标快照 | 关键输出 | Rust 协同动作 | 依赖 |
|------|----------|----------|---------------|------|
| **M0** 已完成 | 架构梳理、计划对齐 | 架构文档（含本文件）、`AGENTS.md` 更新 | 导出 `docs/reference/rust-skeleton.md` | Rust 代码调研 |
| **M1** 进行中 | .NET 解决方案骨架 + Rope 原型 | `Xi.Editor.sln`、`xi.Core`、`xi.Core.Tests` 基线 | Rust 精简工作区，冻结核心 crate | M0 |
| **M2** 进行中 | Rope & Delta 最小集 | `Xi.Core.Rope` 核心结构、SharedNode helper、Stage A-C serde 镜像 | Rust 提供 SharedNode/Metric helper，继续阶段 D（夹具刷新自动化） | M1 |
| **M3** | 编辑命令流水线 | `Xi.Core.Editing`（Selection、Movement、Undo）、CRDT 集成测试 | Rust 拆解宏，输出编辑 helper | M2 |
| **M4** | 视图缓存与通知 | `Xi.Core.Views`（LineCache、Style、Diff） | Rust 精简 `line_cache_shadow` helper | M3 |
| **M5** | 插件 & Tool Call | `Xi.Core.Plugins`、JSON-RPC Host、示例插件 | Rust 统一 `trace` shim、RPC schema | M4 |
| **M6** | 性能与观察性 | Benchmark、Telemetry、使用指南 | Rust 输出基准脚本、性能对照 | M5 |
| **M7** | 双端收敛 | Helper 对齐报告、CI 脚本 | Rust/C# 互检 | 贯穿 |

### 6.2 模块任务摘要

1. **Text / Rope**：移植 `tree`, `rope`, `delta`, `interval`；同步 Rust helper；引入 BenchmarkDotNet；FsCheck 属性测试。
2. **Editing / CRDT**：迁移 `editor`, `selection`, `movement`, `edit_ops`；实现 `UndoManager`；复用 Rust helper；搭建命令层集成测试。
3. **Views / Notifications**：设计 `ViewDiff`, `LineCache` 与 viewport；移植样式层叠；规划 idle 调度；构建视图 diff 测试。
4. **Workspace / Host**：实现 `Workspace`/`CoreHost` 管理 Buffer/View/配置；抽象文件 IO；设计自动保存策略。
5. **Plugins / RPC**：定义 `PluginHost`、`IPluginTransport`；实现进程内/外插件；复用 `System.Text.Json`；提供示例插件。
6. **Infrastructure**：接入 `Microsoft.Extensions.Logging` 或 `EventSource`；设计 idle/任务队列；对齐 Rust `trace` shim。

### 6.3 下一步（短期）
1. 更新 `node-generic-refactor-plan.md`，记录 Rust 泛型化成果与 C# 待办。
2. 评估 `Cursor` 生命周期削薄方案，生成最小 POC 设计笔记。
3. 规划 SharedNode instrumentation（调用计数、ptr_eq 校验），对齐 Rust 输出。
4. 和渲染层对齐 Tool Call 最小契约（插入、删除、选区查询、视图快照）。

---

## 7. 双向协同机制

- **骨架同步**：使用 `scripts/refresh_skeleton_docs.py` 定期生成 `docs/reference/rust-skeleton.md` 与 `docs/skeleton/xi.Core.Rope.cs`，保持签名一致。
- **文档更新**：任何 helper/类型调整需同步 `AGENTS.md`、`rope-port-mapping.md`、`bi-direction-port.md`，形成单一事实来源。
- **CI / 测试基线**：
  - Rust：`cargo test --workspace`，重点关注 `xi-rope`，按需启用 `serde` feature。
  - C#：`dotnet test Xi.Editor.sln`（当前 96 项全绿，含 Stage A-C 序列化回归）。
- **夹具维护**：Stage D 使用 `scripts/refresh_serialization_fixtures.ps1` 调用 `export-serde-fixtures` CLI 更新 JSON，需记录操作与验证结果。
- **差异追踪**：计划在 `scripts/` 中增添对照脚本，输出“Rust helper 列表 vs C# 实现”报告，并纳入 CI。

---

## 8. 风险与缓解

| 风险 | 描述 | 缓解策略 |
|------|------|----------|
| Rope 节点频繁分配导致 GC 压力 | 大文本场景性能退化 | 使用 struct + ArrayPool；引入 Benchmark 监测；必要时探索 `SpanOwner` |
| Rust helper 改造滞后 | C# 阻塞在语言特性差异 | 在 `bi-direction-port.md` 记录待协同项；滞后时以 stub 解锁 C# 测试，后续替换 |
| CRDT 行为偏差 | 撤销栈或插件同步不一致 | 重放 Rust traces；建立属性测试覆盖 Undo/Redo 序列 |
| Idle 调度语义差异 | 更新延迟或 UI 卡顿 | 抽象调度接口；编写集成测试模拟 idle token 行为 |
| 插件 RPC 兼容性 | 插件生态难迁移 | 优先完成进程内插件；JSON-RPC 作为兼容层，提供 schema 校验 |
| Unicode 处理差异 | 选区/光标错误 | 引入 .NET `Rune` API + 辅助宽度表；同步 Rust 测试数据 |
| Stage D 夹具刷新失误 | JSON 黄金数据错位 | 执行脚本后即刻运行 `cargo test -p xi-rope --features serde engine_serialization_regression` 与 `dotnet test` 复核 |

---

## 9. 下一步行动清单（2025-11-14 之后）

1. **Node 泛型同步**：
   - 在 C# 主实现接入 `Node<TInfo, TLeaf, TLeafOps>` 包装层，跑通 81 项 Rope 测试。
   - 刷新 `docs/skeleton/rope.md`/`xi.Core.Rope.cs`，标注泛型签名与 helper 对齐。
2. **Cursor 生命周期筹备**：
   - Phase 1 `CursorDescriptor` 已交付并通过 `xi-editor-ph7/rust/rope/tests/cursor_descriptor.rs`，可在诊断会议上通报命中率与失效路径。
   - Rust 侧已上线可选 `cursor_state` feature gate（`Cursor::state()`, `CursorState::restore`/`from_cursor` 等），默认关闭；现已补充 Base/Lines/Utf16 三类 Metric 的导航对拍测试，确认启用/禁用模式下语义一致，后续仅在需要时通过轻量 instrumentation 观察热点，再评估默认策略。
   - 在 Rust/C# 双端扩展共享夹具：新增 `cursor_state_round_trip_basic`、`cursor_state_handles_deep_paths`、`cursor_state_invalidates_after_edit` 及 `cursor_state_preserves_navigation_*` 系列测试；C# `NodeCursor` 需准备对应实现与验证入口以复用同一套样本。
3. **SharedNode 诊断计划**：
   - 设计计数器与 ptr_eq 校验，明确 Rust/C# instrumentation 输出格式与测试入口。
4. **Stage D 自动化完善**：
   - 将 `run_all_checks` serde/无 serde 与 `dotnet test` 接入统一脚本/CI；补充失败回溯策略。
5. **Tool Call 契约草稿**：
   - 将核心编辑命令映射为 Tool schema，提供样例与验证脚本，支持 Agent 渲染层集成。
6. **文档联动**：
   - 更新 `AGENTS.md`、`rope-port-mapping.md`、`rope-serialization-fixture-playbook.md`，保持本文件与外部记忆一致。

---

*维护人：GitHub Copilot Agent（2025-11-14 更新）*
