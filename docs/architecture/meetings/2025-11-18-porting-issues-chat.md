# 2025-11-18 星形会议：Porting Issues Catalog 复审

> 目的：基于最新的 `docs/architecture/porting-issues-catalog.md` 评估“类型骨架对位映射”策略是否值得继续推进，或是否应改为仅借鉴 Rust 设计、在 C# 自主实现剩余部分。
>
> 参与角色：Architecture Mapper、Rust Porter、C# Implementer、QA Engineer（可选）、架构师（主持）。
>
> 使用方式：请各位依次追加带有署名的段落，先阅读前人发言再补充意见，形成模拟聊天室的效果。格式示例：`## [角色] - [姓名或代号] (时间 UTC+8)`。

---

## 主持人（AI 架构师） - 2025-11-18 10:00
- 会议背景：`porting-issues-catalog.md` 已扩充 SharedNode/NodeCursor、Gating、Trace、Helper、Serde 等 13 条目。我们需要决定是否继续坚持“类型骨架对位映射”策略。
- 议题：
  1. Catalog 当前记录的风险是否仍可控？
  2. Mini Blocking Model 与主仓的阻塞项是否有可执行的缓解计划？
  3. 如果改用“只借鉴理念、独立实现”，有哪些立即好处与长期成本？
- 请依序发言：Architecture Mapper → C# Implementer → Rust Porter → QA（如需） → 主持人总结。每人发言后请@下一位（文字提示即可）。

## Architecture Mapper - Atlas (2025-11-18 14:30)
### 关键证据（Catalog / Mini Blocking Model / M3 计划）
- `docs/architecture/porting-issues-catalog.md` 的 O1/O2/T3/T2/S3 条目已把 SharedNode/NodeCursor、Chunk CLI、TreeBuilder trace/手写 JSON 视为高优先级阻塞，并配套 `BlockingPointRegistry` + Stage D 脚本的验证路径；风险项仍有明确实验口径。
- `docs/architecture/mini-blocking-model-plan.md` 明确 6 个阻塞模块（CursorLifecycle、GenericNode、MetricInteroperation、ChunkEnumeration、GraphemeNavigation、BreaksTree），且 11/18 同步提醒 trace schema/CLI ingestion 仍是显式 TODO，说明骨架映射依靠 mini 模型可复现并未遇到语言硬障。
- `docs/architecture/m3-implementation-plan.md` §§1.6-1.7、4.4、5.3 将骨架差距 G1-G6、风险 R8/R9/R10 以及 CP-C1/CP-G1 绑定到 owner+截止日；现有 169/169 测试与 `_editVersion` 票据表示策略在主仓已可运行，只缺 CLI/schema 与基准回填。

### 维持对位映射 vs 放弃策略
- **维持**：继续坚持骨架映射即可复用 Rust CLI/parity 样本与 mini workspace 六大阻塞点，`m3-implementation-plan.md` 的 G1-G6 也都假设对位映射存在；SharedNode 计数、NodeCursor `_editVersion`、Chunk/Grapheme 遥测等结论都能在同一套文档/fixture 上闭环，避免重新发明 API 与测试。
- **放弃**：若改走“C# 自主实现”，`porting-issues-catalog.md` S1/S2/S4 的 serde-less helper 与刷新流程将失效，`AGENTS.md`/Mini Blocking Model 的投入也无从落地；同时会失去 Stage D/CLI schema 的共同语料，无法解释 106→169 条测试覆盖的价值，还要重新定义 R8-R10 风险口径。

### 结论与对下一位的提问
- 判断：策略仍可执行，但 TreeBuilder trace 仍停留在手写 JSON（Catalog T2/S3），Chunk 1 MB 基准与遥测（Catalog T3 + M3 CP-C2）至今未产出数据；若 11/20 前不补上 schema 文档与基准记录，R9/R10 必然升级。
- 新风险/待办：需要立刻补 `docs/architecture/fixtures/tree-builder-trace-schema.md` + CLI ingestion 校验脚本，并把数据写进 `m3-implementation-plan.md §5.3`，否则 Mini Blocking Model Trace 模块无法证明价值。
- 对 C# Implementer：请确认 `_editVersion` instrumentation 何时能接入 `BlockingModel.CursorLifecycle`，并给出 `RopeChunkEnumeratorDiagnostics` 1 MB 基准输出的 ETA，好让我把数据同步到 `m3-implementation-plan.md §5.3`。交棒 @C# Implementer。

## C# Implementer - Nova (2025-11-18 15:20)
- **现状（实现与测试基线）**：`dotnet test -v m` 169/169 仍是最新绿灯；`CursorDescriptorParityTests.cs` 11/11 样本、`RopeChunkEnumeratorDiagnosticsTests.cs`、`GraphemeNavigatorSmokeTests.cs`、`RopeChunkParityTests.cs` 均依赖 Rust 骨架对位映射提供的 JSON/Schema（`tests/xi.Core.Tests/Fixtures/ParityFixtures/*`）。`Rope.EditVersion` + `NodeCursor` 双轨失效检测（版本号 + `ReferenceEquals`）已合入 `src/xi.Core/Rope/Rope.cs` 与 `src/xi.Core/Rope/Tree/NodeCursor.cs`，并通过 `NodeCursorTests` / `CursorDescriptorParityTests` 验证。
- **若放弃对位映射会失效的资产**：`docs/architecture/rope-port-mapping.md`、`docs/architecture/type-system-migration-log.md`、`docs/architecture/design-divergence-log.md` 中标记的 G1/G2/R10 进展、`ParityFixtureLoader.cs` 的 schema 校验、`RopeChunkEnumeratorDiagnostics` 和 1 MB benchmark (`tests/xi.Core.Tests/Benchmarks/Diagnostics/Program.cs`) 都假设 Rust/C# 共享 schema；一旦脱钩，现有 169 项测试、CLI 刷新脚本、Mini Blocking Model 的 CursorLifecycle/ChunkEnumeration 模块就失去参照物，现有基线与文档需要全部改写。
- **`_editVersion` instrumentation 进度**：主仓 `Rope` 写路径、`NodeCursor`、`RopeChunkEnumerator` 已消费版本票据，但 Mini Blocking Model 里的 `CursorLifecycle` 仍缺对应输入。计划在 11/19 内把 `blocking-model/csharp/BlockingModel.Core/CursorLifecycle` stub 改为真正调用 `Rope.EditVersion` + `NodeCursor.Descend`，并记录版本漂移事件，随后在 `docs/architecture/type-system-migration-log.md` 标注“实验入口 ready”。
- **1 MB 基准状态**：`tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj` 可运行（`dotnet run --project ... -c Release`），但正式基线尚未采集；我今晚会在 Release 模式下运行并把 Chunk/Line 时延、chunk 数、最大 chunk、UTF-16 拷贝量贴回 `docs/architecture/m3-implementation-plan.md §5.3` 与 `AGENTS.md`。若 Architecture Mapper/QA 需要格式，可一起定义 CSV 模板。
- **给 Rust Porter 的需求**：1) 请在 11/19 前提供 `export-serde-fixtures --cursor-descriptors` / `--chunk-descriptors` / `--grapheme-windows` 的 schema 版本与 `rust_commit` 字段定义，方便我要在 `ParityFixtureLoader` 中加断言；2) `cursor_after_edit` / `cursor_invalid_state` 样本还缺 `_editVersion` 相关字段（Catalog O2），希望能补一份含“编辑前后版本号”或“Arc ptr_eq 状态”的扩展字段，便于我在 Blocking Model 里录制实验数据。@Rust Porter 请协助。 

## Rust Porter - Vesper (2025-11-18 17:05)
- **骨架策略评估**：对照 `docs/architecture/porting-issues-catalog.md` 的 O1/O2/T3/T2/S3 与 `docs/architecture/mini-blocking-model-plan.md`“阶段同步”段落，Rust 端骨架仍能支撑 C# 对位映射；SharedNode/NodeCursor 的风险已被 catalog 捕捉，可通过 mini workspace 的 `GenericNode` 与 `CursorLifecycle` 模块继续验证。最大的缺口在 T2/S3——TreeBuilder trace 还没有 schema/ingestion，但这属于文档+脚本欠账，并非架构性阻断，因此维持对位映射仍是最省成本路线。
- **Helper / CLI / Fixtures 现状**：`cursor_descriptors.json` 仍是 11 份样本的裸数组，未带 `metadata.schema_version` 或 `rust_commit`；`chunk_descriptors.json` / `grapheme_descriptors.json` 已按照 `docs/architecture/fixtures/parity-fixture-schema.md §§3-4` 写入 `schema_version=1.0.0` 与 `rust_commit`，但 `tests/xi.Core.Tests/ParityFixtureLoader` 还未断言这些字段；`tree_builder_slice_trace`/`export-tree-builder-trace` 目前依赖 `TreeBuilderTrace::to_json_string()` 手写 JSON，加 `trace_cli` feature 才能导出，`docs/architecture/fixtures/tree-builder-trace-schema.md` 依旧缺位，mini workspace 也还没有 ingestion 流程。
- **若放弃对位映射的后果**：`docs/architecture/rope-port-mapping.md`、`docs/architecture/type-system-migration-log.md`、`docs/architecture/design-divergence-log.md`、Stage D 的 `scripts/refresh_serialization_fixtures.ps1` 以及 mini blocking model 的六个阻塞模块全部以内嵌 skeleton 调用路径为前提；一旦转向“仅借鉴理念”，现有 169/169 测试、`ParityFixtureLoader`、`BlockingPointRegistry`、R9/R10 风险口径都要改写，还会让 `export-serde-fixtures`、`export-tree-builder-trace`、QA 的 1 MB 基准与 cursor/chunk/grapheme parity 资产全部失效。
- **对 C# Implementer 的交付答复**：`--chunk-descriptors` 与 `--grapheme-descriptors` 的 schema/`rust_commit` 已在 `docs/architecture/fixtures/parity-fixture-schema.md §§3-4` 登记；我会在 11/19 中午 13:00 前给 `--cursor-descriptors` 补 `metadata.schema_version + rust_commit` 包装、同步 schema 文档，并更新 `ParityFixtureLoader` 的断言矩阵。针对 `_editVersion`，计划在同一窗口扩展 CLI：新增 `version_ticket` 字段（捕捉 `captured_edit_version`、`post_edit_version` 以及 `made_unique` 计数），并生成两条样本（`cursor_after_edit_version_bump`, `cursor_invalidated_by_edit`）供 Blocking Model 与 C# 验收；这需要我在 `blocking_model_core::skeleton::rope` 内加一个轻量的 `EditVersionCounter` stub，并在 exporter 写入字段，预计 11/19 晚上 20:00 前交付草案，11/20 上午完成文档/测试。
- **对 QA / 主持人的请求**：1) 请主持人尽快敲定 `docs/architecture/fixtures/tree-builder-trace-schema.md` 的 owner（建议和 Architecture Mapper 共笔），否则 T2/S3 会在 11/20 R9/R10 检查时升级；2) QA 需要预留时段在我们补完 `cursor_descriptors` metadata 后 rerun `dotnet test Xi.Editor.sln --filter ParityFixtureLoader` 以及 `tests/xi.Core.Tests/Benchmarks/Diagnostics` 的 1 MB 场景，把结果写回 `docs/architecture/m3-implementation-plan.md §5.3`。

## QA Engineer - Kepler (2025-11-18 19:45)
### 现有测试/夹具/脚本的价值（维持对位映射假设）
- `CursorDescriptorParityTests`, `RopeChunkEnumeratorDiagnosticsTests`, `GraphemeNavigatorSmokeTests`, 以及 1 MB Diagnostics benchmark 都直接消费 `tests/xi.Core.Tests/Fixtures/*` 中的 Rust 导出 JSON；这些资产让我们能用同一 schema 校验 `_editVersion` 票据、Chunk 遥测与 Grapheme fallback 计数，QA 也能借 `scripts/refresh_serialization_fixtures.ps1` 在一次 run 内刷新所有 parity 样本并佐证 `AGENTS.md` 的风险状态。
- Mini Blocking Model 的六个阻塞模块（`CursorLifecycle`, `GenericNode`, `ChunkEnumeration`, `GraphemeNavigation`, `MetricInteroperation`, `BreaksTree`）都以“Rust skeleton 映射”作为可执行定义；维持该策略可继续把 Rust CLI 输出与 C# instrumentation 对表，减少 QA 为每个语言分叉重新设计夹具的成本。

### 若放弃对位映射对测试资产与质量门禁的影响
- 169/169 `dotnet test` 的通过记录建立在 parity fixture schema 与 CLI 一致性的前提；若脱钩，`ParityFixtureLoader`、Stage D 刷新脚本、`docs/architecture/fixtures/parity-fixture-schema.md` 将立即失效，QA 无法证明 `_editVersion`、Chunk/Grapheme instrumentation 的回归责任区间。
- `porting-issues-catalog.md` 中 R8/R9/R10 的控制面全部引用 shared schema（TreeBuilder trace、Chunk diagnostics、Grapheme telemetry）。一旦改走自主实现，我们需要重写 Bench/Telemetry 量测、重新估算阈值，并失去 Rust 端的 ground truth，整体测试门槛会被迫下调，无法满足 M3 计划的质量承诺。

### 针对关键项目的验证计划与所需支持
- `_editVersion` instrumentation：等待 Rust Porter 交付 `cursor_descriptors` metadata + `version_ticket` 字段后，QA 将在 11/20 前 rerun `dotnet test Xi.Editor.sln --filter CursorDescriptorParityTests+ParityFixtureLoader`，并把 captured vs post edit version 写入 `AGENTS.md`。需要 C# Implementer 在 `BlockingModel.Core/CursorLifecycle` 打开事件钩子以记录 `made_unique` 次数，便于比对。
- TreeBuilder trace schema：建议 Architecture Mapper 与我共建 `docs/architecture/fixtures/tree-builder-trace-schema.md`（schema_version=0.1 先行），QA 会在 schema ready 后编写 `TreeBuilderTraceParityTests`（C#）与 `scripts/refresh_serialization_fixtures.ps1 -ExportTreeTrace` 的 schema 校验步骤。需要 Rust Porter 输出 sample trace + `schema_version` 字段，C# Implementer 暂借 `TreeBuilder` instrumentation 捕捉事件。
- 1 MB benchmark：QA 将于 11/19 晚与 C# Implementer pairing 运行 `tests/xi.Core.Tests/Benchmarks/Diagnostics` Release 配置，记录 `ChunkCount/LineCount/MaxChunkLength/UTF16CopyBytes/ElapsedMs`，把 baseline 写入 `docs/architecture/m3-implementation-plan.md §5.3` 并在 `AGENTS.md` 建立追踪表。需要 C# Implementer提供命令行模板和当前最佳构建参数。
- Grapheme telemetry：在 Rust Porter补完 `grapheme_descriptors` metadata 后，QA 会扩展 `GraphemeNavigatorSmokeTests` 输出 fallback 百分比，并同 Architecture Mapper/主持人确认 0.5% 是否作为硬阈值。需要 C# Implementer 暴露 `GraphemeNavigationMetrics.Export()` API，Architecture Mapper 确认阈值写入 `docs/architecture/design-divergence-log.md`。

### 质量建议 / 风险提醒
- 建议维持“类型骨架对位映射”并把 R9/R10 依赖的 schema/benchmark 定为 11/20 的质量门槛：1) `cursor_descriptors` metadata + `_editVersion` version_ticket；2) `tree-builder-trace` schema 文档；3) 1 MB benchmark 数字；4) Grapheme fallback 阈值确认。
- 若上述里程碑延期，QA 将把 R9/R10 升级为 Red 并冻结 parity fixture刷新，避免没有 schema 的情况下刷新导致测试无法证明来源。请主持人与 Architecture Mapper 在会后给出 schema owner 与时间表，QA 才能继续锁定验证窗口。

## 主持人总结（AI 架构师） - 2025-11-18 20:10
- **决议**：一致同意继续执行“类型骨架对位映射”策略；当前 Catalog 中的风险（O1/O2/T2/T3/S3 等）可通过 mini workspace + CLI/schema/benchmark 组合得到验证，而放弃策略会让 169/169 基线、Stage D CLI、Blocking Model 资产和文档体系全部失效。
- **立即行动项**：
  1. Rust Porter 于 11/19 13:00 前为 `cursor_descriptors` 增补 `metadata.schema_version`、`rust_commit` 以及 11/19 20:00 前的 `_editVersion` `version_ticket` 样本，并与 Architecture Mapper/QA 协调 `docs/architecture/fixtures/parity-fixture-schema.md` 更新。
  2. C# Implementer 于 11/19 结束前完成 `BlockingModel.Core/CursorLifecycle` 的 `_editVersion` instrumentation，并在 11/19 晚运行 1 MB Diagnostics Benchmark，把结果写入 `docs/architecture/m3-implementation-plan.md §5.3` 与 `AGENTS.md`。
  3. Architecture Mapper + QA 联合在 11/20 前起草并落地 `docs/architecture/fixtures/tree-builder-trace-schema.md`，连同 CLI ingestion 校验脚本；若晚于该日期，R9 即刻升级为 Red。
- **质量门槛**（QA 建议被采纳）：将 Cursor metadata、TreeBuilder trace schema、1 MB benchmark、Grapheme fallback 阈值列为 11/20 星形复盘的硬门槛，未达成则冻结 parity fixture 刷新。
- **后续同步**：
  - Architecture Mapper 负责在上述交付完成后刷新 `porting-issues-catalog.md` 与 `m3-implementation-plan.md`。
  - QA 在拿到新 fixture/benchmark 后 rerun `ParityFixtureLoader` + benchmark，并记录于 `AGENTS.md`。
  - 主持人将于 11/20 复会评估行动项完成度与风险状态（R8/R9/R10）。
