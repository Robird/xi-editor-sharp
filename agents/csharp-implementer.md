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

## 当前聚焦
> 任务锚点：`docs/architecture/m3-implementation-plan.md` goal tree（G1 Cursor descriptors, G2 Chunk/Line diagnostics, G4 Diff/Search handoff, G6 MetricAdapter bridge）以及 Stage D anchors（`StageD::ParityAssets`, `StageD::FixtureFlow`, `StageD::FeatureGates`）。

### 11/27 Stage D Loader & QA Anchors 检查点
1. **`_editVersion` ↔ `CursorState` 文档 & 证据**：`docs/architecture/m3-implementation-plan.md#[MP-T1]`、`docs/architecture/rope-port-mapping.md#[RPM-Matrix]`、`docs/csharp-refactor/rope-serialization-fixture-playbook.md#[StageD::ParityAssets]` 已记录 NodeCursorState + `cursor_descriptors@1.2.0` manifest 证据；需在 11/21 前 rerun `python scripts/refresh_all_assets.py --only stage-d-fixtures` 并将全绿日志 + `stage-d-inspector-latest.txt` 回填 `[QA-IngestionSmoke]`。
2. **Stage D Breaks/Diff/Search typed DTO 接线**：`StageDDescriptorHydrator` + inspector tests 已消费真实 manifest（`dotnet test Xi.Editor.sln --filter "StageDDescriptorHydratorTests|StageDDescriptorInspectorTests"`），继续盯住 Rust CLI hash 变化并把任何 schema 更新同步到 `[StageD::FeatureGates]`/`fixtures.manifest.json`。
3. **Chunk/Grapheme diagnostics → QA anchors**：`RopeChunkEnumeratorBenchmarks --stage-d` 已生成 `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`（3.67 ms/2.86 ms），下一步换成 Release + alloc 模式并让 QA 在 `[QA-ChunkBench]` 纳管；同时触发 `GraphemeNavigatorSmokeTests` telemetry（TRX）以补 `[QA-Telemetry]` 缺口。

- **参考面 / 快速索引**
  - 实现指南：`docs/csharp-refactor/rope-cow-rebalance-plan.md`, `docs/csharp-refactor/node-generic-refactor-plan.md`, `docs/csharp-refactor/rope-delta-notes.md`, `docs/csharp-refactor/rope-serialization-fixture-playbook.md`
  - 架构映射：`docs/architecture/rope-port-mapping.md`, `docs/architecture/type-system-migration-log.md`, `docs/architecture/design-divergence-log.md`, `docs/architecture/port-blueprint.md`
  - 代码入口：`src/xi.Core/Rope/Tree/*.cs`, `src/xi.Core/Rope/*.cs`, `src/xi.Core/TextBuffer.cs`
  - 测试与夹具：`tests/xi.Core.Tests/**/*.cs`, `tests/xi.Core.Tests/Fixtures/**`
  - 骨架参考：`docs/skeleton/xi.Core.decompiled.cs`, `docs/skeleton/rope.md`

- **实现状态**：`NodeCursor` 拥有型游标、`_editVersion` 失效检测、Base/Lines/Utf16 导航及 26+ `NodeCursorTests` 全部通过；`CursorDescriptorParityTests` 已接入 11 份 JSON（深树 + 多 metric），`NodeCursor` 在编辑后可自动重新 Descend。
- **待交付/下一击**：补完 T1.2/T1.3 中叶片遍历的边界计数修复、`_pathCache` 深树诊断输出，并在下一次 Stage D rerun 中把 NodeCursorState `_editVersion` 证据挂入 `[QA-IngestionSmoke]`；T1.5/T1.6 仍需 Rust Porter 提供 ≥10 份 CLI 导出的 descriptor JSON（`export-serde-fixtures --cursor-descriptors`）以替代手写样本，并将 schema 固化到 Stage D。
- **依赖与协作**：Architecture Mapper 需在 `m3-implementation-plan.md` G1 中记录 `_editVersion` 与 `NodeCursorState` 地图；QA 需在 `QA-IngestionSmoke` 中验证掺入的 JSON；Rust Porter 负责 CLI 与深树样本，回答 Metric/路径缓存疑问。

- **实现状态**：`RopeChunkEnumerator`/`RopeLineEnumerator` 已提供复制型 `ReadOnlyMemory<char>` 枚举；`RopeChunkEnumeratorDiagnostics` 捕获 chunk 总数、最大 chunk 长度、UTF-16 拷贝累计；`RopeChunkEnumeratorBenchmarks` 在 Stage D 模式下会读取 manifest ledger 并把吞吐写入 `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`。
- **待交付/下一击**：基线 chunk fixtures 已随 manifest 提供，当前任务转为：以 Release + alloc 模式 rerun bench、把结果写进 `[QA-ChunkBench]` 与 `docs/architecture/m3-implementation-plan.md §5.3`，并把相同报告纳入 `python scripts/refresh_all_assets.py --only stage-d-fixtures` 的证据链；另需把 Grapheme telemetry 流程与 QA 的 `[QA-Telemetry]` 接线一并完成。
- **依赖与协作**：CLI/schema 由 Rust Porter 定义；QA 负责运行新基准与 ingestion smoke；Architecture Mapper 在 `design-divergence-log.md` 记录“复制型”降级并跟进 Stage D anchor。

### T4 Grapheme Navigator（IGraphemeNavigator / DegradedGraphemeNavigator）
- **实现状态**：`IGraphemeNavigator` 接口、`DegradedGraphemeNavigator` 降级策略与 `GraphemeNavigationMetrics` 遥测计数器已合入；`GraphemeNavigatorSmokeTests` + `GraphemeNavigatorParityTests` 覆盖 surrogate、emoji、跨叶 fallback；`design-divergence-log.md` 登记了降级策略。
- **待交付/下一击**：Rust Porter 尚需交付 `--grapheme-windows` CLI trace 以扩充 parity；Architecture Mapper/AI 架构师需在 11/20 前确认 fallback 命中阈值并写回 Stage D；QA 需采集 ≥10k 操作 telemetry，将 `CodePointFallbackCount` 接入 dashboard。
- **依赖与协作**：与 Rust Porter 合作确认 leaf/chunk schema；与 QA 协调 telemetry 触发器；Architecture Mapper 更新 `type-system-migration-log.md#TS-B5`。

### T6 MetricAdapter Bridge
- **实现状态**：`TypeAliases.cs`、`GenericTreeBuilder`、8 项泛型接口测试已稳定，但 `MetricAdapter` 设计稿、`MetricAdapterTests` smoke 与 `INodeCursor<TInfo, TLeaf>` 适配器尚未落地，Goal Tree G6 维持 ⚠️ watch。
- **待交付/下一击**：撰写 `MetricAdapter` 草案并引用 `[TS-B2]`、实现 smoke 测试、把计划同步到 `m3-implementation-plan.md` & `type-system-migration-log.md`；确定 `NodeCursor` → 泛型游标的接口收口方式，以及 Breaks/Diff/Search 前置的 adapter 使用模式。
- **依赖与协作**：Architecture Mapper 必须处理 Stage D anchor；Rust Porter 提供 convert/edit shim 示例；QA 需要对 adapter instrumentation 制定 checklist，以便 G6 验收。

## 测试基线与工具链
- **114+ 测试基线**：M3 维持 114 项核心测试（106 旧基线 + 8 泛型接口）并扩展至 169 项（游标、Chunk/Grapheme、parity/diagnostics）。最新一次全量运行：2025-11-17 执行 `dotnet test Xi.Editor.sln -v m`，169/169 ✅，其中包含 `CursorDescriptorParityTests` 11/11、`RopeChunkEnumeratorDiagnosticsTests`、`GraphemeNavigatorSmokeTests`、`GenericNodeInterfaceTests`。任何提交前需重跑该命令或针对变更模块运行等价验证。
- **分组命令**：
  - `dotnet test Xi.Editor.sln -v m --filter NodeCursorTests`
  - `dotnet test Xi.Editor.sln -v m --filter "RopeChunkEnumerator|RopeChunkEnumeratorDiagnostics|RopeChunkParity"`
  - `dotnet test Xi.Editor.sln -v m --filter "GraphemeNavigator|GraphemeNavigatorParity"`
  - `dotnet test Xi.Editor.sln -v m --filter GenericNodeInterfaceTests`
- **Bench/Diagnostics**：`tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks` 提供 1 MB chunk/line 统计；`GraphemeNavigationMetrics` 计数器通过单测断言 + telemetry 报表复核。
- **`scripts/refresh_all_assets.py` 注意事项**：
  - 顺序运行 `goal_tree_sync.py` → `refresh_skeleton_docs.py` → `dotnet build Xi.Editor.sln` → `ilspycmd` → `tools/Skeletonizer`，需要 Python 3.11+、`dotnet` SDK、`ilspycmd`（PATH）、以及由 Debug 构建生成的 `src/xi.Core/bin/Debug/net9.0/xi.Core.dll`。
  - 会改写 `docs/architecture/`（Goal Tree 片段）与 `docs/skeleton/`（ILSpy 输出 + Skeletonizer），运行前需确认工作区可接受这些大文件变动；如只需部分步骤可用 `--only goal-tree`、`--skip ilspy`、`--dry-run`，必要时加 `--continue-on-error`。
  - 若 `ilspycmd` 缺失脚本会抛出 `FileNotFoundError` 并提前终止；可先 `--list` 预览命令以确保依赖齐备。

## 风险/阻塞
- **R8（版本票据文档化）**：代码已引入 `_editVersion` + NodeCursor 失效检测，但 `port-blueprint.md`、`rope-port-mapping.md` 与 Stage D anchor 仍未更新；若 11/19 前未补写，Goal Tree G1 将从 ⚠️ 提升为 🔴。需我与 Architecture Mapper 尽快提交文档与样本并同步 QA。
- **R9（CLI fixture 输出滞后）**：`export-serde-fixtures` 尚未合入 `--cursor-descriptors/--chunk-descriptors/--grapheme-windows`，当前 11 份 JSON 由手写维持；若 11/19 仍不可用，QA 的 Stage D smoke 将缺少事实来源。Rust Porter 必须优先完成 CLI，Architecture Mapper 在 Stage D `FixtureFlow` 标记阻塞。
- **R10（Chunk/Grapheme 诊断数据缺席）**：`RopeChunkEnumeratorDiagnostics` 与 `GraphemeNavigationMetrics` 仅在单测中使用，尚无 1 MB baseline 与 fallback 阈值；QA/Architecture Mapper 需在 11/21 前出具数据，否则风险升级为“高/中”。
- **MetricAdapter Bridge（G6）**：`MetricAdapter` 草案和 smoke 测试仍是空白；泛型节点切换、Breaks/Diff/Search 计划（G3/G4）被迫等待。需要在 11/24 前提交设计 + `MetricAdapterTests`，否则 `type-system-migration-log.md#TS-B2` 与 Goal Tree G6 无法关闭。

## 协作接口
- **Rust Porter**
  - 输入：Cursor/Chunk/Grapheme/Breaks JSON fixture、`export-serde-fixtures` CLI 扩展、`cursor_descriptor.rs` 与 `iterator-facade-export.md` 说明。
  - 输出：C# 侧 parity 结果、差异日志（含 JSON、`ITestOutputHelper` dump、`ToDebugString()`）、算法疑问记录（附最小复现 + Rust 源码引用）。
  - 近期需求：冻结 `--cursor-descriptors` schema、交付 10+ 样本、提供 MetricAdapter shim 参考实现。
- **Architecture Mapper**
  - 输入：Goal tree 变更、`rope-port-mapping.md`/`type-system-migration-log.md` 更新窗口、Stage D anchor 规则。
  - 输出：实现新文件/接口/风险后，他们在蓝图和映射表登记，并驱动 `design-divergence-log.md`。
  - 近期需求：记录 `_editVersion`/`NodeCursorState`、Chunk diagnostics 降级、MetricAdapter 计划。
- **QA Engineer**
  - 输入：命令脚本（`dotnet test -v m`, `RopeChunkEnumeratorBenchmarks`, telemetry instrumentation）、`scripts/refresh_all_assets.py` 运行要求。
  - 输出：`QA-IngestionSmoke`, `QA-ChunkBench`, `QA-StageDManual` 报告，以及 fixture 完整性核查。
  - 近期需求：拉起 1 MB chunk baseline + Grapheme fallback telemetry，并监督 CLI 产物进入 Stage D。
- **AI 架构师**
  - 输入：本档案节奏、风险、下一步计划。
  - 输出：优先级决策（例如先收口 T1 再推进 T3/T4/T6）、资源/工时调度、Goal Tree 审批。
- **同步节奏**
  - Rust ↔ C#：Parity 失败或 CLI 变更时立刻同步；平时 2-3 天节奏。
  - Architecture Mapper：每任务完结在 `AGENTS.md` + goal tree 记状态，通过 `scripts/goal_tree_sync.py` 保持 YAML 一致。
  - QA：Chunk/Grapheme 基准完成即交接；`scripts/refresh_all_assets.py` 前后提供环境确认。
  - AI 架构师：星形会议更新 + blocker 当天随时升级。
- **工作原则**
  1. 质量优先：必要时返工，保证 Rope/Node invariants 与 Rust 对齐。
  2. 测试驱动：实现与测试同步提交，保持 `dotnet test -v m` 绿灯。
  3. 对齐骨架：沿用 Rust skeleton，差异必须在 `design-divergence-log.md` 留痕。
  4. 文档同步：Goal tree / blueprint / mapping / divergence 必须随代码更新。
  5. 汇报透明：每次任务完成在“最近完成”记录，并向架构师陈述成果 + 风险。

## 最近完成

- **2025-11-18** · Stage D chunk bench alloc telemetry验证：审阅 `tests/xi.Core.Tests/Benchmarks/Diagnostics/Program.cs` 新增的 `ChunkBenchOptions`/`AllocationSnapshot` 参数解析与 GC 统计写入，确认 Release `--include-alloc-stats` 工作流保持 Stage D 报告格式；执行 `dotnet test Xi.Editor.sln --filter Category=StageDTelemetry` 与 `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj --configuration Release -- --stage-d --include-alloc-stats --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`，`tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` 报告出现 “Allocation statistics” 段并显示 37,784 B/0 GC，供 `[QA-ChunkBench]`/Stage D pipeline 归档。

- **2025-11-18** · Stage D pipeline re-audit：重跑 `dotnet test Xi.Editor.sln --filter StageDDescriptorLoaderTests`、`--filter StageDDescriptorHydratorTests`，确认 `fixtures.manifest.json`（hash `69ba7f25…/eb0c7c66…/5d37a731…/fe76ed31…/7eac7ecf…`）与测试断言一致，并在 `docs/meetings/2025-11-18-goal-alignment-chat.md#23-c-implementer`/A2 更新当前现状、风险与下一步（Stage D rerun + Release chunk bench + Grapheme telemetry）。
- **2025-11-18** · Goal Alignment Chat（`docs/meetings/2025-11-18-goal-alignment-chat.md#23-c-implementer`）：向 Architect/Rust Porter/QA 汇报 NodeCursor `_editVersion`、Stage D loader→hydrator→inspector 流程、Chunk/Grapheme diagnostics 与 `TreeBuilderTracer` 状态，记录 `dotnet test Xi.Editor.sln --filter StageDDescriptor` + `python scripts/refresh_all_assets.py --only stage-d-fixtures` 的验证步骤，并确认 11/27 前的文档与 QA 交付项。

- **2025-11-20** · Stage D manifest ledger刷新（Rust commit `3799d2be9db0ef040517ed69df1b717e96a8958e`）：同步 `StageDDescriptorLoaderTests`、`StageDDescriptorInspectorTests` 的 commit 与 chunk/grapheme/breaks/diff/search payload hash，附注指向 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`（2025-11-20 run），并复跑 loader+hydrator+inspector smoke 以及 `python scripts/refresh_all_assets.py --only stage-d-fixtures --only stage-d-fixtures`，`verify_fixture_manifest.py`/StageD inspector 输出均显示新 hash。验证：`dotnet test Xi.Editor.sln --filter "StageDDescriptorLoaderTests|StageDDescriptorHydratorTests|StageDDescriptorInspectorTests"`、`python scripts/refresh_all_assets.py --only stage-d-fixtures --only stage-d-fixtures`。
- **2025-11-18** · Stage D chunk bench CLI/report：`tests/xi.Core.Tests/Benchmarks/Diagnostics/Program.cs` 现接受 `--stage-d/--manifest/--report`，当 Stage D 打开时会通过 `StageDDescriptorLoader` 重建 Rope、输出 manifest metadata + chunk/line throughput + per-sample 摘要，并把同样内容写入 `tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`；`README.md` 介绍两种模式，QA 可直接引用报告。验证：`dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj -- --stage-d --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`（生成 `[QA-ChunkBench]` 可回放报告）。
- **2025-11-18** · MetricAdapter + Stage D inspector wiring：落地 `MetricAdapter`/`TreeBuilderTracer` 桥接（Lines/Utf16/Breaks 统一由 adapter 提供），新增 `TreeBuilderEvent` Break offsets、`TreeBuilderSliceTraceLoader.LoadFromManifest` 与 `MetricAdapterTests` 覆盖 Lines/Utf16/Breaks/trace round-trip；`python scripts/refresh_all_assets.py --only stage-d-fixtures` 现自动捕获 `stage-d-inspector-latest.txt` 供 QA 入档，并在 `[StageD::ParityAssets]`、`#MP-T1` 补写 `_editVersion` ↔ `NodeCursorState` 映射。验证：`dotnet test Xi.Editor.sln -v m --filter "MetricAdapterTests|TreeBuilderTracerTests|TreeBuilderSliceTraceLoaderTests"`、`python scripts/refresh_all_assets.py --only stage-d-fixtures`（生成 `tests/xi.Core.Tests/Fixtures/Reports/stage-d-inspector-latest.txt` 且 manifest 校验通过）。
- **2025-11-20** · Type mapping sync checkpoint：在 `docs/meetings/2025-11-20-type-mapping-sync-chat.md##C# Implementer` 汇总当前 C# ↔ Rust 差距（`[RPM-Matrix]`, `[TS-B2][TS-B3][TS-B5]`）并列出 2 周内的 Stage D loader/NodeCursorState/MetricAdapter 行动项，便于 Goal Tree `MP-T1..T4` 与 Stage D anchors 同步。
- **2025-11-18** · Stage D descriptor hydrator + mapping tests：实现 `StageDDescriptorHydrator` 统一封装 Breaks/Diff/Search 加载，并新增 `StageDDescriptorHydratorTests` 覆盖 `ascii_guidance` 断点、`ascii_minimal_ops` diff op 序列与 `literal_case_insensitive` 搜索命中上下文，方便 QA/CLI 复用。验证：`dotnet test Xi.Editor.sln -v m --filter "StageDDescriptorLoaderTests|StageDDescriptorHydratorTests|BreaksSkeletonTests|DiffSkeletonTests|SearchSkeletonTests"`。
- **2025-11-18** · Breaks/Diff/Search Stage D skeleton类型与 smoke tests：在 `src/xi.Core/Rope/Breaks`, `src/xi.Core/Diff`, `src/xi.Core/Search` 添增与 Rust 同名的 BreaksTree/BreakBuilder、LineHashDiff/DiffBuilder、Finder/SearchResult 等占位类型，并让新的 `Breaks/Diff/Search SkeletonTests` 绑定 Stage D descriptor view，保持 `StageDDescriptorLoaderTests` 绿灯。验证：`dotnet test Xi.Editor.sln -v m --filter "BreaksSkeletonTests|DiffSkeletonTests|SearchSkeletonTests" 与 `dotnet test Xi.Editor.sln -v m --filter StageDDescriptorLoaderTests`。
- **2025-11-18** · Stage D manifest ledger hashes（二次刷新）同步：`StageDDescriptorLoaderTests` 的 chunk/grapheme 以及 Breaks/Diff/Search optional ledger `payload_hash` 与当前 `fixtures.manifest.json` 对齐，确保 Stage D fixtures 在重复导出后保持稳定。验证：`dotnet test Xi.Editor.sln --filter StageDDescriptorLoaderTests`。
- **2025-11-18** · Stage D descriptor loader tests刷新：`chunk/grapheme` ledger `payload_hash` 对齐 2025-11-18 manifest，并让可选资产断言真实 breaks/diff/search ledger（count=3 + 新 hash）。验证：`dotnet test Xi.Editor.sln --filter StageDDescriptorLoaderTests`。
- **2025-11-18** · Stage D descriptor manifest/tests同步：恢复 `fixtures.manifest.json` ledger（chunk/grapheme/BDS hash）并让 `StageDDescriptorLoader` 元资料读取 manifest 计数，配套更新 `StageDDescriptorLoaderTests` 断言。验证：`dotnet test Xi.Editor.sln --filter StageDDescriptorLoaderTests`。
- **2025-11-18** · 2025-11-18 Porting brainstorm 行动项评估完成，确定 C# 端立即推进 `CursorEditSession`、`TreeTraceSchemaKit`、`ChunkWindowBenchmarks` 与遥测接入计划，输出采用清单与聊天室简报，作为即将汇报的输入。验证：本次行动清单与草稿（当前对话记录）。
- **2025-11-18** · Breaks/Diff/Search Stage D skeleton 与 `StageDDescriptorLoader` 扩展完成，manifest 现支持 `breaks_descriptors/diff_regions/search_spans` 并在 `[StageD::ParityAssets]`、`[TS-B5]` 标注覆盖范围。验证：`dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter StageDDescriptorLoaderTests`。
- **2025-11-18** · `TreeBuilderSliceTraceLoader`、sample JSON 与 smoke tests 就绪，`[TS-B2]`/`[StageD::FixtureFlow]` 可引用 slice trace 资产并等待 Rust CLI 下发真实数据。验证：`dotnet test tests/xi.Core.Tests/xi.Core.Tests.csproj --filter TreeBuilderSliceTraceLoaderTests`。
- **2025-11-17** · TreeBuilder tracer 注入点与 Stage D descriptor loader 基线落地，`TreeBuilderTracerTests` 与 `StageDDescriptorLoaderTests` 进入常规回归集，关联锚点 `[TS-B2]`、`[StageD::FixtureFlow]`。验证：`dotnet test Xi.Editor.sln -v m --filter StageDDescriptorLoaderTests|TreeBuilderTracerTests`。
- **2025-11-16 之前** · 更早的游标/Metric/Chunk/Grapheme 实施、文档评审与风险输入集中记录于 `AGENTS.md##工作日志`，作为单一事实来源与命令历史。

## 待办/下一步
- **Stage D chunk bench → QA/bench**：在 loader/hydrator/inspector smoke 已锁定 `3799d2be...` ledger 的前提下，把 `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj -- --stage-d --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt` 串入 `scripts/refresh_all_assets.py`（Stage D 流程）并让 QA 在 `[QA-ChunkBench]` 模板中引用报告路径，同时记录 chunk bench 版本到 `rope-port-mapping.md`/Goal Tree 以便未来零拷贝实现可以对比同一工作流。
- **MP-T1 收尾**：补写 `_editVersion`/`NodeCursorState`/CLI manifest 流程到 `docs/architecture/m3-implementation-plan.md#MP-T1`、`docs/architecture/rope-port-mapping.md#RPM-Matrix`、`docs/architecture/type-system-migration-log.md#TS-B1`，并通过 `python scripts/refresh_all_assets.py --only stage-d-fixtures` + `StageDDescriptorInspector` 证明 `cursor_descriptors@1.1.0` schema/哈希锁定。
- **MetricAdapter 草案与测试**：在 `docs/csharp-refactor/node-generic-refactor-plan.md`、`src/xi.Core/Rope/Tree/TreeBuilder.cs` 写入 `MetricAdapter` 钩子，产出 `tests/xi.Core.Tests/MetricAdapterTests.cs` smoke，解除 `[TS-B2]` watch。执行 `dotnet test Xi.Editor.sln -v m --filter MetricAdapterTests` 作为验收。
- **⚠️ 阻塞 · Breaks/Diff/Search CLI 样本**：`type-system-migration-log.md#TS-B5` 仍等待 Rust Porter 发布 `export-serde-fixtures --breaks-descriptors/--diff-regions/--search-spans`；到位前只能用 skeleton + 手写样本维护 `StageDDescriptorHydratorTests`，需持续在 `docs/meetings/2025-11-20-type-mapping-sync-chat.md` 同步阻塞状态。
- **文档同步**：每次完成以上任务后，用 `scripts/goal_tree_sync.py` 刷新 Goal Tree，并在 `[StageD::ParityAssets]`、`[StageD::FixtureFlow]` 标注新增证据链，确保 `rope-port-mapping.md` 与 `type-system-migration-log.md` 与会议记录一致。

**开放问题**
1. **泛型接入优先级**：`Node.Generic.cs` 已具备基础能力，何时将其接入主实现路径？是否等待游标系统完成后再统一切换？
2. **字符串特化保留策略**：泛型化后是否保留 `Node.cs` 作为字符串快速路径，还是完全切换到泛型节点 + 类型别名？
3. **测试迁移范围**：现有 81 项 Rope 测试是否需要全部改写为泛型版本，还是通过类型别名最小化改动？
4. **游标实现优先级**：当前很多功能依赖 `Snapshot()` 或 `TraverseLeaves()` 降级实现，游标系统是否是下一步最高优先级？
5. **CursorState 必要性**：Rust 端 `cursor_state` 作为可选 feature，C# 侧是否需要同步实现，还是先聚焦 `CursorDescriptor` 基础能力？
6. **迭代器设计方向**：`RopeChunkEnumerator`/`RopeLineEnumerator` 应该返回 `ReadOnlyMemory<char>` 还是自定义 `ChunkView` 结构？如何平衡零拷贝与易用性？
7. **Metric shim 接入**：Rust 端已提供 4 个 `convert_*` shim，C# 侧是否应该直接 P/Invoke 还是继续用动态 `IMetric` 接口？
8. **Breaks 度量支持**：`BreaksMetricHelper` 已实现，但 Rust 端尚未导出 Breaks 相关 shim，C# 如何推进软换行功能？
9. **泛型 Metric 切换**：何时从 `IMetric` 动态分派切换到静态抽象接口？是否需要性能基准验证收益？
10. **Grapheme 降级监控**：设计分歧日志已确认降级策略，但尚未实现遥测统计。如何插入监控指标而不影响性能？
11. **Diff/Search 骨架时机**：这些模块依赖游标系统，是否应该等游标完成后再创建骨架，还是先建占位目录？

**最后更新**：2025-11-20（Stage D manifest/hash 刷新 + smoke 验证）
**验证**：2025-11-20 · `python scripts/refresh_all_assets.py --only stage-d-fixtures --only stage-d-fixtures`（loader/hydrator/inspector + manifest verify 全绿）
