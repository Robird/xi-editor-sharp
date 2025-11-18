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

## 当前聚焦 / 阻塞（2025-11-20）

1. **Stage D exporter + manifest 真源稳定**  
  - 最新一轮 `python scripts/refresh_all_assets.py --only stage-d-fixtures` 已导出 Breaks/Diff/Search/TreeBuilder 资产并刷新 manifest（`rust_commit=b6fb5999288946daeeadc4b7f9f9756f6dda1f50`，feature gates `cursor_state|serde|tree_builder_slice_trace`，`metric_windows` 已登记 7 项窗口）。  
  - 需要把 exporter 默认 flag 固定为 `--cursor-descriptors --chunk-descriptors --grapheme-descriptors --breaks-descriptors --diff-regions --search-spans --tree-builder-trace --emit-manifest ...`，同时在 clippy lint 再次浮现前保持 `run_all_checks --filter serde-fixtures` 绿灯，避免 `refresh_all_assets` 再次在 Stage D 步骤退出。  
  - 本周交付目标：将 manifest 快照同步到 `StageD::ParityAssets`、`RPM-ParityAssets`、Goal Tree anchors，并确保 `stage-d-inspector-latest.txt` 对应的 ledger SHA 与 manifest 一致。

2. **CursorState schema v1.3 准备**  
  - 需要在 `cursor_descriptors.json` 里新增 Breaks metric / Utf16 失效字段并 bump 到 `cursor_descriptors@1.3.0`，同时保持旧字段向后兼容；`cargo test -p xi-rope --features serde,cursor_state -- cursor_descriptor` 将作为 schema bump 的最小验证。  
  - 阻塞：Architecture Mapper 正在确认字段命名（MetricAdapter 也会消费），C# Implementer 等待 `StageDDescriptorHydrator` 的映射表，QA 必须在 `[QA-IngestionSmoke]` 更新 ingest 日志。  
  - 在 schema 投放前需输出迁移说明（字段含义 + 旧版 hash → 新版 hash）以便 Goal Tree/Stage D 手册引用。

3. **Breaks/Diff/Search payload 与 MetricAdapter 带宽**  
  - Exporter 已生成 3×3 条 payload 与 `metric_windows` 汇总，但 C# 端 `StageDDescriptorHydrator` 尚未消费 `metric_windows` 表，QA 也未把 ingest smoke 写入 `[QA-ChunkBench]`/`[QA-IngestionSmoke]`。  
  - 需要与 C# Implementer 确认 hydrator DTO/MetricAdapter 接口，再由 QA 记录 `StageDDescriptorLoader|Hydrator` run log，形成“Rust exporter → manifest → loader/hydrator → QA 证据”闭环。  
  - 若 MetricAdapter 迟迟不接收这些窗口，后续 chunk bench / diff 诊断将缺少真值，Stage D anchors 无法证明 Breaks/Diff/Search parity。

  ## 对接 / 依赖（2025-11-20）
  - **C# Implementer**：扩展 `StageDDescriptorHydrator`、`MetricAdapter` 以读取 manifest ledger / `metric_windows`；需要在 hydrator tests 中断言 Breaks/Diff/Search payload 计数与 hash。  
  - **QA Engineer**：持续运行 `dotnet test Xi.Editor.sln --filter StageDDescriptor`、`python scripts/verify_fixture_manifest.py --manifest ...` 与 `dotnet run --project tools/StageDDescriptorInspector`，并把输出写到 `[QA-IngestionSmoke]` 与 `tests/xi.Core.Tests/Fixtures/Reports/`；若日志缺失需回填。  
  - **Architecture Mapper**：负责在 `docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]`、Goal Tree YAML 与 `AGENTS.md` 中登记 manifest 快照/metric windows schema，确认 CursorState v1.3 字段名与 `iterator_facade`/MetricAdapter 依赖。

## Helper / CLI / Schema 状态（2025-11-20）

### Stage D Exporter + Manifest
- 默认命令：`cargo run -p xi-rope --features serde,cursor_state,tree_builder_slice_trace --bin export-serde-fixtures -- --cursor-descriptors --chunk-descriptors --grapheme-descriptors --breaks-descriptors --diff-regions --search-spans --tree-builder-trace --emit-manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`。  
- 当前 manifest：`rust_commit=b6fb5999288946daeeadc4b7f9f9756f6dda1f50`、`cli_rev=0.3.0`、feature gates `cursor_state|serde|tree_builder_slice_trace`。Fixtures：chunk=20、cursor=12、grapheme=668、breaks/diff/search=3、tree_builder_trace=3；`payload_hash` 详见 ledger。  
- `metric_windows` 已随 manifest 写入 7 组窗口（chunk/line/grapheme/break/diff/search/tree-builder），供 MetricAdapter/QA 引用；若窗口与 payload count 不符，需立即阻断刷新。  
- 文档入口：`docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::FixtureFlow]/[StageD::ParityAssets]`、`docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]`、`docs/architecture/fixtures/parity-fixture-schema.md#[Fixture-MetricWindows]`。

### Cursor / CursorState Schema
- 现有 schema：`cursor_descriptors@1.2.0`，12 条样本覆盖深树 + chunk 接缝，`cursor_state` gate 仅在 Stage D 导出时启用。  
- 计划中的 v1.3 将添加 Breaks metric hints、Utf16 失效窗口及 `metric_windows` entry，要求 CLI 与 manifest 同步更新。  
- 验证路径：`cargo test -p xi-rope --features serde,cursor_state -- cursor_descriptor` + `dotnet test Xi.Editor.sln --filter StageDDescriptor`；`scripts/refresh_serialization_fixtures.ps1` 需默认执行该组合。  
- 文档需在 schema bump 前补充字段表与兼容性说明（Goal Tree、Stage D playbook、`docs/rust-refactor/CursorCache.md`）。

### Breaks/Diff/Search Payload + Metric Windows
- Rust helpers：`rope/src/metrics/{codepoint,lines,break_indices,identity}.rs` + `serde_fixtures/{breaks_descriptors,diff_regions,search_spans}.rs` 已稳定导出 payload 与 `metric_windows`。  
- C# 侧仍缺少对 `metric_windows` 的消费；`StageDDescriptorHydrator` 与 `MetricAdapter` 需据 manifest ledger 生成 typed DTO，并在 tests 中断言计数/哈希。  
- QA 需把 `dotnet test --filter StageDDescriptorLoader|Hydrator`、`python scripts/verify_fixture_manifest.py`、`StageDDescriptorInspector` 输出贴到 `[QA-IngestionSmoke]`/`[QA-ChunkBench]` 以证明 Breaks/Diff/Search ingest。  
- 若没有这些记录，下一轮 CLI schema 调整将缺少回归基线。

### TreeBuilder Slice Trace
- Feature gate：`tree_builder_slice_trace` 与 `--tree-builder-trace` flag 成对出现，fixture `basic_slice_plan.json`（hash `22724af7fe8b22e4e1dbd01f86ba1b3902a0ccf777944b5b29081259927c3a6e`）继续作为 `[TS-B2]` 例证。  
- Loader：`src/xi.Core/Rope/Diagnostics/TreeBuilder/TreeBuilderSliceTraceLoader.cs` + tests ingest manifest ledger；需在下一轮 Stage D doc 更新里记录事件计数与 CLI 版本。  
- 待办：当树构建 tracer扩展时，Rust 侧要更新 manifest + schema，并在 `StageD::ParityAssets` 标注 hash 变更轨迹。

## Feature Gates
| Feature Gate | 用途 | 状态（2025-11-20） | 触发 / Owner |
| --- | --- | --- | --- |
| `serde` | Stage D exporter + manifest 输出 | `refresh_serialization_fixtures.ps1`/`refresh_all_assets` 默认启用，含 loader/hydrator/inspector smoke | Rust Porter |
| `cursor_state` | 深层游标 state dump + CLI fixtures | Stage D 步骤强制开启；schema v1.3 设计中，等待字段确认 | Rust Porter（CLI）、C# Implementer（消费）、QA（记录） |
| `tree_builder_slice_trace` | TreeBuilder trace fixture | 随 Stage D export 启用，`basic_slice_plan` 作为唯一样本；下一版若扩容需同步 manifest | Architecture Mapper 请求，Rust Porter 维护 |
| `iterator_facade` (计划) | Delta/chunk visitor façade | 方案在 `docs/rust-refactor/iterator-facade-export.md`，尚未编码，预估 M3/M4 | Rust Porter |
| `grapheme_windows` (计划) | Grapheme window parity CLI/Telemetry | 门槛：QA 给出 `[QA-Telemetry]` 阈值与落地脚本；当前仍在 backlog | Rust Porter + QA |

## Stage D 输出接口（Rust → C# / QA）
1. **Exporter 调用**：`cargo run -p xi-rope --features serde,cursor_state,tree_builder_slice_trace --bin export-serde-fixtures -- --cursor-descriptors --chunk-descriptors --grapheme-descriptors --breaks-descriptors --diff-regions --search-spans --tree-builder-trace --emit-manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`。所有 flag 记录在 `docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::FixtureFlow]`。
2. **Manifest Ledger**：`tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 存储 `rust_commit`, `cli_rev`, `feature_gates`, `fixtures[].{name,path,count,schema_hash,payload_hash}`；`python scripts/verify_fixture_manifest.py --manifest …`（自动由脚本调用）校验 canonical hash，并供 `StageDDescriptorLoader` ledger 校对。
3. **脚本链路**：`scripts/refresh_serialization_fixtures.ps1` 负责 run_all_checks → exporter → manifest → `dotnet test Xi.Editor.sln --filter StageDDescriptor` → hydrator smoke → manifest verifier → `dotnet run --project tools/StageDDescriptorInspector -- --fixtures ...`；`python scripts/refresh_all_assets.py --only stage-d-fixtures` 默认调用该脚本。
4. **文档锚点**：`docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]`、`docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::ParityAssets]`、`docs/architecture/design-divergence-log.md#[Div-Active]` 追踪变更与降级；Architecture Mapper 负责把 manifest 摘要写入 Goal Tree anchors。
5. **QA 证据链**：`[QA-IngestionSmoke]` 要求粘贴最新 loader/hydrator/inspector 输出与 manifest 校验结果；若 hash 漂移，QA 负责升级 `StageD::FixtureFlow` 风险并通知 Rust Porter 回滚/再导出。

## 最近完成
- **2025-11-20 – Stage D refresh_all_assets 解锁**：修复 `xi-editor-ph7/rust/rope/src/serde_fixtures/cursor_descriptors.rs` clippy 告警后，重新执行 `./run_all_checks --filter serde-fixtures`、`python scripts/refresh_all_assets.py --only stage-d-fixtures`、`dotnet test Xi.Editor.sln --filter StageDDescriptor(Loader|Hydrator)`，确认 loader → hydrator → manifest verifier → inspector 流水线全绿，产出 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`（`rust_commit=b6fb5999288946daeeadc4b7f9f9756f6dda1f50`）。
- **2025-11-20 – Stage D 文档/测试同步**：依最新刷新结果更新 Stage D loader/hydrator常量、`docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::ParityAssets]/[QA-IngestionSmoke]` 哈希表，并复核 `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt` 与 manifest ledger 对齐。
- **2025-11-19 – Stage D CLI schema更新**：发布 `metric_windows[]` ledger、默认 CLI flag 表与 `cursor_state/tree_builder_slice_trace/grapheme_windows` gate 说明，支撑 `StageD::FeatureGates`、`RPM-ParityAssets`、`Fixture-MetricWindows` 文档同步。
- **2025-11-19 – Manifest verifier自动化**：扩展 `scripts/refresh_serialization_fixtures.ps1` 以串联 loader → hydrator → manifest verifier → inspector，并在 Playbook `[StageD::FixtureFlow]` / `[QA-IngestionSmoke]` 固化命令链。
- **2025-11-18 – Stage D exporter timestamp 稳定化**：在 chunk/grapheme/breaks/diff/search exporter 中复用历史 `generated_at_unix_millis`，消除重复运行导致的 hash 漂移；`run_all_checks --filter serde-fixtures` 通过。

## 待办 / 风险
- [TODO] **`cursor_descriptors@1.3.0` schema drop**：补充 Breaks metric/Utf16 失效字段 + `metric_windows` entry，更新 CLI/manifest/hash，对应迁移说明需同步到 `StageD::ParityAssets`、`CursorCache.md`；依赖 Architecture Mapper 定义字段名，C# Implementer/QA 更新 hydrator/tests/日志。
- [TODO] **Metric windows → MetricAdapter**：将 manifest 中 7 组 `metric_windows` 注入 `StageDDescriptorHydrator` 与 QA smoke（`[QA-ChunkBench]`、`[QA-IngestionSmoke]`），形成“export → manifest → hydrator → MetricAdapter”闭环；完成前不要再扩充 Breaks payload，避免重复返工。
- [TODO] **Goal Tree / Stage D anchors同步**：把 `rust_commit=b6fb5999288946daeeadc4b7f9f9756f6dda1f50`、feature gates、`metric_windows` 摘要写回 Goal Tree YAML 与 `AGENTS.md`，同时在 `stage-d-inspector-latest.txt` 记录时间戳，供后续 refresh 对比。
- [RISK] **Clippy regression 会阻断 Stage D 流水线**：`refresh_all_assets` 依赖 `run_all_checks --filter serde-fixtures`；若新 lint 进入 `xi-editor-ph7/rust/rope/src/serde_fixtures/*`，Stage D export 再次中断。需在合并前先本地跑 clippy。
- [RISK] **QA 证据链仍未收敛**：目前日志仅覆盖 chunk/grapheme；若 Breaks/Diff/Search ingest/inspector 输出未写入 `[QA-IngestionSmoke]` 与 `tests/xi.Core.Tests/Fixtures/Reports/`，无法证明新的 payload hash。必要时暂停 exporter 刷新，等待 QA 补档。

## 关键文档索引
- `docs/csharp-refactor/rope-serialization-fixture-playbook.md`：`[StageD::FixtureFlow]`、`[StageD::ParityAssets]`、`[QA-IngestionSmoke]` 记录 exporter/脚本/QA 证据。
- `docs/architecture/rope-port-mapping.md`：`[RPM-ParityAssets]`/`[RPM-Matrix]` 映射 Rust helper → C#，对应 MetricAdapter backlog。
- `docs/rust-refactor/CursorCache.md` / `docs/rust-refactor/breaks-metrics-templating.md`：cursor_state 与 MetricAdapter 语义真源。
- `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`：Stage D manifest ledger（counts/hash/feature gates）。
- `scripts/refresh_serialization_fixtures.ps1`、`python scripts/refresh_all_assets.py`：Stage D 自动化入口，负责 run_all_checks + exporter + manifest + loader/hydrator/inspector。
- `xi-editor-ph7/rust/rope/src/bin/export-serde-fixtures.rs`, `xi-editor-ph7/rust/rope/src/serde_fixtures/*.rs`：CLI 实现与 schema 定义；`goal_tree_sync.py` 读取 manifest 以保持 Goal Tree 一致。
