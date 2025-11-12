# Rust Workspace Slimming Log

## 2025-11-13 Snapshot
- Captured baseline builds after MSRV uplift.
- Logs: `xi-editor-ph7/rust/logs/20251113-cargo-check.txt`, `xi-editor-ph7/rust/logs/20251113-cargo-test.txt`.
- `cargo check --workspace` completed with warnings only (plugin catalog dead code, incremental hard-link fallback).
- `cargo test --workspace` passed all suites (328 tests + doc tests) with the same warning set and a `serde_test` future incompatibility notice.

## Bench Suite Migration
- Renamed benchmark directories to mark for removal:
  - `experimental/lang/benches` → `experimental/lang/benches.parked`
  - `core-lib/benches` → `core-lib/benches.parked`
  - `rope/benches` → `rope/benches.parked`
  - `trace/benches` → `trace/benches.parked`
  - `unicode/benches` → `unicode/benches.parked`
- Spot-checked builds:
  - `cargo check -p xi-rope`
  - `cargo check -p xi-core-lib`

Next: remove Criterion/dev-deps, update workspace manifests, and archive optional crates per phase plan.
