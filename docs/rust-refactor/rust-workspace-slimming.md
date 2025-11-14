# Rust Workspace Slimming Log

## 2025-11-13 Snapshot
- Captured baseline builds after MSRV uplift.
- Logs: `xi-editor-ph7/rust/logs/20251113-cargo-check.txt`, `xi-editor-ph7/rust/logs/20251113-cargo-test.txt`.
- `cargo check --workspace` completed with warnings only (plugin catalog dead code, incremental hard-link fallback).
- `cargo test --workspace` passed all suites (328 tests + doc tests) with the same warning set and a `serde_test` future incompatibility notice.

## Bench Suite Migration
- 已改名:
  - `experimental/lang/benches` → `experimental/lang/benches.parked`
  - `core-lib/benches` → `core-lib/benches.parked`
  - `rope/benches` → `rope/benches.parked`
  - `trace/benches` → `trace/benches.parked`
  - `unicode/benches` → `unicode/benches.parked`
- Spot-checked builds:
  - `cargo check -p xi-rope`
  - `cargo check -p xi-core-lib`
- 当前各 crate 的 `Cargo.toml` 已无 `criterion` 或 `test::Bencher` 相关依赖，无需额外清理。

## Optional Crate Parking
- 删除非关键 crate：
  - `experimental/lang`
  - `lsp-lib`
  - `sample-plugin`
  - `syntect-plugin`
- `rust/Cargo.toml` 的 `members`/`default-members` 仅保留核心七个 crate（`xi-core`、`xi-core-lib`、`xi-plugin-lib`、`xi-rope`、`xi-rpc`、`xi-trace`、`xi-unicode`）。
- 移除未再使用的 `[patch.onig]` 覆盖；`cargo check --workspace` 现无该 warning，仍剩增量硬链接与 `PluginLoadError` dead code 告警。
- Next：在架构文档中标注这些 crate 的对照关系，以便后续如需回溯可指向 upstream。

## Trace Feature Shim
- `xi-core-lib` 增设 `trace` 可选特性并默认启用；依赖于 `xi-trace` 的 API 均改由 `crate::trace` 模块输出，在禁用特性时回退为 no-op stub。
- `tabs::save_trace` 等入口提供禁用态警告，方便在 C# 端尚未接入 tracing 时维持编译通过。
- Next：评估 `xi-plugin-lib`、`xi-core` 等仍直接引用 `xi-trace` 的路径，复用 shim 以支持进一步裁剪。

Next: 推广 trace shim 覆盖并关注 `.cargo/config` 禁用增量后的构建时长。

## Warning Cleanup (2025-11-13)
- `PluginLoadError` 实现 `Display`/`Error`，日志输出改用字符串描述，编译器不再提示未读字段。
- 新增 `rust/.cargo/config.toml`，设置 `build.incremental = false` 以规避 Windows 环境下的硬链接告警（并记录后续需关注的构建时长影响）。
- `serde_test` 升级至 1.0.177，future incompat 报告消失；`cargo test -p xi-rope` 验证 Rope 序列化回归测试正常。

## Documentation & Script Updates (2025-11-13)
- `xi-editor-ph7/README.md` 更新为注明最小 workspace 与 MSRV 1.75，提示已移除插件/示例工程且性能基准需改用 upstream。
- `docs/architecture/module-migration-plan.md` 增补工作区精简备注，指向核心七个 crate。
- `AGENTS.md` 记录阶段 1 结果，提醒后续任务关注警告清理与 trace shim 推广。
- `rust/run_all_checks` 保留 bench 跳过提示并新增 workspace 精简说明，避免协作者困惑。
