# Xi.Editor System Overview

> **Scope**: Summarize subsystems and doc owners.
> **Owner**: Architecture Mapper
> **Update Frequency**: 每次里程碑
> **Reviewers**: AI Architect · Rust Porter · C# Implementer · QA Engineer
> **Anchor Prefix**: SO
> **Last Synced Goal Tree**: 2025-11-18 19:26 UTC（`goal_tree_sync.py` log `tests/xi.Core.Tests/Fixtures/Reports/goal-tree-sync-20251118-192655.log`）

---

## [SO-Map] System Map
<a id="SO-Map"></a>
以 `[BP-GoalTree]` 的 G1-G6 作为全局航点，下表概括 Rope Core、Delta/Subset、Engine、Stage D、AI Team、Testing/QA 六大子系统的最近状态与单一事实来源。

| Subsystem | 状态概览（中文） | Key anchors / docs | Owner & signal |
| --- | --- | --- | --- |
| Rope Core | G1/G6 专注 `Node`/SharedNode/MetricAdapter，当前以字符串特化实现撑住功能，等待 `[TS-B2]` 的 MetricAdapter 测试合入后再推进 M4 泛型；`_editVersion` + 诊断已经在 `[MP-T1]` 轨道监控。C# skeleton ready（BreakPlan/DiffBuilder/Finder），详情见 `[RPM-Matrix]`。 | `[BP-GoalTree]` · `[RPM-Matrix]` · `[TS-B2]` · `[MP-T1]` | Architecture Mapper + C# Implementer（见 `../../agents/architecture-mapper.md` · `../../agents/csharp-implementer.md`） |
| Delta / Subset | Stage A/B JSON 已由 `[StageD::ParityAssets]` 维护，重点改为追踪 `[TS-B1]` CLI 交付以及 Goal Tree G1 里“版本票据 + parity”事项，确保 Rust/C# 互信不回退。 | `[BP-GoalTree]` · `[StageD::ParityAssets]` · `[TS-B1]` | Rust Porter + C# Implementer（`../../agents/rust-porter.md` · `../../agents/csharp-implementer.md`） |
| Engine | Stage C 镜像完成并记录在 `[RPM-Matrix]`，当前只需在 Goal Tree G4/G5 巡检 Engine 依赖是否阻塞 Diff/Search handoff；任意刷新均需回写 `[StageD::ParityAssets]`。 | `[RPM-Matrix]` · `[StageD::ParityAssets]` · `[BP-GoalTree]` | Architecture Mapper + AI Architect（`../../agents/architect.md`） |
| Stage D Diagnostics | Loader/Hydrator/Inspector 组合在 `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-191129.log` 记录 `python scripts/refresh_all_assets.py --only stage-d-fixtures` → exporter → loader/hydrator → manifest 校验 → StageDDescriptorInspector；Rust 默认 exporter 现同步 `metric_windows[]` ledger（见 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 与 `[Fixture-MetricWindows]`），把 Ready Queue #3 交付的窗口计数直接暴露给 `[StageD::ParityAssets]`。同一批 artefact 驱动 Release chunk bench (`tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`, chunk=255.93 MB/s，line=347.07 MB/s，alloc 37,944 B) 与 Grapheme telemetry (`tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry-20251118T191914Z.txt` + `.trx`, ≥12,000 ops, fallback 0.30%)，并通过 `[QA-ChunkBench]` / `[QA-Telemetry]` / `[SO-StageDQAEvidence]` 链接到 `agents/information-researcher.md#证据快照` 的 evidence index，确保 Stage D → QA → Release → Evidence 的单一事实源。 | `[StageD::StageDChecklist]` · `[StageD::FixtureFlow]` · `[StageD::ParityAssets]` · `[Fixture-MetricWindows]` · `[QA-ChunkBench]` · `[QA-Telemetry]` · `[SO-StageDQAEvidence]` | Rust Porter + QA Engineer（`../../agents/rust-porter.md` · `../../agents/qa-engineer.md`） |
| AI Team | 执行结构由 `[AIT-ExecutionPlan]` 定义；`AGENTS.md` + 各 `agents/*.md` 负责沉淀 SubAgent 产物，本概览文档作为跨文档 API。 | `[AIT-ExecutionPlan]` · `[Decision-M3-KeyCalls]` · `../../AGENTS.md` | AI Architect + Architecture Mapper |
| Testing & QA | QA 通过 `[QA-IngestionSmoke]` 与 `[QA-StageDManual]` 记录脚本运行、哈希、回归；与 `[MP-R10]` 风险表联动，对 Chunk/Grapheme 的遥测与 1 MB 基准做守门。 | `[QA-IngestionSmoke]` · `[QA-StageDManual]` · `[MP-R10]` | QA Engineer + Architecture Mapper |

## [SO-Responsibilities] Responsibilities & Owners
<a id="SO-Responsibilities"></a>
AI Team 角色与职责矩阵（英文标题+中文简述），保证任务与 `[MP-Tx]`、`[TS-Bx]` 锚点可反查。

| Role | 当前职责 | Anchors / dossiers |
| --- | --- | --- |
| AI Architect | 批准里程碑、合并裁决、触发风险升级，维护 `[Decision-M3-KeyCalls]` 与 `[MP-GoalTree]` 的一致性。 | `../../agents/architect.md` · `[Decision-M3-KeyCalls]` · `[MP-GoalTree]` |
| Architecture Mapper | 维护 Blueprint/Mapping/Type Log/System Overview，串联 G1-G6 与 Stage D/QA anchors。 | `../../agents/architecture-mapper.md` · `[BP-GoalTree]` · `[RPM-Matrix]` |
| Rust Porter | 负责 CLI、Parity fixtures、Rust helper，推进 `[TS-B1]`/`[TS-B3]`，并在 Stage D 手册登记。 | `../../agents/rust-porter.md` · `[TS-B1]` · `[TS-B3]` · `[StageD::FixtureFlow]` |
| C# Implementer | 实现 Rope/Delta/Engine/Grapheme 等特性，直连 `[MP-T1]`、`[MP-T3]` 子任务并回填 Tests。 | `../../agents/csharp-implementer.md` · `[MP-T1]` · `[MP-T3]` |
| QA Engineer | 执行 Stage D 脚本、1 MB 基准与遥测复查，守护 `[QA-IngestionSmoke]`、`[QA-StageDManual]`、`[MP-R10]`。 | `../../agents/qa-engineer.md` · `[QA-IngestionSmoke]` · `[QA-StageDManual]` · `[MP-R10]` |
| Information Researcher | 运营资料索引、推送差异，确保 `[AIT-ExecutionPlan]` 与 `AGENTS.md` 中的监控清单实时。 | `../../agents/information-researcher.md` · `[AIT-ExecutionPlan]` |

## [SO-Dependencies] Cross-Doc Dependencies
<a id="SO-Dependencies"></a>
- **Goal Tree → Execution（目标树驱动）**：`[BP-GoalTree]` 与 `[MP-GoalTree]` 是唯一的进度对照表；当 `[TS-B1]`/`[TS-B2]`/`[TS-B3]` 卡片状态变化时，Architecture Mapper 必须同步更新这两份快照与本文表格。
- **Stage D → Fixtures（夹具流水线）**：`[StageD::StageDChecklist]` 定义预检，`[StageD::FixtureFlow]` 负责脚本/CLI，`[StageD::ParityAssets]` 记录 manifest；任何 CLI 新旗标需要同时出现在 `[TS-B1]`（或 `[TS-B3]`）与 QA anchors，避免资产漂移。
- **QA Anchors → Risk（质量回路）**：`[QA-IngestionSmoke]` 与 `[QA-StageDManual]` 产出的哈希/日志直接喂给 `[MP-R10]`，若基线缺失则 Goal Tree 的 G2/G3/G6 自动进入 Watch 状态，由 AI Architect 参考 `[Decision-M3-KeyCalls]` 做进一步处置。

## [SO-StageDQAEvidence] Stage D → QA → Evidence Chain
<a id="SO-StageDQAEvidence"></a>
1. **Stage D Orchestration**：`[StageD::StageDChecklist]`→`[StageD::FixtureFlow]`→`[StageD::ParityAssets]` 负责记录 `python scripts/refresh_all_assets.py --only stage-d-fixtures`、`StageDDescriptorInspector`、manifest ledger与 CLI flag，确保 exporter / loader / hydrator / inspector 产物都有落点。
2. **QA Validation**：`[QA-IngestionSmoke]`、`[QA-ChunkBench]`、`[QA-Telemetry]` 吸收 Stage D artefact，附上 `tests/xi.Core.Tests/Fixtures/Reports/*.log` 路径与阈值结论，再把 smoke/bench/TRX 交付到 `[MP-R10]` 风险表。
3. **Evidence Index**：`docs/operations/do-check-stage-d.md`、`docs/sprints/sptrint-1.md#ready-queue`、`[SO-Map]` 以及 [agents/information-researcher.md#证据快照](../../agents/information-researcher.md#证据快照) 汇总同一批 log/manifest/telemetry，形成审计轨迹并为下一轮 Goal Tree / Stage D 审核提供 anchor。任何链路断裂都需回到 `[StageD::FixtureFlow]` 重新触发刷新，并在 `AGENTS.md` 记录广播。

## [SO-Sprint1EvidenceLoop] Sprint 1 Evidence Loop
<a id="SO-Sprint1EvidenceLoop"></a>
- **`[StageD::FixtureFlow]`**：在 Sprint 1 中承接 Rust Porter 与 C# Implementer 的 exporter/loader/hydrator/inspector 命令，串起 `python scripts/refresh_all_assets.py --only stage-d-fixtures`、`verify_fixture_manifest.py`、StageDDescriptorInspector，并把 manifest / CLI flag / `metric_windows[]` 结果写入唯一事实表，作为后续 QA 入口。
- **`[QA-IngestionSmoke]`**：QA Engineer 以 `[StageD::FixtureFlow]` 落盘的日志与 hash 为输入，在 Stage D pipeline 中确认“导出→装载→校验”无漂移，同时记录 chunk/grapheme/breaks/diff/search/tree trace 的最新哈希，用于解锁 Ready Queue #5 以及 Stage D rerun 决策。
- **`[QA-ChunkBench]`**：围绕 Stage D artefact 运行 Release chunk 基准，追踪 1 MB throughput + alloc gate，确保 Stage D 管线落地产生的 Rope 资产能够满足 `[MP-R10]` 风险门槛，并为 Stage D 之后的 Perf 讨论提供数值证据。
- **`[QA-Telemetry]`**：复用同一批 Stage D 输出驱动 Grapheme telemetry（≥10k operations）并监控 fallback ratio；一旦 `[QA-IngestionSmoke]` 或 `[StageD::FixtureFlow]` 更新 hash，Telemetry 也会立即回放，保证 Stage D pipeline 的 tail-end（遥测 + bench）持续锁定同一 artefact。
> Sprint 1 的 Ready Queue #1-#7 已将上述四个 anchor 联动：`[StageD::FixtureFlow]` 提供命令、`[QA-IngestionSmoke]` 记录刷写、`[QA-ChunkBench]` / `[QA-Telemetry]` 校验性能与稳定性，确保 Stage D pipeline 的证据闭环在一次迭代内完成并可审计。

## [SO-ChangeLog] Change Log
<a id="SO-ChangeLog"></a>
- **2025-11-19 – v1.0**：Architecture Mapper 创建 `system-overview.md`，恢复跨文档导航，并将 `[BP-GoalTree]`、Stage D anchors 与 QA anchors 串成单页治理入口。

[AGENTS.md]: ../../AGENTS.md
[AIT-ExecutionPlan]: ai-team-design-draft.md#ait-executionplan
[BP-GoalTree]: port-blueprint.md#bp-goaltree
[Decision-M3-KeyCalls]: m3-architect-decision.md#decision-m3-keycalls
[MP-GoalTree]: m3-implementation-plan.md#mp-goaltree
[MP-T1]: m3-implementation-plan.md#21-任务-1游标系统实现
[MP-T3]: m3-implementation-plan.md#23-任务-3chunk-迭代器骨架
[MP-R10]: m3-implementation-plan.md#r10
[RPM-Matrix]: rope-port-mapping.md#rpm-matrix
[StageD::StageDChecklist]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::StageDChecklist
[StageD::FixtureFlow]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::FixtureFlow
[StageD::ParityAssets]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::ParityAssets
[TS-B1]: type-system-migration-log.md#ts-b1
[TS-B2]: type-system-migration-log.md#ts-b2
[TS-B3]: type-system-migration-log.md#ts-b3
[QA-IngestionSmoke]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-IngestionSmoke
[QA-ChunkBench]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-ChunkBench
[QA-Telemetry]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-Telemetry
[QA-StageDManual]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-StageDManual
[RPM-Actions]: rope-port-mapping.md#rpm-actions
[Fixture-MetricWindows]: fixtures/parity-fixture-schema.md#Fixture-MetricWindows
[Chat-2025-11-20]: ../meetings/2025-11-20-type-mapping-sync-chat.md#architecture-mapper
