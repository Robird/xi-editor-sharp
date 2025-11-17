# SharedNode API Extraction

## Context
- Current `Node` copy-on-write semantics depend on `Arc::make_mut` and direct access to internals (`NodeBody`, `NodeRef`).
- C# lacks an equivalent to Rust's implicit COW on `Arc<T>`, so we need a helper layer that makes ownership transitions explicit and portable.
- Several editing paths (`with_child_replaced`, `clone_with_children`, `borrow_mut_leaf`) repeat low-level `Arc` logic, causing divergent translations and brittle tests.

## Problem Statement
- Direct `Arc::make_mut` invocations hide whether mutations consume or clone the underlying buffer, making it hard to mirror behavior across languages.
- Unit tests rely on implicit invariants (reference counts, leaf sharing) that are not captured by a reusable API surface.
- Without a shared abstraction, we must reimplement ownership checks in C#, increasing risk of subtle differences in aliasing or structural sharing.

## 实施要点（原计划回顾）
1. Introduce a `SharedNode` wrapper struct encapsulating `Arc<NodeBody>` and exposing explicit helpers:
   - `ensure_unique(&mut self) -> &mut NodeBody`
   - `clone_with_children(&self, new_children: SmallVec<[Arc<NodeBody>; 4]>) -> SharedNode`
   - `replace_child_range(&mut self, range: Range<usize>, replacements: &[SharedNode])`
2. Update existing `Node` editing methods to call the new helpers instead of raw `Arc::make_mut`.
3. Add debug-only instrumentation (feature-gated) to track clone counts for regression tests.
4. Export matching helper signatures in `rope-port-mapping.md` for C# parity.

## Expected Benefits
- Aligns Rust and C# semantics by reducing the surface area of ownership-specific code to a small helper module.
- Simplifies porting by providing a clear set of methods to replicate in `SharedNode<T>` on the .NET side.
- Facilitates unit testing of COW behavior (e.g., verifying clone counts or exclusive access) without invasive instrumentation.

## Status Update（2025-11-14）
- **Rust**：`rope/src/tree.rs` 已切换为 `Node` 持有 `SharedNode`，所有 `Arc::make_mut` 触点集中在 `SharedNode::ensure_unique` 内；`clone_with_children`、`replace_child_range` 覆盖 TreeBuilder/节点拼接路径，`cargo test -p xi-rope` 通过。
- **C#**：`Tree/Node.cs` 引入对等的内部 `SharedNode` 类型，`EnsureUnique/CloneWithChildren/ReplaceChildRange` 成为唯一写时复制入口，维持 81 项 `xi.Core.Tests` 全部通过。
- **文档同步**：`docs/skeleton/xi.Core.decompiled.cs`、`rope-port-mapping.md` 与 `AGENTS.md` 已记录新的 helper API；本文件的调查结论转入实施经验回顾。

## Compatibility & Risks
- Requires auditing every call site touching `Arc::make_mut` to avoid missing stragglers.
- Additional wrapper could impact hot paths unless inlined; need benchmarks to confirm negligible overhead.
- Must ensure helper abstractions cover leaf and internal node scenarios without expanding public API unintentionally.

## Validation Plan
- Run `cargo test -p xi-rope` after each migration patch.
- Add dedicated tests checking that edits performed through `SharedNode` maintain sharing invariants.
- Measure editing benchmarks (if available) or add micro-bench to confirm no regression.

## Open Questions
- Should `SharedNode` expose reference count introspection for diagnostics?
- Can we reuse the wrapper for other modules (`breaks`, `spans`) without causing circular dependencies?
- Do we need to surface a no-alloc fast path when the caller already holds unique ownership?

## Investigation Findings (2025-11-13)
### Current Rust touch points
- Only two concrete invocations of `Arc::make_mut` remain in `rope/src/tree.rs`: `Node::with_leaf_mut` (used by `TreeBuilder::push` for leaf growth) and `Node::merge_leaves` (used by `Node::concat`). These are the choke points for copy-on-write but each re-implements the “clone leaf, recompute `len`/`info`” dance independently.
- Child replacement today is open-coded: `TreeBuilder::push` and `Node::merge_nodes` manually splice `Vec<Node<...>>`, re-accumulate metadata, and construct a fresh `Arc<NodeBody<...>>`. The tuple-struct layout (`Node(Arc<NodeBody<...>>)`), while private to `tree.rs`, still forces every helper to juggle raw `Arc` plumbing.
- The legacy `NodeRef` alias no longer exists; all access funnels through `Node` methods. This creates a good window to hide the `Arc` behind `SharedNode` without breaking external modules.

### C# parity takeaways
- The current C# port (`Tree/Node.cs`) already exposes `EnsureWritableLeaf`, `WithChildReplaced`, `CloneWithChildren`, and `ReplaceChildWithSegments` to make ownership transitions explicit. Eighty-one regression tests assert leaf-capacity invariants, COW behaviour, and structural sharing.
- Without equivalent Rust helpers, the mapping in `rope-port-mapping.md` stays conceptual; every new Rust-side tweak has to be rediscovered and hand-translated into C#, increasing divergence risk.

## Feasibility Deep-Dive
### Wrapper shape and integration
- Introduce `pub(crate) struct SharedNode<N: NodeInfo<L>, L: Leaf> { inner: Arc<NodeBody<N, L>> }` with `#[derive(Clone)]` and transparent `Debug`. `Node<N, L>` becomes a thin newtype over `SharedNode`, preserving the public API (constructors, iterators, `Deref`-like methods).
- Implement `SharedNode::ensure_unique(&mut self) -> &mut NodeBody<N, L>` as the sole site calling `Arc::make_mut`. Existing helpers (`Node::with_leaf_mut`, `merge_leaves`) delegate to it, eliminating duplicated bookkeeping.
- Expose small, `#[inline]` accessors (`as_ptr`, `height`, `len`, etc.) so the compiler can optimise away the wrapper in hot paths.

### API coverage matrix
| Proposed helper | Current Rust source | C# equivalent | Notes |
|-----------------|---------------------|--------------|-------|
| `ensure_unique(&mut self)` | `Node::with_leaf_mut`, `Node::merge_leaves` (`rope/src/tree.rs`) | `Node.EnsureWritableLeaf` | Centralises leaf cloning and info refresh before mutation. |
| `clone_with_children(&self, SmallVec<_>)` | `Node::from_nodes`, child regrouping in `merge_nodes`/`TreeBuilder::push` | `Node.CloneWithChildren` | Encapsulates rebuilding `Arc<NodeBody>` while preserving height/length aggregation. |
| `replace_child_range(&mut self, Range<usize>, &[SharedNode])` | Manual slice/concat logic in `TreeBuilder::push`, `Node::merge_nodes` | `Node.ReplaceChildWithSegments` | Provides a reusable splice primitive that enforces min/max child invariants. |

### Diagnostics and instrumentation
- Gate clone-count tracking behind a feature such as `shared_node_diagnostics`; use `AtomicU64` counters or scoped RAII guards to record `ensure_unique` calls without polluting release builds.
- Mirror the instrumentation surface in C# (e.g., debug-only counters on `SharedNode<T>`), so cross-language tests can assert identical clone behaviour.

### Impact on existing modules
- `breaks.rs`, `spans.rs`, `delta.rs`, and friends interact with `Node` solely through its public API, so wrapping the `Arc` is ABI-neutral as long as constructors and iterators keep their signatures.
- Cursor code (`Cursor<'a, N, L>`) holds `&Node`; it will continue to compile once `Node` delegates to `SharedNode`. Ensure the wrapper keeps pointer identity semantics (`ptr_eq`) by forwarding to `Arc::ptr_eq`.

## Implementation Path（已完成）
1. Add `shared_node.rs` (or an internal module within `tree.rs`) defining `SharedNode` plus its `ensure_unique` helper; re-export it as `pub(crate)`.
2. Refactor `Node` to hold a `SharedNode` instance, updating constructors (`from_leaf`, `from_nodes`) and `Clone` derives accordingly.
3. Replace direct `Arc::make_mut` usages in `with_leaf_mut` and `merge_leaves` with `SharedNode::ensure_unique`.
4. Lift the child-splicing logic from `TreeBuilder::push`/`Node::merge_nodes` into `SharedNode::clone_with_children` and `replace_child_range`, keeping behaviour identical via unit tests.
5. Introduce optional diagnostics behind a cargo feature and document the knobs in `rope-port-mapping.md` for C# parity.
6. Run `cargo test -p xi-rope` and refresh `docs/skeleton/rope.md` to capture the new helper surface; update C# documentation to point at the canonical API names.

## Risk Notes
- Extra indirection must stay zero-cost; liberal `#[inline]` and `#[repr(transparent)]` (if needed) keep codegen on par with the current tuple struct.
- Misusing `ensure_unique` on internal nodes could trigger unintended clones; helper APIs should encode the node kind (leaf vs internal) or expose assertions to catch misuse early.
- Any instrumentation must be thoroughly feature-gated—lingering atomic counters in release builds would regress hot-path performance.
- Pointer equality semantics (`ptr_eq`) must continue to mirror `Arc::ptr_eq`; add regression tests covering shared-leaf scenarios before and after the refactor.

## Next Steps
1. **Instrumentation**：提供 `shared_node_diagnostics`（或等效）特性开关，记录 `ensure_unique` / `clone_with_children` 调用次数，并设计与 C# 侧 `SharedNode` 调试计数器一致的输出格式。
2. **测试补强**：新增针对共享叶片、内部节点子数组替换的回归测试，校验 instrumentation 打开/关闭时结果一致，并验证 `Arc::ptr_eq` 等指针语义未回归。
3. **性能验证**：在 Rust `cargo bench` 与 C# `BenchmarkDotNet` 中补充最小基准，对照 instrumentation on/off 差异，确认 helper 抽象未引入额外分配或可观延迟。
4. **文档同步**：持续更新 `rope-port-mapping.md`、`AGENTS.md` 与 `docs/skeleton/xi.Core.decompiled.cs`，记录 instrumentation 字段、命名与迁移策略，确保双端实现保持对齐。
