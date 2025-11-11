## 跨会话记忆文档
本文档(`./AGENTS.md`)会伴随每个 user 消息注入上下文，是跨会话的外部记忆。完成一个任务、制定或调整计划时务必更新本文件，避免记忆偏差。

## 项目概览

## 当前关键认知
- Metric 体系（Base/Lines/Utf16）通过统一接口支持不同坐标转换；`prev/next` 等操作需跨叶处理断裂，C# 版本必须提供等效能力以避免重复扫描。
- Delta/Subset 组合支撑插入、删除与并发协作：`factor()` 拆分插入/删除，`transform_expand`/`synthesize` 完成坐标重映射，是撤销与插件同步的基础能力。
- C# 迁移需实现写时复制的节点管理（引用计数或复制策略），并评估 `string`、`char[]`、`ArrayPool<char>` 等叶节点承载方式以控制 GC 压力。
- 核心 API 设计需保持嵌入式调用友好，同时为 JSON-RPC/插件层预留事件与通知扩展点。
- 已对 `reference/rust/core-lib` 与 `reference/rust/rope` 的关键入口文件完成首轮梳理，输出 C# 子系统映射与迁移顺序初稿（`docs/architecture/xi-core-structure.md`），并配套 `docs/architecture/module-migration-plan.md`、`docs/architecture/api-contract.md`、`docs/architecture/xi-port-goals-roadmap.md` 与 `docs/architecture/rope-delta-notes.md` 等文档作为实施基线。
- `TreeBuilder` 与 `LeafSplitter` 负责将长文本切分为符合 `MaxLeafSize` 的片段，优先选择换行与代理对友好的边界；配合阶段 B 引入的 `NormalizeLeafMinimum()`，叶片欠载会通过局部合并/借用自动修复，当前仍缺少内部节点层面的整体再平衡策略。
- `Node` 已提供 `SplitAt`、`WithChildReplaced`、`CloneWithChildren`、`EnsureWritableLeaf`、`SplitLeafByBounds`、`NormalizeLeafMinimum()` 等结构共享/调节 API；局部编辑完成后会自动修复欠载叶片并保持 `[MinLeafSize, MaxLeafSize]` 约束，内部节点仍依赖简单聚合，后续需要阶段 C/D 的再平衡与增量刷新。
- 阶段 B（叶片容量与诊断）已收官，当前重点转向阶段 C：需要定义内部节点借用/合并/分裂策略，并评估 `Concat`、`TreeBuilder` 等入口生成失衡树时的调节方案，为保持树高稳定和聚合信息正确性做准备。
- `Node.ValidateInvariants` 可在测试中校验高度、聚合信息与叶片容量，并支持可选的最小叶片严格检查；`RopeTestHelpers.AssertInvariants` 已在单元测试中默认启用该校验。
- `Rope` 通过 `InternalsVisibleTo` 暴露 `DebugRoot`，测试层借此在缓冲区级别断言结构不变量，混合编辑序列覆盖默认开启。
- 最新一次 `dotnet test` 针对 `Xi.Editor.sln` 运行 66 项 Rope/TextBuffer 测试全部通过，新增跨层欠载与 surrogate 场景的诊断回归；在 `NormalizeLeafMinimum()` 引入后所有编辑路径均保持叶片容量约束，为后续 COW 阶段收尾与 Delta 原型验证提供回归基线。
- 已整理《Rope 移植方法论》（`docs/architecture/rope-porting-methodology.md`）与《Rope 文件级映射与类型翻译计划》（`docs/architecture/rope-port-mapping.md`），确认采用“先契约后实现”的分层移植策略，并建立 Rust→C# 文件映射与翻译范式。
- 已在 `ref-outline/rust/rope` 中通过脚本 `scripts/stub_rust_functions.py` 批量移除函数实现，仅保留类型与方法签名骨架，降低上下文压力以支撑接口映射阶段。
- `scripts/stub_rust_functions.py` 现支持递归遍历并输出 Markdown 骨架（方法体以 `...` 占位），默认写入 `docs/reference/rust-skeleton.md`，便于集中查阅 Rust 原始接口。

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
- **Rope COW 阶段推进**：启动阶段 C，聚焦内部节点借用/合并与再平衡设计，实现跨层编辑后仍保持树高与聚合信息稳定。
- **再平衡策略筹备**：收集 `Concat`、`TreeBuilder` 等入口的失衡案例，梳理需要调整的 API 与数据刷新路径，为阶段 C/D 做准备。
- **Delta/Subset 原型**：依据 `docs/architecture/rope-delta-notes.md` 制定 C# 迁移步骤，先实现最小 `Delta`/`Subset` 类型与 `factor()`、`summary()`、坐标重映射流程，为撤销与插件同步奠定基础。
- **行为对照与测试资产**：整理 `reference/rust/core-lib` 中的经典操作序列，规划引入 xUnit 测试或 trace，支撑 Rope 与 Delta 行为比对。

## 已完成行动
1. **阶段 A：节点所有权与引用复用**
  - 在现有 `WithChildReplaced` 基础上落地 `Node`的引用状态检查与调试断言，梳理共享子树的生命周期。
  - 继续扩展 `CloneWithModifiedChildren`/`EnsureWritableLeaf` 在删除、替换流程中的应用，确保所有常见编辑操作都能绕开整树重建。
2. **阶段 B：叶片容量与诊断收官（2025-11-11）**
  - 新增跨多层删除与 surrogate 边界替换测试，验证 `NormalizeLeafMinimum()` 能在编辑后自动修复欠载叶片并保持代理对完整性；`ValidateInvariants(true)` 现用于确认所有编辑路径维持 `[MinLeafSize, MaxLeafSize]` 约束。
    - `ValidateInvariants` 诊断输出增加节点路径上下文、叶片预览与子节点长度摘要，结合 `CollectInvariantIssues` 可在测试与调试中快速定位问题并输出详细日志。

## 下一步行动（高优先级 Backlog）
1. **阶段 C 启动：内部节点再平衡设计与实现**
  - 梳理 `Concat`、`CreateInternal`、`TreeBuilder` 产生的高度失衡案例，定义借用/合并/分裂的触发条件与算法草案。
  - 在 Node 层实现最小可用的内部节点 re-balance 操作，并配套顺序/随机大文本编辑测试验证树高与聚合信息正确性。
  - 评估并规划沿父链的最小聚合刷新策略，为后续增量更新打基础。
  - 参考 `rope-port-mapping.md` 的映射表与翻译范式，优先补齐 `tree.rs` 相关接口占位并对照 Rust 逻辑拆分具体实现任务。
2. **阶段 D 准备：聚合信息增量更新**
  - 设计 `RefreshInfoUpwards` 或等效机制，确保局部编辑后无需整棵树重算聚合。
  - 针对 Base/Lines/Utf16 三种 Metric 增加断言与差分测试，锁定潜在的聚合偏差。
3. **测试与诊断扩展**
  - 引入属性测试或随机编辑序列（可考虑 FsCheck）覆盖更多组合场景，并将不变量断言纳入测试基线。
  - 继续扩展诊断输出（现已包含路径上下文、叶片预览与子节点长度摘要），后续评估记录更精细的片段快照以缩短定位时间。
4. **性能与基准体系搭建**
  - 搭建 `BenchmarkDotNet` 基准，覆盖顺序插入、跨叶替换、大范围删除等典型场景。
  - 建立阶段性性能回归表，记录 COW/再平衡前后的延迟与内存占用，指导后续优化。
5. **Delta/Subset 原型推进**
  - 按 `rope-port-mapping.md` 中的映射表与翻译范式，先完成 `delta.rs` 类型/接口壳子移植，再实现 `factor()`、`summary()`、`apply()` 并与 Rope 缓冲区对接。
  - 构建端到端单元测试，验证 Delta 的应用结果与 Rope 文本状态保持一致。
6. **文档与风险跟踪**
  - 随阶段推进更新 `rope-cow-rebalance-plan.md`、`module-migration-plan.md` 与风险日志，记录参数调整与新假设。
  - 将新的诊断/基准结果同步到文档，保持团队对现状的统一认知。

## 未来候选事项（Backlog）
- 导入参考仓库中的黄金编辑 trace，构建跨语言对照测试套件。
- 探索 `char[]`/`ArrayPool<char>`/`ReadOnlyMemory<char>` 作为叶片存储的可行性，并评估对 GC 压力与性能的影响。
- 设计更完善的可观察性方案（结构摘要、调试可视化、Telemetry）以支撑规模化调试。
- 规划插件示例（echo、spellcheck 等）与最小 JSON-RPC 宿主，验证核心库的嵌入式 API 能力。
- 评估协同编辑/CRDT 功能的技术路线，明确所需的 Delta/Subset 扩展与一致性测试。

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
- **Rope 基础实现（2025-11-11）**：引入 `ITextBuffer` 契约、`RopeInfo` 与 Metric 体系，完成 `Node`、`TreeBuilder` 与 `Rope` 最小可用实现及配套测试，支持切片、插入、删除、替换等核心操作。
- **结构共享与写时复制迭代（2025-11-11）**：实现 `SplitAt`、`WithChildReplaced`、`CloneWithChildren`、`LeafSplitter` 等能力，优化 `Insert`/`Delete`/`Replace` 快速路径与叶片容量控制，并补充测试覆盖，确保 35 项 Rope/TextBuffer 测试全部通过。
- **策略文档与后续计划（2025-11-11）**：发布《Rope 写时复制与再平衡实施方案草案》，更新 `AGENTS.md` 关键认知与下一步行动，明确 COW/再平衡/Delta/Benchmark 推进路线。
## 工作日志
### 2025-11-11
- 初始化跨会话文档框架，整理目标与初步计划。
- 搭建 .NET 解决方案骨架，创建核心/测试项目，编写 `TextBuffer` 占位实现与基础测试并验证通过。
- 阅读 `reference/rust/core-lib` 与 `reference/rust/rope` 关键入口文件，编写架构梳理文档初稿。
- 解析 `editor.rs`、`tabs.rs` 并在架构文档中补充编辑命令、插件消息与 idle 调度流程描述。
- 编写模块级迁移路线图草案，梳理阶段任务、完成判据与风险策略。
- 整理命令/通知/插件交互契约并形成 `api-contract` 文档。
- 调研 `reference/rust/rope` 与 `rope_science` 文档，沉淀 Rope/Delta 迁移要点并成文。
- 实现 `ITextBuffer` 接口与 `TextBuffer` 更新，补充长度/切片测试并验证通过。
- 实现 `RopeInfo`、Metric 抽象与对应测试，建立 Rope 迁移所需的基础类型。
 - 实现 `Node`/`TreeBuilder` 初版与单元测试，验证叶节点拼接、长文本拆分及遍历正确性。
 - 实现 `Rope`并通过接口级单元测试，奠定以 Rope 替换占位实现的基础。
 - 扩展 `Node`/`Rope`替换与插入/删除操作，完善 `ITextBuffer` 契约并新增跨叶编辑测试。
- 执行 `dotnet test`（34 项 Rope/TextBuffer 测试）确认最新 Rope 编辑实现保持通过。
- 实现 `Node.SplitAt` 并重构 `Slice`/`Insert`/`Delete`，让编辑操作复用写时拆分路径以减少冗余构建。
- 执行 `dotnet test`（35 项 Rope/TextBuffer 测试）确认最新实现保持通过。
- 更新 `AGENTS.md` 并补充 Next Steps，保持测试基线与文档一致。
- 设定本次会话阶段目标：产出 Rope 写时复制（COW）与再平衡实施方案草案，并列出对应的代码与测试拆解步骤。
- 撰写并提交《Rope 写时复制与再平衡实施方案草案》，梳理阶段拆解与关键 API 变更。
- 实现 `Node.WithChildReplaced` 及对应单元测试，启动阶段 A（节点局部更新能力）的编码工作。
- 实现 `Node.CloneWithChildren` 并补充叶节点防御性测试，推进阶段 A 的节点引用复用能力。
- 强化 `TreeBuilder` 切片策略，优先在换行处分段并保持 UTF-16 代理对完整，新增相关单元测试。
- 实现 `LeafSplitter`、`EnsureWritableLeaf` 与 `SplitLeafByBounds`，补齐叶节点复制/拆分测试，推进阶段 B 的叶片策略。
- 重构 `Node.Insert`，在叶片容量满足条件时直接写时复制单叶并更新聚合信息，超限时回退到结构共享路径。
- 扩展 `Node.Delete`，支持单叶/单子节点写时复制与子节点折叠，并新增覆盖测试。
- 实现 `Node.Replace` 快速路径并将 `Rope.Replace` 切换为单次编辑流程，新增叶片编辑测试。
- 为 Insert/Replace 添加叶片拆分回退，局部替换父节点子数组并验证单元测试通过。
- 新增叶片溢出拆分回归测试，确保局部拆分策略在根节点与内部节点场景下表现稳定。
- 实现删除后叶片合并，以维持 `MinLeafSize` 约束，并补充相应单元测试验证合并与不合并分支。
- 实现替换后叶片合并，并新增对应的合并/非合并单元测试覆盖。
- 引入叶片借用（重新分配）逻辑，确保无法合并时仍可满足容量约束，并新增删除/替换/代理对齐测试。
- 新增 `ValidateInvariants` API 与复合编辑测试，用于校验 Rope 树高度、聚合信息与叶片容量的一致性。
- 执行 `dotnet test`（60 项 Rope/TextBuffer 测试）确认删除/替换合并与借用逻辑与现有功能兼容。
- 执行 `dotnet test`（61 项 Rope/TextBuffer 测试）确认新增不变量校验与单元测试通过。
- Node 单元测试新增 `AssertInvariants` 帮助方法，在核心编辑路径自动验证结构不变量，以提高回归侦测能力。
- `RopeTestHelpers.AssertInvariants` 现基于 `CollectInvariantIssues` 输出详细诊断信息，失败时直接在断言消息中呈现路径与预览，便于定位问题。
- `Rope` 测试更新为默认断言不变量，并新增混合编辑序列回归用例；当前 `dotnet test` 总数提升至 62 项。
- 将 `Rope` 暴露的 `DebugRoot` 纳入测试，并新增混合编辑序列回归用例，默认断言不变量。
- `Rope` 测试引入默认不变量校验与混合编辑序列回归，验证缓冲区层的写时复制行为。

### 2025-11-12
- `ref-outline/rust/rope` 复制源码已使用脚本化方式统一替换函数体为 `todo!()` 占位，便于后续聚焦类型对齐；新增 `scripts/stub_rust_functions.py` 用于批量化处理。
- 扩展 `scripts/stub_rust_functions.py` 支持 Markdown 导出与递归路径处理，并首次生成 `docs/reference/rust-skeleton.md` 汇总 Rope 模块骨架。