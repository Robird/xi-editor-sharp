# Breaks & Metrics Helper Templating

## Context
- Leaf-specialised metrics inside `xi-editor-ph7/rust/rope/src` (`BaseMetric`, `LinesMetric`, `Utf16CodeUnitsMetric`, `BreaksMetric`, `BreaksBaseMetric`) each hand-roll navigation and conversion logic.
- Implementations sit inside monolithic modules (`rope.rs`, `breaks.rs`), so shared routines (code-point scans, newline lookup, break index search) are duplicated with subtle drift.
- The C# port centralises string manipulation inside helpers (`Utf16BoundaryHelper`, `StringLeafOperations`); mirroring that structure in Rust will drastically simplify the mapping table in `docs/architecture/rope-port-mapping.md` and future generic-node work.

## Current Implementation Audit (2025-11-14)
- **BaseMetric** (`xi-editor-ph7/rust/rope/src/rope.rs`): `measure` returns raw byte length; `to_base_units`/`from_base_units` are guarded identities; `prev`/`next` walk UTF-8 boundaries by scanning byte prefixes; relies on `len_utf8_from_first_byte`.
- **LinesMetric** (`rope.rs`): `measure` surfaces cached `RopeInfo::lines`; `to_base_units` repeatedly calls `memchr` to locate `\n`; `from_base_units` uses `count_newlines`; `prev`/`next` mix manual loops with `memrchr`/`memchr`.
- **Utf16CodeUnitsMetric** (`rope.rs`): `measure` uses `RopeInfo::utf16_size`; `to_base_units` iterates `chars()` maintaining UTF-8/UTF-16 offsets; `from_base_units` defers to `count_utf16_code_units`; boundary scans duplicate `BaseMetric`.
- **BreaksMetric** (`xi-editor-ph7/rust/rope/src/breaks.rs`): uses `Vec<usize>` binary-search for navigation; edge handling for `offset == 0` and “past end” is open-coded in multiple branches.
- **BreaksBaseMetric** (`breaks.rs`): identity metric for base units; repeats boilerplate delegation to `BreaksMetric`.

## Duplication & Pain Points
- UTF-8 boundary scans are maintained twice (Base vs Utf16). Dedicated helpers (`prev_codepoint_boundary`, `next_codepoint_boundary`, `is_codepoint_boundary`) would remove drift and match the C# `Utf16BoundaryHelper` shape.
- Newline navigation (`to_base_units`, `prev`, `next`) repeats almost identical `memchr`/`memrchr` sequences with slightly different guards. Encapsulating “find next newline” logic once will reduce off-by-one risk.
- `BreaksMetric` mixes linear scan and binary-search patterns. A helper layer can standardise `find_prev_index`, `find_next_index`, and “overrun sentinel” handling, then expose them to both metric impls.
- Identity metrics (e.g., `BreaksBaseMetric`) reimplement every trait method just to delegate. A small generic wrapper should cover this pattern for any metric that needs a base-unit mirror.
- Free functions (`len_utf8_from_first_byte`, `count_newlines`, `count_utf16_code_units`) live in `rope.rs` and must remain callable from other crates (e.g., `xi-core-lib::editor`). Moving them blindly risks breaking callers or creating import cycles.

## Additional Constraints & Compatibility Notes
- `xi-editor-ph7/rust/rope/src` currently exposes only flat modules. Introducing helpers requires scaffolding a `metrics` submodule (e.g., `rope/src/metrics/mod.rs`) and wiring it through `lib.rs` with `pub(crate) mod metrics;`. Existing modules (`rope.rs`, `breaks.rs`) will then `use crate::rope::metrics::...` to avoid circular references.
- `count_newlines` is `pub` and consumed by `xi-core-lib::editor` and `xi-plugin-lib::state_cache`. After extraction we must keep a thin `pub fn count_newlines(...)` shim in `rope.rs` that forwards to the helper so downstream crates remain unchanged.
- Helper APIs must stay `pub(crate)` until we stabilise the Rust surface. The C# port will mirror the naming, but we should avoid accidentally committing to a public Rust API during the refactor.
- Any new helper module must remain allocation-free and mark hot paths with `#[inline]` to preserve release builds. We will gather simple before/after timings even though Criterion benches are presently removed.
- When introducing generics (identity wrapper) mind the orphan rules: the struct must live in our crate so we can implement `Metric` for it. Using `PhantomData` keeps the type zero-sized.

## Helper Module Design

### Layout
- Create `xi-editor-ph7/rust/rope/src/metrics/mod.rs` that re-exports submodules: `codepoint.rs`, `lines.rs`, `break_indices.rs`, `identity.rs`.
- `metrics/mod.rs` will expose `pub(crate) use` items such as `codepoint::{next_codepoint_boundary, ...}` to keep call sites terse (`use crate::rope::metrics::codepoint::next_codepoint_boundary;`).
- Keep module-private constants (e.g., `UTF8_CONT_MASK`) inside helpers so `rope.rs` and `breaks.rs` no longer own them.

### Codepoint helpers
- Provide `is_codepoint_boundary(bytes: &[u8], offset)`, `prev_codepoint_boundary(bytes, offset)`, `next_codepoint_boundary(bytes, offset)`.
- Accept raw byte slices to avoid repeated `as_bytes()` calls; callers can pass `leaf.as_bytes()` where needed.
- Internally reuse `len_utf8_from_first_byte` (moved into this module). Keep `#[inline(always)]` on the branchless length helper.

### Lines helpers
- Surface `count_newlines(bytes: &[u8])`, `find_next_newline(bytes, offset)`, `find_prev_newline(bytes, offset)`.
- Implement using `memchr`/`memrchr` so behaviour matches the current implementation, including returning `None` when no newline exists.
- After extraction, `LinesMetric::to_base_units/prev/next` become thin wrappers around these helpers, reducing bespoke loops.
- Expose a slice-based version of `count_newlines`, then keep the existing `pub fn count_newlines(s: &str)` in `rope.rs` calling it for downstream consumers.

### Break index helpers
- Encapsulate binary-search patterns into `find_prev_break(data: &[usize], offset)`, `find_next_break(data: &[usize], offset)`, and `is_break_boundary(data, offset)`.
- Provide `nth_break_offset(data, measured_units)` that reproduces the current guard behaviour (`len + 1` sentinel when measured units exceed the known breaks).
- These helpers will be shared by both `BreaksMetric` and the identity wrapper. Unit tests should cover empty data, duplicates, and exact-boundary cases.

### Identity metric template
- Define `pub(crate) struct BaseUnitsIdentity<M>(PhantomData<M>);` in `identity.rs`.
- Implement `Metric<Info, Leaf>` for `BaseUnitsIdentity<M>` where `M: Metric<Info, Leaf>`:
  - `measure` returns `len`.
  - `to_base_units`/`from_base_units` are pass-through identities with bounds checks left to callers.
  - Boundary methods delegate to `M`.
  - `can_fragment` delegates to `M` to preserve fragmentation capabilities.
- Alias `pub(crate) type BreaksBaseMetric = BaseUnitsIdentity<BreaksMetric>;` to keep existing call sites compiling with minimal churn.
- Document how future metrics can opt into the same wrapper without bespoke boilerplate.

## Execution Plan
1. **Module scaffolding**
	- Add `metrics/mod.rs` plus the four helper files in the Rust rope crate.
	- Move `len_utf8_from_first_byte` and the existing newline/binary-search logic into their respective helpers. Retain `pub fn count_newlines` in `rope.rs` as a forwarder to avoid breaking `xi-core-lib` and `xi-plugin-lib`.
	- Update `lib.rs` to register the new module (`pub(crate) mod metrics;`) and fix imports in `rope.rs`/`breaks.rs`.
2. **Codepoint consolidation**
	- Refactor `BaseMetric` and `Utf16CodeUnitsMetric` to call `metrics::codepoint::*` functions for boundary checks and navigation.
	- Add focused unit tests under `metrics/codepoint.rs` covering ASCII, multi-byte UTF-8, and boundary-at-start/end scenarios.
3. **Lines helper extraction**
	- Switch `LinesMetric` to use `metrics::lines::*`. Ensure `to_base_units`, `from_base_units`, `prev`, and `next` share the helper logic.
	- Extend `xi-editor-ph7/rust/rope/src/test_helpers.rs` (or add a new test module) with multi-leaf newline cases to verify no behavioural regressions.
4. **Breaks helper & identity wrapper**
	- Implement `metrics::break_indices::*` and rewrite `BreaksMetric` plus the new `BaseUnitsIdentity` alias.
	- Replace the bespoke `BreaksBaseMetric` struct with a `type` alias and adjust `DefaultMetricProvider` usage accordingly.
	- Add unit tests for empty break sets, consecutive breaks, and offsets outside the recorded range.
5. **Documentation & parity updates**
	- Update `docs/architecture/rope-port-mapping.md` with helper names so the C# side can mirror them.
	- Run `python scripts/refresh_skeleton_docs.py` to refresh the skeleton snapshots.
	- Reflect the new helper structure in `docs/skeleton/rope.md` and the C# mapping docs as needed.

## Validation Plan
- Run `cargo check -p xi-rope` and `cargo test -p xi-rope` after each stage; finish with `cargo test --workspace` to guard cross-crate usages (`xi-core-lib`, `xi-plugin-lib`).
- Execute `.\scripts\refresh_skeleton_docs.py` to ensure documentation snapshots stay in sync (tracked as part of Stage 5).
- Run `dotnet test tests/xi.Core.Tests` to confirm the C# metrics remain green once naming parity updates land.
- Capture before/after timings for simple cursor-navigation micro-benchmarks (e.g., `rope::rope::tests::bench_style_next_prev` or, temporarily, a debug-only loop over `BaseMetric::next`) since Criterion benches are disabled. Record findings in a scratchpad until the official benchmarking suite returns.
- Verify helper unit tests intentionally cover sentinel cases (`offset == leaf.len()`, empty break lists) before removing duplicated logic.

## Outstanding Questions
- Do we want helper names that exactly match the C# `Utf16BoundaryHelper` and friends (`GetNextBoundary` vs `next_codepoint_boundary`)? If we prefer idiomatic Rust snake_case, document the mapping explicitly in `rope-port-mapping.md` to prevent drift.
- Should we expose the helper module to other crates (e.g., `xi-core-lib`) once stabilised, or keep them internal and rely on `count_newlines`-style shims? This affects long-term API surface commitments.
- What lightweight micro-benchmark should we use while Criterion benches are parked? Options include a bespoke `cargo run --example` harness or instrumented unit tests; decision pending once we see the first perf measurements.
