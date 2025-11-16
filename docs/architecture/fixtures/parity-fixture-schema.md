# Parity Fixture Schema for `export-serde-fixtures`

> **Scope**: Cursor, chunk/line, and grapheme descriptor payloads exported by `cargo run -p xi-rope --features serde --bin export-serde-fixtures`.
> **Last reviewed**: 2025-11-17 by Rust Porter.

## 1. Feature Gates & Invocation Cheatsheet

| Gate | Default | Purpose | Applies to |
|------|---------|---------|-----------|
| `serde` | ❌ (opt-in) | Enables JSON serialization helpers, exporter binary, and fixture structs. | **Required** for all schema in本文件。
| `cursor_state` | ❌ | Adds owning cursor snapshots inside `CursorDescriptor`; exporter emits the same schema but captures richer debug info when the feature is enabled during generation. | Optional when investigating descriptor state; safe to leave off for baseline exports. |
| `tree_builder_slice_trace` | ❌ | Enables `--tree-builder-trace` CLI flag and trace structs that share the same exporter binary. | Not needed for parity fixtures but documented for completeness. |

Typical parity export (creates/refreshes all fixture families in `tests/xi.Core.Tests/Fixtures`):

```powershell
cargo run -p xi-rope --features serde --bin export-serde-fixtures -- `
  --dir tests/xi.Core.Tests/Fixtures `
  --cursor-descriptors tests/xi.Core.Tests/Fixtures/cursor_descriptors `
  --chunk-descriptors tests/xi.Core.Tests/Fixtures/chunk_descriptors `
  --grapheme-descriptors tests/xi.Core.Tests/Fixtures/grapheme_descriptors
```

> Add `--features serde,cursor_state` if cursor state capture is required, or append `--features serde,tree_builder_slice_trace` plus `--tree-builder-trace <dir>` to simultaneously refresh slice traces.

---

## 2. Cursor Descriptor Fixtures (`cursor_descriptors.json`)

- **Location**: `tests/xi.Core.Tests/Fixtures/cursor_descriptors/cursor_descriptors.json`.
- **Export flag**: `--cursor-descriptors <output-dir>` (requires `serde`; accepts additional gates).
- **Top-level shape**: JSON array of `CursorDescriptorFixture` objects (no metadata wrapper). Samples are stable and kept under source control for Stage D parity tests.

### 2.1 `CursorDescriptorFixture`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | ✅ | Stable identifier (snake_case) describing the scenario. |
| `text` | string | ✅ | Source rope contents at capture time. |
| `edited_text` | string | ⛔ optional | If present, text that was applied after capturing the descriptor to validate invalidation semantics. |
| `expect_apply` | bool | ✅ (default true) | Whether the descriptor should rehydrate successfully against `text`. Defaults to `true` even when omitted. |
| `expect_apply_after_edit` | bool | ⛔ optional | Explicit expectation when applying the descriptor to `edited_text`. If `null`, consumers fall back to `expect_apply`. |
| `notes` | string | ⛔ optional | Human-readable scenario summary; keep in sync with §3.2.4 of `m3-implementation-plan.md`. |
| `metric` | enum(`"base"\|"lines"\|"utf16"`) | ✅ | Metric used during navigation; informs parity tests which conversion shim to exercise. |
| `position` | integer | ✅ | Absolute byte-based position (Base metric) captured during descriptor creation. |
| `is_valid` | bool | ✅ | Mirrors `CursorDescriptor::is_valid()`. Invalid descriptors must not be re-applied without rebuilding. |
| `offsets` | [`CursorDescriptorOffsets`](#212-cursordescriptoroffsets) | ✅ | Canonical offsets describing the owning leaf. |
| `leaf_path` | array<[`CursorDescriptorFrame`](#213-cursordescriptorframe)> | ✅ | Ancestor frames (deepest leaf first) that reconstruct the path for cache priming.

#### 2.1.2 `CursorDescriptorOffsets`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `offset_of_leaf` | integer | ✅ | Absolute Base offset of the leaf that holds the cursor. |
| `offset_in_leaf` | integer | ✅ | Offset relative to the leaf start (`position - offset_of_leaf`). |
| `leaf_len` | integer | ⛔ optional | Leaf length at capture time; omitted when not tracked (e.g., after invalidation). |

#### 2.1.3 `CursorDescriptorFrame`

| Field | Type | Description |
|-------|------|-------------|
| `node_height` | integer | Height of the ancestor node. |
| `node_len` | integer | Aggregate byte length carried by that node. |
| `child_index` | integer | Index of the child selected at this depth. |
| `child_offset` | integer | Accumulated Base units before this child. |

### 2.2 Metadata Notes

- Schema revisions must be recorded in `docs/architecture/m3-implementation-plan.md` §1.5 (T0 dependencies) and referenced by Stage D docs.
- When `cursor_state` is enabled during export, descriptors stay schema-compatible; the extra gate only affects internal instrumentation.

---

## 3. Chunk & Line Descriptor Fixtures (`chunk_descriptors.json`)

- **Location**: `tests/xi.Core.Tests/Fixtures/chunk_descriptors/chunk_descriptors.json`.
- **Export flag**: `--chunk-descriptors <output-dir>`.
- **Top-level shape**: Object containing `metadata`, `chunk_descriptors[]`, and `line_descriptors[]`.

### 3.1 `ChunkDescriptorMetadata`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `schema_version` | string | ✅ | Semantic version of this schema (`1.0.0` as of Nov 2025). |
| `rust_commit` | string | ⛔ optional | Git SHA recorded during export; enables traceability. |
| `generated_at_unix_millis` | integer | ✅ | UTC timestamp in milliseconds. |
| `chunk_descriptor_count` | integer | ✅ | Count of entries in `chunk_descriptors`. |
| `line_descriptor_count` | integer | ✅ | Count of entries in `line_descriptors`.

### 3.2 `ChunkDescriptor`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sample` | string | ✅ | Fixture generator ID (see `chunk_samples()` in Rust). |
| `chunk_index` | integer | ✅ | Zero-based index within the sample. |
| `text` | string | ✅ | Exact chunk contents. |
| `byte_range` | [`RangeSnapshot`](#35-rangesnapshot) | ✅ | `[start, end)` byte offsets in Base metric. |
| `utf16_range` | `RangeSnapshot` | ✅ | UTF-16 offsets measured via `Rope::convert_utf16_from_bytes`. |
| `leaf_range` | `RangeSnapshot` | ✅ | Span of the owning leaf in Base units. |
| `contains_crlf` | bool | ✅ | Signals `\r\n` boundaries within the chunk. |
| `is_empty` | bool | ✅ | True for zero-length chunks (only emitted for empty documents). |
| `tags` | string[] | ✅ | Sorted + deduplicated semantic tags (e.g., `chunk`, `emoji`, `crlf`). |
| `path` | [`PathFrameSnapshot`](#36-pathframesnapshot)[] | ✅ | Path frames derived from `CursorDescriptor`. |
| `context` | [`ChunkContext`](#34-chunkcontext) | ✅ | Adjacent text windows for debugging.

### 3.3 `LineDescriptor`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sample` | string | ✅ | Source sample ID. |
| `line_index` | integer | ✅ | Zero-based index. |
| `raw` | string | ✅ | Raw slice including trailing newline characters. |
| `logical` | string | ✅ | Logical line content without terminal newline. |
| `byte_range` | `RangeSnapshot` | ✅ | Base offsets. |
| `utf16_range` | `RangeSnapshot` | ✅ | UTF-16 offsets. |
| `newline_kind` | enum(`none|lf|cr|cr_lf`) | ✅ | Normalised newline tag. |
| `tags` | string[] | ✅ | Semantic tags (e.g., `line`, `crlf`).

### 3.4 `ChunkContext`

| Field | Type | Description |
|-------|------|-------------|
| `before` | string | Up to 16 codepoints preceding the chunk. |
| `after` | string | Up to 16 codepoints following the chunk. |

### 3.5 `RangeSnapshot`

| Field | Type | Description |
|-------|------|-------------|
| `start` | integer | Inclusive offset. |
| `end` | integer | Exclusive offset.

### 3.6 `PathFrameSnapshot`

Matches cursor frames; see §2.1.3.

### 3.7 Sample Command

```powershell
cargo run -p xi-rope --features serde --bin export-serde-fixtures -- `
  --chunk-descriptors tests/xi.Core.Tests/Fixtures/chunk_descriptors
```

---

## 4. Grapheme Descriptor Fixtures (`grapheme_descriptors.json`)

- **Location**: `tests/xi.Core.Tests/Fixtures/grapheme_descriptors/grapheme_descriptors.json`.
- **Export flag**: `--grapheme-descriptors <output-dir>`.
- **Top-level shape**: Object with `metadata` and `grapheme_descriptors[]`.

### 4.1 `GraphemeDescriptorMetadata`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `schema_version` | string | ✅ | Starts at `1.0.0`. Increment when adding/removing fields. |
| `rust_commit` | string | ⛔ optional | Git SHA used for traceability. |
| `generated_at_unix_millis` | integer | ✅ | UTC timestamp in ms. |
| `descriptor_count` | integer | ✅ | Total cluster entries exported.

### 4.2 `GraphemeDescriptor`

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sample` | string | ✅ | Generator ID (see `grapheme_samples()`). |
| `cluster_index` | integer | ✅ | Grapheme index within the sample. |
| `cluster` | string | ✅ | Exact grapheme cluster text. |
| `byte_range` | `RangeSnapshot` | ✅ | Base offsets for the cluster. |
| `utf16_range` | `RangeSnapshot` | ✅ | UTF-16 offsets. |
| `scalar_count` | integer | ✅ | Number of Unicode scalar values in the cluster. |
| `contains_zwj` | bool | ✅ | Whether the cluster includes zero-width joiners. |
| `is_ascii` | bool | ✅ | Quick flag for pure ASCII clusters. |
| `crosses_leaf` | bool | ✅ | True when the cluster spans more than one rope leaf. |
| `requires_fallback` | bool | ✅ | Indicates whether the C# grapheme navigator must fall back to code-point logic (per §2.4 of the M3 plan). |
| `tags` | string[] | ✅ | Sorted semantic tags (e.g., `grapheme`, `emoji`, `cross_leaf`). |
| `context` | [`GraphemeContext`](#44-graphemecontext) | ✅ | Bounded before/after text windows (24 codepoints each). |
| `leaf` | [`LeafSnapshot`](#45-leafsnapshot) | ✅ | Captures owning leaf range and path to reproduce the scenario.

### 4.3 Supporting Types

#### 4.3.1 `GraphemeContext`

| Field | Type | Description |
|-------|------|-------------|
| `before` | string | Text preceding the cluster (clamped to char boundaries). |
| `after` | string | Text following the cluster. |

#### 4.3.2 `LeafSnapshot`

| Field | Type | Description |
|-------|------|-------------|
| `range` | `RangeSnapshot` | Leaf span in Base units. |
| `path` | `PathFrameSnapshot[]` | Frames that describe how to reach the leaf (see §2.1.3). |

### 4.4 Sample Command

```powershell
cargo run -p xi-rope --features serde --bin export-serde-fixtures -- `
  --grapheme-descriptors tests/xi.Core.Tests/Fixtures/grapheme_descriptors
```

---

## 5. Validation Checklist

1. Enable the correct feature set (`serde` mandatory; add `cursor_state`/`tree_builder_slice_trace` only when their diagnostics are needed).
2. Run the combined export command (see §1) or `scripts/refresh_serialization_fixtures.ps1` (Stage D) to update all fixture families in one pass.
3. Commit both the JSON payloads and this schema document whenever fields change. Keep `docs/csharp-refactor/rope-serialization-fixture-playbook.md` and `docs/architecture/m3-implementation-plan.md` in sync with schema bumps.
