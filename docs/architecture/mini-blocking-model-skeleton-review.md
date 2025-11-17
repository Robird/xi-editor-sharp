# Mini Blocking Model Skeleton Review

## 背景
- Mini workspace 仍以还原 `xi-editor-ph7/rust/rope` 的类型关系为目标，但 Rust Porter 已把 `helpers/string_leaf.rs` 并入 skeleton，提供 `MIN_LEAF`/`MAX_LEAF`/`NEWLINE_WINDOW`、UTF-16 计数与拆分策略，并让 `SampleLeaf`/`Rope`/tests 全量引用该 helper。
- `blocking_model_core/Cargo.toml` 现在同时暴露 `cursor_state` 与 `tree_builder_slice_trace` feature；`TreeBuilderTracer` 只有在后者启用时才会记录/导出 `TreeBuilderTrace`，`tests/skeleton.rs::tree_builder_trace_export_respects_feature_gate` 用来验证 gate 行为。
- `Rope::edit`/`slice` 改为 flatten 原树 → 归一化区间 → 使用 helpers 拆分文本 → 通过 `TreeBuilder` 重建并在启用 `tree_builder_slice_trace` 时收集事件，`rope_edit_rebuilds_text_with_helpers` 与 `rope_slice_returns_interval_contents` 已覆盖新版流程。
- `sample_deep_tree_rope` 新增深层 fixture，结合 `deep_tree_sample_supports_cursor_roundtrip` 与 `cursor_state_roundtrip`（feature 开启）确保多层 descriptor/state 能复原，并在 trace feature 打开时附带非空事件。
- `trace_cli` feature 串连 `tree_builder_slice_trace` + `serde_json`，`TreeBuilderTrace::to_json_string()` 与 `export-tree-builder-trace` bin 可在文本重建（`rebuild_text_for_tests`）或 `sample_deep_tree_rope(depth)` 场景导出 JSON，`tests/trace_cli.rs` 验证 CLI 至少输出成对方括号；仍欠 schema/version 与 ingestion/脚本文档。

## 组件矩阵
| 组件/关系 | 状态 | 备注 |
| --- | --- | --- |
| Metric | Aligned | `Metric<N,L>` 与 `DefaultMetricProvider` 已能通过 `representative_leaf` 和 helpers 的 UTF-16 计数完成 roundtrip，`default_metric_provider_roundtrips_utf16_units` 佐证结构；仍欠多叶片拆分、Breaks/Lines shim 与 CLI 对拍。 |
| Leaf | Aligned | `Leaf` trait 现由 `SampleLeaf` + `helpers/string_leaf` 的 `MIN_LEAF/MAX_LEAF/NEWLINE_WINDOW` 驱动拆分与容量检查，UTF-16 计数也复用 helper；仍需把 newline window/诊断输出同步到 CLI。 |
| Node | Partially aligned | `NodeBody/NodeVal/PathFrame` 与 `Node::from_leaf/from_children` 已齐全，但缺少 `edit/concat/ensure_unique` 入口，COW 写入与 metric 累积仍未在 skeleton 内被驱动。 |
| SharedNode | Partially aligned | `Arc` 句柄、`clone_handle`、`ptr_eq` 已可复用，仍缺少 `make_mut` 与高度重算，导致 Cursor 恢复时只能读取静态 info。 |
| TreeBuilder | Partially aligned | `TreeBuilderTracer`、`TreeBuilderEvent`、`TreeBuilderTrace::export` 与 feature gate 均落地，`TreeBuilderTrace::to_json_string()` 已被 `export-tree-builder-trace` 复用；仍缺 chunk 限制、版本化 schema、CLI ingestion/脚本与真实 rebalance。 |
| Cursor | Aligned | `Cursor`、`CursorDescriptor`、`CursorState`（feature）在 `deep_tree_sample_supports_cursor_roundtrip`、`cursor_state_roundtrip` 中覆盖，证明 descriptor/state 可往返；仍需版本计数与缓存失效检测。 |
| Rope API | Partially aligned | `Rope::edit/slice` 会 flatten 文本、复用 helpers 重切叶片并经 `TreeBuilder` 重建；`rebuild_text_for_tests` 现在直接支撑 trace CLI 的文本输入路径，但仍未驱动真实 COW/metrics 累积或 chunk diagnostics，且 CLI 输出缺少 schema/version 说明。 |
| Feature gate/helper | Partially aligned | Cargo 现包含 `cursor_state`、`tree_builder_slice_trace` 与叠加的 `trace_cli`（启用 serde/serde_json、注册 CLI bin），`helpers/string_leaf` 也已挂到 leaf/samples/tests；但 JSON schema/version、ingestion 脚本与 metrics helper CLI 仍缺位。 |
| Fixtures | Aligned | `sample_rope`/`sample_rope_via_builder` 覆盖基础场景，`sample_deep_tree_rope` 已成为 CLI `--depth` 的默认深树 trace；仍需定义 schema/version、提供 ingestion/刷新脚本，并扩展多 metric/feature（helpers parity）样本供 QA/CLI 食用。 |

## 建议
1. **定义 trace JSON schema + CLI 脚本**：在 `docs/architecture` 记录事件字段/版本与 `--text/--depth/--out` 用例，附示例 JSON，并提供导出+校验脚本，形成 Stage D checklist 的入口。
2. **扩展 CLI 参数/fixture ingestion**：为 CLI 增加 schema 版本、输入/输出目录、fixture 选择等参数，确保 C#/QA 可直接 ingest `sample_deep_tree_rope` 与未来 Stage D 资产。
3. **记录 serde 依赖风险**：跟踪 `serde`/`serde_json` 的可选依赖与 feature gate，记录在计划/风险章节，避免 CLI 默认关闭时出现编译或 payload 不兼容问题。
4. **实现真实 incremental edit/slice**：让 `Rope::edit/slice` 逐叶驱动 `Node::edit`/`SharedNode::make_mut`、按 COW 规则重建信息，同时记录 metrics delta，避免长文本 flatten 重建造成性能错判。
5. **强化 helpers parity**：扩展 `helpers/string_leaf` 与 future metrics helper 的 newline window、UTF-16 累积、Breaks/Lines 诊断，确保常量/行为与 `xi-editor-ph7` 完全一致，并输出可比对数据给 CLI。
6. **深树 fixture ingestion**：把 `sample_deep_tree_rope`（含 trace）挂进 mini CLI、C# tests 与 QA pipeline，建立 Stage D “deep tree roundtrip” checklist，以验证 cursor/export 组合。
7. **QA 联动与特性矩阵**：在 QA 触发器中同时启用 `cursor_state` + `tree_builder_slice_trace`，补充 deep-tree roundtrip、helpers 拆分与 trace 空/非空断言，形成跨角色共享的判据。

## 验证方法
- **静态检查**：在新增接口后运行 `cargo check -p blocking_model_core`，确保 skeleton trait 仍可独立编译；并用 `cargo test -p blocking_model_core skeleton` 保证样例覆盖基本类型协作。  
- **对拍验证**：对照 `xi-editor-ph7/rust/rope` 运行 `cargo test -p xi-rope`（或等效 workspace 命令），确认 skeleton 暴露的 trait/函数名称与主仓一致。  
- **文档同步**：每次 skeleton 扩展后更新 `docs/architecture/mini-blocking-model-plan.md` 与 `docs/architecture/rope-port-mapping.md` 的对应行，Architecture Mapper 在 `agents/architecture-mapper.md` 登记状态。
