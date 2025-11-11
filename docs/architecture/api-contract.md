# Xi.Core 对外 API 契约草案

本草案定义 C#/.NET 版本 `Xi.Core` 暴露给宿主应用与插件的最小公共接口集合，旨在：

- 与 Rust 版 `xi-editor-core` 的命令/通知语义保持对齐；
- 支持嵌入式（in-process）与 JSON-RPC 双模式；
- 便于阶段性实现与测试驱动开发。

本文档描述命名空间划分、核心接口、请求/通知流程、插件交互以及并发约束，为后续编码与文档化提供统一基线。

---

## 1. 命名空间与对象模型

| 命名空间 | 主要职责 | 核心类型（草案） |
|-----------|----------|-------------------|
| `Xi.Core.Abstractions` | 宿主/前端交互契约、DTO、事件 | `IEditorHost`, `IEditorSession`, `BufferId`, `ViewId`, `TextDelta`, `SelectionRange`, `ViewUpdate` |
| `Xi.Core.Text` | 文本存储与 Delta | `Rope`, `RopeSlice`, `RopeDelta`, `DeltaBuilder` |
| `Xi.Core.Editing` | 命令模型与撤销栈 | `EditorCommand`, `CommandMetadata`, `UndoToken` |
| `Xi.Core.Views` | 视图缓存与增量通知 | `LineFragment`, `StyleSpan`, `ViewDiff` |
| `Xi.Core.Plugins` | 插件宿主与 RPC 桥接 | `PluginDescriptor`, `IPluginTransport`, `PluginCommand`, `PluginUpdate` |

宿主通过 `IEditorHost` 获取/管理 `IEditorSession`。`IEditorSession` 对应 Rust 中的 `CoreState` + 单个 Buffer 视图组合，负责处理编辑命令并产生 `ViewUpdate` 事件。

---

## 2. 命令与事件模型

### 2.1 请求/通知分类

| 类别 | 特性 | 示例 |
|------|------|------|
| **同步请求** | 立即返回结果，通常无副作用或仅查询 | `GetBufferSnapshot`, `QueryStyleSpans` |
| **异步命令** | 排队执行，完成后通过事件反馈 | `ApplyEdit`, `Save`, `StartPlugin` |
| **事件/通知** | 单向推送给宿主或插件 | `ViewUpdate`, `PluginDiagnostics`, `BufferStatusChanged` |

所有异步命令返回 `CommandToken`（可映射到 Rust 的 idle token / pending rev），便于宿主跟踪完成状态。

### 2.2 并发与顺序保证

- `IEditorSession` 遵循单线程逻辑上下文；外部调用可并行发起，内部排队按提交顺序执行。
- 视图更新通过 `IObservable<ViewUpdate>`/事件回调推送，保证与命令因果顺序一致（更新包含 `BaseRevision`）。
- 插件更新遵循 revision token 机制：只有确认最新 `Revision` 后，旧的 delta 将被拒绝，保持与 Rust `Engine` 语义同步。

---

## 3. Buffer / 编辑命令

下表汇总首批最小可用命令，均通过 `IEditorSession.ExecuteAsync(EditorCommand command)` 入口提交。

| 命令 | 参数摘要 | 结果 | 语义说明 | 依赖模块 |
|------|----------|------|-----------|----------|
| `OpenBuffer` | `OpenBufferOptions { Path?, InitialText? }` | `BufferId` | 打开或创建缓冲区；若携带文本则视为内存缓冲。 | Workspace/FileManager |
| `CloseBuffer` | `BufferId` | `void` | 释放缓冲与视图，触发 `BufferClosed` 事件。 | Workspace |
| `ApplyDelta` | `BufferId`, `TextDelta`, `CommandMetadata` | `UndoToken` | 对选区应用 delta（插入/删除/替换），合并到 CRDT。 | Text, Editing |
| `SetSelections` | `BufferId`, `SelectionRange[]`, `SelectionAffinity` | `void` | 更新多选区，影响后续编辑/视图。 | Editing |
| `MoveCursor` | `BufferId`, `MovementKind`, `MovementModifiers` | `SelectionRange[]` | 按照 movement 规则更新选区并返回新范围。 | Editing, Unicode |
| `Undo` / `Redo` | `BufferId`, `UndoScope?` | `UndoToken` | 与 Rust `undo`/`redo` 同步，利用 undo group 维护 CRDT 状态。 | Editing |
| `Save` | `BufferId`, `SaveOptions` | `SaveResult` | 将缓冲区写入磁盘（可异步）；同步更新 pristine revision。 | Workspace/File |
| `SetLanguage` | `BufferId`, `LanguageId` | `void` | 更新语法/配置，驱动插件与样式重建。 | Config, Plugins |
| `RequestFind` | `BufferId`, `FindQuery` | `FindResultToken` | 异步触发查找/高亮流程，结果通过事件返回。 | Features |

> 其余命令（如缩进、格式化、代码操作）可在后续阶段扩展为特定 `EditorCommand` 子类。

`EditorCommand` 建议设计为 discriminated union（C# 12 `required` record/`enum class`），方便 JSON-RPC 序列化。

---

## 4. 视图事件与通知

宿主通过订阅 `IEditorSession.ViewUpdates`（或注册回调）接收 `ViewUpdate`，该结构映射 Rust 的增量行缓存。

| 事件 | 数据成员 | 触发场景 | 说明 |
|------|----------|-----------|------|
| `ViewUpdate` | `ViewId`, `Revision`, `LineFragments[]`, `InvalidatedRanges[]`, `StyleSpans[]`, `CursorPositions`, `ScrollHint?` | 编辑命令、插件 delta、配置更改 | 对应 Rust `update` 通知；含 diff 信息，宿主可按需重绘。 |
| `BufferStatusChanged` | `BufferId`, `IsDirty`, `Diagnostics[]` | Undo/Redo、保存、插件诊断 | 提供状态栏信息、错误提示。 |
| `IdleTokenFired` | `TokenId`, `Timestamp` | 内部 idle 任务 | 用于重建缓存、自动保存、插件刷新。 |
| `FindResults` | `FindResultToken`, `MatchRanges[]` | 查找任务完成 | 与 Rust `find` 结果对应。 |

事件需包含 `BaseRevision`，确保宿主丢失/乱序的包可检测并请求重发。

---

## 5. 插件交互契约

### 5.1 插件生命周期 API

| 方法 | 方向 | 说明 |
|------|------|------|
| `RegisterPlugin(PluginDescriptor descriptor)` | 宿主 → Core | 声明插件元数据，供自动加载或手动启动。
| `StartPlugin(BufferId, PluginId, PluginLaunchMode)` | 宿主 → Core | 触发插件实例化（进程内/外）。
| `StopPlugin(PluginId)` | 宿主 → Core | 终止插件，释放资源。
| `ListActivePlugins(BufferId)` | 宿主 ← Core | 查询当前附加在缓冲区的插件。

### 5.2 插件与核心消息

- **更新推送**：`PluginUpdate { Revision, RopeDelta, ExtendedState? }` —— Core → 插件；要求插件 ACK。
- **插件响应**：`PluginCommand { ViewId, PluginId, Payload }` —— 插件 → Core；可包含 `PluginEdit`（delta）、`Diagnostics`、`RPC`。
- **通信传输**：
  - 嵌入式：实现 `IPluginTransport.SendAsync`，直接交互 DTO。
  - JSON-RPC：遵循原协议结构（方法名+参数 JSON）。Core 需提供序列化/反序列化层。

### 5.3 版本与兼容

- `ProtocolVersion` 字段用于协商；兼容策略优先保持向后兼容，必要时提供 capability 标志。
- 插件应在收到 `PluginUpdate` 后返回 `Ack(RevisionToken)`，否则 Core 暂缓 GC 与撤销。

---

## 6. 辅助接口

| 域 | API 概述 | 备注 |
|----|-----------|------|
| 配置 | `ApplyConfig(BufferId, ConfigDelta)`, `QueryConfig(BufferId)` | 区分用户/语言/项目层级；需要变更通知。 |
| 诊断 | `PublishDiagnostics(BufferId, DiagnosticEntry[])` | 插件或核心可发送；宿主决定展示。 |
| 遥测 | `TraceEvent(EventKind, EventPayload)` | 可选；结合 .NET EventSource。 |

---

## 7. 测试与验证要求

- **契约测试**：使用 `xi.Core.Tests` 提供的 in-memory 宿主模拟，实现接口后运行端到端命令序列。
- **跨语言回归**：针对 JSON-RPC 模式，重放 Rust 原版前端的黄金消息，确保序列化兼容。
- **属性测试**：对 `ApplyDelta`、`Undo/Redo` 等命令进行随机序列测试，断言 `ViewUpdate` 与 buffer 状态一致。

---

## 8. 后续扩展留白

- `CollaborativeSession`：为多人协作预留接口，可在 CRDT 基础上扩展。
- `BulkOperations`：大批量编辑/格式化命令，可携带 streaming delta。
- `Snapshotting`：提供 `ExportSnapshot(Stream target)` 支持外部持久化。
- `Scripting/Automation`：开放 `CommandPalette` 型接口，供宿主注入脚本。

---

*状态：2025-11-11 初稿；随实现推进迭代。*
