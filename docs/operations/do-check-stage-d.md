# Stage D 文档/资产 Do-Check 清单

> **Scope**: 当自动化（`python scripts/refresh_all_assets.py --only stage-d-fixtures`）无法覆盖全部 Stage D 场景时，本 checklist 确保 Goal Tree、Stage D Playbook 与 QA 证据保持同源。
> **Owner**: Architecture Mapper · QA Engineer
> **Update Frequency**: 每次 Stage D 刷新、Goal Tree 片段重建或 QA 烟雾执行之后立刻运行。
> **Reviewers**: AI Architect · Rust Porter · C# Implementer
> **Anchor Prefix**: DoCheck
> **Last Synced Goal Tree**: 2025-11-21（Stage D 自动化巡检）

---

执行步骤：若任一步失败或被跳过，需在 `AGENTS.md` 与相关 `agents/*.md` 档案登记阻塞、命令、日志路径，并在 PR 里引用此文档。

## 1. Goal Tree / Blueprint
- [ ] 运行 `python scripts/goal_tree_sync.py --check`（或当前等效脚本）；若提示过期，刷新并附上最新 `docs/architecture/goal-tree-sync-<timestamp>.log`。
- [ ] 打开 `docs/architecture/port-blueprint.md` 与 `docs/architecture/m3-implementation-plan.md`，确认 `<!-- goal-tree:meta ... -->` 的 `generated-at` 与 log 对应，且两个片段 hash 相同。
- [ ] 随机抽查 Goal Tree 行，确认 `qaAnchors`、`stageDAnchors` 均能跳转到 `docs/csharp-refactor/rope-serialization-fixture-playbook.md` 中现有锚点。

## 2. Stage D 资产 & Manifest
- [ ] 比对 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 中的 `rust_commit`、`cli_rev`、`feature_gates` 与 PowerShell/Python 日志；若差异存在，先 rerun exporter 再写档。
- [ ] 将 manifest `fixtures[]` 列表与 `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::ParityAssets]` 表逐条对齐（名称、schema、hash、count）。
- [ ] 确认 `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt` 记录最新 Breaks/Diff/Search ledger；缺失时 rerun `python scripts/refresh_all_assets.py --only stage-d-fixtures`。
- [ ] 检查 `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`：吞吐必须 >200 MB/s、`Allocation statistics` <5 MB。如未包含 alloc 行，手动运行 `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj --configuration Release -- --stage-d --include-alloc-stats --report ...`。
- [ ] 验证 `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-<timestamp>.log` 已生成且包含 loader/hydrator/manifest/bench/telemetry 命令输出。

## 3. QA 证据链
- [ ] `dotnet test Xi.Editor.sln --filter "StageDDescriptorLoaderTests|StageDDescriptorHydratorTests"` 已在最新 run 中通过；若脚本跳过，手工补跑并存档结果。
- [ ] `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 输出 “All N fixtures match …”；若使用 `--update`，需记录 diff 行。
- [ ] `dotnet test Xi.Editor.sln --filter Category=StageDTelemetry --logger "trx;LogFileName=tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx"` 完成并把 Grapheme fallback 指标写入 QA 档案。

## 4. 文档同步
- [ ] `docs/csharp-refactor/rope-serialization-fixture-playbook.md` 的 `[StageD::ParityAssets]`、`[QA-IngestionSmoke]`、`[QA-ChunkBench]`、`[QA-Telemetry]` 均引用本次 artefact（manifest、inspector、chunk bench、telemetry TRX、`stage-d-refresh-*.log`）。
- [ ] `docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]` 与 `docs/architecture/type-system-migration-log.md#[TS-B2]/[TS-B3]/[TS-B5]` 记录同一 rust commit/hash，且 Goal Tree 的 `[BP-GoalTree]`/`[MP-GoalTree]` 行引用了对应 `[QA-*]`/`[StageD::*]`。
- [ ] `AGENTS.md`、`agents/architecture-mapper.md`、`agents/qa-engineer.md`「最近完成」均补充命令、hash、日志位置。

## 5. 例外 / 缓解记录
- [ ] 说明阻塞根因（脚本缺失 / 平台限制 / CI 失败 / 角色缺席）。
- [ ] 指定临时替代命令或 artefact（截图、`sha256sum` 输出）。
- [ ] 标记修复 owner + ETA，并在 `[StageD::AutomationBacklog]` 或相应会议纪要登记。

## 6. 完成交付
- 在 PR 描述、`AGENTS.md` 与 QA 档案引用 `docs/operations/do-check-stage-d.md`，说明 “Do-Check completed on <date>” 并列出剩余风险。
- 将本清单的勾选结果复制/截图随同日志一起归档，供 Stage D 审计复查。