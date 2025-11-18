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
| 1 | `AGENTS.md` | 角色职责、SubAgent gating、行 ~354 “待入职员工” 提醒；确认谁能调度信息调查员。 | 架构师更新 `Current Focus` 或 Stage D 任务后会同步本文件；如提醒消失需立即回填。 |
| 2 | `docs/architecture/system-overview.md` | 单页蓝图，`[SO-Map]` 聚合 Goal Tree → Stage D → QA 链，记录 `Last Synced Goal Tree` 与 `goal_tree_sync` 日志。 | 每次 `goal_tree_sync` 生成新 log（现为 `tests/xi.Core.Tests/Fixtures/Reports/goal-tree-sync-20251118-170042.log`）或 `system-overview` header 时间戳变动时更新。 |
| 3 | `docs/architecture/templates/goal-tree.yaml` | Goal Tree 真源（`owner/status/evidence/qaAnchors`）；驱动 `goal_tree_sync` 与 Blueprint/Implementation Plan 片段。 | 监控 `sha256sum` 与 `python scripts/goal_tree_sync.py --check` 输出；hash 改动需记录提交与脚本版本。 |
| 4 | `scripts/goal_tree_sync.py` + `tests/xi.Core.Tests/Fixtures/Reports/goal-tree-sync-*.log` | Goal Tree → `port-blueprint.md`/`m3-implementation-plan.md` 同步器；log 记录写入的文件。 | 新 log 文件 / `generated-at` 注释漂移；若 `DRIFT:` 涉及 Stage D anchors，通知架构师+Rust Porter。 |
| 5 | `docs/architecture/port-blueprint.md#[BP-GoalTree]` · `docs/architecture/m3-implementation-plan.md#[MP-GoalTree]` | 已发布的 Goal Tree 摘要；也是 Stage D/QA anchor 的查证入口。 | 当 `goal_tree_sync` log 或 `system-overview` 更新时，确认两个片段 hash 相同。 |
| 6 | `docs/operations/do-check-stage-d.md` | Stage D Do-Check checklist，声明 exporter 默认 flags（breaks/diff/search/tree trace）与日志要求。 | 每次 `python scripts/refresh_all_assets.py --only stage-d-fixtures` 或手动刷新后对照此清单；更新 `Last Synced Goal Tree` 字段。 |
| 7 | `docs/csharp-refactor/rope-serialization-fixture-playbook.md` | `[StageD::FixtureFlow]`, `[StageD::ParityAssets]`, `[QA-IngestionSmoke]`, `[QA-ChunkBench]`, `[QA-Telemetry]` 真源。 | Rust Porter/QA 交付 manifest、chunk bench或 telemetry 后必须在此文登记路径/哈希。 |
| 8 | `scripts/refresh_serialization_fixtures.ps1` · `python scripts/refresh_all_assets.py --only stage-d-fixtures` · `python scripts/verify_fixture_manifest.py` | Stage D orchestrator链：Rust exporter → loader/hydrator → manifest 校验 → QA；对应日志保存在 `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-*.log`。 | 当脚本退出码 ≠0 或日志缺少 loader/hydrator/manifest段落时报警。 |
| 9 | `docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]` · `docs/architecture/type-system-migration-log.md#[TS-B2]/[TS-B5]` | 记录 `_editVersion ↔ NodeCursorState`、MetricAdapter、Breaks/Search helper 的差距及 Stage D 依赖。 | Manifest hash 或 MetricAdapter字段命名变更时同步；Architecture Mapper 会在这些锚点更新说明。 |
|10 | `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` · `tests/xi.Core.Tests/Fixtures/Reports/{stage-d-inspector-latest.txt,chunk-bench-latest.txt,grapheme-telemetry.trx,stage-d-refresh-*.log}` | Stage D 证据集合：`rust_commit=3799d2be9db0ef040517ed69df1b717e96a8958e`、Chunk吞吐 235 MB/s、Grapheme telemetry 3/3 pass。 | 新日志/哈希或 `feature_gates` 发生变化时更新；缺任一文件即视为证据链断裂。 |

## 监控清单
| 监控对象 | 信号/检测手段 | 触发时动作 | 频率 |
|-----------|---------------|-------------|------|
| Goal Tree 模板 `docs/architecture/templates/goal-tree.yaml` | `sha256sum` 与 `python scripts/goal_tree_sync.py --check` 的 `PLAN/DRIFT` 输出 | 记录 hash + log，在本档案“证据快照”更新引用；若 `DRIFT` 涉及 Stage D，立即 ping 架构师。 | 每日或模板 PR 后 |
| `port-blueprint.md#[BP-GoalTree]` · `m3-implementation-plan.md#[MP-GoalTree]` | `<!-- goal-tree:meta -->` 的 `generated-at`、`sha256` 注释 | 与最新 `goal-tree-sync` log 不一致时，要求 Architecture Mapper rerun `--update` 并在两文之间对齐。 | 每次 Goal Tree 操作后 |
| Stage D manifest + inspector (`fixtures.manifest.json`, `Reports/stage-d-inspector-latest.txt`) | `rust_commit`、`feature_gates`、 ledger payload hash | 若任意 hash 变化或 `feature_gates` 缺少 tree trace，提醒 Rust Porter + QA 并记录 log 路径。 | 每次 Stage D refresh |
| Stage D orchestrator日志 (`Reports/stage-d-refresh-*.log`, `python scripts/refresh_all_assets.py --only stage-d-fixtures`) | 退出码、命令序列是否包含 exporter/loader/hydrator/verify/bench/telemetry | 如日志缺段或命令跳过默认 flags，参考 `docs/operations/do-check-stage-d.md` 打回执行人并补档。 | 每次脚本运行 |
| QA anchors (`docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::ParityAssets][QA-*]`) | Anchor 中的 hash 与 `stage-d-inspector`、`chunk-bench-latest.txt`、`grapheme-telemetry.trx` 是否一致 | 发现不一致时，把缺失 artefact/章节写入 `Need help`，并请求 QA 在下一次刷新时附日志。 | 每次 QA 更新或日志到期前 24h |
| 系统治理 (`docs/architecture/system-overview.md`, `docs/operations/do-check-stage-d.md`) | Header 中的 `Last Synced Goal Tree`，Checklist 勾选状态 | 若时间戳落后 Stage D 日志 >24h，提醒 Architecture Mapper 在下一次会议更新 meta。 | 每次会议准备阶段 |

## 当前关注（Current Focus）
- 维持 Goal Tree 模板 → `goal_tree_sync` log → `port-blueprint` / `m3-implementation-plan` 的一致性，当前最新版日志为 `tests/xi.Core.Tests/Fixtures/Reports/goal-tree-sync-20251118-170042.log`。
- 守护 Stage D 证据链：`fixtures.manifest.json`、`stage-d-inspector-latest.txt`、`chunk-bench-latest.txt`、`grapheme-telemetry.trx`、`stage-d-refresh-*.log`，确保 `[StageD::ParityAssets]` 与 `[QA-*]` 指向同一批 artefact。
- 追踪 `docs/operations/do-check-stage-d.md` checklist 与 `docs/architecture/system-overview.md` 的 `Last Synced Goal Tree` 字段，让下一次 `python scripts/refresh_all_assets.py --only stage-d-fixtures` 可以直接引用。

## 证据快照（2025-11-19）
- `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`：`rust_commit=3799d2be9db0ef040517ed69df1b717e96a8958e`，`cli_revision=0.3.0`，`feature_gates=[cursor_state, serde, tree_builder_slice_trace]`，共 10 个 ledger 文件。
- `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt`：Chunk=20 (`payload=69ba7f2536...`)，Cursor=12 (`9b46bd8e...`)，Grapheme=668 (`eb0c7c66...`)，Breaks/Diff/Search/Tree trace 均有 3 条记录，全部源自同一 manifest。
- `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`：Chunk throughput 235.12 MB/s，Line throughput 307.97 MB/s，Thread alloc 37,944 bytes，指向 `stage-d-inspector-latest.txt` 作为 sha 证据。
- `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx`：3/3 smoke tests通过，duration <50 ms，无 fallback 行为，采集时间 `2025-11-19T00:25:41+08:00`。
- `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-162433.log`：`python scripts/refresh_all_assets.py --only stage-d-fixtures --continue-on-error` 运行成功，包含 exporter、loader、hydrator、`python scripts/verify_fixture_manifest.py`、`dotnet test StageDDescriptor*` 全量输出。
- `tests/xi.Core.Tests/Fixtures/Reports/goal-tree-sync-20251118-170042.log`：记录 `docs/architecture/port-blueprint.md` 与 `docs/architecture/m3-implementation-plan.md` 已使用同一 Goal Tree 片段；下一次刷新需生成 2025-11-19+ 时间戳。

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
- **2025-11-19 – Stage D 证据索引排程**：将 Ready Queue #7 “Stage D evidence index refresh” 登记至 `docs/sprints/sptrint-1.md`，并在 `docs/meetings/2025-11-19-knowledge-refresh-chat.md#Information Researcher` 补写后续行动，提前锁定 `stage-d-refresh` / inspector / bench / telemetry artefact 的下一轮引用。
- **2025-11-19 – 知识刷新自检**：重写本档案的知识索引与监控清单，纳入 `docs/operations/do-check-stage-d.md`、`docs/architecture/system-overview.md`、`tests/xi.Core.Tests/Fixtures/Reports/*.log`，并在 `docs/meetings/2025-11-19-knowledge-refresh-chat.md#information-researcher` 记录输出范围 / 待协作事项。
- **2025-11-18 – Goal Alignment 证据回填**：对照 `docs/meetings/2025-11-18-goal-alignment-chat.md#2.6`、Goal Tree 模板、Stage D Playbook、`stage-d-inspector-latest.txt`，列出仍缺 `stage-d-refresh` / telemetry artefact 的章节并将 rerun 作为 A1/A2 验收条件。
- **2025-11-17 – 入职梳理**：完成初版索引与监控策略，复核 Rope skeleton 映射、Stage D CLI、`AGENTS.md` gating；为后续差异追踪奠定基线。

## 待办 / 风险
- [TODO] 跟踪 Ready Queue #7 “Stage D evidence index refresh”：待 QA/C#/Rust 交付最新 `stage-d-refresh-*.log`、`stage-d-inspector-latest.txt`、`chunk-bench-latest.txt`、`grapheme-telemetry.trx` 与 Goal Tree diff 后，刷新 `agents/information-researcher.md#证据快照`、`docs/operations/do-check-stage-d.md`、`docs/architecture/system-overview.md#[SO-Map]`，并让 `goal_tree_sync.py --check` 的 log id 与 `generated-at` 字段一致。
- [TODO] 追踪下一次 `python scripts/goal_tree_sync.py --check --update`，将 11/19 之后的 log 与 `goal-tree:meta` 注释写回蓝图/计划文档。
- [TODO] 请 QA Engineer 在下一次 `python scripts/refresh_all_assets.py --only stage-d-fixtures` 运行后附上新的 `stage-d-refresh-*.log`、chunk bench、telemetry，并同步 `[QA-*]` anchors。
- [RISK] Rust Porter 尚未交付包含 `metric_windows[]` / tree trace 扩展字段的 manifest；`[RPM-ParityAssets]` 与 `[TS-B5]` 需等待正式 schema 才能更新。
- [RISK] `docs/operations/do-check-stage-d.md` 的 `Last Synced Goal Tree` 若长期滞后，会导致 `system-overview` 与 Goal Tree meta 不一致；需 Architecture Mapper 在每次会议前确认。

