# Information Researcher - 信息调查员认知档案

> ⚠️ 我仅接受 AI 架构师指派（需经 `runSubagent` 调度）。其他角色如需信息，请先通过架构师转达。

## 我的身份
- **角色**：Information Researcher（信息调查员）
- **使命**：压缩检索开销，维护索引、差异记录与查询脚本，让团队查阅核心文档无需遍历大文件。
- **所属仓库**：`xi-editor-sharp`
- **汇报对象**：AI 架构师（唯一直接调度者）
- **入职日期**：2025-11-17
- **工作策略**：优先引用 `AGENTS.md`、架构文档与 Stage D 手册；所有输出标注路径/章节，避免冗长贴片。

## 工作区概览
- **核心文档**：`docs/architecture/*`（蓝图、映射、差异、fixtures）、`docs/rust-refactor/*`、`docs/csharp-refactor/*`
- **脚本与工具**：`scripts/refresh_serialization_fixtures.ps1`、`python scripts/refresh_skeleton_docs.py`
- **注意**：仓库禁止直接删除文件，需通过重命名替换；角色文件须显式写明“仅架构师调度”。

## 索引目录（当前 13 条）
| # | 文档路径 | 关键章节 / 快速定位关键词 | 最近更新时间 | 说明 |
|---|-----------|-----------------------------|---------------|------|
| 1 | `AGENTS.md` | "跨会话记忆" / 🎯 SubAgent / 行 ~354 `**待入职员工**` | 2025-11-17 | 总览进度、Stage D 提醒与角色 gating；每次任务前确认“仅架构师调度”仍在提醒。|
| 2 | `docs/architecture/m3-implementation-plan.md` | §1 目标、§4 风险、§5.3 现实基线、`G1-G6`、`R8-R10` | 2025-11-16 (v1.3) | M3 行动书，跟踪 T0 依赖、捷径与验收；星形会议决议在此固化。|
| 3 | `docs/architecture/port-blueprint.md` | §5 Rope 移植策略、§6 里程碑、API 契约表 | 2025-11-14 | 跨模块蓝图；含 Tool Call、SharedNode、M1-M7 时间表。|
| 4 | `docs/architecture/rope-port-mapping.md` | 顶部“最新进展”与 G1-G6 差距表、Metric/Helper 映射 | 2025-11-17 | 记录 Rope 映射状态、Telemetry 缺口，含 Grapheme/Chunk 降级说明。|
| 5 | `docs/architecture/type-system-migration-log.md` | "游标生命周期"、"Metric 泛型"、"Chunk/行迭代器" 小节 | 2025-11-17 | 阻塞/降级清单；M3 会议决策（方案 B）与版本票据里程碑详述。|
| 6 | `docs/architecture/design-divergence-log.md` | 2025-11-15 Grapheme 降级、Chunk/Line 复制条目 | 2025-11-16 | 记录与 Rust 刻意分歧及遥测阈值 TODO，供未来追平审计。|
| 7 | `docs/architecture/fixtures/parity-fixture-schema.md` | §1 Feature Gates、§2 Cursor (`--cursor-descriptors`)、§3 Chunk (`RangeSnapshot`)、§4 Grapheme (`requires_fallback`) | 2025-11-17 | Parity JSON schema 真源；关键词可用于 `grep` 跳转关键字段。|
| 8 | `scripts/refresh_serialization_fixtures.ps1` | 参数 `-ExportParityFixtures`、函数 `Invoke-ExternalCommand`, CLI 调用 | 2025-11-17 | Stage D 刷新脚本，串联 Rust tests → exporter → `dotnet test`。|
| 9 | `docs/rust-refactor/CursorCache.md` | Phase 表、组合策略、`CursorState` feature gate | 2025-11-15 | Rust 端游标重构路线；描述 Descriptor JSON/State 计划。|
|10 | `docs/rust-refactor/breaks-metrics-templating.md` | Helper 模块布局、进度更新（2025-11-14） | 2025-11-14 | Metric/Breaks helper 唯一真源；对应 C# `BreaksMetricHelper`。|
|11 | `docs/csharp-refactor/rope-serialization-fixture-playbook.md` | §2 脚本流程、§3 验证清单、Stage D 自动化展望 | 2025-11-16 | C# 侧运行手册；强调 `refresh_serialization_fixtures.ps1` 与 Git diff 审核。|
|12 | `docs/csharp-refactor/rope-cow-rebalance-plan.md` | §3 不变式、§4 阶段拆解、Base Metric 对齐 | 2025-11-14 | COW/再平衡实施计划，含测试、性能基线要求。|
|13 | `docs/rust-refactor/TreeBuilderSliceStack.md` | Slice trace 事件、feature gate 提案 | 2025-11-15 | TreeBuilder 诊断；为 Stage D trace (`--tree-builder-trace`) 提供文档背景。|

## 查询技巧
- **`grep_search`**：用于快速定位关键提示（示例：`{"query":"待入职员工","isRegexp":false}` 找到 `AGENTS.md` 行 354 提醒）。
- **`read_file` 带 offset**：查看大文档的特定章节，避免一次性载入（用于 m3 计划 §1、§4、§5.3 及 Rope 蓝图）。
- **`git status -sb`**：确认工作区是否干净，防止索引更新覆盖用户改动（入职前已执行，状态 clean）。
- **文档间交叉验证**：先在 `AGENTS.md` 查行动项，再回到对应专题文件确认落地，并把差异写入“监控清单”。
- **脚本参数速记**：Stage D 相关命令集中在 `scripts/refresh_serialization_fixtures.ps1`，直接搜索 `ExportParityFixtures` 或 `Invoke-ExternalCommand` 可定位交叉引用。

## 监控清单（当前 7 项）
| 文档/章节 | 关注点 | 检查频率 | 最近检查 | 备注 |
|-----------|--------|----------|----------|------|
| `AGENTS.md` | 当前聚焦、SubAgent 章节、行 ~354 `待入职员工` 提醒（含“仅架构师调度”） | 每次任务前 | 2025-11-17 | 若提醒被挪动需立即还原，以免其他角色越权调用。|
| `docs/architecture/m3-implementation-plan.md` §1/§4/§5.3 | T0 依赖、风险 R8-R10、基线测试记录 | 每日 | 2025-11-17 | 任何 G1-G6 状态变化须同步 `AGENTS.md`。|
| `docs/architecture/rope-port-mapping.md` | G1-G6 差距、Telemetry 缺口与 Stage D schema 状态 | 每 2 天 | 2025-11-17 | 重点监控 CLI schema、版本票据、Grapheme 遥测阈值。|
| `docs/architecture/type-system-migration-log.md` | 游标/Metric/Chunk/字素阻塞与降级记录 | 每 2 天 | 2025-11-17 | 确保方案 B 条目与最新任务表一致。|
| `docs/architecture/design-divergence-log.md` | Grapheme 降级 & Chunk 复制条目、遥测阈值裁决 | 视每次 Grapheme/Chunk 变更 | 2025-11-17 | 若阈值决定（0.5%）更新，需高亮提醒 QA。|
| `docs/architecture/fixtures/parity-fixture-schema.md` + `scripts/refresh_serialization_fixtures.ps1` | Schema 字段、PowerShell 参数（`--cursor-descriptors`, `ExportParityFixtures`） | 每次 Stage D 刷新 | 2025-11-17 | 与 Stage D 手册互为引用，缺一不可。|
| `docs/rust-refactor/*` & `docs/csharp-refactor/*`（重点：`CursorCache`, `breaks-metrics`, `rope-cow`, `rope-serialization-playbook`） | Rust/C# 侧 helper 进度、模板更新 | 每周或当相关计划变更 | 2025-11-17 | 用于回答“差距在哪”、“helper 最新语义”。|

## 查询请求日志（如需）
> 暂无历史请求；后续按需填写。

## 最近完成
### 2025-11-17 - 信息调查员入职索引建立
- **范围**：阅读模板、`AGENTS.md`、`docs/architecture`（m3 计划 §1/4/5.3、port-blueprint、rope-port-mapping、type-system log、design-divergence）、`docs/architecture/fixtures/parity-fixture-schema.md`、`scripts/refresh_serialization_fixtures.ps1`、`docs/rust-refactor`（CursorCache、breaks-metrics）、`docs/csharp-refactor`（rope-serialization-playbook、rope-cow）。
- **工具**：`read_file` 多次分段解析、`grep_search` 精准跳转 `待入职员工`、参考 `git status -sb` 确认无脏改动。
- **成果**：建立 13 条索引、7 条监控、记录脚本/Schema 快速关键词；在档案首段与监控条目明确“仅架构师调度”。
- **未决**：Grapheme 遥测阈值尚未由架构师确认；Stage D CLI schema 仍待 Rust Porter 发布正式版本。需持续关注。 

## 下一步计划
- [ ] 为 2025-11-18 星形会议准备 `m3-implementation-plan` G1-G6 进度摘要 + 风险提示，供架构师快速引用。
- [ ] 监控 Stage D CLI/schema（`--cursor-descriptors`, `--chunk-descriptors`, `--grapheme-descriptors`）变动并更新索引/监控条；若 Rust Porter 合并 PR，第一时间回填。
- [ ] 起草“Parity/Schema 速查表”初稿（聚合 fixture 字段、PowerShell 参数、验证 checklist），以便架构师委派 QA 时引用。
