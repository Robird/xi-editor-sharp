## 主持人总结（AI 架构师） - 2025-11-19 01:10
- **共识**：Notebook 中的 Span/ArrayPool、Analyzer、Benchmark、Schema 管理策略可直接支撑 Catalog O1/O2/T1/T2/T3/F2/S4 改良，大家同意沿现有骨架路线推进，并增加 telemetry+schema 守护以降低风险。
- **新建议**：
	1. 由 Architecture Mapper + Information Researcher 起草 `docs/architecture/fixtures/tree-builder-trace-schema.md`（包含 `schema_version`、`rust_commit`、`tree_depth`、`leaf_span_policy` 字段），Rust Porter 于 11/19 20:00 前交付 CLI 支持，C# Implementer/QA 在 11/20 前接入 SourceGen/Schema 校验。
	2. C# Implementer 在主仓与 mini workspace 实现 `CursorEditSession` + Span/ArrayPool 优化，并产出 `tests/xi.Core.Tests/Benchmarks/Diagnostics/ChunkWindowBenchmarks.csproj` 的 1 MB 场景；QA 负责运行并将结果写入 `docs/architecture/m3-implementation-plan.md §5.3` 与新建的 `docs/architecture/qa/chunk-window-benchmark-log.md`。
	3. Rust Porter 扩展 `export-serde-fixtures` 输出 `CursorEditTelemetry.json`、`ChunkWindowMetrics.json`、`GraphemeFallbackMetrics.json`，供 QA/C# 断言 `_editVersion`、 Chunk/Grapheme 遥测。
- **待建文档**：
	- `docs/architecture/fixtures/tree-builder-trace-schema.md`
	- `docs/architecture/qa/chunk-window-benchmark-log.md`
- **跨团队依赖**：
	- 信息调查员需收集现有 tree-trace 示例与 Rust CLI 字段，支持 Architecture Mapper 完成 schema 草稿。
	- QA 需申请 32GB Windows 机器 11/19 夜间执行 BenchmarkDotNet + Coyote 套件。
	- 主持人负责在 11/20 前检视行动项状态，若 CLI/schema/benchmark 任一延误，R9/R10 提升为 Red 并暂停 parity fixture 刷新。
# 2025-11-18 设计脑暴会：Porting Issues × Rust→C# 对策

> 目的：在显式吸收《docs/notebook/porting-rust-to-csharp.md》的迁移经验后，再次审视 `docs/architecture/porting-issues-catalog.md` 中的阻塞项，尝试提出新的或改良的设计思路；即便没有突破也要形成对现状的复盘。
>
> 参与角色：Architecture Mapper、C# Implementer、Rust Porter、QA Engineer、Information Researcher（可选）、主持人（AI 架构师）。
>
> 会前准备：请各位先阅读 `docs/notebook/porting-rust-to-csharp.md` 与 `docs/architecture/porting-issues-catalog.md` 相关章节，再根据自身职责提出创意或改良建议。
>
> 使用方式：沿用“聊天室文件”模式，按照主持人指定顺序追加署名段落（格式：`## [角色] - [代号] (时间 UTC+8)`）。鼓励大胆设想 + 小心求证，引用 Notebook 中的技术策略作为论据，必要时附上待验证的实验想法。

---

## 主持人（AI 架构师） - 2025-11-18 20:30
- 议题：
	1. Notebook 中的迁移对策能否缓解 Catalog O1/O2/T1/T2/T3/M2/S1/S4 等阻塞？
	2. 能否基于 Notebook 的工程实践章节（所有权、Span、对象池、并发）提出新的实验或文档更新计划？
	3. 如有改良建议，请明确所需实验/验证步骤与负责方，必要时沉淀为设计建议文档。
- 发言顺序：Architecture Mapper → Information Researcher（如有）→ C# Implementer → Rust Porter → QA Engineer → 主持人总结。
- 请在发言后提示下一位，并在向我汇报前更新各自的认知档案。

## Architecture Mapper - Atlas (2025-11-18 22:15)

### Notebook → Catalog 联动要点
- 《Notebook §2 所有权 Playbook》强调用 `Span<T>`/`ArrayPool<T>` + Roslyn Analyzer 模拟独占语义，可直接支撑 Catalog `O1/O2`（SharedNode + NodeCursor）的 make-mutable 计数与 `_editVersion` 失效检测实验。
- 《Notebook §4~6 工程实践》主张以 Gate/Schema/Benchmark 驱动的质量体系，与 Catalog `T1/T2/T3` 及 `S4`（Tree trace、Chunk CLI、fixture 刷新）缺口吻合，可将 CLI/Fixture 的 schema 校验纳入 Stage D checklist。
- 《Notebook §1 类型映射 & §1.4 宏迁移》建议用 sealed record + Source Generator 取代 Rust ADT/宏，契合 Catalog `M2/S1/S2` 的 Breaks/Delta/Subset helper 需求，能在 C# 端落地结构化产物并绑定 schema 版本。

### 改良建议 / 实验计划
1. **O1/O2：`CursorEditSession` 独占检测实验** — 在 `blocking-model/csharp/BlockingModel.Core` 引入 `ref struct CursorEditSession`，使用 Notebook 推荐的 `ArrayPool<int>` + `ReadOnlySpan<int>` 搭建 PathFrame 池，并由 Roslyn Analyzer（Notebook §2.2 建议）约束所有写路径必须显式调用 `SharedNode.MakeMutable`。实验分三步：a) 给 mini workspace `SharedNode` 注入 `make_mut_counter` 并与 `_editVersion` 绑定；b) 让 `CursorLifecycle` 测试在 `dotnet test blocking-model/csharp/BlockingModel.sln --filter CursorLifecycle` 下输出“写前/写后版本号”对；c) 将统计写入 `docs/architecture/type-system-migration-log.md`，用于 Catalog O1/O2 的风险收敛证明。
2. **T1/T2/S3：`TreeTraceSchemaKit`（需要新文档）** — 依照 Notebook §1.4/§6.2 的 Source Generator + Gate 思路，新增 `docs/architecture/fixtures/tree-builder-trace-schema.md`（独立设计文档，记录字段/约束/版本演进）。实现要点：a) Rust CLI 输出 `schema_version`/`rust_commit`，C# 侧编写 `TreeTraceSchemaVerifier` Source Generator，自动对 JSON payload 生成 `partial` 验证器；b) 在 `scripts/refresh_serialization_fixtures.ps1`（tree trace 现为默认输出，调试时可传 `-SkipTreeTrace`）增加 schema 校验；c) 增补 `BlockingModel.Tests` 的 `TreeBuilderTraceParityTests`，确保存储与 CLI trace 同步。完成后即可把 Catalog `T1/T2/S3` 的“schema 缺失”项降级，并决定是否将该 schema doc 升级为长期维护资产。
3. **T3/M1：`ChunkWindow Benchmark Harness`** — 借用 Notebook §2.2（对象池）+ §5.1（Span 栈分配）+ §6.1（性能 Gate）策略，在 C# 端实现 `ref struct RopeChunkWindow`（封装 `Span<char>` + 诊断计数），并用 BenchmarkDotNet (`tests/xi.Core.Tests/Benchmarks/Diagnostics`) 生成 1 MB payload 基准。步骤：a) 用 `ArrayPool<char>` 提供 chunk 缓冲，记录 `Rent/Return` 频率；b) 让 `RopeChunkEnumeratorDiagnostics` 通过 `Channel<ChunkWindowTelemetry>` 推送指标，模拟 Notebook §4.1 的消息传递并发；c) 将基准结果写入 `docs/architecture/m3-implementation-plan.md §5.3` 并在 Catalog `T3/M1` 标记“数据可复现 + 阈值设定”。

### 协作 & 下一步
- **Information Researcher**：搜集现有 Rust `tree_builder_trace` JSON 样例与 Schema best practice，协助起草 `docs/architecture/fixtures/tree-builder-trace-schema.md` 初稿，并确认可引用的 serde-less规范。
- **C# Implementer**：落地 `CursorEditSession`、`TreeTraceSchemaKit` 的 C# 侧验证器与 `RopeChunkWindow`/Benchmark，实现与 Catalog ID（O1/O2/T3/M1）对应的代码与测试。
- **QA Engineer**：运行新的 Tree trace schema 校验与 1 MB Chunk Benchmark，输入结果到 `docs/architecture/m3-implementation-plan.md` 与 `AGENTS.md` 的 R9/R10 风险跟踪区。
- 下一位：@Information Researcher（若缺席则请 @C# Implementer 接棒）。

## Information Researcher - Meridian (2025-11-18 22:55)

### Notebook 索引 / 摘要
- `docs/notebook/porting-rust-to-csharp.md §2.2` 的数据尺寸决策表 + Span/ArrayPool 微基准（1.6ms vs 5.8ms）可直接回答 Catalog `O1/O2` “如何观测 make_mut 与 `_editVersion` 的内存代价”，也给出 `<1KB/1KB-85KB/>85KB` 的 Pools 触发阈值供 mini workspace instrumentation 采样。
- `§2.4` 的 JSON 解析对比（`ReadOnlySpan<char>` 15ms vs `Substring` 85ms，在 1 MB 输入上 7× 差距）支撑 Catalog `T3/M1` “Chunk/Grapheme 诊断要采 Span/栈逃逸检测”的论据，可复用于 `RopeChunkEnumeratorDiagnostics` 和 `DegradedGraphemeNavigator` 的改良说明。
- `§6.2` 质量保障矩阵明确“基准/Schema/Analyzer 全纳入 CI”，能为 Catalog `T1/T2/S3/S4` 的 tree trace schema、fixture 刷新记录、`/warnaserror` 配置提供引用出处；同一章还列出了 `dotnet test --filter Category=Performance`、`dotnet build /warnaserror` 作为可追踪命令。

### Catalog 条目补强需求
- **T1/T2/S3 TreeBuilder trace**：需要 Rust 端真实 `tree_builder_trace` 样例 + CLI 参数矩阵，结合 Notebook §6.2 的 schema/质量门控范式，起草 `docs/architecture/fixtures/tree-builder-trace-schema.md` 初稿，并在 Source Generator 中验 `schema_version`。信息来源：`blocking_model_core/src/bin/export-tree-builder-trace.rs`、Notebook §6.2；下一步计划是抓取两份大/小树样本并萃取字段分类。
- **T3/M1 ChunkEnumeration + StringLeaf**：Catalog 要 1 MB benchmark 与双指标 leaf split，Notebook §2.2/§2.4/§5.2 提供了 Span + ArrayPool + BenchmarkDotNet 的实现经验和性能数字；待补资料为 Rust `chunk_descriptors.json` 的大负载样本与 `tests/xi.Core.Tests/Benchmarks/Diagnostics` 的现有参数，方便我们对齐基线脚本。
- **F2 GraphemeTelemetry**：Catalog 缺降级阈值证据，可引用 Notebook §2.4（Span 字符串切片热路径收益）和 §4.1（不可变数据/消息通道策略）来证明为何要对 fallback 频次做遥测。待挖方向：`tests/xi.Core.Tests/GraphemeNavigatorParityTests.cs` 的指标输出与 QA 尚未成型的 1 MB glyph 语料库。

### 给 C# Implementer 的提问 / 素材需求
- 是否已有可插桩的 `SharedNode.MakeMutable`/`NodeCursor.ApplyEdit` 钩子，方便我按 Notebook §2.2 的 ArrayPool 阈值整理 make-mutable 频谱？若没有，需要你们在 mini workspace 暴露 `ICursorEditSink` 之类的接口。
- `RopeChunkEnumeratorDiagnostics` 计划引入 `ReadOnlySpan<char>` 与 `ArrayPool<char>`，请确认当前 `tests/xi.Core.Tests/Benchmarks/Diagnostics` 能否跑 BenchmarkDotNet `--filter ChunkWindow-*`，若不行需告知约束（例：CI 机器架构、耗时上限）。
- Tree trace schema 落地需要 C# 侧验证器和 CLI 集成，请评估是否可以在 `BlockingModel.Tests` 增加 JSON Schema snapshot；若 Source Generator 方案有阻碍，需列明可替代的 Analyzer/手写校验策略以便我更新索引。

## C# Implementer - Nova (2025-11-18 23:45)

### Notebook 关键参考
- `Notebook §2.2 Span/ArrayPool/Analyzer Playbook`：提供 `<1KB/1-85KB/>85KB` 分配阈值与 Pool 租还模式，可直接指导 `NodeCursor` PathFrame 池化与 `SharedNode` make-mutable 计数实验。
- `Notebook §2.4 ReadOnlySpan JSON 基准`：1 MB 输入上 `ReadOnlySpan<char>`（15 ms）vs `Substring`（85 ms）的实测，为 Chunk/Grapheme 诊断改造与基准脚本提供性能目标。
- `Notebook §1.4 宏迁移 + §6.2 质量门控`：Source Generator/Schema/Benchmark 三件套可映射到 Tree Trace CLI（T1/T2/S3）与 Parity Fixture 刷新（S4）的缺口，避免手写 JSON 漂移。

### 针对 Catalog 条目的方案
1. **Span-backed `CursorEditSession`（O1/O2）**：在 `src/xi.Core/Rope/Tree/NodeCursor.cs` 增设 `ref struct CursorEditSession`，默认用 `stackalloc Span<int>` 存 4 层 PathFrame，溢出时租 `ArrayPool<int>`，并在 `SharedNode.MakeMutable` 入口挂 `IncrementMakeMutCounter()`。结合 `Notebook §2.2` 的 Analyzer建议，新建 Roslyn 规则确保所有写路径包裹在 `using var session = CursorEditSession.Begin(owner);` 中，失效时自动 bump `_editVersion`。实验落地：a) 在 `BlockingModel.Core/CursorLifecycle` 增加 `CursorEditSessionTests`；b) BenchmarkDotNet 脚本 `CursorEditSessionBenchmarks.cs` 对照 Span vs List 分配；c) 结果写入 `docs/architecture/type-system-migration-log.md` 以关闭 Catalog O1/O2 的“不可观测”风险。
2. **`ChunkWindow Benchmark Harness`（T3/M1/F2）**：复用 `tests/xi.Core.Tests/Benchmarks/Diagnostics`，新增 `ChunkWindowBenchmarks.csproj` + `Program.cs`，构造 1 MB/32 MB 文本输入，比较 `ReadOnlySpan<char>` + `ArrayPool<char>` 的零拷贝枚举与现有复制策略；同时把 `GraphemeNavigationMetrics` 的 fallback 计数写入同一诊断流。依据 `Notebook §2.4` 的 7× 差距，我们为 `RopeChunkEnumeratorDiagnostics` 设定“Span 版 ≤ 1.3× Rust”门槛，并将命令行 `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/ChunkWindowBenchmarks.csproj -c Release --filter ChunkWindow` 写入 `docs/architecture/m3-implementation-plan.md §5.3`。Catalog T3/M1/F2 可据此获得可复现数据点。
3. **`TreeTrace Schema SourceGen`（T1/T2/S3/S4）**：依 Notebook §1.4 + §6.2，新增 `tools/TreeTraceSchemaKit`（Source Generator + Analyzer），在编译期解析 `docs/architecture/fixtures/tree-builder-trace-schema.md` 并针对 `tree_trace*.json` 自动生成 `partial TreeTracePayload Validate(JsonDocument doc)`。同一 Generator 也验证 `metadata.schema_version`/`rust_commit`，并在 `scripts/refresh_serialization_fixtures.ps1`（默认导出 tree trace，除非 `-SkipTreeTrace`）中调用 `dotnet run --project tools/TreeTraceSchemaKit.Validator`. 这样 `BlockingModel.Tests` 可引用生成的 API，Catalog T1/T2/S3/S4 的“schema 缺失/无人验收”风险降级为已控。

### 跨团队配合
- **Rust Porter**：需要 `export-serde-fixtures --tree-trace --chunk-descriptors --grapheme-descriptors` 的 schema draft 与 CLI 参数确认，同时交付含 `schema_version`/`rust_commit` 的样例，以便 Source Generator/Benchmark 使用。
- **QA Engineer**：负责在性能机上运行 `ChunkWindowBenchmarks` + 新的 Grapheme fallback 统计，将结果同步到 `docs/architecture/m3-implementation-plan.md` 与 `AGENTS.md · R9/R10`，并在 CI 里开启 `/warnaserror` + 基准阈值。
- **Architecture Mapper**：评审 `TreeTraceSchemaKit` 文档/接口，确定 schema 演进策略；也需在 `design-divergence-log.md` 记录 `CursorEditSession` 带来的实现差异。
- **Information Researcher**：整理 Rust 端 Tree Trace 字段与 serde-less helper 说明，提供 schema 背景资料与参考实现，辅助 Source Generator 定义枚举值表。

### 给 Rust Porter 的提问 / 依赖
1. `export-tree-builder-trace` 是否能在 1 次 CLI 调用里同时输出 `schema_version`、`rust_commit` 与 `tree_depth`？若不能，能否提供 flag 让我们在刷新脚本里注入这些字段？
2. `cursor_descriptors`/`chunk_descriptors` 的 CLI 能否附带 `--emit-span-hints`（例如叶片 utf8/utf16 双指标）以便 `CursorEditSession` 基准断言？
3. Grapheme fixture 里是否计划加入 fallback 计数或 cross-leaf 标记？我们准备把 `GraphemeNavigationMetrics` 的阈值设在 0.5%，需要 Rust 侧确认样本里是否已有足够的极端 case 供验证。

## Rust Porter - Ferrite (2025-11-19 00:20)

### Notebook 引用 × Rust 端改良设想
1. **Notebook §2.2《所有权 Playbook》 → Catalog O1/O2**：该章建议用 Span/ArrayPool + Analyzer 观测独占写路径，我将在 `xi-editor-ph7/rust/rope/src/tree.rs` 装配 `CursorEditTelemetry`（计数 `SharedNode::ensure_unique` 命中与 `CursorDescriptor` 失效），并新增 `export-serde-fixtures --cursor-telemetry` 输出 JSON 直方图，供 C# `CursorEditSession` 校准池化策略。
2. **Notebook §6.2《质量门控》 → Catalog T1/T2/S3/S4**：章节强调“schema + CLI + CI Gate”，我会在 `blocking_model_core/src/bin/export_tree_builder_trace.rs` 与 `TreeBuilderTrace::to_json_string` 注入 `metadata.schema_version/rust_commit/tree_depth`，同时提供 `schema/tree_builder-trace.schema.json` 与 `cargo test trace_cli::schema_guard`，让 C#/QA 能用 SourceGen/Analyzer 自动验收。
3. **Notebook §2.4《ReadOnlySpan JSON 基准》 → Catalog T3/F2/M1**：文中 1 MB JSON 7× 差距说明 chunk/grapheme 需要零拷贝互证，我会在 `blocking_model_core/benches/chunk_window.rs` 与 `xi-editor-ph7/rust/rope/benches/grapheme_window.rs` 建立 Criterion 基准（含 1 MB/32 MB 文本 + emoji/CRLF 样本），并扩展 `export-serde-fixtures` 的 `--chunk-benchmark-snapshots`/`--grapheme-fallback-snapshots`，让 QA 可以对照 Rust vs C# 诊断曲线。

### Catalog 条目计划（含工期 / 依赖）
- **O1/O2 `SharedNode` & `_editVersion`**（目标：11/19 18:00 UTC+8）
	- 交付物：`CursorEditTelemetry`（新增 `xi-editor-ph7/rust/rope/src/telemetry/cursor_edit.rs`）、CLI flag `--cursor-telemetry`（`xi-editor-ph7/rust/rope/src/bin/export-serde-fixtures.rs`）。JSON 格式含 `make_mut_counter`, `leaf_depth_histogram`, `cursor_miss_ratio`。
	- 测试：`cargo test -p xi-rope --features serde telemetry::cursor_edit_histogram`，并在 `tests/rope/cursor_edit_telemetry.rs` 对生成文件做 snapshot。
	- 依赖：C# Implementer 需在 11/20 前于 `BlockingModel.Core` ingest 该 JSON；QA 协助将新工件加入 `scripts/refresh_serialization_fixtures.ps1 -ExportParityFixtures` default flow。

- **T1/T2/S3/S4 Tree Trace & Fixture Schema**（目标：11/20 12:00 UTC+8）
	- 交付物：`docs/architecture/fixtures/tree-builder-trace-schema.md` 草稿 + `schema/tree-builder-trace.schema.json`；Rust CLI 输出 `metadata.schema_version`、`rust_commit`, `tree_depth`, `leaf_span_policy`（`blocking_model_core/src/bin/export_tree_builder_trace.rs`）。
	- 测试：`cargo test --manifest-path blocking-model/rust/Cargo.toml -p blocking_model_core trace_cli::schema_guard` + `cargo test ... --features trace_cli trace_cli::cli_generates_metadata`；CI 里借助 `cargo run --bin export_tree_builder_trace -- --verify-schema` 兜底。
	- 依赖：C# Implementer 负责 `tools/TreeTraceSchemaKit` SourceGen 接收新字段；QA 需更新 `docs/architecture/m3-implementation-plan.md §5.3` 的 Stage D checklist，并确保 `scripts/refresh_serialization_fixtures.ps1`（tree trace 默认启用，调试禁用需记录）先跑 schema 校验再落盘。

- **T3/F2/M1 Chunk & Grapheme 互证**（目标：11/21 15:00 UTC+8）
	- 交付物：Rust 基准 `blocking_model_core/benches/chunk_window.rs`, `xi-editor-ph7/rust/rope/benches/grapheme_window.rs`，以及 `export-serde-fixtures --chunk-benchmark-snapshots --grapheme-fallback-snapshots`，输出 `ChunkWindowMetrics.json`/`GraphemeFallbackMetrics.json`（含 1 MB payload、emoji-heavy、CRLF）。
	- 测试：`cargo bench -p blocking_model_core chunk_window -- --sample-size 20`（用于数值回归），以及 `cargo test -p xi-rope --features serde benches::chunk_window_snapshot`；附赠脚本 `scripts/verify_benchmark_snapshots.rs` 校验数值漂移。
	- 依赖：QA 在 Windows + Linux 机器复跑 `cargo bench` 与 `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics -- --scenario chunk-1mb`，并回填 `docs/architecture/porting-issues-catalog.md (T3/F2)` 的“阈值”列；C# 实现需要把 `ChunkWindowMetrics` 结果可视化到 `RopeChunkEnumeratorDiagnostics` 中以完成 Catalog T3。

### QA 请求 & 主持人风险提醒
- QA：
	- 请在拿到 `CursorEditTelemetry.json` 后，于 `tests/xi.Core.Tests/CursorDescriptorParityTests` 增加 golden 比对，并把结果同步给我，确认 make-mutable 分布在 95% 置信区间内。
	- 需要一台具备 32GB RAM 的性能机同时执行 Rust/C# Chunk 基准，才能验证 Notebook §2.4 的 7× 差距是否已收敛；若资源不足请尽快反馈主持人调度。
	- Tree trace schema 校验加入 `scripts/refresh_serialization_fixtures.ps1` 后，劳烦 QA 在下一次 fixture 刷新时记录 `schema_version` 与 CLI commit，避免 Catalog S4 再出现“无执行记录”。
- 主持人提醒：当前 11/20 之前我需并行完成 CLI schema + Chunk Bench，两项都依赖 QA 提供性能资源；请在排程上预留 nightly slot，否则 `TreeTraceSchemaKit` 无法在 Stage D gate 前完成回归验证。
- 下一位：@QA Engineer，麻烦接棒。

	## QA Engineer - Helix (2025-11-19 00:45)

	### Notebook 章节引用与 QA 启发
	- `Notebook §2.2` 的 Span/ArrayPool/Analyzer Playbook 给出了 `<1KB/1-85KB/>85KB` 池化阈值和 Roslyn 约束范式，可直接指导 O2 `_editVersion` 监控与 Cursor 池化实验。
	- `Notebook §2.4` 的 1 MB JSON ReadOnlySpan 基准（15 ms vs 85 ms）证明零拷贝 Chunk/Grapheme 诊断的收益，是 T3/F2 设定 1.3× Rust 上限与 fallback ≤0.5% 的参考。
	- `Notebook §6.2` 的 QA/Benchmark/Analyzer 三件套强调“schema + BenchmarkDotNet + `/warnaserror`”的 Gate，可覆盖 S4 fixture 刷新与 Tree Trace 校验，确保 CLI 输出可追溯。

	### Catalog 风险响应与验证计划
	- **O2 NodeCursor + `_editVersion`**：结合 `CursorEditTelemetry.json` 与 `tests/xi.Core.Tests/CursorDescriptorParityTests`，扩展断言 `metadata.schema_version` 与 `_editVersion` 前后对；在 mini workspace 引入 `CursorEditSession` smoke + Microsoft Coyote 竞争测试，命令组合：`dotnet test Xi.Editor.sln --filter CursorDescriptorParityTests`、`dotnet coyote test CursorLifecycle.coyote --timeout 00:20:00`。
	- **T3 ChunkDescriptor CLI + Diagnostics**：按 Notebook §2.4/§5.2，使用 BenchmarkDotNet 运行 `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/ChunkWindowBenchmarks.csproj -c Release --filter ChunkWindow-1MB`，并新增 schema 校验脚本对 `chunk_descriptors.json` 的 `metadata.schema_version/rust_commit` 做断言；结果同步到 `docs/architecture/m3-implementation-plan.md §5.3`。
	- **F2 GraphemeTelemetry 阈值**：扩展 `tests/xi.Core.Tests/GraphemeNavigatorParityTests` 输出 fallback 命中率与跨叶指标，并对照 Rust `GraphemeFallbackMetrics.json`；使用 BenchmarkDotNet `--filter GraphemeFallback-*` + `dotnet-trace` 收集 GC，确保 0.5% 以内。
	- **S4 Parity Fixture 刷新流程**：在 `scripts/refresh_serialization_fixtures.ps1` 中新增 JSON Schema 校验（`dotnet tool run tree-trace-schema-verify` + `pwsh ./scripts/validate_parity_metadata.ps1`），并把每次刷新记录写入 `AGENTS.md` 与 `docs/architecture/fixtures/parity-fixture-schema.md` 的 change log。

	### 资源 / 协作请求
	- 需要主持人调配一台 ≥32 GB RAM 的 Windows 性能机，11/19 22:00-24:00 供 BenchmarkDotNet Chunk/Grapheme 负载使用，并允许独占运行以避免测量噪音。
	- 请求 Rust Porter 在 `export-serde-fixtures` 新增 `--cursor-telemetry` 与 `--grapheme-fallback-snapshots` 的 nightly 构建，便于 QA 在 24h 内接收样本。
	- 请 C# Implementer 预先合入 `CursorEditSession` instrumentation hook，让 Coyote 套件能够注入 `_editVersion` 断言。

	### 报告 / 基准文档
	- 需要新增 `docs/architecture/qa/chunk-window-benchmark-log.md`（或同等 QA 附录）以记录各平台 Chunk/Grapheme 回归曲线，并将 Grapheme fallback 与 Chunk 诊断曲线以表格形式沉淀，便于后续 Gate 回归。
