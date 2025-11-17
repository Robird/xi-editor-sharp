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
- 通过 ILSpy 导出 + 摘要化处理生成 `docs/skeleton/xi.Core.decompiled.cs`，现可与 `docs/skeleton/rope.md` 对照查看 Rust/C# 两侧的类型骨架，用于统一接口设计与差异审视。

## 工作节奏建议
当前仅由人类开发者与 AI Coder 协作，执行节奏按单次 AI 会话推进；每次会话收尾前需同步更新本文件与相关计划文档。
1. **进入仓库**：优先阅读“当前聚焦事项”，确认阻塞与最新决策，必要时调整计划。
2. **执行任务**：按优先级推进，并实时更新“当前聚焦”“技术笔记”“风险”。
3. **任务完成**：将成果移动至“已完成事项”，在“工作日志”记录关键行动，检视下一步。
4. **质量门禁**：所有代码改动前后需跑通构建/测试，并记录结果。

### 协作与工具心得
- 充分利用 IDE/GUI 工具加速批量操作（如重命名、导航、格式化等），必要时可直接请人类协作者协助执行；相比 RL 阶段的"独立作业"要求，当前环境鼓励主动寻求外部工具/伙伴配合以提升效率。
- `grep_search` 适合作为 `rg` 的轻量替代，按 `query`/`includePattern` 组合即可精准过滤文件，复杂模式时记得设置 `isRegexp=true`；`list_code_usages` 可直接询问 LSP，传入 `symbolName` 与候选定义文件就能获得 Rust 引用/实现清单，优先使用这两项工具再考虑手动 `read_file` 或终端检索。
- 当需要一次性收集多个角色观点时，可像 2025-11-18 星形会议那样，新建 `docs/meetings/<date>-<topic>-chat.md` 充当“聊天室文件”，由主持人在文件头说明议题与顺序，各角色按章节追加署名发言并在汇报前更新各自认知档案；这种模式既能保留上下文，又允许异步补充，使多人会议可在缺乏实时聊天的环境下顺利推进。

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
│   ├─ QA Engineer（agents/qa-engineer.md）✅ 已入职
│   │   ├─ 职责：测试设计、黄金夹具维护、Parity/Stage D 验证
│   │   └─ 状态：认知档案首次填充，记录 8 条测试资产索引、169/169 基线及 R8/R9/R10 风险监控
│   │
│   └─ Information Researcher（agents/information-researcher.md）✅ 已入职
│       ├─ 职责：维护信息索引、执行定向检索（仅服务 AI 架构师）、推送文档改动摘要
│       └─ 状态：认知档案包含 13 条索引、7 条监控清单，重点跟踪 AGENTS/m3 计划/Stage D schema
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
- **Stage D Breaks/Diff/Search parity**：本次确认 `serde_fixtures/breaks_descriptors.rs`、`diff_regions.rs`、`search_spans.rs` 已实现导出逻辑并与 `snapshots.rs` 共享 `RangeSnapshot`/`PathFrameSnapshot`；CLI 入口存在 `--breaks-descriptors`/`--diff-regions`/`--search-spans`，对应目录已在 `tests/xi.Core.Tests/Fixtures/` 落地。下一步需在 `scripts/refresh_serialization_fixtures.ps1`/`refresh_all_assets.py` 默认打开这些 flag，刷新 manifest payload hashes，更新 Stage D Playbook `[StageD::ParityAssets]` 与 `StageDDescriptorLoaderTests` 以覆盖新资产，并在 `[QA-IngestionSmoke]` 记录 loader/CLI 组合校验。
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
  - 更新 `docs/rust-refactor/shared-node-api.md`、`docs/skeleton/xi.Core.decompiled.cs` 与 `AGENTS.md`，记录 helper 封装完成与后续诊断计划。
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
文档同步 + schema/阈值待交付

### 🚀 M3 实施已启动（2025-11-16）
详见 `docs/architecture/m3-implementation-plan.md` v1.2 与 `docs/architecture/m3-architect-decision.md`。

**当前状态**：✅ 计划已批准，C# Implementer 开始 T1.1（游标结构设计）

**关键任务**：
1. **游标系统实现**（6-8.5 天）：`NodeCursor` 基于字符串特化，通过 81 项 Rope 测试 + 10 个 CursorDescriptor JSON Parity 样本
2. **泛型接口挂钩**（1 天）：设计 `INodeCursor<TInfo,TLeaf>` 接口，在 `NodeCursor` 中预留适配器
3. **Chunk 迭代器骨架**（3-4 天）：临时返回 `ReadOnlyMemory<char>`，记录性能基准
4. **Grapheme 降级实现**（2 天）：Surrogate 安全 + 遥测计数器

**监控机制**：
- 日常：C# Implementer 每日更新认知档案，Architecture Mapper 每 2-3 天检查文档同步
- 周会：每 5-7 天星形会议（进度对齐、阻塞讨论、优先级调整）
- 里程碑：游标完成（第 10 天）、Chunk 完成（第 15 天）、M3 验收（第 20 天）

**AI 员工更新**：QA Engineer (`agents/qa-engineer.md`) 与 Information Researcher (`agents/information-researcher.md`) 已完成入职；后续需安排 QA 负责 parity ingestion smoke + 1 MB 基准，Information Researcher 提供星形会议文档索引与 Stage D schema 日志。

### M3 星形会议（2025-11-18）行动项
- **C# Implementer（11/19 前）**：
  - 解除 `dotnet test -v m` 锁文件并回收最新 169/169 结果；
  - 合入 `_editVersion` 版本票据、让 `CursorDescriptorParityTests` 消费全量 11 份 JSON；
  - 补交 `RopeChunkEnumeratorDiagnostics`（chunk/line 计数、最大 chunk、复制字节）、准备 1 MB chunk/line 微基准脚本接入 QA。
- **Rust Porter（11/17-11/19）**：
  - 冻结 `cursor_descriptors`/`chunk_descriptors`/`grapheme_descriptors` schema 与 feature flag 说明，补充 `cursor_state`/`tree_builder_slice_trace` 输出；
  - 更新 `export-serde-fixtures`/`refresh_serialization_fixtures.ps1` 文档，确保 Stage D 默认刷新新目录。
- **Architecture Mapper（11/16-11/19）**：
  - 回写 `rope-port-mapping.md`、`type-system-migration-log.md`、`design-divergence-log.md` 中 Leaf/Cursor/Chunk/Grapheme 现状与缺口；
  - 与架构师确认 Grapheme 遥测阈值，并在 `design-divergence-log.md` 记录；
  - 推送周报草稿，覆盖 R8/R9/R10 与 G1-G6 的状态。
- **QA（11/19-11/21）**：
  - 跑通 chunk/line/glyph parity 资产 ingestion smoke，回传 1 MB 文本微基准数据；
  - 帮助验证 `metadata.rust_commit`/`generated_at_unix_millis` 的留存策略。

### 其他待办事项
1. **Stage D 自动化落地**
  - 将 `run_all_checks`（含 serde/无 serde）与 `dotnet test` 整合为单一脚本或 CI 节点，并记录失败回溯策略。
  - 固化夹具刷新 checklist：Rust 导出、C# 验证、文档更新与 `AGENTS.md` 记载缺一不可。
2. **骨架映射实时维护**
  - 每次调整 `xi.Core.Rope` 或 Rust helper 后，立即刷新 `docs/architecture/rope-port-mapping.md`、`docs/architecture/type-system-migration-log.md` 与 skeleton 文档。
  - 对标困难模块新增/关闭的阻塞项，确保日志准确反映当前状态与负责方。
3. **Diff/Search/Breaks 骨架与依赖梳理**
  - 在 C# 中创建模块骨架并标注 Rust 依赖 helper。
  - 同步 `port-blueprint`/`rope-port-mapping` 中的状态，避免后续忘记接线。
4. **SharedNode instrumentation**
  - 拟定 Rust/C# 共同的调用计数与 ptr_eq 校验策略，准备在调试阶段引入可控开关。
5. **文档与外部记忆同步**
  - 会话结束前检查 `AGENTS.md`、`docs/architecture/design-divergence-log.md` 与各专题文档是否一致，确保跨会话记忆准确。

## 未来候选事项（Backlog）
- 导入参考仓库中的黄金编辑 trace，构建跨语言对照测试套件。
- 探索 `char[]`/`ArrayPool<char>`/`ReadOnlyMemory<char>` 作为叶片存储的可行性，并评估对 GC 压力与性能的影响。
- 设计更完善的可观察性方案（结构摘要、调试可视化、Telemetry）以支撑规模化调试。
- 规划插件示例（echo、spellcheck 等）与最小 JSON-RPC 宿主，验证核心库的嵌入式 API 能力。
- 评估协同编辑/CRDT 功能的技术路线，明确所需的 Delta/Subset 扩展与一致性测试。

## 摘要Agent提示
- **用户提示词已完成重大升级 v3.0**（2025-11-17）：`scripts/user-root-prompt.md` 从"瀑布式单次执行"进化为"迭代循环推进"模式，核心范式从"启动 → 执行一次 → 收尾"转变为"{目标-差距-思考-委派} PDCA 循环，直至目标达成或遇到真正阻塞"，充分利用无限次工具调用能力实现主动推进而非被动等待，通过用户反馈识别"瀑布式思维陷阱"并重构为敏捷迭代模式。
- **SubAgent 委派机制已建立并验证**：通过两次实战（代码搜索 + 功能实现 + 测试）确认 SubAgent 与主 Agent 能力对等，可独立完成完整开发周期；相关工作模式、适用场景、委派原则已文档化于 `AGENTS.md`"协作与工具心得"章节。
- **AI Team 组织已成熟**：5 位核心员工（Rust Porter、C# Implementer、Architecture Mapper、QA Engineer、Information Researcher）已入职并建立认知档案，星形会议、文档先行、认知档案机制等最佳实践已沉淀于 `agents/architect.md` § 成功经验总结。
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
| 时间 | 交付 | 摘要 | 真相源 |
| --- | --- | --- | --- |
| 2025-11-19 | Stage D manifest ledger & Playbook 模板对齐 | `StageDDescriptorLoader` 校验 manifest ledger，`docs/csharp-refactor/rope-serialization-fixture-playbook.md` 对齐 Stage D/QA anchors，`docs/architecture/system-overview.md` 建立跨文档地图。 | docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::FixtureFlow]；docs/architecture/system-overview.md#[SO-Map] |
| 2025-11-18 | TreeBuilder slice trace loader + manifest 校验脚本 | `TreeBuilderSliceTraceLoader`/tests 产出，`scripts/verify_fixture_manifest.py --update` 支持 canonical hash，`refresh_all_assets.py` 接入 Stage D 步骤。 | docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]；docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::ParityAssets] |
| 2025-11-17 | Stage D descriptor loader + TreeBuilder tracer + 文档模板 rollout | `StageDDescriptorLoader`/tests 交付，TreeBuilder tracer 注入 + 测试，Architecture 文档模板与 `scripts/refresh_all_assets.py` 上线，形成 goal-tree ↔ Stage D 单一事实源。 | docs/architecture/type-system-migration-log.md#[TS-B2]；docs/architecture/port-blueprint.md#[BP-GoalTree] |
| ≤2025-11-16 | Stage A/B/C baseline + SharedNode/AI Team 建设 | Node 写时复制、StringLeafOperations parity、Subset/Delta/Engine JSON 镜像、AI Team + SubAgent 机制完成。 | docs/architecture/port-blueprint.md#[BP-Milestones]；docs/architecture/design-divergence-log.md#[Div-Active] |

> 更多执行细节、命令与验证可在“## 工作日志”与所列真相源文档中查询；2025-11-16 之前的完整历史亦可透过这些文档或 Git 历史追溯。

## 工作日志
### 2025-11-19 (Breaks/Diff/Search skeleton smoke ✅)
- **实现**：C# Implementer 补齐 `Rope/Breaks/BreaksTree.cs`（含 `BreakPlan`/`BreakBuilder`）、`Diff/DiffBuilder.cs`/`DiffRegion.cs`/`LineHashDiff.cs` 与 `Search/Finder.cs`/`SearchOptions.cs`/`SearchResult.cs`，为 `[TS-B5]` 要求的 Breaks/Diff/Search 模块提供最小占位树、diff builder 和 finder API，并让 `StageDDescriptorLoader` 可在 skeleton 阶段回放 sample manifest。
- **验证**：`dotnet test Xi.Editor.sln -v m --filter "BreaksSkeletonTests|DiffSkeletonTests|SearchSkeletonTests|StageDDescriptorLoaderTests"` 现作为 smoke 命令覆盖 Stage D loader + 三大 skeleton（BreakPlan materialization、DiffBuilder ops、Finder spans），结果 ✅；QA 只需等待 Rust exporter 写入 `breaks_descriptors.json`/`diff_regions.json`/`search_spans.json` 即可扩充数据面。
- **文档**：Architecture Mapper 回写 `[RPM-Matrix]`/`[RPM-Actions]`、`docs/architecture/system-overview.md#[SO-Map]` 与本日志，记录状态从“Rust-only/Spec ready”跃迁至“Skeleton ready”，并在 `[SO-Map]` 引用“C# skeleton ready（BreakPlan/DiffBuilder/Finder）”以提示 Goal Tree G3/G4 的新事实来源。
### 2025-11-18 (Stage D exporter 稳定化 + refresh_all_assets 全绿)
- **动作**：协调 Rust Porter 在 `chunk/grapheme/breaks/diff/search` exporter 内部读取既有 JSON，保留 `generated_at_unix_millis`，避免重复导出时仅因时间戳造成 hash 漂移；`./xi-editor-ph7/rust/run_all_checks --filter serde-fixtures` 再次通过。
- **C#/QA**：C# Implementer 同步 `StageDDescriptorLoader`/Tests，采用 manifest ledger count + 实际 hash（chunk=`e62a4faa…`、grapheme=`c6b1721d…`、breaks=`ab2f746e…`、diff=`8c400ea4…`、search=`ce068c52…`），QA Engineer 三次运行 `python scripts/refresh_all_assets.py`（先失败→hash 更新→最终成功），记录 `StageDDescriptorLoaderTests`、`verify-stage-d`、hash 校验日志。
- **文档**：Architecture Mapper 更新 `[StageD::ParityAssets]`/`[StageD::FixtureFlow]`/`[StageD::FeatureGates]`，注明 2025-11-18 稳定刷新（`rust_commit=96ce8ddf…`、`feature_gates=["serde"]`、counts 20/11/668 + 3/3/3）及“重复运行复用 generated_at”原则。
- **结果**：`python scripts/refresh_all_assets.py` 现可在默认配置跑通全部步骤（goal-tree → skeleton → dotnet build → stage-d-fixtures → verify-stage-d → ilspy → skeletonizer），输出 “All steps completed.”；新的 Stage D 资产/manifest/测试/文档待审阅后可提交。

### 2025-11-18 (Stage D refresh脚本 Verbose 冲突解除)
- **问题**：`python scripts/refresh_all_assets.py --only stage-d-fixtures` 在触发 `pwsh -File scripts/refresh_serialization_fixtures.ps1 -Verbose` 时命中 PowerShell `MetadataError`，因为脚本自定义 `[switch]$Verbose` 与通用 `-Verbose` 冲突，Stage D 流水线在调用入口即中断。
- **修复**：委派 QA Engineer 将脚本升级为 `[CmdletBinding()]`，移除自定义 `-Verbose`，改用 `$PSBoundParameters.ContainsKey('Verbose')` 记录调用者偏好并保留详细命令回放；其余参数与默认行为保持不变。
- **验证**：运行 `python scripts/refresh_all_assets.py --only stage-d-fixtures --continue-on-error`，`pwsh` 步骤不再报错，Rust run_all_checks/export 以及 dotnet Stage D loader smoke 顺利开启；当前失败仅剩 `cargo clippy` 的 `clippy::len-zero`（`rope/src/serde_fixtures/breaks_descriptors.rs:105`）与 `StageDDescriptorLoaderTests` manifest hash 漂移（新 `96ce8ddf…` vs 旧 `f740a440…`），后续需分别由 Rust Porter/C# Implementer 更新。

### 2025-11-18 (Stage D Breaks/Diff/Search readiness audit)
- **Rust exporter现状**：梳理 `serde_fixtures/breaks_descriptors.rs`、`diff_regions.rs`、`search_spans.rs` 与共享辅助 `snapshots.rs`，确认 `RangeSnapshot`/`PathFrameSnapshot`/`frames_from_descriptor` 已支撑 Breaks/Diff/Search 导出，`export-serde-fixtures` 中的 `--breaks-descriptors`/`--diff-regions`/`--search-spans` 选项可以把 JSON 写入 `tests/xi.Core.Tests/Fixtures/` 对应目录。
- **C#/脚本现状**：`StageDDescriptorLoader` 及现有测试只覆盖 chunk/grapheme，刷新脚本尚未把新 flag 设为默认；`fixtures.manifest.json`、`docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::ParityAssets]` 也尚未登记 Breaks/Diff/Search 的 schema hash。
- **后续思路**：在 `scripts/refresh_serialization_fixtures.ps1`/`refresh_all_assets.py` 中默认导出 Breaks/Diff/Search，刷新 manifest 后扩展 StageD loader 测试、`[QA-IngestionSmoke]` 流程与 parity 文档，确保 QA 证据链覆盖全部 Stage D 资产，并为 `StageDDescriptorLoader` 增加 Breaks/Diff/Search DTO/断言。

### 2025-11-19 (Stage D manifest ledger 校验)
- **Loader 强化**：`StageDDescriptorLoader` 现会把 `fixtures.manifest.json` 的 ledger 条目（name/path/count/schema_hash/payload_hash）hydrate 成 `StageDFixtureLedgerEntry`，并在加载 chunk/grapheme JSON 时检验 manifest 计数与 schema version 是否匹配，避免 hash/计数漂移未被察觉。
- **测试覆盖**：新增 `StageDDescriptorLoader` ledger 单元测试，验证 chunk 与 grapheme 条目的 `count`、`schema_hash`、`payload_hash` 与 manifest 真值一致；`dotnet test Xi.Editor.sln -v m --filter StageDDescriptorLoaderTests`（4/4 ✅，15.8s）作为 smoke 记录。
- **文档同步**：`docs/csharp-refactor/rope-serialization-fixture-playbook.md` 的 `[StageD::FixtureFlow]`/`[QA-IngestionSmoke]` 说明 loader 现会返回 ledger，并强调 loader smoke 会校验 canonical hash + manifest 计数，供 QA/Stage D CLI 复用。

### 2025-11-17 (Stage D descriptor loader + 文档联动)
- **实现落地**：委派 C# Implementer 在 `src/xi.Core/Rope/Diagnostics/Descriptors/StageDDescriptorLoader.cs` 建立 manifest/chunk/grapheme loader，并新增 `tests/xi.Core.Tests/Diagnostics/StageDDescriptorLoaderTests.cs` 读取真实夹具校验 metadata、`emoji_cluster_block` chunk 与 `zwj_family` grapheme；为 DTO 添增 `JsonPropertyName` 注解。`dotnet test Xi.Editor.sln -v m --filter StageDDescriptorLoaderTests` 及全量 `dotnet test Xi.Editor.sln -v m`（176/176 ✅）皆通过。
- **文档同步**：Architecture Mapper 更新 `[TS-B3]`、`[RPM-Matrix]`/`[RPM-Actions]` 与 Stage D Playbook `[StageD::FixtureFlow]/[StageD::ParityAssets]/[QA-IngestionSmoke]`，说明 loader 现可直接消费 manifest，并记录下一步需把 QA smoke/Stage D CLI 接线到该 API。
- **下一步**：驱动 QA Engineer 把 loader 纳入 `[QA-IngestionSmoke]` 自动化，联动 Stage D CLI/`refresh_all_assets.py`；后续推进 `TreeBuilderTracer` 注入与 slice trace replay，关闭 `[TS-B2]/[TS-B3]` 的剩余动作。
### 2025-11-17 (Stage D loader smoke自动化 + TreeBuilder tracer接线)
- **自动化落地**：QA Engineer 更新 `scripts/refresh_serialization_fixtures.ps1`（新增 `-SkipStageDLoaderTest`，默认总是运行 `dotnet test Xi.Editor.sln --filter StageDDescriptorLoaderTests`，即便 `-SkipDotnet`），`scripts/refresh_all_assets.py` 的 `stage-d-fixtures` 步骤同步描述“Export Rust fixtures + run Stage D loader smoke”；Playbook `[StageD::FixtureFlow]`/`[QA-IngestionSmoke]` 与 `agents/qa-engineer.md` 已写明跳过 smoke 必须记录原因。
- **TreeBuilder tracer**：C# Implementer 将 `ITreeBuilderTracer` 注入 `TreeBuilder` 实际路径（`PushLeaf`/`PushNode`/`MergeLeaf`/`MergeInternal`/`PopFrame`/`BuildCompleted`/`Reset`），只在 `Tracer.IsEnabled` 时计算 UTF-8 字节与 32 字符 preview，并新增 `TreeBuilderTracerTests` 确认事件序列；`dotnet test Xi.Editor.sln -v m --filter TreeBuilderTracerTests` 与全量 178/178 ✅ 通过。
- **后续**：等待 Rust Porter 提供 `tree_builder_slice_trace` fixture，与新 tracer 事件对拍以关闭 `[TS-B2]`；QA 需在下一次 Stage D 刷新中检验 loader smoke 日志，继续构建 chunk bench/telemetry 证据链。
### 2025-11-17 (TreeBuilder tracer skeleton + Stage D descriptor DTO 回写)
- **实现落地**：C# Implementer 增加 `Tree/TreeBuilderTracer.cs`（`TreeBuilderEventKind/Event/ITreeBuilderTracer/NoOpTreeBuilderTracer`）并在 `TreeBuilder` 注入占位属性，同时创建 `Rope/Diagnostics/Descriptors/*` DTO（Chunk/Line/Grapheme + manifest metadata），作为 `[TS-B2]`/`[TS-B3]` Skeleton Coverage 的 C# 端映射。
- **验证**：执行 `dotnet test Xi.Editor.sln -v m`（173/173 ✅）确认新增文件未破坏基线；Architecture Mapper 将成果同步到 `rope-port-mapping.md#[RPM-Matrix]`/`[RPM-Actions]`、`type-system-migration-log.md#[TS-B2]/#[TS-B3]` 与 `design-divergence-log.md#[Div-Active]`。
- **下一步**：计划由 C# Implementer 构建 Stage D manifest loader + QA 钩子，并在 tracer 注入后运行 slice-trace replay；Architecture Mapper/QA 将据此更新 `[StageD::FixtureFlow]`、`[QA-ChunkBench]`，推动 `[TS-B2]/[TS-B3]` 从 skeleton 进入 loader/telemetry 阶段。
### 2025-11-17 (Rope Skeleton Coverage Mapping)
- **信息采集**：激活 Information Researcher 梳理 `docs/skeleton/rope.md` 与 `docs/skeleton/xi.Core.decompiled.cs` 中的 Rope Core/Cursor/Metrics/Delta/Chunk/Grapheme 类型，形成 Rust↔C# 对照清单并登记在 `agents/information-researcher.md`。
- **文档更新**：在 `docs/architecture/rope-port-mapping.md#[RPM-Matrix]` 新增“Skeleton Coverage”列，逐行标记 Rust skeleton 现状与 C# 覆盖差距（TreeBuilderTracer、CursorDescriptor DTO、Breaks tree、Chunk/Grapheme exporter 等），为后续骨架映射方案提供单一事实来源。
- **后续聚焦**：依据新列输出，准备为 C# Implementer 制定 Breaks/Chunk/Grapheme skeleton 接入计划，并将缺口同步至 `[TS-B2]`/`[TS-B3]`/`[TS-B5]`。 
### 2025-11-17 (TS Blocker & Divergence Alignment)
- **Blocker细化**：在 `docs/architecture/type-system-migration-log.md` 中为 `[TS-B2]/[TS-B3]/[TS-B5]` 补充 Skeleton Coverage 事实（TreeBuilderTracer、Chunk/Line DTO、Breaks/Diff/Search 目录），并新增“建骨架占位”“记录 Rust-only slice trace”等具体 next steps。
- **降级登记**：`docs/architecture/design-divergence-log.md#[Div-Active]` 新增“TreeBuilder slice trace (Rust-only)”项，说明目前完全依赖 Rust exporter，并将 Stage D Playbook 作为解锁出口。
- **成果**：Skeleton 缺口现在同时体现在 `[RPM-Matrix]`、`[TS-Bx]` 与 `[Div-*]`，后续 PDCA 可直接引用文档驱动行动。
### 2025-11-17 (Stage D ingestion smoke + Serialization filter)
- **命令执行**：在仓库根依次运行 `find tests/xi.Core.Tests/Fixtures -name '*.json' -print0 | sort -z | xargs -0 sha256sum`（再用 `python - <<'PY' ... sort_keys=True, ensure_ascii=False` 复算 canonical SHA256）以及 `dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter Serialization`，10/10 用例 2.5s 通过，hash 与 `fixtures.manifest.json`/`[StageD::ParityAssets]` 完全一致。
- **QA 档案同步**：`agents/qa-engineer.md` 的“当前监控”“[QA-IngestionSmoke]”与“最近完成”记录此次 smoke，风险从“Blocked”降为“✅ Manifest-backed”，后续待办转向 `refresh_all_assets.py --only stage-d-fixtures` 的 canonical hash 自动化。
- **影响**：Stage D 手册、`QA` anchors 现可引用 manifest 作为事实来源，后续 chunk bench/telemetry 只需基于该 manifest 重放即可。

### 2025-11-17 (Rope Port Mapping manifest refresh)
- **文档改写**：`docs/architecture/rope-port-mapping.md` 在 `[RPM-Matrix]` 与 `[RPM-ParityAssets]` 标注 2025-11-17 manifest（`rust_commit=7ac917a05be4bb526844d5cdaa842030411800e5`、`cli_rev=0.3.0`、cursor/chunk/grapheme hashes），并说明命令 `python scripts/refresh_all_assets.py --only stage-d-fixtures` 与 `[QA-IngestionSmoke]` 的对接。
- **行动项更新**：`[RPM-Actions]` 由“解锁 CLI schema”改为“自动化 canonical hash diff”“将 manifest 接入 `[QA-ChunkBench]`/`[QA-Telemetry]`”“捕获 Grapheme iterator CLI trace”“MetricAdapter 设计”“Breaks/Diff/Search skeleton”，与最新状态对齐。
- **认知档案**：`agents/architecture-mapper.md` 记入“Stage D parity assets updated”并在“当前聚焦”强调 canonical hash 自动化 + QA 联动，确保后续任务指向新的事实表。

### 2025-11-17 (AI Team Doc Refresh Kickoff)
- **状态确认**：`document-structure-template.md` 覆盖的 7 份架构文档（Blueprint/Mapping/TS Log/Design Divergence/M3 Plan/M3 Decision/Stage D Playbook）已完成模板化改造，并由 `scripts/refresh_all_assets.py` 提供 goal-tree 同步验证，视为 Phase 1 成功收官。
- **整顿指令**：以新版文档规范为准绳，所有 AI 员工需整理各自认知档案（路径如下），确保 front-matter、职责/进度/风险章节一致，并借机梳理最新认知：
  1. `agents/architecture-mapper.md` —— 对齐 Goal Tree/Stage D 锚点维护职责，更新“最近完成/待办”与文件索引，补充 `document-structure-template.md` 维护计划。
  2. `agents/csharp-implementer.md` —— 采用任务/测试/阻塞三栏，汇总 M3 游标/Chunk/Grapheme 依赖与测试基线；列出需配合的 CLI/Stage D 需求。
  3. `agents/rust-porter.md` —— 将 helper/CLI/schema 维护清单按模块分组，标记 `cursor_state`/`iterator_facade` 等 feature gate 状态，并写明与 Stage D 的输出接口。
  4. `agents/qa-engineer.md` —— 对齐 Stage D Playbook 的 `[QA-*]` 锚点，记录最新 169/169 测试基线、烟雾/基准脚本与待验证阈值。
  5. `agents/information-researcher.md` —— 用“知识索引 + 监控清单”双栏描述当前跟踪的文档/脚本，补齐对 Goal Tree YAML/刷新脚本的监测策略。
- **时间要求**：各档案需在下次 PDCA 循环启动（<= 24h）前提交更新，可酌情使用 runSubagent 完成；更新后在各自“最近完成”章节登记并通知架构师复核。

### 2025-11-17 (Refresh All Assets Script)
- **交付物**：新增 `scripts/refresh_all_assets.py`，串联 `goal_tree_sync.py`、`refresh_skeleton_docs.py`、`dotnet build Xi.Editor.sln`、`ilspycmd -o docs/skeleton ...` 与 `tools/Skeletonizer`，提供 `--only/--skip/--dry-run/--continue-on-error` 选项，默认一次跑完 Goal Tree ↔ Skeleton ↔ 构建 ↔ 反编译流水线。
- **执行验证**：在仓库根运行 `./scripts/refresh_all_assets.py`，成功更新 Goal Tree snippet（meta 时间戳刷新）、重建 Rust skeleton、构建 xi.Core、调用 ILSpy 生成 `docs/skeleton/xi.Core.decompiled.cs` 并通过 Skeletonizer 再压缩 316 个函数体；命令输出纳入本日志以便追踪。
- **使用指引**：`--list` 查看步骤，`--only goal-tree,dotnet-build` 可局部刷新，若 `ilspycmd` 缺失脚本会提示安装；建议后续把脚本接入 `run_all_checks` 或 Stage D 手册，确保一键刷新覆盖 Goal Tree/骨架/反编译产物。

### 2025-11-17 (晚) (Architecture Docs Refactor Complete)
- **目标达成**：基于 `document-structure-template.md` 完成 7 个核心文档重构，所有文档符合生产标准。
- **质量验证**：
  - 第 1 轮审计发现  缺少 6 个关键锚点（`[MP-T1]`/`[MP-T3]`/`[MP-T4]`/`[MP-R8]`/`[MP-R9]`/`[MP-R10]`），导致 60+ 跨文档引用失效
  - 立即修复：在章节标题下/风险表内添加 `<a id="...">` 锚点标记
  - 第 2 轮验证确认：所有锚点存在，82 处 `[MP-*]` 引用全部可达，P0 阻塞解除 ✅
- **交付成果**：
  - ✅ Front-matter 完整性（7/7 文档）
  - ✅ Goal Tree 同步（`port-blueprint.md` ↔ `m3-implementation-plan.md`）
  - ✅ QA/Stage D 锚点验证（8 个锚点全部定义）
  - ✅ 架构文档锚点系统（60+ 跨文档链接可跳转）
- **PDCA 循环实战**：首次应用 v3.0 迭代推进模式，单次会话内完成"启动 → Plan(差距分析) → Do(委派验证) → Check(发现阻塞) → Act(立即修复) → 再次 Check(验证通过) → 收尾"完整流程，无需多次用户输入。
- **AI Team 协作**：Architecture Mapper 承担两轮验证任务，生成详细审计报告（锚点清单/修复建议/影响分析），协作高效。

### 2025-11-17 (User Prompt Upgrade v3.0 - 迭代循环模式)
- **核心突破**：从"瀑布式单次执行"转变为"持续迭代推进"模式
  - **问题识别**：用户指出"所有设想都是瀑布式的（启动→执行一次→收尾→等待下次输入），而非迭代与动态的{目标-差距-思考-委派}循环，工具调用无限制为何要陷入等待？"
  - **认知转变**：意识到自己陷入"单次会话 = 单次任务"的思维陷阱，忽略了"一次会话可以持续迭代推进多个任务直到目标达成"的可能性
- **v3.0 核心改进**：
  - **PDCA 循环范式**：Plan（评估差距）→ Do（委派执行）→ Check（验证整合）→ Act（决策下一步）→ 回到 Plan，直到目标达成或遇到真正阻塞
  - **停止条件明确**：仅 2 种情况停下（✅ 目标已达成、🚨 遇到无法解决的阻塞），其余情况立即进入下一轮迭代
  - **主动推进原则**：错误模式"完成任务 A → 等待用户输入" vs 正确模式"完成任务 A → 检查目标 → 立即启动任务 B → ... → 目标达成后汇报"
  - **启动简化**：从"场景 A/B/C 三档启动"简化为"最小化启动（< 1 分钟）→ 立即进入 PDCA 循环"，启动只是手段，迭代推进才是核心
- **工作模式转变**：
  - v1.0/v2.0：架构师 = "任务执行者"（用户说做什么就做什么，做完等待下次指令）
  - v3.0：架构师 = "自主推进者"（用户给定目标，架构师持续迭代直到目标达成或需要外部支援）
- **实际案例对比**：
  - 瀑布式（v2.0）：用户输入提示词 → 架构师完成子任务 1 → 向用户汇报 → 等待用户再次输入 → 架构师完成子任务 2 → ...（需要 N 次用户输入）
  - 迭代式（v3.0）：用户输入提示词 → 架构师进入 PDCA 循环（子任务 1 → 2 → 3 → ... → N）→ 目标达成后一次性汇报（仅需 1 次用户输入）
- **更新文档**：`scripts/user-root-prompt.md`（v3.0）、`AGENTS.md` § 摘要 + 工作日志

### 2025-11-17 (User Prompt Upgrade v2.0)
- **元任务交付**：基于用户反馈与实验验证，重构 `scripts/user-root-prompt.md` 为场景化灵活指南（v1.0 → v2.0）。
- **设计方法**：
  1. **v1.0 设计**：通过 runSubagent 激活 Architecture Mapper + C# Implementer 深度评审，设计 200+ 行 5 步完整流程
  2. **用户反馈**：指出 3 个关键问题（AGENTS.md 自动注入无需读取、逐个读员工档案消耗过高、过度规范化丧失灵活性）
  3. **实验验证**：通过 runSubagent 模拟架构师执行 v1.0 提示词，发现第三步"扫描 5 个员工档案"消耗 ~30K tokens（40%）且过于机械，缺少"快速通道"
  4. **v2.0 重构**：场景化启动（快速 < 1 分钟/标准 3-5 分钟/深度 5-10 分钟）+ 智能扫描（grep 替代逐个打开）+ 丰田管理哲学（Kaizen/Jidoka/Respect）
- **核心改进**：
  - **场景 A（快速启动）**：仅读 `architect.md § 当前聚焦` + grep 搜索阻塞，< 1 分钟（适合连续会话）
  - **场景 B（标准启动）**：读架构师记忆（聚焦核心）+ 智能扫描团队（grep/日志）+ 按需定位文档，3-5 分钟（常规会话）
  - **场景 C（深度启动）**：完整恢复认知流程，5-10 分钟（长期中断）
  - **委派原则**：从"7 项必须"简化为"3 个必须 + 1 个信任"，强调给员工主观能动性空间
  - **收尾哲学**：从"7 项强制清单"转为"3 个核心目标 + 按需检查"，避免"填表格"心态
  - **管理哲学**：融入丰田生产方式（Kaizen 改善/Jidoka 自働化/Respect for People）+ 敏捷精神（响应变化/工作软件/个体互动）
- **实验发现**：
  - v1.0 完整流程消耗 ~75K tokens，15-20 分钟（人类等效）
  - v2.0 标准启动压缩至 ~20-30K tokens，3-5 分钟
  - 第三步"逐个打开员工档案"是最大瓶颈（30K/75K = 40%），改用 grep 可降至 < 5K
- **更新文档**：`scripts/user-root-prompt.md`（v2.0）、`AGENTS.md` § 摘要 + 工作日志、`agents/architect.md` § 当前聚焦
- **验证计划**：下次用户使用新提示词时，观察是否能在 1-5 分钟内（视场景）恢复上下文并灵活应变

### 2025-11-19 (System Overview Map Launch)
- **交付**：创建 `docs/architecture/system-overview.md`，补齐 front-matter + `[SO-*]` anchors，并以表格形式串联 Rope Core/Delta-Subset/Engine/Stage D/AI Team/Testing&QA 子系统，提供 `[BP-GoalTree]`、`[RPM-Matrix]`、`[StageD::ParityAssets]`、`[QA-IngestionSmoke]` 等跨文档入口。
- **引用关系**：`[SO-Map]` 将 Goal Tree 与 `[TS-Bx]`、`[MP-Tx]`、Stage D/QA 锚点对齐，`[SO-Responsibilities]` 指向 `agents/*.md` 档案，`[SO-Dependencies]` 阐明 Goal Tree→Stage D→QA 闭环，方便 runSubAgent 读取后直接定位事实来源。
- **后续**：Architecture Mapper 需在每次 Goal Tree/Stage D/QA anchor 更新时同步刷新 `system-overview.md` 与本日志，保持文档治理链路可追踪。
### 2025-11-19 (Stage D Playbook Template Alignment)
- **交付**：重写 `docs/csharp-refactor/rope-serialization-fixture-playbook.md`，补齐 front-matter、`[StageD::*]`/`[QA-*]` 锚点、Stage D 操作清单、Fixture 流程与 Feature Gate 策略；新增 parity 资产哈希表（7 个 sha256）与 Automation backlog。
- **引用关系**：所有 Stage D/QA anchor 现可被 `m3-implementation-plan.md`、`fixtures/parity-fixture-schema.md`、`rope-port-mapping.md`、Goal Tree `stageDAnchors` 字段直接引用；`[Fixture-FeatureGates]` 回指本手册的 Feature Gate 表。
- **质量保障**：按模板要求覆盖 `StageD::StageDChecklist`→`FixtureFlow`→`ParityAssets`→`QA-IngestionSmoke`→`QA-StageDManual`→`AutomationBacklog`→`ChangeLog` 链路，并记录更新要求（AGENTS/agents/*.md/rope-cs-mirror-plan）。
- **后续**：下一轮刷新需根据 exporter 输出更新哈希表并在 `agents/qa-engineer.md` 中登记测试结果；若引入 `goal_tree_sync.py` 校验，应扩展至 Stage D anchors。

### 2025-11-19 (Refresh All Assets Stage D Hook)
- **动作**：在 `scripts/refresh_all_assets.py` 新增 `stage-d-fixtures` 步骤（自动使用 `pwsh` 调用 `scripts/refresh_serialization_fixtures.ps1`），并暴露 `--only stage-d-fixtures` 供定向刷新；`python scripts/refresh_all_assets.py --list` 现会列出该步骤。
- **文档更新**：`docs/csharp-refactor/rope-serialization-fixture-playbook.md` 的 `[StageD::StageDChecklist]` 与 `[StageD::FixtureFlow]` 补充“refresh_all_assets 一键执行”说明，提醒 goal tree / skeleton 流程与 Stage D 可并行维护。
- **影响**：全量 `refresh_all_assets` 现自动跑通 goal tree → skeleton → Stage D → ILSpy → Skeletonizer，符合 Stage D 自动化 backlog 要求；缺少 PowerShell 时脚本会提示跳过该步骤。

### 2025-11-17 (Stage D fixture refresh via refresh_all_assets)
- **执行**：在仓库根运行 `python scripts/refresh_all_assets.py --only stage-d-fixtures`（PowerShell step 调用 `scripts/refresh_serialization_fixtures.ps1 -Verbose`），串行完成 `run_all_checks`、serde 回归三套、`cargo export-serde-fixtures --emit-manifest` 与 `dotnet test Xi.Editor.sln`（173/173 通过），确认新管线可在 Linux + pwsh 环境落地。
- **产物**：`tests/xi.Core.Tests/Fixtures/*.json` 与 `fixtures.manifest.json` 刷新至 Rust commit `7ac917a05be4bb526844d5cdaa842030411800e5`、CLI rev `0.3.0`、feature gate `serde`；chunk/cursor/grapheme 资产分别导出 20/11/668 条记录，对应 SHA256（chunk=`bd863f2237dd…`、cursor=`fe963d909d5c…`、grapheme=`a2b84031c5aa…`）。
- **文档同步**：`docs/csharp-refactor/rope-serialization-fixture-playbook.md` 的 `[StageD::ParityAssets]` 更新所有 hash，并追加 manifest 元数据；提醒 QA 在 `[QA-IngestionSmoke]` 登记本次刷新、Architecture Mapper 依据 manifest 更新 Goal Tree/Stage D anchor。

### 2025-11-17 (Stage D manifest verifier automation)
- **交付**：新增 `scripts/verify_fixture_manifest.py`（canonical JSON `sort_keys=True, ensure_ascii=False, separators=(
### 2025-11-19 (Architecture Docs Template Round 2)
- **交付**：重构 `docs/architecture/m3-architect-decision.md`、`docs/architecture/fixtures/parity-fixture-schema.md`、`docs/architecture/ai-team-design-draft.md`，统一 front-matter 与 `[Decision-M3-*]`、`[Fixture-*]`、`[AIT-*]` 锚点，所有风险/行动/引用回指 `[MP-*]`、`[QA-*]`、`[StageD::*]`。
- **内容调整**：决策书现以概览/裁决/控制/行动表格呈现；Stage D schema 文档新增 change log 与 Stage D 链接；AI Team 草案加入阻塞表、ASCII 组织图与执行阶段路线。
- **挂钩关系**：三份文档已对齐 `document-structure-template.md`；`agents/architecture-mapper.md` 的“最近完成”新增 Template Round 2 事项，标注待建 `system-overview.md` 与 Stage D 细粒度锚点。
- **后续**：待 `docs/architecture/system-overview.md` 创建后在 `[AIT-ExecutionPlan]` 补链；Stage D playbook 拆分锚点后，更新 `[Fixture-*]` 段落引用具体 `[StageD::*]` 节点。
- **Stage D Anchor Alignment**：`port-blueprint.md`、`rope-port-mapping.md`、`type-system-migration-log.md`、`design-divergence-log.md`、`fixtures/parity-fixture-schema.md`、`m3-implementation-plan.md`、`m3-architect-decision.md` 的 `[StageD::]`/`[QA-*]` 链接已全部指向 `rope-serialization-fixture-playbook.md` 精确片段，后续刷新 Stage D 文档时同步维护。

### 2025-11-19 (QA Anchors Stage D Playbook)
- **交付**：在 `docs/csharp-refactor/rope-serialization-fixture-playbook.md` 新增 `[QA-ChunkBench]`（1 MB chunk/line 基准）与 `[QA-Telemetry]`（Grapheme fallback 遥测）章节，写入 dotnet 命令、阈值（<5 MB allocation、>200 MB/s throughput、fallback ≤0.5%）、记录渠道与 `[MP-T3]`/`[MP-R10]`/`design-divergence-log.md` 互链，消除模板中的悬挂 `qaAnchors`。
- **配套更新**：`agents/qa-engineer.md` “最近完成”登记了本次补档，提醒后续运行基准/遥测时需在 `m3-implementation-plan.md §5.3`、`AGENTS.md`、`design-divergence-log.md` 同步数据。
- **后续**：QA Engineer 下一次执行 chunk/telemetry 任务时，应引用新章节并附带实际数值，若阈值被突破即刻升级 `[MP-R10]`。

### 2025-11-19 (Tree Builder Trace Manifest Sync)
- **资产更新**：Rust Porter 导出的 `tree_builder_slice/basic_slice_plan.json` 现由 manifest 管理（`tree_builder_slice_trace@1.0.0`, hash `22724af7fe8b…`）；`scripts/refresh_serialization_fixtures.ps1 -ExportTreeTrace` 单次 `cargo run --features serde,tree_builder_slice_trace` 即可产出 parity+trace+manifest。
- **文档回写**：刷新 `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::ParityAssets]/[StageD::FeatureGates]`、`docs/architecture/rope-port-mapping.md#[RPM-Matrix]/[RPM-ParityAssets]` 与 `docs/architecture/type-system-migration-log.md#[TS-B2]/[TS-B3]`，同步记录新哈希、feature gate 列表与 loader 能力。
- **影响**：Stage D manifest 现含 tree builder/chunk/grapheme 最新哈希（`feature_gates=["serde","tree_builder_slice_trace"]`），C# loader/diagnostics可直接消费；下一步是把 loader smoke 接入 `[QA-IngestionSmoke]` 并驱动 `TreeBuilderTracer` 注入管线。 

### 2025-11-18 (Manifest updater + TreeBuilder trace loader)
- **Manifest 维护**：QA Engineer 实现 `scripts/verify_fixture_manifest.py --update`（可重写 `payload_hash`、打印 `Manifest changes`、在写回后自动复验），并在 `[StageD::FixtureFlow]`/`[QA-IngestionSmoke]`/`agents/qa-engineer.md` 描述“manifest diff + loader smoke”证据链；`python scripts/verify_fixture_manifest.py` 与 `python scripts/verify_fixture_manifest.py --update --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 均返回 0，后者在无 drift 场景输出“manifest already in sync”。
- **TreeBuilder slice trace**：C# Implementer 新增 `src/xi.Core/Rope/Diagnostics/TreeBuilder/TreeBuilderSliceTraceLoader.cs`、`tests/xi.Core.Tests/Fixtures/ParityFixtures/tree_builder_trace/basic_slice_plan.json` 与 `TreeBuilderSliceTraceLoaderTests`，可解析 `PushFrame`/`LeafSlice`/`EnterChild`/`MergePop` 事件并在目录缺失时抛明确信息；`dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter TreeBuilderSliceTraceLoaderTests`（3/3 ✅）验证通过。
- **文档同步**：`docs/architecture/rope-port-mapping.md#[RPM-Matrix]/[RPM-ParityAssets]` 与 `type-system-migration-log.md#[TS-B2]` 记录 loader 状态（Implementation, awaiting real traces），说明 `tree_builder_trace` 目前仍为示例且待 Rust CLI 导出；`agents/csharp-implementer.md`/`agents/qa-engineer.md` 各自更新“最近完成”。

### 2025-11-18 (Architecture Docs Template Rollout)
- **交付**：委派 Architecture Mapper 重写 `port-blueprint.md`、`rope-port-mapping.md`、`type-system-migration-log.md`、`design-divergence-log.md` 以符合 `document-structure-template.md`，统一 front-matter、`goal-tree` 片段与 `[QA-*]` / `[StageD::*]` 链接；`m3-implementation-plan.md` 的 Goal Tree 片段与 Blueprint 同步包裹在 `<!-- goal-tree:start -->` 注释中，等待未来脚本自动化。
- **内容调整**：Blueprint 现精简为 Goal Tree + Milestones + Risk 表，映射表文档压缩为 10 个关键模块并新增 `[RPM-ParityAssets]`/`[RPM-Actions]`；类型系统日志转为 `[TS-Bx]` 卡片（Problem/Rust Plan/C# Plan/Status/Links/Next），设计分歧日志改为表格呈现 UTF-16 叶片、Grapheme 降级与 Chunk copy-on-read 三项。
- **挂钩关系**：所有 Goal Tree 行（G1-G6）直接指向 `[TS-Bx]` 与 `[StageD::*]` anchors，`BP-RiskTable` 重新绑定 `[MP-R8]`-`[MP-R10]`，方便 QA/Stage D 追踪；Architecture Mapper 档案新增 2025-11-18 完成记录并列出 CLI schema/telemetry 基准待办。
- **后续**：待 Rust Porter demo `--cursor-descriptors/--chunk-descriptors/--grapheme-windows`，QA 记录 `[QA-ChunkBench]`/`[QA-Telemetry]` 基线后，再刷新 `rope-port-mapping.md` 与 Stage D playbook；若 CLI 延迟，需在 Goal Tree 将 G1/G2/G3 标记为风险状态。

### 2025-11-17 (Architecture Docs Consolidation Planning)
- **会议**：召集 Architecture Mapper、C# Implementer、Rust Porter 参加星形会议，聚焦 `docs/architecture/` 文档数量过多、冗余和交叉引用过重的问题。
- **结论**：确立“共享目标/进度树片段 + 文档职责正交”策略，并决定仅保留 `m3-implementation-plan.md` 作为详细计划，`m3-architect-decision.md` 缩减为裁决摘要/变更日志/链接集合。
- **行动项**：
  - Architecture Mapper 在 11/18 前起草统一目标树模板（含 Owner/Status/Due/Evidence/Next + Rust Commit/Feature Gates/CLI 版本字段），插入 `port-blueprint.md` 与 `m3-implementation-plan.md` 并输出引用规范。
  - C# Implementer 补充每个 G1-G3 叶节点对应的代码/测试/夹具路径链接，验证实现者读取体验。
  - Rust Porter 将 Stage D/CLI/schema 元数据和 feature gate 说明挂钩目标树字段，同时在 `rope-port-mapping.md`、`rope-serialization-fixture-playbook.md` 对应章节标注锚点。
- **交付**：发布 `docs/architecture/document-structure-template.md` 元文档，整合 Architecture Mapper/C# Implementer/Rust Porter/QA Engineer 的字段与锚点需求，定义统一 front-matter、目标树 YAML 真源、脚本同步与 QA/Stage D 仪表，四个角色均在各自档案记录认可。
- **二次迭代**：同日再次召集四个角色传阅模板并精简：保留 14 项核心字段 + 3 项可选锚点引用，将 Stage D / QA 细节改为链接至 `[StageD::*]` 与 `[QA-*]`，收紧 per-doc 章节与治理脚本说明，更新 `docs/architecture/document-structure-template.md` 以反映新结构。
### 2025-11-17 (QA/Info Researcher Onboarding)
- **QA Engineer 入职**：基于 `agents/qa-engineer-template.md` 建立 `agents/qa-engineer.md`，补齐 8 条测试资产索引、169/169 `dotnet test -v m` 基线、R8/R9/R10 风险监控与 parity/Stage D 行动清单，为后续 ingestion smoke 与 1 MB 基准奠定资料来源。
- **Information Researcher 入职**：创建 `agents/information-researcher.md`，填充 13 条索引与 7 条监控清单，并注明“仅接受架构师调度”限制；重点跟踪 AGENTS、m3 计划、Stage D schema、`refresh_serialization_fixtures.ps1` 新开关等差异。
- **组织更新**：`docs/architecture/ai-team-design-draft.md`、`AGENTS.md` 记录信息调查员仅服务架构师的技术限制，并将 QA/Information Researcher 标记为正式员工，后续由 QA 承接 parity smoke + 基准，由信息调查员维护会议资料与 Stage D 日志。

### 2025-11-17 (run_all_checks Rust Lint Sweep)
- **失败复盘**：`./run_all_checks` 在 clippy 阶段连锁暴露 `xi-trace`/`xi-rope`/`xi-core-lib`/`xi-plugin-lib`/CLI 多处 lint、私有字段访问与缺少 `serde` 特性，导致脚本无法进入 `cargo check/test`。
- **修复动作**：
  - Rust 端清理 doc 注释/`to_vec`/`type alias`/`Vec` 初始化/`for` 循环/`is_empty` 等 20+ 处 lint，`export-serde-fixtures` CLI 改用 `ok_or`，`find.rs` 避开 `repeat_n`（MSRV 1.75），`watcher.rs`、`recorder.rs`、`tabs.rs`、`plugin-lib` 文档与 API 均对齐规范；
  - `xi-core-lib` 默认启用 `serde` 特性以解锁插件 RPC 序列化路径，`Delta` 暴露 `elements()` 访问器供插件缓存无需触达私有字段；
  - `plugin-lib`/`core-lib`/`rpc` 依赖面全部通过 `cargo fmt` + `cargo clippy --all -D warnings`；
- **验证结果**：`./run_all_checks` 全流程（fmt、clippy、cargo check、workspace tests、`xi-rope` 无默认特性 + serde 组合测试）现全部通过，确保 Stage D CLI/夹具导出可在 Linux/WSL 上一次完成，无需手动跳过模块。
### 2025-11-17 (Leaf Split & Delete Invariants)
- **LeafSplitter 对齐 Rust**：重写 `StringLeafOperations.FindLeafSplit`，按 Rust `find_leaf_split` 计算上下界并扩展换行窗口搜索范围，遇到代理对拆分时退回安全边界；`TryComputeBalancedSplit` 现复用新的合并对齐逻辑，所有叶片再平衡路径不再撕裂 surrogate。
- **诊断与测试增强**：`RopeTestHelpers.AssertInvariants` 直接抛出 `XunitException` 并打印违规详情，`NodeTests.Delete_AcrossMultipleLevelsMaintainsLeafConstraints` 改为构造 `12` 片段（>8）确保覆盖多层节点，防止高度=1 时误测。
- **验证**：先跑针对性筛选（TreeBuilder/Node split），再执行 `dotnet test -v m`（169 项）全部通过，四个遗留失败清零。

### 2025-11-17 (Cursor Descriptor Deep Tree Parity)
- **构建策略对齐**：在 `Rope` 中新增 `FromNode` 工厂以便直接接管 `TreeBuilder` 输出，测试可无损还原 Rust 侧深树结构。
- **深树夹具重建**：`CursorDescriptorParityTests` 新增 `BuildDeepTreeRope`，复刻 `build_deep_rope()`（511 字符叶片 + 8^5 计数标记），确保 `deep_tree_midpoint` 与 JSON 夹具的层级/leaf path 完全一致。
- **验证**：执行 `dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter CursorDescriptor`，11 项用例全部通过，`deep_tree_midpoint` 现与 Rust 描述符帧完全对齐。

### 2025-11-17 (run_all_checks Windows Shim)
- **问题复盘**：在 Windows + PowerShell 7 中直接调用 `xi-editor-ph7/rust/run_all_checks`（无扩展名的 Bash 脚本）会触发文件关联器而非执行脚本，导致自动化（含 `scripts/refresh_serialization_fixtures.ps1`）在 `Invoke-ExternalCommand` 阶段弹出“打开方式”对话框。
- **落地修复**：新增 `xi-editor-ph7/rust/run_all_checks.ps1`，完整复刻 Bash 版本的 clippy/rustfmt/check/test 流程，并支持 `-Filter`（PowerShell）/`--filter`（通过 `pwsh -File`）参数直传到 `cargo test`；脚本自动 `Push-Location` 到 Rust 工作区并恢复 `RUSTFLAGS`。
- **脚本接线**：`scripts/refresh_serialization_fixtures.ps1` 会在 Windows 优先调用 `.ps1` 版本并自动切换至 `-Filter` 语法，非 Windows 平台继续执行原 Bash 脚本；QA 手册（`docs/csharp-refactor/rope-serialization-fixture-playbook.md`）与 `agents/qa-engineer.md` 已同步指令差异。
- **验证现状**：在 PowerShell 7 直接执行 `run_all_checks.ps1 -Filter serde-fixtures` 现可正常串行 clippy/rustfmt/cargo 流程，当前失败来自既有 `cargo clippy` 告警（`xi-trace` doc comment 与 `xi-rope` `iter().cloned().collect()`），与本次改动无关，需后续单独整改。

### 2025-11-16 (晚) (C# Rope Skeleton 清理)
- **Skeletonizer 工具**：在 `tools/Skeletonizer` 下创建 Roslyn 小工具，自动定位方法/构造函数/访问器的 `BlockSyntax` 并输出占位注释，避免手工逐块编辑造成 diff 噪音。
- **批量替换**：执行 `dotnet run -- tools/Skeletonizer ..\\..\\docs\\skeleton\\xi.Core.Rope.cs`，共 293 个函数体被替换为 `// Body removed for skeleton view.` 注释，保留了原始签名与结构层级。
- **骨架收益**：`docs/skeleton/xi.Core.decompiled.cs` 从 4k+ 行压缩至 1.3k 行，阅读时可以快速对齐类型/方法分布，同时方便未来将 Rust/C# 映射差异附加在注释旁。
- **紧凑注释版**：进一步把函数体内的换行与缩进内容替换成单行 `/* body removed for skeleton view. */` 注释，重新运行 Skeletonizer 以减少 token/行数消耗，方便在 chat 场景快速引用。

### 2025-11-16 (晚) (M3 Implementation Plan Creation)
- **Architecture Mapper 履职**：作为架构映射维护者，基于星形会议决策（`type-system-migration-log.md` 会议章节）创建 `docs/architecture/m3-implementation-plan.md`。
- **计划要点**：
  1. **目标**：4 大任务（游标 5-7 天、泛型接口维护、Chunk 3-4 天、Grapheme 2 天），总工期 15-20 天，测试基线 114 项
  2. **任务分解**：游标 6 个子任务、泛型 2 个维护项、Chunk 5 个子任务、Grapheme 5 个子任务，每项含工作量/依赖/风险评估
  3. **分工**：C# Implementer（实现）、Rust Porter（Parity 样本）、Architecture Mapper（文档同步）、架构师（质量把关）
  4. **风险**：7 项风险（R1-R7）含技术 + 进度维度，每项有缓解措施 + 应急预案
  5. **评审机制**：4 类文档修改权限明确，代码评审分 3 级（自主/交叉/里程碑），评审要点覆盖正确性/性能/可维护性/测试
  6. **同步机制**：4 类认知档案日常更新 + 周会（5-7 天）+ 里程碑同步，通过 `AGENTS.md` + 映射表协调
  7. **回退策略**：3 类触发条件（超期/测试低/性能退化），部分回退（削减功能）+ 全面回退（25 天超期门槛）
- **关键决策记录**：
  - 游标基于字符串特化 `Node.cs`，M4 前不强制泛型切换
  - Chunk 枚举器临时非零拷贝（`ReadOnlyMemory<char>`）
  - Grapheme 降级已确认，需遥测监控
  - 10 项架构管控措施（文档/代码/进度/回退四维度）
- **需全员评审的关键点**：
  1. **游标缓存失效检测**：版本号 vs `ReferenceEquals`，GC 压力监控
  2. **Chunk 性能基准**：M3 接受临时性能损失，M4 优化，需记录基准数据
  3. **回退触发条件**：游标 > 10 天、测试通过率 < 80%、性能慢 5 倍，是否合理？
  4. **周会频率**：5-7 天一次是否足够？是否需要更频繁的日常同步？
- **文档同步**：更新 `AGENTS.md` "下一步行动"章节，增加 M3 实施计划引用。

### 2025-11-18 (M3 Star Meeting - Cursor/Chunk/Grapheme Sync)
- **会议输入**：收集 C# Implementer、Architecture Mapper、Rust Porter 的进度报告，确认 T1/T3/T4 完成度、文档同步缺口与 CLI fixture 状态。
- **状态结论**：游标结构/导航/descriptor/parity 已落地但 `_editVersion` 需挂钩失效检测；Chunk/Line 枚举器已消费 CLI JSON，缺少诊断与 1 MB 基准；Grapheme 降级与遥测插桩已到位但阈值待架构师裁决。
- **风险定位**：R8（NodeCursor 版本票据）、R9（CLI schema & Stage D 集成）、R10（Chunk/Grapheme 遥测 + 基准）仍敞开，若 48h 内不更新文件与脚本，M3 里程碑将失去复现依据。
- **行动项**：指派 C# Implementer 负责 `_editVersion`、chunk diagnostics 与基准脚本；Rust Porter 整理 CLI schema/feature 说明；Architecture Mapper 回写三份核心文档并协调阈值；QA 负责 parity ingestion smoke + 微基准。

### 2025-11-16 及以前（摘要）
- 2025-11-16：完成 rope-port-mapping/design-divergence 互证、SubAgent 机制沉淀、M3 实施计划定稿与 Skeletonizer 批量精简，基线测试扩展到 114 项。
- 2025-11-15：打通 Stage D CLI 与夹具刷新链路、启用 CursorState feature gate、完善字符串 helper/leaf parity、TreeBuilder slice trace 研究→实现→导出闭环，并完成 Metric/Breaks shim 可行性调研。
- 2025-11-14 及更早：Stage A/B/C 基线（SharedNode、StringLeafOperations、subset/delta/engine fixtures）落地，Rust 工作区瘦身与早期文档/Helper 建设见 "已完成事项"。