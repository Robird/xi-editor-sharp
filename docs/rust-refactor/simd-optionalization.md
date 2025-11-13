# SIMD Optionalization Strategy

## Context
- `compare.rs` uses architecture-specific intrinsics (`#[target_feature(enable = "sse4.2")]`, `avx2`) to accelerate byte comparisons.
- These intrinsics are encapsulated in `unsafe` functions and conditionally compiled, making them hard to translate to C# without dedicated intrinsics support.
- Many deployments may run on platforms where SIMD is unavailable or offers marginal benefit.

## Problem Statement
- Hardwired SIMD paths hinder portability and complicate cross-language parity.
- The fallback logic is intertwined with SIMD entry points, making it difficult to disable intrinsics cleanly.
- Maintaining unsafe, architecture-specific code increases maintenance cost while offering limited value for the C# port.

## Proposed Refactor
1. Define a trait (e.g., `ByteComparator`) with methods like `ne_idx`, `ne_idx_rev`, allowing multiple implementations (SIMD vs. scalar).
2. Provide a default `ScalarComparator` that implements the trait using the existing fallback logic.
3. Implement `SimdComparator` behind feature gates (`simd_sse`, `simd_avx`), delegating to the existing intrinsic functions.
4. Update public entry points (`ne_idx`, `ne_idx_rev`, `RopeScanner`) to select comparator implementation at runtime or compile time based on enabled features.
5. Consider removing direct `unsafe` exports from the public API, encapsulating them within the trait implementation.

## Expected Benefits
- Simplifies C# parity by allowing us to implement only the scalar comparator without breaking functionality.
- Reduces unsafe exposure and clarifies where architecture-specific optimizations live.
- Enables targeted benchmarking to assess whether SIMD paths justify maintenance overhead.

## Compatibility & Risks
- Runtime dispatch or feature gating may introduce minor overhead; micro-benchmarks will validate impact.
- Need to ensure feature defaults maintain current performance expectations for Rust users reliant on SIMD.
- Build configurations relying on `is_x86_feature_detected!` may need adjustment to honor new feature flags.

## Validation Plan
- Create unit tests that run with and without SIMD features to confirm identical results.
- Benchmark key workloads (rope equality comparisons, diff scanning) under both comparators.
- Document feature flags and configuration changes in `README` or `rust-workspace-slimming.md`.

## Open Questions
- Should selection be compile-time (feature) or runtime (CPUID detection with trait objects)?
- Do we maintain separate crates/modules for SIMD implementations to keep core clean?
- How do we expose comparator choice to consumers needing deterministic behavior?

## Next Steps
1. Design the `ByteComparator` trait and migrate scalar fallback into `ScalarComparator`.
2. Wrap existing SIMD functions into a `SimdComparator` implementation gated behind features.
3. Update `compare.rs` entry points to use the trait, add configuration plumbing.
4. Run performance tests to verify minimal regressions and capture results for documentation.

**SIMD Plan Review**
- Current state in compare.rs: SIMD helpers already sit behind `#[cfg(target_arch = "x86_64")]`, and the public entry-points (`ne_idx`, `ne_idx_rev`) fall back to `ne_idx_fallback` / `ne_idx_rev_fallback` whenever the CPU check fails or the build target is not x86_64. On other architectures the SIMD code is simply not compiled, so portability to non-x86 (and by extension the scalar-only C# port) is already satisfied without extra plumbing.
- The proposal’s assumption that fallback logic is “intertwined” isn’t borne out by the code; the scalar path is self-contained and easy to keep as the default. What’s missing for parity is just a clean kill-switch on x86_64 builds so we can opt out of intrinsics when validating scalar behaviour. That can be achieved with a lightweight feature flag (`cfg(feature = "simd")` around the runtime checks) or an env-controlled guard, avoiding the trait indirection entirely.
- Introducing a `ByteComparator` trait and runtime dispatch would touch every caller, force `RopeScanner` to store or genericize a comparator, and add overhead unless carefully optimized. It also complicates the public API: today consumers call straight functions, whereas the trait would either leak into the API surface or require hidden statics with `lazy_static`/`OnceLock`. The maintenance cost outweighs the benefit when the only goal is being able to disable SIMD.
- The “unsafe exposure” concern can be addressed more simply by making the intrinsic helpers `pub(crate)` (they are only used in the same module’s tests) and gating their compilation with the same feature. No trait refactor is needed for that.
- Bench coverage and docs already need updating if we add a feature flag, but that work is orthogonal; with a compile-time `simd` flag we can run benches twice (with/without) and document the flag in rust-workspace-slimming.md.
- Recommended path: 1) add a `simd` cargo feature (default-on) guarding the `is_x86_feature_detected!` branches and the intrinsic definitions; 2) mark SIMD helpers `pub(crate)`; 3) add a regression test pass that builds with `--no-default-features` to ensure the scalar path stays sound; 4) document the flag plus the C# parity rationale in simd-optionalization.md. Keep the more invasive trait-based rewrite on hold unless profiling shows the feature-gated approach is insufficient.

Next steps I’d suggest: 1) prototype the feature-gated switch (very small patch touching compare.rs and `Cargo.toml`); 2) run `cargo test -p xi-rope --no-default-features` to validate scalar-only builds; 3) record bench numbers with/without the feature to decide whether we need further abstraction.
