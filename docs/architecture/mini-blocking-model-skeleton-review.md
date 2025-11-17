# Mini Blocking Model Skeleton Review

## 背景
- 依据 `docs/architecture/mini-blocking-model-plan.md`，`blocking-model/rust/blocking_model_core/src/skeleton` 仍以复刻 `xi-editor-ph7/rust/rope` 的类型关系与调用路径为目标，用最小 stub 验证 Cursor、GenericNode、Metric、Chunk 等阻塞点。
- Rust Porter 已在 `metrics.rs/tree.rs/rope.rs/samples.rs/tests/skeleton.rs` 补齐 `Metric`/`DefaultMetricProvider`、`Leaf` 约束、`NodeBody`+`SharedNode` COW 包装、`TreeBuilder` stub、`CursorDescriptor`/`CursorState`（feature gate）以及 `Rope::cursor/apply_descriptor` 调用链，并通过 `sample_rope_via_builder` 的 builder 路径与回归测试覆盖核心类型。
- 目前仍缺少 `DefaultMetricProvider` 的真实转换逻辑、`TreeBuilder` 事件栈（`tree_builder_slice_trace`）、`CursorState` 还原路径、`Rope::edit`/迭代 API 的运行链以及 CLI fixture 与 `helpers/string_leaf` 对应 stub，需要在矩阵和建议中持续跟踪。

## 组件矩阵
| 组件/关系 | 状态 | 备注 |
| --- | --- | --- |
| Metric | Partially aligned | `Metric<N,L>` 与 `DefaultMetricProvider` 已与 `xi-rope` 同步，`BaseMetric/Utf16Metric` 也提供占位实现，但 `convert_*` 仍简单透传 offset，未覆盖真实度量切换与 shim 流程。 |
| Leaf | Aligned | `Leaf` trait 现在暴露 `len/is_ok_child/push_maybe_split/Default`，`SampleLeaf` 可复用字符串 stub；只需在后续阶段挂接 `helpers/string_leaf.rs` 常量即可。 |
| Node | Partially aligned | `NodeBody`、`NodeVal`、`PathFrame` 与 `Node::from_leaf/from_children` 已存在，但缺少 `edit/concat/ensure_unique` 等入口，暂时无法验证 COW 写入与 metric 累积。 |
| SharedNode | Partially aligned | `Arc` 句柄、`clone_handle`、`ptr_eq` 已实现，仍缺 `make_mut/ensure_unique` 与 `NodeBody.height` 重计算逻辑，COW 与 Cursor 恢复路径需要进一步 stub。 |
| TreeBuilder | Partially aligned | `push_leaf/push_node/build` 与默认 builder 路径就绪，不过没有 `tree_builder_slice_trace` 事件栈，也未暴露 chunk 限制或 rebalancing hook。 |
| Cursor | Partially aligned | `Cursor`、`PathFrame`、`CursorDescriptor` 与 `cfg(feature = "cursor_state")` 的 `CursorState` 均可用，但 `restore` 仍强制使用根句柄，尚未记录沿途 `SharedNode`，对 cache 失效验证有限。 |
| Rope API | Partially aligned | `Rope::new/from_root/cursor/apply_descriptor/measure` 接口齐备，`edit/slice` 入口也存在，不过仍是占位 `unimplemented!()`，尚未串起 TreeBuilder、Metric、samples。 |
| Feature gate/helper | Partially aligned | `cursor_state` feature 已启用，`TreeBuilder` stub 也预留构造，但 `tree_builder_slice_trace`、`helpers/string_leaf.rs`、metrics helper CLI 仍未镜像。 |
| Fixtures | Aligned | `sample_rope` 与 `sample_rope_via_builder` 结合 `tests/skeleton.rs`（descriptor roundtrip、SharedNode clone）可覆盖基本流程，后续只需扩展深树/metric 场景。 |

## 建议
1. **完善 DefaultMetricProvider 逻辑**：在 `metrics.rs` 中将 `convert_from_default/convert_to_default` 与 `Metric::measure` 联动，覆盖 UTF-8/UTF-16 与 break metrics 的 shim；同步确认 `mini-blocking-model-plan.md` 中的 Metric 互操作任务是否需要新依赖。
2. **补写 TreeBuilder 事件栈**：为 `TreeBuilder` 引入 `tree_builder_slice_trace` stub、chunk 限制与 `Node::from_children` 平衡策略，并借助 `sample_rope_via_builder` 追加多层样本，让 `Rope::edit` 能串起 builder → SharedNode → metrics。
3. **深化 CursorState/恢复路径**：让 `Cursor::restore` 记录实际路径（非同一个根句柄），暴露 `CursorState` 内部字段并对 `cursor_state` feature 进行最小集成测试，便于后续 C# `NodeCursor` 套件对齐。
4. **扩展 Rope API & helpers**：实现 `Rope::edit/slice` 的最小 stub，调用 `TreeBuilder`/`SampleLeaf::push_maybe_split` 路径，并预留 `helpers/string_leaf` 与 builder fixture，确保 mini blocking model 可以替换 leaf 常量。
5. **同步文档/测试节奏**：`docs/architecture/mini-blocking-model-plan.md` 目前无需改动，但需在下次 TreeBuilder/Metric 更新后批量刷新；同时请 QA Engineer 扩充 `tests/skeleton.rs`（TreeBuilder、Metric 转换、cursor_state feature）以验证上述改动。

## 验证方法
- **静态检查**：在新增接口后运行 `cargo check -p blocking_model_core`，确保 skeleton trait 仍可独立编译；并用 `cargo test -p blocking_model_core skeleton` 保证样例覆盖基本类型协作。  
- **对拍验证**：对照 `xi-editor-ph7/rust/rope` 运行 `cargo test -p xi-rope`（或等效 workspace 命令），确认 skeleton 暴露的 trait/函数名称与主仓一致。  
- **文档同步**：每次 skeleton 扩展后更新 `docs/architecture/mini-blocking-model-plan.md` 与 `docs/architecture/rope-port-mapping.md` 的对应行，Architecture Mapper 在 `agents/architecture-mapper.md` 登记状态。
