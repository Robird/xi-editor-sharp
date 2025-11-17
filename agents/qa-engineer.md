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
- **Stage D ingestion smoke `[QA-IngestionSmoke]`**：✅ 2025-11-17 本地完成 ingestion smoke；`python scripts/verify_fixture_manifest.py`（canonical JSON：`sort_keys=True`,`ensure_ascii=False`）取代手工 `sha256sum` 作为默认校验，必要时仍可逐条 hash 复核。`dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter Serialization` 通过；manifest 与 Playbook 哈希 `bd863f…/fe963d…/a2b840…` 保持一致。
- **1 MB chunk benchmark `[QA-ChunkBench]`**：2025-11-17 rerun `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj -c Release --no-build`，输出 `ChunkCount=1,049`, `MaxChunkLength=1,000`, `TotalUtf16Chars=1,048,625`, `LineCount=8,389`；Chunk 12.62 ms（≈79 MiB/s 名义 / ≈159 MiB/s UTF-16），Line 14.99 ms（≈67 / 133 MiB/s）。吞吐未达 >200 MB/s 且缺少 <5 MB alloc 诊断，已在 Playbook + `m3-implementation-plan.md §5.3` 记档并将 anchor 标记 ⚠️。
- **Grapheme fallback telemetry `[QA-Telemetry]`**：`GraphemeNavigatorSmokeTests` 已统计 fallback 命中率但未写 manifest。目标阈值 <=0.5%，若高于阈值需向 Architecture Mapper 报告并在 Stage D anchor 中登记。
- **Stage D Baseline**：`docs/csharp-refactor/rope-serialization-fixture-playbook.md` 已把 ingestion smoke、chunk bench、telemetry 的链路收口，等待 `[StageD::FixtureFlow]` manifest 线上化后同步 hash。

## 测试基线 / 工具链
| 项 | 命令 | 状态 | 备注 |
| --- | --- | --- | --- |
| 全量 xUnit | ```dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj -v m``` | ✅ 169/169（3.2s，2025-11-17） | 每日基线；记录输出并附带 `TRX` 至 `AGENTS.md` |
| 质量筛选 | ```dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj -v m --filter "CursorDescriptorParityTests|RopeChunkEnumeratorDiagnosticsTests|GraphemeNavigatorSmokeTests"``` | ✅ 2025-11-17 | 快速验证 Stage D ingest / Grapheme fallback / Chunk diag |
| Rust serde 子集 | ```cargo test -p xi-rope --features serde subset delta engine_serialization_regression``` | ✅ 2025-11-15 | 由 Rust Porter 供证；QA 参考日志即可 |
| `run_all_checks` | ```./run_all_checks --filter serde-fixtures``` | ✅ 2025-11-15 | 联动 `scripts/refresh_serialization_fixtures.ps1`；可在 PowerShell 用 `-Filter` 等价 |
| Bench diag | ```dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj -c Release --no-build``` | ⚠️ 2025-11-17：Chunk 12.62 ms / Line 14.99 ms（<200 MB/s） | 1 MB baseline 已采集；需调优 + 增加 <5 MB alloc 监控 |

## Stage D & QA Anchors
| Anchor | 目标 | 当前状态 | 关联文档 |
| --- | --- | --- | --- |
| `[QA-IngestionSmoke]` | `scripts/refresh_serialization_fixtures.ps1` 全链路 + manifest 比对 + ingestion smoke | ✅ 2025-11-17：`sha256sum tests/xi.Core.Tests/Fixtures/**/*.json` 与 `dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter Serialization` 通过；hash 需用 canonical JSON（`ensure_ascii=false`）重算以对齐 manifest | `docs/csharp-refactor/rope-serialization-fixture-playbook.md`, `StageD::FixtureFlow` |
| `[QA-ChunkBench]` | 1 MB chunk/line 架构基准，输出 `ChunkCount/LinesCount/MaxChunkLength/Duration` | ⚠️ 2025-11-17：1,049 chunks（max 1,000），Chunk 12.62 ms / Line 14.99 ms <200 MB/s，alloc telemetry 缺失 | `docs/csharp-refactor/rope-serialization-fixture-playbook.md`, `docs/architecture/m3-implementation-plan.md §5.3` |
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

### `scripts/verify_fixture_manifest.py`
- **用途**：对 `fixtures.manifest.json` 中的 `fixtures[].payload_hash` 执行 canonical JSON（`sort_keys=True`, `ensure_ascii=False`, `separators=(",", ":")`）哈希复核，自动标记缺失/漂移资产。
- **运行**：```python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json```
- **Checklist**：
	1. 默认在 Stage D 刷新后运行，或通过 `python scripts/refresh_all_assets.py --only verify-stage-d` 独立触发；
	2. 若脚本报错，优先排查路径/JSON 结构，再 fallback `sha256sum`；
	3. 未来将追加 `--update` 以重写 manifest（TODO）。

### QA Diagnostics / Benchmark 脚本
- **1 MB chunk bench**：```dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/Diagnostics.csproj --configuration Release -- --payloadMB 1 --export json```；记录 `ChunkCount`, `MaxChunkLengthBytes`, `ElapsedMs`。
- **Grapheme fallback dump**：```dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj -v m --filter GraphemeNavigatorSmokeTests --logger "trx;LogFileName=grapheme.trx"```；解析日志写入 `[QA-Telemetry]`。
- **Stage D ingestion smoke**：待 manifest 完成后，组合命令 `pwsh -File scripts/refresh_serialization_fixtures.ps1 -EmitManifestPath tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` + `dotnet test --filter CursorDescriptorParityTests`，必要时加 `./run_all_checks --filter serde-fixtures` 复核。

- **2025-11-17 - Stage D ingestion smoke（hash + Serialization）**：在仓库根运行 `sha256sum tests/xi.Core.Tests/Fixtures/**/*.json` 并使用 `python - <<'PY' ... sort_keys=True, ensure_ascii=False` 计算 canonical SHA256，确认 chunk/cursor/grapheme/delta/engine/subset/leaf_split 与 `fixtures.manifest.json` 及 `[StageD::ParityAssets]` 匹配；随后执行 `dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter Serialization`（10/10 通过，2.5s）。下一步：持续监控 `refresh_all_assets.py --only stage-d-fixtures` 输出，并在资产漂移时重跑。
- **2025-11-17 - Stage D manifest verifier automation**：新增 `scripts/verify_fixture_manifest.py`（canonical JSON SHA256，TODO `--update`），并把 `verify-stage-d` 步骤串入 `scripts/refresh_all_assets.py`（默认接在 `stage-d-fixtures` 之后）；更新 Playbook `[StageD::StageDChecklist]`/`[QA-IngestionSmoke]` 指南与本档案，明确脚本作为首选校验入口。
- **2025-11-17 - QA 档案重整**：本次任务，依架构模板重写档案结构，补 front-matter、监控面板、Anchors、脚本 checklist，并把 dotnet 基线（169/169）与筛选命令写入；同步 Stage D ingestion smoke / chunk bench / telemetry 状态，确保与 `docs/csharp-refactor/rope-serialization-fixture-playbook.md` 一致。
- **2025-11-17 - QA Playbook Anchor Expansion**：已在 Playbook 中注册 `[QA-ChunkBench]`、`[QA-Telemetry]`，存档阈值与指令。
- **2025-11-17 - QA 入职与资产确认**：完成 parity 夹具盘点、`scripts/refresh_serialization_fixtures.ps1` 选项确认，并记录 `dotnet test` 169/169 基线。
- **2025-11-17 - 1 MB chunk bench rerun**：`cd /repos/xi-editor-sharp && dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj -c Release --no-build`；得到 `ChunkCount=1,049`, `MaxChunkLength=1,000`, `TotalUtf16Chars=1,048,625`, `LineCount=8,389`，Chunk 12.62 ms（≈79 MiB/s 名义 / ≈159 MiB/s UTF-16）、Line 14.99 ms（≈67 / 133 MiB/s）。吞吐 <200 MB/s，alloc <5 MB 未被脚本记录；结果写回 `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[QA-ChunkBench]` 与 `docs/architecture/m3-implementation-plan.md §5.3` 并引入 `[MP-R10]` 跟踪。

## 待办 / 风险
- [ ] **Manifest & Hash 自动化落地**（高优先）：`[QA-IngestionSmoke]` 2025-11-17 已验证，但 `scripts/refresh_serialization_fixtures.ps1` 仍缺 `--emit-manifest`/canonical SHA 写回自动化；需 Rust Porter 完成脚本参数与 Architecture Mapper 对齐 anchors，避免下次 smoke 依赖手工 `python - <<'PY'` 校验。
- [ ] **1 MB Chunk Bench 调优**：2025-11-17 跑分已归档但 Chunk/Line ≈79/67 MiB/s（≈159/133 MiB/s UTF-16）低于 >200 MB/s，且缺少 alloc counter；需 Profiling + Telemetry 以关闭 `[MP-R10]`。
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
