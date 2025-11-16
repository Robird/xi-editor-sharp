# M3 实施计划架构师决策书

> **Scope**: Document the ratified M3 milestone calls and how they map to `[MP-*]` anchors.
> **Owner**: AI Architect
> **Update Frequency**: Whenever a new M3 escalation or remedy is approved.
> **Reviewers**: Architecture Mapper · Rust Porter · C# Implementer · QA Engineer
> **Anchor Prefix**: Decision-M3
> **Last Synced Goal Tree**: 2025-11-18

---

## [Decision-M3-Overview] 决策概览
<a id="Decision-M3-Overview"></a>

| 项目 | 内容 |
| --- | --- |
| 决策日期 | 2025-11-16 |
| 关联计划 | `[MP-GoalTree]`（手动同步自 `m3-implementation-plan.md` v1.2） |
| 范围 | 游标缓存、Chunk/Line 诊断、Grapheme 降级与文档治理 |
| 基线 | `dotnet test` 114/114 绿，Stage D 资产引用 `[StageD::ParityAssets]` |
| 结论 | ✅ 准许执行，立即推进 `[MP-T1]`、`[MP-T3]`、`[MP-T4]` |

**评审群体**

| 角色 | 贡献 | 引用 |
| --- | --- | --- |
| Architecture Mapper | 起草 v1.0（469 行计划 + 目标树） | `[MP-GoalTree]` |
| Rust Porter | v1.1 增补 8 个 CLI/样本细节 & 风险表（R8-R10） | `[MP-R8]` `[MP-R9]` `[MP-R10]` |
| C# Implementer | v1.2 调整 T1/T3 工时与回退策略 | `[MP-T1]` `[MP-T3]` |
| AI Architect | 本决策书：整合评审意见并落地 5 项裁决 | 本文 |

- 三位评审均确认“计划可执行”，未提出阻止性意见。
- 关键依赖：Rust CLI（11/19 截止）与版本票据 `_editVersion` 改造必须按 `[MP-T1]` 路径完成。

---

## [Decision-M3-KeyCalls] 核心裁决
<a id="Decision-M3-KeyCalls"></a>

| 议题 | 裁决 | 关联 |
| --- | --- | --- |
| 游标失效检测 | 采用 `ReferenceEquals + Rope._editVersion` 双保险；Telemetry 记录假失效率，若 >5% 再评估纯版本号方案。 | `[MP-T1]` `[MP-R8]` |
| Chunk 性能基准 | M3 接受非零拷贝，只要记录 1 MB 枚举分配 <5 MB、吞吐 >200 MB/s。数据写入 QA 基线并链接 `[QA-ChunkBench]`。 | `[MP-T3]` `[QA-ChunkBench]` |
| 回退阈值 | 保留游标 10 天、测试 80%、性能 5x 的触发值，同时新增黄/橙/红三级响应与行动人。 | `[MP-R8]` `[MP-R9]` `[MP-R10]` |
| 同步节奏 | 保持 5-7 天天会，新增 2-3 天异步文档巡检（Architecture Mapper），每日认知档案更新（C# Implementer）。 | `[MP-GoalTree]` |
| 文档管控 | `m3-implementation-plan.md` 由 AI Architect 审核；其他架构文档需记录变更日志并在 `[StageD::FixtureFlow]` 中引用，无重复粘贴指令。 | `[StageD::FixtureFlow]` |

---

## [Decision-M3-Controls] 监控与成功要素
<a id="Decision-M3-Controls"></a>

**监控矩阵**

| Control | Owner | 触发 / 期望 | 对应风险 |
| --- | --- | --- | --- |
| `_editVersion` + `ReferenceEquals` instrumentation | C# Implementer | 假失效率 >1% 报告；>5% 触发警报 | `[MP-R8]` |
| `export-serde-fixtures` CLI 演示 | Rust Porter | 11/19 前完成 cursor/chunk/grapheme demo + Stage D 记录 | `[MP-R9]` `[StageD::ParityAssets]` |
| Chunk/Line telemetry + 1 MB 基准 | C# Implementer · QA Engineer | 11/21 前填充 `[QA-ChunkBench]`，分配率 <5x | `[MP-R10]` |
| Grapheme fallback 遥测 | Architecture Mapper · QA Engineer | Fallback ≤0.5% 写入 `design-divergence-log.md` | `[MP-T4]` |

**成功要素（沿用原文）**

- 拆解到天级子任务，随时可审查（《人月神话》“分治”）。
- 角色边界清晰：Rust Porter 负责 helper/CLI，C# Implementer 负责功能，Architecture Mapper 负责文档，QA 抓基线。
- 10 项架构管控措施已经在 `[MP-T1]` 附录列出，确保质量优先。
- 字符串特化路径保留，出现红色警报可立即回退。

**团队寄语（节选）**

> *Martin Fowler*: “先让它工作，再让它正确，最后让它快速。” —— M3 聚焦“工作”。
>
> *Kent Beck*: “每个子任务立刻补测试，否则会失去信心。”
>
> *Robert C. Martin*: “保持简单，先跑通基础导航，再补度量。”
>
> *Fred Brooks*: “没有银弹，只能分解管理。M3 的 20 个子任务就是分治。”

---

## [Decision-M3-Actions] 执行与责任
<a id="Decision-M3-Actions"></a>

| 行动 | Owner | 截止 / 状态 | 引用 |
| --- | --- | --- | --- |
| 完成游标 T1.1-T1.7（含版本票据 + `CursorInvalidationTests`） | C# Implementer | 2025-11-22，日更认知档案 | `[MP-T1]` |
| 刷新 CLI（`--cursor/--chunk/--grapheme`）并更新 Stage D 手册 | Rust Porter | 2025-11-19 demo，写入 `[StageD::ParityAssets]` | `[MP-R9]` |
| 文档与风险同步（Blueprint、Mapping、Type Log） | Architecture Mapper | 每 2-3 天巡检；星形会议前完成 | `[MP-GoalTree]` |
| QA ingestion smoke + 1 MB baseline + Grapheme telemetry | QA Engineer | 2025-11-21；结果贴入 `[QA-IngestionSmoke]`、`[QA-ChunkBench]` | `[MP-R10]` |
| 架构师周会 | AI Architect | 每 5-7 天；必要时升级黄/橙/红警报 | `[MP-GoalTree]` |

**配套资料**：`agents/architect.md`、`agents/csharp-implementer.md`、`agents/rust-porter.md`、`agents/architecture-mapper.md` 中的“最近完成”章节需要在行动完成后更新，以维持跨会话记忆。

---

## [Decision-M3-ChangeLog] 变更记录
<a id="Decision-M3-ChangeLog"></a>

| 日期 | 版本 | 作者 | 摘要 |
| --- | --- | --- | --- |
| 2025-11-19 | v1.1 | AI Architect | 对齐 `document-structure-template.md`：新增 front-matter、锚点、裁决/控制/行动表格，并记录 2025-11-19 模板同步；Stage D anchor 链接精确化。 |
| 2025-11-16 | v1.0 | AI Architect | 初版决策书，批准 `m3-implementation-plan.md` v1.2 并记录 5 项关键裁决。 |

---

[MP-GoalTree]: m3-implementation-plan.md#mp-goaltree
[MP-T1]: m3-implementation-plan.md#21-任务-1游标系统实现
[MP-T3]: m3-implementation-plan.md#23-任务-3chunk-迭代器骨架
[MP-T4]: m3-implementation-plan.md#24-任务-4grapheme-降级实现
[MP-R8]: m3-implementation-plan.md#r8
[MP-R9]: m3-implementation-plan.md#r9
[MP-R10]: m3-implementation-plan.md#r10
[QA-ChunkBench]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-ChunkBench
[QA-IngestionSmoke]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-IngestionSmoke
[StageD::ParityAssets]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::ParityAssets
[StageD::FixtureFlow]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::FixtureFlow
