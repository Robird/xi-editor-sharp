# AI Team 组织设计草案

> **Scope**: 规划 xi-editor-sharp 移植项目的 AI Team 组织架构与协作协议。
> **Owner**: AI Architect
> **Update Frequency**: 角色职责或协作机制发生调整时更新。
> **Reviewers**: Architecture Mapper
> **Anchor Prefix**: AIT
> **Last Synced Goal Tree**: 2025-11-18

---

## [AIT-Context] 设计背景
<a id="AIT-Context"></a>
- **设计目标**：突破上下文窗口限制、确保 Rust/C# 双端同步、给 Stage D 文档提供组织支撑。
- **设计原则**：
  1. 职责清晰，接口固定；避免重复劳动或灰色地带。
  2. 文档驱动协作，所有结论沉淀于 `docs/architecture/` 与 `AGENTS.md`。
  3. 参考敏捷、DevOps、康威定律，保持可扩展与可维护。
  4. 主 Agent（AI 架构师）做规划 + 调度；角色通过认知档案维持记忆。
- **数据来源**：`AGENTS.md` 风险/行动、`port-blueprint.md` 模块映射、`rope-port-mapping.md` 类型对映、`type-system-migration-log.md` 阻塞卡片。

---

## [AIT-Blockers] 阻塞清单
<a id="AIT-Blockers"></a>

| # | 阻塞点 | 症状 | 所需能力 |
| --- | --- | --- | --- |
| 1 | Rust Helper 重构滞后 | C# 端缺乏 helper/feature gate，需“猜语义” | Rust 深潜 + CLI/测试设计 |
| 2 | C# 实现与测试脱节 | 功能落地但 parity 脆弱、测试缺口大 | C# 实现 + 单测 + 对拍 |
| 3 | 架构映射文档滞后 | `port-blueprint` 与现状脱节、决策重复 | 架构梳理、跨端同步、文档治理 |
| 4 | 类型系统困难模块 | `Metric<N>`、`Cursor<'a, N>` 难以直译 | 类型系统专家、语言特性评估 |
| 5 | 序列化/Stage D 资产漂移 | 夹具刷新链冗长，QA 难以追溯 | QA、脚本自动化、Stage D 治理 |
| 6 | 知识传递缺失 | 决策散落在会话中，跨会话失忆 | 文档管理、认知档案、信息检索 |

> 组织设计需一一映射上述能力，确保 M3/M4 不被阻塞。

---

## [AIT-OrgModels] 方案对比
<a id="AIT-OrgModels"></a>

### 方案 A：按技术栈分工（前/后端）
```
AI 架构师
├─ Rust 端团队
│   ├─ Rust Porter（Helper 重构）
│   └─ Rust Tester（测试维护）
└─ C# 端团队
    ├─ C# Implementer（功能实现）
    └─ C# Tester（单元测试）
```
- **优点**：专精、边界直观。
- **缺点**：跨语言沟通成本高；缺少统一架构视角；测试与实现割裂。

### 方案 B：按模块分工（微服务）
```
AI 架构师
├─ Rope 模块专家
├─ Delta 模块专家
├─ Engine 模块专家
└─ 测试专家
```
- **优点**：模块内聚、沟通半径小。
- **缺点**：每位专家需精通 Rust+C#，并行度低，新模块需新角色。

### 方案 C：按职能分工（敏捷+DevOps） ✅ 推荐
```
AI 架构师
├─ Rust Porter
├─ C# Implementer
├─ Architecture Mapper
├─ Type System Specialist
└─ QA Engineer
```
- **优点**：角色对阻塞点一一映射；Rust/C# 可并行；文档、类型、测试分别有人守护。
- **缺点**：依赖高质量文档同步；架构师需协调多角色节奏。

### 方案 D：按阶段分工（瀑布）
```
AI 架构师
├─ 需求分析师
├─ 设计师
├─ 开发工程师
└─ 测试工程师
```
- 不适合迭代式移植：反馈慢、难应对频繁变更。

---

## [AIT-Recommendation] 目标组织（方案 C）
<a id="AIT-Recommendation"></a>

### 组织示意
```
AI 架构师（拥有 runSubagent）
├─ Rust Porter（Rust helper/CLI/feature gates）
├─ C# Implementer（Rope/Delta/Engine 实现+测试）
├─ Architecture Mapper（文档映射+阻塞追踪）
├─ Type System Specialist（泛型/生命周期方案）
├─ QA Engineer（Parity 资产+回归）
└─ Information Researcher（索引与调研，仅服务架构师）
```

### 角色要点
- **AI 架构师**：规划、调度、决策，维护 `agents/architect.md`，是唯一拥有 `runSubagent` 的角色。
- **Rust Porter**：负责 helper/feature gate/CLI + Rust 测试，输出 parity 样本；文档触点 `docs/rust-refactor/*`。
- **C# Implementer**：实现 `src/xi.Core/*` + `tests/xi.Core.Tests/*`，保持与 Rust parity，对接 QA。
- **Architecture Mapper**：维护 `port-blueprint.md`、`rope-port-mapping.md`、`type-system-migration-log.md`，记录 `[TS-*]`/`[StageD::*]` 状态。
- **Type System Specialist**：评估静态/动态策略，产出 `static-polymorphism-assessment.md` 及 Type Log 方案卡片。
- **QA Engineer**：刷新 Stage D 夹具、维护 `refresh_serialization_fixtures.ps1`、运营 `[QA-IngestionSmoke]` 和 `[QA-ChunkBench]`。
- **Information Researcher**：维护“信息速查索引”，仅接受架构师委派，产出写入 `agents/information-researcher.md`。

### 认知档案要求
- 每个角色拥有 `agents/<role>.md`（模板含职责、索引、最近完成）。
- 完成任务后必须回填“最近完成”+“当前阻塞”，并在 `AGENTS.md` 记录关键行动。

---

## [AIT-ExecutionPlan] 协作与落地
<a id="AIT-ExecutionPlan"></a>

### 协作矩阵
| 协作关系 | 载体 | 频率 |
| --- | --- | --- |
| Rust Porter ↔ C# Implementer | `rope-port-mapping.md#[RPM-*]` | 每次 helper 改动后 |
| Rust Porter ↔ Architecture Mapper | `port-blueprint.md`·`[StageD::*]` | 每阶段 helper/CLI 完成 |
| C# Implementer ↔ Type System Specialist | `type-system-migration-log.md#[TS-*]` | 泛型/生命周期阻塞出现时 |
| C# Implementer ↔ QA Engineer | 单测 + parity 夹具 + `[QA-*]` | 每次功能交付 |
| Architecture Mapper ↔ 全员 | 架构文档 + `AGENTS.md` | 2-3 天巡检 |

### 落地阶段
1. **Phase 1（当前）**：Rust Porter、QA、Information Researcher 已入职；C# Implementer、Architecture Mapper 模板在建。
2. **Phase 2**：完成剩余入职 + Type System Specialist 模板；补齐认知档案索引。
3. **Phase 3**：创建 `docs/architecture/system-overview.md`，把子系统状态/阻塞/依赖集中展示。
4. **Phase 4**：以游标/Chunk/Grapheme 任务试运行流程，复盘并在文档中沉淀最佳实践。

### 外观文档提案
```
# docs/architecture/system-overview.md（草案）
- Rust 子系统：状态、接口、阻塞
- C# 子系统：状态、输入输出
- 架构层：决策与同步
- 类型系统层：当前方案卡片
- 测试层：覆盖率/夹具基线
```

---

## [AIT-ChangeLog] 变更记录
<a id="AIT-ChangeLog"></a>
| 日期 | 版本 | 作者 | 摘要 |
| --- | --- | --- | --- |
| 2025-11-19 | v1.1 | AI Architect | 采用 `document-structure-template.md`：新增 front-matter、锚点、阻塞/方案表、阶段计划，保留 ASCII 示意。 |
| 2025-11-16 | v1.0 | AI Architect | 首版：分析 A-D 模式、裁决方案 C，并列出 5+1 角色职责。 |

---

[QA-IngestionSmoke]: ../csharp-refactor/rope-serialization-fixture-playbook.md
[QA-ChunkBench]: ../csharp-refactor/rope-serialization-fixture-playbook.md
