---
role: QA Engineer
project: xi-editor-sharp
reports_to: AI 架构师
partners:
	- C# Implementer
	- Rust Porter
	- Architecture Mapper
	- Information Researcher
start_date: 2025-11-17
responsibilities:
	- 维护回归/benchmark 基线并对接 Stage D 验收
	- 消费/校验 Rust parity 夹具与 manifest
	- 监控 Grapheme/Chunk 遥测与 1 MB 基准
interfaces:
	- Rust Porter: parity fixtures / CLI schema / Stage D manifest
	- C# Implementer: dotnet tests, diagnostics, ingest scripts
	- Architecture Mapper: Stage D anchors、Goal Tree、风险登记
cadence:
	status_update: 任务完成 ≤24h 内刷新档案 + Goal Tree
	baseline_run: `dotnet test -v m` 每日班次结束前一次
	fixture_refresh: Stage D 请求或 manifest/hash 漂移时立刻重跑
---

## 当前监控
- **Stage D ingestion smoke `[QA-IngestionSmoke]`**：✅ 2025-11-17 本地完成 ingestion smoke；`sha256sum tests/xi.Core.Tests/Fixtures/**/*.json` + `dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter Serialization` 均通过。manifest/Playbook 哈希以 canonical JSON（`sort_keys=true`,`ensure_ascii=false`）计算，已用 `python - <<'PY' ...` 复核 `bd863f…/fe963d…/a2b840…`，与 `fixtures.manifest.json` 保持一致。
- **1 MB chunk benchmark `[QA-ChunkBench]`**：脚本位于 `tests/xi.Core.Tests/Benchmarks/Diagnostics/Program.cs`，参数集完成，尚缺最新跑分与 `ChunkCount/MaxChunkLength` 记录。预期阈值：吞吐 >200 MB/s、额外分配 <5 MB，结果需写回 `m3-implementation-plan.md §5.3` 与 `design-divergence-log.md`。
- **Grapheme fallback telemetry `[QA-Telemetry]`**：`GraphemeNavigatorSmokeTests` 已统计 fallback 命中率但未写 manifest。目标阈值 <=0.5%，若高于阈值需向 Architecture Mapper 报告并在 Stage D anchor 中登记。
- **Stage D Baseline**：`docs/csharp-refactor/rope-serialization-fixture-playbook.md` 已把 ingestion smoke、chunk bench、telemetry 的链路收口，等待 `[StageD::FixtureFlow]` manifest 线上化后同步 hash。

## 测试基线 / 工具链
| 项 | 命令 | 状态 | 备注 |
| --- | --- | --- | --- |
| 全量 xUnit | ```dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj -v m``` | ✅ 169/169（3.2s，2025-11-17） | 每日基线；记录输出并附带 `TRX` 至 `AGENTS.md` |
| 质量筛选 | ```dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj -v m --filter "CursorDescriptorParityTests|RopeChunkEnumeratorDiagnosticsTests|GraphemeNavigatorSmokeTests"``` | ✅ 2025-11-17 | 快速验证 Stage D ingest / Grapheme fallback / Chunk diag |
| Rust serde 子集 | ```cargo test -p xi-rope --features serde subset delta engine_serialization_regression``` | ✅ 2025-11-15 | 由 Rust Porter 供证；QA 参考日志即可 |
| `run_all_checks` | ```./run_all_checks --filter serde-fixtures``` | ✅ 2025-11-15 | 联动 `scripts/refresh_serialization_fixtures.ps1`；可在 PowerShell 用 `-Filter` 等价 |
| Bench diag | ```dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/Diagnostics.csproj --configuration Release``` | ⏳ 待跑 | 产出 1 MB chunk/line 指标，与 `[QA-ChunkBench]` 对齐 |

## Stage D & QA Anchors
| Anchor | 目标 | 当前状态 | 关联文档 |
| --- | --- | --- | --- |
| `[QA-IngestionSmoke]` | `scripts/refresh_serialization_fixtures.ps1` 全链路 + manifest 比对 + ingestion smoke | ✅ 2025-11-17：`sha256sum tests/xi.Core.Tests/Fixtures/**/*.json` 与 `dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter Serialization` 通过；hash 需用 canonical JSON（`ensure_ascii=false`）重算以对齐 manifest | `docs/csharp-refactor/rope-serialization-fixture-playbook.md`, `StageD::FixtureFlow` |
| `[QA-ChunkBench]` | 1 MB chunk/line 架构基准，输出 `ChunkCount/LinesCount/MaxChunkLength/Duration` | Pending rerun；脚本 ready，需记录结果和 pass 阈值 | `docs/csharp-refactor/rope-serialization-fixture-playbook.md`, `docs/architecture/m3-implementation-plan.md §5.3` |
| `[QA-Telemetry]` | Grapheme fallback hit-rate + 失效样本留档 | Instrumentation ready，缺最新 telemetry dump + manifest entry | `docs/csharp-refactor/rope-serialization-fixture-playbook.md`, `docs/architecture/design-divergence-log.md` |
| `[QA-StageDManual]` | Stage D Playbook QA Checklist & CLI flags | 模板锁定，待 manifest/hash 融入 `scripts/refresh_serialization_fixtures.ps1` | `docs/architecture/document-structure-template.md` |

## Automation & Scripts
### `scripts/refresh_serialization_fixtures.ps1`
- **用途**：Stage D orchestrator，串联 `cargo run -p xi-rope --features serde --bin export-serde-fixtures`, dotnet parity tests, manifest 写回。
- **默认参数**：`-ExportParityFixtures:$true -SkipRust:$false -SkipDotnet:$false -EmitManifestPath tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`（待实现）。
- **Checklist**：
	1. 确认 `rust` 子模块已同步目标 commit；
	2. 运行脚本并保留 stdout；
	3. 校验 `fixtures.manifest.json` 中 `rust_commit`、`cli_rev`、`feature_gates[]`；
	4. 使用 `jq -S` 或 `goal_tree_sync.py` 校验 `schema_hash`/`payload_hash`；
	5. 若 dotnet 套件失败，阻断 Stage D ingest 并回报。

### `scripts/refresh_all_assets.py`
- **用途**：批量刷新 `docs/architecture` front-matter + Stage D anchors，触发 hash lint。
- **运行**：```python scripts/refresh_all_assets.py --mode stage-d --check-anchors QA-IngestionSmoke QA-ChunkBench QA-Telemetry```
- **Checklist**：
	1. 运行前确保 `fixtures.manifest.json` 最新；
	2. 比对输出的 anchor lint；
	3. 若发现缺失 anchor，更新 Playbook/Goal Tree 后重跑；
	4. 结果写入 `AGENTS.md`。

### QA Diagnostics / Benchmark 脚本
- **1 MB chunk bench**：```dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/Diagnostics.csproj --configuration Release -- --payloadMB 1 --export json```；记录 `ChunkCount`, `MaxChunkLengthBytes`, `ElapsedMs`。
- **Grapheme fallback dump**：```dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj -v m --filter GraphemeNavigatorSmokeTests --logger "trx;LogFileName=grapheme.trx"```；解析日志写入 `[QA-Telemetry]`。
- **Stage D ingestion smoke**：待 manifest 完成后，组合命令 `pwsh -File scripts/refresh_serialization_fixtures.ps1 -EmitManifestPath tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` + `dotnet test --filter CursorDescriptorParityTests`，必要时加 `./run_all_checks --filter serde-fixtures` 复核。

## 最近完成
- **2025-11-17 - Stage D ingestion smoke（hash + Serialization）**：在仓库根运行 `sha256sum tests/xi.Core.Tests/Fixtures/**/*.json` 并使用 `python - <<'PY' ... sort_keys=True, ensure_ascii=False` 计算 canonical SHA256，确认 chunk/cursor/grapheme/delta/engine/subset/leaf_split 与 `fixtures.manifest.json` 及 `[StageD::ParityAssets]` 匹配；随后执行 `dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter Serialization`（10/10 通过，2.5s）。下一步：持续监控 `refresh_all_assets.py --only stage-d-fixtures` 输出，并在资产漂移时重跑。
- **2025-11-17 - QA 档案重整**：本次任务，依架构模板重写档案结构，补 front-matter、监控面板、Anchors、脚本 checklist，并把 dotnet 基线（169/169）与筛选命令写入；同步 Stage D ingestion smoke / chunk bench / telemetry 状态，确保与 `docs/csharp-refactor/rope-serialization-fixture-playbook.md` 一致。
- **2025-11-17 - QA Playbook Anchor Expansion**：已在 Playbook 中注册 `[QA-ChunkBench]`、`[QA-Telemetry]`，存档阈值与指令。
- **2025-11-17 - QA 入职与资产确认**：完成 parity 夹具盘点、`scripts/refresh_serialization_fixtures.ps1` 选项确认，并记录 `dotnet test` 169/169 基线。

## 待办 / 风险
- [ ] **Manifest & Hash 自动化落地**（高优先）：`[QA-IngestionSmoke]` 2025-11-17 已验证，但 `scripts/refresh_serialization_fixtures.ps1` 仍缺 `--emit-manifest`/canonical SHA 写回自动化；需 Rust Porter 完成脚本参数与 Architecture Mapper 对齐 anchors，避免下次 smoke 依赖手工 `python - <<'PY'` 校验。
- [ ] **1 MB Chunk Bench rerun**：需在 11/21 前跑完并把结果写入 Playbook + `m3-implementation-plan.md §5.3`；若未完成，`[MP-R10]` 将升高。
- [ ] **Grapheme fallback Telemetry dump**：需产出最新 hit-rate 与失效样本，联动 `design-divergence-log.md`；若 >0.5%，立即向架构师报警。
- [ ] **Stage D ingestion smoke rehearsal**：待 manifest ready 后，与 C# Implementer 对齐 ingest checklist，避免 CLI schema 漂移；风险在于 CLI 仍为草稿。

## 文档索引
- `docs/csharp-refactor/rope-serialization-fixture-playbook.md`：Stage D QA 手册，含 `[QA-IngestionSmoke]` / `[QA-ChunkBench]` / `[QA-Telemetry]` 指令。
- `docs/architecture/m3-implementation-plan.md`：指标 G1/G2、风险 R8/R9/R10 与 chunk/telemetry 记录位置。
- `docs/architecture/design-divergence-log.md`：记录遥测异常、chunk bench 结果及降级策略。
- `docs/architecture/document-structure-template.md`：Stage D/QA anchor 格式，配合 `scripts/refresh_all_assets.py` lint。
- `docs/architecture/fixtures/parity-fixture-schema.md`：manifest/schema hash 规范。
- `tests/xi.Core.Tests/` + `tests/xi.Core.Tests/Benchmarks/Diagnostics/`：基线测试与 benchmark 脚本所在。
- `scripts/refresh_serialization_fixtures.ps1`, `scripts/refresh_all_assets.py`, `goal_tree_sync.py`：自动化脚本链路。

---

> ⚠️ 除非架构师要求，不要另建 markdown 文档；所有状态写入本档案与 Playbook/Goal Tree/Stage D 文档并保持锚点对齐。
