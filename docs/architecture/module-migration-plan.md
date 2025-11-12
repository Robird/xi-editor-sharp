# xi-core 模块级移植路线图（草案）

本路线图在《xi-core 结构梳理》基础上，给出各关键子系统的迁移顺序、依赖关系、测试策略与完成判据。目标是让 C# 版本能够在保持语义一致性的同时，逐步替换占位实现并建立可持续的工程节奏。

---

## 0. 总体策略

- **分阶段交付**：以可运行骨架 → Rope/Delta → 编辑流水线 → 视图通知 → 插件与宿主 为主线，每阶段都要保证回归测试与性能验证。
- **保持最小可用面**：优先实现核心文本存储和编辑语义，语法高亮、搜索等高级特性可延后。
- **测试先行**：每个模块的实现都需伴随对应的单元/属性/集成测试，以 Rust 版行为为基准。
- **文档与决策同步**：阶段完成后更新《AGENTS.md》《xi-core 结构梳理》与本文档，记录决策与风险。

---

## 1. 阶段性里程碑

| 阶段 | 目标快照 | 关键输出 | 主要依赖 |
|------|-----------|----------|----------|
| M0 | 架构梳理完成，明确模块边界 | 架构文档、迁移计划（即本文） | 已有 Rust 代码调研 |
| M1 | .NET 解决方案骨架 & 测试基线 | `Xi.Editor.sln`、`xi.Core`、`xi.Core.Tests`、占位 `TextBuffer` | M0 |
| M2 | Rope & Delta 移植最小集 | `Xi.Core.Text`（Rope、Delta、Interval）、对照测试、基准脚手架 | M1 |
| M3 | 编辑命令流水线 | `Xi.Core.Editing`（选择、移动、Undo/Redo）、CRDT 集成测试 | M2 |
| M4 | 视图缓存与通知 | `Xi.Core.Views`（LineCache、Style）、增量更新协议与测试 | M3 |
| M5 | 插件/宿主接口 | `Xi.Core.Plugins`、JSON-RPC Host、示例插件 | M4 |
| M6 | 性能 & 观察性 & 文档 | Benchmark、Telemetry、使用指南 | M5 |

> 2025-11-13 注：Rust 工作区已精简为 `xi-core`、`xi-core-lib`、`xi-plugin-lib`、`xi-rope`、`xi-rpc`、`xi-trace`、`xi-unicode` 七个核心 crate；`experimental/lang`、`lsp-lib`、`sample-plugin`、`syntect-plugin` 已删除，原基准测试目录已重命名为 `*.parked` 以保留参考源码。`PluginLoadError` dead code 与硬链接告警已处理完毕（新增 `.cargo/config.toml` 禁用增量编译），当前仅保留 `serde_test` future incompat 需后续评估。

---

## 2. 模块任务拆解

### 2.1 Text / Rope 子系统

- **目标**：提供等价于 `xi_rope` 的 Rope 树、Delta、Interval、Diff、Metric 支持。
- **子任务**：
  - 翻译 `tree`, `rope`, `delta`, `interval`, `engine` 中的结构体与算法。
  - 为节点与 metric 使用 struct + Span 友好 API，避免过度 GC。
  - 引入 `BenchmarkDotNet` 验证常见操作（插入、删除、slice、line count）。
- **测试**：
  - 逐文件单元测试：节点分裂、合并、度量计算。
  - 属性测试：随机编辑序列保持 Rope 不变式（Base + FsCheck）。
  - 与 Rust 版对照：重放 `xi_rope` 的 golden fixtures。
- **完成判据**：通过所有单元/属性测试；性能基准达到 Rust 同阶或可接受差距；API 替换 `TextBuffer` 并保持测试通过。

### 2.2 Editing / CRDT

- **目标**：移植 `editor`, `edit_ops`, `selection`, `movement`, `annotations`, `layers` 关键逻辑。
- **子任务**：
  - 移植 `Engine`-based CRDT 与撤销/重做管理（undo group、GC 策略）。
  - 实现 `SelectionSet`, `SelRegion`, `Movement`，保证 Unicode 一致性。
  - 构建 `UndoManager`，覆盖 `MAX_UNDOS`, `force_undo_group` 场景。
  - 定义 `EditCommand` 枚举/类，与公共 API 对齐。
- **测试**：
  - 操作级单元测试：插入、删除、换行、缩进、粘贴、撤销/重做链路。
  - 属性测试：随机命令序列与撤销栈一致性。
  - 集成测试：`Editor` + `View` 协同验证 selection drift、undo groups。
- **完成判据**：CRDT 合并、撤销逻辑与 Rust 回归测试一致；所有编辑命令的 happy path/edge case 覆盖。

### 2.3 Views / Notifications

- **目标**：移植 `view`, `line_cache_shadow`, `styles`, `width_cache`，提供增量 diff 推送。
- **子任务**：
  - 定义 `ViewDiff`, `LineCache` 数据模型，保持 RPC 兼容。
  - 移植样式层叠、范围合并算法。
  - 设计 idle 调度等价实现（可能基于 `TaskScheduler` 或自定义事件循环）。
- **测试**：
  - 行缓存单元测试：换行、折行、查找高亮。
  - 集成测试：操作触发 diff、断言通知序列。
  - 性能：大文件滚动、批量编辑下的 diff 开销。
- **完成判据**：在模拟前端上验证 diff 正确性；通知协议文档化，测试覆盖主要场景。

### 2.4 Workspace / Host

- **目标**：C# 版本的 `CoreState` 等价体，管理 buffer/view 生命周期、配置与文件 IO。
- **子任务**：
  - 设计 `Workspace` / `CoreHost` 类，处理命令调度与上下文创建。
  - 可插拔的配置提供者、文件管理器。
  - 持久化与自动保存策略。
- **测试**：
  - 模拟客户端命令流，验证 buffer/view 生命周期、配置更新。
  - 文件操作模拟：打开、保存、冲突处理。
- **完成判据**：能驱动 Rope + Editing + Views，与伪客户端完成基本编辑、保存、撤销流程。

### 2.5 Plugins / RPC

- **目标**：提供嵌入式插件接口、可选 JSON-RPC Host，与原版协议兼容。
- **子任务**：
  - 定义 `IPluginTransport`、`PluginHost`，支持进程内/进程外通信。
  - 复用 `System.Text.Json` + Pipelines 优化序列化。
  - 实现示例插件（echo、spellcheck）作为集成验证。
- **测试**：
  - 协议单元测试：序列化兼容、错误处理、重连。
  - 集成测试：插件生命周期、增量更新、一起撤销。
- **完成判据**：JSON-RPC 模式与 Python 示例互通；嵌入模式可在同进程执行。

### 2.6 Infrastructure

- **目标**：提供日志、追踪、调度与诊断支撑。
- **子任务**：
  - 选取日志框架（`Microsoft.Extensions.Logging`?）并接入关键路径。
  - 设计可配置的 idle 调度器、任务队列。
  - 将 Rust 端 `xi-trace` 依赖抽象为可选 shim，便于 C# 侧以 no-op 或替代实现落地。
  - 集成性能计数与 tracing（可选 EventSource）。
- **测试**：
  - 负载场景下的调度稳定性测试。
  - 日志/Tracing 开关的回归测试。
- **完成判据**：关键子系统均有可观测性；长时间运行稳定。

---

## 3. 风险与缓解措施（按模块）

| 风险 | 影响 | 缓解策略 |
|------|------|-----------|
| Rope 节点频繁分配导致 GC 压力 | 性能退化 | 使用 struct + ArrayPool，提前做基准；必要时引入 `SpanOwner` 等池化策略 |
| CRDT 合并语义与 Rust 不完全一致 | 数据错乱 | 建立密集回归测试，参考 Rust traces；分阶段对照结果 |
| Idle 调度在 .NET 中语义差异 | 更新延迟或 UI 卡顿 | 抽象调度接口，模拟 Rust idle token 行为；在集成测试中覆盖 |
| 插件 RPC 兼容性 | 插件无法复用 | 用 Python 示例作为金标准；保持 JSON schema 一致；提供兼容层 |
| Unicode 处理差异 | 光标/选区错误 | 使用 .NET ICU API 或引入第三方库（如 `Rune` API + 自定义宽度表） |

---

## 4. 交付物 & 文档要求

- 每阶段完成需更新：
  - 《AGENTS.md》中的当前状态、关键认知、下一步计划。
  - 《xi-core 结构梳理》中的模块细节与图谱。
  - 本路线图对进展、风险与缓解措施的调整。
- 随阶段交付的辅助资料：
  - API 参考草图（命名空间、公共类型）。
  - Benchmark 报告与结果记录。
  - 使用示例或演示脚本。

---

## 5. 即将开展的行动建议

1. **API 契约草案**：在 `docs/architecture/api-contract.md` 中定义最小外部接口（Buffer 命令、View 通知、插件交互）。
2. **Rope 移植预研**：整理 `xi_rope` 数据结构与算法要点，评估 .NET 中的等价实现策略与可能的 helper 类型。
3. **测试资源整理**：筛选原仓库中可复用的测试/trace，并规划如何在 .NET 测试项目中引入。

---

*状态：2025-11-11 初稿；随阶段推进迭代。*
