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
3. **刷新夹具**：优先使用 `scripts/refresh_serialization_fixtures.ps1` 触发 exporter（默认启用 `-ExportParityFixtures`），如需调试可改用手动命令（见 `[StageD::FixtureFlow]`）。若希望与 goal tree / skeleton 流水线一键执行，可运行 `python scripts/refresh_all_assets.py`（默认包含 `stage-d-fixtures` 步骤），或使用 `python scripts/refresh_all_assets.py --only stage-d-fixtures` 单独执行 Stage D。
4. **快速 diff**：运行 `git status --short tests/xi.Core.Tests/Fixtures` 与定向 `git diff`，确认变化仅限目标 JSON；若结构调整，需先在 Rust helper/文档更新 schema。
5. **校验 manifest + 测试矩阵**：先运行 `python scripts/verify_fixture_manifest.py`（基于 canonical JSON：`sort_keys=True`, `ensure_ascii=False`, `separators=(",", ":")`）核对 `fixtures.manifest.json`，如遇异常可 fallback 到 `sha256sum tests/xi.Core.Tests/Fixtures/**/*.json` 手工逐条比对；随后执行 `dotnet test Xi.Editor.sln` 并重跑 `cargo test -p xi-rope --no-default-features`，结果链接到 `[QA-IngestionSmoke]` 报告。
6. **文档与日志**：在 `AGENTS.md`、`agents/rust-porter.md`、`agents/qa-engineer.md`、`docs/architecture/rope-cs-mirror-plan.md` 等文件记录操作；若 CLI/流程有变化，更新 `[Fixture-*]` 文档与本手册对应章节。

---

## [StageD::FixtureFlow] Fixture 刷新流程
<a id="StageD::FixtureFlow"></a>

> 推荐脚本：`scripts/refresh_serialization_fixtures.ps1 -Verbose`。此脚本会顺序执行 Rust 校验（可用 `-SkipRust` 跳过）、调用 exporter、重跑 `dotnet test`（可用 `-SkipDotnet` 跳过），并在日志中写入实际命令。刷新结束后它会自动触发 `StageDDescriptorLoaderTests`（Stage D loader smoke），即使指定 `-SkipDotnet` 也会运行，只有在显式传入 `-SkipStageDLoaderTest` 时才会跳过。Linux/WSL 可直接调用 Bash 版本流程；若需要与 goal tree / skeleton 流程串行执行，可使用 `python scripts/refresh_all_assets.py` 让 `stage-d-fixtures`（description: “Export Rust fixtures + run Stage D loader smoke”）自动串入。

> **Manifest diff + loader smoke 证据链**：刷新或手动导出后必须运行 `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`。若预计存在 hash 漂移（Rust exporter 更新或 JSON 被覆写），追加 `--update` 让脚本重写 `payload_hash` 并打印 `Manifest changes` 摘要；无漂移时脚本会输出 “no changes” 提示。该日志与 `StageDDescriptorLoaderTests` 的通过记录一起，构成 `[QA-IngestionSmoke]` 所需的 “manifest diff + loader smoke” 证据，禁止跳过。

> **最新 manifest（2025-11-18 稳定刷新）**：QA 运行 `python scripts/refresh_all_assets.py --only stage-d-fixtures` 后生成 `fixtures.manifest.json`，记录 `rust_commit=96ce8ddff31f368b52ca930b3930f1cf8ecd909a`、`feature_gates=["serde"]`、descriptor 计数 `chunk/cursor/grapheme = 20/11/668`，并新增 `breaks/diff/search = 3/3/3` 可选资产。导出脚本会复用既有 `generated_at_unix_millis` 字段，因此重复运行不会触发 hash 漂移。所有哈希均由 `scripts/verify_fixture_manifest.py` 回写，可在 `[StageD::ParityAssets]` 查阅细节。

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
cargo run -p xi-rope --features serde --bin export-serde-fixtures \
  --dir tests/xi.Core.Tests/Fixtures \
  --cursor-descriptors tests/xi.Core.Tests/Fixtures/cursor_descriptors \
  --chunk-descriptors tests/xi.Core.Tests/Fixtures/chunk_descriptors \
  --grapheme-descriptors tests/xi.Core.Tests/Fixtures/grapheme_descriptors \
  --breaks-descriptors tests/xi.Core.Tests/Fixtures/breaks_descriptors \
  --diff-regions tests/xi.Core.Tests/Fixtures/diff_regions \
  --search-spans tests/xi.Core.Tests/Fixtures/search_spans \
  --emit-manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json
cd "$XI_EDITOR_SHARP_ROOT"
```

> 调试模式：把 `--features serde` 替换为 `--features serde,cursor_state` 以捕获更详细的 `CursorDescriptor`，或追加 `--features serde,tree_builder_slice_trace --tree-builder-trace tests/xi.Core.Tests/Fixtures/ParityFixtures/tree_builder_trace` 以并行导出切片事件。每次开启额外特性都要在 `[Fixture-FeatureGates]` 和 `[StageD::FeatureGates]` 记录原因，并确认 `fixtures.manifest.json` 中的 `feature_gates[]` 与实际命令一致。

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

1. **默认校验**：刷新脚本或手工 exporter 结束后立即运行 `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`，保留摘要表格输出。
2. **更新模式**：若 canonical JSON drift 属于预期，增加 `--update` 参数；脚本会重写相应 `payload_hash` 值、打印逐条 `old -> new` 摘要，并在写入后自动再跑一次校验。
3. **记录要求**：将 `Manifest changes` 或 `--update: manifest already in sync; no changes written.` 行复制到 `agents/qa-engineer.md`、`AGENTS.md`、PR 描述，作为哈希证据。禁止绕过脚本手工修改 manifest。
4. **与 loader smoke 绑定**：`--update` 完成后必须再次运行（或确认脚本自动触发的）`StageDDescriptorLoaderTests`，输出 + manifest diff 才能满足 `[QA-IngestionSmoke]` 的证明链。

### 4. 使用 StageDDescriptorLoader 校验 manifest

`src/xi.Core/Rope/Diagnostics/Descriptors/StageDDescriptorLoader.cs` 是唯一的 ingestion 入口：它会读取 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`、`chunk_descriptors/chunk_descriptors.json`、`grapheme_descriptors/grapheme_descriptors.json` 并返回 `StageDDescriptorManifest`（含 manifest ledger：每个 `fixtures[].name` 的 `count/schema_hash/payload_hash`）。`scripts/refresh_serialization_fixtures.ps1` 会在流程末尾自动执行 `StageDDescriptorLoaderTests`；仅在需要隔离 exporter 问题或调试 dotnet 环境时，才使用 `-SkipStageDLoaderTest`。手动复核命令如下：

```bash
dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter StageDDescriptorLoaderTests
```

> 结果会验证 manifest 中的 `rust_commit`, `cli_rev`, `feature_gates[]`、payload 计数，以及 `fixtures[].count/schema_hash/payload_hash` 是否与 JSON 内容一致（参考 `tests/xi.Core.Tests/Diagnostics/StageDDescriptorLoaderTests.cs`）。如需在 QA 工具或 CLI 中手动调用，可执行 `StageDDescriptorLoader.LoadFromFixtureDirectory("tests/xi.Core.Tests/Fixtures")` 并缓存返回的 `StageDDescriptorManifest` 供 `[QA-IngestionSmoke]`、`[QA-ChunkBench]` 或 Stage D CLI 脚本复用。若出于调试目的跳过 smoke，QA 需在 `agents/qa-engineer.md` 与 `AGENTS.md` 记录 `-SkipStageDLoaderTest` 的使用原因。

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

| Asset | Path | Export Flag | Schema / Version | SHA256（2025-11-18 刷新） | 备注 |
| --- | --- | --- | --- | --- | --- |
| Subset Regression | `tests/xi.Core.Tests/Fixtures/subset_regression.json` | `--dir` 默认覆盖 | `serde_fixtures::subset` | `28fa3c807f83961f6cdf9695f3605bdf22972f734e2470adae1234c85deb138f` | Stage A（Subset）黄金串，回归测试直接消费；hash 与 2025-11-18 manifest 一致。 |
| Delta Regression | `tests/xi.Core.Tests/Fixtures/delta_regression.json` | `--dir` 默认覆盖 | `serde_fixtures::delta` | `59c45336bace174a9ab50cefdef05bdd2890d4d93a376df4e6897ca71a20cf7f` | Stage B（Delta）黄金串；manifest 确认 `count=1`。 |
| Engine Regression | `tests/xi.Core.Tests/Fixtures/engine_regression.json` | `--dir` 默认覆盖 | `serde_fixtures::engine` | `8707d5de24e9369bf3a818a40030626118da2752ee1bdb54363cfb3e1ffa1cc2` | Stage C（Engine）黄金串；`python scripts/refresh_all_assets.py --only stage-d-fixtures` 2025-11-18 运行后保持稳定。 |
| Cursor Descriptors | `tests/xi.Core.Tests/Fixtures/cursor_descriptors/cursor_descriptors.json` | `--cursor-descriptors` | `cursor_descriptors@1.1.0` | `fe963d909d5c4e225bfd6e0c7085def006dccfadde7da8f5ea15a574a1483375` | `[MP-T1]` NodeCursor parity；manifest 记录 `count=11`。 |
| Chunk Descriptors | `tests/xi.Core.Tests/Fixtures/chunk_descriptors/chunk_descriptors.json` | `--chunk-descriptors` | `chunk_descriptors@1.0.0` | `e62a4faa936a20b261b167ddbd2be3b4d3566f3099149b2be215fbfafeecd756` | `[MP-T3]` Chunk/Line 样本；`2025-11-18 稳定刷新` 已锁定 `count=20`，manifest 哈希与表格同步。 |
| Grapheme Descriptors | `tests/xi.Core.Tests/Fixtures/grapheme_descriptors/grapheme_descriptors.json` | `--grapheme-descriptors` | `grapheme_descriptors@1.0.0` | `c6b1721d29286f01e67d2b7491361c2636a6a6affcc9e15206fb31c2fa5d1f0e` | `[MP-T4]` Grapheme fallback 遥测；`2025-11-18 稳定刷新` 记录 `count=668`。 |
| Breaks descriptors (soft line metrics) | `tests/xi.Core.Tests/Fixtures/breaks_descriptors/breaks_descriptors.json` | `--breaks-descriptors`（Stage D exporter 默认传入） | `breaks_descriptors@1.0.0` | `ab2f746e2bdd945e69b0acf9cd275c068a5ba6546f52a820144244f3b0e6e22e` | 状态：C# skeleton + smoke tests 已上线，用以在 Rust CLI 尚未写出真实 JSON/manifest 时覆盖 loader + 型骨架；待 exporter 落地后改回真实 ingestion。 |
| Diff region snapshots | `tests/xi.Core.Tests/Fixtures/diff_regions/diff_regions.json` | `--diff-regions`（Stage D exporter 默认传入） | `diff_regions@1.0.0` | `8c400ea433b77d9aa4f7b0cb57cbbcd6d1b935d52ff7e61101ad7a8babac5076` | 状态：C# skeleton + smoke tests 已上线，Rust exporter 写 manifest 前暂以 skeleton 维持监控，落地后切回 manifest backed ingestion。 |
| Search hits & span windows | `tests/xi.Core.Tests/Fixtures/search_spans/search_spans.json` | `--search-spans`（Stage D exporter 默认传入） | `search_spans@1.0.0` | `ce068c5217d2c45d213609d538e5a30280d10559894ef81bb9692d75d23c3502` | 状态：沿用 C# skeleton + smoke tests 进行守护，待 Rust exporter 输出并写入 manifest 后即可替换为真实 ingestion。 |
| Leaf Split Parity | `tests/xi.Core.Tests/Fixtures/leaf_split_parity_samples.json` | （共享 `--dir` 输出） | `leaf_split_parity@0.2.0` | `e15b2528c7f6…` | 追踪 Rust/C# 叶片拆分差异；刷新时与 Stage D 一并校验。 |
| TreeBuilder Slice Trace | `tests/xi.Core.Tests/Fixtures/tree_builder_slice/basic_slice_plan.json` | `--tree-builder-trace`（需 `-ExportTreeTrace`） | `tree_builder_slice_trace@1.0.0` | `22724af7fe8b…` | `[TS-B2]` TreeBuilder tracer parity 样本，供 C# loader/诊断消费。 |

> **Manifest（2025-11-18 刷新）**：`python scripts/refresh_all_assets.py --only stage-d-fixtures` 写入 `fixtures.manifest.json`，记录 `rust_commit=96ce8ddff31f368b52ca930b3930f1cf8ecd909a`、`cli_rev=0.3.0`、`feature_gates=["serde"]`，descriptor 计数为 `chunk=20` / `cursor=11` / `grapheme=668`，新导出的 `breaks/diff/search` 均为 3 条样本（可选资产 3/3/3）。所有 hash 以 manifest 为准，Stage D Loader smoke 会在 `[QA-IngestionSmoke]` 中登记。

> `StageDDescriptorLoader` 现已成为 ingestion 的默认实现（参见 `src/xi.Core/Rope/Diagnostics/Descriptors/StageDDescriptorLoader.cs`）；QA/Stage D 工具在引用 `[StageD::ParityAssets]` 时，应先通过 loader 或 `dotnet test --filter StageDDescriptorLoaderTests` 读取 manifest，再将返回的 `StageDDescriptorManifest` 注入 ChunkBench、CLI parity 或 Telemetry 脚本。

> 所有哈希采用 `sha256sum` 计算。刷新资产时需更新本表并在 PR 描述附带新旧哈希 diff，以便 QA 记录在 `[QA-IngestionSmoke]`。

---

## [StageD::FeatureGates] Feature Gate 策略
<a id="StageD::FeatureGates"></a>

`2025-11-18` 的 `stage-d-fixtures` 刷新仅启用了 `serde` gate，`--breaks-descriptors/--diff-regions/--search-spans` 现已默认串入并写入 manifest（见 `[StageD::ParityAssets]`）。若需要追加调试 telemetry，再考虑重新启用下表中的其他 gate。

> 本次稳定刷新除 `serde` 外未开启额外 gate；Breaks/Diff/Search flag 已默认开启且随 manifest 一并交付，无需额外配置。

| Gate | 默认 | 用途 | 备注 |
| --- | --- | --- | --- |
| `serde` | ✅（运行 Stage D 必须） | 启用所有 JSON 导出路径 | 关闭时 exporter 无法生成任何资产；2025-11-18 刷新包含 chunk/cursor/grapheme + breaks/diff/search。 |
| `cursor_state` | ⛔ | 调试游标失效，扩充 descriptor payload | 仅在 `[MP-R8]` 调试时开启，并在 `[Fixture-FeatureGates]` 记录。 |
| `tree_builder_slice_trace` | ⛔ | 生成 `--tree-builder-trace` 样本 | 输出写入 `tests/xi.Core.Tests/Fixtures/tree_builder_slice/`（通过 `-ExportTreeTrace` 启用），供 `TreeBuilder` 研究与 loader parity。 |
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
| 运行脚本 | `./scripts/refresh_serialization_fixtures.ps1 -Verbose -SkipRust -SkipDotnet`（如仅验证导入） | exporter 成功覆写所有 JSON，日志包含 CLI 标识符与 feature 列表；脚本随后会运行 `StageDDescriptorLoaderTests` 作为 loader smoke，除非显式传入 `-SkipStageDLoaderTest`。 |
| 校验哈希 | `python scripts/verify_fixture_manifest.py`（canonical JSON：`sort_keys=True`, `ensure_ascii=False`, `separators=(",", ":")`）；若刷新预期会修改 hash，追加 `--update --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 让脚本写回 `payload_hash` 并自动二次校验；必要时 fallback `sha256sum tests/xi.Core.Tests/Fixtures/**/*.json` | 输出表格必须贴入 QA 档案；若使用 `--update`，务必记录 `Manifest changes` 或 “no changes” 行并与 loader smoke 一同上传，禁止跳过此步骤或手动编辑 manifest。 |
| Loader 校验 | `dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter StageDDescriptorLoaderTests` | `StageDDescriptorLoader` 读取 `fixtures.manifest.json` + descriptor JSON 时不抛异常，metadata（`rust_commit`, `cli_rev`, `feature_gates`, descriptor count）与 manifest/表格一致；脚本默认为 QA 运行该 smoke，如因调试跳过需补跑并在 `agents/qa-engineer.md` 说明。 |
| 运行测试 | `dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter Serialization` | 所有 Stage D 测试通过；失败则回滚夹具并打开 `[MP-R9]` 风险。 |
| 记录结果 | 更新 `agents/qa-engineer.md`（“最近完成”）并在 `AGENTS.md` 工作日志写入 hash、命令与测试状态 | 提供 CI 链接或本地日志路径。若本次刷新使用 `-SkipStageDLoaderTest`，在两份档案中记录跳过理由与补偿动作。 |

### Skeleton 备用路径

当 Rust CLI 尚未导出 Breaks/Diff/Search JSON 时，可运行：

```bash
dotnet test Xi.Editor.sln -v m --filter "BreaksSkeletonTests|DiffSkeletonTests|SearchSkeletonTests|StageDDescriptorLoaderTests"
```

该命令串联 loader smoke 与 C# skeleton 用例，确保 QA 仍能监控 Stage D Breaks/Diff/Search 链路。Rust exporter 一旦能写回 manifest，即用默认的 manifest diff + loader smoke 流程替换此备用方案。

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
| 运行基准 | `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj -c Release` | 命令同 `tests/xi.Core.Tests/Benchmarks/Diagnostics/README.md`（PowerShell/Bash 相同）。
| 抓取指标 | 输出会打印 Chunk/Line 计数、最大 chunk 长度、总复制字节及枚举时长；人工记录 `chunkCount`, `maxChunkLength`, `bytesCopied`, `chunkEnumerationMs`, `lineEnumerationMs`。 | Screenshot/log 附在 `m3-implementation-plan.md §5.3` 与 `agents/qa-engineer.md`“最近完成”。
| 校验阈值 | - 载荷：必须是 README 构造的 1 MB synthetic rope。<br>- 内存：GC/Process Alloc < **5 MB**。<br>- 吞吐：`1MB / chunkEnumerationMs` 与 `1MB / lineEnumerationMs` 推算 > **200 MB/s**。 | 若任一阈值失败，将结果写入 `docs/architecture/design-divergence-log.md` 并引用 `[QA-ChunkBench]`，同时在 `AGENTS.md` 标记为 Pending fix。

> **Latest run — 2025-11-17**：`dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj -c Release --no-build` 输出 `ChunkCount=1,049`, `MaxChunkLength=1,000`, `TotalUtf16Chars=1,048,625`, `LineCount=8,389`；Chunk 枚举 12.62 ms（≈79 MiB/s 名义 1 MB / ≈159 MiB/s UTF-16）与 Line 枚举 14.99 ms（≈67 / 133 MiB/s）。吞吐仍低于 `[MP-R10]` 200 MB/s 门槛，额外分配 <5 MB 缺少诊断信号，已在 `docs/architecture/m3-implementation-plan.md §5.3`、`agents/qa-engineer.md` 记档，并在 `[QA-ChunkBench]` 状态栏标记为 ⚠️。

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

- **后续动作**：
  - 将本次 run 的 `dotnet test` 命令与结果链接到 `[QA-IngestionSmoke]` 中的测试矩阵，以便 Stage D smoke 与 telemetry 数据共用。
  - 若 fallback ratio 曾经触发 Divergence，恢复 ≤0.5% 后需在 `design-divergence-log.md` 更新 Exit 条件并引用 `[QA-Telemetry]`。
  - 对应的 `QA` dossier与 `m3-implementation-plan.md` QA 表必须在 24 小时内刷新，确保 `qaAnchors` 不指向过期数据。

---

## [StageD::AutomationBacklog] 自动化与开放问题
<a id="StageD::AutomationBacklog"></a>

- 追加 `run_all_checks` + exporter + `dotnet test` 的 CI job，发布夹具漂移报告。
- 为 `scripts/refresh_serialization_fixtures.ps1` 增加 `-ExportTreeTrace`、`-OnlyParity` 等模式，以便 QA 快速定位差异。
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
