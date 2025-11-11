## 跨会话记忆文档
本文档(`./AGENTS.md`)会伴随每个 user 消息注入上下文，是跨会话的外部记忆。完成一个任务、制定或调整计划时务必更新本文件，避免记忆偏差。

## 项目概览
- **愿景**：将 `xi-editor-core` 的核心能力移植为高性能、可嵌入的 C#/.NET 9 组件，同时保持与原版的增量文本处理特性和插件生态兼容。
- **范围**：核心文本引擎、Rope/CRDT 数据结构、撤销/重做、通知/视图同步、可选的 JSON-RPC/插件宿主。
- **技术栈约束**：.NET 9、C# 12、xUnit；优先依赖 BCL/主流 NuGet 生态，必要时引入 Span/Memory、Pipelines 等高性能 API。
- **依赖资源**：原仓库 `reference/rust`、`reference/docs`、Rust 测试用例与文档、现有插件示例。
- **成功标准（初稿）**：
  1. 核心编辑操作（插入/删除/批量应用 diff）与视图更新语义对齐 Rust 版本。
  2. 百万字符规模文本编辑保持可接受延迟（常规操作目标 <50ms）。
  3. 提供清晰的公共 API、示例与测试，支持嵌入式调用与可选 RPC 模式。
  4. 具备端到端集成测试、性能基准与完善文档。

## 当前状态（持续补充）
- 阶段：调研 + 工程骨架搭建。
- 代码：已创建 `Xi.Editor.sln`，包含 `xi.Core` 类库与 `xi.Core.Tests` xUnit 项目，内置占位 `TextBuffer` 与首个烟囱测试。
- 资产：`reference/` 中保留原始 Rust 源码、文档与插件示例，待持续对照拆解。
- 阻塞项：无。

## 当前关键认知
- 目前工程同时保留 `StringBuilder` 版 `TextBuffer` 与基于 Rope 的 `RopeTextBuffer`；后者已覆盖 Append/GetSlice/Replace/Insert/Delete 语义并通过单元测试验证，但仍以重建子树为主，需要引入节点重用、写时复制和再平衡约束以恢复 O(log n) 性能。
- Rust Rope 采用泛型 B-树（`Node<NodeInfo>`），叶节点大小受 `MIN_LEAF=511` / `MAX_LEAF=1024` 限制，内部节点聚合 `lines`、`utf16_size` 等指标，为多坐标系遍历和增量更新提供 O(log n) 行为。
- Metric 体系（Base/Lines/Utf16）通过统一接口支持不同坐标转换；`prev/next` 等操作需跨叶处理断裂，C# 版本必须提供等效能力以避免重复扫描。
- Delta/Subset 组合支撑插入、删除与并发协作：`factor()` 拆分插入/删除，`transform_expand`/`synthesize` 完成坐标重映射，是撤销与插件同步的基础能力。
- C# 迁移需实现写时复制的节点管理（引用计数或复制策略），并评估 `string`、`char[]`、`ArrayPool<char>` 等叶节点承载方式以控制 GC 压力。
- 核心 API 设计需保持嵌入式调用友好，同时为 JSON-RPC/插件层预留事件与通知扩展点。
- 已对 `reference/rust/core-lib` 与 `reference/rust/rope` 的关键入口文件完成首轮梳理，输出 C# 子系统映射与迁移顺序初稿（`docs/architecture/xi-core-structure.md`）。
- 已建立模块级迁移路线图（`docs/architecture/module-migration-plan.md`）与对外 API 契约（`docs/architecture/api-contract.md`），作为持续实施基线。
- 新增《Xi.Editor 迁移目标与路线图》文档（`docs/architecture/xi-port-goals-roadmap.md`），明确面向 LLM Agent 的目标功能与阶段里程碑。
- `docs/architecture/rope-delta-notes.md` 汇总 Rope/Delta 迁移要点，为后续设计与编码提供结构化指导。
- 已引入 `ITextBuffer` 接口并对占位实现和测试完成适配，为 Rope 替换提供统一契约与校验基线。
- `RopeInfo` 与 Metric 抽象（Base/Lines/Utf16）已落地并具备测试支撑，为 Rope 节点实现提供依赖类型。
- `RopeNode`/`TreeBuilder` 已与 `RopeTextBuffer` 接轨，支持跨叶切片与编辑；但节点聚合信息仍需针对写时复制与再平衡优化，避免长文本编辑频繁重建整棵树。
- 最新一次 `dotnet test` 运行覆盖 34 项 Rope/TextBuffer 测试全部通过，为性能优化与 Delta 原型验证提供回归基线。

（后续将随 Rope 优化、Delta 迁移及测试导入推进，持续补充新的关键认知。）

## 工作节奏建议
1. **进入仓库**：优先阅读“当前聚焦事项”，确认阻塞与最新决策，必要时调整计划。
2. **执行任务**：按优先级推进，并实时更新“当前聚焦”“技术笔记”“风险”。
3. **任务完成**：将成果移动至“已完成事项”，在“工作日志”记录关键行动，检视下一步。
4. **质量门禁**：所有代码改动前后需跑通构建/测试，并记录结果。

## 里程碑路线图
- M0：架构梳理与迁移策略文档（进行中）。
- M1：.NET 解决方案骨架 + 测试基线（进行中）。
- M2：Rope/编辑核心最小可用集（未开始）。
- M3：视图同步与增量通知管线（未开始）。
- M4：撤销/重做与持久化支持（未开始）。
- M5：嵌入式 API 封装 + 示例应用（未开始）。
- M6：JSON-RPC/插件宿主实现与互操作测试（未开始）。
- M7：性能调优、文档、发布准备（未开始）。

## 当前聚焦事项（WIP）
- **Rope 结构优化**：为现有编辑流程引入节点写时复制与聚合信息增量更新，补齐叶片大小约束与再平衡策略，确保 Replace/Insert/Delete 在长文本场景维持 O(log n) 复杂度。
- **Delta/Subset 原型**：依据 `docs/architecture/rope-delta-notes.md` 制定 C# 迁移步骤，先实现最小 `Delta`/`Subset` 类型与 `factor()`、`summary()`、坐标重映射流程，为撤销与插件同步奠定基础。
- **行为对照与测试资产**：整理 `reference/rust/core-lib` 中的经典操作序列，规划引入 xUnit 测试或 trace，支撑 Rope 与 Delta 行为比对。

## 下一步行动（高优先级 Backlog）
1. 拓展 `RopeTextBuffer`：实现节点写时复制、叶片容量约束与再平衡策略，减少编辑操作的整树重建，并补充大文本/跨叶边界测试。
2. 拓展 Delta/Subset：落地 C# 原型并验证简单插入/删除与 `factor()`、`summary()` 等关键流程。
3. 深入梳理 `editor.rs`、`tabs.rs`、`plugins/`，在架构文档中补充撤销栈、配置同步、idle 调度序列图，提炼对核心 API 的附加需求。
4. 整理可复用的 Rust 测试/trace 资产，规划导入 xUnit 的策略，为后续功能验证做准备。

## 未来候选事项（Backlog）
- 建立对齐原版的黄金测试集（复用参考仓库 traces）。
- 设计性能基准框架，覆盖常见编辑场景与极端负载。
- 调研 .NET 内存池/Span 友好容器，用于 Rope 节点与缓存。
- 设计可观察性基础（日志/Telemetry）以支撑调试。
- 规划插件示例（如 echo、spellcheck）在 C# 版本中的最小实现。

## 决策 & 假设日志
- [假设] 保持与 Rust 版相同的树/片段结构以便复用测试与算法描述。
- [假设] 优先通过单一 Solution 管理所有项目，便于构建脚本与 CI。
- [TODO] 后续记录更多架构决策（通道选型、序列化库、内存策略等）。

## 研究 / 阅读清单
- `reference/rust/core-lib/src`：核心编辑引擎实现。
- `reference/rust/rope/src`：Rope 数据结构与算法细节。
- `reference/docs/docs/rope_science_*.md`：Rope 科学系列文章。
- `reference/docs/docs/crdt*.md`：CRDT 与协作模型说明。
- `reference/rust/rpc` 与 `python/` 插件示例：RPC 协议与插件交互流程。

## 技术笔记（随任务更新）
### 数据结构
- Rope 采用 B-树节点 + 写时复制，叶节点倾向 1KB 左右；C# 实现需维护 `lines`、`utf16_size` 聚合信息以支撑多 Metric。
- 叶节点候选：短期继续使用 `string`，中长期评估 `char[]` / `ArrayPool<char>` + `ReadOnlyMemory<char>` 的池化方案。
- 临时实现：`TextBuffer` 使用 `StringBuilder` 作为占位，便于快速落地测试；后续需由 Rope 实现替换并保持 API 兼容。

### 并发模型
- TODO：对照 Rust 中的调度（channel + worker），评估 C# 中 `System.Threading.Channels` / `Task` 的映射策略。

### 插件 & RPC
- TODO：梳理 JSON-RPC 消息流，明确核心库与宿主的边界。

### 测试策略
- TODO：定义单元、属性、集成、基准测试的分层结构。

## 风险 & 未解问题
- Rope/CRDT 在 .NET 中的内存布局差异可能导致 GC 压力，需要及早验证。
- JSON-RPC 性能与兼容性尚未验证，可能需要探索二进制协议替代方案。
- 原版依赖的增量渲染/前端协议在 C# 生态中的宿主适配尚未明确。

## 已完成事项
- 2025-11-11：建立 `.NET 9` 解决方案骨架（`Xi.Editor.sln`），创建 `xi.Core` 类库与 `xi.Core.Tests` 测试项目，引入 `TextBuffer` 占位实现及首个 xUnit 烟囱测试。
- 2025-11-11：梳理 `xi-editor-core` 架构并输出 C# 子系统划分草案初稿（`docs/architecture/xi-core-structure.md`）。
- 2025-11-11：制定模块级迁移路线图草案（`docs/architecture/module-migration-plan.md`），明确阶段任务、测试策略与风险缓解措施。
- 2025-11-11：起草 `Xi.Core` 对外 API 契约（`docs/architecture/api-contract.md`），覆盖命令、事件、插件交互与并发约束。
- 2025-11-11：整理 Rope/Delta 迁移要点并形成备忘录（`docs/architecture/rope-delta-notes.md`）。
- 2025-11-11：引入 `ITextBuffer` 接口并更新 `TextBuffer` 实现与测试基线，为 Rope 替换打通契约。
- 2025-11-11：补齐 `RopeInfo` 与 Metric 基础类型及首批单元测试，为 Rope 节点实现提供依赖。
- 2025-11-11：实现 `RopeNode` 与 `TreeBuilder` 骨架及配套测试，支持多叶节点拼接与叶片拆分策略。
- 2025-11-11：实现 `RopeTextBuffer` 最小可用版并补充单元测试，验证接口契约与基础操作。
- 2025-11-11：增强 `RopeNode` 切片与 `RopeTextBuffer.GetSlice`，新增跨叶验证测试确保树遍历正确。
- 2025-11-11：扩展 `RopeNode`/`RopeTextBuffer` 支持插入、删除与通用替换，统一文本缓冲契约并覆盖跨叶编辑测试。
- 2025-11-11：`dotnet test`（34 项 Rope/TextBuffer 相关测试）确认最新 Rope 编辑实现保持通过，为后续优化提供回归基线。

## 工作日志
- 2025-11-11：初始化跨会话文档框架，整理目标与初步计划。
- 2025-11-11：搭建 .NET 解决方案骨架，创建核心/测试项目，编写 `TextBuffer` 占位实现与基础测试并验证通过。
- 2025-11-11：执行 `dotnet test`（默认配置）确认核心与测试项目编译与单元测试通过。
- 2025-11-11：阅读 `reference/rust/core-lib` 与 `reference/rust/rope` 关键入口文件，编写架构梳理文档初稿。
- 2025-11-11：解析 `editor.rs`、`tabs.rs` 并在架构文档中补充编辑命令、插件消息与 idle 调度流程描述。
- 2025-11-11：编写模块级迁移路线图草案，梳理阶段任务、完成判据与风险策略。
- 2025-11-11：整理命令/通知/插件交互契约并形成 `api-contract` 文档。
- 2025-11-11：调研 `reference/rust/rope` 与 `rope_science` 文档，沉淀 Rope/Delta 迁移要点并成文。
- 2025-11-11：实现 `ITextBuffer` 接口与 `TextBuffer` 更新，补充长度/切片测试并验证通过。
- 2025-11-11：实现 `RopeInfo`、Metric 抽象与对应测试，建立 Rope 迁移所需的基础类型。
- 2025-11-11：实现 `RopeNode`/`TreeBuilder` 初版与单元测试，验证叶节点拼接、长文本拆分及遍历正确性。
- 2025-11-11：实现 `RopeTextBuffer` 并通过接口级单元测试，奠定以 Rope 替换占位实现的基础。
- 2025-11-11：扩展 `RopeNode`/`RopeTextBuffer` 替换与插入/删除操作，完善 `ITextBuffer` 契约并新增跨叶编辑测试。
- 2025-11-11：执行 `dotnet test`（34 项 Rope/TextBuffer 测试）确认最新 Rope 编辑实现保持通过。