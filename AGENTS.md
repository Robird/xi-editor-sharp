## 跨会话记忆文档
本文档(`./AGENTS.md`)会伴随每个user消息注入你的上下文。这是一种外部辅助记忆机制，当你需要跨会话记录信息时，就记录在本文件中。完成一个工作、制定或调整计划后务必更新本文档，避免记忆偏差。

## 项目概览
- **愿景**：将 `xi-editor-core` 的核心能力移植为高性能、可嵌入的 C#/.NET 9 组件，同时保持与原版的增量文本处理特性和插件生态兼容。
- **范围**：核心文本引擎、rope/CRDT 数据结构、撤销/重做、通知/视图同步、可选的 JSON-RPC/插件宿主。
- **技术栈约束**：.NET 9、C# 12、xUnit；优先使用 BCL / 现有 NuGet 生态，必要时可引入 Span/Memory、Pipelines 等高性能 API。
- **依赖资源**：原仓库 `reference/rust`、`reference/docs`、Rust 测试用例与文档、现有插件示例。
- **成功标准（初稿）**：
	1. 核心编辑操作（插入/删除/批量应用 diff）与视图更新语义与 Rust 版一致。
	2. 在百万级字符文本上保持可接受的延迟（目标 <50ms 的常规编辑操作）。
	3. 提供公共 API 及最少示例/测试覆盖，支持内嵌调用与可选 RPC 模式。
	4. 完成端到端集成测试与基准，文档清晰。

## 当前状态（持续补充）
- 阶段：调研 + 工程骨架搭建。
- 代码：已创建 `Xi.Editor.sln`，包含 `xi.Core` 类库与 `xi.Core.Tests` xUnit 项目，内置占位 `TextBuffer` 与首个烟囱测试。
- 资产：`reference/` 中保留了原始 Rust 源码、文档与插件示例，待对照拆解。
- 阻塞项：无。

## 当前关键认知
- 目前的 C# 骨架提供 `TextBuffer` 占位实现，使用 `StringBuilder` 仅支持线性追加，未来将被 Rope 结构替换。
- 单元测试（`TextBufferTests`）已验证字符串与 `ReadOnlySpan<char>` 追加语义，可作为后续 Rope 行为的回归基线。
- Rust 版核心能力集中在 `core-lib` 与 `rope` 模块；移植需优先厘清这些模块的 API 边界、数据结构与性能假设。
- JSON-RPC/插件层可以独立于核心存在，因此核心 API 设计需保持嵌入式调用友好，同时预留事件/通知扩展点。
- 已对 `reference/rust/core-lib` 与 `reference/rust/rope` 的关键入口文件完成首轮梳理，输出了 C# 子系统映射与迁移顺序初稿（见 `docs/architecture/xi-core-structure.md`）。
- `CoreState` 通过 `EventContext` 串联 `View`、`Editor`、配置与插件，编辑命令在 `Editor::add_delta/commit_delta` 内完成 CRDT 合并；视图更新与插件通知依赖 idle token 合批调度，需要在 .NET 中提供等价机制。
- 已形成模块级迁移路线图（见 `docs/architecture/module-migration-plan.md`），明确各阶段任务、测试策略与风险缓解措施，为后续实施提供依据。
- 已起草对外 API 契约（见 `docs/architecture/api-contract.md`），界定首批编辑命令、视图通知与插件交互模型，为实现阶段提供统一接口基线。

（后续将随 Rope 预研、测试导入等任务推进，持续补充新的关键认知。）

## 工作节奏建议
1. **每次进入仓库**：先阅读“当前聚焦事项”、确认阻塞与决策，必要时调整计划。
2. **执行任务**：按优先级推进，过程中同步更新“当前聚焦”“技术笔记”“风险”。
3. **完成后**：将成果移动至“已完成事项”，记录在“工作日志”，并检视下一步。
4. **质量门禁**：任何代码提交前都需跑通构建/测试，记录结果。

## 里程碑路线图
- M0：架构梳理与移植策略文档（进行中）。
- M1：.NET 解决方案骨架 + 测试基线（未开始）。
- M2：Rope/编辑核心最小可用集（未开始）。
- M3：视图同步与增量通知管线（未开始）。
- M4：撤销/重做与持久化支持（未开始）。
- M5：嵌入式 API 封装 + 示例应用（未开始）。
- M6：JSON-RPC/插件宿主实现与互操作测试（未开始）。
- M7：性能调优、文档、发布准备（未开始）。

## 当前聚焦事项（WIP）
- 梳理原始 `xi-editor-core` 架构，输出 C# 子系统划分草案。
	- 明确核心数据结构（rope、CRDT、撤销/重做栈）的职责、API 边界、依赖关系。
	- 评估插件/RPC 层的嵌入式 vs. 独立 Host 策略与接口形态。
	- 当前已输出初稿：`docs/architecture/xi-core-structure.md`，后续需结合更细节的模块调研持续迭代。

## 下一步行动（高优先级 Backlog）
1. 继续梳理 `editor.rs`、`tabs.rs`、`plugins/` 细节，补充架构文档对配置同步、撤销栈、idle 调度策略的序列图，并提炼待移植的抽象接口需求。
2. 开展 Rope/Delta 移植预研：总结关键数据结构、评估 .NET Span/内存池策略，形成技术备忘录。
3. 整理可复用的 Rust 测试/trace 资产，规划在 xUnit 中的导入方式。
4. 根据 API 契约定义，提炼核心 DTO/接口的 C# 原型（例如 `IEditorSession`, `EditorCommand`），为后续实现奠定骨架。
	- 产出初版接口说明或伪代码，以便下一阶段直接开始编码。

## 未来候选事项（Backlog）
- 建立对齐原版的黄金测试集（复用参考仓库 traces）。
- 设计性能基准框架，覆盖常见编辑场景与极端负载。
- 调研 C# 内存池/Span 友好容器，用于 Rope 节点与缓存。
- 设计可观察性基础（日志/Telemetry）以支持调试。
- 规划插件示例（如 echo、spellcheck）在 C# 版本中的最小实现。

## 决策 & 假设日志
- [假设] 保持与 Rust 版相同的树/片段结构以便复用测试与算法描述。
- [假设] 首选单一解决方案（Solution）管理所有项目，利于构建脚本与 CI。
- [TODO] 记录未来的架构决策（例如：通道选型、序列化库、内存策略）。

## 研究 / 阅读清单
- `reference/rust/core-lib/src`：核心编辑引擎实现。
- `reference/rust/rope/src`：Rope 数据结构与算法细节。
- `reference/docs/docs/rope_science_*.md`：rope 科学系列文章。
- `reference/docs/docs/crdt*.md`：CRDT 与协作模型说明。
- `reference/rust/rpc` 与 `python/` 插件示例：RPC 协议与插件交互。

## 技术笔记（随任务更新）
### 数据结构
- TODO：提炼 Rope 节点布局、平衡策略、分裂/合并逻辑。
- 临时实现：`TextBuffer` 使用 `StringBuilder` 作为占位，便于快速落地测试；后续需以 Rope 替换并保持 API 向后兼容。

### 并发模型
- TODO：对照 Rust 中的调度（channel + worker），评估 C# 中 `System.Threading.Channels` / `Task` 的映射。

### 插件 & RPC
- TODO：梳理 JSON-RPC 消息流，分离核心库与宿主的边界。

### 测试策略
- TODO：定义单元、属性测试、集成测试、基准测试的分层结构。

## 风险 & 未解问题
- Rope/CRDT 在 .NET 中的内存布局差异可能引起 GC 压力，需要早期验证。
- JSON-RPC 性能与兼容性尚未验证，可能需要二进制协议替代方案。
- 原版依赖的增量渲染/前端协议在 C# 生态中的宿主适配尚未明确。

## 已完成事项
- 2025-11-11：建立 `.NET 9` 解决方案骨架（`Xi.Editor.sln`），创建 `xi.Core` 类库与 `xi.Core.Tests` 测试项目，引入 `TextBuffer` 占位实现及首个 xUnit 烟囱测试，通过 `dotnet test` 验证。
- 2025-11-11：梳理 `xi-editor-core` 架构并输出 C# 子系统划分草案初稿（`docs/architecture/xi-core-structure.md`）。
- 2025-11-11：制定模块级移植路线图草案（`docs/architecture/module-migration-plan.md`），明确阶段任务、测试策略与风险缓解措施。
- 2025-11-11：起草 `Xi.Core` 对外 API 契约（`docs/architecture/api-contract.md`），覆盖命令、事件、插件交互与并发约束。

## 工作日志
- 2025-11-11：初始化跨会话文档框架，整理目标与初步计划。
- 2025-11-11：搭建 .NET 解决方案骨架，创建核心/测试项目，编写 `TextBuffer` 占位实现与基础测试并验证通过。
- 2025-11-11：执行 `dotnet test`（默认配置）确认核心与测试项目编译与单元测试均通过。
- 2025-11-11：阅读 `reference/rust/core-lib` 与 `reference/rust/rope` 关键入口文件，编写架构梳理文档初稿。
- 2025-11-11：进一步解析 `editor.rs`、`tabs.rs`，在架构文档中补充编辑命令、插件消息与 idle 调度流程描述。
- 2025-11-11：编写模块级迁移路线图草案，梳理阶段任务、完成判据与风险缓解策略。
- 2025-11-11：整理命令/通知/插件交互契约并形成 `api-contract` 文档，为后续实现统一接口。