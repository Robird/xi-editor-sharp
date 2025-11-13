# Iterator Façade Export

## Context
- Many rope utilities expose behavior exclusively through custom iterators (`Subset::zip`, `Breaks` builders, diff region iterators).
- C# translation struggles to mirror Rust's iterator trait and lifetime combinations while keeping semantics identical.
- Consumers often need collected results rather than streaming iterators, suggesting helper functions could remove iterator complexity.

## Problem Statement
- Iterator-heavy APIs complicate cross-language parity and increase maintenance burden.
- Re-implementing iterator logic in C# risks diverging from Rust behavior, especially when state machines become intricate.
- Tests focus on iterator outputs, not internal state transitions, making it difficult to validate translated implementations.

## Proposed Refactor
1. Identify iterator types used by external modules (`Subset::RangeIter`, `ZipIter`, `DiffBuilder` consumers, `Breaks` scanners).
2. Introduce façade functions returning `Vec`/`SmallVec` snapshots (`subset_ranges(matcher)`, `collect_zip_segments`, `diff_ops_to_regions`).
3. Refactor call sites that immediately collect iterator output to use the new façade functions.
4. Retain iterator implementations for streaming use cases but mark them `#[doc(hidden)]` or re-export through the façade to discourage new dependencies.

## Expected Benefits
- Simplifies C# porting by providing deterministic helper outputs without replicating iterator protocols.
- Improves testability by enabling straightforward golden-output comparisons.
- Allows future optimization by specializing façade functions without breaking external iterator consumers.

## Compatibility & Risks
- Some consumers may depend on laziness; the façade must not degrade performance for large datasets.
- Maintaining both iterator and façade paths may add duplication unless we refactor iterators to delegate to shared helpers.
- Need to verify that exposing new helper functions does not inflate the public API beyond intended scope.

## Validation Plan
- Add unit tests for façade functions ensuring they match iterator-collected outputs exactly.
- Benchmark representative workloads (e.g., diff generation) before and after refactor to confirm no significant regressions.
- Document façade functions in `rope-port-mapping.md` to guide C# implementation.

## Open Questions
- Should façades return owned containers or iterators over borrowed data with explicit lifetimes?
- Can we generalize façade generation via macros or traits to minimize boilerplate?
- Are there iterator consumers outside `xi-rope` (e.g., plugins) that require streaming semantics?

## Next Steps
1. Audit iterator usage across `xi-rope` and dependent crates to prioritize targets.
2. Prototype a façade for one iterator (e.g., `Subset::range_iter`) and migrate call sites.
3. Evaluate performance implications, adjust implementation (e.g., reserve capacity) as needed.
4. Expand façade coverage progressively, updating documentation and port mapping artifacts.

## Deep-Dive Findings (2025-11-13)

### Iterator Inventory
- `Cursor::iter::<M>()` (`xi-editor-ph7/rust/rope/src/tree.rs`)
	- Consumers: `xi-editor-ph7/rust/core-lib/src/linewrap.rs` (tests and helpers collect into `Vec<_>`); no production code keeps the iterator streaming.
	- Implication: providing a façade like `collect_break_offsets(cursor, metric)` can mirror current behavior without emulating the iterator state machine in C#.
- `Delta::iter_inserts` / `Delta::iter_deletions` (`xi-editor-ph7/rust/rope/src/delta.rs`)
	- Consumers: `xi-editor-ph7/rust/core-lib/src/find.rs` plus unit tests; both iterate immediately and never retain the iterator itself.
	- Implication: helper functions returning `SmallVec<[DeltaRegion; 4]>` (or `Vec<DeltaRegion>`) would cover existing usage while avoiding lifetime-heavy iterator ports.
- `Subset::range_iter` / `Subset::zip` / `Subset::mapper` (`xi-editor-ph7/rust/rope/src/multiset.rs`)
	- External usage is limited to other `multiset` helpers and `delta.rs`; no crates outside `xi-rope` rely on the iterator types directly.
	- Implication: façade helpers could live beside existing methods without expanding the public API surface, while C# can lean exclusively on the façade.
- `MergedBreaks` / `VisualLines` (`xi-editor-ph7/rust/core-lib/src/linewrap.rs`)
	- Iterator outputs feed directly into `collect()` or short-lived `map` pipelines that materialize full results.
	- Implication: exposing façade functions (e.g., `visual_lines_snapshot`) would decouple wrapped iteration logic from consumer loops.
- `Rope::iter_chunks` (`xi-editor-ph7/rust/rope/src/rope.rs`)
	- Mixed usage: some call sites (`core-lib/src/line_ending.rs`, `core-lib/src/file.rs`) depend on streaming to avoid allocating whole documents.
	- Implication: this iterator should remain available; any façade must preserve lazy semantics or provide chunked traversal without forcing full buffers.

### Feasibility Observations
- Most iterator surfaces already materialize into `Vec`/`SmallVec` in call sites, so façade helpers can match prevailing usage without regressions.
- Lifetimes on iterator structs (`CursorIter<'c, 'a, ...>`, `InsertsIter<'a, ...>`) are primary friction points for the C# port; façade helpers eliminate the need for equivalent borrow-checked state machines.
- Existing iterators derive data from owned `Vec` fields (e.g., `Delta.els`, `Subset.segments`), making it cheap to collect snapshots without extra traversals.
- Iterators that truly require laziness are rare (`iter_chunks` is the main outlier), so we can scope façade work narrowly to safe candidates.

### Candidate Façade Signatures
- `fn delta_insert_regions<N, L>(delta: &Delta<N, L>) -> SmallVec<[DeltaRegion; 4]>`
- `fn delta_delete_regions<N, L>(delta: &Delta<N, L>) -> SmallVec<[DeltaRegion; 4]>`
- `fn subset_ranges(subset: &Subset, matcher: CountMatcher) -> Vec<(usize, usize)>`
- `fn subset_zip_segments(a: &Subset, b: &Subset) -> Vec<ZipSegment>`
- `fn collect_metric_breaks<N, L, M>(cursor: &mut Cursor<N, L>, limit: Option<usize>) -> Vec<usize>`
- `fn visual_lines_snapshot(lines: &Lines, text: &Rope, start: usize, count: usize) -> Vec<VisualLine>`

### Compatibility & Risk Notes
- Allocate-once façade implementations should reserve capacity based on existing internal vectors to keep parity with iterator performance.
- Public API growth can be contained by marking low-level iterators `#[doc(hidden)]` while documenting façade-first usage in `rope-port-mapping.md`.
- For `iter_chunks`, prefer adding chunk-processing utilities (e.g., `fn visit_chunks<F>` accepting a closure) rather than collecting, to preserve constant-memory scans.
- Unit tests should assert façade outputs match `collect::<Vec<_>>()` over the original iterators to guarantee behavioral equivalence during the transition.

### Migration Outline
1. Prototype `delta_*_regions` façade, update `core-lib/src/find.rs` to rely on it, and gate the old iterator behind `#[doc(hidden)]`.
2. Mirror the pattern for `Subset` helpers, keeping iterator implementations private to `multiset.rs`.
3. Introduce a `Cursor` façade returning break offsets, migrate `linewrap` tests/helpers, and document the new entry point in the C# port mapping docs.
4. Evaluate whether `VisualLines` façade should precompute `Vec<VisualLine>` or expose a closure-based visitor to limit allocations when only partial results are needed.
