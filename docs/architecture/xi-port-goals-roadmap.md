# Xi.Editor C# 迁移目标与路线图

> 版本：2025-11-11（初稿），随着实现推进持续迭代。

## 1. 项目背景
- **定位**：为创新型自研 Agent 系统提供 LLM Native & Friendly 的文本编辑核心，引入 Xi-Editor 的增量编辑优势，并适配 .NET/.NET 9 生态。
- **现状**：
   - AvalonEdit Core 已完成 WPF/GUI 剥离，可作为传统文本模型与 UI 注解体系的参考。
   - Xi-Editor 迁移已构建最小解决方案骨架、Rope 原型及测试基线，正向写时复制与性能优化拓展。
   - `xi-editor-ph7` fork 正在重构 Rust 源以减少关联类型、`Arc::make_mut` 等语言特性依赖，为 C# 端提供可直接映射的骨架。
- **总体愿景**：结合两套体系长处，为 Agent 提供高性能、增量友好的文本缓冲区与编辑流水线，使 LLM 能通过渲染后的 Doc 界面安全、准确地执行复杂编辑任务。

## 2. 目标使用场景
- 编辑器以库形式内嵌在 Agent 进程，每个可编辑目标对应一个 Buffer + View。
- 渲染层输出“文档样式”文本帧：正文采用代码围栏，光标/选区/折叠通过内联标记与图例呈现。
- 仅向 LLM 注入最新帧，减少上下文膨胀；编辑操作通过 Tool Calling 暴露。
- 支持虚拟行号、软换行、区域折叠、Viewport 裁剪与快照预览，降低误操作风险。

## 3. 功能范围（Scope）
### 3.1 核心必须项
1. **Rope 文本缓冲与 Delta 管线**
   - Rope: Append/GetSlice/Insert/Delete/Replace 均为 O(log n)；维护 Base/Lines/Utf16 度量。
   - Delta & Subset: 支持 factor/summary/transform，生成可组合的增量补丁。
2. **编辑命令与撤销/重做**
   - Selection/Movement/Insert/Delete/Indent/Outdent；Undo/Redo 与 undo groups。
3. **视图增量同步**
   - LineCache、Viewport 抽象；增量 diff 推送；软换行与折叠状态维护。
4. **Tool Calling 集成契约**
   - 将核心命令映射为结构化工具调用协议；定义参数与返回值模型。
5. **可观测性 & 测试**
   - 单元/属性测试、Golden Trace 对照、性能基准；日志与基本统计指标。

### 3.2 重要增强项
- 插件/宿主接口：JSON-RPC 兼容 + 进程内插件。
- 文本属性层（Style/Annotations）：为高亮、诊断、提示提供结构化通道。
- 多 Buffer/多 View 管理：支持 Agent 同时维护多个文档与窗口。

### 3.3 非目标（短期不做）
- GUI 渲染、输入法、图形交互；由外部渲染层负责。
- 完整终端/远程编辑器；聚焦嵌入式 Agent 场景。
- 与 AvalonEdit 的源码级对位迁移；只提炼其概念用于渲染与锚点管理。

## 4. 成功判据
- **一致性**：Rope/Delta/编辑命令与 Rust 版行为对齐，核心测试与 golden trace 通过。
- **性能**：百万字符文本常规操作<$50\text{ms}$，内存占用与 GC 压力可控。
- **嵌入体验**：提供清晰 API & Tool Call 模型，Agent 能够稳定生成/接收增量帧。
- **可维护性**：文档、日志、测试齐备；关键决策在 `AGENTS.md` 与架构文档留痕。

## 5. 双向协同策略

- **同步演进**：Rust 与 C# 双方共享 `docs/reference/rust-skeleton.md` 与 `docs/skeleton/xi.Core.Rope.cs`，每次改动需同步刷新骨架与 `rope-port-mapping.md` 状态。
- **首选在 Rust 调整**：遇到难以机械移植的语句，优先评估 Rust 是否可通过 helper/泛型化/去宏化等方式重写，再复制到 C#。
- **测试互证**：确保 Rust 端 `cargo test -p xi-rope` 与 C# 端 `dotnet test Xi.Editor.sln` 均保持通过；新增用例需在两端标注对应性。
- **脚本化差异追踪**：规划在 `scripts/` 中追加对比工具，输出“Rust helper 列表 vs C# 实现进度”，结果写入 `AGENTS.md` 的关键认知。
- **决策闭环**：所有跨语言接口调整需在 `AGENTS.md`、`bi-direction-port.md` 留存背景与取舍，避免多线分叉。

## 6. 路线图（高层）
| 里程碑 | 目标快照 | 关键交付 | 验证方式 |
| ------ | -------- | -------- | -------- |
| **M0 完成** | 架构梳理、计划对齐 | `AGENTS.md`、结构/模块计划、本文档 | 文档评审 |
| **M1 进行中** | .NET 骨架 + Rope 最小实现 | `xi.Core` 项目、`Rope`、基础测试 | `dotnet test` 基线 |
| **M2** | Rope/Delta 完整语义 | Rope 节点写时复制、Delta/Subset、FsCheck 属性测试、基准脚手架 | 单元+属性测试、Benchmark |
| **M3** | 编辑命令流水线 | Selection/Movement/Undo 管线、命令 API、golden trace 导入 | 集成测试、回归报告 |
| **M4** | 视图与增量通知 | LineCache/ViewDiff、Viewport、折叠/软换行处理 | 视图 diff 测试、模拟前端 |
| **M5** | 插件 & Tool Call 适配 | Tool 调用契约、JSON-RPC Host、示例插件 | 端到端试用、互通验证 |
| **M6** | 性能与观察性 | Benchmark、Telemetry、调优报告、使用指南 | 性能报告、文档审查 |
| **M7** | 双端收敛 | Rust/C# helper 对齐、测试矩阵，形成迁移流水线 | Rust/C# 双端基线报告 |

> 注：M1~M6 可迭代推进，必要时拆分为更细粒度的冲刺。

## 7. 关键依赖与输入
- Rust 原仓库（`reference/rust`）与 `xi-editor-ph7` fork：算法参考、测试数据、语言特性重构成果。
- AvalonEdit Core 拆解成果：虚拟行、锚点、折叠模型可借鉴。
- Agent 渲染层需求：提供 Tool Call 协议、帧渲染 DSL 的约束。

## 8. 风险与缓解
| 风险 | 描述 | 缓解策略 |
| ---- | ---- | -------- |
| Rope 性能/内存与 Rust 有显著差距 | 大文本操作延迟过高 | 尽早引入写时复制与叶片容量控制，使用 BenchmarkDotNet 监测 |
| Rust 重构偏离上游 | 双端接口不兼容 | 在 fork 中启用 feature flag，确保回放原版测试；将不兼容变更记录到 `bi-direction-port.md` |
| Delta/Undo 行为偏差 | 撤销栈或插件同步错误 | 重放 Rust traces；建立属性测试覆盖 Undo/Redo 序列 |
| ViewDiff/Viewport 语义不清 | LLM 渲染帧异常 | 先设计阶段性最小协议，工具调试统一产生帧，并与渲染层对通 |
| Tool Call 协议与核心耦合度过高 | 难以扩展其他前端 | 将 Tool 调用抽象为独立层（Command Adapter），保持核心干净 |
| JSON-RPC 互通复杂 | 插件生态难迁移 | 优先实现进程内插件，RPC 作为可选层，逐步验证兼容性 |

## 9. 下一步行动（针对 M1→M2）
1. 与 Rust 侧确认 `SharedNode`/`NodeKind` 等 helper 重构时间表，并在 C# 端同步命名。
2. 明确 Rope 写时复制与再平衡设计（节点重用策略、叶片最小/最大容量），补充设计文档与 todo 列表。
3. 起草 Delta/Subset C# 结构草图，列出关键方法（factor/summary/transform_expand）与测试样例来源，并在 Rust 侧标注即将改写的 helper。
4. 收敛 Tool Call 最小 API（文本插入、删除、选区查询、视图快照），与 Agent 渲染层对通需求。
5. 更新 `AGENTS.md`、`module-migration-plan.md` 对应条目，保持文档一致性。

---

*维护人：GitHub Copilot Agent*  
*如有更改，请在 PR 或提交说明中同步更新。*
