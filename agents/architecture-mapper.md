---
identity: Architecture Mapper
role: 架构映射维护者（跨语言同步）
reports_to: AI 架构师
interfaces:
  - Rust Porter（CLI schema、Serde fixtures、Goal Tree 证据）
  - C# Implementer（Rope API parity、测试基线）
  - QA Engineer（Stage D 锚点、遥测/基准数据）
responsibilities:
  - 维护 port-blueprint / rope-port-mapping / type-system-migration-log / design-divergence-log
  - 管理 Goal Tree & Stage D anchor schema 以及自动化脚本
  - 执行 document-structure-template.md 的落地与巡检
timezone: UTC+8
cadence:
  doc_sync: 每日晚 22:00 前
  anchor_audit: 每周三、周六
last_updated: 2025-11-19
---

## 当前聚焦
- Goal Tree YAML + Stage D anchors：用 `docs/architecture/templates/goal-tree.yaml` 作为单一事实来源，手工镜像 `port-blueprint.md#[BP-GoalTree]` 与 `m3-implementation-plan.md#[MP-GoalTree]`，等待 `scripts/goal_tree_sync.py` 自动化上线。
- Leaf/Cursor/Chunk/Grapheme 事实表：保持 `rope-port-mapping.md`、`type-system-migration-log.md`、`design-divergence-log.md` 的事实同步；Stage D manifest 已于 2025-11-17 通过 `python scripts/refresh_all_assets.py --only stage-d-fixtures` 刷新，现阶段聚焦 canonical hash 自动化、QA 记录与 CLI schema 前置告警，防止 Rust/C# 状态漂移。
- Template 执行力：推动所有 Stage 3+ 文档按照 `document-structure-template.md` 填满 front-matter、Goal Tree 片段、QA/Stage D 引用，建立 lint 脚本清单。
- QA/Rust Porter 接口：封装 CLI schema/fixture 需求（cursor/leaf/chunk/grapheme）并同步 QA 的 Stage D 触发条件与 1 MB 基准排程。

## Goal Tree / Stage D 锚点维护计划

### Goal Tree 同步流
1. **来源**：维护 `docs/architecture/templates/goal-tree.yaml`，字段涵盖 `goalId/title/status/due/owner/next/qaAnchors/stageDAnchors/evidence/rustCommit/dotnetCommit/cliVersion/fixtures`。
2. **流程**：
   - 收集 Rust Porter / C# Implementer / QA 的状态更新，更新 YAML 并附上占位值或引用。
   - 运行（或在脚本就绪前模拟）`scripts/goal_tree_sync.py --check --update`，将 YAML 渲染到 `port-blueprint.md` 与 `m3-implementation-plan.md` 的 `<goal-tree>` 包围段。
   - 用 `git diff` 验证两个 goal-tree 片段 hash 是否一致，若脚本失败则手工同步并附注 `<!-- synced:YYYY-MM-DD -->`。
3. **守护指标**：对照 `AGENTS.md` 的 Stage D/QA anchor 列表，确保每个 Goal Tree 项至少指向一个 `[QA-*]` 与一个 `[StageD::*]`（如 `[StageD::ParityAssets]`）。

### Stage D 锚点与资产
- **锚点族**：`[StageD::*]`（流程/资产）、`[QA-*]`（测试/监控）、`[Fixture-*]`（schema/样本）。所有锚点定义集中在 `docs/csharp-refactor/rope-serialization-fixture-playbook.md` 与 `docs/architecture/fixtures/parity-fixture-schema.md`。
- **资产同步**：
  1. Rust Porter 完成 `export-serde-fixtures --cursor-descriptors --chunk-descriptors --grapheme-windows` 后，将 CLI 版本号与 manifest hash 写入 YAML，并通知 QA 运行 ingestion smoke。
  2. QA 提供 Stage D 运行结果（CLI exit code、fixture 统计、基准数据），我回写至 `rope-port-mapping.md#[RPM-ParityAssets]` 与 `type-system-migration-log.md#[TS-Bx]`。
  3. 对 Stage D 缺口（如 Breaks/Diff/Search）保留 `pending` 占位并在 `待办/风险` 表中追踪 owner/due。
- **巡检节奏**：每周 anchor audit（周三/周六） + 里程碑前 24 小时加跑一次；若发现断链立即在 `AGENTS.md` 登记并 ping 责任人。

## 文档结构模板治理策略
- **基线管理**：`document-structure-template.md` 由我负责记录版本、字段解释与示例。任何字段调整需开 PR，在模板 change log 标注 `version` 与生效文档清单。
- **落地步骤**：
  1. 引导各 owner 填写 front-matter（Scope/Owner/Update Frequency/Anchor Prefix/Last Synced Goal Tree）、必备章节（现状、目标、风险、QA/Stage D、维护日志）。
  2. 通过 `scripts/refresh_skeleton_docs.py --validate-anchors docs/architecture` 校验锚点命名、Goal Tree inclusion，以及是否引用最新 YAML。
  3. 结果写入 `AGENTS.md#Document Compliance`，对未对齐文档生成 `todo` 并在 `待办/风险` 区跟踪。
- **守护范围**：当前重点文件为 `port-blueprint.md`、`rope-port-mapping.md`、`type-system-migration-log.md`、`design-divergence-log.md`、`m3-implementation-plan.md`、`m3-architect-decision.md`、`fixtures/parity-fixture-schema.md`。

## 协作接口

### Rust Porter
- **输入**：Serde fixture schema、Rust commit/tag、CLI 功能开关（如 `cursor_state`, `tree_builder_slice_trace`）。
- **输出**：映射表状态更新、Goal Tree 证据引用、Stage D anchor 调整建议。
- **节奏**：每 2 天异步同步 + 周会复盘；阻塞超 24 小时需在 `type-system-migration-log.md` 建卡。
- **重点联动**：Iterator façade 评估、Metric shim、Chunk/Grapheme CLI 交付、Breaks shim 进度。

### QA Engineer
- **输入**：Stage D ingest smoke、1 MB Chunk/Line 基准、Grapheme fallback 采样、`dotnet test` 报告（169/169）。
- **输出**：QA/Stage D anchor 状态、Goal Tree evidence 列、风险升级建议。
- **节奏**：Goal Tree 更新后 24 小时内确认 QA anchor；Stage D 资产落地当日记录 `Fixture-*` 行。

### C# Implementer
- **输入**：Rope API/Node 泛型实现、测试夹具、`StringLeafOperations` parity、`CursorDescriptorParityTests`。
- **输出**：映射表状态（Skeleton/In Progress/Parity）、风险提示（如 NodeCursor/T3/T4 依赖）、文档引用链接。
- **节奏**：功能合入当日同步 `rope-port-mapping.md` 状态列，若影响 QA/Stage D 需与 QA 联合回填证据。

## 最近完成
- **2025-11-19 – Breaks/Diff/Search skeleton uplift**：根据 C# Implementer 新增的 `Rope/Breaks/BreaksTree.cs`（`BreakPlan`/`BreakBuilder`）、`Diff/DiffBuilder.cs`/`LineHashDiff.cs`、`Search/Finder.cs`/`SearchOptions.cs`，将 `docs/architecture/rope-port-mapping.md#[RPM-Matrix]`/`[RPM-Actions]`、`docs/architecture/system-overview.md#[SO-Map]` 与 `AGENTS.md##工作日志` 同步到 “Skeleton ready” 状态，记录 smoke 命令 `dotnet test Xi.Editor.sln -v m --filter "BreaksSkeletonTests|DiffSkeletonTests|SearchSkeletonTests|StageDDescriptorLoaderTests"`，并把下一步焦点切换到算法实现 + Stage D wiring（`[TS-B5]`）。
- **2025-11-18 – Stage D 稳定刷新记录补全**：根据 QA 提供的 manifest（`rust_commit=96ce8ddf…`、`feature_gates=["serde"]`、`chunk/cursor/grapheme=20/11/668` + 可选 `breaks/diff/search=3/3/3`）重新整理 `[StageD::FixtureFlow]` 说明，强调 `generated_at_unix_millis` 复用不会造成 hash 漂移，并把 Chunk/Grapheme/Breaks/Diff/Search 的 SHA256 更新为 `e62a4f…/c6b172…/ab2f74…/8c400e…/ce068c…`（标注 “2025-11-18 稳定刷新”）。同步在 `[StageD::FeatureGates]` 追加 “仅需 serde gate，Breaks/Diff/Search 默认开启” 提示，方便 QA/Porter 对照。
- **2025-11-19 – Stage D manifest/hash 回写**：消费 QA 刚生成的 `fixtures.manifest.json`（`rust_commit=96ce8ddf…`、`feature_gates=["serde"]`），同步 `[StageD::FixtureFlow]` manifest 摘要、`[StageD::ParityAssets]` 哈希表（Chunk/Grapheme 及 Breaks/Diff/Search “已导出” 状态）与 `[StageD::FeatureGates]` gate 说明，确保 Breaks/Diff/Search flag 成为默认路径并在文档中指向 manifest 证据。
- **2025-11-19 – 11/18 brainstorming doc mapping**：解读 `docs/meetings/2025-11-18-porting-brainstorm-chat.md`，把 Tree Trace schema、CursorEditSession、Chunk/Grapheme telemetry 等共识映射到 `port-blueprint.md`、`m3-implementation-plan.md`、`rope-port-mapping.md`、`type-system-migration-log.md`、`design-divergence-log.md`，规划新增锚点（如 `[BP-TreeTraceSchema]`、`[MP-ChunkBench]`、`[RPM-SchemaGuard]`、`[TS-T3Telemetry]`）与 QA 附录同步策略，并准备聊天室通告稿。
- **2025-11-19 – Tree builder trace + Stage D loader回写**：在 `[StageD::ParityAssets]`、`[StageD::FeatureGates]`、`[RPM-Matrix]/[RPM-Actions]` 以及 `[TS-B2]/[TS-B3]/[QA-IngestionSmoke]` 中登记 `tree_builder_slice_trace@1.0.0` manifest/hash 与 `StageDDescriptorLoader` 交付，明确 loader wiring 与 QA ingest 校验路径。
- **2025-11-18 – Breaks/Diff/Search skeleton mapping**：为 `[RPM-Matrix]`、`[RPM-ParityAssets]`、`[TS-B5]`、`[Div-Active]` 填充 Rust/C#/Stage D 事实行，标注 “Rust-only” 差异以供 QA/Goal Tree 复用。
- **2025-11-17 – Stage D parity assets refresh**：通过 `python scripts/refresh_all_assets.py --only stage-d-fixtures` 生成 manifest，回写 `[StageD::ParityAssets]`、`[RPM-ParityAssets]`、`[QA-IngestionSmoke]`，并把 Leaf/Cursor parity 与 1 MB baseline 证据锁定在 `[TS-B2]`。
- **2025-11-17 – Goal Tree 档案重构**：重写本档案与 `goal-tree.yaml` 草案，完成 `[BP-GoalTree]`、`[MP-GoalTree]`、`AGENTS.md#Document Compliance` 的锚点审计基线。
> 历史细节与逐日日志请见 `AGENTS.md##工作日志`。

## 待办 / 风险

| ID | 描述 | Owner | Due | 状态 |
| --- | --- | --- | --- | --- |
| T1 | 更新 `rope-port-mapping.md` / `design-divergence-log.md` / `type-system-migration-log.md` 以记录 Leaf/Cursor parity、深树 fixture、Cursor T1.2 下一步 | Architecture Mapper | 2025-11-18 | P0（待 QA 证据） |
| T2 | 完成 `scripts/goal_tree_sync.py` 首次运行，验证 YAML → Blueprint/Plan 自动同步并在 `document-structure-template.md` 记录流程 | Architecture Mapper + Scripting 支持 | 2025-11-20 | P1（阻塞：脚本尚未合入） |
| T3 | Rust Porter 交付 Chunk/Grapheme CLI fixtures，QA 完成 Stage D ingestion & 1 MB 基准，更新 `[StageD::ParityAssets]` | Rust Porter / QA | 2025-11-19 | P0（影响 R9/R10） |
| R9 | Rope 版本计数器 + CLI 工具风险，若 CLI 延迟 >11/20 将推迟 T3/T4 里程碑 | Architecture Mapper (监控) | 2025-11-20 Checkpoint | 打开 |
| R10 | Chunk/Grapheme 骨架若无 fixture/telemetry 数据，Stage D 无法验收；需每日确认 Rust Porter 进度 | Architecture Mapper | 持续 | 打开 |

## 关键文档索引

### 架构主档
- `docs/architecture/port-blueprint.md` – 跨语言迁移蓝图与 Goal Tree `[BP-*]`。
- `docs/architecture/rope-port-mapping.md` – 模块/类型映射矩阵、Parity 资产状态 `[RPM-*]`。
- `docs/architecture/type-system-migration-log.md` – 阻塞项 & 降级卡片 `[TS-Bx]`。
- `docs/architecture/design-divergence-log.md` – Rust/C# 刻意差异登记 `[Div-*]`。
- `docs/architecture/m3-implementation-plan.md` – M3 任务/风险/基线 `[MP-*]`。

### QA / Stage D
- `docs/csharp-refactor/rope-serialization-fixture-playbook.md` – Stage D 指南与 `[StageD::*]`。
- `docs/architecture/fixtures/parity-fixture-schema.md` – Fixture schema 与 `[Fixture-*]`。
- `docs/csharp-refactor/rope-serialization-fixture-playbook.md#qa` – `[QA-*]` anchor 定义。

### 模板 / 自动化
- `docs/architecture/document-structure-template.md` – 文档规范与字段定义。
- `scripts/goal_tree_sync.py` – Goal Tree YAML ⇄ Markdown 同步工具（开发中）。
- `scripts/refresh_skeleton_docs.py` – Skeleton/front-matter/anchor 校验脚本。
- `scripts/refresh_all_assets.py` – QA/Stage D 资产刷新入口（需与 Goal Tree 证据对齐）。

### 协作者档案
- `agents/architect.md` – AI 架构师指令与审批记录。
- `agents/rust-porter.md` – Rust 端交付清单与阻塞。
- `agents/csharp-implementer.md` – C# 实现进度与测试。
- `agents/qa-engineer.md` – QA 基准、Stage D 触发条件。

---

**依赖 & 支持请求**：等待 Rust Porter 提供最新 CLI schema/hash 以及 QA 确认 Stage D 运行窗口；若 11/19 前仍无结果，需要架构师介入重新排期。需要脚本团队协助让 `goal_tree_sync.py` 支持字段校验输出，以便在 `document-structure-template.md` 中记录运行指南。
