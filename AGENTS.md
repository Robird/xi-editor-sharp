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
- `RopeNode` 新增 `SplitAt`，`Slice`/`Insert`/`Delete` 已改写为走写时拆分+拼接路径，显著减少重复构建带来的性能浪费，为后续 COW/再平衡铺路。
- 最新一次 `dotnet test` 运行覆盖 35 项 Rope/TextBuffer 测试全部通过，为性能优化与 Delta 原型验证提供回归基线。
- 发布《Rope 写时复制与再平衡实施方案草案》（`docs/architecture/rope-cow-rebalance-plan.md`），确立叶片/内部节点约束、阶段拆解（A~F）与测试、基准计划，作为后续 Rope 优化的执行蓝图。
- `RopeNode` 新增 `WithChildReplaced` 帮助方法，并通过单元测试验证聚合信息正确刷新，为阶段 A（节点局部更新与引用复用）提供基础能力。
- `RopeNode` 新增 `CloneWithChildren` 支撑批量子节点替换与聚合信息重建，为结构共享下的多子节点编辑奠定基础。
- `SplitAt` 语义：`SplitAt(index)` 将 rope 在树的路径内拆分为左右两个节点（Left, Right），保留未修改的子树引用，实现结构共享；该操作对 Leaf/内部节点均递归有效，并保持 `RopeInfo` 聚合信息正确。
- `TreeBuilder` 的 `PushString` 使用 `MaxLeafSize` 切片策略（并避免在 UTF-16 surrogate 边界拆分），保证叶节点大小在目标范围附近（当前为 `MaxLeafSize`），但尚未实现最小叶片合并策略或内部节点重平衡。
- `TreeBuilder` 拆分长文本时新增换行优先策略，并保留 UTF-16 代理对完整性，为阶段 B 的叶片分裂/合并逻辑提供基础能力。
- 引入 `LeafSplitter` 统一叶片拆分逻辑，并在 `RopeNode` 增加 `EnsureWritableLeaf`、`SplitLeafByBounds`，为叶片写时复制与容量约束提供可复用 API。
- `RopeNode.Insert` 在叶片容量允许的情况下直接执行单叶写时复制，减少整棵树重建；超出容量时回退到结构共享路径并保持叶片限制。
- `RopeNode.Delete` 对位于同一叶片或单个子节点内的删除操作复用写时复制路径，可直接移除或调整目标叶片，避免整树重建并自动折叠空子树。
- `RopeNode.Replace` 在单叶范围内组合删除与插入，并在容量允许时一次性写时复制；超限或跨子树时回退至拆分策略，减少双遍 Edit 成本。
- 单叶编辑触发超长时会通过 `LeafSplitter` 动态拆分为多个叶片，并在父节点内局部替换，避免重建整棵树，为阶段 B 的叶片容量控制提供落地基础。
- 新增叶片溢出拆分测试验证 Insert/Replace 在根节点与内部节点场景下生成合规叶片并保持兄弟节点内容稳定，为后续容量约束策略提供回归保障。
- Delete 场景下当叶片缩小到 `MinLeafSize` 以下时，`RopeNode` 会尝试与左右兄弟合并以保持叶片容量，并通过新增单元测试覆盖该行为。
- 内部节点聚合（`CreateInternal`）仍采用简单的 Child-Height/Length 聚合逻辑，`Concat`/`AppendNode` 等函数依赖高度匹配与局部合并行为，但不会主动执行 B-tree 风格的分裂/合并或再平衡，需要补充以确保长期健康的高度约束与最坏情形下的 O(log n) 行为。
- 叶片当前以 `string` 存储，这实现简单但在大文本或频繁修改下可能产生大量 GC/内存复制，长期目标是评估并迁移到 `char[]`/`ArrayPool<char>` 或 `ReadOnlyMemory<char>` 以减少分配压力并支持零拷贝切片。
- `RopeInfo`/Metric 体系（`Base/Lines/Utf16`）已实现并用于聚合 `Line`/`Utf16Length` 等指标；这些指标是 `prev/next`、多坐标系遍历和增量通知的基础，必须在任何写时复制或再平衡流程中保持一致性。

(小结) 目前已完成基础的结构共享路径改造（SplitAt + 编辑重写），下一阶段将把实现从“功能正确”转向“性能与长期稳定性”，通过写时复制、叶片容量限制与再平衡保证 O(log n) 性能边界。

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
- **Rope COW 阶段推进**：按《rope-cow-rebalance-plan》阶段 A/B 落实节点局部更新与叶片写时复制策略，确保结构共享在插入/删除中正确生效。
- **再平衡策略筹备**：为阶段 C/D 收集内部节点借用/合并案例与现有 `Concat`/`TreeBuilder` 行为，明确需要调整的入口点与聚合信息更新流程。
- **Delta/Subset 原型**：依据 `docs/architecture/rope-delta-notes.md` 制定 C# 迁移步骤，先实现最小 `Delta`/`Subset` 类型与 `factor()`、`summary()`、坐标重映射流程，为撤销与插件同步奠定基础。
- **行为对照与测试资产**：整理 `reference/rust/core-lib` 中的经典操作序列，规划引入 xUnit 测试或 trace，支撑 Rope 与 Delta 行为比对。

## 下一步行动（高优先级 Backlog）
1. **阶段 A：节点所有权与引用复用**
  - 在现有 `WithChildReplaced` 基础上落地 `RopeNode` 的引用状态检查与调试断言，梳理共享子树的生命周期。
  - 继续扩展 `CloneWithModifiedChildren`/`EnsureWritableLeaf` 在删除、替换流程中的应用，确保所有常见编辑操作都能绕开整树重建。
2. **阶段 B：叶节点写时复制与容量约束**
  - 实现叶片分裂/合并流程（遵循 `MIN_LEAF`/`MAX_LEAF` 约束），并在插入/删除/替换导致超限时自动拆分，保持父节点结构共享。
  - 编写跨叶编辑测试和 surrogate 对齐测试，验证结构共享下的长度、行计数、UTF-16 指标。
  - 扩展已落地的删除场景合并逻辑，覆盖借用、替换等路径并确保在不同父节点情况下同样生效。
3. **阶段 C：内部节点再平衡**
  - 设计并实现借用/合并/分裂操作，在 `Concat`、`CreateInternal`、`TreeBuilder` 中挂接。
  - 构造顺序/随机大规模编辑测试，确保树高度保持在对数级。
4. **阶段 D：聚合信息增量更新**
  - 引入沿父链的 `RefreshInfoUpwards`，避免全树重算。
  - 针对 Metric（Base/Lines/Utf16）补充断言与回归测试。
5. **阶段 E：测试与诊断扩展**
  - 增强 xUnit 测试覆盖（共享引用、借用、合并、分裂、重复编辑）。
  - 评估 FsCheck/属性测试引入成本，计划随机编辑序列验证。
6. **阶段 F：性能基线与基准**
  - 搭建 `BenchmarkDotNet` 基准（顺序插入、随机编辑、批量删除）。
  - 记录 COW/再平衡前后的时间与内存数据，形成对比报告并沉淀至文档。
7. **Delta/Subset 原型推进**
  - 按 `rope-delta-notes` 的迁移路径实现最小 `Delta`/`Subset` 类型，串联 `factor()`、`summary()`、`apply()`。
  - 与 Rope 优化后的结构集成测试，确保编辑语义一致。
8. **文档同步与风险跟踪**
  - 随阶段推进更新 `rope-cow-rebalance-plan.md`、`module-migration-plan.md`，在“决策 & 假设”中记录参数调整。
  - 梳理 COW/再平衡执行过程中的风险点与缓解策略。

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
- 写时复制（COW）实现细节：如何在不引入复杂并发/锁问题的前提下复用节点（通过不可变结构与引用复用），以及是否需要引用计数或弱引用池来管理共享节点生命周期。
- GC/内存压力：目前叶片是 `string`，内存复制风险在大文本与频繁编辑中更明显；需要设计并比较 `char[]+ArrayPool` 与 `string` 实现的折中。
- 再平衡算法的工程复杂性：与 Rust 的细节对齐需要时间，优先以正确、可测且渐进优化的方式实现功能而不是追求一次性完美。

## 已完成事项
- **工程骨架与测试基线（2025-11-11）**：建立 `.NET 9` 解决方案骨架（`Xi.Editor.sln`），创建 `xi.Core`/`xi.Core.Tests` 并通过首轮 `dotnet test` 验证基础编译与测试链路。
- **架构规划资产（2025-11-11）**：产出 `docs/architecture/xi-core-structure.md`、`module-migration-plan.md` 与 `api-contract.md`，梳理迁移路线、API 契约和阶段目标；同步撰写《Xi.Editor 迁移目标与路线图》确定阶段里程碑。
- **Rope/Delta 研究成果（2025-11-11）**：整理 `reference/rust` 资料并形成 `rope-delta-notes.md`，明确 Rope/Delta 迁移要点与后续实施参考。
- **Rope 基础实现（2025-11-11）**：引入 `ITextBuffer` 契约、`RopeInfo` 与 Metric 体系，完成 `RopeNode`、`TreeBuilder` 与 `RopeTextBuffer` 最小可用实现及配套测试，支持切片、插入、删除、替换等核心操作。
- **结构共享与写时复制迭代（2025-11-11）**：实现 `SplitAt`、`WithChildReplaced`、`CloneWithChildren`、`LeafSplitter` 等能力，优化 `Insert`/`Delete`/`Replace` 快速路径与叶片容量控制，并补充测试覆盖，确保 35 项 Rope/TextBuffer 测试全部通过。
- **策略文档与后续计划（2025-11-11）**：发布《Rope 写时复制与再平衡实施方案草案》，更新 `AGENTS.md` 关键认知与下一步行动，明确 COW/再平衡/Delta/Benchmark 推进路线。

## 工作日志
### 2025-11-11
- 初始化跨会话文档框架，整理目标与初步计划。
- 搭建 .NET 解决方案骨架，创建核心/测试项目，编写 `TextBuffer` 占位实现与基础测试并验证通过。
- 执行 `dotnet test`（默认配置）确认核心与测试项目编译与单元测试通过。
- 阅读 `reference/rust/core-lib` 与 `reference/rust/rope` 关键入口文件，编写架构梳理文档初稿。
- 解析 `editor.rs`、`tabs.rs` 并在架构文档中补充编辑命令、插件消息与 idle 调度流程描述。
- 编写模块级迁移路线图草案，梳理阶段任务、完成判据与风险策略。
- 整理命令/通知/插件交互契约并形成 `api-contract` 文档。
- 调研 `reference/rust/rope` 与 `rope_science` 文档，沉淀 Rope/Delta 迁移要点并成文。
- 实现 `ITextBuffer` 接口与 `TextBuffer` 更新，补充长度/切片测试并验证通过。
- 实现 `RopeInfo`、Metric 抽象与对应测试，建立 Rope 迁移所需的基础类型。
- 实现 `RopeNode`/`TreeBuilder` 初版与单元测试，验证叶节点拼接、长文本拆分及遍历正确性。
- 实现 `RopeTextBuffer` 并通过接口级单元测试，奠定以 Rope 替换占位实现的基础。
- 扩展 `RopeNode`/`RopeTextBuffer` 替换与插入/删除操作，完善 `ITextBuffer` 契约并新增跨叶编辑测试。
- 执行 `dotnet test`（34 项 Rope/TextBuffer 测试）确认最新 Rope 编辑实现保持通过。
- 实现 `RopeNode.SplitAt` 并重构 `Slice`/`Insert`/`Delete`，让编辑操作复用写时拆分路径以减少冗余构建。
- 执行 `dotnet test`（35 项 Rope/TextBuffer 测试）确认最新实现保持通过。
- 更新 `AGENTS.md` 并补充 Next Steps，保持测试基线与文档一致。
- 设定本次会话阶段目标：产出 Rope 写时复制（COW）与再平衡实施方案草案，并列出对应的代码与测试拆解步骤。
- 撰写并提交《Rope 写时复制与再平衡实施方案草案》，梳理阶段拆解与关键 API 变更。
- 实现 `RopeNode.WithChildReplaced` 及对应单元测试，启动阶段 A（节点局部更新能力）的编码工作。
- 实现 `RopeNode.CloneWithChildren` 并补充叶节点防御性测试，推进阶段 A 的节点引用复用能力。
- 强化 `TreeBuilder` 切片策略，优先在换行处分段并保持 UTF-16 代理对完整，新增相关单元测试。
- 实现 `LeafSplitter`、`EnsureWritableLeaf` 与 `SplitLeafByBounds`，补齐叶节点复制/拆分测试，推进阶段 B 的叶片策略。
- 重构 `RopeNode.Insert`，在叶片容量满足条件时直接写时复制单叶并更新聚合信息，超限时回退到结构共享路径。
- 扩展 `RopeNode.Delete`，支持单叶/单子节点写时复制与子节点折叠，并新增覆盖测试。
- 实现 `RopeNode.Replace` 快速路径并将 `RopeTextBuffer.Replace` 切换为单次编辑流程，新增叶片编辑测试。
- 为 Insert/Replace 添加叶片拆分回退，局部替换父节点子数组并验证单元测试通过。
- 新增叶片溢出拆分回归测试，确保局部拆分策略在根节点与内部节点场景下表现稳定。
- 实现删除后叶片合并，以维持 `MinLeafSize` 约束，并补充相应单元测试验证合并与不合并分支。
- 执行 `dotnet test`（54 项 Rope/TextBuffer 测试）确认叶片合并逻辑与现有功能兼容。