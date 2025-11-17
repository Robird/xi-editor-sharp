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

## 当前聚焦
- 将 Stage D/Goal Tree/CLI schema 保持单一事实来源，落在本档案与 `StageD::FixtureFlow` 之间可溯。
- 扩展 `export-serde-fixtures`（`--cursor-descriptors`/`--chunk-descriptors`/`--grapheme-windows`/`--breaks-descriptors`）并附 manifest + hash 策略。
- 记录 SharedNode、Metrics、Iterator Façade 等模块状态与下一步，确保与 `docs/architecture/rope-port-mapping.md`、Stage D anchors 一致。
- 明确 C#/QA 对接点，保证 CLI 输出、测试夹具与 schema 迭代节奏一致。

## Helper / CLI / Schema 状态

### Rust Helper 模块
| 模块 | 当前状态 | 下一步 | 对齐文档 |
| --- | --- | --- | --- |
| SharedNode / Tree (`xi-editor-ph7/rust/rope/src/tree.rs`) | `ensure_unique` 集中 `Arc::make_mut`，`clone_with_children`/`replace_child_range` 已封装；TreeBuilder trace 可选注入 | 在 Stage D 报告中补充 `SharedNode` 触点列表，并把 COW instrumentation 结果写回 `StageD::FixtureFlow.shared_node` | `docs/rust-refactor/shared-node-api.md`, `docs/architecture/rope-port-mapping.md` |
| Metrics Helper (`metrics/*`) | codepoint/lines/breaks/identity shim 已抽离；`convert_*` API 对应 C# shim；Breaks templating 文档已冻结 | M2 前补完 `Breaks` shim 方法簇并把结果填入 `StageD::FeatureGates.breaks_metrics`; 需要 QA 对断点覆盖给确认 | `docs/rust-refactor/breaks-metrics-templating.md`, `docs/architecture/rope-port-mapping.md` |
| Cursor Cache & Descriptor | Phase 1 完成（`CursorDescriptor` + CLI fixture）；`cursor_state` gate 下已有 `CursorState`/`Cursor::state()` | `--cursor-descriptors` 下一版需参数化 `build_deep_rope`、覆盖 Base/Lines/Utf16/Breaks 组合并记录 schema hash | `docs/rust-refactor/CursorCache.md`, `docs/architecture/m3-implementation-plan.md` |
| Iterator Façade (`Delta`, `iter_chunks`) | 处于调研阶段，visitor/Façade 策略在 `iterator-facade-export.md`；C# 侧暂以 Snapshot() 降级 | M2/M3 分两步：先交付 `Delta::iter_*` Façade + parity fixture，再引入 `iterator_facade` gate；需 Stage D anchor 标记 | `docs/rust-refactor/iterator-facade-export.md`, `StageD::FeatureGates.iterator_facade` |
| TreeBuilder Slice Trace | Feature gate 已上线，支撑 `StageD::FixtureFlow.tree_builder_trace` | 将 trace schema 纳入 manifest，并与 QA 确认消费脚本 | `docs/rust-refactor/TreeBuilderSliceStack.md`, `docs/architecture/rope-port-mapping.md` |

### Serde Exporter / CLI Schema
| Flag / Asset | 状态（覆盖范围） | 待交付内容 | 下游对接 |
| --- | --- | --- | --- |
| `--cursor-descriptors` | v1 CLI 输出 11 份 Base/Lines/Utf16 fixture，已被 `CursorDescriptorParityTests` 消费 | v2 需参数化 `build_deep_rope`、追加 Breaks metric/失效路径、记录 `schema_hash` 与 `payload_hash` | C# Implementer (`tests/xi.Core.Tests/Fixtures/cursor_descriptors`)，QA 通过 StageD manifest 追踪 |
| `--chunk-descriptors` | schema 草案完成（包含 `(byte_len, utf16_len)`、leaf path、跨叶标志），尚未落地 | 实作 CLI builder + `chunk_descriptor.rs` 测试，导出到 `tests/xi.Core.Tests/Fixtures/chunk_descriptors` | C# Chunk iterator、「Chunk smoke」测试；QA 需要样本计数；参考 `docs/rust-refactor/rope-generic-simplification-g.md` |
| `--grapheme-windows` | 设计列于 `GraphemeNavigation.md`，计划导出窗口 + fallback 标志 | 完成 `GraphemeCursor` 覆盖 + JSON schema 冻结，确保 UAX#29 版本字段写入 manifest | C# Grapheme parity、QA 验证 surrogate fallback |
| `--breaks-descriptors` | Breaks helper 已模块化，CLI 尚缺 | 生成断点位置/索引 JSON + `Breaks` shim API 演示，用于 Stage D `BreaksMetric` 交接 | C# Breaks 实装、QA 行内换行测试 |
| `--emit-manifest` / CLI schema | 草案：输出 `rust_commit`, `cli_rev`, `feature_gates[]`, `fixtures[]`(name,path,count,schema_hash,payload_hash) | 在 exporter 内引入 `--emit-manifest <path>`，默认 sha256(hash(canonical_json))，并让 `scripts/refresh_serialization_fixtures.ps1` 写回 `StageD::FixtureFlow` | `docs/architecture/fixtures/parity-fixture-schema.md`, `StageD::ParityAssets` |

**CLI Schema / Manifest / Hash 策略**
- Hash：对每份 JSON fixture 做 canonical JSON（排序 key + 去空白）后取 `sha256`，写入 manifest 的 `payload_hash`；`schema_hash` 取自 `parity-fixture-schema.md` 定义版本。
- Manifest：位于 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`（待建），由 exporter 生成，字段映射 Stage D anchors，并由 `goal_tree_sync.py` 校验。
- C#/QA 对接：`scripts/refresh_serialization_fixtures.ps1` 负责 orchestrate CLI（Rust）→ manifest → `docs/architecture` front-matter；QA 以 manifest 中的 hash 对比确保 ingest 资产未漂移。

## Feature Gates
| Feature Gate | 用途 | 当前状态 | 触发 / Owner |
| --- | --- | --- | --- |
| `serde` | 启用 serde fixtures / exporter | 默认开启于 Stage D 流程 | Rust Porter 维护 |
| `cursor_state` | CurState 缓存与 CLI 深度样本 | 已实现，默认关闭；仅 parity 验证时开启 | Rust Porter + C# Implementer 协调 |
| `tree_builder_slice_trace` | 记录 TreeBuilder push/pop trace | 已实现；Stage D 需要提供 trace 证据时开启 | Architecture Mapper 请求触发 |
| `iterator_facade` (计划) | 安全材化 `Delta`/chunk 迭代器 | 未实现；需在 M2/M3 期间 gated rollout | Rust Porter owner |
| `grapheme_windows` (计划) | Grapheme parity CLI | 未实现；与 `GraphemeNavigation` 评估绑定 | Rust Porter + QA |

## Stage D 输出接口
1. **Exporter 调用**：`cargo run -p xi-rope --features serde --bin export-serde-fixtures -- <flags>`；扩展 flags 由 Stage D 请求驱动并记录于 `StageD::ParityAssets`。
2. **Manifest 写回**：`--emit-manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 输出字段：`rust_commit`, `cli_rev`, `feature_gates`, `fixtures[]`；`goal_tree_sync.py` 读取 manifest，将哈希与计数写入 `StageD::FixtureFlow`。
3. **脚本链路**：`scripts/refresh_serialization_fixtures.ps1` 统一执行导出→manifest→docs front-matter 更新，保证 Stage D/Goal Tree 为单一事实源。
4. **对齐文档**：`docs/architecture/rope-port-mapping.md` 标注每个 helper/fixture 的 Rust 来源；`docs/csharp-refactor/rope-serialization-fixture-playbook.md` 说明 C# ingest，`docs/architecture/design-divergence-log.md` 记录降级。
5. **QA 介面**：QA 通过 manifest 中的 `schema_hash` + `payload_hash` 验证资产，并在 `tests/xi.Core.Tests/Fixtures/*` 中消费；若 hash mismatch，即触发 `StageD::FixtureFlow` 回归项。

## 最近完成
- **2025-11-18 - Stage D Descriptor Exporters Keep Timestamps Stable**：为 chunk/grapheme/breaks/diff/search 五类 exporter 增加“读取既有 JSON metadata 并沿用 generated_at_unix_millis”逻辑，只有在缺档或解析失败时才回退当前时间，解决 `StageDDescriptorLoaderTests` 因时间戳抖动持续失败的问题；在 `xi-editor-ph7/rust` 目录执行 `./run_all_checks --filter serde-fixtures` 验证 exporter、clippy、tests 与 serde fixture 路径全部通过。
- **2025-11-18 - Breaks Descriptor Clippy Fix & serde run_all_checks**：依 clippy::len-zero 建议在 `xi-editor-ph7/rust/rope/src/serde_fixtures/breaks_descriptors.rs` 改用 `Rope::is_empty()` 守护 `break_offsets`，保持语义不变；随后在 `xi-editor-ph7/rust` 目录执行 `./run_all_checks --filter serde-fixtures`，确认 clippy + tests + serde fixture exporters 全数通过，作为 Stage D 刷新前置信号记录给 QA。
- **2025-11-19 - Tree Trace & Telemetry Rollout Plan**：研读 11/18 脑暴会记录，梳理 `tree_builder_trace` schema metadata、`CursorEditTelemetry`、`ChunkWindowMetrics`、`GraphemeFallbackMetrics` CLI 需求，规划交付日期、触及文件与测试命令，并整理对 C#/QA/Architecture Mapper 的接口承诺与同步话术，待会后在 Stage D/Goal Tree 对齐。
- **2025-11-18 - Breaks/Diff/Search CLI Spec**：将 `--breaks-descriptors`/`--diff-regions`/`--search-spans` flag、输出目录、manifest 占位与 feature gate 说明写入 `[StageD::FixtureFlow]`、`[StageD::ParityAssets]`、`[StageD::FeatureGates]`，并在 `docs/architecture/fixtures/parity-fixture-schema.md` 增补 `breaks_descriptors@1.0.0`、`diff_regions@1.0.0`、`search_spans@1.0.0` schema（含字段表 + JSON 示例），同步更新 `[TS-B5]` 状态与 `rope-port-mapping.md`/`StageD` 资产表，确保 QA/Goal Tree 可追踪 “CLI spec ready, awaiting implementation”。
- **2025-11-18 - Tree Trace Manifest & Refresh Flow**：`export-serde-fixtures` 现将 `--tree-builder-trace` 输出写入 manifest（含 `tree_builder_slice_trace@1.0.0` schema、事件计数、payload hash），`scripts/refresh_serialization_fixtures.ps1 -ExportTreeTrace` 在一次 `cargo run --features serde,tree_builder_slice_trace` 调用里同步生成 parity + trace + manifest，并通过 `pwsh -File scripts/refresh_serialization_fixtures.ps1 -ExportTreeTrace -SkipRust -SkipDotnet -SkipStageDLoaderTest`、`python scripts/verify_fixture_manifest.py --update --manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json` 验证链路；`cargo test -p xi-rope --features serde,tree_builder_slice_trace -- tree_builder_slice_trace` 佐证 feature wiring 未破坏现有测试。
- **2025-11-17 - Exporter Manifest + Hash**：`export-serde-fixtures` 支持 `--emit-manifest` 默认写入 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`，完成 canonical JSON + sha256 策略并把结果写入 `scripts/refresh_serialization_fixtures.ps1`/`[Fixture-Manifest]`，Stage D 现在可用 manifest 校验 parity 资产。
- **2025-11-17 - Rust Porter 档案刷新**：按 front-matter + Stage D 结构重写认知档案，补齐 CLI 扩展、feature gate、Stage D 接口与 manifest/hash 策略，确保单一事实来源。
- **2025-11-17 - Stage D 文档模板精简评审**：定义 Goal Tree + Stage D anchors 保留字段与 CLI/feature gate 落点，提出 exporter manifest → 脚本写回 → anchor lint 三步闭环。
- **2025-11-17 - Parity Fixture Schema Freeze & Wiring**：冻结 `--cursor|chunk|grapheme` schema，扩展 PowerShell 刷新脚本默认导出 parity 资产，并记录 CLI 示例与样本计数。
- **2025-11-16 - Cursor Descriptor Fixture Exporter**：实现 `--cursor-descriptors` CLI、测试与 JSON schema，C# `CursorDescriptorParityTests` 已消费 11/11 样本。

## 待办 / 风险
- [TODO] `--cursor-descriptors` v2：参数化 `build_deep_rope`、注入 Breaks metric + 失效路径；依赖 Architecture Mapper 提供 schema 字段确认与样本目标数。
- [TODO] `--chunk-descriptors`/`--grapheme-windows`/`--breaks-descriptors`: 完成 CLI + 测试 + manifest wiring；需 QA 提供 ingest 计划避免资产漂移。
- [RISK] Iterator Façade：若 M2 内无法交付 visitor/Façade，将影响 C# chunk iterator 性能；需在 `StageD::FeatureGates.iterator_facade` 标注降级策略。
- [RISK] Leaf parity 触发条件未冻结：等待 Architecture Mapper/C# Implementer 在 48h 内回覆，否则缺少自动 guard。

## 关键文档索引
- Architecture：`docs/architecture/rope-port-mapping.md`, `docs/architecture/port-blueprint.md`, `docs/architecture/design-divergence-log.md`, `docs/architecture/fixtures/parity-fixture-schema.md`.
- Stage D & Goal Tree：`docs/architecture/document-structure-template.md`, `docs/architecture/m3-implementation-plan.md`, `docs/csharp-refactor/rope-serialization-fixture-playbook.md`, `docs/architecture/rope-port-mapping.md` (`StageD::FixtureFlow`).
- Rust Refactor 专题：`docs/rust-refactor/shared-node-api.md`, `docs/rust-refactor/CursorCache.md`, `docs/rust-refactor/breaks-metrics-templating.md`, `docs/rust-refactor/iterator-facade-export.md`, `docs/rust-refactor/GraphemeNavigation.md`, `docs/rust-refactor/TreeBuilderSliceStack.md`.
- CLI / 脚本：`xi-editor-ph7/rust/rope/src/bin/export-serde-fixtures.rs`, `scripts/refresh_serialization_fixtures.ps1`, `goal_tree_sync.py`.
- 测试/夹具入口：`tests/xi.Core.Tests/Fixtures/*`, `xi-editor-ph7/rust/rope/tests/` (cursor/chunk/grapheme descriptor测试待补), `xi-editor-ph7/rust/rope/src/serde_fixtures.rs`.
