# TreeBuilder Slice Stack

## Current Snapshot
- **Current Rust design**: `TreeBuilder::push`, `push_slice`, `push_leaf_slice`, and `pop` in `xi-editor-ph7/rust/rope/src/tree.rs` maintain a `Vec<Vec<Node>>` stack sorted by descending height; balancing relies on `Node::concat`, `Node::merge_nodes`, and selective leaf edits before `build()` drains the stack.
- **Portability blocker**: Reproducing the stack discipline requires translating `Interval::translate`, `translate_neg`, and `intersect`, and knowing when nodes are reused versus cloned; small mistakes break balancing or leak nodes.
- **Potential Rust-side action**: Add a `TreeBuilderTracer` behind `#[cfg(feature = "tree_builder_slice_trace")]` to emit push/pop and interval events without mutating release logic.
- **Resulting C# implication**: Slice-plan fixtures recorded from Rust allow the C# implementation to compare stack operations event by event before removing the diagnostics.

## Investigation Highlights
- `TreeBuilder::push` compares heights in a loop and takes three branches (stack taller, equal, shorter); equal-height frames prefer `is_ok_child` reuse, otherwise they merge leaves or split internal nodes, with overflow handled by `self.pop()` (roughly `tree.rs:L520-L610`).
- `TreeBuilder::push_slice` handles empty intervals and whole-node reuse first, then accumulates `offset` inside internal nodes and computes `rec_iv = iv.intersect(child_iv.translate(offset)).translate_neg(offset)` before recursing when non-empty (roughly `tree.rs:L612-L680`).
- `TreeBuilder::pop` always returns the top frame: single-node frames are reused, multi-node frames rebuild a parent via `Node::from_nodes` (roughly `tree.rs:L700-L730`).
- Interval arithmetic depends on `Interval::translate`, `translate_neg`, and `intersect` (`interval.rs:L56-L130`), which form the minimal metadata a trace must capture.
- C# `src/xi.Core/Rope/Tree/TreeBuilder.cs` currently keeps a linear `_pending` list and repeatedly calls `Node.Concat`, so it lacks the Rust stack semantics and reuse logic.

## Value Assessment
- Rust `Node::subseq` and `Node::edit` rebuild ropes via `TreeBuilder::push_slice` (`tree.rs:L570-L606`); if C# diverges, every slice/edit path deviates from the golden behavior.
- The present C# TreeBuilder cannot express whole-node reuse or leaf-only cloning; without Rust traces, unit tests alone cannot cover every stack permutation, so the port has high risk.
- Stage D seeks shared golden assets beyond serialization; slice-plan traces provide deterministic fixtures for rope and delta regression tests.
- Trace events also show when `Node::concat` or `Node::merge_nodes` trigger, guiding C# tuning of copy-on-write frequency and the `is_ok_child` invariant.

## Rationality Assessment
- The `cursor_state` feature already demonstrates a gated diagnostic inside `tree.rs` using `#[cfg(feature = "cursor_state")]` (around `tree.rs:L800-L980`), showing the build tolerates optional instrumentation; a slice tracer can reuse the pattern.
- Event types such as `PushFrame`, `ExtendFrame`, `MergePop`, `LeafSlice`, and `EnterChild` align with existing branch structure, so no invasive changes to `Node` or `Leaf` are required.
- Required metadata (stack depth, node height/len, source and translated intervals, reuse flags) matches the prior `collect_slice_plan` idea recorded in `docs/architecture/rope-port-mapping.md`.
- With the feature disabled, the compiler elides all tracer calls; debug/test builds can inject either trait objects or lightweight generics to collect events.

## Feasibility Assessment
- All stack mutations occur in `push`, `push_slice`, `push_leaf_slice`, and `pop`; emitting tracer hooks before and after those spots keeps the change set small.
- Node identity can be derived from `Arc<NodeBody<N, L>>` (`Arc::as_ptr`) or hashed handles; the exported trace can serialize the value as `u64` so C# only uses it for comparisons.
- Event volume scales with recursion depth; for typical edits it stays well below rope length. Large traces can be produced selectively by extending `scripts/refresh_serialization_fixtures.ps1` with a `--CollectSliceTrace` switch.
- Tests can follow the existing pattern: add `tree::tests::tree_builder_slice_trace_*` behind the feature and run them via `cargo test -p xi-rope`; C# can ingest the JSON fixtures in `xi.Core.Tests`.
- Documentation plumbing (`docs/architecture/rope-port-mapping.md`, `AGENTS.md`) already tracks the tracer concept, so keeping status in sync is straightforward.

## Risks and Mitigations
- **Node identity drift**: `Arc` pointers remain stable after clones. If future generic `SharedNode` work changes the representation, define a shared `NodeKey` format and keep it internal to tests.
- **Trace volume**: Restrict the feature to tests or scripted runs. The exporter can apply caps or chunking when writing fixtures.
- **Future TreeBuilder refactors**: When generic nodes land, update the event schema and note the dependency in `docs/rust-refactor/node-generic-refactor-plan.md`.
- **Cross-language lag**: If Rust emits traces before C# consumes them, store the JSON under Stage D fixtures so information is not lost, then add the C# verifier later.

## Recommended Next Steps
- Rust: implement the `TreeBuilderTracer` trait and the `tree_builder_slice_trace` feature, cover `push`/`push_slice`/`push_leaf_slice`/`pop`, and add unit tests that assert event sequences.
- Rust: teach the `export-serde-fixtures` CLI to emit slice plans into `tests/xi.Core.Tests/Fixtures/tree_builder_slice/*.json` when requested.
- C#: extend `TreeBuilder` to mirror the Rust stack semantics and add tests that replay recorded traces.
- Docs: update `docs/architecture/rope-port-mapping.md` and `docs/csharp-refactor/rope-cow-rebalance-plan.md`, and log progress in `AGENTS.md` as the tracer lands.
- Tooling: add a switch to `scripts/refresh_serialization_fixtures.ps1` so serde fixtures and slice plans can be refreshed together.

## Evidence
- `xi-editor-ph7/rust/rope/src/tree.rs`: source of `TreeBuilder::push`/`push_slice`/`pop` and the existing `cursor_state` feature gate pattern.
- `xi-editor-ph7/rust/rope/src/interval.rs`: interval arithmetic primitives that define the required trace payload.
- `src/xi.Core/Rope/Tree/TreeBuilder.cs`: current C# implementation that lacks the stack discipline.
- `docs/architecture/rope-port-mapping.md`: records the planned `collect_slice_plan` guard this tracer would satisfy.
- `scripts/refresh_serialization_fixtures.ps1`: Stage D export script that can be extended to collect slice plans.
