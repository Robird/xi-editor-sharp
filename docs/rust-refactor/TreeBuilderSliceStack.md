**TreeBuilder Slice Stack**
- **Current Rust design**:  and  in  recursively descend intervals, pushing nodes onto a  stack and relying on / balancing during rebuild.
- **Portability blocker**: Reproducing the stack discipline requires translating multiple interval transforms (`translate`, `translate_neg`, `intersect`) and understanding when nodes are cloned vs. reused; subtle mistakes break balancing or leak nodes.
- **Potential Rust-side action**: Add an instrumentation hook (e.g., ) that emits a sequence of  before execution. Enabled only in debug/tests, it would document the exact push/pop schedule without mutating the runtime path.
- **Resulting C# implication**: Using the recorded plans as fixtures, the C# implementation can verify its stack-based slicing against Rust’s sequence, giving confidence that the translated stack logic preserves balancing and interval math before removing the diagnostics.

**Summary**
- TreeBuilder’s stack evolves through well-defined push/merge/pop paths that can be observed without mutating release logic.
- A debug-gated tracer capturing stack mutations plus interval transforms would materially aid the C# port in validating slice behavior.

**Key Findings**
- `TreeBuilder::push` keeps `stack: Vec<Vec<Node>>` in strictly descending heights, merging frames via `pop()` and `Node::concat`, exposing natural hook points before `stack.push`, `tos.push`, and each `pop` (`xi-editor-ph7/rust/rope/src/tree.rs:L510-L640`).
- `TreeBuilder::push_slice` reuses whole nodes when `iv == n.interval()` and otherwise descends, deriving child ranges with `translate`, `intersect`, `translate_neg`; only leaves are cloned via `Leaf::subseq`, clarifying when instrumentation must note reuse vs clone (`xi-editor-ph7/rust/rope/src/tree.rs:L642-L705`).
- Interval helpers (`translate*`, `intersect`) guarantee bounds-safe arithmetic, so logging the parent offset, translated range, and resulting interval is sufficient for replay (`xi-editor-ph7/rust/rope/src/interval.rs:L56-L130`).
- Port-mapping doc already calls for a “collect_slice_plan” guard, aligning with a feature-flagged tracer emitting push/pop and interval records (`docs/architecture/rope-port-mapping.md`).

**Risks & Unknowns**
- Need to agree on a stable node identifier (e.g., `Arc::as_ptr` hashed) to distinguish reuse without leaking raw pointers across FFI.
- Volume of trace data for large edits could be high; must ensure sampling or bounded buffers for practical fixture generation.
- Interaction with future TreeBuilder refactors (e.g., generic node work) might require tracer updates; no automated enforcement today.

**Proposed Implementation Steps in Rust**
- Add `#[cfg(feature = "tree_builder_slice_trace")]` gated `TreeBuilderTracer` trait plus `TreeBuilder::with_tracer`.
- Emit events for `PushFrame`, `ExtendFrame`, `MergePop`, `LeafSlice`, and `EnterChild` directly in `push`, `push_slice`, `push_leaf_slice`, and `pop`.
- Capture per-event metadata: stack depth, node height/len, interval (original & translated), and a reuse flag derived from pointer equality vs new leaf creation.
- Provide a lightweight default tracer stub so existing callers remain zero-cost when the feature is disabled.

**Validation Strategy**
- Add unit tests under the feature flag that slice representative ropes and assert recorded plans against golden JSON in Stage D fixtures.
- Integrate tracer runs into existing serialization refresh scripts (`scripts/refresh_serialization_fixtures.ps1`) to regenerate plans alongside text fixtures.
- Spot-check performance by running benches with and without the feature to confirm it compiles away in release builds.

**C# Port Implications**
- Serialized slice plans let the C# TreeBuilder mimic stack choreography, highlighting deviations in merge thresholds or interval math.
- The port can consume trace fixtures to drive deterministic tests ensuring both implementations produce identical push/pop sequences for shared ropes.
- Minimal metadata (depth, height, interval, reuse flag) aligns with current C# needs; no additional cross-language plumbing required.

**Recommendation**
Proceed with a feature-gated TreeBuilder slice-stack tracer: it is feasible, low-risk when disabled, and will materially de-risk the C# port’s slice parity work.

---

## 实施进度追踪（Implementation Progress Tracking）

### 当前状态（Current Status）
- **实施阶段**: 0 - 规划与准备
- **开始日期**: 2025-11-15
- **负责人**: AI SubAgent 调度
- **Rust 工作区状态**: 待确认 xi-editor-ph7 目录是否存在
- **C# 测试基线**: 102 项测试通过

### 阶段清单（Phase Checklist）

#### 阶段 0: 环境准备与背景调研 ✓
- [x] 理解 TreeBuilderSliceStack.md 文档内容
- [x] 确认 C# 侧测试基线（102 tests passing）
- [x] 添加实施进度追踪章节到文档
- [x] 确认 xi-editor-ph7 Rust 工作区状态 - **适配方案：先行设计规范**
- [ ] 验证现有 cargo test 基线（待 Rust 工作区可用时）

**环境适配说明**: 
- xi-editor-ph7 目录当前不在仓库中（已从 git 索引移除）
- **调整策略**: 先完成以下工作，Rust 实现可在工作区可用时补充：
  1. 详细设计 Rust 侧追踪器规范（trait 接口、事件格式、集成点）
  2. 实现 C# 侧消费基础设施（反序列化、验证、测试工具）
  3. 创建示例追踪数据作为格式参考与 C# 侧验证基础
  4. 提供完整的 Rust 实施指南文档

#### 阶段 1: Rust 追踪器接口设计
- [ ] 定义 `TreeBuilderTracer` trait 接口
  - 事件类型：`PushFrame`, `ExtendFrame`, `MergePop`, `LeafSlice`, `EnterChild`
  - 元数据字段：stack depth, node height/len, interval (original & translated), reuse flag
- [ ] 设计事件序列化格式（JSON schema）
- [ ] 添加 `tree_builder_slice_trace` feature flag 到 `xi-rope/Cargo.toml`
- [ ] 实现默认 no-op tracer stub

#### 阶段 2: TreeBuilder 埋点集成
- [ ] 在 `TreeBuilder::push` 中添加 `PushFrame`/`ExtendFrame`/`MergePop` 事件
- [ ] 在 `TreeBuilder::push_slice` 中添加 `EnterChild`/`LeafSlice` 事件
- [ ] 捕获 interval transforms (`translate`, `intersect`, `translate_neg`)
- [ ] 实现节点复用检测（pointer equality vs new allocation）
- [ ] 确保 feature 禁用时零成本（条件编译验证）

#### 阶段 3: 测试与夹具生成
- [ ] 添加 feature-gated 单元测试
  - 简单切片场景（单叶、跨叶）
  - 复杂嵌套场景（多层内部节点）
  - 边界情况（空切片、全量复制）
- [ ] 实现黄金 JSON 夹具生成
- [ ] 集成到 `scripts/refresh_serialization_fixtures.ps1`
- [ ] 添加回归测试验证夹具稳定性

#### 阶段 4: 性能与文档验证
- [ ] 运行 cargo bench 验证 feature 禁用时无性能影响
- [ ] 运行 cargo test 验证 feature 启用时功能正确
- [ ] 更新 `docs/architecture/rope-port-mapping.md` 标记追踪器完成
- [ ] 更新 `docs/architecture/port-blueprint.md` 添加 C# 消费指引
- [ ] 在 `AGENTS.md` 记录完成状态与后续计划

### 技术决策记录（Technical Decisions）

#### 节点标识符方案
- **待定**: 使用 `Arc::as_ptr` 哈希值还是序列号？
- **权衡**: 指针地址不跨进程稳定，序列号需要全局状态

#### 数据量控制
- **待定**: 是否需要采样或缓冲区限制？
- **策略**: 先实现完整追踪，根据实测数据决定优化方案

#### Feature Flag 命名
- **选定**: `tree_builder_slice_trace`
- **理由**: 明确作用域，与现有 `serde`/`cursor_state` 特性命名一致

### 风险与阻塞（Risks & Blockers）

#### 高优先级风险
1. **xi-editor-ph7 目录不存在**: 需要确认如何获取或是否有替代方案
2. **Rust 工作区编译问题**: 需要验证 cargo build/test 基线

#### 中优先级风险
1. **与泛型节点工作的冲突**: TreeBuilder 重构可能需要同步更新追踪器
2. **跨语言夹具同步**: 需要确保 Rust/C# 侧夹具格式兼容

### 下一步行动（Next Actions）
1. 检查 xi-editor-ph7 目录状态，必要时从文档/README 获取设置指引
2. 使用 SubAgent 实施阶段 1（Rust 追踪器接口设计）
3. 迭代实施阶段 2-4，每阶段完成后更新本文档

### 完成标准（Completion Criteria）
- [ ] 所有阶段清单项完成
- [ ] Rust 侧 `cargo test -p xi-rope --features tree_builder_slice_trace` 通过
- [ ] Rust 侧 `cargo test -p xi-rope --no-default-features` 通过（零成本验证）
- [ ] 黄金夹具生成并集成到刷新脚本
- [ ] 文档更新完成（port-blueprint, rope-port-mapping, AGENTS.md）
- [ ] C# 侧有明确的夹具消费指引
