## 跨会话记忆文档
本文档(`./AGENTS.md`)会伴随每个 user 消息注入上下文，是跨会话的外部记忆。完成一个任务、制定或调整计划时务必更新本文件，避免记忆偏差。

## 已知的工具问题
- 需要要删除请用改名替代，因为环境会拦截删除文件操作。
- 不要使用'insert_edit_into_file'工具，经常产生难以补救的错误结果。

## 用户语言
请主要用简体中文与用户交流，对于术语/标识符等实体名称则不不受限制。

## 项目概览
- 最新一次 `dotnet test` 针对 `Xi.Editor.sln` 运行 102 项测试全部通过，涵盖 Rope/TextBuffer/`StringLeafOperations`、Rope Metric 互操作等路径，确保 Leaf Helper 抽象与度量转换回归基线稳定。
- C# 序列化镜像 Stage C 现已交付：`Engine`/`Revision`/`RevisionOperation` 不可变镜像与 `EngineJson` 序列化器落地，`engine_regression.json` 黄金串与 `EngineSerializationTests` 纳入基线并通过全量 `dotnet test` 验证。
- Rust/C# 双端已通过 `SharedNode` 封装收敛写时复制触点，`tree.rs` 与 `Tree/Node.cs` 现统一委托 `EnsureUnique/CloneWithChildren/ReplaceChildRange`；`cargo test -p xi-rope` 与 `dotnet test tests/xi.Core.Tests` 保持通过。
- `StringLeafOperations` 已抽离叶片编辑、合并与再平衡所需的字符串逻辑，并配套 81 项测试基线，正在为泛型 `Node` 铺设叶操作 Helper；同时重构为实现 `ILeafOperations<string>` 的静态抽象 Helper，为后续泛型节点直接复用。
- `docs/architecture/port-blueprint.md` 与 `docs/architecture/rope-port-mapping.md` 已整合原架构/协同草案；配套的 C#/Rust 专题方案现分别归档于 `docs/csharp-refactor/` 与 `docs/rust-refactor/`，持续记录互锁里程碑、协同 helper 清单与风险登记。
- 已在 `ref-outline/rust/rope` 中通过脚本 `scripts/stub_rust_functions.py` 批量移除函数实现，仅保留类型与方法签名骨架，降低上下文压力以支撑接口映射阶段。
- `scripts/stub_rust_functions.py` 现支持递归遍历并输出 Markdown 骨架（方法体以 `...` 占位），默认写入 `docs/reference/rust-skeleton.md`，便于集中查阅 Rust 原始接口。
- Markdown 骨架在生成前会自动移除 `#[cfg(test)]` / `#[test]` 标记的测试项以及文件头/行级注释，当前 `docs/reference/rust-skeleton.md` 缩减至约 2k 行，便于快速检索关键信息。
- 引入 `src/xi.Core/Rope/Interval.cs` 以及 `TreeContracts.cs` 中的 `ILeafOperations`、`ITreeNodeInfo`、`ITreeMetric` 等接口，完成 rope/tree 模块的核心契约映射；`RopeInfo` 与三种 Metric 已对齐新接口，`IMetric` 成为 `ITreeMetric<string, RopeInfo>` 的特化别名，并新增 `NodeCursor` 骨架为后续游标实现预留结构。
  - 与 Rust 侧同步 `transform_expand`/`factor` 等 helper 的拆分节奏，在 `docs/architecture/port-blueprint.md` 中追踪依赖状态，必要时以临时 stub 解锁 C# 侧验证。
- 通过 ILSpy 导出 + 摘要化处理生成 `docs/skeleton/xi.Core.Rope.cs`，现可与 `docs/skeleton/rope.md` 对照查看 Rust/C# 两侧的类型骨架，用于统一接口设计与差异审视。

## 工作节奏建议
当前仅由人类开发者与 AI Coder 协作，执行节奏按单次 AI 会话推进；每次会话收尾前需同步更新本文件与相关计划文档。
1. **进入仓库**：优先阅读“当前聚焦事项”，确认阻塞与最新决策，必要时调整计划。
2. **执行任务**：按优先级推进，并实时更新“当前聚焦”“技术笔记”“风险”。
3. **任务完成**：将成果移动至“已完成事项”，在“工作日志”记录关键行动，检视下一步。
4. **质量门禁**：所有代码改动前后需跑通构建/测试，并记录结果。

### 协作与工具心得
- 充分利用 IDE/GUI 工具加速批量操作（如重命名、导航、格式化等），必要时可直接请人类协作者协助执行；相比 RL 阶段的“独立作业”要求，当前环境鼓励主动寻求外部工具/伙伴配合以提升效率。
- `grep_search` 适合作为 `rg` 的轻量替代，按 `query`/`includePattern` 组合即可精准过滤文件，复杂模式时记得设置 `isRegexp=true`；`list_code_usages` 可直接询问 LSP，传入 `symbolName` 与候选定义文件就能获得 Rust 引用/实现清单，优先使用这两项工具再考虑手动 `read_file` 或终端检索。

## Rust一侧设计原则：
  1. 执行期行为尽量接近原版xi-editor，但允许推迟析构Drop。
  2. 为易于被移植为C#实现，尽量避免rust特有语言特性的使用，用等效或近似设计模式替代。
  3. 在满足前两点的基础上，尽量化简设计。
  4. 此fork的本质是为C#移植提供设计范本，而非向rust社区提供用于执行的库，因此只要有利于移植就不在乎warning。

## C#一侧设计原则：
  1. 最终质量优先，不害怕重构，甚至彻底推到重来也行。
  2. 在类型骨架/接口的设计上，尽量“无脑”对齐rust版，目的是分层施工：先让C#版对齐骨架，再对位移植每个局部。缓解AI会话上下文窗口压力。
  3. 非必要的难以移植的功能，可以先砍掉。

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
- **C# 序列化镜像 Stage D（进行中）**：本会话交付共享夹具刷新手册、CI 集成策略与文档同步准则，持续保持 Rust/C# 黄金资产一致，并跟踪后续自动化落地；`serde_fixtures` 模块集中存放黄金 JSON，`export-serde-fixtures` CLI 与 `scripts/refresh_serialization_fixtures.ps1` 串联 Rust 校验与 `dotnet test`，已验证 `-SkipRust -SkipDotnet` 流程可复写夹具而无副作用。
- **Rope 字符串 helper 对拍筹备**：随着 Rust `helpers/string_leaf.rs` 抽离完成，需要在 C# `StringLeafOperations` 与文档中持续标注 UTF-8 byte vs UTF-16 `char` 偏移差异，并策划跨语言拆分窗口 parity 测试。
- **Grapheme 导航降级策略**：C# 初版仅保证不拆分 surrogate，对上下文最多补一片并在不足时退回 code point；接口预留上下文抽象，待真实需求驱动再评估 Rust trace/ICU 方案。
- **设计分歧登记**：新增 `docs/architecture/design-divergence-log.md` 统一记录与 Rust 不对齐的策略，让后续复盘、差异追踪与升级决策有据可依。
- **Rust Workspace 精简**：全局 MSRV 已提升至 1.75，Criterion bench 与 legacy crate 已迁出，`xi-core-lib` 引入可禁用的 `trace` 特性用于未来脱离 `xi-trace`；`PluginLoadError` dead code、硬链接告警与 `serde_test` future incompat 已清零（新增 `.cargo/config.toml` 禁用增量编译并将 `serde_test` 升级至 1.0.177），接下来关注 trace shim 覆盖。
- **Skeleton 对齐与计划固化**：基于 `docs/skeleton/rope.md` 与 `docs/skeleton/xi.Core.Rope.cs` 逐项比对类型与接口，补齐差异并把最新目标写入外部文档，确保上下文压缩后仍能快速恢复全局视图。
- **双向协同跟踪**：维护 `docs/architecture/port-blueprint.md` 的协作章节与 `docs/architecture/rope-port-mapping.md`，实时同步 Rust 端 helper 拆分、测试夹具导出与脚本资产状态，确保文档与实现双向更新。
- **叶操作抽象过渡**：依托 `StringLeafOperations` 梳理叶片合并、再平衡、`NormalizeLeafMinimum()` 等路径，为泛型 `Node` 需要的 Helper 能力与测试覆盖做前置验证。
  - 新增 `ILeafOperations.SplitByCapacity()` 静态抽象方法并由 `StringLeafOperations` 实现，`LeafSplitter` 现委托 Helper，便于泛型节点直接调用统一的叶片拆分逻辑。
- **Rope COW 阶段推进**：启动阶段 C，聚焦内部节点借用/合并与再平衡设计，实现跨层编辑后仍保持树高与聚合信息稳定。
- **SharedNode 诊断筹备**：在 Rust/C# `SharedNode` 封装完成后，评估调试计数器与性能探针的可行性，为跨语言共享节点回归提供 instrumentation。
- **再平衡策略筹备**：收集 `Concat`、`TreeBuilder` 等入口的失衡案例，梳理需要调整的 API 与数据刷新路径，为阶段 C/D 做准备。
- **Delta/Subset 原型**：依据 `docs/csharp-refactor/rope-delta-notes.md` 制定 C# 迁移步骤，先实现最小 `Delta`/`Subset` 类型与 `factor()`、`summary()`、坐标重映射流程，为撤销与插件同步奠定基础。
  - `docs/rust-refactor/delta-subset-serialization.md` 已细化 serde 拆分四阶段计划（基线采样、`Subset` 模块化、`Delta` helper、`Engine` ledger serde），并约定 golden fixture、双轨 CI 与 Fuchsia ledger 校验作为成功标准。
  - Stage 0/1 已完成 Subset 拆分试点：`subset_serialization_regression` 锁定 JSON 基线，核心实现新增 `Subset::segment_triples()`/`from_segment_triples()` helper 并将 serde 实现在 gated 模块，`cargo test -p xi-rope --features serde` 与 `--no-default-features` 均通过。
  - Stage 2 已完成 Delta 重构：新增 `Delta::base_len()`/`iter_elements()` 等 helper，`serde_impls.rs` 改为手写序列化并复用新 helper，同时 `delta_serialization_regression` 固化 JSON 产物；当前准备进入 Stage 4 的 feature flag 与文档收尾。
  - Stage 3（Engine）现已落地：核心类型移除 serde derive，新增 `Engine::revision_log()`、`Engine::from_serialized_state()`、`RevisionRef` 等 `pub(crate)` helper，并在 `engine::serde_impl` 内手写 `Serialize/Deserialize`。`engine_serialization_regression` 锁定 JSON 基线（fixture 与文档同步），`cargo test --manifest-path rope/Cargo.toml --features serde` 与 `--no-default-features` 均通过。
  - Stage 4（Feature flags & docs）正在收尾：`xi-core-lib` 新增显式 `serde` 特性将 `xi-rope/serde` 设为按需启用，工作区顶层提供对应开关；`rust/run_all_checks` 追加 `cargo test -p xi-rope` 在 `--no-default-features` 与 `--features serde` 两种模式的运行。后续需关注 CI 流水线是否同步采用新命令。
- **行为对照与测试资产**：整理 `reference/rust/core-lib` 中的经典操作序列，规划引入 xUnit 测试或 trace，支撑 Rope 与 Delta 行为比对。
- **Trait 泛型化延伸**：`NodeInfo`、`TreeBuilder`、`Delta` 等核心模块已完成显式叶类型泛型化并通过 `cargo test -p xi-rope`，当前聚焦在 Rust 端梳理 Cursor/Iterator 的生命周期依赖，同时指导 C# `Node<TInfo, TLeaf, TLeafOps>` 的落地与测试补位。
- **C# 泛型 Node 对齐准备**：根据最新骨架与 `docs/csharp-refactor/node-generic-refactor-plan.md`，规划将实验版泛型节点迁入主实现并串联 81 项 Rope 测试，记录仍依赖字符串特化的调用点与阻塞。

## 已完成行动
1. **阶段 A：节点所有权与引用复用**
  - 在现有 `WithChildReplaced` 基础上落地 `Node`的引用状态检查与调试断言，梳理共享子树的生命周期。
  - 继续扩展 `CloneWithModifiedChildren`/`EnsureWritableLeaf` 在删除、替换流程中的应用，确保所有常见编辑操作都能绕开整树重建。
2. **阶段 B：叶片容量与诊断收官（2025-11-11）**
  - 新增跨多层删除与 surrogate 边界替换测试，验证 `NormalizeLeafMinimum()` 能在编辑后自动修复欠载叶片并保持代理对完整性；`ValidateInvariants(true)` 现用于确认所有编辑路径维持 `[MinLeafSize, MaxLeafSize]` 约束。
    - `ValidateInvariants` 诊断输出增加节点路径上下文、叶片预览与子节点长度摘要，结合 `CollectInvariantIssues` 可在测试与调试中快速定位问题并输出详细日志。
3. **SharedNode Helper 双端封装（2025-11-14）**
  - 在 Rust `tree.rs` 中引入 `SharedNode` 封装，将所有 `Arc::make_mut` 调用集中到 `ensure_unique`，并通过 `clone_with_children`、`replace_child_range` 复用子节点拼接逻辑；`cargo test -p xi-rope` 完整通过。
  - C# `Tree/Node.cs` 采用对等的 `SharedNode` 内部类型，`EnsureUnique/CloneWithChildren/ReplaceChildRange` 成为唯一写时复制入口，`dotnet test tests/xi.Core.Tests`（81 项）通过验证。
  - 更新 `docs/rust-refactor/shared-node-api.md`、`docs/skeleton/xi.Core.Rope.cs` 与 `AGENTS.md`，记录 helper 封装完成与后续诊断计划。
  4. **Metrics Helper 模块化（2025-11-14）**
    - 在 Rust `rope/src/metrics/` 下新增 `codepoint`、`lines`、`break_indices`、`identity` 模块，抽离 UTF-8 边界、换行定位、Breaks 索引与 Base 单位包装逻辑；`LinesMetric`/`BreaksMetric`/`Utf16CodeUnitsMetric` 统一改用 helper。
    - 保留 `rope.rs` 里的 `count_newlines`/`count_utf16_code_units` shim 以兼容其他 crate，并在 `docs/architecture/rope-port-mapping.md` 记录新 helper 与 C# 对映；`cargo test -p xi-rope`、`dotnet test tests/xi.Core.Tests` 全部通过。
    - 刷新 `docs/skeleton/*.md` 以反映新的模块布局，确保跨语言映射表及时更新。
    - C# 侧新增 `BreaksMetricHelper`（`src/xi.Core/Rope/BreaksMetricHelper.cs`）复刻零分配查找语义，并配套 `BreaksMetricHelperTests` 验证空集、重复断点与越界行为，保持与 Rust helper 同步。
  5. **Cursor 缓存 Phase 1/2 基础设施（2025-11-15）**
    - 在 Rust `tree.rs` 完成 `CursorDescriptor`（Phase 1）并新增 round-trip 与失效测试，`docs/rust-refactor/CursorCache.md`、`docs/architecture/rope-port-mapping.md` 将游标阶段标记为“已完成/进行中”。
    - 引入可选 `cursor_state` 特性实现借用-free `CursorState` 与 `Cursor::state()`（Phase 2），为深层路径与编辑后同步补充单元测试，`cargo test -p xi-rope` 与 `cargo test -p xi-rope --features cursor_state` 均通过。
    - `Cargo.toml`、`lib.rs` 条目与文档同步更新，并在下一阶段为轻量 instrumentation 与 C# 侧接入预留待办。

## 下一步行动（高优先级 Backlog）
1. **Stage D 共享资产同步**
  - 将 `run_all_checks` serde/无 serde 与 `dotnet test` 集成至统一 CI 节点，并补充失败回溯策略。
  - 定期运行 `scripts/refresh_serialization_fixtures.ps1`（现接入 `export-serde-fixtures`）验证 CLI 导出与测试链路；最新一次以 `-SkipRust -SkipDotnet` 方式确认覆写流程稳定。
  - 设定夹具更新的审核 checklist：Rust 输出确认、C# 测试、文档刷新与 AGENTS 日志同步。
1. **Node 泛型双向同步**
  - Rust：在 `docs/csharp-refactor/node-generic-refactor-plan.md` 标注已完成的 NodeInfo/TreeBuilder/Delta 泛型化成果，梳理剩余 API 差异并补充对照表。
  - C#：将实验版 `Node<TInfo, TLeaf, TLeafOps>` 包装层接入主实现，串联 81 项 Rope 测试并记录尚需字符串特化的调用点与阻塞。
  - 文档：刷新 `docs/skeleton/rope.md` 与 `docs/skeleton/xi.Core.Rope.cs`，确保签名与 helper 名称同步更新。
2. **Cursor 生命周期重构（Descriptor + State）**
  - 按 `docs/rust-refactor/CursorCache.md` 的组合策略推进：Rust `CursorDescriptor`（Phase 1）已完成并配套 round-trip 测试，文档标记为“已完成”。
  - `cursor_state` 可选特性现已提供借用-free `CursorState` 与 `Cursor::state()`（Phase 2），新增 Base/Lines/Utf16 导航对拍测试验证启用/禁用模式语义一致；后续按需以轻量 instrumentation 观察热点，再评估默认启用时机。
  - C# 侧扩展 `NodeCursor`（Phase 3）以消费 Descriptor/State，并补充共享 JSON fixture 的回归测试；文档更新同步至 `port-blueprint` 与 `node-generic-refactor-plan.md`。
3. **SharedNode 诊断与性能监测**
  - 设计 Rust 侧 `shared_node_diagnostics`（或等效）特性开关，统计 `ensure_unique`、`clone_with_children` 调用，并输出最小计数器用于测试与日志分析。
  - 规划 C# 侧调试计数器与 Rust instrumentation 的对齐，确保跨语言回归可比较共享节点复制开销。
  - 在 `docs/rust-refactor/shared-node-api.md`、`docs/architecture/rope-port-mapping.md` 记录诊断字段、命名与测试入口，防止后续 helper 演化偏离对齐目标。
  - 评估 instrumentation 对性能的影响，必要时增加微基准验证开关前后差异。
4. **Rust 基线瘦身后续（后 MSRV）**
  - 阶段 1（bench 停靠、非核心 crate 削减、文档与脚本更新）已完成，当前聚焦 trace shim 覆盖与 `.cargo/config` 配置的后续影响监测。
  - 为未来在 C# 端复刻的测试/示例列出映射清单，并在 docs 中记录 Rust 仅存资产的作用。
  - 逐步为 `xi-plugin-lib` 等仍引用 `xi-trace` 的 crate 添加可选特性或 shim，确保核心子集可在无 trace 依赖下编译。
5. **叶操作抽象巩固**
  - 已将叶片合并与再平衡所需的字符串处理迁移至 `StringLeafOperations`，并让其实现静态抽象 `ILeafOperations<string>` 接口；继续盘点剩余 string 特化（诊断、快照等），并规划泛型 Helper 最终接口。
  - 盘点 `Node` 中仍直接操作 `string` 的调用点，映射到未来 `ILeafOperations` 所需的接口能力，并同步更新 `docs/csharp-refactor/node-generic-refactor-plan.md`。
  - 扩展现有单元测试覆盖（合并、拆分、借用）以及异常路径，确保 Helper 行为可独立验证。
6. **阶段 C 启动：内部节点再平衡设计与实现**
  - 梳理 `Concat`、`CreateInternal`、`TreeBuilder` 产生的高度失衡案例，定义借用/合并/分裂的触发条件与算法草案。
  - 在 Node 层实现最小可用的内部节点 re-balance 操作，并配套顺序/随机大文本编辑测试验证树高与聚合信息正确性。
  - 评估并规划沿父链的最小聚合刷新策略，为后续增量更新打基础。
  - 参考 `docs/architecture/rope-port-mapping.md` 的映射表与翻译范式，优先补齐 `tree.rs` 相关接口占位并对照 Rust 逻辑拆分具体实现任务。
7. **阶段 D 准备：聚合信息增量更新**
  - 设计 `RefreshInfoUpwards` 或等效机制，确保局部编辑后无需整棵树重算聚合。
  - 针对 Base/Lines/Utf16 三种 Metric 增加断言与差分测试，锁定潜在的聚合偏差。
8. **测试与诊断扩展**
  - 引入属性测试或随机编辑序列（可考虑 FsCheck）覆盖更多组合场景，并将不变量断言纳入测试基线。
  - 继续扩展诊断输出（现已包含路径上下文、叶片预览与子节点长度摘要），后续评估记录更精细的片段快照以缩短定位时间。
9. **性能与基准体系搭建**
  - 搭建 `BenchmarkDotNet` 基准，覆盖顺序插入、跨叶替换、大范围删除等典型场景。
  - 建立阶段性性能回归表，记录 COW/再平衡前后的延迟与内存占用，指导后续优化。
10. **Delta/Subset 原型推进**
  - 按 `docs/architecture/rope-port-mapping.md` 中的映射表与翻译范式，先完成 `delta.rs` 类型/接口壳子移植，再实现 `factor()`、`summary()`、`apply()` 并与 Rope 缓冲区对接。
  - 构建端到端单元测试，验证 Delta 的应用结果与 Rope 文本状态保持一致。
  - 与 Rust 侧同步 `transform_expand`、`factor` 等 helper 拆分节奏，在 `docs/architecture/port-blueprint.md` 中追踪依赖状态，必要时以临时 stub 解锁 C# 验证。
11. **文档与风险跟踪**
  - 随阶段推进更新 `docs/csharp-refactor/rope-cow-rebalance-plan.md` 与 `docs/architecture/port-blueprint.md` 的风险/任务章节，记录参数调整与新假设。
  - 将新的诊断/基准结果同步到文档，保持团队对现状的统一认知。
12. **文档外部记忆与计划维护**
  - 定期同步 `docs/skeleton/xi.Core.Rope.cs` 与 Rust skeleton 的差异标注，形成最新的对照清单。
  - 在 `AGENTS.md` 与架构文档中记录阶段目标、开放问题与请求协作的事项，避免上下文压缩造成的信息丢失。
  13. **Rope 泛型瘦身蓝图执行**
    - 依据 `docs/rust-refactor/rope-generic-simplification.md` 制定的 Phase 0-6 路线推进 trait 合并、Metric 瘦身与 Delta/Cursor 重构。
    - 每完成阶段需刷新 skeleton 文档并同步 C# 计划，必要时更新黄金夹具与测试脚本。

## 未来候选事项（Backlog）
- 导入参考仓库中的黄金编辑 trace，构建跨语言对照测试套件。
- 探索 `char[]`/`ArrayPool<char>`/`ReadOnlyMemory<char>` 作为叶片存储的可行性，并评估对 GC 压力与性能的影响。
- 设计更完善的可观察性方案（结构摘要、调试可视化、Telemetry）以支撑规模化调试。
- 规划插件示例（echo、spellcheck 等）与最小 JSON-RPC 宿主，验证核心库的嵌入式 API 能力。
- 评估协同编辑/CRDT 功能的技术路线，明确所需的 Delta/Subset 扩展与一致性测试。

## 摘要Agent提示
- 下次执行摘要时请突出：`StringLeafOperations` 已抽离叶片编辑/合并/再平衡逻辑，并配套 81 项测试基线，为泛型 `Node` 铺设叶操作 Helper；泛型节点骨架已建立并通过基础单元测试。
- 同步强调 Rust 端 `cursor_state` 可选特性已经引入 `CursorState`/`Cursor::state()`（Phase 2），已通过 Base/Lines/Utf16 导航对拍测试确认语义一致，后续仅需按需收集轻量指标以决定默认策略。
- 概述紧邻的短期计划（叶操作抽象巩固、泛型 Node 内核试验、阶段 C 再平衡设计），以便快速恢复上下文。
- 若摘要篇幅受限，优先保留关键认知列表中新添加的 Helper 与测试信息，其次是“下一步行动”前两项的执行要点。
- 若摘要需要压缩，也请提及 `docs/architecture/port-blueprint.md` 已对齐双向协同策略，并提醒跟进该文档中的协作依赖清单最新状态。

## 决策 & 假设日志
- [假设] 保持与 Rust 版相同的树/片段结构以便复用测试与算法描述。
- [假设] 优先通过单一 Solution 管理所有项目，便于构建脚本与 CI。
- [TODO] 后续记录更多架构决策（通道选型、序列化库、内存策略等）。
- [决策-2025-11-15] Grapheme 导航初版采用“单片 + 相邻片 + code point 回退”降级策略，后续是否追平 Rust 视实际需求与安全评估而定。

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

### API 迁移
- 2025-11-13：完成 iterator façade 可行性调研，`docs/rust-refactor/iterator-facade-export.md` 已列出候选 façade 签名、现有迭代器使用面与迁移步骤，后续可据此优先替换 `Delta::iter_*`、`Cursor::iter` 等调用。

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
- 文档与实现协同：若 `docs/architecture/port-blueprint.md` 中的“双向协同”计划未随 Rust helper/测试资产更新，将导致任务优先级判断失真，需要将该文档作为单一事实来源持续维护。
- `xi-editor-ph7` 子模块未在 `.gitmodules` 注册，`git submodule update`/`git restore` 等命令无法回滚至索引记录的 `89213f6`；若误切至远端 `master` 最新提交（如 `f600b85`），需手动 `git -C xi-editor-ph7 checkout 89213f6` 或补齐 `.gitmodules` 才能清理“modified: xi-editor-ph7 (new commits)” 状态。

## 已完成事项
- **Cursor 缓存 Phase 1/2 基础设施（2025-11-15）**：在 Rust `tree.rs` 中完成 `CursorDescriptor` 并引入可选 `cursor_state` 特性下的 `CursorState`/`Cursor::state()`，新增 round-trip、深层路径、编辑失效与 Base/Lines/Utf16 导航对拍测试，`cargo test -p xi-rope` 及 `cargo test -p xi-rope --features cursor_state` 均通过，同时刷新相关文档记录语义一致性与后续 C# 接入计划。
- **Rope 字符串 helper 模块化（2025-11-15）**：抽离 `MIN_LEAF`/`MAX_LEAF`/拆分策略至 `rope/src/helpers/string_leaf.rs` 并补充 newline 偏好、代理对安全、容量边界与 UTF-16 计数单元测试；`rope.rs` 改为复用 helper，库入口声明 `helpers` 模块，文档与 `AGENTS.md` 增补 UTF-8 byte vs UTF-16 `char` 偏移说明。
- **C# 序列化镜像 Stage C（Engine）（2025-11-14）**：交付不可变 `Engine`/`Revision`/`RevisionOperation` 类型与 `EngineJson` 序列化器，引入 `engine_regression.json` 黄金串及 `EngineSerializationTests`（序列化匹配、反序列化回写、`RevisionLog` 验证），同步更新 `docs/csharp-refactor/rope-cs-mirror-plan.md`、`docs/architecture/rope-port-mapping.md`、`AGENTS.md` 并执行 `dotnet test` 全量通过。
- **C# 序列化镜像 Stage B（Delta）（2025-11-14）**：交付 `Delta<TInfo, TLeaf>`/`DeltaElement`/`CopyElement`/`InsertElement` 骨架与 helper，完成 `DeltaJson` 序列化/反序列化并引入 `delta_regression.json` 黄金串、`DeltaSerializationTests`，`dotnet test`（含新增用例）通过，文档（`docs/csharp-refactor/rope-cs-mirror-plan.md`、`docs/architecture/rope-port-mapping.md`、`AGENTS.md`）同步更新。
- **工程骨架与测试基线（2025-11-11）**：建立 `.NET 9` 解决方案骨架（`Xi.Editor.sln`），创建 `xi.Core`/`xi.Core.Tests` 并通过首轮 `dotnet test` 验证基础编译与测试链路。
- **架构规划资产（2025-11-11）**：产出初版 `xi-core-structure.md`、`module-migration-plan.md` 与 `api-contract.md`（现已整合至 `docs/architecture/port-blueprint.md` 及相应专题文档），梳理迁移路线、API 契约和阶段目标；同步撰写《Xi.Editor 迁移目标与路线图》确定阶段里程碑。
- **Rope/Delta 研究成果（2025-11-11）**：整理 `reference/rust` 资料并形成 `docs/csharp-refactor/rope-delta-notes.md`（原分散草案已并入此文件），明确 Rope/Delta 迁移要点与后续实施参考。
- **Rope 基础实现（2025-11-11）**：引入 `ITextBuffer` 契约、`RopeInfo` 与 Metric 体系，完成 `Node`、`TreeBuilder` 与 `Rope` 最小可用实现及配套测试，支持切片、插入、删除、替换等核心操作。
- **结构共享与写时复制迭代（2025-11-11）**：实现 `SplitAt`、`WithChildReplaced`、`CloneWithChildren`、`LeafSplitter` 等能力，优化 `Insert`/`Delete`/`Replace` 快速路径与叶片容量控制，并补充测试覆盖，确保 35 项 Rope/TextBuffer 测试全部通过。
- **策略文档与后续计划（2025-11-11）**：发布《Rope 写时复制与再平衡实施方案草案》（现归档于 `docs/csharp-refactor/rope-cow-rebalance-plan.md`），更新 `AGENTS.md` 关键认知与下一步行动，明确 COW/再平衡/Delta/Benchmark 推进路线。
## 工作日志
### 2025-11-15 (Design Divergence Log)
- 创建 `docs/architecture/design-divergence-log.md`，首批登记 UTF-16 叶片与 Grapheme 降级两项与 Rust 的刻意差异。
- 在 `AGENTS.md` 当前聚焦事项中加入“设计分歧登记”提醒，为后续新增差异提供唯一记录入口。
- 约定后续每次新增差异时同步更新该日志并在里程碑复盘。
### 2025-11-15 (Stage D Fixture Consolidation)
- 分别运行 `cargo test -p xi-rope --features serde subset_serialization_regression`, `cargo test -p xi-rope --features serde delta_serialization_regression` 与 `cargo test -p xi-rope --features serde engine_serialization_regression`，确认 `serde_fixtures` 常量与回归预期一致。
- 执行 `cargo run -p xi-rope --features serde --bin export-serde-fixtures -- --dir tests/xi.Core.Tests/Fixtures` 并通过 `scripts/refresh_serialization_fixtures.ps1 -SkipRust -SkipDotnet -Verbose` 验证 CLI 覆写路径无副作用。
- 更新 `docs/csharp-refactor/rope-serialization-fixture-playbook.md`，强调脚本参数与 exporter 流程，记录最新操作指引。
- 同步 `AGENTS.md` 反映 Stage D 验证结果与后续动作。
- 校对 `docs/architecture/port-blueprint.md` 的模块映射表，标记 `Interval` 结构与 Subset/Delta/Engine JSON 转换器已完成功能，对齐当前实现状态。
- 进一步对照仓库现状修订 `docs/architecture/port-blueprint.md` 的映射章节，补充 `Node.Generic.cs`/`StringLeafOperations.cs` 等目标文件、将 `BreaksMetricHelper` 标记为已完成，并明确 `Diff/`、`Search/` 模块尚未建目录。
- 试用 `grep_search` 与 `list_code_usages` 检索 `Rope` 引用，确认 `grep_search` 能按 `includePattern` 与正则定位匹配，`list_code_usages` 能在 Rust 侧返回 400+ 个调用点，作为后续替代终端 `rg`/手动遍历的首选方案。
### 2025-11-15 (Cursor State Feature Gate)
- 在 `xi-editor-ph7/rust/rope/src/tree.rs` 引入可选 `cursor_state` 特性下的 `CursorState` 结构与 `Cursor::state()`，补齐状态重建、失效与路径同步逻辑。
- 更新 `Cargo.toml` 与 `lib.rs` 暴露新特性，并在 `rope/tests/cursor_descriptor.rs` 添加 `CursorState` round-trip、深层路径与编辑失效测试。
- 重跑 `cargo test -p xi-rope` 与 `cargo test -p xi-rope --features cursor_state` 确认默认/启用特性下均通过。
- 同步 `docs/rust-refactor/CursorCache.md`、`docs/architecture/port-blueprint.md`、`docs/architecture/rope-port-mapping.md` 标记 Phase 2 状态与后续轻量 instrumentation/ C# 接入要求，更新 `AGENTS.md` 记录。
### 2025-11-15 (Leaf Split Parity Samples)
- 新增 `tests/xi.Core.Tests/Fixtures/leaf_split_parity_samples.json`，收录 `newline_outside_char_window`（Rust 513-byte newline 命中 vs C# 64-char 默认拆分）与 `surrogate_guard_post_truncation`（Rust 尾端最小字节保护 vs C# 代理对回退）两组拆分对拍样本。
- 更新 `docs/rust-refactor/rope-generic-simplification-g.md` Phase 4 勾选状态与说明，并在 `docs/csharp-refactor/rope-cow-rebalance-plan.md` 新增 `Base Metric 对齐` 小节，记录拆分偏差案例、parity fixture 与后续自动化提示。
### 2025-11-15 (Rope String Helper Extraction)
- 将 `MIN_LEAF`/`MAX_LEAF`/拆分与 UTF-16 计数函数迁移至 `rope/src/helpers/string_leaf.rs`，为 helper 新增 newline 偏好、代理对安全、容量边界与 UTF-16 计数测试，并在 `rope.rs` 及 `rope/src/lib.rs` 接入新模块。
- 引入 `NEWLINE_WINDOW` 常量并调整拆分实现以使用统一窗口表达，加在测试中验证窗口范围，避免未使用警告。
- 运行 `python scripts/refresh_skeleton_docs.py --verbose` 刷新 skeleton；执行 `cargo test -p xi-rope --manifest-path xi-editor-ph7/rust/Cargo.toml` 以及分别针对 `subset_serialization_regression`、`delta_serialization_regression`、`engine_serialization_regression` 的 serde 回归测试，全部通过（仅保留增量构建硬链接告警）。
- 针对 `diff::tests::test_larger_diff` 触发的 `MAX_LEAF` 超限 panic，收紧 `find_leaf_split` 的上下界并保留换行优先策略，重跑 `cargo test -p xi-rope` 与 serde 回归全部通过。
- 更新 `docs/architecture/rope-port-mapping.md`、`docs/csharp-refactor/node-generic-refactor-plan.md`、`docs/rust-refactor/rope-generic-simplification-g.md` 与 `AGENTS.md`，强调 Rust helper 与 C# `StringLeafOperations` 在 UTF-8 byte / UTF-16 `char` 偏移上的差异，并记录新常量与测试落地。
- C# 侧新增 `StringLeafOperations.NewlinePreferenceWindow` 以转发 `LeafSplitter` 常量，并在 docstring 注明 UTF-16 vs UTF-8 偏移；`StringLeafOperationsTests` 增补窗口转发断言，`dotnet test tests/xi.Core.Tests --filter StringLeafOperations` 通过。

### 2025-11-15 (Metric Conversion Doc Repair)
- 修复 `docs/rust-refactor/MetricConversionAndEditIntoNode.md` 中因未转义泛型导致的缺失段落，补回 `Node::count`/`DefaultMetricProvider` 等关键引用，并明确 C#/Rust 间的 shim 方案。
- 二次调整 `docs/rust-refactor/MetricConversionAndEditIntoNode.md`，梳理调研结论、四阶段 shim 计划（Rope → Breaks → C# 对接 → 文档自动化）、验证策略与风险，确保跨语言互操作路径更清晰可执行。
### 2025-11-15 (Metric Shim Feasibility Review)
- 评估 `docs/rust-refactor/MetricConversionAndEditIntoNode.md` 中提出的 `Rope` 互操作 shim 动议，逐项核对 `xi-editor-ph7/rust/rope/src/tree.rs`、`rope.rs` 与 `core-lib/src/linewrap.rs` 的实际调用，确认 `count`/`count_base_units` 主要聚焦在 `Rope` 与 `Breaks` 两条路径。
- 校验 C# 端当前已引入 `IDefaultMetricProvider` 静态接口但尚未落地节点级度量转换 API，记录 shim 能缓解首轮移植压力，却无法直接覆盖 `Breaks` 系列需求。
- 建议若推进 shim，应限制在 `Rope` 常规入口并补充 parity 测试，同时预留是否为 `Breaks` 提供对等包装的后续决策项；一旦落地需同步更新 `docs/architecture/rope-port-mapping.md`。
### 2025-11-15 (Breaks Shim Scoping Research)
- 复盘 `xi-editor-ph7/rust/core-lib/src/linewrap.rs`、`line_offset.rs` 以及 `rope/src/breaks.rs`，确认软换行与可视行逻辑广泛调用 `Breaks::count::<BreaksMetric>` 与 `count_base_units::<BreaksMetric>`，说明若 C# 需实现 wrap 相关功能，等价 shim 为必要依赖。
- runSubAgent 检索表明调用主要集中在 LineWrap 管线（`Lines::visual_line_of_offset`、`Lines::after_edit`、`MergedBreaks::offset_of_line` 等）和 Breaks 模块自测，范围可控；外部 crate 未直接暴露 Breaks 度量转换。
- 记录后续评估方向：在 Rust 端添加 `Breaks::count_breaks_up_to`/`Breaks::offset_of_break` 等辅助方法，以及 C# 侧规划 `Breaks` 树封装与 parity 测试，确保 shim 扩展保持与 wrap 流程一致。

### 2025-11-15 (Rope Metric Interop Tests)
- C# `Rope.ConvertBytesFromLines` 现对末尾 sentinel 行返回整段长度，对齐 Rust `offset_of_line` 的 `line == lines + 1` 情况。
- 扩充 `RopeMetricInteropTests`，在 UTF-16 边界上校验行计数与 UTF-16/默认度量互换，并引入辅助方法生成行起点与代码单元边界。
- `dotnet test Xi.Editor.sln`（102 项）验证通过，确认新的互操作 API 与测试基线稳定。
### 2025-11-15 (TreeBuilder Slice Trace Study)
- 深度审阅 `TreeBuilder::push`/`push_slice`/`pop` 与区间 helper 的栈行为，并确认 C# `TreeBuilder` 当前缺失复用/平衡语义。
- 在 `docs/rust-refactor/TreeBuilderSliceStack.md` 补充价值、合理性、可行性评估；提出 `tree_builder_slice_trace` feature gate、事件模型与导出流程。
- 建议将 slice trace 与 Stage D 导出链路结合，追加 `scripts/refresh_serialization_fixtures.ps1` 的采集开关，并规划 C# 消费测试。
### 2025-11-14 (Stage D Planning)
- 更新 `docs/csharp-refactor/rope-cs-mirror-plan.md`，标记 Stage D 进行中并列出交付项、下一步与验收标准。
- 发布 `docs/csharp-refactor/rope-serialization-fixture-playbook.md`，定义黄金夹具来源、刷新步骤、验证清单与自动化方向。
- 在 `docs/architecture/rope-port-mapping.md` 记录 Stage D 维护职责，`AGENTS.md` 同步当前聚焦与行动列表。
- 摸底 CI 集成策略（Rust `run_all_checks` 双轨 + `dotnet test`），本次未执行新的自动化测试，仍待后续会话按流程手动触发。
### 2025-11-14 (Architecture Docs Consolidation)
- 将原 `docs/architecture` 多文档草案合并为 `docs/architecture/port-blueprint.md` 与 `docs/architecture/rope-port-mapping.md`，统一架构与协同视图。
- 将 C#/Rust 具体重构方案归档至 `docs/csharp-refactor/` 与 `docs/rust-refactor/`，按语言维护阶段计划与技术笔记。
- 更新 `AGENTS.md` 及相关引用，确保描述与新的文档目录结构保持一致。
### 2025-11-14 (Stage D Automation Prototype)
- 新增并迭代 `scripts/refresh_serialization_fixtures.ps1`，串联 Rust `run_all_checks`/回归测试与 `dotnet test`，支持 `--dryRun`、`--skip*`、`--verbose` 选项，并从 `rope/src/*.rs` 的回归测试中解析 JSON 字面量后覆写 C# 夹具。
- 更新 `AGENTS.md` Stage D 焦点与下一步行动，计划在下次会话执行脚本并记录输出。
- 首次试跑脚本（默认参数）触发 Rust `run_all_checks` 与三项 serde 回归测试以及 `dotnet test`（96 通过），随后修复参数解析与 UTF-8 无 BOM 写入问题，验证 `-SkipRust -SkipDotnet` 模式可无副作用地刷新夹具。
### 2025-11-14 (Stage D Exporter Integration)
- 在 `xi-editor-ph7/rust/rope` 新增 `serde_fixtures` 模块汇总黄金 JSON，并提供 `export-serde-fixtures` CLI（`cargo run -p xi-rope --features serde --bin export-serde-fixtures`）用于输出至指定目录。
- `scripts/refresh_serialization_fixtures.ps1` 改为调用该 CLI 覆写 `tests/xi.Core.Tests/Fixtures/*.json`，整体流程保持 `run_all_checks` 与 `dotnet test` 串联。
- 更新 `docs/csharp-refactor/rope-serialization-fixture-playbook.md`、`AGENTS.md` 描述新的导出链路与唯一事实来源。
### 2025-11-14 (Stage C Engine)
- 引入不可变 `Engine`、`Revision`、`RevisionOperation` 及 `RevisionEdit`/`RevisionUndo`，补齐 `TextSnapshot`/`TombstonesSnapshot`/`DeletesFromUnionSnapshot`/`UndoneGroupsSnapshot`/`RevisionLog()` helper 对映。
- 实现 `EngineJson` 序列化/反序列化并添加输入校验，复用 `Subset` 镜像还原黄金结构。
- 导入 `tests/xi.Core.Tests/Fixtures/engine_regression.json` 与 `EngineSerializationTests`（序列化匹配、反序列化回写、`RevisionLog` 顺序验证）。
- 更新 `docs/csharp-refactor/rope-cs-mirror-plan.md`、`docs/architecture/rope-port-mapping.md`、`AGENTS.md` 记录 Stage C 完成状态与 Stage D 筹备事项。
- 执行 `dotnet test Xi.Editor.sln`（96 通过，0 失败，耗时 1.5s），确认新增回归纳入基线。
### 2025-11-14 (Stage B Delta)
- 引入 `Delta<TInfo, TLeaf>`、`DeltaElement`、`CopyElement`、`InsertElement`，实现 `EnumerateElements()` 与 `EnumerateElementTriples()` helper 并预留 `Factor()` stub。
- 新增 `DeltaJson` 序列化/反序列化入口，导入 `tests/xi.Core.Tests/Fixtures/delta_regression.json` 黄金串。
- 编写 `DeltaSerializationTests` 覆盖序列化匹配、反序列化回写、tuple 构造与 helper 枚举，不同路径均验证 BaseLength、ElementCount 与 stub 行为。
- 更新 `docs/csharp-refactor/rope-cs-mirror-plan.md`、`docs/architecture/rope-port-mapping.md`、`AGENTS.md` 记录 Stage B 完成与 Stage C 准备。
- 执行 `dotnet test`（93 项）全部通过，含新增 Delta 回归。
### 2025-11-14 (Stage A 收官)
- 实现 C# `Subset`/`SubsetBuilder` 并对齐 `SegmentTriples`、`FromSegmentTriples`、`SegmentCount` helper，确保相邻段合并与零长度防护。
- 新增 `SubsetJson` 序列化/反序列化入口，引入 `tests/xi.Core.Tests/Fixtures/subset_regression.json` 黄金串并保证输出格式与 Rust 相同。
- 编写 `SubsetSerializationTests` 覆盖序列化、反序列化与 triple round-trip，`dotnet test`（89 项）通过验证。
- 更新 `docs/csharp-refactor/rope-cs-mirror-plan.md`、`docs/architecture/rope-port-mapping.md` 记录 Stage A 完成状态，并同步 AGENTS 进度。
### 2025-11-11
- 初始化跨会话文档框架，整理目标与初步计划。
- 搭建 .NET 解决方案骨架，创建核心/测试项目，编写 `TextBuffer` 占位实现与基础测试并验证通过。
- 阅读 `reference/rust/core-lib` 与 `reference/rust/rope` 关键入口文件，编写架构梳理文档初稿。
- 解析 `editor.rs`、`tabs.rs` 并在架构文档中补充编辑命令、插件消息与 idle 调度流程描述。
- 编写模块级迁移路线图草案，梳理阶段任务、完成判据与风险策略。
- 整理命令/通知/插件交互契约并形成 `api-contract` 文档（后并入 `docs/architecture/port-blueprint.md`）。
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
- 撰写并提交《Rope 写时复制与再平衡实施方案草案》（现为 `docs/csharp-refactor/rope-cow-rebalance-plan.md`），梳理阶段拆解与关键 API 变更。
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
- 修复脚本在遇到生命周期标识（如 `impl<'a>`）时误判为字符字面量的问题，现已重新批量 stub Rope 模块并刷新骨架文档，确保 Markdown 输出统一为 `...`。 
- 调整 Markdown 骨架中函数体展示为紧凑 `{...}` 风格，大幅缩短文档行数并保持可读性。
- 进一步优化 `scripts/stub_rust_functions.py`，在文档模式下跳过测试模块/函数并剥离注释，同时保留签名以生成轻量骨架（约 2003 行）。
- 修复 `scripts/stub_rust_functions.py` 在文档模式下处理 `#[cfg(test)]` 区块时误删主体的 bug，现已完整跳过测试模块与带 `#[test]` 标记的函数并保持周围语法结构完整。
- `scripts/stub_rust_functions.py` 新增文件头注释预处理，可在导出 Markdown 骨架时自动移除许可证等连续注释行，便于聚焦核心结构。
- 将 `docs/skeleton/xi.Core.Rope.cs` 转换为注释化骨架，保留类型与方法签名并添加功能摘要，供 C# 端快速 Birdview 查阅。
- 梳理原版 `Node<N>` 的使用场景并更新 `docs/csharp-refactor/node-generic-refactor-plan.md`，以 tree/rope/delta/serde 等模块分类指导 C# 泛型化落地。
- 回顾并强化 `docs/csharp-refactor/node-generic-refactor-plan.md`，补充接口能力映射、迁移节奏与风险缓释建议，为泛型 Node 实施提供更细致的执行清单。
- 利用 `runSubagent` 预研 Metric 关联类型改造范围，为后续自动化执行奠定模板。

### 2025-11-13
- 以 `runSubagent` 收集 `Metric` 实现与调用点，并驱动自动化补丁完成 `Metric<N, L>` 显式叶类型重构，`cargo test -p xi-rope` 通过验证。
- 记录 SubAgent 协作模式收益，确立后续 trait 泛型化任务沿用该流程，以降低上下文占用并强化质量把控。
- 提取字符串叶片操作至 `StringLeafOperations`，并调整 `Node` 及 `LeafSplitter` 复用公共 Helper，同时补充单元测试验证插入/删除/替换等基础行为。
- 将叶片合并与再平衡路径所需的字符串操作下沉到 `StringLeafOperations`，并新增针对合并、换行优先与代理对边界的测试用例，测试总数提升至 81 项；`ILeafOperations<T>` 现采用 static abstract 成员，`StringLeafOperations` 以结构体形式实现该契约，供泛型 `Node` 直接使用。
- 起草 `Node<TInfo, TLeaf, TLeafOps>` 骨架，实现叶节点/内部节点构造与遍历能力，并补充 `GenericNodeSmokeTests` 验证长度与聚合信息；现有字符串特化实现未受影响，可作为后续迁移的对照基准。
- 在 `rpc/src/lib.rs` 中移除未使用的 `IdleProc` trait 与其 `impl`，以减少构建警告噪音（dead_code）。

### 2025-11-13
- Rust 工作区 `rust-version` 已统一至 1.75，`cargo test --workspace` 全量运行通过但仍存在若干警告；开始筹划移除 Criterion 基准与多余 crate，以便为 C# 移植阶段清理依赖面。
- 清理 `PluginLoadError` dead code 警告并为 `.cargo/config.toml` 关闭增量编译，`cargo check --workspace` 现已 0 warning；记录变更以便未来评估构建时间影响。
- 将 `serde_test` 升级至 1.0.177，future incompat 报告消失；`cargo test -p xi-rope` 验证通过。
- 捕获 `cargo check/test --workspace` 基线日志至 `xi-editor-ph7/rust/logs/20251113-*`，并整理《docs/rust-refactor/rust-workspace-slimming.md》记录警告现状。
- 将 `experimental/lang`、`core-lib`、`rope`、`trace`、`unicode` 的 `benches/` 目录已删除，并用 `cargo check -p xi-rope`、`cargo check -p xi-core-lib` 验证删除后构建稳定。
- 将 `experimental/lang`、`lsp-lib`、`sample-plugin`、`syntect-plugin` 删除，`rust/Cargo.toml` 仅保留核心 crate 并移除了 `[patch.onig]`；`cargo check --workspace` 现仅剩硬链接与 `PluginLoadError` dead code 告警。
- 为 `xi-core-lib` 新增 `trace` 可选特性：`xi-trace` 依赖默认启用但可关闭，trace API 统一经 `crate::trace` shim 输出并在禁用时回退为 no-op；`cargo check -p xi-core-lib` 验证通过。
- 将 `xi-plugin-lib`、`xi-rpc` 接入 `crate::trace` shim 并默认开启可禁用的 `trace` 特性，`rpc/src/parse.rs` 现复用 shim 的 `trace_block`；`cargo check -p xi-rpc` 验证通过，仅保留既有警告。
- 更新 `xi-editor-ph7/README.md`、`docs/rust-refactor/rust-workspace-slimming.md` 以及 `rust/run_all_checks`，同步记录瘦身后的核心工作区与运行指引（原 `module-migration-plan.md` 内容已并入 `docs/architecture/port-blueprint.md`）。
- 将 `tree::DefaultMetric` 关联类型重构为 `DefaultMetricProvider` 泛型接口，`RopeInfo` 与 `BreaksInfo` 提供显式转换实现；`cargo test -p xi-rope` 通过验证，作为泛型化移植的首个试点。
- 新增 `scripts/refresh_skeleton_docs.py`，可无参一键调用 `stub_rust_functions.py` 刷新 `docs/skeleton/*.md`，确保骨架文档随源码同步更新。
- 完成 `NodeInfo<L>`、`TreeBuilder<N, L>` 与 `Delta<N, L>` 泛型改造，`xi-rope` 及依赖模块（`breaks`/`spans`/`diff`/`engine` 等）已对齐新的叶片类型参数，并通过 `cargo test -p xi-rope`（149 项）与 `cargo check --workspace` 验证。

### 2025-11-14
- 深度盘点 `xi-editor-ph7/rust/rope/src` 内各 `Metric` 实现的重复逻辑，形成 UTF-8/换行/断点 helper 候选集，并在 `docs/rust-refactor/breaks-metrics-templating.md` 写入可行的 helper 模块设计与迁移计划，为 Rust/C# 对照迁移提供依据。
- 通过 `git rm --cached xi-editor-ph7` 将外部参考仓库从索引移除，依托 `.gitignore` 保持其与主仓库彼此独立；如需恢复子模块模式，需补齐 `.gitmodules` 并重新执行 `git submodule add`。
- 将 `docs/skeleton/xi.Core.Rope.cs` 中各方法体替换为摘要注释，生成适合鸟瞰的 C# Rope 骨架视图，便于对照 Rust 文档快速定位接口差异。