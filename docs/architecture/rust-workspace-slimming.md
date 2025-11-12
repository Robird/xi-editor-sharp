# Rust Workspace Slimming Log

## 2025-11-13 Snapshot
- Captured baseline builds after MSRV uplift.
- Logs: `xi-editor-ph7/rust/logs/20251113-cargo-check.txt`, `xi-editor-ph7/rust/logs/20251113-cargo-test.txt`.
- `cargo check --workspace` completed with warnings only (plugin catalog dead code, incremental hard-link fallback).
- `cargo test --workspace` passed all suites (328 tests + doc tests) with the same warning set and a `serde_test` future incompatibility notice.

## Bench Suite Migration
- 已删除:
  - `experimental/lang/benches`
  - `core-lib/benches`
  - `rope/benches`
  - `trace/benches`
  - `unicode/benches`
- Spot-checked builds:
  - `cargo check -p xi-rope`
  - `cargo check -p xi-core-lib`
- 当前各 crate 的 `Cargo.toml` 已无 `criterion` 或 `test::Bencher` 相关依赖，无需额外清理。

## Optional Crate Parking
- 删除非关键 crate：
  - `legacy/experimental-lang`
  - `legacy/lsp-lib`
  - `legacy/sample-plugin`
  - `legacy/syntect-plugin`
- `rust/Cargo.toml` 的 `members`/`default-members` 仅保留核心七个 crate（`xi-core`、`xi-core-lib`、`xi-plugin-lib`、`xi-rope`、`xi-rpc`、`xi-trace`、`xi-unicode`）。
- 移除未再使用的 `[patch.onig]` 覆盖；`cargo check --workspace` 现无该 warning，仍剩增量硬链接与 `PluginLoadError` dead code 告警。
- Next：整理 legacy crate 中的 bench/dev-deps 说明，并在迁移文档中挂接 legacy 对应关系。

## Trace Feature Shim
- `xi-core-lib` 增设 `trace` 可选特性并默认启用；依赖于 `xi-trace` 的 API 均改由 `crate::trace` 模块输出，在禁用特性时回退为 no-op stub。
- `tabs::save_trace` 等入口提供禁用态警告，方便在 C# 端尚未接入 tracing 时维持编译通过。
- Next：评估 `xi-plugin-lib`、`xi-core` 等仍直接引用 `xi-trace` 的路径，复用 shim 以支持进一步裁剪。

Next: 继续完善 legacy 目录说明，推广 trace shim 覆盖并清理 residual warnings（`PluginLoadError`、hard-link fallback）。
