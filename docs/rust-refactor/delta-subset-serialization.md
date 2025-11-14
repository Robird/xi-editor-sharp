# Delta & Subset Serialization Cleanup

## Goals
- Separate serde-only code from core Delta/Subset logic so the data structures compile and behave identically with or without the `serde` feature.
- Provide portable helper APIs (`to_ops`, `to_segments`, etc.) consumed by both serde shims and eventual C# bindings.
- Establish golden artifacts and documentation that lock in the current JSON schema for Fuchsia sync and plugin ecosystems.

## Background
- `multiset.rs`, `delta.rs`, and `engine.rs` currently mix serde derives with editing logic, and disabling the `serde` feature removes entire data paths.
- Downstream crates (Fuchsia ledger, plugins) rely on stable JSON produced through these derives; tests such as `delta.rs` `serialize_delta` assert exact output.
- The C# port needs serde-free parity plus a deterministic serialization story for cross-language validation.

## Deliverables
- Core modules free from unconditional serde derives; serde logic moved to `*_serde.rs` or equivalent gated submodules.
- Helper methods returning serialization-friendly data (segments, ops, revision logs) available without enabling serde.
- Golden serialization fixtures and snapshot tests covering `Delta`, `Subset`, and `Engine` JSON.
- Updated documentation describing schema, helper APIs, and feature-flag usage, with guidance mirrored in `rope-port-mapping.md`.

## Implementation Plan

### Stage 0 – Baseline capture (shared for all stages)
1. Collect canonical JSON samples for `Delta`, `Subset`, and `Engine` (fixtures from existing tests and Fuchsia ledger traces).
2. Add `serde_test::assert_ser_tokens` or snapshot-based tests to lock current behavior before refactoring.
3. Ensure CI runs `cargo test -p xi-rope` under both `--features serde` and `--no-default-features` (expected to fail initially, serving as a guard).

- **Progress (2025-11-14):** Added `subset_serialization_regression` in `xi-editor-ph7/rust/rope/src/multiset.rs` (gated behind `cfg(feature = "serde")`) to capture the current Subset JSON shape. Fixture string: `{"segments":[{"len":2,"count":0},{"len":3,"count":3},{"len":1,"count":0},{"len":1,"count":1},{"len":2,"count":0}]}`.
- **Progress (2025-11-14):** Captured the Delta baseline via `delta_serialization_regression` in `xi-editor-ph7/rust/rope/src/delta.rs`. Fixture string: `{"els":[{"copy":[0,3]},{"insert":"[ins]"},{"copy":[8,10]},{"insert":"!"},{"copy":[15,62]}],"base_len":62}`.

### Stage 1 – Multiset (`Subset` / `Segment`)
1. Introduce read-only helpers returning segment iterators (`Subset::segments_iter`, `Segment::to_range`). Keep helpers `pub(crate)` initially.
2. Move serde derives and impls into `multiset/serde.rs` guarded by `cfg(feature = "serde")`; re-export only the derives.
3. Update `SubsetBuilder`, iterators, and tests to consume the new helpers.
4. Validate: run targeted tests (`cargo test -p xi-rope --features serde multiset`) and the new snapshot suite; repeat with serde disabled to show core logic still compiles.

- **Progress (2025-11-14):** Implemented helper surface with `Subset::segment_triples()`, `Subset::from_segment_triples()`, and `Subset::segment_count()` (all `pub(crate)`). Serde derives removed from `Segment`/`Subset`; new gated module `subset_serde` reuses the helpers to provide manual `Serialize`/`Deserialize` while preserving the Stage 0 fixture. Follow-up: Stage 2 (Delta) and Stage 3 (Engine) still require analogous helper extraction and serde shims.

### Stage 2 – Delta (`Delta`, `InsertDelta`, `DeleteDelta`)
1. Add helpers exposing op sequences (`Delta::ops_iter`, `InsertDelta::spans`). Ensure they do not depend on serde traits.
2. Restructure `delta/serde_impls.rs` to consume the helpers, trimming any direct access to private fields.
3. If practical, generalize the serde shim to work with `Delta<TInfo, TLeaf>` (defaulting to `RopeInfo`/`String`) without widening the public surface.
4. Extend snapshot tests to cover mixed deltas (insert+delete). Re-run Stage 0 checks.

- **Progress (2025-11-14):** Added `Delta::base_len()`, `Delta::element_count()`, `Delta::iter_elements()`, `Delta::element_triples()`, and `Delta::from_element_vec()/from_element_tuples()` helpers (all `pub(crate)`) so serde and future C# bindings no longer reach into private fields. `serde_impls.rs` now hand-writes `Serialize`/`Deserialize` for `DeltaElement`/`Delta` using those helpers, eliminating the temporary `RopeDelta_` wrappers while keeping the Stage 0 JSON fixture locked by `delta_serialization_regression`.

### Stage 3 – Engine / Revision log

- **Progress (2025-11-14):** Added `Engine::revision_log()`, `Engine::text_snapshot()`, `Engine::tombstones_snapshot()`, `Engine::deletes_from_union_snapshot()`, `Engine::undone_groups_snapshot()`, and `Engine::from_serialized_state()` helpers alongside `RevisionRef`/`RevisionContentsRef` so serde and future C# bindings can enumerate revisions without touching private fields. Manual serde implementations now live in `engine::serde_impl` and reuse those helpers while keeping the structs serde-free. A gated regression test `engine_serialization_regression` locks the JSON fixture `{"text":"Hi there","tombstones":"Well, ","deletes_from_union":{"segments":[{"len":6,"count":1},{"len":8,"count":0}]},"undone_groups":[2],"revs":[{"rev_id":{"session1":0,"session2":0,"num":0},"max_undo_so_far":0,"edit":{"Undo":{"toggled_groups":[],"deletes_bitxor":{"segments":[]}}}},{"rev_id":{"session1":1,"session2":0,"num":1},"max_undo_so_far":0,"edit":{"Edit":{"priority":0,"undo_group":0,"inserts":{"segments":[{"len":2,"count":1}]},"deletes":{"segments":[{"len":2,"count":0}]}}}},{"rev_id":{"session1":1,"session2":0,"num":2},"max_undo_so_far":1,"edit":{"Edit":{"priority":1,"undo_group":1,"inserts":{"segments":[{"len":2,"count":0},{"len":6,"count":1}]},"deletes":{"segments":[{"len":8,"count":0}]}}}},{"rev_id":{"session1":1,"session2":0,"num":3},"max_undo_so_far":2,"edit":{"Edit":{"priority":0,"undo_group":2,"inserts":{"segments":[{"len":6,"count":1},{"len":8,"count":0}]},"deletes":{"segments":[{"len":14,"count":0}]}}}},{"rev_id":{"session1":1,"session2":0,"num":4},"max_undo_so_far":2,"edit":{"Undo":{"toggled_groups":[2],"deletes_bitxor":{"segments":[{"len":6,"count":1},{"len":8,"count":0}]}}}}]}`.
1. Extract serde-only fields from `Engine`, `Revision`, and `Contents` into a gated module (`engine/serde.rs`). Introduce an internal `SerializableEngine` struct mirroring the JSON shape.
2. Add helpers on core types for revision walk (`Engine::revision_log()`, `Revision::as_payload()`), reused by both serde shim and C# port.
3. Create integration tests that round-trip ledger samples via serde (requires fixture capture or synthetic ledger test).
4. Coordinate with Fuchsia consumers to validate the preserved schema; record any field mapping notes in docs.

### Stage 4 – Feature flag hardening and docs
1. Audit workspace `Cargo.toml` files to stop unconditionally enabling `serde` on `xi-rope`; add opt-in flags with explicit propagation where needed.
2. Update `docs/architecture/rope-port-mapping.md` and `AGENTS.md` to reflect the new helper surface.
3. Add build matrix entries (CI script, `rust/run_all_checks`) covering `--no-default-features` and `--features serde`.
4. Document schema (JSON keys, numeric units, ordering) in this file and link from `docs/rust-refactor/iterator-facade-export.md` if relevant.

- **Progress (2025-11-14):** Added opt-in `serde` feature gating through `xi-core-lib` and workspace-level `serde` flag so `xi-rope/serde` is disabled by default; refreshed `xi-editor-ph7/rust/run_all_checks` to run `cargo test -p xi-rope` under both `--no-default-features` and `--features serde`.

## Validation Matrix
- `cargo test -p xi-rope --features serde` (default) – must stay green throughout.
- `cargo test -p xi-rope --no-default-features` – becomes required once Stage 1 lands.
- `cargo test --workspace --all-features` – ensures dependent crates remain compatible.
- Snapshot or serde-test comparisons for every fixture, run before and after each stage.
- Optional: QuickCheck/FsCheck style fuzz to ensure helper iterators align with existing logic.

## Risks & Mitigations
- **Schema drift:** Mitigate with pre/post golden fixtures and `serde_test::assert_ser_tokens` ensuring byte-identical output.
- **Fuchsia ledger regression:** Coordinate with ledger owners, add integration fixtures, and provide a staged rollout plan.
- **Public API creep:** Keep new helpers `pub(crate)` initially; only promote once C# binding requirements crystallize.
- **Feature-flag churn:** Introduce CI coverage and update documentation so downstream crates know how to opt in explicitly.
- **Generic delta surface expansion:** Document decision (specialized vs generic) before Stage 2 merges, and gate experimental APIs behind `#[cfg(feature = "experimental-generic-delta")]` if needed.

## Dependencies
- SharedNode/COW helpers already landed, enabling safe refactors without altering mutation semantics.
- Metric templating work reduces duplicate logic touched by Delta helpers.
- Need availability of ledger JSON samples (coordinate with Rust repo maintainers).

## Stage Decisions (2025-11-14)
- **Schema versioning**：暂不在 JSON 结构内嵌入显式版本号。现阶段目标是维持与 Fuchsia ledger 及现有插件的二进制兼容性，引入版本字段会立刻破坏黄金快照与外部消费端。版本记录改由文档 + golden fixture 校验承担；若将来出现破坏性变更，再通过新文件后缀或独立通道引入版本元数据。
- **Alternative encodings**：保持 serde(JSON) 专属 shim，暂不扩展到 bincode/messagepack。当前跨语言验证与 C# 端只依赖 JSON；强行抽象公共编码层会额外锁定 API 表面而缺乏收益。若后续需要二进制格式，可在 serde 子模块旁新增平行 shim，并重用 Stage 1-3 建立的 helper。
- **Helper surface for .NET**：计划对外公开（或 `pub(crate)` 暂存）的最小集合如下，保证无须访问私有字段即可完成 JSON 序列化与 C# 互操作：
	- `Delta::iter_elements()`（或等价）返回 `Copy{begin,end}` / `Insert{rope}` 枚举视图，配套 `Delta::base_len()`。
	- `InsertDelta::iter_inserts()` 与 `InsertDelta::copied_ranges()`，供 serde shim 与 C# `factor()` 路径共享。
	- `Subset::segments_iter()` / `Subset::is_empty()` / `Subset::len()`，返回 `(start, len, count)` 三元组迭代器以匹配现有 JSON 阵列结构。
	- `SubsetBuilder::from_segments()`（可选）便于测试和 C# 回写。
	- `Engine::revision_log()`（返回只读迭代器），`Revision::as_delta()`，`Revision::tombstones()`，让 serde shim 与 ledger 同用。
	- `Contents::as_text()` / `Contents::as_tombstones()`，对应 JSON 中双字段布局。
 这些 API 可先标记为 `pub(crate)` 并在 C# 需要跨 crate 调用时再择机公开；所有 helper 默认返回只读迭代器或 `Copy` 数据，避免外部修改内部状态。

## Open Questions
- 调整为文档外追踪的 schema 版本是否需要同步到 plugin 协议文档？
- 第二种编码格式若落地（例如 Fuchsia ledger 引入 bincode），是否同步沿用同一 helper 集合还是增设独立模块？
- C# 侧若改用泛型叶片（非 `String`），上述 helper 是否需要进一步泛型化或扩展 trait 约束？

## Communication & Documentation
- Announce the staged plan in the weekly sync; solicit feedback from plugin authors.
- After each stage, update `docs/skeleton/rope.md` and `docs/skeleton/xi.Core.Rope.cs` to keep cross-language references aligned.
- Record any downstream migration notes (e.g., ledger config changes) in `docs/rust-refactor/shared-node-api.md` or a new ledger-focused appendix.
