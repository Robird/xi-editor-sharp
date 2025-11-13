# Cursor Lifetime Simplification

## Context
- `Cursor<'a, N, L>` currently holds borrowed references into the rope structure, relying on lifetimes to guarantee safety.
- C# cannot express the same borrowing semantics, forcing ad-hoc solutions that compromise performance or correctness.
- Lifetimes hinder exposing cursor-based helpers (`RopeScanner`, `find`, diff iterators) without duplicating logic.

## Problem Statement
- Borrow-based cursors prevent us from sharing iterator logic verbatim with C#.
- Attempting to mirror lifetimes with reference counting or pinned pointers leads to complex, error-prone code.
- Many APIs expect cursors to outlive temporary edits, an assumption that does not translate across language boundaries.

## Proposed Refactor
1. Prototype an index-oriented cursor state (`CursorState`) capturing node path, leaf index, and offsets without lifetimes.
2. Provide adapter constructors from `Cursor<'a, N, L>` to `CursorState` to maintain backwards compatibility.
3. Implement new traversal helpers (`next_leaf`, `prev_leaf`, `seek_forward`) operating on `CursorState`.
4. Gradually migrate high-level utilities (`RopeScanner`, `find`, `compare_cursor_*`) to the borrow-free cursor, leaving legacy APIs intact behind feature flags.

### CursorState sketch (from tree.rs audit)
- Store `root: Node<N, L>` (cheap `Arc` clone) alongside `position` and `offset_of_leaf` to preserve the current invariants without borrowing.
- Replace the fixed cache `[Option<(&Node, usize)>; 4]` with `SmallVec<[PathElem<N, L>; 4]>` where each element keeps a cloned child handle and index.
- Cache the current leaf as `Option<Node<N, L>>` and expose borrowed views through helper wrappers (e.g., `LeafRef<'_>`) to avoid cloning leaf payloads per traversal step.
- Encode invariants (`position <= root.len()`, child indices < arity, cache depth matches node height) with debug assertions so adapters surface divergence early.

## Cursor Implementation Audit (xi-editor-ph7/rust/rope/src/tree.rs)
- Cursor caches at most the last four ancestors; tall trees re-traverse from the root when the cache rolls over, so the new state must retain this bounded cache to keep hot paths allocation-free.
- Core helpers (`descend`, `next_leaf`, `prev_leaf`, metric descent) only require node lengths and child indices, enabling a drop-in translation that uses owned `Node` handles.
- Public consumers (`compare::RopeScanner`, `find::*`, `spans::SpanBuilder`) hold cursors for read-only iteration; none exploit the borrow lifetime beyond guaranteeing immutability.
- Copy-on-write edits rebuild nodes behind an `Arc`, and `Cursor<'a>` never exposes mutable access, implying cloned `Node` handles inside `CursorState` will not violate aliasing expectations.

## Feasibility Assessment
- Owning `Node` handles incurs only `Arc` bump costs; cache updates stay O(1) so long as we keep the path structure stack-allocated via `SmallVec`.
- Legacy APIs can continue returning `Cursor<'a, ...>` by borrowing from `CursorState` on the fly (`Cursor::from_state<'a>(&'a Node, &'a CursorState)`), allowing gradual migration of downstream crates.
- Metric conversions (`measure_leaf`, `descend_metric`) already scan from the root; translating them to `CursorState` simply swaps borrowed references for owned handles.
- The existing rope tests (diff/find/cursor iteration) combined with new property tests can validate behavioral parity before flipping the default implementation flag.

## Expected Benefits
- Enables direct porting to C# by copying the index-based state machine and helper functions.
- Reduces unsafe code and lifetime gymnastics in Rust, easing maintenance.
- Clarifies invariants by expressing cursor behavior in explicit data structures rather than implicit borrow scopes.

## Compatibility & Risks
- The new cursor representation may introduce performance overhead if not carefully optimized (extra allocations or cloning of node paths).
- Existing crates may rely on implicit borrow semantics (e.g., ensuring references remain valid during mutations); we must document and enforce replacement invariants.
- Dual cursor implementations risk divergence; must ensure consistent behavior via shared tests.

## Validation Plan
- Add property tests comparing results of legacy and new cursor traversals across randomized edits.
- Benchmark heavy cursor consumers (`find`, diff) to quantify any regression.
- Provide feature flag to switch default cursor implementation for broader testing before removing lifetimes.
- Add stress tests covering tall trees (depth > cache) and mixed metric traversals to ensure the owned-path cache mirrors legacy semantics.

## Open Questions
- What is the minimal state required to resume traversal after edits—do we need to store revision counters or rope version IDs?
- Can we leverage `SmallVec` or arena allocation to keep cursor path updates cheap?
- Should we merge cursor redesign with planned iterator façade export to avoid double work?
- How do we expose borrowed leaf slices to consumers without forcing repeat cloning (e.g., temporary `LeafRef<'a>` wrappers sourced from cached nodes)?
- Do we need a `CursorState::repair_after_edit` hook so adapters can revalidate cached path segments after localized copy-on-write edits?

## Next Steps
1. Spec `CursorState` structure and identify required invariants.
2. Implement prototype alongside adapter methods, gated behind a `cursor_state` feature.
3. Port `RopeScanner` to use the new state while retaining backward-compatible constructors.
4. Evaluate results, collect metrics, and plan broad rollout if successful.
