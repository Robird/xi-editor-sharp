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

## 当前聚焦（2025-11-19）
- **Sprint 1 Ready Queue 协调（A2）**：`docs/sprints/sptrint-1.md` 现已补齐 Ready Queue #1-#7 的执行顺序、依赖、通知矩阵；持续监控各角色在 Coordination Notes 中登记的触发条件，完成后即刻追踪到 `[MP-Tx]`、`[TS-Bx]`、`[QA-*]`、`[StageD::*]`，并将通知结果写回 `AGENTS.md` 与会议纪要。
- **Goal Tree ↔ Stage D doc sync（A1）**：`python scripts/goal_tree_sync.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json --inspector tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt --chunk-report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` 已刷新 `docs/architecture/port-blueprint.md#[BP-GoalTree]` 与 `docs/architecture/m3-implementation-plan.md#[MP-GoalTree]`，最新日志 `tests/xi.Core.Tests/Fixtures/Reports/goal-tree-sync-20251118-170042.log`。待将 `--check` guard 并入 `scripts/refresh_all_assets.py`，发现 drift 立即同步 QA anchors。
- **Stage D manifest ledger + QA anchors（A1/A2）**：`tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`（`rust_commit=3799d2be9db0ef040517ed69df1b717e96a8958e`，chunk `69ba7f25…`、grapheme `eb0c7c66…`、breaks `5d37a731…`、diff `fe76ed31…`、search `7eac7ecf…`）配合 `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt`、`chunk-bench-latest.txt` 驱动 `[RPM-ParityAssets]`、`[StageD::ParityAssets]`、`[QA-IngestionSmoke]`。等待 QA 把 chunk bench / grapheme telemetry 附到 `[QA-ChunkBench]`、`[QA-Telemetry]` 以封口风险。
- **TS-B blockers（TS-B1/B2/B5）**：`docs/architecture/type-system-migration-log.md#[TS-Bx]`、`docs/architecture/rope-port-mapping.md#[RPM-Matrix]`、`docs/architecture/system-overview.md#[SO-Map]` 需要持续写入 `_editVersion ↔ NodeCursorState`、StageDDescriptorHydrator、MetricAdapter/Breaks/Diff/Search skeleton 的 Rust/C#/QA 进度。等待 Rust Porter 泄露 `metric_windows[]` schema，QA rerun Stage D ingestion后更新 `[TS-B5]`。

## Goal Tree / Stage D 守护手册

### Goal Tree 同步流
1. **真源**：`docs/architecture/templates/goal-tree.yaml` 记录 G1–G6 的 `id/status/owner/next/rustCommit/dotnetCommit/rustCliVersion/featureGates/qaAnchors/stageDAnchors`。
2. **执行**：`python scripts/goal_tree_sync.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json --inspector tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt --chunk-report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`。`--check` 仅校验，默认写回 `docs/architecture/port-blueprint.md`、`docs/architecture/m3-implementation-plan.md`。
3. **产物**：脚本生成 `tests/xi.Core.Tests/Fixtures/Reports/goal-tree-sync-*.log`（最近：`goal-tree-sync-20251118-170042.log`），并在目标文件植入 `<!-- goal-tree:meta generated-at=... rust-commit=... payload-hash=... -->`。
4. **守护**：把 `--check` 纳入 `python scripts/refresh_all_assets.py --only goal-tree`，若 meta 与 YAML 不符，先刷新 `[BP-GoalTree]`/`[MP-GoalTree]`，再在 `[RPM-ParityAssets]`/`[StageD::ParityAssets]` 记录 drift 来源与修复动作。

### Stage D ledger + QA anchors
1. **刷新命令**：`python scripts/refresh_all_assets.py --only stage-d-fixtures` → `pwsh -File scripts/refresh_serialization_fixtures.ps1`（默认导出 cursor/chunk/grapheme/breaks/diff/search/tree_trace）→ loader/hydrator smoke。
2. **校验链**：完成导出后运行 `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`、`dotnet test Xi.Editor.sln --filter StageDDescriptor`、`dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures`，落盘 `stage-d-inspector-latest.txt`。
3. **事实表**：将 manifest (`rust_commit=3799d2be9db0ef040517ed69df1b717e96a8958e`，chunk `69ba7f25…`、grapheme `eb0c7c66…`、breaks `5d37a731…`、diff `fe76ed31…`、search `7eac7ecf…`、tree trace `22724af7…`) 与 CLI/inspector日志写入 `[RPM-ParityAssets]`、`[StageD::ParityAssets]`、`[TS-Bx]`、`[QA-IngestionSmoke]`。
4. **QA 接口**：当 QA 运行 chunk bench or telemetry 时，将 `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`、`grapheme-telemetry.trx` 同步到 `[QA-ChunkBench]`、`[QA-Telemetry]` 并附命令 `dotnet test Xi.Editor.sln -v m --filter "ChunkEnumeratorDiagnostics|GraphemeTelemetry"`。
5. **巡检节奏**：周三/周六完成 anchor audit；重大里程碑前 24h 追加一次。若 manifest/inspector 缺失立即在 `AGENTS.md` 标记阻塞并 ping owner。

## 最近完成
- **2025-11-19 – Sprint 1 协调记录**：在 `docs/sprints/sptrint-1.md#Coordination Notes` 写入 Ready Queue #1-#7 执行顺序、依赖与通知触发，确保 Rust→C#→QA→Information Researcher 行动链被 Goal Tree/Stage D/QA anchors 全量引用，同时在 `docs/meetings/2025-11-19-knowledge-refresh-chat.md` 备案该协调结果。
- **2025-11-19 – Sprint 1 runSubAgent skeleton**：创建 `docs/sprints/sptrint-1.md`，按照 `document-structure-template.md` front-matter 与 Intake/Ready Queue 章节搭建骨架，并提醒 C# Implementer / Rust Porter / QA Engineer / Information Researcher 在 Ready Queue 与 Coordination Notes 中补充证据化任务；待他们填充后再把锚点链接回 `Goal Tree` 与 QA/Stage D 台账。
- **2025-11-19 – 档案瘦身 + Goal Tree 再同步**：清理本档案冗余段落、重写聚焦/协作/风险清单，并运行 `python scripts/goal_tree_sync.py --manifest ...`（日志 `tests/xi.Core.Tests/Fixtures/Reports/goal-tree-sync-20251118-170042.log`）刷新 `[BP-GoalTree]` 与 `[MP-GoalTree]` meta。
- **2025-11-19 – Stage D manifest ledger sweep**：核对 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`、`stage-d-inspector-latest.txt`、`chunk-bench-latest.txt`，把最新 hash/命令回写到 `[RPM-ParityAssets]`、`[TS-B5]`、`[StageD::ParityAssets]`、`[QA-IngestionSmoke]`，同时确保 `system-overview.md#[SO-Map]` 指向相同证据。
- **2025-11-19 – Stage D playbook与系统图对齐**：完成 `docs/architecture/system-overview.md` 与 `docs/csharp-refactor/rope-serialization-fixture-playbook.md` 第二轮模板化，补全 `[StageD::StageDChecklist]`、`[QA-ChunkBench]`、`[QA-Telemetry]` 锚点并与 Goal Tree `stageDAnchors` 交叉引用。
- **2025-11-18 – Stage D checklist 实操化**：修复 `docs/operations/do-check-stage-d.md`、`scripts/refresh_serialization_fixtures.ps1`、`scripts/refresh_all_assets.py` 的 Stage D 步骤说明，形成 loader→hydrator→manifest verifier→inspector→chunk bench 的单一执行路径，供 QA 直接复用。
> 更早的历史请参考 `AGENTS.md##工作日志`。

## 待办 / 风险

| ID | 描述 | Owner | Due | 状态 |
| --- | --- | --- | --- | --- |
| T1 | 将 `python scripts/goal_tree_sync.py --check` 并入 `scripts/refresh_all_assets.py` nightly guard，并在 drift 时自动 tee `goal-tree-sync-*.log` | Architecture Mapper + Tooling | 2025-11-20 | 进行中（需脚本钩子） |
| T2 | Rust Porter 输出 `metric_windows[]`/`tree_builder_slice_trace` schema 到 manifest，并在 `[RPM-ParityAssets]`/`[TS-B5]` 建立字段映射 | Rust Porter | 2025-11-21 | 阻塞（等待 CLI 更新） |
| T3 | QA rerun `--only stage-d-fixtures` 并上传 `chunk-bench-latest.txt`、`grapheme-telemetry.trx` 至 `[QA-ChunkBench]`/`[QA-Telemetry]` | QA Engineer | 2025-11-20 | 待 QA 运行 |
| R9 | 若 nightly goal tree guard未上线，`[BP-GoalTree]`/`[MP-GoalTree]` 可能脱离 YAML，影响 A1 验收 | Architecture Mapper | 2025-11-20 检查点 | 风险监控 |
| R10 | Chunk/Grapheme/Breaks/Diff/Search 若无最新 manifest + QA 数据，`[TS-B5]` 难以关闭，Stage D 评审无法通过 | Architecture Mapper + Rust Porter + QA | 持续 | 风险监控 |

## 关键文档索引

| 文档 | 关键锚点 | 说明 |
| --- | --- | --- |
| `docs/architecture/port-blueprint.md` | `[BP-GoalTree]`、`[BP-RiskTable]` | Goal Tree 渲染目标片段与风险摘要，脚本更新目标之一。 |
| `docs/architecture/m3-implementation-plan.md` | `[MP-GoalTree]`、`[MP-T1]`、`[MP-R10]` | M3 任务/风险表；需与 Blueprint 共享 goal-tree 片段。 |
| `docs/architecture/rope-port-mapping.md` | `[RPM-Matrix]`、`[RPM-ParityAssets]`、`[RPM-Actions]` | 记录 Rust/C#/Stage D/QA 状态与事实表。 |
| `docs/architecture/type-system-migration-log.md` | `[TS-B1]`、`[TS-B2]`、`[TS-B5]` | Stage D/NodeCursor block 卡片，映射 TS-B 系列动作。 |
| `docs/architecture/system-overview.md` | `[SO-Map]`、`[SO-Responsibilities]` | 连接 Goal Tree ↔ Stage D ↔ QA 的跨文档索引。 |
| `docs/csharp-refactor/rope-serialization-fixture-playbook.md` | `[StageD::*]`、`[QA-*]` | Stage D/QA 手册，定义 CLI、fixture、基准命令。 |
| `docs/architecture/fixtures/parity-fixture-schema.md` | `[Fixture-*]` | 夹具 schema，与 manifest 字段命名保持一致。 |
| `docs/architecture/design-divergence-log.md` | `[Div-Active]` | 记录 Rust-only 或临时降级差异，避免漏斗。 |
| `docs/architecture/document-structure-template.md` | `template::*` | 文档模板唯一事实源，用于 lint 与收敛。 |
| `agents/*.md` | `role sections` | 协作者档案（Rust Porter / C# Implementer / QA / Architect）提供上下游输入。 |

---

**依赖 & 支持请求**
1. **Rust Porter**：尽快将 `metric_windows[]`、tree trace schema、默认 CLI flags 写入 `fixtures.manifest.json` 并在 `[StageD::FeatureGates]` 备案；无此字段 `[TS-B5]` 无法关闭。
2. **C# Implementer**：提供 `_editVersion ↔ NodeCursorState`、StageDDescriptorHydrator、MetricAdapter 变更摘要链接，方便回写 `[MP-T1]`、`[RPM-Matrix]`、`[TS-B2]`。
3. **QA Engineer**：本周内 rerun `python scripts/refresh_all_assets.py --only stage-d-fixtures`、`dotnet test Xi.Editor.sln --filter StageDDescriptor`、chunk bench 与 grapheme telemetry，将日志填入 `[QA-IngestionSmoke]`、`[QA-ChunkBench]`、`[QA-Telemetry]`。
