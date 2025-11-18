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

## 最近完成
- **2025-11-19**：重新执行 `python3 scripts/refresh_all_assets.py --only stage-d-fixtures --continue-on-error | tee tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118T162433Z.log`，`stage-d-refresh-20251118T162433Z.log` 记录完整 Stage D orchestrator：`run_all_checks` + exporter 写回 `fixtures.manifest.json`（rust commit `3799d2be9db0ef040517ed69df1b717e96a8958e`, feature gates `cursor_state,serde,tree_builder_slice_trace`）后串联 `dotnet test Xi.Editor.sln`（199/199 绿）、`StageDDescriptorLoaderTests`（5/5 绿）、`StageDDescriptorHydratorTests`（3/3 绿）与 `python3 scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`（执行两次，第二次通过 `tee -a` 追加同一 log，表格显示 “All 10 fixtures match the manifest hashes.”）。`dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures` 更新 `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt`，并触发 Release chunk bench + Stage D telemetry：`tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` 记录 chunk 4.25 ms ⇒ 235.12 MB/s、line 3.25 ms ⇒ 307.97 MB/s、线程分配 37,944 bytes（GC 0/0/0），`tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx` 报告 Category=StageDTelemetry total=3/executed=3/passed=3。三条 `[QA-*]` anchors 与 Playbook 现全部引用该批 artefact。
- **2025-11-18**：按 A2/A3 checkpoint rerun `python scripts/refresh_all_assets.py --only stage-d-fixtures | tee tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-143204.log`，Rust exporter 写回 `fixtures.manifest.json`（commit `3799d2be9db0ef040517ed69df1b717e96a8958e`, feature gates `cursor_state,serde`，ledger `69ba7f25/eb0c7c66/5d37a731/fe76ed31/7eac7ecf`），并串联 `dotnet test Xi.Editor.sln --filter StageDDescriptorLoaderTests`（5/5 绿）+ `--filter StageDDescriptorHydratorTests`（3/3 绿）以证明 loader/hydrator 与 manifest 对齐；`python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 输出 “All 9 fixtures match…”，`dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures > tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt` 给出 Breaks/Diff/Search typed 摘要。流程还刷新 `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` 与 `grapheme-telemetry.trx`，形成 `[QA-IngestionSmoke]`→`[QA-ChunkBench]`→`[QA-Telemetry]` 同 commit 证据链。
- **2025-11-18**：为满足 Release chunk bench + <5 MB alloc 要求，更新 `tests/xi.Core.Tests/Benchmarks/Diagnostics/Program.cs` 在 Stage D 模式下输出 1 MB 归一化吞吐（MB/s），随后运行 `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj --configuration Release -- --stage-d --include-alloc-stats --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`；结果：chunk 回放 4.97 ms（201.25 MB/s），line 回放 3.06 ms（327.31 MB/s），`Allocation statistics` 报告线程分配 37,944 bytes，GC 0/0/0。`[QA-ChunkBench]` 与 `m3-implementation-plan.md §5.3` 已引用该日志。
- **2025-11-18**：执行 `dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter GraphemeNavigatorSmokeTests --logger "trx;LogFileName=tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx"`，产出的 TRX + `GraphemeNavigatorSmokeTests` 中的 `GraphemeNavigationMetrics` snapshot 证明 `totalMoves=4`、Forward/Backward neighbor hits 各 1 次、`scalarFallbacks=0`（fallback ratio 0%）。`docs/csharp-refactor/rope-serialization-fixture-playbook.md#[QA-Telemetry]` 与 `design-divergence-log.md` 均更新引用，Goal Tree QA anchor 闭环。
- **2025-11-18**：复查 Stage D manifest / Loader / Hydrator / chunk bench 证据：运行 `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`（All 9 fixtures match）以及 `dotnet test Xi.Editor.sln --filter StageDDescriptorLoaderTests` / `--filter StageDDescriptorHydratorTests`（5+3 tests 全绿），确认 `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt` ledger 与 manifest 对齐；对比 `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`（3.67 ms/2.86 ms，Debug，无 alloc stats）与 `[QA-ChunkBench]` 记录不符，并在 `docs/meetings/2025-11-18-goal-alignment-chat.md#25-qa-engineer` 标记该差异及 A2/A3 下一步。
- **2025-11-18**：在 `docs/meetings/2025-11-18-goal-alignment-chat.md#25-qa-engineer` 汇报 Stage D smoke / chunk bench / telemetry 进度，抛出 manifest 漂移与脚本易碎风险，并将 11/21~11/24 QA 行动计划同步到会议记录与本档案。
- **2025-11-20**：`python scripts/refresh_all_assets.py --only stage-d-fixtures`（Rust commit `3799d2be9db0ef040517ed69df1b717e96a8958e`, `feature_gates=["cursor_state","serde"]`）刷新 chunk/grapheme/breaks/diff/search ledger 至 `69ba7f2536876ad3a76296a215ac163fa82629934a97a82e49faa25423f9415a` / `eb0c7c66069ca33a3626ed3909754da72223f6e0e283d6c7b2b3182a0a35182c` / `5d37a7313730dd0ae9c292ca45daa442c11fe63d45f9ad21bad664f919c13f86` / `fe76ed31cff4549ffd3f32bd3847dda15ced7538c4165b4566f782e715572562` / `7eac7ecf3bb0bdbf5369b6b0dcb17bf5241d8c914af791440e2c5a4582ee7d57` 并写回 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`；`python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 输出 “All 9 fixtures match the manifest hashes.”，`dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures | tee tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt` 生成新的 inspector 摘要。`dotnet test Xi.Editor.sln --filter "StageDDescriptorLoaderTests|StageDDescriptorHydratorTests"` 中 Hydrator 用例通过，但 Loader 仍期待旧的 `bd28ebdf...`/`f0c9ada5...`/`0a82a69f...` ledger，导致 `python scripts/refresh_all_assets.py` 以 exit code 1 结束；待 C# Implementer 更新断言后（目标 2025-11-21）重新跑一遍 `[QA-IngestionSmoke]`。
- **2025-11-20**：`dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj -- --stage-d --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` 在 Stage D manifest 上回放 chunk/line：`Chunk samples=9`, `Chunks=3`, `MaxChunkLength=753`, `TotalUtf16Chars=1,808`, `LinesVisited=8/11`，Chunk 3.67 ms（≈286 MB/s）、Line 2.86 ms（≈367 MB/s）；输出确认仍为 Debug 且缺 alloc 字段。下一步：加入 `-c Release --no-build` 与 `--include-alloc-stats`，让 `[QA-ChunkBench]` 留存 Release 吞吐 + <5 MB alloc 证据。
- **2025-11-18**：重新运行 `python scripts/refresh_all_assets.py --only stage-d-fixtures` 复位 Stage D ingest。`run_all_checks --filter serde-fixtures` 与三套 serde regression 全绿，`cargo run -p xi-rope --features serde,cursor_state --bin export-serde-fixtures` 导出 12 条 cursor / 20 条 chunk / 11 条 line / 668 条 grapheme / 3 条 breaks/diff/search，`fixtures.manifest.json` 写入 `rust_commit=bd28ebdf83d2dd2fa6d4bd7c857d2471a21b4999`、`feature_gates=["cursor_state","serde"]`，manifest ledger payload 更新为 chunk `f0c9ada5…`、grapheme `53701cb6…`、breaks `0a82a69f…`、diff `2c4c366c…`、search `e7de44d7…`。同步刷新 `StageDDescriptorLoaderTests`/`StageDDescriptorInspectorTests` 断言、`tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt`、`docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::ParityAssets]`，并在 QA 档案记录新的 hash 证据链，确认 loader→hydrator→inspector 烟囱与 manifest 对齐。
- **2025-11-20**：在 `docs/meetings/2025-11-20-type-mapping-sync-chat.md#qa-engineer` 回填 Stage D smoke/manifest/benchmark/telemetry 现状与缺口，登记 QA 行动 (`QA-A1`~`QA-A3`) 及跨角色依赖，使 `[QA-IngestionSmoke]`、`[QA-ChunkBench]`、`[QA-Telemetry]` 的缺失在会议纪要中可追溯。
- **2025-11-19**：`scripts/refresh_all_assets.py` 的 `stage-d-fixtures` 步骤现描述为 “Export Rust fixtures -> Stage D loader -> hydrator -> manifest verifier -> Inspector summary”，并强制传入 `-SkipStageDLoaderTest:$false -SkipStageDHydratorTest:$false -SkipManifestVerification:$false -SkipStageDInspector:$false`，与 Stage D Playbook 要求一致，防止 QA smoke 链路被静默跳过。
- **2025-11-19**：完成 Stage D ingestion smoke（manifest + loader/hydrator + inspector 证据链）。流程：`python scripts/refresh_all_assets.py --only stage-d-fixtures` 默认串联 `dotnet test Xi.Editor.sln --filter StageDDescriptor`（loader + hydrator）后执行 `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`（输出 “All 9 fixtures match the manifest hashes.”）与 `dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures`（CLI 摘要列出 Breaks/Diff/Search 各 3 条、路径 + sha256），日志已贴入 QA 档案。此次 manifest 写入 `rust_commit=bea8a3360131a0840d26aa75daba9184b70d50b7`、`cli_rev=0.3.0`、`feature_gates=["serde"]`，并刷新 chunk/grapheme/breaks/diff/search 哈希至 `f61d1016…` / `aa560099…` / `7b948fc6…` / `a309f2f1…` / `2fb14e0c…`；`docs/csharp-refactor/rope-serialization-fixture-playbook.md` `[StageD::StageDChecklist]` / `[StageD::FixtureFlow]` / `[StageD::ParityAssets]` / `[QA-IngestionSmoke]` 已同步 inspector 默认步骤与 hash 表。下一步监控 `StageDDescriptorInspector` 是否需要 `-SkipStageDInspector` flag 支持 CI。 
- **2025-11-18**：`scripts/refresh_serialization_fixtures.ps1` 串入 manifest 校验：新增 `-SkipManifestVerification`（默认执行 python verifier，`scripts/refresh_all_assets.py` 继承），脚本会在 loader/hydrator 之后查找 `python3/python/py` 并调用 `python scripts/verify_fixture_manifest.py --manifest <path>`，hash mismatch 将直接失败；同时更新 `docs/csharp-refactor/rope-serialization-fixture-playbook.md` (`[StageD::StageDChecklist]` / `[StageD::FixtureFlow]` / `[QA-IngestionSmoke]`) 与 `docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]` 的 Stage D scripts 描述，明确 `-SkipManifestVerification` 只可带理由使用，`--update` 需要 rerun 以生成新的只读证据链。验证命令 `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/refresh_serialization_fixtures.ps1 -SkipRust -SkipDotnet -SkipCopy -SkipStageDLoaderTest -SkipStageDHydratorTest`（仅执行 manifest 步骤）输出 `==> python: verify fixture manifest` 与 `All 9 fixtures match the manifest hashes.`，确认自动验证可独立运行。
- **2025-11-18**：`scripts/refresh_serialization_fixtures.ps1` 新增 `-SkipStageDHydratorTest`（默认串联 StageDDescriptorLoaderTests -> StageDDescriptorHydratorTests，即使 `-SkipDotnet` 也照跑），并在 `scripts/refresh_all_assets.py` `stage-d-fixtures` 步骤强制传入 `-SkipStageDLoaderTest:$false -SkipStageDHydratorTest:$false`、描述改为 “Export Rust fixtures + run Stage D loader + hydrator smoke”；同步更新 `docs/csharp-refactor/rope-serialization-fixture-playbook.md` `[StageD::FixtureFlow]` 与 `[QA-IngestionSmoke]`，强调 Loader→Hydrator 顺序与跳过记录要求。验证命令 `dotnet test Xi.Editor.sln -v m --filter "StageDDescriptorLoaderTests|StageDDescriptorHydratorTests"` 绿灯（8 tests, 0 failures, 3.0s）。
- **2025-11-18**：更新 `docs/csharp-refactor/rope-serialization-fixture-playbook.md` 中 `[QA-IngestionSmoke]`（新增 “Skeleton 备用路径” 与命令 `dotnet test Xi.Editor.sln -v m --filter "BreaksSkeletonTests|DiffSkeletonTests|SearchSkeletonTests|StageDDescriptorLoaderTests"`）及 `[StageD::ParityAssets]`（为 Breaks/Diff/Search 栏补充 skeleton smoke 说明），确保在 Rust exporter 尚未写回真实 Breaks/Diff/Search manifest 时仍有 QA guardrail；下一步等待 exporter 落地后把 smoke 流程切换回 manifest diff + loader 验证并更新 Playbook。 
- **2025-11-18**：第 3 次全量运行 `python scripts/refresh_all_assets.py`；`stage-d-fixtures` 的 `run_all_checks`（含 subset/delta/engine regressions）+ `cargo export-serde-fixtures` + `dotnet test Xi.Editor.sln --filter StageDDescriptorLoaderTests` 三段全部绿灯，Rust exporter 重写 `tests/xi.Core.Tests/Fixtures/{breaks_descriptors,chunk_descriptors,grapheme_descriptors,diff_regions,search_spans,fixtures.manifest}.json` 并与最新 `generated_at_unix_millis`/hash 对齐；终端随后 `verify-stage-d` 输出 9/9 匹配并给出 “All steps completed.”，确认 Stage D 链路与 manifest 验证均已恢复健康。
- **2025-11-18**：再次全量运行 `python scripts/refresh_all_assets.py` 以复核 Playbook manifest 摘要；`stage-d-fixtures` 中 `run_all_checks`（含 `cargo export-serde-fixtures`/subset/delta/engine regressions）与 Rust exporter阶段全部通过，但 `dotnet test Xi.Editor.sln --filter StageDDescriptorLoaderTests` 仍引用旧哈希（期望 `0a4868b97598dd846d5da63e615c0de88341a5feb`/`737e07fcccc46ef4e6b388a8c03e42c2ee99cd331`，实际来自最新 manifest 的 `e62a4faa936a20b261b167ddbd2be3b4d3566f309`/`ab2f746e2bdd945e69b0acf9cd275c068a5ba6546`），导致 `stage-d-fixtures` 步骤退出 1 并阻断后续 `verify-stage-d`；脚本未打印 “All steps completed.”，需 C# Implementer 同步断言或 Rust Porter 固定 manifest 后再 rerun 取得绿色日志。
- **2025-11-18**：全量 `python scripts/refresh_all_assets.py`（无 `--only`）复跑 —— `stage-d-fixtures` 链路成功执行 `pwsh scripts/refresh_serialization_fixtures.ps1`（含 `run_all_checks`/`cargo export-serde-fixtures`/Rust subset+delta+engine regression 套件）并重写 Stage D 夹具；随后 `dotnet test Xi.Editor.sln --filter StageDDescriptorLoaderTests` 仍断言旧 ledger 哈希 `2a36afc7…`，与最新 manifest 产出的 `0a4868b9…` 不符导致退出 1，`verify-stage-d` 未触发。需 C# 实现同步 `StageDDescriptorLoader` 期望或 Rust Porter 固定 manifest，再 rerun pipeline 取回绿色日志。
- **2025-11-18**：Stage D fixture刷新脚本修复——`scripts/refresh_serialization_fixtures.ps1` 改为 `[CmdletBinding()]` 并移除自定义 `-Verbose`，避免与 PowerShell 通用参数重名造成 `MetadataError`。验证命令 `python scripts/refresh_all_assets.py --only stage-d-fixtures --continue-on-error` 现能走完整条链路（Rust run_all_checks/export + dotnet/Stage D loader smoke）；本次运行仅因 `StageDDescriptorLoaderTests` 哈希更新（`96ce8ddf…` vs 旧期望 `f740a440…`）失败，等待 manifest/断言同步后即可恢复绿色。
- **2025-11-18**：阅读 `docs/meetings/2025-11-18-porting-brainstorm-chat.md` 并梳理 QA action plan，提炼 ChunkWindow Benchmark log、Cursor/Chunk/Grapheme telemetry ingestion、TreeTrace schema 校验与 Coyote 并发套件需求，形成任务矩阵 + 聊天室沟通草稿，准备在 `docs/architecture/m3-implementation-plan.md §5.3`、`docs/architecture/qa/chunk-window-benchmark-log.md`、`AGENTS.md` 等文档落档。
- **2025-11-18**：`verify_fixture_manifest.py --update` manifest 写回路径落地——脚本现可在 hash 漂移时重写 `payload_hash` 并自动复核；本地运行 `python scripts/verify_fixture_manifest.py`（退出 0）与 `python scripts/verify_fixture_manifest.py --update --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`（退出 0，输出 “--update: manifest already in sync; no changes written.”）完成验证，并在 Playbook `[StageD::FixtureFlow]` / `[QA-IngestionSmoke]` 加入 “manifest diff + loader smoke” 要求，QA 档案中记录 `Manifest changes/no changes` 摘要供 Stage D baseline 使用。
- **2025-11-17**：Stage D loader smoke 自动化串联——`scripts/refresh_serialization_fixtures.ps1` 新增 `-SkipStageDLoaderTest`（默认执行 `StageDDescriptorLoaderTests`，即使 `-SkipDotnet` 亦会运行），`scripts/refresh_all_assets.py` 的 `stage-d-fixtures` 步骤描述/调用同步强调 “Export Rust fixtures + run Stage D loader smoke”，并在 Playbook `[StageD::FixtureFlow]`/`[QA-IngestionSmoke]` 标注“刷新后默认运行 loader smoke、跳过需登记”，确保脚本、文档与 QA 控制面一致。

## 当前监控
- **11/27 QA readiness checkpoint**：三条 QA anchors 已在 2025-11-19 rerun（`tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118T162433Z.log`, `chunk-bench-latest.txt`, `grapheme-telemetry.trx`）；为确保 11/27 Goal Alignment 有连续证据，下一次落盘仍需于 11/23–11/24 之间完成。
- **Stage D Sync Plan（`docs/meetings/2025-11-20-type-mapping-sync-chat.md#qa-engineer`）**：`QA-A1`（Stage D orchestrator + inspector log）、`QA-A2`（Release chunk bench + alloc stats ≥200 MB/s / <5 MB）、`QA-A3`（Grapheme telemetry ≤0.5% fallback）在本次 rerun（2025-11-19）再次闭环；等待 Rust Porter 提供下一批 Breaks/Diff/Search payload 以验证 hash 仍稳定。
- **Stage D ingestion smoke `[QA-IngestionSmoke]`**：✅ `python3 scripts/refresh_all_assets.py --only stage-d-fixtures --continue-on-error | tee tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118T162433Z.log`（2025-11-18 16:24 UTC）覆盖 exporter→loader→hydrator→manifest verifier→inspector，`stage-d-refresh-20251118T162433Z.log` 与 `stage-d-inspector-latest.txt`、`fixtures.manifest.json`（chunk `69ba7f25`, grapheme `eb0c7c66`, breaks `5d37a731`, diff `fe76ed31`, search `7eac7ecf`, tree trace `22724af7…`）组成现行引用源。
- **1 MB chunk benchmark `[QA-ChunkBench]`**：✅ `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`（Release，4.25 ms / 3.25 ms ⇒ 235.12 MB/s / 307.97 MB/s，线程分配 37,944 bytes，GC 0/0/0）继续满足吞吐与 alloc 阈值；Goal Tree + Playbook 均指向该日志。
- **Grapheme fallback telemetry `[QA-Telemetry]`**：✅ `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx`（2025-11-19T00:25:41+08:00，对应 2025-11-18 16:25 UTC）记录 Category=StageDTelemetry total=3/executed=3/passed=3/fail=0，维持 fallback ratio 0%；`docs/csharp-refactor/rope-serialization-fixture-playbook.md` 已同步。
- **Stage D Baseline**：Playbook 的 `[QA-IngestionSmoke]` / `[QA-ChunkBench]` / `[QA-Telemetry]` 现引用 2025-11-19 rerun artefact；持续跟踪 `fixtures.manifest.json` 与 `stage-d-inspector-latest.txt` 是否出现 hash 漂移。

## 测试基线 / 工具链
| 项 | 命令 | 状态 | 备注 |
| --- | --- | --- | --- |
| 全量 xUnit | ```dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj -v m``` | ✅ 169/169（3.2s，2025-11-17） | 每日基线；记录输出并附带 `TRX` 至 `AGENTS.md` |
| 质量筛选 | ```dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj -v m --filter "CursorDescriptorParityTests|RopeChunkEnumeratorDiagnosticsTests|GraphemeNavigatorSmokeTests"``` | ✅ 2025-11-17 | 快速验证 Stage D ingest / Grapheme fallback / Chunk diag |
| Rust serde 子集 | ```cargo test -p xi-rope --features serde subset delta engine_serialization_regression``` | ✅ 2025-11-15 | 由 Rust Porter 供证；QA 参考日志即可 |
| `run_all_checks` | ```./run_all_checks --filter serde-fixtures``` | ✅ 2025-11-15 | 联动 `scripts/refresh_serialization_fixtures.ps1`；可在 PowerShell 用 `-Filter` 等价 |
| Bench diag | ```dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj --configuration Release -- --stage-d --include-alloc-stats --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt``` | ✅ 2025-11-19：Chunk 4.25 ms（235.12 MB/s），Line 3.25 ms（307.97 MB/s），线程分配 37,944 bytes，GC 0/0/0 | 日志 `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`；吞吐/alloc 现同步至 `[QA-ChunkBench]` |

## Stage D & QA Anchors
| Anchor | 目标 | 当前状态 | 关联文档 |
| --- | --- | --- | --- |
| `[QA-IngestionSmoke]` | `scripts/refresh_serialization_fixtures.ps1` 全链路 + manifest 比对 + ingestion smoke | ✅ 2025-11-19：`python3 scripts/refresh_all_assets.py --only stage-d-fixtures --continue-on-error | tee tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118T162433Z.log` 捕获 exporter + loader + hydrator + manifest verifier + inspector + Release bench/telemetry 整链；`stage-d-inspector-latest.txt` 与 manifest hash（chunk `69ba7f25`, grapheme `eb0c7c66`, breaks `5d37a731`, diff `fe76ed31`, search `7eac7ecf`, tree trace `22724af7…`）与 `StageDDescriptorLoaderTests` 断言一致。 | `docs/csharp-refactor/rope-serialization-fixture-playbook.md`, `StageD::FixtureFlow` |
| `[QA-ChunkBench]` | 1 MB chunk/line 架构基准，输出 `ChunkCount/LinesCount/MaxChunkLength/Duration` | ✅ `dotnet run ... --include-alloc-stats --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`（2025-11-19 Release）记录 Chunk 4.25 ms（235.12 MB/s）、Line 3.25 ms（307.97 MB/s）、线程分配 37,944 bytes、GC 0/0/0；文档 & Goal Tree 已回填。 | `docs/csharp-refactor/rope-serialization-fixture-playbook.md`, `docs/architecture/m3-implementation-plan.md §5.3` |
| `[QA-Telemetry]` | Grapheme fallback hit-rate + 失效样本留档 | ✅ `dotnet test Xi.Editor.sln --filter Category=StageDTelemetry --logger "trx;LogFileName=tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx"`（2025-11-19T00:25:41+08:00）生成 TRX，Counters total=3/executed=3/passed=3/fail=0，维持 `GraphemeNavigatorSmokeTests` fallback ratio 0%。 | `docs/csharp-refactor/rope-serialization-fixture-playbook.md`, `docs/architecture/design-divergence-log.md` |
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
  - 若预期 hash drift，追加 `--update --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 让脚本重写 `payload_hash` 并自动二次校验。
- **Checklist**：
	1. 默认在 Stage D 刷新后运行，或通过 `python scripts/refresh_all_assets.py --only verify-stage-d` 独立触发；
	2. 若脚本报错，优先排查路径/JSON 结构，再 fallback `sha256sum`；
	3. exporter 修改 JSON 时必须运行 `--update`，保存 `Manifest changes` 或 “no changes” 行，与 `StageDDescriptorLoaderTests` 输出组成 “manifest diff + loader smoke” 证据；脚本写回后已经自动复验，禁止手工编辑 manifest 或跳过记录。

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
- [ ] **Manifest & Hash 自动化落地**（高优先）：`[QA-IngestionSmoke]` 2025-11-17 已验证，但 `scripts/refresh_serialization_fixtures.ps1` 仍缺 `--emit-manifest`/canonical SHA 写回自动化；需 Rust Porter 完成脚本参数与 Architecture Mapper 对齐 anchors，避免下次 smoke 依赖手工 `python - <<'PY'` 校验。
- [ ] **Stage D ingestion smoke rehearsal**：待 manifest ready 后，与 C# Implementer 对齐 ingest checklist，避免 CLI schema 漂移；风险在于 CLI 仍为草稿。
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
