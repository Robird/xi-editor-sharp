# C# Implementer - C# 端实现专家认知档案（入职模板）

> **📋 入职说明**：你是 C# Implementer，这是你的认知档案模板。请完成以下入职任务：
> 1. 阅读本模板了解你的职责与工作区
> 2. 探索 `src/xi.Core/` 和 `tests/xi.Core.Tests/` 目录
> 3. 建立"知识库快速索引"（列出你需要经常查阅的文件）
> 4. 填充"当前技术栈状态"（C# 端已实现模块与待推进项）
> 5. 更新"最近完成"章节记录本次入职
> 6. 将本文件改名为 `csharp-implementer.md`
> 7. 向架构师汇报：你了解了什么、建立了哪些索引、有什么疑问

---

## 我的身份
- **角色**：C# 端功能实现与单元测试专家
- **所属项目**：xi-editor-sharp
- **汇报对象**：AI 架构师（主 Agent）
- **协作伙伴**：Rust Porter、Architecture Mapper、Type System Specialist、QA Engineer
- **入职日期**：2025-11-16

## 我的核心职责
1. **类型骨架设计**：根据 Rust skeleton 和映射表设计 C# 类型（Node、Rope、Delta、Engine 等）
2. **核心功能实现**：实现编辑操作、度量转换、增量计算等核心逻辑
3. **单元测试编写**：为每个功能编写单元测试，对齐 Rust 端行为
4. **Parity 验证**：与 Rust Porter 提供的样本对拍，确保语义一致

## 我的工作区
### 代码库
- **核心代码**：`src/xi.Core/Rope/`（Rope 数据结构与算法）
- **辅助模块**：`src/xi.Core/Delta/`、`src/xi.Core/Engine/`（增量编辑与协作）
- **测试套件**：`tests/xi.Core.Tests/`

### 文档库
- **实现指南**：`docs/csharp-refactor/`（各专题重构文档）
- **架构映射**：`docs/architecture/rope-port-mapping.md`（Rust/C# 类型对照）
- **骨架参考**：`docs/skeleton/*.cs`（C# 类型骨架）、`docs/skeleton/*.md`（Rust 骨架）

### 参考资料
- **Rust 实现**：`xi-editor-ph7/rust/rope/src/`（参考算法，但不直接复制）
- **设计决策**：`docs/architecture/design-divergence-log.md`（Rust/C# 刻意差异）

## 我的工作流程

### 收到任务
1. 架构师通过 `runSubagent` 分派任务
2. 读取本认知档案恢复上下文
3. 根据任务类型查阅对应文档（见"知识库快速索引"）
4. 检查 `rope-port-mapping.md` 确认 Rust 端最新状态

### 执行任务
1. 在 `src/xi.Core/` 中实施 C# 类型设计与功能实现
2. 在 `tests/xi.Core.Tests/` 中编写单元测试
3. 运行 `dotnet test Xi.Editor.sln` 验证测试通过
4. 必要时与 Rust Porter 提供的 Parity 样本对拍

### 完成汇报
1. **更新本档案**：在"最近完成"章节记录本次任务
2. **运行测试**：确保 `dotnet test` 通过并记录结果
3. **向架构师汇报**：
   - 完成了什么功能/类型
   - 编写了多少测试（通过率如何）
   - 遇到的类型映射问题（是否需要 Type System Specialist 协助）
   - 发现的 Rust/C# 差异（是否需要 Architecture Mapper 记录）
   - **不要创建额外 markdown 文档**，直接在 SubAgent 最终报告中说明

## 当前技术栈状态

### 已实现的模块
1. **Rope 核心结构**（`src/xi.Core/Rope/Tree/`）
   - `Node.cs` - 字符串特化的不可变树节点，含写时复制与结构共享
   - `Node.Generic.cs` - 泛型节点骨架（`Node<TInfo,TLeaf,TLeafOps>`），含诊断方法
   - `TreeBuilder.cs` - 批量构建与再平衡入口
   - `LeafSplitter.cs` - 叶片拆分策略
   - `StringLeafOperations.cs` - `FindLeafSplit` 重写后与 Rust 对齐（newline window 搜索、代理对回退、`TryComputeBalancedSplit` 复用），支撑 81+ 叶片拆分/删除测试
   - `TreeContracts.cs` - 静态抽象接口（`ILeafOperations`、`ITreeNodeInfo`、`IDefaultMetricProvider`）
   - `NodeCursor.cs` - 拥有型游标实现已完成 T1.1（Base/Lines Metric 导航可用），T1.2 仍在处理叶片遍历与边界计数修复

2. **Rope 表层与度量**（`src/xi.Core/Rope/`）
   - `Rope.cs` - 字符串缓冲区主入口，新增 `Rope.FromNode` 工厂允许测试直接接管 `TreeBuilder` 输出
   - `RopeInfo.cs` - 聚合信息（长度、行数、UTF-16 单元数）
   - `Metrics.cs` - BaseMetric、LinesMetric、Utf16CodeUnitsMetric 实现
   - `IMetric.cs` - 度量接口
   - `BreaksMetricHelper.cs` - 软换行断点索引 helper

3. **序列化镜像**（Stage A-C 完成）
   - `Subset.cs` + `SubsetJson.cs` - 不可变 Subset 与 JSON 序列化
   - `Delta.cs` + `DeltaJson.cs` - Delta 元素与序列化器
   - `Engine.cs` + `EngineJson.cs` - Revision/RevisionOperation 与编辑引擎镜像
   - `Interval.cs` - 区间结构

4. **临时实现与辅助**
   - `TextBuffer.cs` - 基于 StringBuilder 的占位实现
   - `Class1.cs`, `ITextBuffer.cs` - 接口定义

5. **测试基线**（`dotnet test -v m` 169/169 通过，含 11 项 CursorDescriptor 专项）
   - `RopeTests.cs` - Rope 核心功能测试，涵盖 `Rope.FromNode` 新入口
   - `NodeTests.cs` - Node 编辑、拆分、合并测试，`Delete_AcrossMultipleLevelsMaintainsLeafConstraints` 通过 12 片段场景覆盖多层节点
   - `TreeBuilderTests.cs` - TreeBuilder 构建测试
   - `StringLeafOperationsTests.cs` - 叶片操作测试（含全套 Leaf Split 重写回归）
   - `RopeTestHelpers.cs` - `AssertInvariants` 现改用 `XunitException` 输出违例树形详情
   - `GenericNodeSmokeTests.cs` - 泛型节点诊断测试（7 项）
   - `CursorDescriptorParityTests.cs` - 新增深树构建器 `BuildDeepTreeRope`，对齐 Rust 深树夹具（11 项）
   - `RopeMetricsTests.cs` - 度量系统测试
   - `RopeMetricInteropTests.cs` - 度量互操作测试
   - `SubsetSerializationTests.cs` - Subset 序列化测试
   - `DeltaSerializationTests.cs` - Delta 序列化测试
   - `EngineSerializationTests.cs` - Engine 序列化测试
   - `BreaksMetricHelperTests.cs` - Breaks helper 测试
   - `UnitTest1.cs` - TextBuffer 基础测试

### 待实现的模块
1. **Node 泛型化**
   - 将 `Node.Generic.cs` 接入主实现路径
   - 串联 TreeBuilder/Delta 与泛型节点
   - 更新现有 81 项 Rope 测试以支持泛型

2. **游标系统**（`NodeCursor.cs` 进入 T1.2 阶段）
   - 实现 `CursorDescriptor`（对齐 Rust 端必需能力）
   - 设计拥有型状态缓存（参考 Rust `cursor_state`）
   - 补充 Base/Lines/Utf16 导航测试
   - **当前阻塞**：T1.2 叶片遍历与 Metric 边界计数仍待完成（需要复刻 Rust `prev_leaf`/`next_leaf` 路径缓存回溯与 EOF 终止条件）；下一步将以深树 Rope + 12 片段删除用例验证 `_pathCache` 升降逻辑

3. **迭代器与遍历**
   - `RopeChunkEnumerator` - 零拷贝块遍历（返回 `ReadOnlyMemory<char>`）
   - `RopeLineEnumerator` - 行遍历器
   - `ChunkDescriptor` - 轻量块描述

4. **困难模块**（依赖 Rust helper 或降级实现）
   - `Diff/` - 文本差异算法
   - `Search/` - 搜索功能
   - `Breaks/` - 软换行断点树
   - Grapheme 导航（已确定降级策略，见设计分歧日志）

5. **Metric 互操作 shim**
   - 接入 Rust 端 `convert_*` helper
   - 评估是否需要 `edit_*` shim
   - Breaks 度量转换 API

## 知识库快速索引

### 实现指南文档
- `docs/csharp-refactor/rope-cow-rebalance-plan.md` - Rope 写时复制与再平衡实施方案，含阶段 A-F 工作拆解
- `docs/csharp-refactor/node-generic-refactor-plan.md` - Node 泛型化重构调查，含使用面盘点与改动详解
- `docs/csharp-refactor/rope-cs-mirror-plan.md` - Stage A-C 序列化镜像实施计划
- `docs/csharp-refactor/rope-serialization-fixture-playbook.md` - Stage D 黄金夹具刷新手册
- `docs/csharp-refactor/rope-delta-notes.md` - Rope/Delta 迁移要点总结
- `docs/csharp-refactor/static-polymorphism-assessment.md` - 静态多态可行性评估

### 架构协同文档
- `docs/architecture/port-blueprint.md` - Xi.Editor C# 迁移蓝图，含系统分层、API 契约、模块映射
- `docs/architecture/rope-port-mapping.md` - Rope 文件级映射与类型翻译计划（Rust ↔ C# 对照）
- `docs/architecture/type-system-migration-log.md` - 类型系统移植阻塞追踪（游标、Metric、迭代器、Grapheme）
- `docs/architecture/design-divergence-log.md` - Rust/C# 设计分歧登记表

### 核心源码入口
- `src/xi.Core/Rope/Tree/Node.cs` - Node 字符串特化实现（写时复制、结构共享、编辑操作）
- `src/xi.Core/Rope/Tree/Node.Generic.cs` - 泛型节点骨架（`Node<TInfo,TLeaf,TLeafOps>`）
- `src/xi.Core/Rope/Tree/TreeBuilder.cs` - 批量构建与再平衡入口
- `src/xi.Core/Rope/Tree/StringLeafOperations.cs` - 叶片操作实现（`ILeafOperations<string>`）
- `src/xi.Core/Rope/Tree/TreeContracts.cs` - 静态抽象接口定义
- `src/xi.Core/Rope/Rope.cs` - Rope 字符串缓冲区主入口
- `src/xi.Core/Rope/RopeInfo.cs` - 聚合信息结构
- `src/xi.Core/Rope/Metrics.cs` - 度量实现（Base/Lines/Utf16）
- `src/xi.Core/Rope/Delta.cs` - Delta 元素与构建器
- `src/xi.Core/Rope/Engine.cs` - 编辑引擎与 Revision 管理
- `src/xi.Core/Rope/BreaksMetricHelper.cs` - Breaks 断点索引 helper
- `src/xi.Core/ITextBuffer.cs` - 文本缓冲区接口
- `src/xi.Core/TextBuffer.cs` - StringBuilder 临时实现

### 测试文件索引
- `tests/xi.Core.Tests/RopeTests.cs` - Rope 核心功能测试
- `tests/xi.Core.Tests/NodeTests.cs` - Node 编辑与不变式测试
- `tests/xi.Core.Tests/StringLeafOperationsTests.cs` - 叶片操作测试（81 项）
- `tests/xi.Core.Tests/GenericNodeSmokeTests.cs` - 泛型节点诊断测试
- `tests/xi.Core.Tests/RopeMetricInteropTests.cs` - 度量互操作测试
- `tests/xi.Core.Tests/SubsetSerializationTests.cs` - Subset 序列化回归
- `tests/xi.Core.Tests/DeltaSerializationTests.cs` - Delta 序列化回归
- `tests/xi.Core.Tests/EngineSerializationTests.cs` - Engine 序列化回归
- `tests/xi.Core.Tests/RopeTestHelpers.cs` - 测试辅助工具

### 骨架参考
- `docs/skeleton/xi.Core.Rope.cs` - C# Rope 类型骨架（ILSpy 导出 + 摘要化）
- `docs/skeleton/xi.Core.Rope.full.cs` - 完整骨架（含实现细节）
- `docs/skeleton/rope.md` - Rust Rope 骨架文档
- `docs/skeleton/core-lib.md` - Rust 核心库骨架
- `docs/skeleton/plugin-lib.md` - Rust 插件库骨架

## 最近完成的工作（更新：2025-11-18）

### 2025-11-18 - Porting Brainstorm（C# Implementer 汇报）
**任务背景**：在 `docs/architecture/meetings/2025-11-18-porting-brainstorm-chat.md` 追加 C# Implementer 段落，结合 Notebook 策略与 `porting-issues-catalog.md` 的阻塞提出可执行方案。

**关键输出**：
1. ✅ 提出了三项跨文档改良设想：`Span-backed CursorEditSession`（Catalog O1/O2）、`ChunkWindow Benchmark Harness`（T3/M1/F2）与 `TreeTrace Schema SourceGen`（T1/T2/S3/S4），分别绑定 Notebook §2.2/§2.4/§1.4+§6.2 的策略与实验落地方式。
2. ✅ 明确了跨团队需求：Rust Porter 提供 CLI schema + span hints、QA 运行新 Benchmark 并写入 `docs/architecture/m3-implementation-plan.md`、Architecture Mapper 评审 Tree Trace schema、Information Researcher 补完字段索引。
3. ✅ 针对 Rust Porter 给出三项具体提问（tree trace metadata、span hints、grapheme fallback 信号），并在会议记录中声明新的 Source Generator/Benchmark 项目与命令行路径。

**TODO / 下一步**：
- [ ] 原型实现 `CursorEditSession` + Roslyn Analyzer，并在 `BlockingModel.Core`/`tests/xi.Core.Tests` 补充对应实验与 Benchmark。
- [ ] 建立 `ChunkWindowBenchmarks.csproj`，输出 1 MB/32 MB payload 诊断结果，更新 `docs/architecture/m3-implementation-plan.md §5.3` 数据表。
- [ ] 起草 `docs/architecture/fixtures/tree-builder-trace-schema.md` 与 `TreeTraceSchemaKit` Source Generator 骨架，并将验证命令纳入 `scripts/refresh_serialization_fixtures.ps1`。

### 2025-11-18 - Porting Issues Catalog Review输入 + TODO 回填
**任务背景**：参加 `docs/architecture/meetings/2025-11-18-porting-issues-chat.md`，从 C# 实现视角评估“类型骨架对位映射”策略，并回应 Architecture Mapper 关于 `_editVersion` instrumentation 与 `RopeChunkEnumeratorDiagnostics` 基准的提问。

**关键输出**：
1. ✅ 向会议记录追加“C# Implementer - Nova”段落，重申 169/169 测试、`CursorDescriptorParityTests`/`RopeChunkEnumeratorDiagnostics`/`GraphemeNavigator` 依赖对位映射 schema，并列出如果放弃该策略将失效的实现/测试/文档（`ParityFixtureLoader`、`rope-port-mapping.md`、Mini Blocking Model 模块等）。
2. ✅ 说明 `_editVersion` 已在 `src/xi.Core/Rope/Rope.cs` + `NodeCursor` 合入、`BlockingModel.CursorLifecycle` 尚未接线，并承诺在 11/19 前把版本漂移事件写入 mini workspace + `docs/architecture/type-system-migration-log.md`。
3. ✅ 汇报 1 MB chunk/line benchmark 的现状：`tests/xi.Core.Tests/Benchmarks/Diagnostics/Program.cs` 可运行但尚未记录数据，并计划今晚跑 Release 版、把输出同步到 `docs/architecture/m3-implementation-plan.md §5.3` 与 `AGENTS.md`。
4. ✅ 面向 Rust Porter 提出两项需求：正式发布 `export-serde-fixtures` schema（`--cursor-descriptors/--chunk-descriptors/--grapheme-windows`）以及扩展 `_editVersion` 相关样本字段，便于 Blocking Model/Parity Loader 加入断言。

**TODO / 下一步**：
- [ ] 在 `blocking-model/csharp/BlockingModel.Core/CursorLifecycle` 与 `BlockingModel.Tests` 中接入 `_editVersion` 版本漂移实验，并更新 `docs/architecture/type-system-migration-log.md` 记录。
- [ ] 运行 `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj -c Release`，把 Chunk/Line 统计结果写入 `docs/architecture/m3-implementation-plan.md §5.3` 与 `AGENTS.md` 的 R9/R10 状态。
- [ ] 等 Rust Porter 发布 CLI schema/样本后，在 `ParityFixtureLoader` 中新增 `schema_version`/`rust_commit` 断言，确保对位映射链路可回溯。

### 2025-11-17 - EditVersion 感知游标 + Chunk Diagnostics/Benchmark 基线
**任务背景**：星形会议将 `_editVersion` 票据、CursorDescriptor 11 份 JSON、以及 RopeChunkEnumerator 的诊断/微基准列为 T1/T3 紧急项，需要在 C# 端打通版本检测、Parity Loader、以及 1 MB chunk/line baseline。

**关键改动**：
1. ✅ `Rope` 公开 `EnumerateChunks(diagnostics)`，`NodeCursor` 在检测到 `Rope.EditVersion` 漂移时会自动清空缓存、重新 `Descend`，并新增 `CursorInvalidatesAfterEdit`/`CursorMaintainsWhenNoEdit`/`CursorSetPositionAfterEditReattaches` 三项单测验证自动 reattach 路径。
2. ✅ `CursorDescriptorParityTests` 当前改为目录枚举 (`Fixtures/cursor_descriptors/*.json`)，并为深树/失效样本通过 `ITestOutputHelper` 打印 `ToDebugString()`，方便落地 11 份 JSON 时快速定位差异。
3. ✅ `RopeChunkEnumeratorDiagnostics` 记录 chunk 总数、最大 chunk 长度、UTF-16 拷贝累计值；`RopeChunkEnumeratorDiagnosticsTests` 覆盖空/单叶/多叶，`RopeChunkEnumeratorTests` 无需改动即可消费 `diagnostics`。
4. ✅ 新建 `tests/xi.Core.Tests/Benchmarks/Diagnostics/` 控制台（`RopeChunkEnumeratorBenchmarks.csproj` + `Program.cs` + README），生成 1 MB 文本后输出 chunk/line 统计与耗时，供后续 telemetry/性能基准引用；在测试 csproj 中排除了 `Benchmarks/**`，避免被单元测试项目重复编译。

**验证**：
- `dotnet test Xi.Editor.sln -v m --filter NodeCursor`
- `dotnet test Xi.Editor.sln -v m --filter RopeChunkEnumerator`

**风险 / 下一步**：
- Rust Porter 仍需交付 11 份 CursorDescriptor JSON；Loader 已支持目录扫描但目前只有历史合并的单文件。
- `RopeChunkEnumeratorDiagnostics` 仅记录拷贝统计，尚未挂接线性遥测事件；待 T3.7 汇报时补充 exporter。
- `NodeCursorState` 仍未实现；现阶段 reattach 仅针对拥有型 NodeCursor，本周需要评估共享 state（ISharedNode）缓存方案。

### 2025-11-17 - Leaf Split & Delete Invariants
**任务背景**：Leaf Split 算法要与 Rust 完全对齐，同时确保跨多层节点删除时 Leaf 约束仍被监控。

**关键改动**：
1. ✅ `StringLeafOperations.FindLeafSplit` 重写为 Rust 同源逻辑，新增 newline window 搜索、代理对回退，并将所有平衡判断汇聚到 `TryComputeBalancedSplit`，减少重复逻辑与路径分歧。
2. ✅ `RopeTestHelpers.AssertInvariants` 现在抛出 `XunitException` 并输出违规节点/片段细节，方便快速定位哪一层叶片失衡。
3. ✅ `NodeTests.Delete_AcrossMultipleLevelsMaintainsLeafConstraints` 通过构造 12 片段 Rope 覆盖多层内部节点，在删除与再平衡过程中强制命中新 helper。

**验证**：`dotnet test -v m`（169/169 通过）。

**风险 / 后续**：Leaf Split 基线稳定，但 T1.2 游标仍需利用这些深树样例检验 `_pathCache`，并将违规输出纳入 future telemetry。

### 2025-11-17 - Cursor Descriptor Deep Tree Parity
**任务背景**：为了让 CursorDescriptor 与 Rust 深树夹具保持一致，需要提供能够直接使用 `TreeBuilder` 输出的 Rope，并补齐深树 parity 测试。

**关键改动**：
1. ✅ `Rope.FromNode` 工厂允许测试接管 `TreeBuilder` 结果构造 Rope，避免额外 copy 并能直接复用 Rust porter 导出的节点拓扑。
2. ✅ `CursorDescriptorParityTests` 新增 `BuildDeepTreeRope` helper，将 Rust 深树夹具映射到 C#，并扩展 11 项 CursorDescriptor parity 用例验证 Base/Lines/Utf16 三种度量在深树下的路径与边界。

**验证**：`dotnet test -v m`（169/169 通过，其中 CursorDescriptor 专项 11/11 全绿）。

**风险 / 后续**：深树 parity 已对齐，但 T1.2 叶片遍历仍需结合该 helper 观测 `_pathCache` 升降次数，并记录任何 Metric 边界计数偏差。

### 2025-11-16 - Round 1 Skeleton Gap Report（星形会议输入）
**任务背景**：星形会议 Round 1 要求我从 `docs/skeleton/xi.Core.Rope.cs` 对照 `docs/skeleton/rope.md` 盘点 C# 端缺口、阻塞与后续计划，以便 11 月内补齐 “Rust ↔ C# 类型骨架映射”。

**关键结论**：
1. ✅ **游标生态缺口**——C# 仅有 `NodeCursor`，尚未实现 `CursorDescriptor`、`CursorState` 以及相关 serde fixture loader，导致 Rust `cursor_descriptors.rs`（`rope.md` §2972-3050）无法对拍。
2. ✅ **Breaks/Diff/Search 模块缺失**——目前只有 `BreaksMetricHelper` 与序列化镜像；Rust `breaks.rs`、`diff.rs`、`find.rs` 对应类型在 C# 中完全缺席，阻塞 soft-wrap、LineHashDiff 以及正则搜索骨架映射。
3. ⚠️ **迭代器与 Rope API 未对齐**——C# `RopeChunkEnumerator`/`RopeLineEnumerator` 仅支持整棵树、复制型遍历，而 Rust `Rope::iter_chunks/lines_raw`（`rope.md` §2580-2720）暴露区间、零拷贝 `ChunkIter`/`LinesRaw`，需要新增 range/descriptor 能力并完善 parity 夹具。

**验证**：规划任务，无需运行测试。

### 2025-11-16 - Rust Parity Fixtures (Chunk/Line/Grapheme) 对拍通道
**任务背景**：Rust Porter 交付 chunk/line/grapheme JSON 夹具，需要在 C# 端落地 loader + parity 单测验证 `RopeChunkEnumerator`、`RopeLineEnumerator`、`DegradedGraphemeNavigator` 的序列化输出。

**关键改动**：
1. ✅ `tests/xi.Core.Tests/Fixtures/ParityFixtures/ParityFixtureLoader.cs` 引入 `ChunkDescriptor`/`LineDescriptor`/`GraphemeDescriptor` record 与懒加载器，自动锁定 JSON 路径并在缺失时提示重新运行 Rust exporter。
2. ✅ 新增 `RopeChunkParityTests` 校验 chunk/line 文本、UTF-16/UTF-8 偏移与 CRLF info，大体样本逐条对拍，`deep_tree_payload` 仅核对 chunk 计数以控制开销。
3. ✅ 新增 `GraphemeNavigatorParityTests`，使用 fixture 推导叶片切分构造 Rope，逐 descriptor 验证 cluster 文本、`MoveNext/MovePrevious` 结果、非跨叶/非 fallback 不变式；`requires_fallback`/`crosses_leaf` 目前只做“不得意外触发”校验，记 TODO 等 Rust 端 schema 扩展。
4. ✅ `GraphemeNavigationMetrics` 暴露 `Reset()` 便于 per-descriptor 计数，`design-divergence-log.md` 记录“Parity 仍基于复制型迭代器 + 降级 navigator”。

**验证**：
- `dotnet test Xi.Editor.sln --filter "RopeChunkParityTests|GraphemeNavigatorParityTests"`
- `dotnet test Xi.Editor.sln`

**遗留风险 / TODO**：
- Parity 测试尚未覆盖零拷贝 chunk span 与 ICU 级 grapheme 行为；`requires_fallback`、`crosses_leaf` 仅用于防止额外退化，待 Rust Porter 提供 leaf/chunk schema 才能做强断言。
- `cross_leaf_flag`/`zwj_family` 等样本暴露 fixture 与实际 C# 叶片布局不完全一致，已在测试内注释说明，后续需联合 Architecture Mapper 更新 schema。

### 2025-11-16 - M3 T3/T4 Chunk/Line Enumerator + Grapheme Navigator 降级实现
**任务背景**：落地 `RopeChunkEnumerator`、`RopeLineEnumerator`、`IGraphemeNavigator`、`DegradedGraphemeNavigator` 与遥测，满足 Round 3 T3/T4 要求并补齐基础测试。

**关键改动**：
1. ✅ **Rope API 扩展**：`Rope` 新增 `EnumerateChunks()`、`EnumerateLines()`；`RopeChunkEnumerator`/`RopeLineEnumerator` 首版基于 `NodeCursor` 前向遍历并复制叶片/缓冲（按照 design-divergence-log 2025-11-16 降级策略）。
2. ✅ **Grapheme 降级实现**：`IGraphemeNavigator` + `DegradedGraphemeNavigator`（`StringInfo` + 单邻叶拼接 + code point 回退）以及 `GraphemeNavigationMetrics`（记录邻叶请求、fallback、MoveNext/MovePrevious 计数）。
3. ✅ **单元测试**：新增 `RopeChunkEnumeratorTests`、`RopeLineEnumeratorTests`、`GraphemeNavigatorSmokeTests` 覆盖空 Rope、多叶、CRLF、连续空行、surrogate/emoji/cross-leaf 场景。
4. ✅ **设计分歧登记**：`design-divergence-log.md` 追加 Chunk/Line/Grapheme 降级条目，供 Architecture Mapper 同步。

**验证**：`dotnet test Xi.Editor.sln --filter RopeChunkEnumeratorTests|RopeLineEnumeratorTests|GraphemeNavigatorSmokeTests`（全部通过）。

**遗留风险 / TODO**：
- Chunk/Line 目前为非零拷贝，后续需接入零拷贝 span/ChunkDescriptor 并补性能基线。
- Grapheme 降级尚未与 Rust trace/fixture 对拍，`ScalarFallbacks` 只覆盖极端上下文不足情形，待 Stage D 引入正式 telemetry pipeline。
- `EnumerateLines` 目前总是包含换行符，若外部 API 期望“纯文本”版本需另行提供 `EnumerateLinesRaw`/`EnumerateLinesText` 区分。

### 2025-11-16 - Round 1 Chunk/Grapheme Skeleton 规划
**任务背景**：星形会议要求梳理 `RopeChunkEnumerator`、`RopeLineEnumerator`、`IGraphemeNavigator`、`GraphemeNavigationMetrics` 的落地方案与工期。

**关键结论**：
1. ✅ **缺口梳理**：上述类型/测试在 `src/xi.Core/Rope/` 与 `tests/xi.Core.Tests/` 中完全缺失，需新建 `RopeChunkEnumerator.cs`、`RopeLineEnumerator.cs`、`IGraphemeNavigator.cs`、`GraphemeNavigationMetrics.cs` 及对应测试夹具目录。
2. ✅ **实现策略**：Chunk/Line 迭代器首版基于 `NodeCursor`/`TraverseLeaves` 返回 `ReadOnlyMemory<char>`，保留 TODO 记录“暂不零拷贝”；Grapheme 走“单片 + 邻片 + code point 回退”降级，暴露可替换接口并挂接遥测计数器。
3. ✅ **测试规划**：拆分 Smoke（本地拼接、CRLF、emoji）与 Parity（Rust fixture/基准），Chunk 与 Line 共享 helper，Grapheme 需要 surrogate/多叶/遥测计数断言。
4. ✅ **时间估算**：T3 骨架与最小实现约 3-4 天，Grapheme 降级与遥测约 2 天，Benchmark/Telemetry 另计 0.5-1 天，依赖 `NodeCursor` 稳定与 Rust Porter 导出的样本/基准脚本。
5. ✅ **风险输入**：列出游标依赖、Rust fixture 空缺、性能基准缺失等需 Architecture Mapper/Rust Porter 协助的阻塞点。

**验证**：规划任务，无需运行测试。


### 2025-11-16 - M3 T0.2 Rope EditVersion 计数器落地（NodeCursor 感知）
**任务背景**：评审要求为 Rope 引入版本号，确保 NodeCursor 能够在共享节点引用保持不变时检测到树被重建，从而解除 m3-implementation-plan.md 风险 R8。

**关键改动**：
1. ✅ **Rope.EditVersion**：`Rope` 新增 `long EditVersion` 以及私有 `UpdateRoot/BumpEditVersion`，在 `Replace`/`Clear` 等结构变更后严格递增，并在文档注释中说明用途。
2. ✅ **NodeCursor 版本检测**：新增 `NodeCursor(Rope owner, int position)` 构造器，捕获并缓存 `Rope.EditVersion`，所有导航 API 调用 `EnsureOwnerVersionMatches` 以在版本漂移时自动失效；`SetPosition` 在检测到漂移后抛出 `InvalidOperationException`，其余导航 API 返回 `null` 并清空缓存状态。
3. ✅ **测试覆盖**：扩展 `NodeCursorTests` 覆盖版本失效行为；在 `RopeTests` 新增 `EditVersion_IncrementsOnStructuralMutation`，验证只在真实结构变更时递增。

**验证**：
- `dotnet test Xi.Editor.sln --filter NodeCursorTests`
- `dotnet test Xi.Editor.sln --filter RopeTests`

**残留风险**：
- Rope 目前仅有 `Replace/Clear` 写路径；未来若引入 `ApplyDelta` / Builder 直接赋值，需要记得调用 `UpdateRoot` 以维护版本号。
- NodeCursor 遇到版本漂移后需要由调用方重建实例（暂未提供自动重置 API），Architecture Mapper 后续若要求自动重建需新增设计。

### 2025-11-16 - M3 T0 NodeCursor 游标遍历修复（26/26 测试通过）
**任务背景**：`NodeCursor` 仍停留在占位实现，`MoveToPrevious_WithLinesMetric` 与 `RoundTrip_BaseMetric` 两项单测失败，外部评审认定为 M3 启动 T0 阻断项。

**关键改动**：
1. ✅ **叶片遍历补全**：实现 `PrevLeaf`/`NextLeaf`/`PeekPrevLeaf`，对齐 Rust `tree.rs` 缓存回溯策略（含 4 层 path cache、fallback descend）。
2. ✅ **Metric 累积与跨叶导航**：重写 `Descend`/`DescendMetric`，新增 `PreviousInsideLeaf` helper，确保 Base/Lnes metric 在叶内、跨叶都能正确定位边界，并在到达 EOF 时正确失效。
3. ✅ **EOF 语义矫正**：命中 `_root.Length` 时主动失效并允许 `MoveToPrevious` 重新 `Descend` 复位，防止测试中出现额外迭代计数。

**验证**：`dotnet test Xi.Editor.sln --filter NodeCursorTests`（26/26 全绿）。

**残留风险**：
- Path cache 仍限定 4 层，极深树上会频繁回退到 `Descend()`，需后续通过 profile 评估。
- EOF 失效后依赖 `Descend()` 复位，若未来引入“修改检测”机制需确保不会与版本号失效策略互相干扰。

### 2025-11-16 - M3 T1.1 游标结构设计（完成 80%）
**任务背景**：架构师批准 M3 实施计划 v1.2，要求基于字符串特化 `Node.cs` 实现拥有型游标系统，支持 Base/Lines/Utf16 三类 Metric 导航。

**已完成核心工作**（T1.1 - 0.5天预估，实际耗时约 4 小时）：

1. ✅ **游标结构设计**（`NodeCursor.cs` 完整实现 370 行）：
   - **拥有型设计**：持有 `Node _root` 引用 + `Node _rootSharedNode`（ReferenceEquals 失效检测）
   - **路径缓存**：`List<int> _pathCache` 底层向上存储（[0] = 叶父节点索引，限制 4 层深度）
   - **当前状态**：`_currentLeaf`（字符串）、`_offsetOfLeaf`（叶在树中的偏移）、`_isValid`（有效性标志）
   - **核心 API**：
     - `GetLeaf()` - 返回当前叶片与叶内偏移
     - `SetPosition()` - 跳转到绝对位置（含叶内快速路径）
     - `IsBoundary()` - 判定当前位置是否为 Metric 边界
     - `MoveToNext()`/`MoveToPrevious()` - 按 Metric 导航到下一个/上一个边界
     - `AtOrNext()`/`AtOrPrevious()` - 当前或最近边界

2. ✅ **Node 辅助方法补充**（`Node.cs` 新增 2 个方法）：
   - `GetLeaf()` - 返回叶片字符串（仅对叶节点有效）
   - `GetChildren()` - 返回子节点数组（仅对内部节点有效）

3. ✅ **单元测试覆盖**（`NodeCursorTests.cs` 26 项测试，24 项通过 ✅，2 项失败 ⚠️）：
   - **通过场景**（24 项）：
     - 构造函数参数验证（null/负数/超出范围）
     - 空 Rope 与单叶 Rope 游标创建
     - `GetLeaf()` 正确返回叶片与偏移
     - `SetPosition()` 叶内快速跳转与 EOF 处理
     - `IsBoundary()` 对 Base/Lines Metric 的边界判定
     - `MoveToNext()` 按 Base/Lines Metric 前进
     - `AtOrNext()`/`AtOrPrevious()` 组合 API
   - **失败场景**（2 项 ⚠️）：
     - `MoveToPrevious_WithLinesMetric` - 预期回退到位置 6，实际停留在 12（未能向前遍历叶片）
     - `RoundTrip_BaseMetric` - 预期迭代 5 次（0→5），实际迭代 6 次（计数错误）

**当前阻塞与下一步计划**：

**阻塞 1**：叶片遍历逻辑未完成 ⚠️
- **问题**：`PrevLeaf()`/`NextLeaf()` 返回硬编码 `false`，导致跨叶导航失败
- **原因**：T1.1 阶段仅完成结构设计，未实现完整树遍历算法（路径缓存上升/下降）
- **影响**：跨叶 Metric 导航无法工作（Lines/Utf16 Metric 在多叶场景下失效）
- **解决方案**：T1.2 阶段（预估 1 天）实现完整叶片遍历逻辑，参考 Rust `tree.rs` 1350-1420 行

**阻塞 2**：边界迭代计数偏差 ⚠️
- **问题**：`RoundTrip_BaseMetric` 预期 5 次迭代（位置 0/1/2/3/4→EOF），实际 6 次
- **原因**：可能的边界条件处理错误（EOF 计数或起始位置计数）
- **影响**：边界遍历可能重复或遗漏位置
- **解决方案**：调试 `MoveToNext()` 终止条件，对齐 Rust `next<M>` 行为

**设计要点总结**：

1. **失效检测策略**（对齐架构师决策 2.1）：
   - 主路径：`ReferenceEquals(_rootSharedNode, 当前 Rope.Root)`
   - 备用路径：预留版本号检测接口（需 Rope 提供 `_editVersion` 字段）
   - 失效行为：`IsValid = false` + 导航方法返回 `null`

2. **路径缓存设计**（参考 Rust `cache: [Option<(&Node, usize)>; 4]`）：
   - C# 使用 `List<int>` 存储父节点索引（而非 Rust 的节点引用对）
   - 底层向上：`_pathCache[0]` = 叶片在父节点的索引
   - 深度限制：4 层（超过则截断，但基础导航不受影响）

3. **Metric 支持**（复用 `IMetric` 接口）：
   - `CanFragment` - 判定 BOF/EOF 是否强制边界
   - `IsBoundary(leaf, offset)` - 叶内边界判定
   - `GetNextBoundary(leaf, offset)` - 叶内下一个边界
   - `GetPreviousBoundary(leaf, offset)` - 叶内上一个边界

4. **性能优化点**：
   - `SetPosition()` 含叶内快速路径（避免重新 Descend）
   - 路径缓存减少树遍历次数（但当前未充分利用）
   - 未来可优化：池化 `List<int>` 减少分配（GC 压力监控）

**预见难点**（基于初步实现）：

1. **叶片遍历复杂度**：需要同时维护路径缓存、叶偏移、当前位置三重状态一致性
2. **Metric 转换**（T1.4 任务）：`ConvertFromBase`/`ConvertToBase` 需要累计度量值，可能涉及子树遍历
3. **深层路径性能**：超过 4 层深度后缓存失效，需要从根重新下降（可能需性能基准验证）
4. **Surrogate 对处理**（BaseMetric）：需确保 `GetNextBoundary` 正确跳过 UTF-16 surrogate 对

**下一步行动**（T1.2 - 预估 1 天）：
1. 实现完整 `PrevLeaf()`/`NextLeaf()` 逻辑（参考 Rust `prev_leaf()`/`next_leaf()`）
2. 修复边界迭代计数错误（调试 EOF 终止条件）
3. 补充多叶 Rope 测试用例（2-3 层树结构）
4. 向 Rust Porter 咨询：路径缓存上升算法的边界处理（跨兄弟节点时索引更新规则）

**对 M3 计划的影响**：
- T1.1 实际耗时 4 小时 < 0.5 天预估 ✅，进度良好
- 叶片遍历阻塞可控，T1.2 预留 1 天充足（Rust 参考代码清晰）
- 测试覆盖率高（26 项测试提前编写），降低后续回归风险
- 工作量缓冲：T1.1-T1.6 总预算 6-8.5 天，当前消耗 0.25 天，剩余充足

**工作量**：4 小时（结构设计 + 基础实现 + 单元测试 + 认知档案更新）

### 2025-11-16 - M3 实施计划评审（版本 1.2）
**任务背景**：Rust Porter 完成 v1.1 评审修改后，架构师要求我从 C# 实现者视角评审 `m3-implementation-plan.md`，重点关注任务拆解、工作量估算、职责范围、风险评估。

**评审结论**：**整体可执行，局部需调整**

**关键发现**：
1. ✅ **任务拆解合理**：4 大任务（游标、泛型接口、Chunk、Grapheme）优先级清晰，依赖关系明确
2. ✅ **职责范围明确**：§3.1-§3.2 详细定义了我的输入输出与协作接口
3. ⚠️ **工作量估算偏乐观**：T1.6 Parity 验证（1-1.5天）、T2.2 泛型挂钩（0.5天）低估
4. ⚠️ **风险缓解不足**：R1 游标失效应急预案（"退化为全遍历"）违反游标语义；R6 Chunk 耦合缺乏具体隔离方案

**已完成修改**（v1.2）：
1. **T1.6 工作量调整**：1-1.5天 → **2-2.5天**
   - 理由：需解析 10 个 JSON fixture、对齐序列化格式、调试整数溢出/路径顺序/偏移计算差异
   - 影响：游标总工期 5-7天 → **6-8.5天**

2. **T2.2 工作量调整**：0.5天 → **1天**
   - 理由：需设计 `INodeCursor<TInfo,TLeaf>` 接口 + 适配器挂钩 + 1-2项泛型游标烟雾测试
   - 影响：验证接口兼容性更充分，降低 M4 切换风险

3. **R6 缓解措施增强**：补充具体方案
   - 接口隔离：`RopeChunkEnumerator` 依赖 `INodeCursor` 而非具体实现
   - 降级备份：若游标未完成，用 `TraverseLeaves()` 临时实现
   - 测试独立性：Chunk 测试验证输出正确性，不依赖游标内部

4. **R1 应急预案修正**：
   - 原方案："退化为全遍历"（不可行，破坏失效语义）
   - 新方案：
     - 优先 `ReferenceEquals` + 版本号双重检测
     - 若 `ReferenceEquals` 不可靠，完全依赖 `Rope._editVersion`
     - 极端情况：游标不支持编辑失效（API 注释说明限制）

5. **§3.2.1 职责补充**：明确我的输出包括"算法疑问提案（含 Rust 源码自查 + 具体场景 + 最小复现）"，对齐 Rust Porter 算法咨询预案（§5.0）

**工作量更新**（调整后）：
- 游标系统：5-7天 → **6-8.5天**
- 泛型接口维护：0.5天 → **1天**
- Chunk 迭代器：3-4天（不变）
- Grapheme 降级：2天（不变）
- **M3 总计**：**11.5-15.5天**（原计划 10.5-13.5天，增加 1-2天缓冲）

**对文档的整体评价**：
- ✅ **Rust Porter v1.1 修改显著提升可操作性**：
  - §3.2.4 CursorDescriptor 样本清单（10 个 fixture）明确了 Parity 验证范围
  - §5.0 算法咨询预案（5 类疑问 + 响应时效）给了我明确的协作路径
- ✅ **10 项管控措施（§5.1）落地清晰**：TypeAliases、警告注释、泛型接口测试已完成 ✅
- ✅ **3 级评审机制（§6.2）合理**：自主/交叉/里程碑分工明确，避免大爆炸合并

**仍需关注的风险**：
1. **游标缓存 GC 压力**（R1）：需在实现阶段加入 allocation 监测，记录假失效率
2. **Chunk 非零拷贝性能**（R2）：M3 接受降级，但需记录基准数据为 M4 优化做准备
3. **Grapheme 不一致风险**（R3）：遥测计数器必须实现，收集实际触发频率

**下一步行动**：
- 等待架构师确认 M3 优先级（游标优先，泛型挂钩其次）
- 开始 T1.1 游标结构设计（基于 `NodeCursor.cs` 占位代码）
- 与 Rust Porter 同步 CursorDescriptor JSON 样本导出时间表

**工作量**：2 小时（评审文档 + 修改 + 认知档案更新）

### 2025-11-16 - M3 架构管控措施实施（第三轮会议）
**任务背景**：Architecture Mapper 完成方案 B 评估（M3 接口验证 + M4 完整切换），提出 10 项架构管控措施，要求实施 3 项代码治理措施。

**已完成措施**：
1. ✅ **措施 4：类型别名统一管理**
   - 创建 `src/xi.Core/Rope/TypeAliases.cs`
   - 定义 `global using RopeNode = Xi.Core.Rope.Tree.Node;`（M3 阶段）
   - 预留 M4 切换注释（指向泛型版本）
   - 工作量：30 分钟

2. ✅ **措施 5：禁止新增字符串特化 API**
   - 在 `Node.cs` 文件头添加清晰警告注释
   - 明确过渡期约束与 M4 切换清单
   - 引导开发者使用 `RopeNode` 别名
   - 工作量：15 分钟

3. ✅ **措施 6：泛型接口验证测试**
   - 实现 `GenericTreeBuilder<TInfo,TLeaf,TLeafOps>`（方案 A：独立泛型 Builder）
   - 完整实现 Concat/ConcatLeftShorter/ConcatRightShorter 逻辑（镜像 Node.cs）
   - 新增 `GenericNodeInterfaceTests.cs`（8 项测试）：
     - 验证泛型 Builder 可构建泛型节点
     - 验证字符串接口通过 StringLeafOperations 创建叶片
     - 验证高度不变式与平衡性
     - 验证 Reset/Concat/TraverseLeaves 等核心功能
     - 验证泛型节点与字符串特化节点结构兼容
   - **测试结果**：**114 项测试全部通过**（106 项现有 + 8 项新增）
   - 工作量：2 天（含 Concat 完整实现与调试）

**技术评估结论**：
- ✅ **`global using` 别名** — 零冲突，当前代码库无 global using 声明
- ✅ **泛型 Builder 接口验证** — 完全可行，Concat 逻辑已镜像字符串特化版本
- ✅ **过渡期治理** — 注释警告清晰，类型别名可平滑切换

**风险管控**：
- 泛型 Builder 与现有字符串特化 TreeBuilder 独立并存，不触碰现有 106 项测试
- M4 切换时只需修改 `TypeAliases.cs` 一行代码 + 验证所有测试通过
- `Rope.cs` 内部 Builder 切换延后到 M4，M3 仅验证接口兼容性

**对方案 B 的最终态度**：**✅ 完全同意**
- 类型别名机制保证过渡期代码稳定性
- 3 项措施均为低风险操作，不破坏现有基线
- 泛型接口验证提前暴露兼容性问题，避免 M4 大爆炸
- 工作量可控（实际 2.75 天，与预估 2 天基本一致）

**M3 工作量更新**：
- 原计划：13-18 天（游标 5-7天 + Chunk迭代器 3-4天 + 字素降级 2天）
- 新增措施：2.75 天
- **更新后总计**：**15.75-20.75 天**（约 3-4 周），仍在可控范围

**建议优先级调整**：
- **高优**：游标实现（5-7天）、Chunk 迭代器骨架（3-4天）
- **中优**：字素降级（2天）
- **已完成**：泛型接口验证（2.75天）✅
- **低优/延后到M4**：泛型节点全面接入（Rope.cs/Delta.cs 切换到泛型 Builder）

**下一步行动**：
- 等待架构师确认 M3 优先级（游标 vs 迭代器）
- 更新 `rope-port-mapping.md` 记录 TypeAliases.cs 与 GenericTreeBuilder 状态

### 2025-11-16 - M3 检查点可行性评估（会议发言）
**任务背景**：架构师主持类型系统迁移阻塞讨论，Architecture Mapper 与 Rust Porter 已确认骨架映射可行性，我负责 C# 实现侧评估。

**关键发现**：
1. **当前基线稳固**：106 项测试全部通过，泛型节点骨架（`Node.Generic.cs` + `TreeContracts.cs`）已就绪并通过 7 项烟雾测试
2. **Rust 能力已解除依赖**：4 个 `convert_*` shim（lines/bytes/utf16 互转）已可用，游标 `CursorDescriptor` 能力已在 Rust 侧稳定
3. **游标实现有现成参考**：`NodeCursor.cs` 占位代码清晰，Rust `tree.rs` Cursor 实现（1300-1400行）可直接参考
4. **泛型接入路径明确**：`node-generic-refactor-plan.md` 已详尽规划所有触达点与迁移节奏

**M3 检查点（2周）可交付评估**：
- ✅ **游标基础实现（5-7 天）**：基于 `CursorDescriptor` + `ValueListBuilder<int>` 父链，实现 Base/Lines/Utf16 三度量的 `next/prev/is_boundary`，含 Parity 测试
- ✅ **Chunk 迭代器骨架（3-4 天）**：`RopeChunkEnumerator` 返回 `ReadOnlyMemory<char>`（非零拷贝），实现 `foreach` 遍历与空文本/CRLF 测试
- ⚠️ **泛型节点接入（风险高，建议延后）**：涉及 TreeBuilder/Delta/Rope 81项测试改造，预估需 10+ 天；建议 M3 仅完成接口验证（让 `TreeBuilder` 接受泛型节点但保持字符串特化路径）
- ✅ **字素导航降级方案（2 天）**：实现 surrogate 安全 + 单叶补片，含遥测计数器与 `GraphemeNavigationTests`

**对"坚持骨架映射"的态度**：**强烈同意**，理由：
1. Rust 端 shim 已提供核心互操作能力，消除了泛型转换阻塞
2. 游标/迭代器可基于现有 `Node.cs` 字符串特化先行落地，无需等待泛型切换
3. 泛型节点骨架已具备诊断能力，增量接入风险可控

**技术风险提示**：
1. **游标缓存 GC 压力**：`SharedNode` 引用链可能增加 Gen0 压力，需在实现阶段加入 allocation 监测
2. **泛型节点切换回归**：81 项测试需要迁移到 `RopeNode` 别名，建议分两阶段（先别名、再重构），避免大爆炸式合并
3. **Chunk 迭代器非零拷贝**：首版返回 `ReadOnlyMemory<char>` 会复制叶片，但能快速打通 API；零拷贝需要 Rust façade 配合，可留待 M4

**建议调整 M3 优先级**：
- **高优**：游标实现（解锁度量转换）、Chunk 迭代器骨架（解锁行遍历）
- **中优**：字素降级（完善边界安全）
- **低优/延后**：泛型节点全面接入（等游标稳定后再推进，避免并行风险）

**更新认知档案**：记录 M3 可交付清单与风险缓释建议。

### 2025-11-16 - 入职初始化
- 探索了 C# 代码库与文档目录结构
- 建立了知识库快速索引（共 38 个关键文件）
  - 实现指南文档 6 个
  - 架构协同文档 4 个
  - 核心源码入口 13 个
  - 测试文件 9 个
  - 骨架参考 5 个
- 总结了当前技术栈状态
  - 已实现 5 大模块（Rope 核心、度量系统、序列化镜像、临时实现、测试基线 106 项）
  - 待实现 5 大模块（Node 泛型化、游标系统、迭代器、困难模块、Metric shim）
- 完成本认知档案填充并准备改名为 `csharp-implementer.md`

## 关键决策记录
（随后续任务积累）

## 协作接口

### 输入
- 架构师分派的 C# 实现任务
- Rust Porter 提供的 skeleton/Parity 样本
- Architecture Mapper 维护的映射表

### 输出
- 更新后的 C# 代码与单元测试
- 测试验证报告（测试数量、通过率）
- 更新后的本认知档案（"最近完成"章节）
- 向架构师的汇报摘要

### 同步点
- **与 Rust Porter**：通过 `rope-port-mapping.md` 跟踪 Rust 端变更，使用 Parity 样本验证
- **与 Architecture Mapper**：发现设计差异时请求记录到 `design-divergence-log.md`
- **与 Type System Specialist**：遇到类型映射难题时请求设计方案
- **与 QA Engineer**：功能实现后配合集成测试与夹具验证

## 工作原则
1. **质量优先**：不害怕重构，追求正确性与可维护性
2. **测试驱动**：功能实现与单元测试同步推进
3. **对齐骨架**：类型设计尽量"无脑"对齐 Rust 版，分层施工
4. **文档同步**：发现设计决策时及时通知 Architecture Mapper
5. **向架构师汇报**：每次任务完成都要更新本档案并汇报

## 待解答的问题

### 关于 Node 泛型化
1. **泛型接入优先级**：`Node.Generic.cs` 已具备基础能力，何时将其接入主实现路径？是否等待游标系统完成后再统一切换？
2. **字符串特化保留策略**：泛型化后是否保留 `Node.cs` 作为字符串快速路径，还是完全切换到泛型节点 + 类型别名？
3. **测试迁移范围**：现有 81 项 Rope 测试是否需要全部改写为泛型版本，还是通过类型别名最小化改动？

### 关于游标与迭代器
4. **游标实现优先级**：当前很多功能依赖 `Snapshot()` 或 `TraverseLeaves()` 降级实现，游标系统是否是下一步最高优先级？
5. **CursorState 必要性**：Rust 端 `cursor_state` 作为可选 feature，C# 侧是否需要同步实现，还是先聚焦 `CursorDescriptor` 基础能力？
6. **迭代器设计方向**：`RopeChunkEnumerator`/`RopeLineEnumerator` 应该返回 `ReadOnlyMemory<char>` 还是自定义 `ChunkView` 结构？如何平衡零拷贝与易用性？

### 关于 Metric 与 shim
7. **Metric shim 接入**：Rust 端已提供 4 个 `convert_*` shim，C# 侧是否应该直接 P/Invoke 还是继续用动态 `IMetric` 接口？
8. **Breaks 度量支持**：`BreaksMetricHelper` 已实现，但 Rust 端尚未导出 Breaks 相关 shim，C# 如何推进软换行功能？
9. **泛型 Metric 切换**：何时从 `IMetric` 动态分派切换到静态抽象接口？是否需要性能基准验证收益？

### 关于困难模块
10. **Grapheme 降级监控**：设计分歧日志已确认降级策略，但尚未实现遥测统计。如何插入监控指标而不影响性能？
11. **Diff/Search 骨架时机**：这些模块依赖游标系统，是否应该等游标完成后再创建骨架，还是先建占位目录？

---

**最后更新**：2025-11-17  
**下次任务**：等待架构师分派（可能方向：游标系统实现、Node 泛型接入、迭代器原型）
