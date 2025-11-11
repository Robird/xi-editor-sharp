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
- 目前工程同时保留 `StringBuilder` 版 `TextBuffer` 与新建的 `RopeTextBuffer`；后者已复用 `ITextBuffer` 契约完成基本替代，并接入基于树的切片逻辑，下一步需实现原地编辑以摆脱整串重建。
- Rust Rope 基于泛型 B-树（`Node<NodeInfo>`），叶节点大小受 `MIN_LEAF=511`/`MAX_LEAF=1024` 控制，内部节点聚合 `lines`、`utf16_size`，为多指标遍历与增量更新提供 O(log n) 性能。
- Metric 体系（Base/Lines/Utf16）通过单接口实现多坐标系转换，`prev/next` 等操作需跨叶处理断裂；C# 版本需提供等效接口避免重复扫描。
- Delta/Subset 组合支撑插入、删除与并发协作：`factor()` 拆分插入与删除，`transform_expand`/`synthesize` 负责坐标重映射，是撤销、插件同步的基础能力。
- C# 迁移须提供写时复制的节点管理（自定义引用计数或复制策略），并评估 `string` vs `char[]`/`ArrayPool<char>` 等叶节点承载方案以控制 GC 压力。
- JSON-RPC/插件层可以独立于核心存在，因此核心 API 设计需保持嵌入式调用友好，同时预留事件/通知扩展点。
- 已对 `reference/rust/core-lib` 与 `reference/rust/rope` 的关键入口文件完成首轮梳理，输出 C# 子系统映射与迁移顺序初稿（见 `docs/architecture/xi-core-structure.md`）。
- 已形成模块级迁移路线图（`docs/architecture/module-migration-plan.md`）与对外 API 契约（`docs/architecture/api-contract.md`），作为持续实施的基线。
- `docs/architecture/rope-delta-notes.md` 汇总 Rope/Delta 迁移要点，为接下来设计与编码提供结构化指导。
- 已引入 `ITextBuffer` 接口并调整占位实现与测试，为 Rope 替换提供统一契约与校验基线。
- 初步落地 `RopeInfo` 与 Metric 抽象（Base/Lines/Utf16），为后续 Rope 节点实现提供依赖类型与测试支撑。
- 已实现最小 `RopeNode` 与 `TreeBuilder` 骨架，支持多叶节点拼接与基本平衡策略，为 Rope 替换铺路。
- 新增基于 Rope 的 `RopeTextBuffer` 实现，复用既有接口并通过单元测试验证追加、切片与清空语义。

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
- Rope/Delta 迁移预研已完成初版总结（`docs/architecture/rope-delta-notes.md`）；现阶段聚焦将结论转化为代码骨架与测试基线。
	- 设计 `ITextBuffer`/Metric 等抽象，准备替换现有 `TextBuffer` 实现。
	- 明确 Rope 节点与写时复制机制的 C# 实现路径。
	- 籍由抽象整合，为后续 Delta/Subset 移植奠定基础。
	- `ITextBuffer` 初版已落地，后续任务可围绕 Rope/Metric 实现展开。
	- Metric 抽象与首批实现已就绪，可在此基础上构建 `RopeNode` 与 `TreeBuilder`。
- `RopeNode`/`TreeBuilder` 最小实现完成，下一步需要将其接入 `ITextBuffer` 并扩展编辑操作。
- `RopeTextBuffer` 已提供最小可用实现，并支持基于 Rope 的切片；需继续扩展编辑 API，使其在插入/删除场景下保持 O(log n) 性能，并规划与现有 API/测试的切换策略。

## 下一步行动（高优先级 Backlog）
1. 拓展 `RopeTextBuffer` 功能：在现有切片基础上实现原生编辑（插入/删除）与节点重用，补充跨叶编辑测试并评估性能。
2. 拓展 Delta/Subset 相关类型的 C# 原型，验证简单插入/删除与 `factor()`、`summary()` 等关键流程。
3. 继续梳理 `editor.rs`、`tabs.rs`、`plugins/`，补充架构文档中对撤销栈、配置同步、idle 调度的序列图，并提炼对核心 API 的额外需求。
4. 整理可复用的 Rust 测试/trace 资产，规划在 xUnit 中的导入策略，为后续功能验证做准备。

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
- Rope 采用 B-树节点 + 写时复制，叶节点倾向 1KB 左右大小；C# 实现需维护 `lines`、`utf16_size` 聚合信息以支撑多 Metric。
- 叶节点候选：短期持续使用 `string`，中长期评估 `char[]/ArrayPool<char>` 搭配 `ReadOnlyMemory<char>` 的池化方案。
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
- 2025-11-11：整理 Rope/Delta 迁移要点并形成备忘录（`docs/architecture/rope-delta-notes.md`），总结数据结构映射与落地计划。
- 2025-11-11：引入 `ITextBuffer` 接口，更新 `TextBuffer` 实现与测试基线，为 Rope 替换打通契约。
- 2025-11-11：补齐 `RopeInfo` 与 Metric 基础类型及首批单元测试，为 Rope 节点实现提供依赖与验证。
- 2025-11-11：实现 `RopeNode` 与 `TreeBuilder` 骨架及配套测试，支持多叶节点拼接与叶片拆分策略。
- 2025-11-11：实现 `RopeTextBuffer` 最小可用版并补充单元测试，验证接口契约与基础操作。
- 2025-11-11：为 `RopeNode`/`RopeTextBuffer` 增强树内切片能力，避免整串快照获取子串，并扩展跨叶测试。

## 工作日志
- 2025-11-11：初始化跨会话文档框架，整理目标与初步计划。
- 2025-11-11：搭建 .NET 解决方案骨架，创建核心/测试项目，编写 `TextBuffer` 占位实现与基础测试并验证通过。
- 2025-11-11：执行 `dotnet test`（默认配置）确认核心与测试项目编译与单元测试均通过。
- 2025-11-11：阅读 `reference/rust/core-lib` 与 `reference/rust/rope` 关键入口文件，编写架构梳理文档初稿。
- 2025-11-11：进一步解析 `editor.rs`、`tabs.rs`，在架构文档中补充编辑命令、插件消息与 idle 调度流程描述。
- 2025-11-11：编写模块级迁移路线图草案，梳理阶段任务、完成判据与风险缓解策略。
- 2025-11-11：整理命令/通知/插件交互契约并形成 `api-contract` 文档，为后续实现统一接口。
- 2025-11-11：调研 `reference/rust/rope` 与 `rope_science` 文档，沉淀 Rope/Delta 迁移要点并落地备忘文档。
- 2025-11-11：实现 `ITextBuffer` 接口与 `TextBuffer` 更新，补充长度/切片测试并验证通过。
- 2025-11-11：实现 `RopeInfo`、Metric 抽象与对应测试，建立 Rope 迁移所需的基础类型。
- 2025-11-11：实现 `RopeNode`/`TreeBuilder` 初版与单元测试，验证叶节点拼接、长文本拆分及遍历正确性。
- 2025-11-11：实现 `RopeTextBuffer` 并通过接口级单元测试，奠定以 Rope 替换占位实现的基础。
- 2025-11-11：增强 `RopeNode` 切片与 `RopeTextBuffer.GetSlice`，新增跨叶验证测试确保树遍历正确。