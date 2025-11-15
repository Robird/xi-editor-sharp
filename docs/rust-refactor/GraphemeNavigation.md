**Grapheme Navigation**
- **Current Rust design**: `Cursor::next_grapheme`/`prev_grapheme` wrap `unicode_segmentation::GraphemeCursor`, repeatedly feeding leaf slices and servicing `GraphemeIncomplete` by walking to neighbouring leaves.
- **Portability blocker**: .NET lacks an equivalent stateful grapheme iterator; `StringInfo` operates on contiguous strings and cannot resume across segmented leaves. Re-implementing `GraphemeCursor`’s Unicode tables and state machine in C# is non-trivial and error-prone.
- **Potential Rust-side action**: Extract the state machine into a helper module, exposing a serializable `GraphemeStep` trace (behind a debug/fixture feature) that records which leaf slices and context requests occurred for a given navigation. This keeps runtime behavior unchanged while providing an authoritative spec the C# port can mirror.
- **Resulting C# implication**: With recorded traces and a documented state machine, the C# implementation can recreate the control flow using its own Unicode tables (or ICU) and validate against the Rust-generated fixtures when we eventually pursue full parity.

**Decision（2025-11-15）**
- **初版策略**：C# 侧首轮移植仅保证 UTF-16 surrogate 对不被拆分，遇到 grapheme 边界判定时最多向前/向后补足一个相邻叶片；若仍无法确认边界，则回退至单 code point 导航。
- **动机**：
	- **安全性**：受限上下文可防止恶意构造的超长 grapheme 诱发无限拼接或过度内存占用。
	- **覆盖常态场景**：常见文本（中日韩、拉丁、Emoji 组合）通常落在同一叶或毗邻叶片内，单叶 + 相邻叶即可满足大多数人工/LLM 场景。
	- **实现复杂度**：不需要引入 ICU/ICU4N 或复刻 `GraphemeCursor` 状态机，易于在当前 Rope 基础上落地并复用 existing UTF-16 helper。
	- **扩展点保留**：接口会暴露上下文抽象，允许后续替换为更完整的 grapheme 导航实现或接入 Rust trace。
- **约束**：
	- 跨越两片以上的合法 grapheme（长 combining 串、极长 ZWJ 链等）会被拆分，光标行为与 Rust 不一致。
	- Stage D 若引入 Rust 端 grapheme fixtures，需在测试中标记此限制或提供条件跳过。
	- 未来若真实需求覆盖此类长序列，计划再回到“Rust helper + trace”路线重新实现。

**Summary**
- The current grapheme navigation lives entirely in `xi-editor-ph7/rust/rope/src/rope.rs`, where `Cursor::next_grapheme`/`prev_grapheme` drive `unicode_segmentation::GraphemeCursor` across rope leaves using `Cursor`’s leaf-walking APIs.
- Extracting this loop into a reusable helper is straightforward because it only touches public `Cursor` methods plus the public `GraphemeCursor` API; instrumentation can observe each chunk/offset handoff without touching Unicode tables.
- A trace-emitting helper would unblock deterministic fixtures for C#, but parity still depends on supplying a compatible segmentation engine (or replaying traces) and handling UTF-8↔UTF-16 conversions.
- C# 初版明确采用“单片 + 相邻片 + code point 回退”的降级实现，在 API 层预留扩展，待真实需求验证后再评估是否追求 Rust 等价实现。

**Key Findings**
- `xi-editor-ph7/rust/rope/src/rope.rs` (lines ~500-575) shows `Cursor::next_grapheme` building a `GraphemeCursor`, resolving `GraphemeIncomplete::{PreContext,NextChunk}` via `Cursor::prev_leaf`/`next_leaf`, and returning the boundary as a global byte offset.
- `xi-editor-ph7/rust/rope/src/rope.rs` (lines ~575-640) mirrors this logic for `Cursor::prev_grapheme`, using `GraphemeIncomplete::{PreContext,PrevChunk}` and `Cursor::set` to rewind into prior leaves.
- `xi-editor-ph7/rust/rope/src/tree.rs` (lines ~780-1100) defines the `Cursor` cache, leaf accessors, and measurement utilities that the grapheme routines rely on; any helper must keep using these public methods to avoid duplicating tree internals.
- `xi-editor-ph7/rust/rope/Cargo.toml` pins `unicode-segmentation = 1.2.1`, fixing the Unicode tables/algorithms that Rust exposes; parity requires either freezing to that spec or clearly versioning fixtures.

**Risks & Unknowns**
- The trace helper must capture enough context (chunk text, global/leaf offsets, incomplete variants) to replay decisions; logging raw UTF-8 slices may be large unless gated behind an opt-in feature.
- `unicode-segmentation` could add new `GraphemeIncomplete` variants in future releases; helper design should default-match to avoid panics if crates update.
- C# still lacks a segmentation engine that matches Unicode 9-era behavior; trace fixtures validate results but do not provide the runtime algorithm, and UTF-8 byte offsets must be converted to UTF-16 code-unit indices.

**Proposed Implementation Steps in Rust**
- Introduce a `grapheme` helper module (e.g., `rope/src/grapheme.rs`) exposing a `GraphemeNavigator` that accepts a `Cursor`, direction enum, and optional trace sink; move the shared loop there while keeping public API unchanged.
- Define a `GraphemeStep` struct (direction, chunk range, chunk length, boundary result, incomplete discriminator, optional context slice length) behind `#[cfg(feature = "trace")]` and emit steps from the helper.
- Update `Cursor::next_grapheme`/`prev_grapheme` in `rope.rs` to delegate to the helper, preserving existing semantics and unit tests.
- Wire trace emission into existing fixture infrastructure (`serde_fixtures.rs`) so enabling the feature flag writes JSON traces for Stage D.

**Validation Strategy (tests/fixtures)**
- Retain current grapheme regression coverage (existing cursor tests) and add targeted tests that assert `GraphemeNavigator` returns identical offsets for representative multi-leaf texts.
- When the trace feature is enabled, add snapshot tests that serialize a short rope’s grapheme steps and compare against checked-in fixtures.
- Integrate trace replay into Stage D pipeline to ensure Rust and C# consume identical fixtures and detect drift when upgrading `unicode-segmentation`.

**C# Port Implications**
- Traces must include both UTF-8 byte offsets and derived UTF-16 lengths so the C# rope (which is UTF-16 based) can map boundaries without rescanning.
- C# can replay traces to validate its grapheme iterator, but shipping parity still needs either a port of `GraphemeCursor` or an ICU/ICU4N dependency pinned to the same Unicode data.
- Fixture-driven tests should cover surrogate pairs, combining sequences, and CRLF spans to confirm the C# navigator matches Rust boundaries.

Recommendation: Extract the grapheme-driving loop into a helper with optional trace emission—this is practical, keeps maintenance low, and supplies the observability C# needs, provided we pair it with a Unicode-compatible segmentation strategy on the C# side.
