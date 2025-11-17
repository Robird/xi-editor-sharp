# Mini Blocking Model Skeleton Review

## 背景
- 依据 `docs/architecture/mini-blocking-model-plan.md`，`blocking-model/rust/blocking_model_core/src/skeleton` 仍以复刻 `xi-editor-ph7/rust/rope` 的类型关系与调用路径为目标，用最小 stub 验证 Cursor、GenericNode、Metric、Chunk 等阻塞点。
- `metrics.rs` 中的 `DefaultMetricProvider::convert_*` 现已通过 `representative_leaf` 与 `Metric::measure` 建立最小 roundtrip，`tree.rs` 提供 `TreeBuilderTracer`/`TreeBuilderEvent`，`tests/skeleton.rs` 亦加入 `default_metric_provider_roundtrips_utf16_units`、`tree_builder_tracer_records_events` 与 `#[cfg(feature = "cursor_state")] cursor_state_roundtrip`，证实 tracer 与 `cursor_state` feature 均可用。
- `Rope::edit/slice` 仍为占位逻辑，尚未对接真实叶片复制、`TreeBuilder` slice trace（`tree_builder_slice_trace` feature）、`helpers/string_leaf`、深树 fixture 以及 Cursor 恢复的版本计数；矩阵与建议需持续标记这些差距。

## 组件矩阵
| 组件/关系 | 状态 | 备注 |
| --- | --- | --- |
| Metric | Aligned | `Metric<N,L>` 与 `DefaultMetricProvider` 完整暴露，`convert_*` 现会通过 `representative_leaf`、`Metric::measure` 和 `SampleLeaf` stub 做 roundtrip；`tests/skeleton.rs::default_metric_provider_roundtrips_utf16_units` 可验证结构，但尚未覆盖多叶片拆分、Breaks/Lines shim。 |
| Leaf | Aligned | `Leaf` trait 暴露 `len/is_ok_child/push_maybe_split` 并由 `SampleLeaf` 的字符串实现佐证拆分策略，后续仅需把 `helpers/string_leaf.rs` 的常量/诊断移植进来。 |
| Node | Partially aligned | `NodeBody/NodeVal/PathFrame` 与 `Node::from_leaf/from_children` 已齐全，但缺少 `edit/concat/ensure_unique` 入口，COW 写入与 metric 累积仍未在 skeleton 内被驱动。 |
| SharedNode | Partially aligned | `Arc` 句柄、`clone_handle`、`ptr_eq` 已可复用，仍缺少 `make_mut` 与高度重算，导致 Cursor 恢复时只能读取静态 info。 |
| TreeBuilder | Partially aligned | `TreeBuilderTracer`、`TreeBuilderEvent` 与 `TreeBuilder::with_tracer` 均就绪（`tests/skeleton.rs::tree_builder_tracer_records_events` 覆盖），但未接入 `tree_builder_slice_trace` feature、chunk 限制或 slice trace 导出。 |
| Cursor | Aligned | `Cursor`、`CursorDescriptor` 与 `Cursor::restore` 现在会按 descriptor 路径逐层还原，并在 `cursor_state` feature 下注入 `CursorState` + roundtrip 测试，仍需后续扩展版本计数与 cache 失效检测。 |
| Rope API | Partially aligned | `Rope::new/from_root/cursor/apply_descriptor/measure` 结构完整，`Rope::edit/slice` 已串上 `TreeBuilderTracer` 与 `DefaultMetricProvider` roundtrip，但仍只返回默认叶片、缺少真实 splice/slice 行为。 |
| Feature gate/helper | Partially aligned | `blocking_model_core/Cargo.toml` 仅定义 `cursor_state` feature，TreeBuilder tracer 直接随构造暴露；`tree_builder_slice_trace` feature、`helpers/string_leaf`、metrics helper CLI 仍未移植到 mini workspace。 |
| Fixtures | Aligned | `sample_rope`/`sample_rope_via_builder` 结合 `TreeBuilderTracer`、`sample_rope_via_builder` 内的 roundtrip 断言，已提供基础与 builder 路径样本，尚欠深树/多 metric fixture。 |

## 建议
1. **补齐真实 edit/slice 行为**：在 `blocking_model_core/src/skeleton/rope.rs` 内让 `Rope::edit/slice` 复制 interval 内的实际叶片并驱动 `TreeBuilderTracer`，以便后续可以对 splice/slice 的 metric 结果做最小断言。
2. **引入 helpers 模块**：把 `xi-editor-ph7/rust/rope/helpers/string_leaf.rs` 与 metrics helper shim 的常量迁入 mini workspace，并在 `SampleLeaf::push_maybe_split` 之外提供统一 helper，使 `DefaultMetricProvider` 能覆盖 break/line metric 的转换。
3. **复刻 TreeBuilder slice trace**：新增 `tree_builder_slice_trace` feature gate，与 `TreeBuilderTracer` 对接 CLI/serde 输出，明确 chunk 限制与 EnterChild 样本生成流程，方便 C# 端共享事件序列。
4. **扩展 cursor_state 覆盖**：在 `tests/skeleton.rs` 添加深树 fixture（多层 builder + descriptor）并在开启 `cursor_state` feature 时验证还原路径、版本计数与 `SharedNode::ptr_eq`；同时为 `sample_rope_via_builder` 增加多 metric 场景。
5. **强化文档/测试协同**：待上述能力合入后，通知 QA 在 mini workspace 刷新 TreeBuilder/Metric/cursor_state 用例，并预留深树 fixture 以供 `docs/architecture/mini-blocking-model-plan.md` 下一次同步引用。

## 验证方法
- **静态检查**：在新增接口后运行 `cargo check -p blocking_model_core`，确保 skeleton trait 仍可独立编译；并用 `cargo test -p blocking_model_core skeleton` 保证样例覆盖基本类型协作。  
- **对拍验证**：对照 `xi-editor-ph7/rust/rope` 运行 `cargo test -p xi-rope`（或等效 workspace 命令），确认 skeleton 暴露的 trait/函数名称与主仓一致。  
- **文档同步**：每次 skeleton 扩展后更新 `docs/architecture/mini-blocking-model-plan.md` 与 `docs/architecture/rope-port-mapping.md` 的对应行，Architecture Mapper 在 `agents/architecture-mapper.md` 登记状态。
