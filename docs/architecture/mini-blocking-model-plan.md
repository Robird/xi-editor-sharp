# Mini Blocking Model 计划

> 目的：在独立于主线实现的精简 {Rust + C#} 配对工程中，为每个阻塞移植问题建立最小可复现模型，集中验证类型系统与软件工程策略，再将结论安全地回放到主仓库。

## 目标
- **降噪**：剥离编辑器业务，仅保留阻塞点所需的类型/接口，确保上下文窗口集中在纯技术问题。
- **快迭代**：在小仓内快速创建/丢弃实验，把成功的接口与测试沉淀为可重复资产。
- **成对验证**：所有实验同时提供 Rust/C# 版本，保持语义一致并记录差异。
- **可回放**：每项实验完结时给出“如何迁回主线”的 checklist，并在 `AGENTS.md`/认知档案更新状态。

## 范围
- 当前建模的阻塞点：
  1. **CursorLifecycle**：拥有型游标 + descriptor 失效策略。
  2. **GenericNode**：`Node<TInfo, TLeaf>` builder/验证骨架。
  3. **MetricInteroperation**：`convert_*` shim 与静态/动态接口协同。
  4. **ChunkEnumeration**：Chunk/Line owned descriptor + diagnostics。
  5. **GraphemeNavigation**：降级策略 + telemetry 阈值。
  6. **BreaksTree**：Breaks descriptor + search shim。
- 若阻塞点之间存在强耦合（例如 Cursor + Chunk），优先在同一实验中建模，让跨模块依赖在 mini workspace 中一次性暴露。
- 后续若新增阻塞点，需在本文件追加条目并同步 `BlockingPointRegistry`（C#/Rust）与 `AGENTS.md`。

## 工程结构
```
blocking-model/
  README.md               # 共享约束与使用说明
  csharp/                 # .NET 9 解决方案
    BlockingModel.sln
    BlockingModel.Core/   # 阻塞点建模库
    BlockingModel.Tests/  # xUnit 测试，含 parity/diagnostics
  rust/
    Cargo.toml            # Workspace，统一管理实验 crate
    blocking_model_core/  # 阻塞点建模库 + 测试
```

### C# 工作区
- `BlockingPointRegistry`：列举阻塞点、目标、Rust/C# 对应实验提示。
- 后续每个阻塞点创建单独文件夹（例如 `CursorLifecycle/`）存放接口原型与测试。
- `BlockingModel.Tests` 负责：
  - 验证 registry 完整性。
  - 为每个阻塞点提供最小属性/单测，必要时引入 Snapshot/Fixture。
  - 通过 `[Trait]` 或命名约定区分实验阶段，便于选择性运行。

### Rust 工作区
- `blocking_model_core` 镜像 C# registry，提供相同的枚举/Spec，并新增 `skeleton/` 模块维护 `Node/SharedNode/Cursor/Rope` 最小骨架。
- 每个阻塞点可以直接在同一 crate 内通过 module/feature 展开，保留 `xi-editor-ph7/rust/rope` 的静态签名与调用路径，具体逻辑用占位返回值代替。
- CLI/fixture 草案也在该 workspace 中先行建模，再迁回主仓 `xi-editor-ph7`。

## Rust Skeleton 指南
- **目标**：在不引入业务逻辑的前提下，保留 `xi-editor-ph7/rust/rope` 中核心类型（`Metric`、`NodeInfo`、`Leaf`、`Node`、`SharedNode`、`Cursor`、`CursorDescriptor`、`Rope`）的签名、约束与调用关系。
- **结构**：`blocking_model_core::skeleton` 暴露 `metrics/tree/rope/samples` 四个子模块，并提供 `prelude` 便于快速引用；`samples.rs` 内含 `SampleNodeInfo`/`SampleLeaf`，可用于快速生成 `Rope` 与 `Cursor`。
- **测试**：`tests/skeleton.rs` 只验证“类型能协同工作”与“descriptor roundtrip”两类约束，确保未来扩展时能保持静态关系；任何新增 skeleton API 都需同步加入类似的约束测试。
- **演进**：当需要针对特定阻塞点扩展结构（如 chunk descriptor、CursorState），优先在 skeleton module 内添加类型/接口，再通过占位实现保证不会与主仓逻辑冲突。

## 实施流程
1. **登记任务**：在 `docs/architecture/mini-blocking-model-plan.md` 记录新的阻塞点或实验阶段，同时更新 `AGENTS.md` 的“当前聚焦事项”。
2. **实现模型**：
   - Rust/C# 各自创建对应 module + 测试。
   - 若需要 CLI/脚本，先在 mini workspace 验证明细（可使用 PowerShell/Python 小工具）。
3. **验证**：运行 `dotnet test blocking-model/csharp/BlockingModel.sln` 与 `cargo test`（workspace 根）。
4. **沉淀结论**：
   - 在本文件追加“完成记录”子章节。
   - 在相关文档（`rope-port-mapping.md`、`type-system-migration-log.md` 等）贴出链接。
   - 更新 `AGENTS.md` 工作日志，说明验证内容、结果与下一步。
5. **迁回主线**：通过 PR 或文档对齐的方式，把成熟解法复制到 `src/xi.Core` / `xi-editor-ph7`。

## 阶段同步（2025-11-17）

### TreeBuilder / Helpers 现状
- `blocking_model_core/src/skeleton/helpers/string_leaf.rs` 已提供 `MIN_LEAF/MAX_LEAF/NEWLINE_WINDOW` 常量、UTF-16 计数与拆分策略，`SampleLeaf`/`Rope`/tests 全部接线，Stage D 的“Helpers” 子任务在 mini skeleton 内具备真实入口。
- `Cargo.toml` 新增 `tree_builder_slice_trace` feature gate，`TreeBuilderTracer` 仅在启用时录制事件；`Rope::edit/slice` 经过 flatten -> helper 拆分 -> `TreeBuilder` 重建的流程，满足 Stage D 的 trace/重建基础要求。

### 深树 fixture 与 Stage D 资产
- `sample_deep_tree_rope(depth)` 可以产出 `TreeBuilderTrace` + cursor roundtrip，`tests/skeleton.rs` 在开启 `cursor_state` 与 `tree_builder_slice_trace` 时会断言 trace 非空，提供 Stage D 深树验证素材。
- 该 fixture 仍需被 CLI/serde exporter 和 C# mini workspace 摄取，才能在 Stage D checklist 中形成“深树 ingestion” 步骤。

### Stage D / CLI 串联待办
- 将 `tree_builder_slice_trace` 输出串联到 Rust CLI/serde，明确事件 schema、chunk 限制与默认采样策略，并提供 C# parity 脚本。
- 把 helpers 的 newline window、UTF-16 细节与 COW/incremental edit 的真实流程纳入 Stage D 任务，避免当前 flatten 重建掩盖性能或信息丢失风险。
- 在 QA/文档侧新增“深树 roundtrip + trace 导出 + helper parity” 的联动检查，确保 Stage D 资产可直接支持主线 CLI 与 C# 端验证。

## SubAgent 指南
- 召唤 AI 员工时，指示其先阅读 `agents/<role>.md` 与本文件，确认职责与上下文。
- 强调“完成任务前必须更新自身认知档案”的要求，再向主 Agent 汇报。
- 建议任务模板：
  1. 阅读本计划 + registry 代码，了解阻塞点定义。
  2. 在 mini workspace 中实现/扩展指定模块与测试。
  3. 运行相应测试命令，并在档案中记录结果。

## 下一步（2025-11-17）
- [ ] 在 `BlockingModel.Core`/`blocking_model_core` 中为每个阻塞点添加占位 module + TODO 注释。
- [ ] 设计统一的 parity fixture 目录（如 `blocking-model/fixtures/`），方便共享 JSON。
- [ ] 将 mini workspace 运行命令添加至 `AGENTS.md`“质量门禁”提醒。
- [ ] 评估是否需要引入独立的 `agents/type-system-specialist.md` 来专职维护该 workspace。

## Backlog（新增）
1. **串接 tree_builder_slice_trace 序列化 CLI**：在 mini workspace 先定义 `TreeBuilderTrace` 的 serde schema、导出命令与示例 JSON，再回写至 `xi-editor-ph7` CLI，Stage D 依赖该任务完成。
2. **强化 helpers parity**：补充 newline window 诊断、UTF-16 多指标与 Breaks/Lines helper，让 Rust/C# 双端可引用相同常量/函数，并形成可回放的对拍测试。
3. **深树 fixture ingestion**：把 `sample_deep_tree_rope` 输出的 trace/cursor JSON 纳入 CLI/QA/C#，建立“深树 roundtrip” baseline，并规划 fixture 刷新脚本。
4. **真实 incremental edit/slice**：在 skeleton 里驱动 `Node::edit/SharedNode::make_mut`（或 stub），记录版本计数与 metrics delta，确保 Stage D 可观察 COW/缓存策略。
5. **QA 联动脚本**：为 `cursor_state` + `tree_builder_slice_trace` 提供组合测试入口（PowerShell/pytest 均可），将结果写回 `AGENTS.md` 与主文档。
