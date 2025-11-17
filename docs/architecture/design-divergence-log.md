# C# Port Design Divergence Log

> **Scope**: Track intentional deviations between the C# port and the Rust baseline, plus the monitoring hooks that make each decision safe.
> **Owner**: Architecture Mapper (co-maintained with QA)
> **Update Frequency**: Whenever a divergence is added, retired, or its monitoring plan changes.
> **Reviewers**: AI Architect · C# Implementer · Rust Porter · QA Engineer
> **Anchor Prefix**: Div
> **Last Synced Goal Tree**: 2025-11-18 (ties into `[BP-GoalTree]` / G2-G4)

---

## [Div-Active] Active Divergences
<a id="Div-Active"></a>
| Feature | Reason | Mitigation | Exit Criteria | QA Anchor |
| --- | --- | --- | --- | --- |
| NodeCursor versioning | Rust relies on `Arc::ptr_eq` + cursor descriptors, but C# reuses objects less aggressively and leans on `_editVersion`. | `_editVersion` increments + `CursorDescriptorParityTests` (`dotnet test Xi.Editor.sln -v m`) guard regressions; Stage D manifest (`[Fixture-Manifest]`, `[StageD::FixtureFlow]`) will backfill exporter evidence. | Export `cursor_descriptors.json` into `fixtures.manifest.json`, update docs with `NodeCursorState`, and prove parity under Stage D (`[QA-IngestionSmoke]`). | `[QA-IngestionSmoke]` |
| UTF-16 string leaves | .NET `string` is UTF-16; mirroring Rust's UTF-8 storage would balloon the MVP | `StringLeafOperations`, leaf-split fixtures, and `_editVersion` diagnostics highlight drift; Stage D manifests capture split points | Switch to shared `LeafSplit` CLI output or accept Rust-provided dual-metric helper; also requires `[TS-B2]` MetricAdapter | `[StageD::ParityAssets]` |
| Degraded Grapheme navigation | No ICU4N dependency yet; re-implementing `GraphemeCursor` would block M3 | `DegradedGraphemeNavigator` stitches at most one neighbor leaf, logs fallback counts via `GraphemeNavigationMetrics`, and cross-checks Stage D `grapheme_descriptors` entries in `fixtures.manifest.json`. | Adopt ICU4N or Rust trace once fallback >0.5% or Stage D manifest surfaces full descriptors; decision lived under G4/G6 review | `[QA-Telemetry]` |
| Chunk enumerator copy-on-read | Zero-copy spans require iterator façade + owned descriptors, which Rust has not exported yet | Copy enumerators + telemetry counters prove safety; every exporter run must stamp `chunk_descriptors.json` rows inside `fixtures.manifest.json` (`[StageD::FixtureFlow]`, `[Fixture-Manifest]`). | Replace enumerators with descriptor ingestion once `[TS-B3]` closes, `[QA-ChunkBench]` logs the 1 MB baseline, and Stage D manifest shows chunk entries. | `[QA-ChunkBench]` |
| TreeBuilder slice trace (Rust-only) | Rust CLI已输出 `TreeBuilderTracer` 事件，但 C# 缺失同名骨架，无法在 Stage D replay/诊断树形结构。 | 暂时完全依赖 Rust exporter + manifest 记录 slice trace，`[RPM-Matrix]` Skeleton Coverage/`[TS-B2]` 跟踪差距。 | 在 `src/xi.Core/Rope/Tree/` 引入 `TreeBuilderTracer`/`TreeBuilderEventKind` 占位，并在 Stage D Playbook 中完成 slice-trace CLI 接入/验证。 | `[StageD::FixtureFlow]` |

## [Div-Retired] Retired Divergences
<a id="Div-Retired"></a>
尚无条目；所有分歧仍处于监控与降级阶段。

## [Div-ChangeLog] Change Log
<a id="Div-ChangeLog"></a>
- **2025-11-18 – Template rollout**: adopted the table-based log, added chunk copy-on-read entry, and linked each divergence to the QA/Stage D anchors that verify it.

[StageD::ParityAssets]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::ParityAssets
[StageD::FixtureFlow]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::FixtureFlow
[QA-Telemetry]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-Telemetry
[QA-ChunkBench]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-ChunkBench
[QA-IngestionSmoke]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-IngestionSmoke
[Fixture-Manifest]: fixtures/parity-fixture-schema.md#fixture-manifest
