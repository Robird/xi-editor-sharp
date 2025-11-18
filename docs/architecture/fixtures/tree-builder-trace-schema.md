# Tree Builder Trace Schema

> **Anchor**：`[Fixture-TreeTraceSchema]`
> **Owner**：Architecture Mapper · Information Researcher · Rust Porter
> **Update Frequency**：必要时（CLI 字段或验证管线变更）
> **Scope**：定义 `export-tree-builder-trace` 所产生 JSON 的强制字段、版本策略、验证门禁，并作为 `TreeTraceSchemaKit` Source Generator 的输入。

## [Fixture-TreeTraceSchema] Schema 概览
- **适用资产**：`tree_builder_trace*.json`（Rust CLI 导出，用于 Stage D/Parity/诊断）。
- **版本策略**：`metadata.schema_version = tree_builder_trace@<major>.<minor>.<patch>`；当字段集合或语义变化时必须 bump `major/minor`，SourceGen 代码生成器通过哈希锁定版本。
- **消费方**：
  1. `tools/TreeTraceSchemaKit`（C# Source Generator + Analyzer）在编译期生成验证器。
  2. `scripts/refresh_serialization_fixtures.ps1`（tree trace 现为默认输出，如需跳过需显式 `-SkipTreeTrace`）调用 `TreeTraceSchemaKit.Validator`，无验证通过不得落盘。
  3. QA 在 Stage D checklist（`[QA-IngestionSmoke]`/`[QA-ChunkBench]`/`[QA-Telemetry]`）记录 schema_version/rust_commit。

## [Fixture-Fields] 字段定义
| 字段 | 类型 | 约束 | 说明 |
| --- | --- | --- | --- |
| `metadata.schema_version` | `string` | 必填；格式 `tree_builder_trace@x.y.z` | 驱动 SourceGen 生成的版本化验证器。 |
| `metadata.rust_commit` | `string` | 必填；长度 7-40；仅含十六进制 | 记录 Rust CLI 构建 commit，供 Stage D 回溯。 |
| `metadata.generated_at_unix_millis` | `number` | 必填；`>= 0` | Unix 时间戳毫秒。 |
| `metadata.feature_gates` | `string[]` | 可空；元素来自 CLI gate 列表 | 记录 `serde`, `tree_builder_slice_trace` 等 gate。 |
| `metadata.tree_depth` | `number` | 必填；`>= 0` | 导出树的最大深度，用于 C# 端栈深度预警。 |
| `metadata.leaf_span_policy` | `string` | 必填；`Enum {"Span", "ArrayPool", "Hybrid"}` | 记录导出时的 Span/池化策略，便于诊断配置漂移。 |
| `payload.frames[]` | `object` | 必填；≥1 项 | 每帧描述 `node_kind`, `child_index`, `aggregate_metrics`。 |
| `payload.frames[].node_kind` | `string` | 必填；`Enum {"Leaf", "Internal"}` | 节点类型，驱动 TreeBuilderTracer 回放。 |
| `payload.frames[].child_index` | `number` | Leaf 固定 0；Internal 为子节点索引 | 与 `node_kind` 配合确定合并顺序。 |
| `payload.frames[].aggregate_metrics` | `object` | 允许空；字段 `code_units`, `graphemes`, `lines` | 供 Metric parity（`type-system-migration-log.md#[TS-B3]`）消费。 |

> **扩展字段**：未来如需追加 `payload.frames[].leaf_span_policy` 等字段，必须更新本表并 bump `schema_version`。

## [Fixture-Workflow] 导出与验证流程
1. **Rust CLI**：
   ```bash
   cargo run -p xi-rope --features serde,tree_builder_slice_trace --bin export-serde-fixtures -- \
     --tree-builder-trace --schema-version tree_builder_trace@1.0.0 --emit-manifest tests/xi.Core.Tests/Fixtures/fixtures.manifest.json
   ```
2. **Schema 校验**：
   ```bash
   dotnet run --project tools/TreeTraceSchemaKit.Validator -- \
     --schema docs/architecture/fixtures/tree-builder-trace-schema.md --input artifacts/tree_builder_trace.json
   ```
3. **刷新脚本**：`pwsh ./scripts/refresh_serialization_fixtures.ps1 -VerifySchema` 在导出后自动运行验证器，若只想刷新 tree trace 可保持默认参数；若调试时跳过 trace，需显式 `-SkipTreeTrace` 并补充原因。校验失败则退出非零状态。
4. **QA 记录**：QA 在 `docs/architecture/m3-implementation-plan.md §5.3` 与 `docs/architecture/qa/chunk-window-benchmark-log.md` 登记 `schema_version`/`rust_commit`，并将 manifest hash 写入 `[QA-IngestionSmoke]`。

## [Fixture-Timeline] 近期交付
| 里程碑 | 截止 | Owner | 说明 |
| --- | --- | --- | --- |
| Schema 草稿 + 验证表 | 2025-11-19 20:00 UTC+8 | Architecture Mapper · Information Researcher | 本文件 + 字段定义表提交评审。 |
| Rust CLI metadata 支持 | 2025-11-19 20:00 UTC+8 | Rust Porter | `export-tree-builder-trace` 输出 `schema_version/rust_commit/tree_depth/leaf_span_policy` 并列入 manifest。 |
| TreeTraceSchemaKit SourceGen | 2025-11-20 12:00 UTC+8 | C# Implementer | 生成 `TreeTraceSchemaValidator.g.cs`，供 `BlockingModel.Tests` 调用。 |
| QA Schema Gate | 2025-11-20 18:00 UTC+8 | QA Engineer | `cargo run ... --verify-schema` + `TreeTraceSchemaKit.Validator` 组合通过并记录到 `[QA-IngestionSmoke]`。 |

## [Fixture-ChangeLog]
| 日期 | 版本 | 修改者 | 说明 |
| --- | --- | --- | --- |
| 2025-11-18 | 0.1 | AI 架构师 | 创建初稿，基于 11/18 Cross-Branch Adoption Chat 决议，定义核心字段与验证流程。 |
