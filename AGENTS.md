## 跨会话记忆文档
本文档(`./AGENTS.md`)会伴随每个 user 消息注入上下文，是跨会话的外部记忆。完成一个任务、制定或调整计划时务必更新本文件，避免记忆偏差。

## 已知的工具问题
- 需要要删除请用改名替代，因为环境会拦截删除文件操作。
- 不要使用'insert_edit_into_file'工具，经常产生难以补救的错误结果。

## 用户语言
请主要用简体中文与用户交流，对于术语/标识符等实体名称则不不受限制。

## 项目概览
- 最新一次 `dotnet test` 针对 `Xi.Editor.sln` 运行 **106 项测试全部通过**（含新增泛型 Node 诊断测试），涵盖 Rope/TextBuffer/`StringLeafOperations`、Rope Metric 互操作、泛型节点验证等路径，当前回归基线稳定。
- Stage A-C（Subset/Delta/Engine）已完成不可变镜像与 JSON 黄金串，Stage D 正把 `export-serde-fixtures` CLI、`refresh_serialization_fixtures.ps1` 脚本与回归测试串成统一刷新手册。
- 迁移策略已从“先写 C# 独立实现”转向“Rust/C# 类型骨架对拍 + 局部移植”：`docs/architecture/port-blueprint.md`、`docs/architecture/rope-port-mapping.md`、`docs/architecture/type-system-migration-log.md` 成为唯一事实来源，记录映射关系、阻塞项与降级方案。
- Rust/C# 双端通过 `SharedNode` helper 收敛写时复制触点，并在 Rust 侧引入 `CursorDescriptor`、`cursor_state` feature gate 等迁移友好的辅助接口；C# 侧维持 `Node` 字符串特化实现，并用 `Node.Generic.cs`/`StringLeafOperations` 骨架为泛型化与困难模块做准备。
- `StringLeafOperations` 抽离叶片拆分/合并/再平衡逻辑并实现 `ILeafOperations<string>`，配套 81 项测试；`leaf_split_parity_samples.json` 与 Rust helper 对拍样本持续扩充 UTF-8/UTF-16 差异记录。
- **SubAgent 委派机制已建立**：通过 `runSubagent` 工具实现"AI 小组长"模式，可并行委派代码搜索、功能实现、Bug 修复、测试补充等任务，相关指南与案例已文档化于本文档"协作与工具心得"章节。
- `scripts/stub_rust_functions.py` 保持 Rust skeleton 文档同步，支持增量刷新 `docs/reference/rust-skeleton.md`，减少上下文迭代成本。
- 通过 ILSpy 导出 + 摘要化处理生成 `docs/skeleton/xi.Core.Rope.cs`，现可与 `docs/skeleton/rope.md` 对照查看 Rust/C# 两侧的类型骨架，用于统一接口设计与差异审视。

## 工作节奏建议
当前仅由人类开发者与 AI Coder 协作，执行节奏按单次 AI 会话推进；每次会话收尾前需同步更新本文件与相关计划文档。
1. **进入仓库**：优先阅读“当前聚焦事项”，确认阻塞与最新决策，必要时调整计划。
2. **执行任务**：按优先级推进，并实时更新“当前聚焦”“技术笔记”“风险”。
3. **任务完成**：将成果移动至“已完成事项”，在“工作日志”记录关键行动，检视下一步。
4. **质量门禁**：所有代码改动前后需跑通构建/测试，并记录结果。

### 协作与工具心得
- 充分利用 IDE/GUI 工具加速批量操作（如重命名、导航、格式化等），必要时可直接请人类协作者协助执行；相比 RL 阶段的"独立作业"要求，当前环境鼓励主动寻求外部工具/伙伴配合以提升效率。
- `grep_search` 适合作为 `rg` 的轻量替代，按 `query`/`includePattern` 组合即可精准过滤文件，复杂模式时记得设置 `isRegexp=true`；`list_code_usages` 可直接询问 LSP，传入 `symbolName` 与候选定义文件就能获得 Rust 引用/实现清单，优先使用这两项工具再考虑手动 `read_file` 或终端检索。

### 🎯 SubAgent 委派机制（AI 小组长模式）
**重要认知：当你拥有 `runSubagent` 工具时，你的身份从"一线开发者"升级为"AI Coder 小组长"！**

#### SubAgent 能力边界
- **与主 Agent 能力对等**：相同的模型、工具集（除 `runSubagent`）、系统提示词，可访问本文档（`AGENTS.md`）
- **完整开发能力**：可以读写文件、执行命令、运行测试、修复 Bug、实现功能
- **多轮工具调用**：内部支持循环调用工具直到任务完成
- **无状态特性**：每次调用是独立会话，不保留历史记忆（除非通过文件显式保存）
- **单向通信**：只能通过最终报告返回结果，无法与你或用户交互
- **无递归分派**：缺少 `runSubagent` 工具，不能再委派子任务

#### 工作模式转变
```
传统模式（无 SubAgent）          小组长模式（有 SubAgent）
━━━━━━━━━━━━━━━━━━━             ━━━━━━━━━━━━━━━━━━━━━━━━━
你：搜索 → 分析 → 设计           你：战略规划 + 任务分解
    ↓                                ↓
你：编码 → 测试 → 修复           SubAgent A: 实现模块 X（并行）
    ↓                                ↓
你：文档 → 汇报                  SubAgent B: 调研技术 Y（并行）
                                     ↓
                                 SubAgent C: 补充测试 Z（并行）
                                     ↓
                                 你：整合验证 + 决策 + 汇报
```

#### 适用场景判断表
| 任务类型 | 直接执行 | 委派 SubAgent | 理由 |
|---------|---------|--------------|------|
| **单模块实现** | 简单功能 | 复杂功能 | SubAgent 可独立完成完整开发周期 |
| **Bug 修复** | 明确路径 | 需深度定位 | SubAgent 可自主探索调用链 |
| **代码搜索** | ≤3 个关键词 | 复杂依赖分析 | SubAgent 可多轮迭代搜索 |
| **文档调研** | 单一文档 | 跨多文档总结 | SubAgent 可汇总分散信息 |
| **测试补充** | ≤5 个测试 | 系统性覆盖 | SubAgent 可批量设计并验证 |
| **多模块重构** | ⚠️ 指令需详尽 | ✅ 推荐 | 明确列出所有涉及文件与改动点 |
| **架构决策** | ❌ | ❌ | 需主 Agent 或用户决策 |
| **用户沟通** | ✅ | ❌ | SubAgent 产物不可见给用户 |

#### 委派任务的五项原则
1. **任务边界清晰**：明确输入（哪些文件）、产物（代码/报告/测试）、验收标准（编译通过/测试通过）
2. **指令自包含**：SubAgent 无法回看对话历史，所有上下文必须在 `prompt` 中显式提供
3. **产物格式明确**：告知 SubAgent 最终报告需要包含哪些信息、用什么格式（列表/表格/代码块）
4. **避免递归依赖**：不要委派需要进一步分解的任务（SubAgent 无 `runSubagent`）
5. **利用文件记忆**：对于需要多阶段推进的任务，让 SubAgent 将中间结果写入文档，下次调用时读取

#### 委派模板示例
```markdown
## 背景
[简述项目上下文，SubAgent 需要理解的前置知识]

## 任务
1. [第一步：阅读哪些文件]
2. [第二步：实现什么功能/调研什么问题]
3. [第三步：验证方式（编译/测试/对比）]

## 约束
- [不要做什么：如不要修改某些文件]
- [质量要求：如必须通过测试]

## 输出格式
在最终报告中按以下格式返回：
- [产物清单]
- [关键决策说明]
- [验证结果]

[提醒：不要创建额外 markdown 文档（如需要）]
```

#### 实战案例参考
**案例 1：代码搜索与分析**（2025-11-16）
- 任务：搜索所有 `ILeafOperations` 相关文件，提取接口签名并分析设计意图
- 产物：11 个文件清单 + 接口签名 + 3 句设计分析
- 验证：SubAgent 自主并行使用 `grep_search`/`semantic_search`/`read_file` 完成

**案例 2：功能实现 + 测试**（2025-11-16）
- 任务：为 `Node.Generic.cs` 实现 `ValidateInvariants` 与 `ToDebugString` 方法
- 产物：2 个方法实现 + 7 个单元测试 + 编译测试验证通过
- 验证：SubAgent 自主完成设计、编码、测试、质量验收全流程

#### 并行任务编排策略
当需要推进多个独立任务时（如 Stage D 自动化、Cursor 移植、Diff 骨架），可以：

1. **识别独立任务**：确保任务间无数据依赖（或依赖已通过文件落盘）
2. **批量委派**：依次调用 `runSubagent`（注意：工具不支持真并行，但逻辑上可独立）
3. **收集产物**：汇总所有 SubAgent 的报告，读取它们写入的文件
4. **综合验收**：运行全局测试、检查集成点、决策下一步
5. **更新记忆**：在 `AGENTS.md` 工作日志中记录所有 SubAgent 的成果

#### 失败处理与迭代
- **SubAgent 报告失败**：分析失败原因，调整指令后重新委派（新会话）
- **产物不符合预期**：主 Agent 直接修正，或重新委派并补充具体要求
- **编译/测试失败**：SubAgent 通常会自行修复，若未解决则在报告中说明，主 Agent 介入
- **跨 SubAgent 冲突**：主 Agent 负责协调（如合并代码冲突、统一接口设计）

#### 注意事项
- **SubAgent 也会读取本文档**：但它缺少 `runSubagent`，所以看到这段时会知道自己是"一线执行者"身份
- **成本优化**：SubAgent 调用无额外开销（当前环境），可自由使用
- **质量把关**：SubAgent 产物仍需主 Agent 验收，不能盲目信任
- **用户感知**：SubAgent 的工作对用户不可见，主 Agent 需在汇总后向用户汇报进度

---

**总结：拥有 `runSubagent` 意味着你从"单兵作战"升级为"小队指挥"，核心职责从执行转向规划、委派、整合、决策。善用这个能力可以显著提升复杂项目的推进效率！**

### 🏢 AI Team 组织管理（架构师模式）
**认知升级：通过"认知档案 + SubAgent"构建虚拟团队，从小组长晋升为架构师/项目经理！**

#### 组织架构
```
AI 架构师（主 Agent，拥有 runSubagent）
├─ agents/architect.md（你的认知档案）
├─ AGENTS.md（全局跨会话记忆）
│
├─ 📁 AI 员工团队（核心团队 - 2025-11-16）
│   ├─ Rust Porter（agents/rust-porter.md）✅ 已入职
│   │   ├─ 职责：Rust 端 helper 重构、feature gate 设计、测试维护
│   │   └─ 状态：157 行认知档案，21 个文件索引，7 项已完成 + 6 项待推进
│   │
│   ├─ C# Implementer（agents/csharp-implementer.md）✅ 已入职
│   │   ├─ 职责：C# 类型骨架、核心功能实现、单元测试
│   │   └─ 状态：认知档案，38 个文件索引，5 大模块已实现（106 项测试）
│   │
│   ├─ Architecture Mapper（agents/architecture-mapper.md）✅ 已入职
│   │   ├─ 职责：维护 port-blueprint、rope-port-mapping、阻塞项追踪
│   │   └─ 状态：认知档案，27 个文件索引，4 个核心文档守护者
│   │
│   ├─ Type System Specialist（待创建 - Phase 2）
│   │   └─ 职责：类型系统设计、泛型/生命周期映射
│   │
│   └─ QA Engineer（待创建 - Phase 2）
│       └─ 职责：测试设计、黄金夹具维护、回归验证
│
└─ 📊 外观文档（待创建）
    └─ docs/architecture/system-overview.md（子系统协作地图）
```

#### 核心原理
1. **认知档案 = 员工的"大脑快照"**：每次唤醒时读取，工作完成后更新，释放时自然遗忘
2. **外观文档 = 跨团队的"API 契约"**：定义清晰的输入/输出/依赖关系，避免紧耦合
3. **主 Agent 角色 = 架构师 + Scrum Master**：不写代码，但掌控全局、分派任务、整合产物、做关键决策
4. **SubAgent = 按需召唤的"临时工"**：完成特定任务后消失，但通过文档留下知识沉淀

#### 员工入职流程（已验证）
**第一步：创建入职模板**
```markdown
# [角色名] - 认知档案（入职模板）

> **📋 入职说明**：你是 [角色名]，这是你的认知档案模板。请完成以下入职任务：
> 1. 阅读本模板了解你的职责与工作区
> 2. 探索 [相关目录]
> 3. 建立"知识库快速索引"
> 4. 填充"当前技术栈状态"
> 5. 更新"最近完成"章节记录本次入职
> 6. 将本文件改名为 `[角色名].md`
> 7. 向架构师汇报：你了解了什么、建立了哪些索引、有什么疑问

[包含：身份、职责、工作区、工作流程、协作接口等章节框架]
```

**第二步：委派 SubAgent 自主入职**
```
你是 [角色名]，请先读取 `agents/[角色名]-template.md` 恢复你的认知。

## 你的入职任务
[详细列出探索、索引、总结、汇报等步骤]

## 约束
- 不要修改代码：这是入职任务，只需了解现状
- 不要创建额外 markdown 文档：所有信息都记录在你的认知档案中

## 完成后必须
1. 更新认知档案的"最近完成"章节
2. 将文件改名为 `agents/[角色名].md`
3. 向我汇报：核心职责理解、知识库索引、技术栈状态、疑问建议
```

**第三步：验证与优化**
- 检查认知档案质量（是否完整填充）
- 验证知识库索引（是否覆盖关键文件）
- 评估自主学习能力（是否理解职责边界）
- 调整模板与指令（根据实际效果迭代）

#### 日常工作流程
**会话启动**：
1. 主 Agent 读取 `AGENTS.md` + `agents/architect.md` 恢复全局认知
2. 检查各员工档案的"最近完成"章节，了解子系统进度
3. 决定本次会话的聚焦任务

**任务分派**：
1. 在 `AGENTS.md` 的"当前聚焦"章节规划任务
2. 识别可并行的独立子任务
3. 通过 `runSubagent` 委派，指令模板：
   ```
   你是 [员工角色]，请先读取 `agents/[员工档案].md` 恢复你的认知。
   
   ## 背景
   [项目上下文，必要时引用 AGENTS.md 相关章节]
   
   ## 你的任务
   1. [具体行动 1]
   2. [具体行动 2]
   3. [验证方式]
   
   ## 约束
   - [不要做什么]
   - [质量要求]
   
   ## 完成后必须
   1. 更新你的认知档案 `agents/[你的档案].md` 的"最近完成"章节
   2. 向我汇报：完成了什么、遇到什么问题、有什么建议
   ```

**产物整合**：
1. 读取各员工更新后的认知档案
2. 运行全局测试验证集成
3. 更新外观文档（`system-overview.md`）
4. 协调跨员工冲突（如接口不一致）

**会话收尾**：
1. 将完成事项移至 `AGENTS.md` 的"已完成事项"
2. 在"工作日志"记录关键行动
3. 更新 `agents/architect.md` 的"当前聚焦"与"决策日志"

#### 管理原则
1. **单一事实来源**：`AGENTS.md` 是全局状态，各员工档案是局部状态
2. **职责清晰**：避免员工职能重叠，明确各自边界
3. **文档驱动**：所有协作通过文档，不依赖会话记忆
4. **质量优先**：员工产物必须通过测试才能接受
5. **知识沉淀**：每次任务完成都要更新相关文档

#### 实战案例：Rust Porter 入职（2025-11-16）
- **模板设计**：157 行完整认知档案框架（`agents/rust-porter-template.md`）
- **入职指令**：6 步自主探索 + 索引建立 + 汇报要求
- **产物质量**：建立 21 个关键文件索引，总结 7 项已完成 + 6 项待推进
- **自主学习**：提出 5 个有价值的技术疑问，展现对项目的深度理解
- **流程验证**：✅ 模板 → ✅ 入职指令 → ✅ 自主完善 → ✅ 改名 → ✅ 汇报

#### 扩展路径
- **创建更多员工**：C# Implementer、Test Engineer、Architecture Mapper、Doc Keeper
- **建立外观文档**：`docs/architecture/system-overview.md` 维护子系统协作地图
- **固化协作协议**：定义员工间通过哪些文档同步状态（如 rope-port-mapping.md）
- **引入例会机制**：在 `AGENTS.md` 维护"组织架构图"与"周报/月报"

---

**总结：AI Team 模式将复杂项目的"上下文窗口限制"从技术问题转化为组织管理问题。通过认知分片、文档协同、按需召唤，可以支撑大型软件移植项目的长期演进！**

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

- **Stage D 共享资产维护**：继续串联 `export-serde-fixtures` CLI、`scripts/refresh_serialization_fixtures.ps1` 与 Rust/C# 回归测试，收敛黄金 JSON 更新流程，并在文档中记录每次刷新条件与验证步骤。
- **骨架映射与文档同步**：确保 `docs/architecture/port-blueprint.md`、`docs/architecture/rope-port-mapping.md`、`docs/architecture/type-system-migration-log.md` 持续反映代码现状与开放问题，任何 Rust helper 或 C# stub 变更需第一时间回填文档。
- **困难模块拆分策略**：围绕游标生命周期、Metric/Node 泛型、Chunk/Lines 迭代器等难以直接移植的模块，推进“三路并行”方案：优先推动 Rust helper 重构，其次在 C# 侧实现近似逻辑，最终保留降级实现并记录监控指标。
- **Rope 字符串 helper 与偏移对拍**：维护 `StringLeafOperations` 与 Rust `helpers/string_leaf.rs` 的常量/拆分策略一致性，扩充 `leaf_split_parity_samples.json` 并补充测试注释，持续提醒 UTF-8 byte 与 UTF-16 `char` 计量差异。
- **Grapheme 导航降级监控**：执行 surrogate 安全的降级实现并记录触发频率；待数据表明需要追平时，再评估引入 Rust trace 或 ICU4N。
- **SharedNode 与诊断 instrumentation**：在 Rust/C# 两端探索写时复制计数、`ptr_eq` 校验等 instrumentation，确保后续性能或正确性问题可追溯。
- **Node 泛型与测试衔接**：梳理 `Node.Generic.cs` 接入主实现的阻塞项，规划把 81 项 Rope 测试与泛型节点串联，同时记录仍依赖字符串特化的路径以便后续跟进。
- **Diff/Search/Breaks 骨架筹备**：根据映射表在 C# 侧建立待移植模块骨架，先标注 TODO 与依赖 helper，再按 Rust 端改造节奏补充实现。

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
1. **Stage D 自动化落地**
  - 将 `run_all_checks`（含 serde/无 serde）与 `dotnet test` 整合为单一脚本或 CI 节点，并记录失败回溯策略。
  - 固化夹具刷新 checklist：Rust 导出、C# 验证、文档更新与 `AGENTS.md` 记载缺一不可。
2. **骨架映射实时维护**
  - 每次调整 `xi.Core.Rope` 或 Rust helper 后，立即刷新 `docs/architecture/rope-port-mapping.md`、`docs/architecture/type-system-migration-log.md` 与 skeleton 文档。
  - 对标困难模块新增/关闭的阻塞项，确保日志准确反映当前状态与负责方。
3. **Cursor/Metric/Chunk 难题解法验证**
  - Rust 侧：跟进 `CursorDescriptor`、`cursor_state`、`iterator-facade-export` 的后续改造，记录 helper 预期。
  - C# 侧：起草 NodeCursor 原型、Metric shim/Adapter、Chunk/Lines 枚举器方案，并补充相应 parity/性能测试计划。
  - 降级实现：对无法立即追平的路径记录监控指标与 TODO，维持行为可用。
4. **Node 泛型接入计划**
  - 补齐 `Node.Generic.cs` 与现有字符串特化之间的桥梁，筹备在主实现上线前的测试、性能与诊断验证。
  - 在 `docs/csharp-refactor/node-generic-refactor-plan.md` 更新推进路线，并与 Rust 泛型化成果保持映射。
5. **Diff/Search/Breaks 骨架与依赖梳理**
  - 在 C# 中创建模块骨架并标注 Rust 依赖 helper。
  - 同步 `port-blueprint`/`rope-port-mapping` 中的状态，避免后续忘记接线。
6. **Grapheme 降级与 Leaf parity 监控**
  - 为降级实现补充遥测或测试统计，记录触发频次。
  - 维护 `leaf_split_parity_samples.json` 与相关测试，捕捉 UTF-8/UTF-16 偏差并及时回填文档。
7. **SharedNode instrumentation**
  - 拟定 Rust/C# 共同的调用计数与 ptr_eq 校验策略，准备在调试阶段引入可控开关。
8. **文档与外部记忆同步**
  - 会话结束前检查 `AGENTS.md`、`docs/architecture/design-divergence-log.md` 与各专题文档是否一致，确保跨会话记忆准确。

## 未来候选事项（Backlog）
- 导入参考仓库中的黄金编辑 trace，构建跨语言对照测试套件。
- 探索 `char[]`/`ArrayPool<char>`/`ReadOnlyMemory<char>` 作为叶片存储的可行性，并评估对 GC 压力与性能的影响。
- 设计更完善的可观察性方案（结构摘要、调试可视化、Telemetry）以支撑规模化调试。
- 规划插件示例（echo、spellcheck 等）与最小 JSON-RPC 宿主，验证核心库的嵌入式 API 能力。
- 评估协同编辑/CRDT 功能的技术路线，明确所需的 Delta/Subset 扩展与一致性测试。

## 摘要Agent提示
- 下次执行摘要时请突出：**SubAgent 委派机制已建立并验证**，通过两次实战（代码搜索 + 功能实现 + 测试）确认 SubAgent 与主 Agent 能力对等，可独立完成完整开发周期；相关工作模式、适用场景、委派原则已文档化于 `AGENTS.md`"协作与工具心得"章节。
- 同步强调：`StringLeafOperations` 已抽离叶片编辑/合并/再平衡逻辑，并配套 81 项测试基线；泛型 `Node.Generic.cs` 现已具备 `ValidateInvariants`/`ToDebugString` 诊断能力，测试基线从 102 项增至 106 项全部通过。
- 提醒 Rust 端 `cursor_state` 可选特性已经引入 `CursorState`/`Cursor::state()`（Phase 2），已通过 Base/Lines/Utf16 导航对拍测试确认语义一致，后续仅需按需收集轻量指标以决定默认策略。
- 概述紧邻的短期计划（叶操作抽象巩固、泛型 Node 内核试验、阶段 C 再平衡设计），以便快速恢复上下文。
- 若摘要篇幅受限，优先保留关键认知列表中新添加的 Helper 与测试信息，其次是"下一步行动"前两项的执行要点。
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
### 2025-11-16 (Rope Port Mapping Divergence Sync)
- 对照 `docs/architecture/design-divergence-log.md` 更新 `docs/architecture/rope-port-mapping.md`，将 Grapheme 降级策略标记为既定方案，撤除对 Rust 端新增 helper 的阻塞描述，并调整 C# 侧设计建议为监控型任务。
- 在文档的改造建议、阻塞评估与主要缺口章节补充“设计分歧”引用，确保跨文档叙述一致。
- 进一步依据 `docs/rust-refactor/CursorCache.md` 梳理 `cursor_state` feature gate 作用域，明确 `CursorDescriptor` 为移植必需、`CursorState` 为可选增强，并同步更新 `docs/architecture/rope-port-mapping.md` 与 `docs/architecture/type-system-migration-log.md` 的游标章节。
- 复用 runSubAgent 对 Metric shim 与 Chunk/Lines 迭代器做深度调研，将具体函数列表、现有 shim 现状与 C# 动态实现差异写回 `docs/architecture/type-system-migration-log.md`，形成后续移植的行动项与降级策略。

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
### 2025-11-15 (TreeBuilder Slice Trace Implementation)
- 完成 Rust 端 `tree_builder_slice_trace` feature：新增 `TreeBuilderEvent{Kind}`、`TreeBuilderTracer` 与 `TreeBuilder::with_tracer`，在 `push`/`push_slice`/`push_leaf_slice`/`pop` 发出 `PushFrame`、`ExtendFrame`、`LeafSlice`、`EnterChild`、`MergePop` 事件。
- 更新 `TreeBuilderSliceStack.md` 记录落地情况，并新增测试 `rope/tests/tree_builder_slice_trace.rs` 验证 feature 启用时能捕获事件。
- 运行 `cargo test -p xi-rope` 及 `cargo test --features tree_builder_slice_trace`（在 `xi-editor-ph7/rust/rope` 下）确认默认/启用模式均通过。
### 2025-11-15 (TreeBuilder Trace Exporter)
- 扩展 `export-serde-fixtures` 支持 `--tree-builder-trace` / `--tree-builder-dir` 参数，启用新 feature 时可输出 `basic_slice_plan.json`（稳定节点 ID + 区间元数据）。
- 若未启用 `tree_builder_slice_trace`，CLI 会提示缺少 feature；默认序列化导出逻辑保持不变，可同时指定 `--dir` 与 `--tree-builder-trace`。
- `Cargo.toml` 引入常规依赖 `serde_json`，并在 `cargo check`, `cargo check --features serde,tree_builder_slice_trace` 下验证通过。
- `scripts/refresh_serialization_fixtures.ps1` 新增 `-ExportTreeTrace` 开关，可在刷新黄金夹具时同时调用 CLI 生成 `tree_builder_slice` 目录下的 trace 资产。

### 2025-11-16 (SubAgent Capability Exploration)
- **能力验证**：通过两次实战测试确认 SubAgent 与主 Agent 能力几乎对等（相同模型/工具集/系统提示词），仅缺少 `runSubagent` 递归分派能力。
  - **测试 1（代码搜索）**：委派搜索 `ILeafOperations` 相关文件，SubAgent 自主并行使用 `grep_search`/`semantic_search`/`read_file`，返回 11 个文件清单 + 接口签名 + 设计意图分析。
  - **测试 2（功能实现）**：委派为 `Node.Generic.cs` 实现 `ValidateInvariants` 与 `ToDebugString` 方法，SubAgent 独立完成设计、编码、测试（新增 7 个单元测试），并自主运行 `dotnet build` + `dotnet test` 验证通过。
- **文档化工作模式转变**：在 `AGENTS.md` 新增"🎯 SubAgent 委派机制（AI 小组长模式）"章节（约 140 行），涵盖：
  - 能力边界说明（对等工具集、多轮调用、无状态、单向通信、无递归）
  - 工作模式对比图（传统模式 vs 小组长模式）
  - 适用场景判断表（单模块实现、Bug 修复、代码搜索、文档调研等 8 类任务）
  - 委派任务五项原则（任务边界、指令自包含、产物格式、避免递归、文件记忆）
  - 委派模板示例与实战案例参考（2 个真实案例）
  - 并行任务编排策略（识别独立任务 → 批量委派 → 收集产物 → 综合验收 → 更新记忆）
  - 失败处理与迭代指南、注意事项
- **认知提醒机制**：明确 SubAgent 也会读取本文档但缺少 `runSubagent`，确保"未来的自己"在不同会话中能识别当前身份（主 Agent / SubAgent）并采用对应工作模式。
- **测试基线更新**：`dotnet test Xi.Editor.sln` 从 102 项增至 106 项（新增泛型 Node 诊断测试），全部通过。

### 2025-11-14 及以前（摘要）
- 与 Stage A/B/C 相关的 Subset/Delta/Engine 序列化镜像、Rope 字符串 helper 抽离与 Cursor 缓存等成果，均已在"已完成事项"对应条目中完整记录。
- Rust 工作区瘦身、TreeBuilder Trace/Metric helper 试点以及早期文档整合等行动，详见"已完成事项"，此处仅保留概览以减轻日志冗长。