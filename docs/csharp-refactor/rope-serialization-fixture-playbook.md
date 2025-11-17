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
5. **测试矩阵**：执行 `dotnet test Xi.Editor.sln` 并重跑 `cargo test -p xi-rope --no-default-features`，结果链接到 `[QA-IngestionSmoke]` 报告。
6. **文档与日志**：在 `AGENTS.md`、`agents/rust-porter.md`、`agents/qa-engineer.md`、`docs/architecture/rope-cs-mirror-plan.md` 等文件记录操作；若 CLI/流程有变化，更新 `[Fixture-*]` 文档与本手册对应章节。

---

## [StageD::FixtureFlow] Fixture 刷新流程
<a id="StageD::FixtureFlow"></a>

> 推荐脚本：`scripts/refresh_serialization_fixtures.ps1 -Verbose`。此脚本会顺序执行 Rust 校验（可用 `-SkipRust` 跳过）、调用 exporter、重跑 `dotnet test`（可用 `-SkipDotnet` 跳过），并在日志中写入实际命令。Linux/WSL 可直接调用 Bash 版本流程；若需要与 goal tree / skeleton 流程串行执行，可使用 `python scripts/refresh_all_assets.py` 让 `stage-d-fixtures` 步骤自动串入。

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
  --emit-manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json
cd "$XI_EDITOR_SHARP_ROOT"
```

> 调试模式：把 `--features serde` 替换为 `--features serde,cursor_state` 以捕获更详细的 `CursorDescriptor`，或追加 `--features serde,tree_builder_slice_trace --tree-builder-trace tests/xi.Core.Tests/Fixtures/ParityFixtures/tree_builder_trace` 以并行导出切片事件。每次开启额外特性都要在 `[Fixture-FeatureGates]` 和 `[StageD::FeatureGates]` 记录原因，并确认 `fixtures.manifest.json` 中的 `feature_gates[]` 与实际命令一致。

### 4. 差异审计与格式化

```powershell
git status --short tests/xi.Core.Tests/Fixtures
git diff tests/xi.Core.Tests/Fixtures/*.json
```

- 核对结构字段（`els`, `revs`, `metadata.schema_version` 等）顺序是否与 Rust 输出一致。
- 数值字段（`pos`, `len`, `priority`、`descriptor_count` 等）若出现大幅波动，需要回到 Rust helper 查证。
- 空数组/可选字段应遵循 serde `skip_serializing_if` 约定，若 diff 中出现 `null` 字段，说明 Rust 端需要修复。

### 5. 文档同步

刷新完成后务必更新：

- `docs/architecture/rope-cs-mirror-plan.md`（Stage D 章节）。
- `docs/architecture/fixtures/parity-fixture-schema.md`（若 schema/feature gate 改动）。
- `docs/architecture/rope-port-mapping.md`（新增 helper/资产时标注 `[RPM-ParityAssets]`）。
- `AGENTS.md` 与相关 `agents/*.md`（记录操作、结果和下一步）。

---

## [StageD::ParityAssets] Parity 资产总览
<a id="StageD::ParityAssets"></a>

| Asset | Path | Export Flag | Schema / Version | SHA256 (2025-11-19) | 备注 |
| --- | --- | --- | --- | --- | --- |
| Subset Regression | `tests/xi.Core.Tests/Fixtures/subset_regression.json` | `--dir` 默认覆盖 | `serde_fixtures::subset` | `88c815d87b7f…` | Stage A（Subset）黄金串，回归测试直接消费。 |
| Delta Regression | `tests/xi.Core.Tests/Fixtures/delta_regression.json` | `--dir` 默认覆盖 | `serde_fixtures::delta` | `d23af6d6bcd3…` | Stage B（Delta）黄金串。 |
| Engine Regression | `tests/xi.Core.Tests/Fixtures/engine_regression.json` | `--dir` 默认覆盖 | `serde_fixtures::engine` | `e08196711c79…` | Stage C（Engine）黄金串。 |
| Cursor Descriptors | `tests/xi.Core.Tests/Fixtures/cursor_descriptors/cursor_descriptors.json` | `--cursor-descriptors` | `cursor_descriptors@1.1.0` | `2a6a076efaf8…` | `[MP-T1]` 用于 NodeCursor parity。 |
| Chunk Descriptors | `tests/xi.Core.Tests/Fixtures/chunk_descriptors/chunk_descriptors.json` | `--chunk-descriptors` | `chunk_descriptors@1.0.0` | `12267b734ca6…` | `[MP-T3]` Chunk/Line 诊断样本。 |
| Grapheme Descriptors | `tests/xi.Core.Tests/Fixtures/grapheme_descriptors/grapheme_descriptors.json` | `--grapheme-descriptors` | `grapheme_descriptors@1.0.0` | `a56d6e2489d4…` | `[MP-T4]` Grapheme fallback 遥测。 |
| Leaf Split Parity | `tests/xi.Core.Tests/Fixtures/leaf_split_parity_samples.json` | （共享 `--dir` 输出） | `leaf_split_parity@0.2.0` | `e15b2528c7f6…` | 追踪 Rust/C# 叶片拆分差异；刷新时与 Stage D 一并校验。 |

> 所有哈希采用 `sha256sum` 计算。刷新资产时需更新本表并在 PR 描述附带新旧哈希 diff，以便 QA 记录在 `[QA-IngestionSmoke]`。

---

## [StageD::FeatureGates] Feature Gate 策略
<a id="StageD::FeatureGates"></a>

| Gate | 默认 | 用途 | 备注 |
| --- | --- | --- | --- |
| `serde` | ✅（运行 Stage D 必须） | 启用所有 JSON 导出路径 | 关闭时 exporter 无法生成任何资产。 |
| `cursor_state` | ⛔ | 调试游标失效，扩充 descriptor payload | 仅在 `[MP-R8]` 调试时开启，并在 `[Fixture-FeatureGates]` 记录。 |
| `tree_builder_slice_trace` | ⛔ | 生成 `--tree-builder-trace` 样本 | 输出写入 `tests/xi.Core.Tests/Fixtures/ParityFixtures/tree_builder_trace`，供 `TreeBuilder` 研究。 |

开启额外 gate 时，需：

1. 在命令中追加对应 feature；
2. 在 `docs/architecture/fixtures/parity-fixture-schema.md` 与本节更新说明；
3. 在 `[QA-StageDManual]` 附上运行记录，确保 QA 能复现结果。

---

## [QA-IngestionSmoke] 夹具 Ingestion Smoke
<a id="QA-IngestionSmoke"></a>

| 步骤 | 命令 | 期望 |
| --- | --- | --- |
| 运行脚本 | `./scripts/refresh_serialization_fixtures.ps1 -Verbose -SkipRust -SkipDotnet`（如仅验证导入） | exporter 成功覆写所有 JSON，日志包含 CLI 标识符与 feature 列表。 |
| 校验哈希 | `sha256sum tests/xi.Core.Tests/Fixtures/**/*.json` | 输出应与 `[StageD::ParityAssets]` 表一致；如不一致需重新导出或更新表。 |
| 运行测试 | `dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter Serialization` | 所有 Stage D 测试通过；失败则回滚夹具并打开 `[MP-R9]` 风险。 |
| 记录结果 | 更新 `agents/qa-engineer.md`（“最近完成”）并在 `AGENTS.md` 工作日志写入 hash、命令与测试状态 | 提供 CI 链接或本地日志路径，供下次对照。 |

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
| --- | --- | --- | --- |
| 2025-11-19 | v2.0 | Architecture Mapper | 套用 `document-structure-template.md`：新增 front-matter、Stage D/QA anchors、资产哈希表、Feature Gate 策略、Automation backlog。 |
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
