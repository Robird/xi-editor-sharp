# Delta & Subset Serialization Cleanup

## Context
- `delta.rs` and `multiset.rs` interleave core editing logic with optional serde serialization.
- Structures like `Delta`, `InsertDelta`, `Subset`, and `Revision` include serde-only derives and fields.
- C# porting requires deterministic data structures independent of serde, but current code tightly couples serialization with logic.

## Problem Statement
- Mixing serde derives with core logic complicates cross-language parity and increases compilation dependencies.
- Tests often rely on serialized representations to validate behavior, making it harder to reproduce in C#.
- Serialization-specific fields and helper methods introduce conditional compilation branches that are difficult to mirror.

## Proposed Refactor
1. Split serialization functionality into dedicated modules (e.g., `delta::serde_impls`, `multiset::serde`), keeping core types free from serde derives.
2. Provide canonical helper functions (`Delta::to_ops()`, `Subset::to_segments()`) that output serialization-friendly data structures without requiring serde.
3. Update serde modules to use the helpers, ensuring serialization logic is additive rather than intertwined.
4. Document serialization formats (JSON structure, invariants) to align Rust and C# serde implementations or alternatives.

## Expected Benefits
- Clean separation of core logic from optional serialization features, simplifying C# translation.
- Reduced compile-time dependencies when serde is disabled, improving workspace slim-down efforts.
- Clearer debugging by exposing helper methods that can produce portable representations for golden tests.

## Compatibility & Risks
- Refactor may require changes to existing serde configuration (`cfg(feature = "serde")`), potentially affecting downstream crates.
- Need to ensure derived implementations (e.g., `Serialize`, `Deserialize`) remain consistent with previous behavior.
- Additional helper functions could expand public API surface; must review stability implications.

## Validation Plan
- Run `cargo test -p xi-rope` with serde enabled and disabled to ensure identical behavior.
- Add integration tests round-tripping `Delta`/`Subset` through serde to confirm compatibility.
- Provide documentation snippets demonstrating new helper usage for debugging and testing.

## Open Questions
- Should we define explicit schema versions for serialized deltas to guard against future changes?
- Do we need to support alternative serialization backends (e.g., bincode) under the new structure?
- Are there downstream consumers relying on private fields currently exposed via serde derives?

## Next Steps
1. Identify serde-only code sections and draft new modules housing their logic.
2. Implement helper functions returning portable data (vectors of segments, ops) and migrate serde code to use them.
3. Update build configuration and documentation, ensuring feature flags continue to work as expected.
4. Engage with downstream users to confirm the refactor does not break their serialization workflows.

**Key Observations**  
- `Subset`/`Segment` in multiset.rs are entirely guarded by `#[cfg(feature = "serde")]` (see `L27-L56`), so the core data structure disappears if the feature is disabled. This confirms the proposal’s claim that serialization code is interwoven with core logic and currently blocks a “no-serde” build, which we will need for lightweight C# parity.  
- `Engine`, `RevId`, `Revision`, and `Contents` in engine.rs follow the same pattern (`L44-L115`), and Fuchsia sync persists `Engine` via `serde_json` (`xsync.rs L27-L47`). Any refactor must preserve this JSON shape to avoid breaking ledger sync.  
- `Delta` already offloads most JSON work to serde_impls.rs, but those impls are hard-coded to `Delta<RopeInfo, String>`, which complicates reuse once we move toward generic leaves. Tests under delta.rs (`L892-L907`) and rope.rs (`L1200-L1206`) assert the exact serialized form, so byte-for-byte compatibility is a must.  
- There are no helper APIs today that expose `Subset` segments or `Delta` ops in a serialization-friendly form; serde impls reach directly into private state. That matches the proposal’s call for canonical helpers before splitting modules.  
- Workspace-wide `cargo test --workspace` currently succeeds only because every dependent crate enables the `serde` feature on `xi-rope` (Cargo.toml links `features = ["serde"]`). The refactor would let us drop that dependency when we just need the data structures.

**Feasibility Notes**  
- Moving derives into `cfg_attr` (or into dedicated `serde` modules) is straightforward for `Subset`/`Segment` once we add minimal accessors (e.g., `pub(crate) fn raw_segments(&self) -> impl Iterator<Item = (usize, usize)>`). Expect touch-points in `SubsetBuilder` and iterators; invariants stay intact.  
- `Engine` will need custom `Serialize`/`Deserialize` impls to keep the existing layout (`skip_serializing` + `default`). Extracting a mirror struct (e.g., `SerializableEngine`) inside `engine::serde`) keeps core code free of serde attributes while preserving JSON compatibility.  
- `Delta`’s existing serde_impls.rs can be reshaped to consume new helpers so only a thin layer remains serde-aware. We should add golden tests (ideally `serde_test::assert_ser_tokens`) before and after to prove no format drift.  
- Introducing helper methods (`Delta::to_ops`, `Subset::to_segments`, maybe `Engine::to_revision_log`) creates the “portable view” the doc requests and will immediately benefit C# translation. These can stay `pub(crate)` until the API design settles.  
- Once derives move out, building `xi-rope` without `serde` becomes viable; we’ll need CI coverage for `--no-default-features`. Expect follow-up tweaks in crates that currently assume serde is always on.

**Risks / Open Questions**  
- `Engine` deserialization is used only on Fuchsia; we need sample ledgers or integration tests to ensure we don’t regress that path.  
- `Delta` serialization currently requires `Node<RopeInfo, String>`; making it generic might expand the public API surface. We should decide if we keep the specialization or expose trait bounds for arbitrary leaves.  
- Exposing raw `Subset` segments may leak invariants; we should document that callers must not mutate or reorder segments.  
- Any format change breaks stored documents and plugin expectations. Before refactoring we should capture a corpus of serialized deltas/subsets/engines for regression comparison.

**Suggested Next Steps**  
1. Prototype `Subset`/`Segment` un-gating: add helpers, move serde derives into a new `multiset::serde` module, and run `cargo test -p xi-rope` with and without `--features serde`.  
2. Mirror the approach in engine.rs, introducing a serde shim that round-trips an `Engine` pulled from `core-lib`’s ledger tests; extend tests to cover that JSON path.
