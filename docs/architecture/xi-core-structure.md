# xi-editor-core C# 移植架构梳理（草案）

本文档概述原始 Rust 版本 `xi-editor-core` 的关键子系统，并给出 C#/.NET 版本的目标分层与迁移顺序。目标是在保持核心语义和性能特征的前提下，交付一个可嵌入、可测试、可扩展的文本编辑引擎，并与 `xi-editor-ph7` fork 上的 Rust 重构形成双向协同（Rust 主动提供迁移友好 helper，C# 侧保持结构对齐）。

---

## 1. 系统分层总览

```
┌──────────────────┐
│ 前端 UI / 宿主应用 │  ⇄  JSON-RPC / 嵌入式 API  ⇄  Core 服务接口
└──────────────────┘
                                 │
                 ┌────────────────┴────────────────┐
                 │                │                │
           编辑命令调度       视图/通知层        插件桥接层
                 │                │                │
             文本存储与操作（Rope/CRDT 引擎）
                 │
           持久化 / Undo-Redo / 诊断
```

- **Core 服务接口**：对外暴露命令与查询，向内协调编辑、视图与插件。
- **文本存储与操作层**：`xi_rope` 及相关 delta/interval 逻辑，是性能与正确性的关键。
- **插件桥接层**：处理语言服务、语法高亮等扩展；通过 RPC 与核心交互。

---

## 2. Rust 核心模块速览与 C# 映射

| Rust 模块 | 职责概述 | 关键依赖 | 建议的 C# 命名空间/组件 | Rust 协同策略 |
|-----------|----------|----------|---------------------------|-------------------|
| `core` / `tabs` | 管理 Buffer/View 生命周期；处理前端 RPC；组织插件 | `xi_rpc`, `serde_json`, `core-lib` 内部模块 | `Xi.Core.Host`（RPC 宿主）、`Xi.Core.Workspace` | Rust 正拆分 idle token/helper，确保 C# 可无宏复用 |
| `editor` | 处理编辑命令（insert/delete, movement, selection）并生成 delta | `xi_rope`, `selection`, `movement`, `edit_ops` | `Xi.Core.Editing`（命令处理、编辑状态） | Rust 移除宏/关联类型，输出 helper skeleton |
| `line_cache_shadow` / `view` | 维护视图缓存和增量更新，向前端发出差异 | Rope 视图、`find`, `styles` | `Xi.Core.Views`（增量呈现、通知模型） | Rust 抽离行缓存 diff helper，降低闭包依赖 |
| `plugins` / `plugin_rpc` | 插件注册、生命周期、消息路由 | `xi_rpc`, `serde_json`, `plugins::manifest` | `Xi.Core.Plugins`（宿主与嵌入式接口） | Rust 引入 `trace` shim、精简 RPC schema |
| `backspace`, `movement`, `selection`, `edit_ops` | 高阶编辑语义 | `xi_rope`, Unicode 工具 | `Xi.Core.Editing` 的子命名空间（`Operations`, `Selection`） | Rust 导出测试 fixture + helper，以复用在 C# |
| `find`, `syntax`, `layers` | 搜索、语法高亮、层叠样式 | `xi_rope`, `syntect` | 暂定 `Xi.Core.Features`（可按需渐进移植） | 迁移到后期，Rust 保持接口稳定 |
| `xi_rope` crate | Rope 树、Delta、Interval、Diff | `tree`, `delta`, `interval` | 单独项目 `Xi.Core.Rope` 或 `Xi.Core.Text` | Rust 正封装 `SharedNode`、泛型 helper，避免 `Arc::make_mut` 直暴露 |
| `xi_unicode` | Unicode 宽度、词边界 | `unicode-segmentation` | 视情况直接引入 .NET 标准库或移植核心算法 | Rust 输出宽度表/测试数据，C# 侧可重用 |

> 注：JSON-RPC/插件层可以独立于核心库存在，因此核心 API 需保持宿主无关，便于嵌入模式和独立 Host 复用。

---

## 3. 关键数据流

1. **前端/宿主 → Core**：通过命令（RPC 或内嵌 API）提交编辑操作、配置更新、插件指令。
2. **Core → 文本引擎**：将命令翻译为 `RopeDelta` 操作，对文本缓冲区应用增量更新，同时维护撤销栈。
3. **Core → 视图层**：基于新的 buffer 快照生成增量视图（行缓存、样式区间），推送给前端。
4. **Core ↔ 插件**：选择性地镜像 buffer 状态到插件，接收插件返回的补丁或诊断，再回流到 Core。
5. **持久化 / 日志**：`core-lib` 通过 `file`, `config` 等模块管理磁盘交互与设置；C# 版本需预留钩子。

### 3.1 编辑命令生命周期（以 `insert` 为例）

1. 客户端通过 `CoreNotification::Edit`（JSON-RPC 或内嵌 API 调用）携带 `EditCommand` 进入 `CoreState::client_notification`。
2. `CoreState` 为目标视图创建 `EventContext`，该上下文持有 `Editor`、`View`、配置、插件句柄等引用。
3. `EventContext::do_edit` 根据命令类型调用 `Editor` 方法，例如 `Editor::do_insert`，并设置当前 `EditType`。
4. `Editor::add_delta`/`commit_delta` 组合 `xi_rope::Engine` 的 CRDT 状态，将操作归入合适的 undo group，并更新内部 `Rope`。
5. `View` 根据 delta 更新 `Selection`、`LineCache`，随后 `CoreState` 触发 `Client::update_view` 向前端发送增量 diff（通过 idle token 合批）。
6. 所有撤销/重做、插件 delta、同步事件均复用相同流水线，差异在于 delta 的构造方式与 priority/undo group 归属。

### 3.2 插件消息路线

1. 插件启动：`CoreState::do_start_plugin` 根据 manifest 创建子进程或嵌入式实例，并通过 `WeakXiCore` 提供回调能力。
2. 插件订阅文档：`PluginCatalog` 跟踪活跃插件；当 buffer 改变时，`EventContext` 会通过 `PluginManager` 派发 `update`，携带 `RopeDelta` 与当前 `Revision`。
3. 插件返回 `PluginEdit`：`WeakXiCore::handle_plugin_update` 调用 `Editor::apply_plugin_edit`，在确认 revision 合法后合并 delta。
4. 插件与核心通过 `PluginCommand<Request/Notification>` 结构体通讯，可双向 relay JSON-RPC 消息；C# 需暴露统一传输接口。

### 3.3 视图初始化与空闲调度

- 新视图通过 `CoreRequest::NewView` 创建：同步返回 `ViewId`，随后将 `view_init` 事件加入 `pending_views`。
- `CoreState` 在下一个 idle tick (`NEW_VIEW_IDLE_TOKEN`) 完成视图配置、主题加载、插件自动启动，实现顺序化初始化。
- 其他 idle token（渲染、重排、查找）用于节流昂贵操作；C# 实现需保留等价调度点，可使用 `TaskScheduler` 或自定义事件循环。Rust 端计划将 idle token 逻辑拆成显式 helper，便于 C# 复用。

---

## 4. 关键数据结构与行为

- **Rope**：基于 B-Tree 的文本存储，节点携带多种 metric（字节、UTF-16、行、可见宽度）。应优先移植 `rope`, `delta`, `interval`, `tree` 模块。
- **Delta / Transformer**：描述文本 diff 的中间形式，用于合并插件/用户操作，并维护 revision graph。
- **Selection / Movement**：抽象文本光标和多选区，依赖 Unicode 边界、行宽度等工具。
- **Line Cache**：缓存可视行，支持增量更新；C# 需提供等效 diff 通知结构。
- **Undo/Redo**：`edit_types` 标注操作类别，结合 delta 记录回放；需设计高效的历史栈。

---

## 5. C# 子系统规划

| 子系统 | 目标职责 | 关键类型与接口 | 状态 |
|--------|----------|----------------|------|
| `Xi.Core.Abstractions` | 对外 API（命令、事件、View 模型） | `ITextBuffer`, `IEditorSession`, `ViewUpdate` | 拟议 |
| `Xi.Core.Text` | Rope/Delta/Interval 实现 | `Rope`, `RopeSlice`, `RopeDelta`, `DeltaBuilder` | 待设计 |
| `Xi.Core.Editing` | 编辑命令、选择、撤销 | `EditCommand`, `SelectionSet`, `UndoManager` | 待设计 |
| `Xi.Core.Views` | 行缓存、通知生成 | `LineCache`, `ViewDiff`, `StyleSpan` | 待设计 |
| `Xi.Core.Plugins` | 插件生命周期、RPC 桥接 | `PluginHost`, `PluginSession`, `IPluginTransport` | 待设计 |
| `Xi.Core.Infrastructure` | 日志、诊断、调度 | `IClock`, `ILogger`, `Scheduler` | 待设计 |

当前仅有 `TextBuffer` 作为占位，计划在 Rope 完成后迁移到 `Xi.Core.Text`。与此同时，Rust 端正调整 `xi-rope` helper，以确保新 `Node<TInfo, TLeaf, TLeafOps>` 骨架在双端保持同步。

---

## 6. 迁移顺序与里程碑建议

1. **M0 - 架构梳理（当前任务）**
   - 输出跨模块依赖图、公共 API 草案。
   - 判定哪些 Rust 模块可延后（如语法高亮），并与 Rust 侧约定 helper 改造优先级。

2. **M1 - .NET 骨架与测试基线**
   - 已完成基本 Solution + 测试项目。
   - 后续在基线上补充 `ITextBuffer` 接口与 smoke tests；Rust 侧同步清理工作区与骨架文档。

3. **M2 - Rope & Delta 移植**
   - 按 `xi_rope` 子模块逐步翻译，建立对照测试（可引入原始 fixtures）。
   - 构建性能基准（插入/删除/查找）。
   - 依赖 Rust 提供 `SharedNode`、`Delta` helper 等迁移友好结构。

4. **M3 - 编辑命令与撤销/重做**
   - 移植 `editor`, `edit_ops`, `selection` 等模块。
   - 与 Rope 集成，打通基本编辑流水线。
   - Rust 侧同步拆除宏，输出可复用 helper。

5. **M4 - 视图缓存与通知**
   - 移植 `line_cache_shadow`, `view`，定义增量更新协议。
   - 构建前端模拟测试，验证 diff 正确性。
   - 对应 Rust 拆分行缓存 helper，提供共享测试数据。

6. **M5 - 插件/扩展层**
   - 设计嵌入式接口 + JSON-RPC Host。
   - 运行示例插件（echo、spellcheck）作为集成测试。
   - Rust 侧提供 `trace` no-op shim 与协议文档。

7. **M6 - 性能与工具化**
   - 基准测试、诊断、日志、遥测。
   - 文档与示例完善。
   - 双端记录性能数字，形成对照表。

---

## 7. 性能与测试策略提示

- **性能基线**：参考 Rust 版本在百万字符级的操作延迟；C# 版需监控 GC 分配、Span 使用。
- **测试层级**：
  - 单元测试：Rope 操作、Delta 合并、Selection 算法。
  - 属性测试：随机编辑序列验证不变式（可借助 FsCheck）。
  - 集成测试：重放官方 traces，验证视图更新与插件交互。
  - 性能测试：BenchmarkDotNet 或自定义 harness。

---

## 8. 风险与开放问题

1. **内存布局差异**：C# GC 对频繁节点分配的影响，需要池化策略或 struct 优化。
2. **并发模型映射**：Rust channel + worker 需在 .NET 中选用 `System.Threading.Channels` 或任务调度模型，确保顺序语义。
3. **跨端 helper 偏差**：Rust 重构节奏与 C# 实现可能不同步，导致接口不一致。
4. **插件隔离**：嵌入式模式与独立 Host 的契约需要统一抽象，避免重复实现。
5. **跨平台文件系统差异**：Rust 依赖 notify/tracing 等库，需要评估 C# 对应方案。
6. **Unicode 工具链**：Rust 采用专用 crate，C# 需验证内置 API 是否满足一致性。

---

## 9. 下一步建议

- 深入 `reference/rust/core-lib/src/editor.rs` 及 `tabs/` 代码，补充交互序列图，并与 Rust 侧确认 helper 改造计划。
- 起草 `Xi.Core` 对外 API 接口草图，明确最小可用命令集合。
- 评估 Rope 迁移所需的 .NET 数据结构支持（Span、MemoryPool、ValueTask）。
- 选取 2-3 个原始测试样例，准备仪式性回归测试框架，并规划如何生成跨语言共享数据。

---

*状态：2025-11-11 初稿；后续根据深入调研持续更新。*
