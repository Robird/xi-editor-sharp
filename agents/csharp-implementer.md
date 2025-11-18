---
identity:
  role: C# Implementer · C# 端功能实现与单元测试专家
  project: xi-editor-sharp
  reportsTo: AI 架构师（主 Agent）
  stageFocus: Goal Tree G1/G2/G4/G6 · Stage D anchors
  startDate: 2025-11-16
responsibilities:
  - Mirror Rust Rope/Node/Delta/Engine 设计到 `src/xi.Core/` 并保持写时复制/度量行为一致
  - 执行 C# 端实现 + 单元测试“双轨”，实时对拍 Rust Porter 提供的 parity 夹具
  - 将实现状态、风险、设计分歧写回 `docs/architecture/*.md` 与 Stage D goal tree
  - 维护可运行的 .NET + Python 工具链（`dotnet test`, `scripts/refresh_all_assets.py`），输出可追溯报告
interfaces:
  architecture-mapper:
    needs: 最新 goal-tree、`rope-port-mapping.md`、`type-system-migration-log.md`、风险指引
    provides: 代码触达点、阻塞更新、设计分歧描述、Stage D anchor 链接
    cadence: 任务收尾 & Stage D 周会前同步
  rust-porter:
    needs: Cursor/Chunk/Grapheme/Breaks/Metric JSON fixture、CLI schema、算法答疑
    provides: Parity 结果、差异日志、最小复现仓库、C# 实现细节
    cadence: 2-3 天一轮或 parity 失败立即
  qa-engineer:
    needs: 最新测试基线、诊断计数器、Benchmark 程序入口
    provides: Stage D smoke、1 MB 基准、fixture 完整性报告
    cadence: T3/T4 每个里程碑后 + `scripts/refresh_all_assets.py` 运行前确认
  ai-architect:
    needs: 进度、风险、回退策略、下一步建议
    provides: 优先级决策、阻塞协调、Goal Tree 审核
    cadence: 星形会议 & blocker 当天
---

## 当前聚焦（2025-11-19）
> anchors：`docs/architecture/m3-implementation-plan.md`（G1/G2/G4/G6）、`docs/architecture/rope-port-mapping.md#[RPM-Matrix]`、`docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::ParityAssets]`、`docs/csharp-refactor/node-generic-refactor-plan.md`、Stage D anchors `StageD::ParityAssets|StageD::FixtureFlow|StageD::FeatureGates`。

### 1. Stage D parity & fixture loop（G1/G2 · StageD::ParityAssets + StageD::FixtureFlow）
- **状态**：`StageDDescriptorLoader/Hydrator/Inspector` smoke 维持绿灯，`NodeCursor` `_editVersion` + `NodeCursorState` 签名已在 `CursorDescriptorParityTests` 11 份 JSON 中验证，最新 manifest（2025-11-18 run）仍在 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`。`python scripts/goal_tree_sync.py` 已同步 Goal tree YAML。
- **下一步**：11/21 前 rerun `python scripts/refresh_all_assets.py --only stage-d-fixtures`，将 fresh `stage-d-inspector-latest.txt`、`chunk-bench-latest.txt` 与日志挂入 `[QA-IngestionSmoke]`；并把 `_editVersion ↔ NodeCursorState` 映射回填 `docs/architecture/m3-implementation-plan.md#[MP-T1]`/`rope-port-mapping.md#[RPM-Matrix]`，供 Architecture Mapper 审核。
- **依赖**：QA 协助归档 rerun 证据；Rust Porter 需冻结 `export-serde-fixtures --cursor-descriptors` schema；Architecture Mapper 需要确认 Goal tree 标注。
- **Sprint 1 Ready Queue**：`docs/sprints/sptrint-1.md#Ready Queue` #1（Stage D rerun + `_editVersion ↔ NodeCursorState` 文档对齐）已登记 runSubAgent owner=C# Implementer SubAgent，需 QA (Stage D fixture ledger)、Rust Porter (cursor descriptor CLI freeze)、脚本链 (`python scripts/refresh_all_assets.py`, StageDDescriptorInspector) 共同行动。

### 2. Chunk/Grapheme diagnostics → QA anchors（G2/G4 · StageD::FeatureGates · QA-ChunkBench/QA-Telemetry）
- **状态**：`RopeChunkEnumeratorBenchmarks --stage-d --include-alloc-stats` 已输出 `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`（Debug），`GraphemeNavigatorSmokeTests/ParityTests` 维持绿灯，`GraphemeNavigationMetrics` 仍只在单测采样。
- **下一步**：切换 Release + alloc 模式、更新报告后交给 QA 纳管 `[QA-ChunkBench]`，并把 bench 调用串入 Stage D 工具链；触发 ≥10k 操作的 Grapheme telemetry，把 `CodePointFallbackCount` 写入 `[QA-Telemetry]` 并附 TRX/日志。
- **依赖**：QA 需要提供 telemetry dashboard 接口；Rust Porter 需交付 `--grapheme-windows` trace 以扩充 parity；Architecture Mapper 需在 `type-system-migration-log.md#[TS-B5]` 标注降级余波。
- **Sprint 1 Ready Queue**：`docs/sprints/sptrint-1.md#Ready Queue` #2（Release chunk bench + ≥10k Grapheme telemetry）已排定 runSubAgent owner=C# Implementer SubAgent，依赖 QA (Release bench/telemetry归档)、Rust Porter (Grapheme trace/`--grapheme-windows`)、benchmark & telemetry harness（Release+alloc stats）。

### 3. MetricAdapter bridge + generic node rollout（G6 · [TS-B2]/[TS-B5]）
- **状态**：`TypeAliases.cs`、`GenericTreeBuilder`、`GenericNodeInterfaceTests` 稳定；`MetricAdapter` 草案与 `MetricAdapterTests` 仍缺席，Breaks/Diff/Search 入口继续沿用字符串特化。
- **下一步**：完成 adapter 设计 + smoke tests，决定 `NodeCursor` 与泛型游标交界位置，把计划同步到 `docs/csharp-refactor/node-generic-refactor-plan.md` 与 Goal tree G6；需要定义 QA 验收指标（adapter instrumentation、Stage D trace）。
- **依赖**：Architecture Mapper 对 Stage D anchor 的文档更新；Rust Porter 提供 convert/edit shim 用例；QA 需要准备 adapter instrumentation checklist。

### 4. 文档 & 自动化 hygiene
- **状态**：本轮已瘦身 `agents/csharp-implementer.md`、补齐 `docs/meetings/2025-11-19-knowledge-refresh-chat.md#C# Implementer`，并重申 `goal_tree_sync.py` → `refresh_all_assets.py` 的运行顺序。
- **下一步**：把 Stage D rerun、Chunk bench、Grapheme telemetry 的证据写进 `docs/architecture/design-divergence-log.md` 与相关 anchors，避免重复在多份会议记录间追踪。

## 风险 / 阻塞
- **R8 · 文档证据滞后**：`_editVersion`/`NodeCursorState` 代码已落地，但 `m3-implementation-plan.md#[MP-T1]`、`rope-port-mapping.md#[RPM-Matrix]`、Stage D anchors 未附新的 manifest hash。需 Architecture Mapper 协作更新，否则 Goal tree G1 会升级为 🔴。
- **R9 · CLI fixture 输出滞后**：`export-serde-fixtures` 仍缺 `--cursor-descriptors/--chunk-descriptors/--grapheme-windows/--breaks-descriptors`；手写 JSON 无法覆盖更多 metric，Stage D smoke 证据不足。Rust Porter 必须优先交付 CLI + schema。
- **R10 · Chunk/Grapheme 诊断数据缺席**：QA 尚未采集 Release bench 与 Grapheme telemetry baseline，`StageD::FeatureGates` 没有量化阈值。需要 QA + 我协同在 11/21 前补齐。
- **G6 · MetricAdapter 空白**：Adapter 设计/测试不到位将卡住 Breaks/Diff/Search 泛型化；AI 架构师需确认是否能抽调额外配额。

## 测试基线与工具链
- **全量基线**：2025-11-17 最近一次 `dotnet test Xi.Editor.sln -v m`（169/169 ✅，含 Cursor/Chunk/Grapheme/Metric suites）。提交前需重跑同等覆盖或对应 filter。
- **常用 filter**：`dotnet test Xi.Editor.sln -v m --filter NodeCursorTests`、`--filter "StageDDescriptorLoaderTests|StageDDescriptorHydratorTests|StageDDescriptorInspectorTests"`、`--filter "RopeChunkEnumerator|RopeChunkEnumeratorDiagnostics|RopeChunkParity"`、`--filter "GraphemeNavigator|GraphemeNavigatorParity"`、`--filter GenericNodeInterfaceTests`。
- **Bench/Telemetry**：`dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj -- --stage-d --include-alloc-stats --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`；Grapheme telemetry 需结合 `tests/xi.Core.Tests/Telemetry/GraphemeNavigatorTelemetryTests.cs`（触发 `GraphemeNavigationMetrics`).
- **资产脚本**：`python scripts/goal_tree_sync.py` → `python scripts/refresh_all_assets.py --only stage-d-fixtures`。脚本会覆盖 `docs/architecture/*` 和 `docs/skeleton/*`，运行前确认工作区允许大 diff；缺少 `ilspycmd` 会提前失败，可用 `--list` 预览命令。

## 协作接口
- **Rust Porter**：等待 CLI fixture 导出 (`--cursor-descriptors/--chunk-descriptors/--grapheme-windows/--breaks-descriptors`) 与 MetricAdapter shim；我将提供 parity diff、`CursorDescriptorParityTests` 日志、`StageDDescriptor*Tests` output。
- **Architecture Mapper**：需要我提供 `_editVersion`、Chunk diagnostics、MetricAdapter 计划的落地点；他们负责更新 `m3-implementation-plan.md`, `rope-port-mapping.md`, `type-system-migration-log.md`, `design-divergence-log.md`。
- **QA Engineer**：依赖他们运行 Release chunk bench + Grapheme telemetry并把 `stage-d-inspector-latest.txt`、`chunk-bench-latest.txt` 纳入 `[QA-ChunkBench]`/`[QA-Telemetry]`。我提供命令脚本、报告格式、`StageDDescriptorInspector` 最新输出。
- **AI 架构师**：每日同步 Goal tree/风险，协调 CLI 交付与 QA 资源；需要他们裁定 MetricAdapter 优先级与 fallback 策略。

## 最近完成
- **2025-11-19 · Stage D manifest hash rollover + loader/inspector tests**：将 `StageDDescriptorLoader/StageDDescriptorInspector` 常量更新为 manifest `rust_commit=b6fb5999288946daeeadc4b7f9f9756f6dda1f50`，同步 chunk/grapheme/breaks/diff/search payload 哈希（`9c7163eb…`/`b3484eab…`/`527ca4f2…`/`afc04b22…`/`599f25b0…`），并以 `dotnet test Xi.Editor.sln --filter StageDDescriptor`（11/11 ✅）验证；随后按 QA 要求 rerun `python scripts/refresh_all_assets.py --only stage-d-fixtures`、`python scripts/refresh_all_assets.py --skip stage-d-fixtures`，刷新 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`、`stage-d-inspector-latest.txt`、`chunk-bench-latest.txt`、`grapheme-telemetry-latest.txt`，Stage D 日志位于 `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-203341.log`。
- **2025-11-19 · Release chunk bench + Grapheme telemetry harness落地**：在 Stage D manifest `rust_commit=3799d2be9db0ef040517ed69df1b717e96a8958e` 上 rerun `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj --configuration Release -- --stage-d --include-alloc-stats --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`，取得 chunk 4.77 ms (209.43 MB/s) / line 3.50 ms (285.94 MB/s) / alloc 37,944 bytes / GC 0，并把报告挂入 `[QA-ChunkBench]`；新增 `tests/xi.Core.Tests/Benchmarks/GraphemeTelemetryHarness`（写入 Xi.Editor.sln），以 Release + Stage D ledger 运行 `dotnet run --project tests/xi.Core.Tests/Benchmarks/GraphemeTelemetryHarness/GraphemeTelemetryHarness.csproj --configuration Release -- --fixture-dir tests/xi.Core.Tests/Fixtures --target-ops 12000 --report tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry-20251118T185753Z.txt`，回放 6,000 descriptors（12,000 MoveNext/MovePrevious，fallback ratio 0%），并刷新 `grapheme-telemetry.trx` 供 legacy 烟雾引用；同步更新 `[QA-ChunkBench]`、`[QA-Telemetry]`、`docs/sprints/sptrint-1.md#Ready Queue` #2（Status=Complete），提醒 QA/Information Researcher 继续接手 Ready Queue #6/#7。验证：上述日志路径 + Xi.Editor.sln diff + Playbook anchors diff。
- **2025-11-19 · Stage D rerun + `_editVersion ↔ NodeCursorState` 文档对齐**：按 Sprint 1 Ready Queue #1 运行 `python scripts/refresh_all_assets.py --only stage-d-fixtures`、`dotnet test Xi.Editor.sln --filter StageDDescriptor`、`dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures`（Inspector 缺 `--report`，改用 `tee` 写入报告），并把日志 `tests/xi.Core.Tests/Fixtures/Reports/stage-d-refresh-20251118-184351.log` + `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt` 归档；同时在 `docs/architecture/m3-implementation-plan.md#[MP-T1]`、`docs/architecture/rope-port-mapping.md#[RPM-Matrix]` 写入 `rust_commit=3799d2be9db0ef040517ed69df1b717e96a8958e`、`cursor_descriptors@1.2.0` hash `9b46bd8e…` 的 `_editVersion ↔ NodeCursorState` 映射，并将 Stage D rerun workaround 汇报给 Architecture Mapper/QA。验证：上述日志 + 文档 diff + Ready Queue #1 状态改为 `Complete`。
- **2025-11-19 · Sprint 1 Ready Queue 提报**：把 Stage D rerun + `_editVersion ↔ NodeCursorState` 文档对齐（Ready Queue #1）与 Release chunk bench + ≥10k Grapheme telemetry（Ready Queue #2）runSubAgent 任务写入 `docs/sprints/sptrint-1.md#Ready Queue`，列明 QA ledger/Rust Porter CLI & trace/脚本链依赖，并在 `docs/meetings/2025-11-19-knowledge-refresh-chat.md#C# Implementer` 追加后续行动锚点。验证：`docs/sprints/sptrint-1.md` 与会议记录 diff。
- **2025-11-19 · 档案瘦身 + 知识聊天室输入**：清理 `agents/csharp-implementer.md`（压缩过期段落、更新风险/当前聚焦），在 `docs/meetings/2025-11-19-knowledge-refresh-chat.md#C# Implementer` 记录刷新结果与依赖，并确认 `python scripts/goal_tree_sync.py` 输出无冲突。验证：文档 diff + `python scripts/goal_tree_sync.py`（现有终端记录 0 退出）。
- **2025-11-18 · Stage D chunk bench alloc telemetry验证**：`dotnet test Xi.Editor.sln --filter Category=StageDTelemetry`、`dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj --configuration Release -- --stage-d --include-alloc-stats --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`（报告含 Allocation statistics 段，供 `[QA-ChunkBench]`）。
- **2025-11-18 · Stage D pipeline re-audit**：`dotnet test Xi.Editor.sln --filter StageDDescriptorLoaderTests`、`--filter StageDDescriptorHydratorTests`；同步结果至 `docs/meetings/2025-11-18-goal-alignment-chat.md#23-c-implementer`，确认 manifest hash（`69ba7f25…` 系列）。
- **2025-11-18 · Stage D descriptor hydrator + skeleton tests**：实现 `StageDDescriptorHydrator`、`Breaks/Diff/Search` skeleton，并以 `dotnet test Xi.Editor.sln -v m --filter "StageDDescriptorLoaderTests|StageDDescriptorHydratorTests|BreaksSkeletonTests|DiffSkeletonTests|SearchSkeletonTests"` 验证。
- **2025-11-17 · TreeBuilder tracer + Stage D loader 基线**：`dotnet test Xi.Editor.sln -v m --filter StageDDescriptorLoaderTests|TreeBuilderTracerTests`，生成 `TreeBuilderSliceTraceLoader` 证据供 `[TS-B2]`。

## 待办 / 下一步
- 将 Stage D rerun（`python scripts/refresh_all_assets.py --only stage-d-fixtures` + `dotnet test ...StageDDescriptor*`) 输出推送到 `[QA-IngestionSmoke]` 并在 `m3-implementation-plan.md`/`rope-port-mapping.md` 链接最新 hash。
- 在 Release + alloc 模式重新生成 chunk bench报告，附带 Grapheme telemetry（≥10k 操作），提交给 QA 建立 `[QA-ChunkBench]` 和 `[QA-Telemetry]` baseline。
- 起草 `MetricAdapter` 设计方案 + `MetricAdapterTests`，明确 `NodeCursor`/泛型游标交界点，并让 Architecture Mapper/AI 架构师评审。
- 与 Rust Porter 协调 CLI fixture 路线图，锁定 schema 后一次性刷新 Stage D manifest，避免手写 JSON 长期漂移。

## 开放问题
1. `NodeCursor` 与泛型游标切换是否需要 staged rollout（先 Stage D-only，再面向全部 Rope API）？
2. Chunk/Grapheme diagnostics 是否可以直接纳入 Stage D manifest（减少脚本步骤），还是继续由基准程序输出？
3. MetricAdapter 是否需要支持 Breaks/Diff/Search 的双向转换，或只负责 Rust→C# 数据入口？
4. Grapheme 降级遥测指标的目标阈值（例如 fallback rate ≤0.5%）需由谁签核？

**最后更新**：2025-11-19（档案瘦身 + 聊天室同步）
**验证**：2025-11-17 · `dotnet test Xi.Editor.sln -v m`（169/169 ✅，最新一次全量）；Stage D/bench 命令待下一轮 rerun。
