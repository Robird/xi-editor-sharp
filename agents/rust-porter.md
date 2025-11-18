---
identity:
  role: Rust 端 helper 重构与移植改造专家
  project: xi-editor-sharp
  reports_to: AI 架构师 (主 Agent)
  partners:
    - C# Implementer
    - Architecture Mapper
    - QA/Test Engineer
    - Stage D Coordinator
  start_date: 2025-11-16
responsibilities:
  - 抽离与稳固 Rust helper 模块，保持行为等价
  - 设计/维护 feature gate、CLI 与 serde fixture 输出
  - 提供 C# 实装所需算法参考、parity 资产与测试信号
  - 以 Stage D anchor 作为事实源同步 Goal Tree 与文档
interfaces:
  - Architecture Mapper：`docs/architecture/rope-port-mapping.md`、`StageD::*` 锚点
  - C# Implementer：`tests/xi.Core.Tests/Fixtures/*`、`docs/csharp-refactor/*`
  - QA/Test Engineer：`scripts/refresh_serialization_fixtures.ps1`、`export-serde-fixtures` CLI
  - AI 架构师：SubAgent 汇报与 Goal Tree 状态
cadence:
  status_update: 任务完成当日刷新本档案 + Goal Tree (≤24h)
  fixtures_refresh: parity 资产变更即刻 rerun CLI + manifest 写回
  testing: `cargo test -p xi-rope` 覆盖任意 helper/CLI 变更
---

# Rust Porter 档案

## 当前聚焦 / 阻塞（2025-11-19）

1. **Sprint 1 Ready Queue (#3/#4) 交付**  
  - `docs/sprints/sptrint-1.md#Ready Queue` 已登记 “Stage D exporter 默认开启 + metric_windows manifest 字段” 与 “Stage D CLI schema & feature gate spec drop”；两项任务均由 Rust Porter SubAgent 承担。  
  - 交付目标：更新 `xi-editor-ph7/rust/export-serde-fixtures` 默认 flag、写入 `metric_windows[]`（含 schema 版本）、刷新 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`、补齐 `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::ParityAssets]` 与 `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::FeatureGates]` 以及 `docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]` 的 schema/snippet，并把 `goal_tree_sync.py --check` 维持为 green。  

2. **Stage D exporter / manifest 单一事实源**  
  - `python scripts/refresh_all_assets.py --only stage-d-fixtures` 触发 `xi-editor-ph7/rust/rope/src/bin/export-serde-fixtures.rs` 与 `scripts/refresh_serialization_fixtures.ps1`，导出所有 Stage D 资产并写入 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`。  
  - 当前 manifest 锁定 `rust_commit=3799d2be9db0ef040517ed69df1b717e96a8958e`、`cli_rev=0.3.0`，默认 feature gates `"cursor_state","serde","tree_builder_slice_trace"`。  
  - 下一步：把 manifest 计数/哈希同步到 `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::ParityAssets]` 与 `docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]`，并让 Architecture Mapper/Goal Tree anchor 直接引用该事实源。

3. **cursor_state gate & CursorDescriptor v2**  
  - `cursor_descriptors.json` 已扩展至 12 份样本；`cursor_state` gate 仍需覆盖 Breaks metric、Utf16 失效路径（参见 `docs/rust-refactor/CursorCache.md`）。  
  - 需在 `scripts/refresh_serialization_fixtures.ps1` 里默认执行 `cargo test -p xi-rope --features serde,cursor_state -- cursor_descriptor`，并准备 schema v1.3 的字段表与哈希变更流记录给 Stage D anchors。

4. **MetricAdapter / Breaks helper 对齐**  
  - Breaks/Lines/Utf16 helper 已模块化（`xi-editor-ph7/rust/rope/src/metrics/*`，`docs/rust-refactor/breaks-metrics-templating.md`），但 Stage D exporter 尚未输出 MetricAdapter 需要的 offsets/line counts。  
  - Architecture Mapper 需定义 manifest 字段命名，C# Implementer 要在 `StageDDescriptorHydrator` / `MetricAdapter` 中消费；QA 需在 `[QA-ChunkBench]`/`[QA-IngestionSmoke]` 中登记 ingest 结果。

5. **Stage D automation / QA 证据链**  
  - `scripts/refresh_serialization_fixtures.ps1` 已串联 loader → hydrator → manifest verifier → inspector，但 QA 仍需把 inspector 输出写到 `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[QA-IngestionSmoke]`。  
  - 依赖：Architecture Mapper 更新 Goal Tree anchors；C# Implementer 扩展 hydrator ledger 字段；QA Engineer 记录 `dotnet test Xi.Editor.sln --filter StageDDescriptor` 与 `python scripts/verify_fixture_manifest.py --manifest ...` 的最新 run。

  ## 对接 / 依赖（2025-11-19）
  - **C# Implementer**：`StageDDescriptorHydrator` 需接收 manifest ledger 新字段、`MetricAdapter` 需要 `metric_windows[]`；cursor_state schema bump 也要同步到 `NodeCursorState` 文档（`docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::ParityAssets]`）。
  - **QA Engineer**：负责在 `scripts/refresh_serialization_fixtures.ps1` 中保留 loader/hydrator/manifest verifier/inspector 步骤，并将输出附在 `[QA-IngestionSmoke]`；另需为 `cargo test -p xi-rope --features serde,cursor_state -- cursor_descriptor` 建立 smoke 记录。
  - **Architecture Mapper**：更新 `docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]`、Goal Tree anchors 与 `AGENTS.md` manifest 快照；同时敲定 MetricAdapter 字段命名及 `iterator_facade` 降级策略。

## Helper / CLI / Schema 状态（2025-11-19）

### Stage D Exporter + Manifest
- CLI：`cargo run -p xi-rope --features serde,cursor_state,tree_builder_slice_trace --bin export-serde-fixtures -- --cursor-descriptors --chunk-descriptors --grapheme-descriptors --breaks-descriptors --diff-regions --search-spans --tree-builder-trace --emit-manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`。  
- Snapshot：chunk=20、cursor=12、grapheme=668、breaks/diff/search=3、tree_builder_trace=3；`payload_hash`/`schema_hash` 详见 manifest。  
- Hash/时间戳策略：读取既有 JSON 的 `generated_at_unix_millis`，仅在缺失时回落当前时间，防止 Stage D inspector/loader hash 漂移。  
- 文档锚点：`docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::FixtureFlow]`, `docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]`, `docs/architecture/fixtures/parity-fixture-schema.md`。

### Cursor / cursor_state helpers
- `CursorDescriptor` + `CursorState` 实现在 `xi-editor-ph7/rust/rope/src/cursor.rs` 与 `serde_fixtures/cursor_descriptors.rs`，`cursor_state` gate 仅在 Stage D 导出时启用。  
- 需要在 CLI 中注入 Breaks metric/Utf16 失效样本，并 bump `cursor_descriptors@1.3.0`（待 Architecture Mapper/C# Implementer 共同确认字段）。  
- 验证命令：`cargo test -p xi-rope --features serde,cursor_state -- cursor_descriptor`（待脚本默认执行）。

### Metrics / MetricAdapter
- Rust metrics helper：`xi-editor-ph7/rust/rope/src/metrics/{codepoint,lines,break_indices,identity}.rs`；`docs/rust-refactor/breaks-metrics-templating.md` 记录 shim 要求。  
- 下一步：在 `--breaks-descriptors` 输出中附加 line/utf16 offsets、cursor hints，供 C# `MetricAdapter`（`docs/architecture/rope-port-mapping.md#[RPM-Matrix]`）复原；需要 QA 在 `[QA-ChunkBench]` 对 ingest 结果做 smoke。  
- 风险：Manifest 尚无专用字段，需 Architecture Mapper 定义 `metric_windows[]` schema。

### TreeBuilder Slice Trace
- Feature gate：`tree_builder_slice_trace`；样本 `tests/xi.Core.Tests/Fixtures/tree_builder_slice/basic_slice_plan.json`，哈希 `22724af7fe8b22e4e1dbd01f86ba1b3902a0ccf777944b5b29081259927c3a6e`。  
- C# loader：`src/xi.Core/Rope/Diagnostics/TreeBuilder/TreeBuilderSliceTraceLoader.cs`，消费 manifest ledger。  
- 待办：将事件计数/版本写入 `[StageD::ParityAssets]`，供 `[TS-B2]` 佐证。

## Feature Gates
| Feature Gate | 用途 | 状态（2025-11-19） | 触发 / Owner |
| --- | --- | --- | --- |
| `serde` | Stage D exporter + manifest 输出 | `scripts/refresh_serialization_fixtures.ps1` 默认开启（含 loader/hydrator 验证） | Rust Porter |
| `cursor_state` | 深层游标 state dump + CLI fixtures | Stage D 刷新时强制开启，workstation 默认关闭 | Rust Porter（负责 CLI），C# Implementer（消费），QA（记录） |
| `tree_builder_slice_trace` | TreeBuilder trace fixture | Stage D 流程开启，通常与 `--tree-builder-trace` 同时传入 | Architecture Mapper 请求，Rust Porter 维护 |
| `iterator_facade` (计划) | Delta/chunk visitor façade | 方案已在 `docs/rust-refactor/iterator-facade-export.md`，尚未实现 | Rust Porter |
| `grapheme_windows` (计划) | Grapheme window parity CLI | 设计完成，等待 QA 设置 fallback 阈值后开 gate | Rust Porter + QA |

## Stage D 输出接口（Rust → C# / QA）
1. **Exporter 调用**：`cargo run -p xi-rope --features serde,cursor_state,tree_builder_slice_trace --bin export-serde-fixtures -- --cursor-descriptors --chunk-descriptors --grapheme-descriptors --breaks-descriptors --diff-regions --search-spans --tree-builder-trace --emit-manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`。所有 flag 记录在 `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::FixtureFlow]`。
2. **Manifest Ledger**：`tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 存储 `rust_commit`, `cli_rev`, `feature_gates`, `fixtures[].{name,path,count,schema_hash,payload_hash}`；`python scripts/verify_fixture_manifest.py --manifest …`（自动由脚本调用）校验 canonical hash，并供 `StageDDescriptorLoader` ledger 校对。
3. **脚本链路**：`scripts/refresh_serialization_fixtures.ps1` 负责 run_all_checks → exporter → manifest → `dotnet test Xi.Editor.sln --filter StageDDescriptor` → hydrator smoke → manifest verifier → `dotnet run --project tools/StageDDescriptorInspector -- --fixtures ...`；`python scripts/refresh_all_assets.py --only stage-d-fixtures` 默认调用该脚本。
4. **文档锚点**：`docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]`、`docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::ParityAssets]`、`docs/architecture/design-divergence-log.md#[Div-Active]` 追踪变更与降级；Architecture Mapper 负责把 manifest 摘要写入 Goal Tree anchors。
5. **QA 证据链**：`[QA-IngestionSmoke]` 要求粘贴最新 loader/hydrator/inspector 输出与 manifest 校验结果；若 hash 漂移，QA 负责升级 `StageD::FixtureFlow` 风险并通知 Rust Porter 回滚/再导出。

## 最近完成
- **2025-11-19 – Stage D CLI schema drop**：同步 `metric_windows[]` ledger、默认 CLI flag 表与 `cursor_state`/`tree_builder_slice_trace`/`grapheme_windows` 样例——更新 `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::FeatureGates]`、`docs/architecture/fixtures/parity-fixture-schema.md#[Fixture-MetricWindows]`、`docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]`，让 C#/QA/Architecture 以 manifest 为唯一引用。 
- **2025-11-19 – Sprint 1 Ready Queue 提案**：依据知识更新输出将 “Stage D exporter 默认开启 + metric_windows manifest 字段” 与 “Stage D CLI schema & feature gate spec drop” 追加到 `docs/sprints/sptrint-1.md#Ready Queue`，并在 `docs/meetings/2025-11-19-knowledge-refresh-chat.md#Rust Porter` 记录后续行动，方便 Architecture Mapper / QA / C# 跟进依赖。
- **2025-11-19 – 档案瘦身 + manifest 快照**：阅读 `AGENTS.md`、`tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 与 `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::ParityAssets]`，重写本档案“当前聚焦”“Helper/CLI 状态”并记录 manifest `rust_commit=3799d2be9db0ef040517ed69df1b717e96a8958e`、默认 gates `cursor_state|serde|tree_builder_slice_trace`；同批准备 `docs/meetings/2025-11-19-knowledge-refresh-chat.md#rust-porter` 更新。
- **2025-11-19 – Stage D manifest + inspector doc sync**：依 `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::StageDChecklist]` 执行 `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 与 `dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures`，将 chunk/grapheme/breaks/diff/search ledger 哈希写回 `[StageD::ParityAssets]`，并在 `[QA-IngestionSmoke]` 描述 inspector 摘要需求。
- **2025-11-19 – Manifest verifier 自动化**：与 QA/C# 协作扩展 `scripts/refresh_serialization_fixtures.ps1`，默认在 exporter 后调用 loader/hydrator/manifest verifier/inspector，提供 `-SkipManifestVerification`、`-SkipStageDLoaderTest` 等开关；手册在 `[StageD::FixtureFlow]`/`[QA-IngestionSmoke]` 记录命令链。
- **2025-11-18 – Stage D exporter timestamp 稳定化**：在 `xi-editor-ph7/rust/rope/src/serde_fixtures/{chunk_descriptors,grapheme_descriptors,breaks_descriptors,diff_regions,search_spans}.rs` 引入 `generated_at_unix_millis` 复用逻辑，避免 hash 因时间戳漂移；`./run_all_checks --filter serde-fixtures` 验证 clippy/tests/exporter。
- **2025-11-18 – Manifest updater + TreeBuilder trace loader**：`export-serde-fixtures` 写入 `tree_builder_slice_trace@1.0.0`，`scripts/verify_fixture_manifest.py --update` 支持 canonical diff，C# 侧 `TreeBuilderSliceTraceLoader` + tests（`dotnet test --filter TreeBuilderSliceTraceLoaderTests`）完成 ingestion，并在 `docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]` 标记状态。

## 待办 / 风险
- [TODO] **`cursor_descriptors` v1.3**：扩充 Breaks metric/Utf16 失效样本并 bump schema hash；依赖 Architecture Mapper 确认字段、C# Implementer 更新 `StageDDescriptorHydrator` 映射、QA 在 `[QA-IngestionSmoke]` 记录 ingest。
- [TODO] **MetricAdapter metadata**：为 `--breaks-descriptors` 附带 `metric_windows[]`（line/utf16 offsets、cursor hints），并在 manifest 中暴露；待 Architecture Mapper 定义 schema，C# Implementer/QA 验证 `MetricAdapter`/ingestion。
- [TODO] **Stage D manifest → Goal Tree**：要求 Architecture Mapper/AI Architect 把 manifest counts/hash 写回 Goal Tree anchors（`m3-implementation-plan.md#[MP-Tx]`），并在 `AGENTS.md` 记录刷新时间戳。
- [RISK] **Iterator façade缺位**：若 `iterator_facade` gate 未在 M2/M3 内推出，C# chunk iterator 性能将继续受限（参考 `docs/rust-refactor/iterator-facade-export.md`）；需在 Stage D anchors记录降级策略。
- [RISK] **QA 证据链不完整**：若 QA 未在 `[QA-IngestionSmoke]` 附加 Stage D inspector/manifest 验证日志，则无法证明资产未漂移；Rust 侧需暂停刷新直至证据补齐。

## 关键文档索引
- `docs/csharp-refactor/rope-serialization-fixture-playbook.md`：`[StageD::FixtureFlow]`、`[StageD::ParityAssets]`、`[QA-IngestionSmoke]` 记录 exporter/脚本/QA 证据。
- `docs/architecture/rope-port-mapping.md`：`[RPM-ParityAssets]`/`[RPM-Matrix]` 映射 Rust helper → C#，对应 MetricAdapter backlog。
- `docs/rust-refactor/CursorCache.md` / `docs/rust-refactor/breaks-metrics-templating.md`：cursor_state 与 MetricAdapter 语义真源。
- `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`：Stage D manifest ledger（counts/hash/feature gates）。
- `scripts/refresh_serialization_fixtures.ps1`、`python scripts/refresh_all_assets.py`：Stage D 自动化入口，负责 run_all_checks + exporter + manifest + loader/hydrator/inspector。
- `xi-editor-ph7/rust/rope/src/bin/export-serde-fixtures.rs`, `xi-editor-ph7/rust/rope/src/serde_fixtures/*.rs`：CLI 实现与 schema 定义；`goal_tree_sync.py` 读取 manifest 以保持 Goal Tree 一致。
