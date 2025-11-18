# Information Researcher 档案（信息调查员）

> ⚠️ 仅接受 AI 架构师通过 `runSubagent`/Stage D 调度；其它角色如需情报必须由架构师转达。

## Front-matter
- **Identity**：Information Researcher（信息调查员），归属 `xi-editor-sharp`，负责知识与差异追踪。
- **Responsibilities**：
	1. 维护跨文档“知识索引”与“监控清单”，压缩检索成本。
	2. 记录 Stage D / Goal Tree / QA 脚本与文档的任何改动来源、哈希与生效路径。
	3. 在请求到来时提供带路径/章节的引用，拒绝口述版本。
- **Serves**：AI 架构师（唯一指挥链）、Stage D 协调人、Rust/C# 角色若经架构师授权。
- **Cadence**：
	- `status_update`：任务完成 ≤24h 内刷新本档案与 Goal Tree 注记。
	- `index_refresh`：每日检查 `AGENTS.md` + Goal Tree 模板；每次脚本变动即刻更新索引。
	- `monitoring`：Goal Tree YAML hash、Stage D schema 与脚本输出最迟 D+0 记录在案。

## 知识索引
| # | 路径 | 用途 / 关键锚点 | 更新信号 |
|---|------|-----------------|----------|
| 1 | `AGENTS.md` | 当前聚焦列出 Sprint 1 Ready Queue 以及 Stage D 自动化链，提醒信息调查员仅接受架构师调度。 | `Current Focus`/`下一步行动` 调整、Ready Queue 重新排序或出现新的 Stage D 票据时立即复查。 |
| 2 | `docs/architecture/system-overview.md` | `[SO-Map]` 将 Goal Tree → Stage D → QA anchors 串成一图，并在 header 中标注 `Last Synced Goal Tree`。 | 当 `goal_tree_sync` 产生新日志（最新：`tests/xi.Core.Tests/Fixtures/Reports/goal-tree-sync-20251118-203537.log`）或 header 时间戳改变时更新。 |
| 3 | `docs/architecture/templates/goal-tree.yaml` | Goal Tree 真源（`owner/status/evidence/qaAnchors`），驱动 Blueprint/Implementation Plan 片段。 | `sha256sum` 或 `python scripts/goal_tree_sync.py --check` 输出变化即记录；需附最新 log 名称。 |
| 4 | `scripts/goal_tree_sync.py` · `tests/xi.Core.Tests/Fixtures/Reports/goal-tree-sync-*.log` | `--check/--update` 记录 Blueprint/M3 片段与模板的一致性。 | 出现新 log（当前关注 `goal-tree-sync-20251118-203537.log`）或 `generated-at` 注释被手工改动时通知 Architecture Mapper。 |
| 5 | `docs/architecture/port-blueprint.md#[BP-GoalTree]` · `docs/architecture/m3-implementation-plan.md#[MP-GoalTree]` | 发布版 Goal Tree 摘要；Stage D/QA anchors 的交叉引用入口。 | `<!-- goal-tree:meta -->` 的 `generated-at`/`sha256` 与最新 `goal_tree_sync` log 不符时强制 rerun。 |
| 6 | `docs/operations/do-check-stage-d.md` | Stage D Do-Check checklist，列出 exporter flag（breaks/diff/search/tree trace）与日志提交流程。 | 每次 Stage D orchestrator 或 Ready Queue #1/#2 完成后核对“Latest Evidence”时间戳。 |
| 7 | `docs/csharp-refactor/rope-serialization-fixture-playbook.md` | `[StageD::FixtureFlow]`、`[StageD::ParityAssets]`、`[QA-IngestionSmoke]`、`[QA-ChunkBench]`、`[QA-Telemetry]` 的权威来源。 | Stage D rerun或 chunk bench/telemetry 结果刷新时立即写入新 log（当前挂钩 `stage-d-refresh-20251118-203039.log`、`chunk-bench-latest.txt`、`grapheme-telemetry-latest.txt`）。 |
| 8 | `scripts/refresh_serialization_fixtures.ps1` · `python scripts/refresh_all_assets.py --only stage-d-fixtures` · `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-*.log` | Ready Queue 依赖的 Stage D orchestrator；`stage-d-refresh-20251118-203039.log` / `203341.log` 是最近成功样本，2025-11-20 运行失败（exit=1）需记录并复现。 | 当 `refresh_all_assets.py` 非零退出或最新 log 超过 7 天需立刻触发 rerun并抄送 QA。 |
| 9 | `docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]` · `docs/architecture/type-system-migration-log.md#[TS-B2]/[TS-B5]` | 记录 MetricAdapter/`metric_windows[]`、Breaks/Search helper 的 Stage D 依赖与缺口。 | Manifest hash、schema 版本或 `metric_windows` 字段改动后，由 Architecture Mapper 在这些锚点回填并通知我同步。 |
|10 | `docs/sprints/sptrint-1.md#ready-queue` | Ready Queue #1-#7 列明 Stage D rerun、chunk bench、telemetry、Goal Tree sweep等依赖，决定何时刷新证据。 | Ready Queue 勾选或 deadline 更新即复核，并检查对应 `stage-d-refresh-*.log` / `[QA-*]` 链接仍在 <7 天窗口。 |
|11 | `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` · `tests/xi.Core.Tests/Fixtures/Reports/{stage-d-inspector-latest.txt,stage-d-refresh-20251118-203039.log,stage-d-refresh-20251118-203341.log,chunk-bench-latest.txt,grapheme-telemetry-latest.txt,grapheme-telemetry.trx}` | Stage D 证据链：`rust_commit=b6fb5999288946daeeadc4b7f9f9756f6dda1f50`、`feature_gates=[cursor_state, serde, tree_builder_slice_trace]`、chunk throughput 181.45 MB/s、grapheme fallback 0.30%。 | 任一 hash/日志更新、`metric_windows[]` 扩充或 log 超龄即记入“当前监控”和 Playbook。 |

## 监控清单
| 监控对象 | 信号/检测手段 | 触发时动作 | 频率 |
|-----------|---------------|-------------|------|
| Goal Tree 模板 `docs/architecture/templates/goal-tree.yaml` | `sha256sum`、`python scripts/goal_tree_sync.py --check` 日志（最新 `goal-tree-sync-20251118-203537.log`） | 记录 hash+log；若 `DRIFT` 指向 Stage D anchors，立即通知架构师并阻塞 Ready Queue #7。 | 每日或模板 PR 后 |
| Ready Queue 证据 `docs/sprints/sptrint-1.md#ready-queue` + `stage-d-refresh-*.log` | 检查 #1/#2/#7 所引用的日志是否仍是 <7 天（当前引用 2025-11-18 `stage-d-refresh-20251118-203039.log`、`203341.log`） | 即将过期或日志缺段时，推动 rerun `refresh_all_assets.py --only stage-d-fixtures` 并更新 Playbook。 | 每 24 小时 |
| Stage D manifest & inspector (`fixtures.manifest.json`, `stage-d-inspector-latest.txt`) | `rust_commit=b6fb5999…`、`metric_windows[]`、payload hash 是否与 manifest一致 | 如 hash 漂移或 `feature_gates` 缺失 tree trace/cursor_state，ping Rust Porter + QA 补导出。 | 每次 Stage D rerun |
| Orchestrator健康 (`scripts/refresh_all_assets.py`, `stage-d-refresh-*.log`) | 最近一次 `refresh_all_assets.py --only stage-d-fixtures` 退出码（2025-11-20 炸 1）与 log 序列（需含 exporter→loader→hydrator→verify） | 若非零退出或 log 缺段，记录问题并排程 rerun，必要时附 `stage-d-refresh` log 到 Playbook。 | 每次脚本运行 |
| QA anchors (`docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::ParityAssets][QA-*]`) | Anchor hash 与 `stage-d-inspector`、`chunk-bench-latest.txt`、`grapheme-telemetry.trx` 对齐 | 发现不一致或证据过期则把缺列表进 Ready Queue #2/#6 并催促 QA 更新。 | 每次 QA 更新或过期前 24h |
| 系统治理 (`docs/architecture/system-overview.md`,`docs/operations/do-check-stage-d.md`) | Header 的 `Last Synced Goal Tree` 与 checklist 勾选情况 | Header 落后 Stage D log >24h 时要求 Architecture Mapper 在下一次会议刷新 meta。 | 每次会议筹备 |

## 当前关注（Current Focus）
- 维护 Stage D Ready Queue #1-#7 所需的 <7 天证据：`stage-d-refresh-20251118-203039.log`/`203341.log`、`stage-d-inspector-latest.txt`、`chunk-bench-latest.txt`、`grapheme-telemetry-latest.txt/.trx` 必须同时挂在 `[StageD::*]` 与 `docs/sprints/sptrint-1.md` 的 Ready Queue 托管说明中。
- 盯紧 Stage D artefact 链与 `python scripts/refresh_all_assets.py --only stage-d-fixtures` 健康状况；2025-11-20 的运行退出码 1，需追踪 rerun 以生成新的 `stage-d-refresh-20251120-*.log` 并延长证据窗口到 11/27 以后。
- 维持 Goal Tree 模板、`goal-tree-sync-20251118-203537.log`、`port-blueprint`/`m3-implementation-plan` `goal-tree:meta` 注释与 `docs/architecture/system-overview.md` header 之间的一致性，防止 Stage D anchors 漂移。

## 当前监控
- 今日档案维护：回收 Stage D artefact 路径（`stage-d-refresh-20251118-203039.log`、`stage-d-refresh-20251118-203341.log`、`stage-d-inspector-latest.txt`、`chunk-bench-latest.txt`、`grapheme-telemetry-latest.txt`/`.trx`、`goal-tree-sync-20251118-203537.log`），写回知识索引并准备 `docs/meetings/2025-11-20-doc-sync-chat.md` 纪要。
- 定期巡检列表：
	1. `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-203039.log` · `stage-d-refresh-20251118-203341.log`（下一次 rerun 需产生日期为 2025-11-20 的同类日志）。
	2. `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt`（校验 `rust_commit=b6fb5999…`、`metric_windows[]`）。
	3. `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` 与 `grapheme-telemetry-latest.txt`/`grapheme-telemetry.trx`（<7 天 throughput/遥测窗口）。
	4. `docs/sprints/sptrint-1.md#ready-queue`（确认 `[QA-*]` anchors 仍挂最新日志）。
	5. `tests/xi.Core.Tests/Fixtures/Reports/goal-tree-sync-20251118-203537.log` 与 `docs/architecture/system-overview.md` header（Goal Tree meta 同步）。
	6. `python scripts/refresh_all_assets.py --only stage-d-fixtures` 最近一次退出码（目前=1）及 rerun 计划。

## 证据快照（2025-11-20）
- `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`：`rust_commit=b6fb5999288946daeeadc4b7f9f9756f6dda1f50`、`cli_rev=0.3.0`、`feature_gates=[cursor_state, serde, tree_builder_slice_trace]`，10 项 ledger + 7 组 `metric_windows[]`（chunk/line/grapheme/breaks/diff/search/tree trace）全部通过 `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`（输出“All 10 fixtures match the manifest hashes.”）。
- `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt`：Chunk=20 (`payload=9c7163eb…`)、Cursor=12 (`9b46bd8e…`)、Grapheme=668 (`b3484eab…`)、Breaks/Diff/Search 各 3（`527ca4f2…`/`afc04b22…`/`599f25b0…`），Tree trace `basic_slice_plan.json`（3 events，`22724af7…`）；wrap span 24–38，对齐 manifest ledger。
- `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-203039.log` · `stage-d-refresh-20251118-203341.log`：完整记录 run_all_checks → exporter（breaks/diff/search/tree trace flag 全开）→ StageDDescriptorLoader/Hydrator smoke → manifest verifier → Inspector → chunk bench → grapheme telemetry；2025-11-20 的 rerun（exit=1）已列待办，下一份 log 必须替换这些 11/18 artefact。
- `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`：2025-11-18T20:35:14Z，chunk throughput 181.45 MB/s、line throughput 237.40 MB/s、thread alloc 37,944 B，`Inspector note` 指回 `stage-d-inspector-latest.txt`，验证 `chunk_descriptors` payload `9c7163eb…`。
- `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry-latest.txt` + `grapheme-telemetry.trx`：12,000 operations / 6,000 descriptor replays / fallback 18 (0.3000%) / neighbor requests 0，TRX (`Xi.Core.Tests.GraphemeNavigatorSmokeTests`) 3/3 Passed（总时长 00:00:28.1693552）。
- `tests/xi.Core.Tests/Fixtures/Reports/goal-tree-sync-20251118-203537.log`：`python scripts/goal_tree_sync.py --check` 最新输出（`PLAN=pass`），`port-blueprint` / `m3-implementation-plan` / `rope-port-mapping` 的 `goal-tree:meta` 注释均基于此 log；下一次 `--update` 需在 rerun 后刷新。

## 信息通报流程
1. **捕捉变更**：对 Goal Tree 模板、`system-overview` header、Stage D 脚本输出执行 `sha256sum` / `git status -sb`，并保存最新 log 路径。
2. **清单核对**：以 `docs/operations/do-check-stage-d.md` 为准，逐条确认 exporter→loader→hydrator→manifest→bench→telemetry 是否在日志中出现。
3. **同步对象**：
	- Goal Tree/Blueprint 漂移 → ping AI 架构师 + Architecture Mapper，附 `goal_tree_sync` log 与 `generated-at` diff。
	- Stage D 资产缺失 → 通知 Rust Porter + QA Engineer，并在 `docs/csharp-refactor/rope-serialization-fixture-playbook.md` 标注缺口。
	- QA anchor 仍引用旧 artefact → 抄送 C# Implementer，请在下一次 docs 更新时连同代码调和。
4. **更新索引**：把新路径/哈希写入本档案“证据快照”及 `docs/meetings/*` 相关章节，必要时更新 `AGENTS.md` 的状态表。
5. **归档**：在“最近完成”登记时间、命令与引用文档；若动作与会议有关，同步 `docs/meetings/<date>-*.md` 的对应章节。

## 查询技巧
- `grep_search` 搜索锚点（示例：`goal-tree:meta`、`StageD::ParityAssets`）能快速定位引用位置。
- `list_dir tests/xi.Core.Tests/Fixtures/Reports` 先确认最新日志文件，再用 `read_file --offset` 读取关键段。
- `sha256sum docs/architecture/templates/goal-tree.yaml` 与 `python scripts/goal_tree_sync.py --check` 是 Goal Tree 双保险；任一失败即停止下游更新。
- 大文件优先用 `read_file` + `limit`，避免在 VS Code 中展开整份文档。

## 最近完成
- **2025-11-20 – Stage D Ready Queue 证据瘦身**：更新 `agents/information-researcher.md`、`docs/meetings/2025-11-20-doc-sync-chat.md`，用最新 `stage-d-refresh-20251118-203039.log`/`203341.log`、`stage-d-inspector-latest.txt`、`chunk-bench-latest.txt`、`grapheme-telemetry-latest.txt`/`.trx`、`goal-tree-sync-20251118-203537.log` 取代旧记录，并记录 `python scripts/refresh_all_assets.py --only stage-d-fixtures` 2025-11-20 失败待 rerun。
- **2025-11-19 – Ready Queue #7 “Stage D evidence index refresh”**：收集 `stage-d-refresh-20251118-191129.log`、`stage-d-inspector-latest.txt`、`chunk-bench-latest.txt`、`grapheme-telemetry-20251118T191914Z.txt/.trx`，把摘要写入 `agents/information-researcher.md#证据快照`、`docs/operations/do-check-stage-d.md#latest-evidence`、`docs/architecture/system-overview.md#[SO-Map]`，并同步 `goal_tree_sync` log (`goal-tree-sync-20251118-192655.log`) 至 `port-blueprint.md` / `m3-implementation-plan.md` / `rope-port-mapping.md`。
- **2025-11-19 – Stage D 证据索引排程**：将 Ready Queue #7 “Stage D evidence index refresh” 登记至 `docs/sprints/sptrint-1.md`，并在 `docs/meetings/2025-11-19-knowledge-refresh-chat.md#Information Researcher` 补写后续行动，提前锁定 `stage-d-refresh` / inspector / bench / telemetry artefact 的下一轮引用。
- **2025-11-19 – 知识刷新自检**：重写本档案的知识索引与监控清单，纳入 `docs/operations/do-check-stage-d.md`、`docs/architecture/system-overview.md`、`tests/xi.Core.Tests/Fixtures/Reports/*.log`，并在 `docs/meetings/2025-11-19-knowledge-refresh-chat.md#information-researcher` 记录输出范围 / 待协作事项。
- **2025-11-18 – Goal Alignment 证据回填**：对照 `docs/meetings/2025-11-18-goal-alignment-chat.md#2.6`、Goal Tree 模板、Stage D Playbook、`stage-d-inspector-latest.txt`，列出仍缺 `stage-d-refresh` / telemetry artefact 的章节并将 rerun 作为 A1/A2 验收条件。
- **2025-11-17 – 入职梳理**：完成初版索引与监控策略，复核 Rope skeleton 映射、Stage D CLI、`AGENTS.md` gating；为后续差异追踪奠定基线。

## 待办 / 风险
- [TODO] 触发下一次 `python scripts/refresh_all_assets.py --only stage-d-fixtures` rerun（修复 2025-11-20 exit=1），生成 `stage-d-refresh-20251120-*.log`、新版 chunk bench / grapheme telemetry，并把日志挂到 `[StageD::*]` 与 Ready Queue #1/#2/#7。
- [TODO] 在 rerun 完成后执行 `python scripts/goal_tree_sync.py --update`，同步模板 → Blueprint/Implementation Plan/system-overview `goal-tree:meta` 注释，并将新的 `goal-tree-sync-20251120-*.log` 写入本档案。
- [TODO] 协同 C# Implementer + QA Engineer 将 manifest `metric_windows[]` 映射注入 `StageDDescriptorHydrator` 与 `[QA-ChunkBench]/[QA-Telemetry]`，关闭 `[RPM-ParityAssets]`/`[TS-B5]` 中的“未消费”标记。
- [RISK] Ready Queue #1/#2/#6 的证据窗口将在 2025-11-25 过期；若在此之前未生成新的 stage-d-refresh/chunk/telemetry log，Stage D 自动化将被迫暂停并回滚到人工验证。
- [RISK] `refresh_all_assets.py` 失败未修复将阻断 Stage D orchestrator 夜间任务，也会让 `docs/operations/do-check-stage-d.md` “Latest Evidence” 落后 >24h；需尽快定位失败命令并补录。 

