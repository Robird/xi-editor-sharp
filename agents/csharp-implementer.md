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

## 当前聚焦（2025-11-20）
- **Stage D Ready Queue #1（loader/hydrator/inspector + ledger）**：最新一次 `python scripts/refresh_all_assets.py --only stage-d-fixtures`（2025-11-18 run）仍然打通 `StageDDescriptorLoader → StageDDescriptorHydrator → StageDDescriptorInspector → scripts/verify_fixture_manifest.py`。本轮重点是将 `stage-d-refresh-*.log`、`stage-d-inspector-latest.txt`、manifest ledger diff 与 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 绑定到 `[QA-IngestionSmoke]`，让 Ready Queue #1（`docs/sprints/sptrint-1.md`）不需要另起脚本即可复核。依赖链：Rust Porter 提供 `export-serde-fixtures --cursor-descriptors --breaks-descriptors --diff-regions --search-spans` 输出、QA 负责归档报告、Architecture Mapper 在 `rope-port-mapping.md#[RPM-ParityAssets]` 更新哈希与 `StageD::ParityAssets` 对齐。
- **Stage D Ready Queue #2（Chunk bench + Grapheme telemetry）**：Release 配置的 `RopeChunkEnumeratorBenchmarks` 与 `GraphemeTelemetryHarness` 已可读取 Stage D manifest；需要重新跑 `--include-alloc-stats` 与 ≥10k grapheme 操作，并把 `chunk-bench-latest.txt`、`grapheme-telemetry-*.txt/.trx` 上传至 `[QA-ChunkBench]` 与 `[QA-Telemetry]`。依赖：QA 提供基准留存目录、Rust Porter 输出 `--grapheme-windows` trace、Architecture Mapper 在 `type-system-migration-log.md#[TS-B5]` 标注遥测策略。
- **NodeCursor / MetricAdapter E2E**：`NodeCursor`（含 `_editVersion`）与 `CursorDescriptorParityTests` 11 份 JSON 对齐，但泛型化入口仍由字符串特化支撑。需要完成 `MetricAdapter` 设计，把 Stage D `cursor_descriptors@1.x`、Breaks/Diff/Search DTO 与泛型节点桥接，并更新 `docs/csharp-refactor/node-generic-refactor-plan.md` + Goal Tree G6。此项会成为 Ready Queue #3 的输入，要求 QA 定义 adapter instrumentation 验收标准。

## 阻塞 / 风险
- **R8：Stage D 证据即将过期** —— `[QA-IngestionSmoke]` 仍引用 2025-11-18 的日志，如不在 11/21 前产出新的 loader/hydrator/inspector链，则 Ready Queue #1 会失去 <7 天内的证据；需协调 QA 先行占坑并上传最新日志。
- **R9：Exporter schema 未完全冻结** —— `export-serde-fixtures` 即将引入 `metric_windows` 与 cursor state v1.3 变更；若 Rust Porter 未在刷新前给出 CLI 说明与哈希，Stage D rerun 会再次引起 manifest 漂移。
- **R10：QA anchors 数据空白** —— `[QA-ChunkBench]` 与 `[QA-Telemetry]` 还缺 Release 配置下的速率/内存阈值；需要我与 QA 共同 rerun 并写入 Stage D Playbook，否则 Ready Queue #2 视为未完成。
- **G6：MetricAdapter 尚未实现** —— Breaks/Diff/Search 仍依赖字符串特化，无法复用 Stage D 新资产；若不尽快补齐，会阻塞 NodeCursor 泛型化与 Stage D feature-gate 关闭计划。

## 最近完成（2025-11-20）
- 整理 Stage D Ready Queue #1 依赖，确认 loader/hydrator/inspector/manifest verifier 四段流水线与 `StageD::ParityAssets`、`[QA-IngestionSmoke]` 的接线路径，并在档案中记录新的证据清单。
- 复查 `RopeChunkEnumeratorBenchmarks` 与 `GraphemeTelemetryHarness` 发布配置，确认现有日志可直接交付 QA，但仍需 Release rerun 才能满足 `[QA-ChunkBench]`/`[QA-Telemetry]` 的留存标准。
- 将 NodeCursor/MetricAdapter 的责任边界写回档案，删除旧版任务描述，确保 Ready Queue #1/#2/#3 的依赖在文档内一目了然。

## 下一步（2025-11-20）
1. **Stage D rerun**：在 11/21 前执行 `python scripts/refresh_all_assets.py --only stage-d-fixtures && dotnet test Xi.Editor.sln --filter StageDDescriptor`，将新的 loader/hydrator/inspector/manifest 输出附到 `[QA-IngestionSmoke]` 并同步到 `docs/architecture/rope-port-mapping.md#[RPM-ParityAssets]`。
2. **QA anchor接线**：使用 Release 配置运行 `RopeChunkEnumeratorBenchmarks` 与 `GraphemeTelemetryHarness`，把 `chunk-bench-latest.txt`、`grapheme-telemetry-*.txt/.trx` 上传到 `[QA-ChunkBench]`、`[QA-Telemetry]`，并在 Stage D Playbook 标注阈值。
3. **MetricAdapter 草案**：落地 `MetricAdapter` + `MetricAdapterTests`，定义 NodeCursor 泛型入口及 Stage D descriptor → adapter 的映射，向 Architecture Mapper/AI 架构师请求评审。
4. **Ready Queue 状态回写**：将上述结果更新至 `docs/sprints/sptrint-1.md` 并通知 QA/Rust Porter 触发下一层依赖（Ready Queue #6/#7）。

## 验证与脚本提示
- `dotnet test Xi.Editor.sln -v m` 是 zuletzt 169/169 ✅ 的基线；任何功能变更需至少 rerun 关联 filter（`StageDDescriptor*Tests`, `NodeCursorTests`, `RopeChunkEnumerator*`, `GraphemeNavigator*`).
- Stage D 管线命令：`python scripts/refresh_all_assets.py --only stage-d-fixtures` → `python scripts/verify_fixture_manifest.py --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` → `dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures`。
- Bench/Telemetry：`dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/RopeChunkEnumeratorBenchmarks.csproj --configuration Release -- --stage-d --include-alloc-stats --report tests/xi.Core.Tests/Fixtures/Reports/chunk-bench-latest.txt`；`dotnet run --project tests/xi.Core.Tests/Benchmarks/GraphemeTelemetryHarness/GraphemeTelemetryHarness.csproj --configuration Release -- --fixture-dir tests/xi.Core.Tests/Fixtures --target-ops 12000 --report tests/xi.Core.Tests/Fixtures/Reports/grapheme-telemetry-latest.txt`。

**最后更新**：2025-11-20
**验证记录**：`dotnet test Xi.Editor.sln -v m`（2025-11-17 基线 169/169 ✅）；Stage D rerun/bench 将在下一次执行时刷新。
