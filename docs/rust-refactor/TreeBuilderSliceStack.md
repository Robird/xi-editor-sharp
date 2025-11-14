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
