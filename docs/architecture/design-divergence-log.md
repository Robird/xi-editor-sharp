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
| UTF-16 string leaves | .NET `string` is UTF-16; mirroring Rusts UTF-8 storage would balloon the MVP | `StringLeafOperations`, leaf-split fixtures, and `_editVersion` diagnostics highlight drift; Stage D manifests capture split points | Switch to shared `LeafSplit` CLI output or accept Rust-provided dual-metric helper; also requires `[TS-B2]` MetricAdapter | `[StageD::ParityAssets]` |
| Degraded Grapheme navigation | No ICU4N dependency yet; re-implementing `GraphemeCursor` would block M3 | `DegradedGraphemeNavigator` stitches at most one neighbor leaf and logs fallback counts via `GraphemeNavigationMetrics` | Adopt ICU4N or Rust trace once fallback >0.5% or CLI trace arrives; decision lived under G4/G6 review | `[QA-Telemetry]` |
| Chunk enumerator copy-on-read | Zero-copy spans require iterator façade + owned descriptors, which Rust has not exported yet | `RopeChunkEnumeratorDiagnostics` tracks chunk count/max length/allocations; parity tests run against string copies | Replace enumerators with descriptor ingestion once `[TS-B3]` closes and `[QA-ChunkBench]` shows acceptable perf; also needs Stage D CLI flag | `[QA-ChunkBench]` |

## [Div-Retired] Retired Divergences
<a id="Div-Retired"></a>
尚无条目；所有分歧仍处于监控与降级阶段。

## [Div-ChangeLog] Change Log
<a id="Div-ChangeLog"></a>
- **2025-11-18 – Template rollout**: adopted the table-based log, added chunk copy-on-read entry, and linked each divergence to the QA/Stage D anchors that verify it.

[StageD::ParityAssets]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::ParityAssets
[QA-Telemetry]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-Telemetry
[QA-ChunkBench]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-ChunkBench
