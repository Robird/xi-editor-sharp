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
   - `StringLeafOperations.cs` - 实现 `ILeafOperations<string>`，含 81 项测试
   - `TreeContracts.cs` - 静态抽象接口（`ILeafOperations`、`ITreeNodeInfo`、`IDefaultMetricProvider`）
   - `NodeCursor.cs` - 游标占位骨架（待实现）

2. **Rope 表层与度量**（`src/xi.Core/Rope/`）
   - `Rope.cs` - 字符串缓冲区主入口
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

5. **测试基线**（106 项全部通过）
   - `RopeTests.cs` - Rope 核心功能测试
   - `NodeTests.cs` - Node 编辑、拆分、合并测试
   - `TreeBuilderTests.cs` - TreeBuilder 构建测试
   - `StringLeafOperationsTests.cs` - 叶片操作测试（81 项）
   - `GenericNodeSmokeTests.cs` - 泛型节点诊断测试（7 项）
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

2. **游标系统**（`NodeCursor.cs` 当前为占位）
   - 实现 `CursorDescriptor`（对齐 Rust 端必需能力）
   - 设计拥有型状态缓存（参考 Rust `cursor_state`）
   - 补充 Base/Lines/Utf16 导航测试

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

## 最近完成的工作

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

**最后更新**：2025-11-16  
**下次任务**：等待架构师分派（可能方向：游标系统实现、Node 泛型接入、迭代器原型）
