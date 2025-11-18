# Rope 序列化夹具维护手册 (Stage D)

> **Scope**: Describe and enforce the Stage D pipeline that keeps Rust/C# fixture assets in sync.
> **Owner**: Rust Porter · QA Engineer
> **Update Frequency**: After every `export-serde-fixtures` change or fixture refresh.
> **Reviewers**: AI Architect · Architecture Mapper · C# Implementer
> **Anchor Prefix**: StageD
> **Last Synced Goal Tree**: 2025-11-19

---

## [StageD::StageDChecklist] Stage D 操作清单
<a id="StageD::StageDChecklist"></a>

1. **定位仓库根目录**：设置 `XI_EDITOR_SHARP_ROOT`（PowerShell `Set-Variable` 或 Bash `export`），并确保 `xi-editor-ph7` 指向 `feature/generic-node-refactor-experiment`。
2. **验证 Rust 基线**：在 `xi-editor-ph7/rust` 运行 `./run_all_checks`（Bash）或 `./run_all_checks.ps1 -Filter serde-fixtures`（PowerShell），随后执行三个 `cargo test -p xi-rope --features serde <regression>` 用例，确保 `serde_fixtures` 常量与测试同步。
3. **刷新夹具**：优先使用 `scripts/refresh_serialization_fixtures.ps1` 触发 exporter（默认启用 `-ExportParityFixtures`），如需调试可改用手动命令（见 `[StageD::FixtureFlow]`）。若希望与 goal tree / skeleton 流水线一键执行，可运行 `python scripts/refresh_all_assets.py`（默认包含 `stage-d-fixtures` 步骤），或使用 `python scripts/refresh_all_assets.py --only stage-d-fixtures` 单独执行 Stage D；该步骤会在 loader/hydrator/manifest verifier 结束后自动运行 `StageDDescriptorInspector`，以 Release + `--include-alloc-stats` 配置回放 `RopeChunkEnumeratorBenchmarks --stage-d`，并执行 Grapheme telemetry 烟雾（`dotnet test --filter Category=StageDTelemetry`）。流程完成后应看到 `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-<timestamp>.log`、`stage-d-inspector-latest.txt`、`chunk-bench-latest.txt`（包含 Allocation statistics 段）以及 `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx`。
4. **快速 diff**：运行 `git status --short tests/xi.Core.Tests/Fixtures` 与定向 `git diff`，确认变化仅限目标 JSON；若结构调整，需先在 Rust helper/文档更新 schema。
5. **校验 manifest + loader/hydrator/inspector 证据链**：`scripts/refresh_serialization_fixtures.ps1` 在 `StageDDescriptorLoaderTests -> StageDDescriptorHydratorTests` 通过后，会将 `python scripts/verify_fixture_manifest.py --manifest <path>` 与 `dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures` 作为默认尾段，记录 canonical ledger hash 以及 Breaks/Diff/Search typed 摘要。Loader + Hydrator + Inspector 现被视为 `[QA-IngestionSmoke]` 的不可分割 smoke 证据链，只有在 `-DryRun`、`-SkipManifestVerification` 或 `-SkipStageDInspector`（新增跳过标记，必须在 `agents/qa-engineer.md` / `AGENTS.md` 记事并补跑）时才允许省略任一步。若需要写回 `payload_hash`，先运行 `python scripts/verify_fixture_manifest.py --update --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`，然后再次执行脚本（禁止附带跳过参数）以产出新的 loader/hydrator/inspector + CLI 摘要日志；疑难排查仍可 fallback 到 `sha256sum tests/xi.Core.Tests/Fixtures/**/*.json`。流程通过后执行 `dotnet test Xi.Editor.sln` 与 `cargo test -p xi-rope --no-default-features` 并将结果链接至 `[QA-IngestionSmoke]`。
6. **文档与日志**：在 `AGENTS.md`、`agents/rust-porter.md`、`agents/qa-engineer.md`、`docs/architecture/rope-cs-mirror-plan.md` 等文件记录操作；若 CLI/流程有变化，更新 `[Fixture-*]` 文档与本手册对应章节。若自动化无法覆盖某一步骤，立刻执行 [`docs/operations/do-check-stage-d.md`](../operations/do-check-stage-d.md) 并在档案中附上 Do-Check 结果。

---

## [StageD::FixtureFlow] Fixture 刷新流程
<a id="StageD::FixtureFlow"></a>

> 推荐脚本：`scripts/refresh_serialization_fixtures.ps1 -Verbose`。此脚本会顺序执行 Rust 校验（可用 `-SkipRust` 跳过）、调用 exporter、重跑 `dotnet test`（可用 `-SkipDotnet` 跳过），并在日志中写入实际命令。刷新结束后它会自动串联 `StageDDescriptorLoaderTests -> StageDDescriptorHydratorTests`（Stage D loader + hydrator smoke），即使指定 `-SkipDotnet` 也会运行，除非显式传入 `-SkipStageDLoaderTest` 或 `-SkipStageDHydratorTest`。脚本尾段对 `[QA-IngestionSmoke]` 而言是固定模板：`python scripts/verify_fixture_manifest.py --manifest <path>` + `dotnet run --project tools/StageDDescriptorInspector -- --fixtures <dir>`（默认路径 `tests/xi.Core.Tests/Fixtures`）+ `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics --configuration Release -- --stage-d --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`。三者依次提供 canonical hash、Breaks/Diff/Search typed 摘要、Stage D chunk/line 吞吐数据；仅在传入 `-SkipManifestVerification`、`-SkipStageDInspector` 或 `-SkipStageDChunkBench` 时才会跳过，对应理由必须写入 QA 档案。Linux/WSL 可直接调用 Bash 版本流程；若需要与 goal tree / skeleton 流程串行执行，可使用 `python scripts/refresh_all_assets.py` 让 `stage-d-fixtures`（description: “Export Rust fixtures + run Stage D loader + hydrator smoke + verify manifest + inspector summary + Stage D chunk bench (Release + alloc stats) + Stage D telemetry capture + stage-d refresh log”) 自动串入。

> **Manifest diff + loader + hydrator + inspector 证据链**：刷新或手动导出后，PowerShell 脚本会自动执行 `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 并把 “Manifest changes/no changes” 行写入日志，紧接着运行 `dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures` 输出 Breaks/Diff/Search typed 摘要，再执行 Release `RopeChunkEnumeratorBenchmarks --stage-d --include-alloc-stats` 与 `dotnet test Xi.Editor.sln --filter Category=StageDTelemetry`，最后把标准输出写入 `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-<timestamp>.log`。任何 `-SkipManifestVerification`、`-SkipStageDInspector`、`-SkipStageDChunkBench` 或 `-SkipStageDTelemetry`（见下文新增参数）都必须在 QA 档案记录，并在同一任务内补跑以修复证据链。若预计存在 hash 漂移，需要单独运行 `python scripts/verify_fixture_manifest.py --update --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`（脚本默认只做只读验证），写回后再 rerun `scripts/refresh_serialization_fixtures.ps1`（勿携带跳过参数）以取得新的 manifest diff + loader + hydrator + inspector/bench/telemetry/log 日志。该日志需与 `StageDDescriptorLoaderTests`、`StageDDescriptorHydratorTests` 的通过记录一并归档，禁止拆分；若自动化无法补跑，参考 [`docs/operations/do-check-stage-d.md`](../operations/do-check-stage-d.md) 手工记录。

> **最新 manifest（2025-11-21 QA run）**：`python scripts/refresh_all_assets.py --only stage-d-fixtures` 在 rust submodule `3799d2be9db0ef040517ed69df1b717e96a8958e` 上重写 `fixtures.manifest.json`，`cli_rev=0.3.0`、`feature_gates=["cursor_state","serde"]`，descriptor 计数保持 `chunk/cursor/grapheme = 20/12/668` 与 `breaks/diff/search = 3/3/3`。脚本自动运行 `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`（输出 “All 9 fixtures match the manifest hashes.”）、`dotnet test Xi.Editor.sln --filter "StageDDescriptorLoaderTests|StageDDescriptorHydratorTests"`（5+3 用例全部通过）以及 `dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures | tee tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt`，捕获最新 Breaks/Diff/Search ledger（chunk `69ba7f2536876ad3a76296a215ac163fa82629934a97a82e49faa25423f9415a`, grapheme `eb0c7c66069ca33a3626ed3909754da72223f6e0e283d6c7b2b3182a0a35182c`, breaks `5d37a7313730dd0ae9c292ca45daa442c11fe63d45f9ad21bad664f919c13f86`, diff `fe76ed31cff4549ffd3f32bd3847dda15ced7538c4165b4566f782e715572562`, search `7eac7ecf3bb0bdbf5369b6b0dcb17bf5241d8c914af791440e2c5a4582ee7d57`)。同一 `stage-d-fixtures` 步骤也在 Release 配置下执行 `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics --configuration Release -- --stage-d --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` 并生成 chunk/line 吞吐统计（chunk 回放 4.26 ms，line 回放 2.94 ms，并在 `Allocation statistics` 段记录线程分配 37,784 bytes / GC 0/0/0），同时记录 `dotnet test Xi.Editor.sln --filter Category=StageDTelemetry` 输出（保存为 `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx`）以及完整的 `stage-d-refresh-<timestamp>.log`。所有 artefact 已写入 `tests/xi.Core.Tests/Fixtures/Reports/` 目录，供 Goal Tree 与 `[QA-ChunkBench]`/`[QA-Telemetry]` 对账。

> **Descriptor hydrator**：`src/xi.Core/Rope/Diagnostics/Descriptors/StageDDescriptorHydrator.cs` 现是 Breaks/Diff/Search ingestion 的唯一入口。脚本需在 loader 校验通过后调用 `StageDDescriptorHydrator.LoadBreakPlans/LoadDiffRegions/LoadSearchSpans`，这些 API 会复用 `StageDDescriptorLoader.LoadFromFixtureDirectory` 与 manifest ledger，避免 QA/CLI 重复解析 JSON。任何对 `--breaks-descriptors`、`--diff-regions`、`--search-spans` flag 或 manifest 结构的调整，都要同步更新 `[StageD::FixtureFlow]`、`[StageD::ParityAssets]` 与 `AGENTS.md` 的 Stage D 记录。

### 1. 环境变量与分支

```powershell
$env:XI_EDITOR_SHARP_ROOT = 'C:\src\xi-editor-sharp'
git -C "$env:XI_EDITOR_SHARP_ROOT/xi-editor-ph7" checkout feature/generic-node-refactor-experiment
git -C "$env:XI_EDITOR_SHARP_ROOT/xi-editor-ph7" pull --ff-only
```

> Bash/WSL：`export XI_EDITOR_SHARP_ROOT=$PWD`；`git rev-parse --show-toplevel` 可快速校验路径。

### 2. Rust 校验（可由脚本代劳）

```powershell
Set-Location "$env:XI_EDITOR_SHARP_ROOT/xi-editor-ph7/rust"
./run_all_checks.ps1 -Filter serde-fixtures
cargo test -p xi-rope --features serde subset_serialization_regression -- --nocapture
cargo test -p xi-rope --features serde delta_serialization_regression -- --nocapture
cargo test -p xi-rope --features serde engine_serialization_regression -- --nocapture
Set-Location "$env:XI_EDITOR_SHARP_ROOT"
```

> Bash 直接调用 `./run_all_checks --filter serde-fixtures`。三个 regression 用例共享 `rope/src/serde_fixtures.rs` 常量，是 Stage D 的唯一事实来源。

### 3. Exporter 命令

**批量脚本（推荐）**

```powershell
./scripts/refresh_serialization_fixtures.ps1 -Verbose `
  -ExportParityFixtures:$true `
  -ExtraCargoFeatures "serde"        # 追加 `cursor_state` 或 `tree_builder_slice_trace` 以收集诊断
  -ManifestPath "tests/xi.Core.Tests/Fixtures/fixtures.manifest.json"
```

> Manifest：脚本默认把 `--emit-manifest <path>` 传给 exporter，并将路径记入日志；不传 `-ManifestPath` 时会写入 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`。刷新后应在 `[Fixture-Manifest]` 所述位置看到新的 `rust_commit` 与哈希。

**手动命令（Windows PowerShell）**

```powershell
Set-Location "$env:XI_EDITOR_SHARP_ROOT/xi-editor-ph7/rust"
cargo run -p xi-rope --features serde --bin export-serde-fixtures -- `
  --dir tests/xi.Core.Tests/Fixtures `
  --cursor-descriptors tests/xi.Core.Tests/Fixtures/cursor_descriptors `
  --chunk-descriptors tests/xi.Core.Tests/Fixtures/chunk_descriptors `
  --grapheme-descriptors tests/xi.Core.Tests/Fixtures/grapheme_descriptors `
  --breaks-descriptors tests/xi.Core.Tests/Fixtures/breaks_descriptors `
  --diff-regions tests/xi.Core.Tests/Fixtures/diff_regions `
  --search-spans tests/xi.Core.Tests/Fixtures/search_spans `
  --emit-manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json
Set-Location "$env:XI_EDITOR_SHARP_ROOT"
```

**手动命令（Bash/WSL）**

```bash
cd "$XI_EDITOR_SHARP_ROOT/xi-editor-ph7/rust"
cargo run -p xi-rope --features serde,cursor_state,tree_builder_slice_trace --bin export-serde-fixtures \
  --dir tests/xi.Core.Tests/Fixtures \
  --cursor-descriptors tests/xi.Core.Tests/Fixtures/cursor_descriptors \
  --chunk-descriptors tests/xi.Core.Tests/Fixtures/chunk_descriptors \
  --grapheme-descriptors tests/xi.Core.Tests/Fixtures/grapheme_descriptors \
  --breaks-descriptors tests/xi.Core.Tests/Fixtures/breaks_descriptors \
  --diff-regions tests/xi.Core.Tests/Fixtures/diff_regions \
  --search-spans tests/xi.Core.Tests/Fixtures/search_spans \
  --tree-builder-trace tests/xi.Core.Tests/Fixtures/tree_builder_slice \
  --emit-manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json
cd "$XI_EDITOR_SHARP_ROOT"
```

> 调试模式：若需要隔离 `cursor_state` 或 tree trace，可用 `--features serde`（或 `-SkipTreeTrace`）临时关闭默认资产，再在日志中标注原因；否则请维持脚本/命令的 `serde,cursor_state,tree_builder_slice_trace` 组合与 `--tree-builder-trace tests/xi.Core.Tests/Fixtures/tree_builder_slice`。无论启用/禁用何种特性，都要在 `[Fixture-FeatureGates]` 与 `[StageD::FeatureGates]` 记录原因，并确认 `fixtures.manifest.json` 中的 `feature_gates[]` 与实际命令一致。

### 3.2 Breaks/Diff/Search Flag 规范

| Asset | Flag | 输出目录 | 默认文件 | Schema Id | Feature Gate |
| --- | --- | --- | --- | --- | --- |
| Breaks descriptors | `--breaks-descriptors <dir>` | `tests/xi.Core.Tests/Fixtures/breaks_descriptors` | `breaks_descriptors.json` | `breaks_descriptors@1.0.0` | `serde` + `breaks_diagnostics`（计划） |
| Diff regions | `--diff-regions <dir>` | `tests/xi.Core.Tests/Fixtures/diff_regions` | `diff_regions.json` | `diff_regions@1.0.0` | `serde` + `diff_regions`（计划） |
| Search spans | `--search-spans <dir>` | `tests/xi.Core.Tests/Fixtures/search_spans` | `search_spans.json` | `search_spans@1.0.0` | `serde` + `search_traces`（计划） |

- **Rust CLI 行为**：每个 flag 会生成一份汇总 JSON 并写入 manifest（`fixtures[].name` 与默认文件同名）。字段定义见 `docs/architecture/fixtures/parity-fixture-schema.md` 相应章节，依托 `docs/rust-refactor/breaks-metrics-templating.md`、`iterator-facade-export.md`、`rope/src/{breaks,diff,find}.rs` 中的结构。
- **PowerShell**：`scripts/refresh_serialization_fixtures.ps1` 将新增 `-ExportBreaksDiffSearch`（占位）开关，把三条路径传给 exporter；在脚本落地前，可使用 `-ExtraCargoArgs "--breaks-descriptors ... --diff-regions ... --search-spans ..."` 手动透传 flag，并在日志中标注 “Breaks/Diff/Search spec rehearsal”。
- **`refresh_all_assets.py`**：计划在 `stage-d-fixtures` 阶段追加 `breaks/diff/search` 子步骤或新增 `stage-d-breaks` 任务。实施前，可在运行 `python scripts/refresh_all_assets.py --only stage-d-fixtures` 后立刻执行上面的 `cargo run` 命令，确保 hash 仍由同一 manifest 记录。
- **Manifest 占位**：允许提前写入如下条目（`payload_hash="pending"`, `count=0`）以便 Goal Tree/QA 跟踪，待 Rust CLI 输出真实 hash 后再通过 `scripts/verify_fixture_manifest.py --update` 落实：

```jsonc
// status: pending — waiting for exporter implementation
{
  "name": "breaks_descriptors.json",
  "path": "tests/xi.Core.Tests/Fixtures/breaks_descriptors/breaks_descriptors.json",
  "count": 0,
  "schema_hash": "breaks_descriptors@1.0.0",
  "payload_hash": "pending"
}
```

> 同样的占位 JSON 适用于 `diff_regions.json` 与 `search_spans.json`，仅在 CLI 未落地前存在；实现完成后请删除 “pending” 注释并记录真实 hash。

### 3.1 Manifest 校验与写回

1. **默认校验**：刷新脚本现已自动运行 `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 并打印摘要表格；若需单独调试，可直接调用同一命令，输出仍需保存。
2. **更新模式**：若 canonical JSON drift 属于预期，增加 `--update` 参数；脚本会重写相应 `payload_hash` 值、打印逐条 `old -> new` 摘要，并在写入后自动再跑一次校验。
3. **记录要求**：将 `Manifest changes` 或 `--update: manifest already in sync; no changes written.` 行复制到 `agents/qa-engineer.md`、`AGENTS.md`、PR 描述，作为哈希证据。禁止绕过脚本手工修改 manifest。
4. **与 loader/hydrator smoke 绑定**：`--update` 完成后必须再次运行（或确认脚本自动触发的）`StageDDescriptorLoaderTests` 与 `StageDDescriptorHydratorTests`，输出 + manifest diff 才能满足 `[QA-IngestionSmoke]` 的证明链。

### 4. 使用 StageDDescriptorLoader 校验 manifest

`src/xi.Core/Rope/Diagnostics/Descriptors/StageDDescriptorLoader.cs` 是唯一的 ingestion 入口：它会读取 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`、`chunk_descriptors/chunk_descriptors.json`、`grapheme_descriptors/grapheme_descriptors.json` 并返回 `StageDDescriptorManifest`（含 manifest ledger：每个 `fixtures[].name` 的 `count/schema_hash/payload_hash`）。`scripts/refresh_serialization_fixtures.ps1` 会在流程末尾自动执行 `StageDDescriptorLoaderTests`；仅在需要隔离 exporter 问题或调试 dotnet 环境时，才使用 `-SkipStageDLoaderTest`。`StageDDescriptorHydratorTests` 则负责验证 Breaks/Diff/Search 描述符能通过 hydrator API 正确比对 manifest ledger，默认同一脚本中与 loader smoke 串联运行，仅在传入 `-SkipStageDHydratorTest` 时才会跳过。手动复核命令如下：

```bash
dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter StageDDescriptorLoaderTests
```

```bash
dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter StageDDescriptorHydratorTests
```

> Loader smoke 会验证 manifest 中的 `rust_commit`, `cli_rev`, `feature_gates[]`、payload 计数，以及 `fixtures[].count/schema_hash/payload_hash` 是否与 JSON 内容一致（参考 `tests/xi.Core.Tests/Diagnostics/StageDDescriptorLoaderTests.cs`）。Hydrator smoke 覆盖 `StageDDescriptorHydrator.LoadBreakPlans/LoadDiffRegions/LoadSearchSpans` 的 ingest 链路，确保 Breaks/Diff/Search skeleton 与 manifest ledger 同步（参考 `tests/xi.Core.Tests/Diagnostics/StageDDescriptorHydratorTests.cs`）。如需在 QA 工具或 CLI 中手动调用，可执行 `StageDDescriptorLoader.LoadFromFixtureDirectory("tests/xi.Core.Tests/Fixtures")` 并缓存返回的 `StageDDescriptorManifest` 注入 hydrator。若出于调试目的跳过任一 smoke，QA 需在 `agents/qa-engineer.md` 与 `AGENTS.md` 记录 `-SkipStageDLoaderTest` 或 `-SkipStageDHydratorTest` 的使用原因。

### 5. 差异审计与格式化

```powershell
git status --short tests/xi.Core.Tests/Fixtures
git diff tests/xi.Core.Tests/Fixtures/*.json
```

- 核对结构字段（`els`, `revs`, `metadata.schema_version` 等）顺序是否与 Rust 输出一致。
- 数值字段（`pos`, `len`, `priority`、`descriptor_count` 等）若出现大幅波动，需要回到 Rust helper 查证。
- 空数组/可选字段应遵循 serde `skip_serializing_if` 约定，若 diff 中出现 `null` 字段，说明 Rust 端需要修复。

### 6. 文档同步

刷新完成后务必更新：

- `docs/architecture/rope-cs-mirror-plan.md`（Stage D 章节）。
- `docs/architecture/fixtures/parity-fixture-schema.md`（若 schema/feature gate 改动）。
- `docs/architecture/rope-port-mapping.md`（新增 helper/资产时标注 `[RPM-ParityAssets]`）。
- `AGENTS.md` 与相关 `agents/*.md`（记录操作、结果和下一步）。

---

## [StageD::ParityAssets] Parity 资产总览
<a id="StageD::ParityAssets"></a>

> `scripts/refresh_serialization_fixtures.ps1 -ExportParityFixtures` 现默认传入 `--breaks-descriptors --diff-regions --search-spans --tree-builder-trace` 并启用 `tree_builder_slice_trace` feature；若仅需调试部分资产，可借助 `-SkipBreaksDiffSearch`（跳过 Breaks/Diff/Search）或 `-SkipTreeTrace`（跳过 Tree trace）临时关闭，但必须在 QA 档案与 `docs/operations/do-check-stage-d.md` 记录理由。

| Asset | Path | Export Flag | Schema / Version | SHA256（2025-11-21 刷新） | 备注 |
| --- | --- | --- | --- | --- | --- |
| Subset Regression | `tests/xi.Core.Tests/Fixtures/subset_regression.json` | `--dir` 默认覆盖 | `serde_fixtures::subset` | `28fa3c807f83961f6cdf9695f3605bdf22972f734e2470adae1234c85deb138f` | Stage A（Subset）黄金串，回归测试直接消费；hash 与 2025-11-18 manifest 一致。 |
| Delta Regression | `tests/xi.Core.Tests/Fixtures/delta_regression.json` | `--dir` 默认覆盖 | `serde_fixtures::delta` | `59c45336bace174a9ab50cefdef05bdd2890d4d93a376df4e6897ca71a20cf7f` | Stage B（Delta）黄金串；manifest 确认 `count=1`。 |
| Engine Regression | `tests/xi.Core.Tests/Fixtures/engine_regression.json` | `--dir` 默认覆盖 | `serde_fixtures::engine` | `8707d5de24e9369bf3a818a40030626118da2752ee1bdb54363cfb3e1ffa1cc2` | Stage C（Engine）黄金串；`python scripts/refresh_all_assets.py --only stage-d-fixtures` 2025-11-18 运行后保持稳定。 |
| Cursor Descriptors | `tests/xi.Core.Tests/Fixtures/cursor_descriptors/cursor_descriptors.json` | `--cursor-descriptors` | `cursor_descriptors@1.2.0` | `9b46bd8e29042e38c36556afc4054a5a2c4a6cfa405b5bbd738ca1909f8a4d38` | `[MP-T1]` NodeCursor parity；manifest 记录 `count=12`，`feature_gates` 含 `cursor_state`。 |
| Chunk Descriptors | `tests/xi.Core.Tests/Fixtures/chunk_descriptors/chunk_descriptors.json` | `--chunk-descriptors` | `chunk_descriptors@1.0.0` | `69ba7f2536876ad3a76296a215ac163fa82629934a97a82e49faa25423f9415a` | `[MP-T3]` Chunk/Line 样本；`2025-11-21 QA run` 通过 StageDDescriptorInspector ledger + Release chunk bench 重新确认。 |
| Grapheme Descriptors | `tests/xi.Core.Tests/Fixtures/grapheme_descriptors/grapheme_descriptors.json` | `--grapheme-descriptors` | `grapheme_descriptors@1.0.0` | `eb0c7c66069ca33a3626ed3909754da72223f6e0e283d6c7b2b3182a0a35182c` | `[MP-T4]` Grapheme fallback 遥测；count=668，hash 由 QA verifier + inspector 双重确认。 |
| Breaks descriptors (soft line metrics) | `tests/xi.Core.Tests/Fixtures/breaks_descriptors/breaks_descriptors.json` | `--breaks-descriptors`（Stage D exporter 默认传入） | `breaks_descriptors@1.0.0` | `5d37a7313730dd0ae9c292ca45daa442c11fe63d45f9ad21bad664f919c13f86` | 状态：Rust CLI 导出的 3 条 breaks 样本，StageDDescriptorHydrator + Inspector 均验证 ledger。 |
| Diff region snapshots | `tests/xi.Core.Tests/Fixtures/diff_regions/diff_regions.json` | `--diff-regions`（Stage D exporter 默认传入） | `diff_regions@1.0.0` | `fe76ed31cff4549ffd3f32bd3847dda15ced7538c4165b4566f782e715572562` | 3 条 diff 快照；Hydrator/Inspector 输出列举 patch id 与 manifest hash，确保与 Rust exporter 一致。 |
| Search hits & span windows | `tests/xi.Core.Tests/Fixtures/search_spans/search_spans.json` | `--search-spans`（Stage D exporter 默认传入） | `search_spans@1.0.0` | `7eac7ecf3bb0bdbf5369b6b0dcb17bf5241d8c914af791440e2c5a4582ee7d57` | 3 组 search spans；Inspector 摘要记录 window + hit count，hash 对应 manifest ledger。 |
| Leaf Split Parity | `tests/xi.Core.Tests/Fixtures/leaf_split_parity_samples.json` | （共享 `--dir` 输出） | `leaf_split_parity@0.2.0` | `e15b2528c7f6…` | 追踪 Rust/C# 叶片拆分差异；刷新时与 Stage D 一并校验。 |
| TreeBuilder Slice Trace | `tests/xi.Core.Tests/Fixtures/tree_builder_slice/basic_slice_plan.json` | `--tree-builder-trace`（脚本默认传入，可用 `-SkipTreeTrace` 停用） | `tree_builder_slice_trace@1.0.0` | `22724af7fe8b…` | `[TS-B2]` TreeBuilder tracer parity 样本，供 C# loader/诊断消费。 |

> 数据来源：`python scripts/refresh_all_assets.py --only stage-d-fixtures`（2025-11-21 QA run）触发 exporter/loader/hydrator/manifester；`python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 输出 “All 9 fixtures match the manifest hashes.”，随后 `dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures | tee tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt` 记录 Breaks/Diff/Search typed 摘要与 SHA。上述 artefact 必须一并贴入 QA 档案。

> **NodeCursorState ↔ `_editVersion` 证据链**：当 Rust Porter 以 `cargo run -p xi-rope --features serde,cursor_state --bin export-serde-fixtures -- --cursor-descriptors tests/xi.Core.Tests/Fixtures/cursor_descriptors --emit-manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 运行 exporter 时，manifest 的 `feature_gates` 会追加 `"cursor_state"`，并在 `cursor_descriptors@1.x.y` JSON 内写入 `cursor_state.edit_version` 与 `cursor_state.path[]`。C# 侧的 `Rope._editVersion` 在每次编辑时自增，`CursorDescriptorParityTests`/`StageDDescriptorLoader` 将该字段映射到 `NodeCursorState.EditVersion`，从而让 QA 可通过 `[QA-IngestionSmoke]`、`StageDDescriptorInspector` 报表以及 manifest ledger 证明 `_editVersion` 票据与 Rust `cursor_state` schema 一致。

> **Manifest（2025-11-21 刷新）**：`python scripts/refresh_all_assets.py --only stage-d-fixtures` 写入 `fixtures.manifest.json`，记录 `rust_commit=3799d2be9db0ef040517ed69df1b717e96a8958e`、`cli_rev=0.3.0`、`feature_gates=["cursor_state","serde"]`，descriptor 计数为 `chunk=20` / `cursor=12` / `grapheme=668`，`breaks/diff/search` 各 3 条。所有 hash 以 manifest 为准，Stage D loader + hydrator + inspector smoke 会在 `[QA-IngestionSmoke]` 中登记。

> `StageDDescriptorLoader` 现已成为 ingestion 的默认实现（参见 `src/xi.Core/Rope/Diagnostics/Descriptors/StageDDescriptorLoader.cs`）；QA/Stage D 工具在引用 `[StageD::ParityAssets]` 时，应先通过 loader 或 `dotnet test --filter StageDDescriptorLoaderTests` 读取 manifest，再将返回的 `StageDDescriptorManifest` 注入 ChunkBench、CLI parity 或 Telemetry 脚本。

> 所有哈希采用 `sha256sum` 计算。刷新资产时需更新本表并在 PR 描述附带新旧哈希 diff，以便 QA 记录在 `[QA-IngestionSmoke]`。

---

## [StageD::FeatureGates] Feature Gate 策略
<a id="StageD::FeatureGates"></a>

`2025-11-21` 起 `stage-d-fixtures` 刷新在默认 `serde` gate 之外亦启用 `tree_builder_slice_trace`，且 `--breaks-descriptors/--diff-regions/--search-spans --tree-builder-trace` 均由脚本自动串入（见 `[StageD::ParityAssets]`）。若需短暂关闭，可在命令行传入 `-SkipBreaksDiffSearch` 或 `-SkipTreeTrace`，并在 QA 档案记录理由。

| Gate | 默认 | 用途 | 备注 |
| --- | --- | --- | --- |
| `serde` | ✅（运行 Stage D 必须） | 启用所有 JSON 导出路径 | 关闭时 exporter 无法生成任何资产；2025-11-18 刷新包含 chunk/cursor/grapheme + breaks/diff/search。 |
| `cursor_state` | ✅ | 调试游标失效，扩充 descriptor payload | exporter 默认启用以生成 `cursor_state` 字段；若禁用需同步 `[Fixture-FeatureGates]` 并更新 manifest 记录。 |
| `tree_builder_slice_trace` | ✅ | 生成 `--tree-builder-trace` 样本 | 输出写入 `tests/xi.Core.Tests/Fixtures/tree_builder_slice/`（默认导出；如需禁用，传入 `-SkipTreeTrace`）供 `TreeBuilder` 研究与 loader parity。 |
| `breaks_diagnostics`（计划） | ⛔ | 配合 `--breaks-descriptors` 导出软换行栈、`BreaksMetric` 序列 | 目前 `serde` 即可导出 3 条 breaks 样本；如需更高粒度 telemetry，再开启并在 manifest `feature_gates[]` 登记。 |
| `diff_regions`（计划） | ⛔ | 暴露 `LineHashDiff`/`DiffBuilder` 快照 | 现有导出由 `serde` 加 flag 覆盖，若未来要捕捉增量 diff 事件，可用该 gate 启用扩展。 |
| `search_traces`（计划） | ⛔ | 捕获 `find.rs` 命中与 `Spans<T>` 状态 | 已能在 `serde` 运行中导出 3 条 search 样本；保留 gate 供 regex instrumentation 或扩展格式时使用。 |

开启额外 gate 时，需：

1. 在命令中追加对应 feature；
2. 在 `docs/architecture/fixtures/parity-fixture-schema.md` 与本节更新说明；
3. 在 `[QA-StageDManual]` 附上运行记录，确保 QA 能复现结果。

---

## [QA-IngestionSmoke] 夹具 Ingestion Smoke
<a id="QA-IngestionSmoke"></a>

| 步骤 | 命令 | 期望 |
| --- | --- | --- |
| 运行脚本 | `./scripts/refresh_serialization_fixtures.ps1 -Verbose -SkipRust -SkipDotnet`（如仅验证导入） | exporter 成功覆写所有 JSON，日志包含 CLI 标识符与 feature 列表；脚本随后默认串联 `StageDDescriptorLoaderTests`、`StageDDescriptorHydratorTests`、`python scripts/verify_fixture_manifest.py --manifest <path>` 与 `dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures`，除非显式传入 `-SkipStageDLoaderTest`、`-SkipStageDHydratorTest`、`-SkipManifestVerification` 或 `-SkipStageDInspector`。任何跳过都要在 QA 档案/AGENTS 中记录原因。 |
| Python orchestrator | `python scripts/refresh_all_assets.py --only stage-d-fixtures` | 一键触发 exporter + loader + hydrator + manifest verifier + inspector + Release 模式 chunk bench + Grapheme telemetry；命令结束后会更新 `fixtures.manifest.json`、`tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt`、`chunk-bench-latest.txt`、`grapheme-telemetry.trx` 与最新的 `stage-d-refresh-<timestamp>.log`。 |
| 校验哈希 | `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`（脚本已自动执行此命令；此处主要用于隔离问题或 `--update` 模式） | 自动验证输出必须贴入 QA 档案。若预期 hash 漂移，需要手动运行 `--update`，记录 `Manifest changes` 行后再次执行 `scripts/refresh_serialization_fixtures.ps1`（勿携带 `-SkipManifestVerification`）以生成新的只读校验日志；必要时 fallback `sha256sum tests/xi.Core.Tests/Fixtures/**/*.json`。 |
| Loader 校验 | `dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter StageDDescriptorLoaderTests` | `StageDDescriptorLoader` 读取 `fixtures.manifest.json` + descriptor JSON 时不抛异常，metadata（`rust_commit`, `cli_rev`, `feature_gates`, descriptor count）与 manifest/表格一致；脚本默认为 QA 运行该 smoke，如因调试跳过需补跑并在 `agents/qa-engineer.md` 说明。 |
| Hydrator 校验 | `dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter StageDDescriptorHydratorTests` | `StageDDescriptorHydrator` 能从 loader 产出的 manifest ledger 中加载 Breaks/Diff/Search 描述符，并验证 skeleton/hash 与实际 JSON 匹配；用于确认 ingestion 链路完整。 |
| Inspector 摘要 | `python scripts/refresh_all_assets.py --only stage-d-fixtures` 会自动执⾏ `dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures` 并把 stdout 写入 `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt`（如需单独 rerun 可直接调用 dotnet 命令）。 | 将该 artefact 附在 QA 记录：文本需列出 Breaks/Diff/Search typed 数量、样本路径与 manifest sha256，以证明 Stage D loader→hydrator→inspector 烟囱完整。 |
| 运行测试 | `dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter Serialization` | 所有 Stage D 测试通过；失败则回滚夹具并打开 `[MP-R9]` 风险。 |
| 记录结果 | 更新 `agents/qa-engineer.md`（“最近完成”）并在 `AGENTS.md` 工作日志写入 hash、命令与测试状态 | 提供 CI 链接或本地日志路径。若本次刷新使用 `-SkipStageDLoaderTest` 或 `-SkipStageDHydratorTest`，在两份档案中记录跳过理由与补偿动作。 |

> **Latest run — 2025-11-18（✅ Stage D orchestrator + Release bench + telemetry）**：`python scripts/refresh_all_assets.py --only stage-d-fixtures | tee tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-143204.log` 重新导出 Rust commit `3799d2be9db0ef040517ed69df1b717e96a8958e`（`feature_gates=["cursor_state","serde"]`），生成 chunk/grapheme/breaks/diff/search ledger `69ba7f25` / `eb0c7c66` / `5d37a731` / `fe76ed31` / `7eac7ecf`。脚本串联 `dotnet test Xi.Editor.sln --filter StageDDescriptorLoaderTests`（5/5 通过）+ `--filter StageDDescriptorHydratorTests`（3/3 通过），随后执行 `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`（All 9 fixtures match）与 `dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures`，输出写入 `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt`。同一 orchestrator 还刷新 `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`（Release 重放 4.97 ms chunk / 3.06 ms line，201.25 MB/s / 327.31 MB/s，线程分配 37,944 bytes，GC 0/0/0）以及 `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx`；所有 artefact 现可链接至 `[QA-ChunkBench]` / `[QA-Telemetry]` / Goal Tree。

### Skeleton 备用路径

当 Rust CLI 尚未导出 Breaks/Diff/Search JSON 时，可运行：

```bash
dotnet test Xi.Editor.sln -v m --filter "BreaksSkeletonTests|DiffSkeletonTests|SearchSkeletonTests|StageDDescriptorLoaderTests|StageDDescriptorHydratorTests"
```

该命令串联 loader + hydrator smoke 与 C# skeleton 用例，确保 QA 仍能监控 Stage D Breaks/Diff/Search 链路。Rust exporter 一旦能写回 manifest，即用默认的 manifest diff + loader + hydrator smoke 流程替换此备用方案。

---

## [QA-StageDManual] 手动验收与故障排查
<a id="QA-StageDManual"></a>

1. **结构异常**：若 diff 中出现未知字段/缺失字段，先比对 `docs/architecture/fixtures/parity-fixture-schema.md`，再检查 Rust `serde_fixtures.rs` 与 exporter 分支是否一致。
2. **hash 漂移**：若仅有少量样本 hash 变化，记录在 `[StageD::ParityAssets]` 表并说明原因；若 hash 全量变化且 Rust 端无 commit 更新，视为脚本误运行，需要回退。
3. **CLI 失败**：
   - `cargo run` 返回 `serde` gate 未启用 ⇒ 检查脚本参数。
   - `cursor_state`/`tree_builder_slice_trace` feature 不存在 ⇒ 确认 Rust 子仓库已拉取最新 commit。
4. **平台差异**：PowerShell 通过 `.ps1` 版本的 `run_all_checks` 与 exporter ；Bash/WSL 使用无扩展脚本。若在 Windows 上调用 Bash 脚本会弹出“打开方式”对话框，需改用 `.ps1`（已在 `QA` 手册记录）。
5. **升级/回滚流程**：所有失败需在 `AGENTS.md`“风险 & 未解问题”登记；严重回滚（如 `serde_fixtures` schema 回退）需同步 `[Decision-M3-*]`/`[MP-Rx]`。

---

## [QA-ChunkBench] 1 MB Chunk/Line Benchmark
<a id="QA-ChunkBench"></a>

- **Purpose**：锁定 `[MP-T3]` / `[MP-R10]` 要求的 1 MB chunk/line 基线，防止 `design-divergence-log.md` 中记录的临时降级成为常态；亦用于 `docs/architecture/m3-architect-decision.md` 中的 throughput 阈值复核。
- **Inputs**：`tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj`、`tests/xi.Core.Tests/RopeChunkEnumeratorDiagnosticsTests.cs`，与 Stage D 同步的 chunk/grapheme 夹具（见 `[StageD::ParityAssets]`）。

| 步骤 | 命令 | 记录点 |
| --- | --- | --- |
| 切换到仓库根 | `cd /repos/xi-editor-sharp` | 确保与 Stage D 流水线使用同一 commit，hash 记入 `AGENTS.md`。
| 运行基准 | `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj --configuration Release -- --stage-d --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` | 命令同 `tests/xi.Core.Tests/Benchmarks/Diagnostics/README.md`，`--stage-d` 读取最新 manifest ledger；`refresh_all_assets.py --only stage-d-fixtures` 会自动运行此命令。
| 抓取指标 | 输出会打印 Chunk/Line 计数、最大 chunk 长度、总复制字节及枚举时长；`--report` 会把同样的数据写入 `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` 供 QA 入档。 | Screenshot/log 附在 `m3-implementation-plan.md §5.3` 与 `agents/qa-engineer.md`“最近完成”。
| 校验阈值 | - 载荷：必须是 README 构造的 1 MB synthetic rope。<br>- 内存：GC/Process Alloc < **5 MB**。<br>- 吞吐：`1MB / chunkEnumerationMs` 与 `1MB / lineEnumerationMs` 推算 > **200 MB/s**。 | 若任一阈值失败，将结果写入 `docs/architecture/design-divergence-log.md` 并引用 `[QA-ChunkBench]`，同时在 `AGENTS.md` 标记为 Pending fix。

> **Latest run — 2025-11-18（Release chunk/line replay + alloc stats）**：`dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj --configuration Release -- --stage-d --include-alloc-stats --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`（由 `refresh_all_assets.py` 触发后再次手动确认）在相同 manifest ledger 上回放 9 个 chunk 样本 / 11 条 line：chunk 阶段 4.97 ms ⇒ `1MB / 4.97ms ≈ 201.25 MB/s`，line 阶段 3.06 ms ⇒ `327.31 MB/s`。`Allocation statistics` 报告线程分配 37,944 bytes，GC gen0/1/2 均为 0，满足 <5 MB 目标；完整指标与样本摘要记录在 `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` 并引用 `stage-d-inspector-latest.txt` 的同 commit 描述。

> **QA 附件提醒**：执行 `python scripts/refresh_all_assets.py --only stage-d-fixtures` 或任何 Stage D smoke 前后，请将生成的 `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt`、`chunk-bench-latest.txt`、`grapheme-telemetry.trx` 以及相应的 `stage-d-refresh-<timestamp>.log` 一并入档，便于复核 manifest 哈希、1 MB 枚举指标与 Grapheme telemetry 的同 commit 证据。

- **后续动作**：
  - 在 `AGENTS.md`「下一步行动」写入下次 rerun 日期，与 `[QA-IngestionSmoke]` hash 审计保持同频。
  - 如需比较 Rust/C# 之间差异，把数据追加到 `docs/architecture/type-system-migration-log.md` 对应 blocker 证据栏。
  - 提交 PR 时在描述引用 `[QA-ChunkBench]` 以解锁 `qaAnchors` 验证。

---

## [QA-Telemetry] Grapheme Fallback Monitoring
<a id="QA-Telemetry"></a>

- **Purpose**：追踪 Grapheme fallback 比例，满足 `[MP-T4]`、`[MP-R10]` 与 `docs/architecture/m3-architect-decision.md` 里的 telemetry 目标；违反 <=0.5% 阈值时需在 `docs/architecture/design-divergence-log.md` 登记临时缓解策略。
- **Inputs**：`tests/xi.Core.Tests/GraphemeNavigatorSmokeTests.cs`、`tests/xi.Core.Tests/GraphemeNavigatorParityTests.cs`、`GraphemeNavigationMetrics` 产出的 counters，以及 `tests/xi.Core.Tests/Fixtures/grapheme_descriptors/grapheme_descriptors.json`（保持与 `[StageD::ParityAssets]` 同步）。

| 步骤 | 命令 | 记录点 |
| --- | --- | --- |
| 运行测试 | `dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter GraphemeNavigator` | `--filter` 会覆盖 smoke + parity 测试，输出 `GraphemeNavigationMetrics` 日志。Bash/PowerShell 命令一致。 |
| 捕获指标 | 从测试输出或 `TestResults/*.trx` 中提取：`fallbackCount`, `totalMoves`, `neighborLookups`, `fallbackRatio = fallbackCount / totalMoves`。 | 将原始数字贴到 `agents/qa-engineer.md`、`AGENTS.md`，并在 `m3-implementation-plan.md §5.3` 的 QA 表更新 `qaAnchors.qa-telemetry.note`。 |
| 阈值判断 | `fallbackRatio <= 0.5%`（<=0.005），`neighborLookups` 相比前次 run 不得激增 >10%，`moveCalls` 数量应与 `tests/xi.Core.Tests/GraphemeNavigatorSmokeTests.cs` 预期范围匹配。 | 若超标：<br>1. 在 `docs/architecture/design-divergence-log.md` 新增记录，说明触发点与拟定缓解。<br>2. 在 `AGENTS.md` 风险表登记，引用 `[QA-Telemetry]`。<br>3. 通知 Architecture Mapper / C# Implementer。 |

- **Latest run — 2025-11-18（Grapheme telemetry smoke）**：`dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter GraphemeNavigatorSmokeTests --logger "trx;LogFileName=tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx"` 生成 `tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry.trx`。依据 `GraphemeNavigatorSmokeTests` 中的 `GraphemeNavigationMetrics` snapshot：`totalMoves=4`（MoveNext=2, MovePrevious=2），`neighborLookups` 分别为 Forward=1 / Backward=1，`scalarFallbacks=0`，因此 `fallbackRatio=0/4 = 0%`，满足 ≤0.5% 阈值。若未来 `RequiresFallback` 样本数量或 CLI instrumentation 增加，此 TRX 作为 `[QA-Telemetry]` 的基准 artefact。

- **后续动作**：
  - 将本次 run 的 `dotnet test` 命令与结果链接到 `[QA-IngestionSmoke]` 中的测试矩阵，以便 Stage D smoke 与 telemetry 数据共用。
  - 若 fallback ratio 曾经触发 Divergence，恢复 ≤0.5% 后需在 `design-divergence-log.md` 更新 Exit 条件并引用 `[QA-Telemetry]`。
  - 对应的 `QA` dossier与 `m3-implementation-plan.md` QA 表必须在 24 小时内刷新，确保 `qaAnchors` 不指向过期数据。

---

## [StageD::AutomationBacklog] 自动化与开放问题
<a id="StageD::AutomationBacklog"></a>

- 追加 `run_all_checks` + exporter + `dotnet test` 的 CI job，发布夹具漂移报告。
- 为 `scripts/refresh_serialization_fixtures.ps1` 补强默认导出守护（Breaks/Diff/Search/TreeTrace 现已默认启用，可用 `-SkipBreaksDiffSearch`、`-SkipTreeTrace` 精准关闭），以便 QA 快速定位差异。
- 评估 `python scripts/goal_tree_sync.py`（筹备中）是否同时校验 `[StageD::ParityAssets]` 的哈希并提醒更新。
- 研究 `cargo xtask fixtures` 以提供单入口，减少 `cargo run` 命令参数错误。

---

## [StageD::ChangeLog] 变更记录
<a id="StageD::ChangeLog"></a>

| 日期 | 版本 | 作者 | 摘要 |
| 2025-11-19 | v2.0 | Architecture Mapper | 套用 `document-structure-template.md`：新增 front-matter、Stage D/QA anchors、资产哈希表、Feature Gate 策略、Automation backlog。 |
| 2025-11-17 | v2.1 | QA Engineer | `[QA-IngestionSmoke]`/`[QA-ChunkBench]`：新增 canonical manifest 校验脚本指引、记录 1 MB chunk 基准（12.62 ms/14.99 ms，<200 MB/s）并标记风险。 |
| 2025-11-15 | v1.1 | Rust Porter | 引入 `scripts/refresh_serialization_fixtures.ps1`、PowerShell `run_all_checks.ps1`，强化 Windows 指南。 |
| 2025-11-11 | v1.0 | Rust Porter | 初版 Stage D 手册，记录 serde 回归/导出流程。 |

---

[Fixture-FeatureGates]: ../architecture/fixtures/parity-fixture-schema.md#fixture-featuregates
[Fixture-Overview]: ../architecture/fixtures/parity-fixture-schema.md#fixture-overview
[MP-T1]: ../architecture/m3-implementation-plan.md#21-任务-1游标系统实现
[MP-T3]: ../architecture/m3-implementation-plan.md#23-任务-3chunk-迭代器骨架
[MP-T4]: ../architecture/m3-implementation-plan.md#24-任务-4grapheme-降级实现
[MP-R8]: ../architecture/m3-implementation-plan.md#r8
[MP-R9]: ../architecture/m3-implementation-plan.md#r9
