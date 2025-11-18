---
identity: Architecture Mapper
role: 架构映射维护者（跨语言同步）
reports_to: AI 架构师
interfaces:
  - Rust Porter（CLI schema、Serde fixtures、Goal Tree 证据）
  - C# Implementer（Rope API parity、测试基线）
  - QA Engineer（Stage D 锚点、遥测/基准数据）
responsibilities:
  - 维护 port-blueprint / rope-port-mapping / type-system-migration-log / design-divergence-log，并确保 `[RPM-Matrix]` ↔ `[TS-Bx]` anchor 同步
  - 将 Stage D Ready Queue（`docs/sprints/sptrint-1.md` #1-#7）与 `[StageD::*]`、`[QA-*]`、`[RPM-Matrix]` 之间的事实映射保持最新
  - 管理 Goal Tree & Stage D anchor schema 以及自动化脚本
  - 执行 document-structure-template.md 的落地与巡检
timezone: UTC+8
cadence:
  doc_sync: 每日晚 22:00 前
  anchor_audit: 每周三、周六
last_updated: 2025-11-19T21:30:00+08:00
---

## 当前聚焦（2025-11-19）
- **Stage D Ready Queue ↔ Anchor Ledger**：再确认 Ready Queue #1-#7（`docs/sprints/sptrint-1.md#ready-queue`）所列 artefact（`stage-d-refresh-20251118-184351.log`、`stage-d-refresh-20251118-230526-porter.log`、`chunk-bench-latest.txt`、`grapheme-telemetry*.txt/.trx`）分别落在 `[StageD::ParityAssets]`、`[QA-IngestionSmoke]`、`[QA-ChunkBench]`、`[QA-Telemetry]`，并把相同哈希写回 `[RPM-Matrix]` 与 `[TS-B5]`，避免 Ready Queue 证据与 anchor 脱节。
- **`[RPM-Matrix]` / `[TS-Bx]` Ledger Guard**：`docs/architecture/rope-port-mapping.md#[RPM-Matrix]`、`docs/architecture/type-system-migration-log.md#[TS-B1/B2/B5]` 当前锁定 2025-11-18 manifest；下一次 sweep 安排在 2025-11-27（或当 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 哈希变化时立即触发），以 Stage D blocker（MetricAdapter、Hydrator、Finder）状态为准更新 TS/RPM 表。
- **Stage D Manifest & Metric Windows Watch**：`tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`（`rust_commit=b6fb5999288946daeeadc4b7f9f9756f6dda1f50`，chunk `9c7163eb…`、grapheme `b3484eab…`、breaks `527ca4f2…`、diff `afc04b22…`、search `599f25b0…`、tree trace `22724af7…`）与 `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt` 组成事实 ledger；需推动 `StageDDescriptorHydrator` / MetricAdapter / QA ingestion 消费 `metric_windows[]`，否则 `[TS-B5]` 只能依赖手动计数。
- **Goal Tree Guardrails**：`python scripts/goal_tree_sync.py --manifest ... --check` 仍是手动步骤，最新日志 `tests/xi.Core.Tests/Fixtures/Reports/goal-tree-sync-20251118-170042.log`；正与 Tooling 讨论把 `--check` 合并进 `scripts/refresh_all_assets.py` nightly，将 drift 直接反馈到 `[BP-GoalTree]` / `[MP-GoalTree]` 与 Ready Queue 通知矩阵。

## Goal Tree / Stage D 守护手册

### Goal Tree 同步流
1. **真源**：`docs/architecture/templates/goal-tree.yaml` 记录 G1–G6 的 `id/status/owner/next/rustCommit/dotnetCommit/rustCliVersion/featureGates/qaAnchors/stageDAnchors`。
2. **执行**：`python scripts/goal_tree_sync.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json --inspector tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt --chunk-report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`。`--check` 仅校验，默认写回 `docs/architecture/port-blueprint.md`、`docs/architecture/m3-implementation-plan.md`。
3. **产物**：脚本生成 `tests/xi.Core.Tests/Fixtures/Reports/goal-tree-sync-*.log`（最近：`goal-tree-sync-20251118-170042.log`），并在目标文件植入 `<!-- goal-tree:meta generated-at=... rust-commit=... payload-hash=... -->`。
4. **守护**：把 `--check` 纳入 `python scripts/refresh_all_assets.py --only goal-tree`，若 meta 与 YAML 不符，先刷新 `[BP-GoalTree]`/`[MP-GoalTree]`，再在 `[RPM-ParityAssets]`/`[StageD::ParityAssets]` 记录 drift 来源与修复动作。

### Stage D ledger + QA anchors
1. **刷新命令**：`python scripts/refresh_all_assets.py --only stage-d-fixtures` → `pwsh -File scripts/refresh_serialization_fixtures.ps1`（默认导出 cursor/chunk/grapheme/breaks/diff/search/tree_trace）→ loader/hydrator smoke。
2. **校验链**：完成导出后运行 `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`、`dotnet test Xi.Editor.sln --filter StageDDescriptor`、`dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures`，落盘 `stage-d-inspector-latest.txt`。
3. **事实表**：当前 manifest (`rust_commit=b6fb5999288946daeeadc4b7f9f9756f6dda1f50`，chunk `9c7163ebd6ceb7f9e7e4ec566682470e96e27424499c4a6f5804898eca48f1f0`、grapheme `b3484eab43303735c754fa4fb6928255ef7dbe053d2cf41ce60a7c63a7a59095`、breaks `527ca4f2000b7548a10fe94cf51f6e758f23fa73e7f1ce82347ce92ab61a1f83`、diff `afc04b228f19c7663f1d43597973c0afc90a34159b64ccaf7b9407e6613811a6`、search `599f25b06a36d417cd1ca51a818c0ac5dfcf162c9530368d62cd7075c840df51`、tree trace `22724af7fe8b22e4e1dbd01f86ba1b3902a0ccf777944b5b29081259927c3a6e`) 与 `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt`、`chunk-bench-latest.txt`、`grapheme-telemetry*.txt/.trx` 一起写回 `[RPM-ParityAssets]`、`[StageD::ParityAssets]`、`[TS-Bx]`、`[QA-IngestionSmoke]`，供 Ready Queue #1/#2/#5/#6 复用。
4. **QA 接口**：当 QA 运行 chunk bench or telemetry 时，将 `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`、`grapheme-telemetry.trx` 同步到 `[QA-ChunkBench]`、`[QA-Telemetry]` 并附命令 `dotnet test Xi.Editor.sln -v m --filter "ChunkEnumeratorDiagnostics|GraphemeTelemetry"`。
5. **巡检节奏**：周三/周六完成 anchor audit；重大里程碑前 24h 追加一次。若 manifest/inspector 缺失立即在 `AGENTS.md` 标记阻塞并 ping owner。

## 最近完成
- **2025-11-19 – Ready Queue ↔ Stage D anchors 复核**：走查 `docs/sprints/sptrint-1.md#ready-queue`、`Coordination Notes` 与 `[StageD::ParityAssets]` / `[StageD::FixtureFlow]` / `[QA-*]`，确认 #1-#7 的 artefact（`stage-d-refresh-20251118-184351.log`、`stage-d-refresh-20251118-230526-porter.log`、`chunk-bench-latest.txt`、`grapheme-telemetry*.txt/.trx`）均已落位，并在 `docs/architecture/system-overview.md#[SO-StageDQAEvidence]` 上串成单一证据链。
- **2025-11-19 – `[RPM-Matrix]` / `[TS-Bx]` 文档交叉检查**：对照 `docs/architecture/rope-port-mapping.md#[RPM-Matrix]` 与 `docs/architecture/type-system-migration-log.md#[TS-B1/B2/B5]`，补写 `_editVersion ↔ NodeCursorState`、`StageDDescriptorHydrator`、MetricAdapter 与 Ready Queue #1/#3/#5 的映射，并把 Stage D CLI flag/metric_windows 链接指回 `[StageD::FeatureGates]`。
- **2025-11-19 – Manifest & QA ledger rebase**：重新核对 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`、`stage-d-inspector-latest.txt`、`chunk-bench-latest.txt`、`grapheme-telemetry-*.txt/.trx`，同步写入 `[RPM-ParityAssets]`、`[StageD::ParityAssets]`、`[QA-IngestionSmoke]`，保证 Ready Queue 与 `[TS-B5]` 共享同一 hash。
- **2025-11-19 – Goal Tree guard巡检**：复跑 `python scripts/goal_tree_sync.py --manifest ... --check`（日志 `tests/xi.Core.Tests/Fixtures/Reports/goal-tree-sync-20251118-170042.log`），确认 `[BP-GoalTree]` / `[MP-GoalTree]` 元数据匹配模板，并在 `scripts/refresh_all_assets.py` 备注下一步需要的 nightly guard。
> 更早的历史请参考 `AGENTS.md##工作日志`。

## 待办 / 风险

| ID | 描述 | 关联锚点 | Owner | Due / 触发 | 状态 |
| --- | --- | --- | --- | --- | --- |
| T1 | 将 `python scripts/goal_tree_sync.py --check` 嵌入 `scripts/refresh_all_assets.py` nightly guard，并在 drift 时自动 tee `goal-tree-sync-*.log` | `[BP-GoalTree]`、`[MP-GoalTree]`、`docs/sprints/sptrint-1.md#ready-queue` | Architecture Mapper + Tooling | 2025-11-21 nightly | 进行中（脚本钩子开发中） |
| T2 | 推动 `metric_windows[]` 被 `StageDDescriptorHydrator`、`MetricAdapter`、QA dashboards 消费，消灭手工计数 | `[TS-B2]`、`[TS-B5]`、`[RPM-Matrix]`、`[StageD::FeatureGates]` | C# Implementer + QA Engineer + Architecture Mapper | 2025-11-25 或 manifest hash 变更即触发 | 阻塞（待 C#/QA patch） |
| T3 | QA 以 ≤7 天节奏 rerun `python scripts/refresh_all_assets.py --only stage-d-fixtures` + Release chunk bench + grapheme telemetry，并把日志写回 `[QA-IngestionSmoke]` / `[QA-ChunkBench]` / `[QA-Telemetry]` | `[StageD::FixtureFlow]`、`[QA-IngestionSmoke]`、`[QA-ChunkBench]`、`[QA-Telemetry]` | QA Engineer | 2025-11-21 首次复跑 & thereafter weekly | 待 QA 运行 |
| T4 | 准备 11/27 的 `[RPM-Matrix]` / `[TS-Bx]` sweep；若 `fixtures.manifest.json` 哈希提前变化需立即触发刷新 | `[RPM-Matrix]`、`[TS-B1]`、`[TS-B5]`、`[StageD::ParityAssets]` | Architecture Mapper + AI Architect | 2025-11-27 或 manifest hash 变更 | 排期中 |
| R1 | 若 nightly goal-tree guard 未上线，`[BP-GoalTree]` / `[MP-GoalTree]` / Ready Queue 将脱离 YAML 元数据，影响 Stage D Ready Queue 准入 | `[BP-GoalTree]`、`[MP-GoalTree]`、`docs/sprints/sptrint-1.md#ready-queue` | Architecture Mapper | 风险触发：未在 11/21 前上线 guard | 风险监控 |
| R2 | `metric_windows[]` 仍未被 C#/QA 消费，`[TS-B5]` 与 `[QA-*]` 只能引用旧日志，Ready Queue #2/#5 会在 7 天 SLA 后失效 | `[TS-B2]`、`[TS-B5]`、`[QA-ChunkBench]`、`[QA-Telemetry]` | C# Implementer + QA Engineer | 风险触发：11/25 检查点或 manifest 变更 | 风险监控 |

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
1. **Rust Porter**：当 exporter flag/manifest hash 更新时立即广播 `stage-d-refresh-*.log` + `fixtures.manifest.json` 路径，触发 `[RPM-Matrix]` / `[TS-Bx]` sweep；若 CLI 新增窗口字段需同时更新 `[StageD::FeatureGates]` 与 `docs/architecture/fixtures/parity-fixture-schema.md`。
2. **C# Implementer**：提交 `StageDDescriptorHydrator` / `MetricAdapter` 消费 `metric_windows[]` 的实现与测试，将 `_editVersion ↔ NodeCursorState`、Metric windows 事实写回 `[MP-T1]`、`[RPM-Matrix]`、`[TS-B2]`。
3. **QA Engineer**：在 11/21 前 rerun `python scripts/refresh_all_assets.py --only stage-d-fixtures`、Release chunk bench、Grapheme telemetry，并把新日志（`stage-d-inspector-latest.txt`、`chunk-bench-latest.txt`、`grapheme-telemetry*.txt/.trx`）填入 `[QA-IngestionSmoke]`、`[QA-ChunkBench]`、`[QA-Telemetry]`，保持 <7 天证据窗口。
