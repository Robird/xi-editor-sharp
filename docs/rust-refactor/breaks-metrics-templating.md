# Breaks & Metrics Helper Templating

## Context
- Leaf-specialized metrics inside `xi-editor-ph7/rust/rope/src` (`BaseMetric`, `LinesMetric`, `Utf16CodeUnitsMetric`, `BreaksMetric`, `BreaksBaseMetric`) each hand-roll navigation and conversion logic.
- Implementations live alongside unrelated rope code, so shared routines (code-point scans, newline lookup, break index search) are duplicated with subtle naming differences.
- The C# port concentrates string-specific manipulation in static helpers (`StringLeafOperations`) and plans to mirror metric helpers; aligning the Rust side will make the mapping table in `docs/architecture/rope-port-mapping.md` far less mechanical.

## Current Implementation Audit (2025-11-14)
- **BaseMetric** (`xi-editor-ph7/rust/rope/src/rope.rs`): `measure` returns raw byte length; `to_base_units`/`from_base_units` are identity operations guarded by `String::is_char_boundary`; `prev` and `next` manually walk UTF-8 boundaries (duplicated in `Utf16CodeUnitsMetric`); `can_fragment` is `false`; depends on `len_utf8_from_first_byte`.
- **LinesMetric** (`xi-editor-ph7/rust/rope/src/rope.rs`): `measure` surfaces cached `RopeInfo::lines`; `to_base_units` advances through newline boundaries with repeated `memchr`; `from_base_units` uses `count_newlines`; `prev`/`next` use paired `memrchr`/`memchr`; `can_fragment` is `true` to allow multi-leaf lines.
- **Utf16CodeUnitsMetric** (`xi-editor-ph7/rust/rope/src/rope.rs`): `measure` uses `RopeInfo::utf16_size`; `to_base_units` iterates code points accumulating UTF-16 and UTF-8 widths; `from_base_units` defers to `count_utf16_code_units`; `prev`/`next` repeat the UTF-8 boundary scans found in `BaseMetric`; `can_fragment` is `false`.
- **BreaksMetric** (`xi-editor-ph7/rust/rope/src/breaks.rs`): `measure` counts break markers held in `BreaksLeaf::data`; `to_base_units` maps the Nth break to its offset with guard clauses; `from_base_units`, `is_boundary`, `prev`, and `next` all rely on the sorted `Vec<usize>` plus `binary_search`; `can_fragment` is `true` because break runs can straddle leaves.
- **BreaksBaseMetric** (`xi-editor-ph7/rust/rope/src/breaks.rs`): identity metric over base units; `measure` is length; remaining trait methods forward directly to `BreaksMetric`, resulting in boilerplate that repeats in every “pass-through” metric we might add.

## Duplication & Pain Points
- UTF-8 boundary scans for `prev`/`next` are maintained twice (Base vs. Utf16). A helper like `codepoint::prev_boundary`/`next_boundary` would remove drift and enable inline reuse from C#.
- Both newline operations (`to_base_units` loops and `prev`/`next`) replicate `memchr` calls with slightly different guard clauses; a helper that advances to the next/previous match would consolidate behavior.
- Identity metrics (e.g., `BreaksBaseMetric`) reimplement all trait functions only to delegate. A thin wrapper or macro-free newtype helper could provide the delegation once.
- Conversion helpers (`convert_metrics::<BaseMetric, M>` vs. `convert_metrics::<BreaksBaseMetric, M>`) expose naming mismatches that complicate `rope-port-mapping.md`.

## Feasibility Assessment
- The trait signatures already accept helper delegation because every method is static; moving shared logic into `metrics::helpers` (or another dedicated module under `rope/src/metrics/`) only requires importing functions.
- Existing free functions (`len_utf8_from_first_byte`, `count_newlines`, `count_utf16_code_units`) can be wrapped into cohesive helper structs to mirror the static-abstract pattern we are adopting in C#.
- `DefaultMetricProvider` already centralises conversions between “default” and derived metrics; templated helpers can plug into this without API breaks.
- A helper layer will ease future generic-node work: once `Node<TInfo, TLeaf, TLeafOps>` lands on Rust, metric helpers can be parameterised on the leaf type without touching every metric impl.

### Candidate Helper Surface
- `helpers::codepoint` – exposes `prev_boundary`, `next_boundary`, and `is_boundary` for UTF-8 navigation.
- `helpers::lines` – encapsulates repeated `memchr`/`memrchr` usage and newline counting with names aligned to `StringLeafOperations` (`LocateNextBreak`, `CountBreaks`).
- `helpers::breaks` – formalises binary-search wrappers (`find_prev_index`, `find_next_index`, `is_index_boundary`) and identity conversions so `BreaksBaseMetric` becomes an instantiation rather than handwritten delegation.
- `helpers::identity_metric` – generic wrapper that adapts any `Metric` as its base-unit equivalent, eliminating boilerplate for future metrics requiring identity conversions.

## Compatibility & Risks
- Moving logic into helpers could impact inlining. Mark the new helper functions with `#[inline]`/`#[inline(always)]` where appropriate and collect perf samples (line navigation, cursor movement) to ensure no regressions.
- Visibility tightening: helpers should live in `crate::rope::metrics` and remain `pub(crate)` to avoid committing to public APIs before we stabilise the surface.
- Binary-search and newline helpers must preserve edge-case behaviour (empty leaves, offsets at leaf boundaries). Documented regression tests are mandatory before cutting over existing metrics.
- The helper names must stay aligned with the C# static helpers; otherwise `rope-port-mapping.md` will drift. Establish a checklist entry in that doc for future helper additions.

## Validation Plan
- Re-run `cargo test -p xi-rope` plus focused `cargo test -p xi-core-lib` suites touching metrics.
- Extend `xi-editor-ph7/rust/rope/src/breaks.rs` tests to cover helper edge cases (empty `BreaksLeaf`, offsets at start/end, inter-leaf fragments).
- Introduce unit tests in the new helper module before altering metric impls to guard behaviour during refactor.
- Capture baseline timings for cursor navigation micro-benchmarks (line iteration, UTF-8 stepping) before and after helper extraction to quantify performance impact once Criterion benches return.

## Open Questions
- **Placement**: Should helpers live under `rope/src/metrics/helpers.rs` or in a shared crate for reuse in `xi-core-lib`? Current leaning: keep them within `xi-rope` until other crates need them.
- **Configuration surface**: Do helpers require awareness of leaf min/max sizes? Early investigation suggests no—metrics only operate on offsets, so we can keep capacity logic inside leaf helpers like `BreaksLeaf::push_maybe_split`.
- **Parity enforcement**: Document helper names in `docs/architecture/rope-port-mapping.md` and add a lightweight script (or review checklist item) to verify Rust/C# helper lists stay in sync.

## Proposed Execution Plan
1. Scaffold `rope/src/metrics/helpers.rs` with UTF-8 boundary helpers reused by both `BaseMetric` and `Utf16CodeUnitsMetric`; update those impls to call the helpers.
2. Introduce newline navigation helpers and migrate `LinesMetric` (covering `to_base_units`, `prev`, and `next`) while expanding regression tests across multi-leaf scenarios.
3. Create a binary-search helper suite for `BreaksMetric`, then replace `BreaksBaseMetric` with an identity wrapper built atop the helper.
4. Update `rope-port-mapping.md` with the new helper vocabulary and ensure the forthcoming C# metric helpers adopt matching names.
5. Re-run `cargo test -p xi-rope` and .NET rope metric tests; if perf benches reopen, compare before/after numbers.
