# Parity Fixture Schema for `export-serde-fixtures`

> **Scope**: Track the JSON schemas consumed by Stage D parity assets（`subset/delta/engine` baselines、`cursor`、`chunk/line`、`grapheme`、`tree_builder_slice_trace`）。
> **Owner**: Rust Porter · QA Engineer
> **Update Frequency**: After every CLI/schema tweak or Stage D export script change.
> **Reviewers**: AI Architect · Architecture Mapper · C# Implementer
> **Anchor Prefix**: Fixture
> **Last Synced Goal Tree**: 2025-11-19

---

## [Fixture-Overview] Export 背景
<a id="Fixture-Overview"></a>
- 所有 schema 服务于 `[StageD::ParityAssets]`；刷新流程与脚本位于 `[StageD::FixtureFlow]`。
- `export-serde-fixtures` 是唯一输出来源，`tests/xi.Core.Tests/Fixtures` 中的快照为 Stage D 单一事实。
- Schema 变更需同时回写 `m3-implementation-plan.md`（T0 依赖）与 QA 手册，以便 `[QA-IngestionSmoke]` 复用。

---

## [Fixture-FeatureGates] Feature Gates & CLI
<a id="Fixture-FeatureGates"></a>

| Gate | Default | 作用 | 影响 |
| --- | --- | --- | --- |
| `serde` | ❌（必显式开启） | 启用 JSON 序列化 helper、导出二进制与所有 parity 结构 | **所有** schema 必需 |
| `cursor_state` | ✅ | 在 `CursorDescriptor` 导出期间捕获 `NodeCursorState` 快照（含 `edit_version`/`path`/`metric`） | Stage D parity 默认启用；若禁用需在 `[StageD::FixtureFlow]` 记录理由 |
| `tree_builder_slice_trace` | ❌ | 启用 `--tree-builder-trace`（与 parity 资产共用 exporter，可并行生成 slice trace） | 输出写入 `tests/xi.Core.Tests/Fixtures/tree_builder_slice/` 并在 manifest 中登记 `tree_builder_slice_trace@1.0.0` |

**组合命令（刷新全部 parity 资产）**

```powershell
cargo run -p xi-rope --features serde --bin export-serde-fixtures -- `
  --dir tests/xi.Core.Tests/Fixtures `
  --cursor-descriptors tests/xi.Core.Tests/Fixtures/cursor_descriptors `
  --chunk-descriptors tests/xi.Core.Tests/Fixtures/chunk_descriptors `
  --grapheme-descriptors tests/xi.Core.Tests/Fixtures/grapheme_descriptors
```

> 默认命令现包含 `--features serde,cursor_state`；若需 tree trace，可追加 `tree_builder_slice_trace --tree-builder-trace <dir>`。任何临时禁用/追加 feature 都要在 `[StageD::FixtureFlow]` 记录用途。

---

## [Fixture-Manifest] Manifest & Hash
<a id="Fixture-Manifest"></a>

- **命令**：`export-serde-fixtures` 始终带有 `--emit-manifest <path>`（默认为 `tests/xi.Core.Tests/Fixtures/fixtures.manifest.json`，可通过脚本参数或 CLI 覆盖）。刷新 parity 资产时请把 manifest 路径写入 `[StageD::FixtureFlow]` 日志，供 Goal Tree/QA 校验。
- **结构**：

| 字段 | 说明 |
| --- | --- |
| `rust_commit` | `git rev-parse HEAD` 的结果（`xi-editor-sharp` 仓库）。 |
| `cli_rev` | `env!("CARGO_PKG_VERSION")`，标识 exporter 版本。 |
| `feature_gates[]` | 本次 build 启用的 feature（`serde`、`cursor_state`、`tree_builder_slice_trace` 等），供 QA 复现。 |
| `fixtures[]` | 每个输出 JSON 一条记录，包含：`name`（文件名）、`path`（相对 repo 路径）、`count`（样本数量，chunk 文件为 chunk+line 总数）、`schema_hash`（下表）、`payload_hash`（canonical JSON → SHA256）。 |

- **schema_hash 对应表**：

| Asset | schema_hash | 备注 |
| --- | --- | --- |
| `subset_regression.json` | `serde_fixtures::subset` | Stage A baseline，计数恒为 1。 |
| `delta_regression.json` | `serde_fixtures::delta` | Stage B baseline。 |
| `engine_regression.json` | `serde_fixtures::engine` | Stage C baseline。 |
| `cursor_descriptors.json` | `cursor_descriptors@1.2.0` | 同 `[StageD::ParityAssets]` 版本列。 |
| `chunk_descriptors.json` | `chunk_descriptors@1.0.0` | count = chunk + line。 |
| `grapheme_descriptors.json` | `grapheme_descriptors@1.0.0` | count = descriptor 数。 |
| `tree_builder_slice/*.json` | `tree_builder_slice_trace@1.0.0` | count = 事件条目，记录 `TreeBuilder` push/pop/merge trace。 |

- **哈希规则**：
  1. 读取导出的 JSON，解析为 `serde_json::Value`。
  2. 对象字段按 key 排序，去除所有多余空白（canonical JSON），数组及标量保持原值。
  3. 对 canonical 字符串取 `sha256` 并写入 `payload_hash`（小写十六进制）。
  4. `schema_hash` 永远引用本节表格中的版本字符串，变更时须更新文档与 exporter 常量。

> QA 需在 `scripts/refresh_serialization_fixtures.ps1` 输出中记录 manifest 路径与 `feature_gates[]`，并把 `fixtures.manifest.json` 附在 Stage D MR/PR 里，Goal Tree 的 `[StageD::FixtureFlow]` 会据此同步 hash。

---

## [Fixture-CursorSchema] Cursor Descriptor Schema
<a id="Fixture-CursorSchema"></a>

- **目录**：`tests/xi.Core.Tests/Fixtures/cursor_descriptors/cursor_descriptors.json`
- **导出 flag**：`--cursor-descriptors <output-dir>`（必须启用 `serde`）
- **结构概览**：无 metadata 包装；数组中每个元素为 `CursorDescriptorFixture`。

**CursorDescriptorFixture**

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `name` | string | ✅ | 场景标识（snake_case），与 `[MP-T1]` 样本表保持一致 |
| `text` | string | ✅ | 捕获时的 Rope 文本 |
| `edited_text` | string | ⛔ | 若提供，则用于校验编辑后的失效行为 |
| `expect_apply` | bool | ✅ (默认 true) | 是否期望 descriptor 可在 `text` 上重建 |
| `expect_apply_after_edit` | bool | ⛔ | 指定 `edited_text` 时的期望；缺省回落到 `expect_apply` |
| `notes` | string | ⛔ | 与 `m3-implementation-plan.md` §3.2.4 描述同步 |
| `metric` | enum(`"base"\|"lines"\|"utf16"`) | ✅ | 指示 parity 测试所需的 Metric shim |
| `position` | integer | ✅ | Base Metric 的绝对偏移 |
| `is_valid` | bool | ✅ | 与 `CursorDescriptor::is_valid()` 对齐 |
| `offsets` | [`CursorDescriptorOffsets`](#fixture-cursor-offsets) | ✅ | 叶片位置摘要 |
| `leaf_path` | [`CursorDescriptorFrame`](#fixture-cursor-frame)[] | ✅ | 自叶片向上的祖先帧，供缓存预热 |
| `cursor_state` | [`CursorStateSnapshot`](#fixture-cursor-state) | ⛔ | 默认启用 `cursor_state` gate 后导出，提供 `_editVersion`/路径/Metric 等 parity 数据 |

#### [`CursorDescriptorOffsets`](#) <a id="Fixture-Cursor-Offsets"></a>

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `offset_of_leaf` | integer | ✅ | 持有该游标的叶片 Base 起点 |
| `offset_in_leaf` | integer | ✅ | 相对叶片的偏移（`position - offset_of_leaf`） |
| `leaf_len` | integer | ⛔ | 捕获时叶片长度；若已无缓存可省略 |

#### [`CursorDescriptorFrame`](#) <a id="Fixture-Cursor-Frame"></a>

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `node_height` | integer | 祖先高度 |
| `node_len` | integer | 该节点聚合的 Base 长度 |
| `child_index` | integer | 当前路径所选子节点索引 |
| `child_offset` | integer | 该子节点之前的 Base 偏移 |

#### [`CursorStateSnapshot`](#) <a id="fixture-cursor-state"></a>

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `cursor_state_enabled` | bool | 字段出现即表示启用了 `cursor_state` feature。 |
| `position` | integer | Base Metric 下的绝对位置。 |
| `offset_of_leaf` | integer | 当前叶片在整棵树中的偏移。 |
| `is_valid` | bool | `CursorState::is_valid()` 结果。 |
| `leaf_len` | integer | （可选）叶片长度；若缺失表示缓存中无叶信息。 |
| `path` | [`PathFrameSnapshot`](#fixture-pathframe)[] | 与 chunk/breaks schema 共用的帧结构。 |
| `metric` | enum(`base`
`lines`
`utf16`
`breaks`) | 表示 `NodeCursorState` 捕获时使用的 Metric（`breaks` 用于软换行样本）。 |
| `edit_version` | integer | 捕获 `_editVersion` 值，供 Stage D / QA 对齐。 |
| `edit_version_after_edit` | integer | （可选）`edited_text` 应用后的预期 `_editVersion`。 |
| `invalidated_after_edit` | bool | （可选）应用 `edited_text` 后 descriptor/state 是否应该失效。 |

> Metadata：schema 调整需在 `m3-implementation-plan.md` §1.5 与 `[StageD::ParityAssets]` 同步；`cursor_descriptors@1.2.0` 起新增 `cursor_state` 字段，并要求 manifest 的 `feature_gates[]` 同步记录 `cursor_state`。

---

## [Fixture-ChunkSchema] Chunk & Line Schema
<a id="Fixture-ChunkSchema"></a>

- **目录**：`tests/xi.Core.Tests/Fixtures/chunk_descriptors/chunk_descriptors.json`
- **导出 flag**：`--chunk-descriptors <output-dir>`
- **结构概览**：对象，包含 `metadata`、`chunk_descriptors[]`、`line_descriptors[]`。

**ChunkDescriptorMetadata**

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `schema_version` | string | ✅ | 语义化版本（当前 `1.0.0`） |
| `rust_commit` | string | ⛔ | 生成时的 Git SHA（追溯用途） |
| `generated_at_unix_millis` | integer | ✅ | UTC ms |
| `chunk_descriptor_count` | integer | ✅ | chunk 数量 |
| `line_descriptor_count` | integer | ✅ | line 数量 |

**ChunkDescriptor**

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `sample` | string | ✅ | Rust `chunk_samples()` 中的样本 ID |
| `chunk_index` | integer | ✅ | 样本内序号 |
| `text` | string | ✅ | Chunk 原文 |
| `byte_range` | [`RangeSnapshot`](#fixture-range) | ✅ | Base `[start,end)` |
| `utf16_range` | `RangeSnapshot` | ✅ | UTF-16 偏移，用于 shim 校验 |
| `leaf_range` | `RangeSnapshot` | ✅ | 所属叶片区间 |
| `contains_crlf` | bool | ✅ | 含 `\r\n` 则为 true |
| `is_empty` | bool | ✅ | 空文档/空 chunk 时为 true |
| `tags` | string[] | ✅ | 语义标签（排序 + 去重） |
| `path` | [`PathFrameSnapshot`](#fixture-pathframe)[] | ✅ | 复用游标帧，支撑缓存对拍 |
| `context` | [`ChunkContext`](#fixture-chunkcontext) | ✅ | 16 codepoints 前后文 |

**LineDescriptor**

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `sample` | string | ✅ | 样本 ID |
| `line_index` | integer | ✅ | 行序号（0-based） |
| `raw` | string | ✅ | 包含换行符的原始行 |
| `logical` | string | ✅ | 去除结尾换行后的行 |
| `byte_range` | `RangeSnapshot` | ✅ | Base 偏移 |
| `utf16_range` | `RangeSnapshot` | ✅ | UTF-16 偏移 |
| `newline_kind` | enum(`none|lf|cr|cr_lf`) | ✅ | 归一化换行类型 |
| `tags` | string[] | ✅ | 语义标签 |

**Supporting Types**

| 名称 | 说明 |
| --- | --- |
| `ChunkContext` <a id="Fixture-ChunkContext"></a> | `before`/`after` 各保留 16 codepoints，便于调试 GGJ / CRLF 样本 |
| `RangeSnapshot` <a id="Fixture-Range"></a> | `start`（含）与 `end`（不含）偏移，供 chunk、line、leaf 等结构复用 |
| `PathFrameSnapshot` <a id="Fixture-PathFrame"></a> | 与游标帧一致（见 §[Fixture-CursorSchema]），提供树路径 |

> CLI 示例：`cargo run ... --chunk-descriptors tests/xi.Core.Tests/Fixtures/chunk_descriptors`（可与其他 flags 组合）；所有 ingestion 流程记录在 `[StageD::FixtureFlow]`。

---

## [Fixture-GraphemeSchema] Grapheme Descriptor Schema
<a id="Fixture-GraphemeSchema"></a>

- **目录**：`tests/xi.Core.Tests/Fixtures/grapheme_descriptors/grapheme_descriptors.json`
- **导出 flag**：`--grapheme-descriptors <output-dir>`
- **结构概览**：对象，包含 `metadata` 与 `grapheme_descriptors[]`。

**GraphemeDescriptorMetadata**

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `schema_version` | string | ✅ | 当前 `1.0.0`，改动字段时递增 |
| `rust_commit` | string | ⛔ | 导出时的 Git SHA |
| `generated_at_unix_millis` | integer | ✅ | UTC ms |
| `descriptor_count` | integer | ✅ | Cluster 数量 |

**GraphemeDescriptor**

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `sample` | string | ✅ | `grapheme_samples()` 中的 ID |
| `cluster_index` | integer | ✅ | 0-based cluster 序号 |
| `cluster` | string | ✅ | 完整 grapheme 文本 |

---

## [Fixture-TreeBuilderTrace] Tree Builder Slice Trace Schema
<a id="Fixture-TreeBuilderTrace"></a>

- **目录**：`tests/xi.Core.Tests/Fixtures/tree_builder_slice/`
- **导出 flag**：`--tree-builder-trace <dir>`（需 `--features serde,tree_builder_slice_trace`，`scripts/refresh_serialization_fixtures.ps1 -ExportTreeTrace` 已封装）
- **结构概览**：
  - Manifest 版（默认）：JSON **数组**，每个元素为 Rust `TreeBuilderEvent` 序列化结果。
  - 研究版：对象形式，包含 `metadata`（可选）与 `events[]`，如 `tests/xi.Core.Tests/Fixtures/ParityFixtures/tree_builder_trace/basic_slice_plan.json`。

**TreeBuilderSliceTraceEvent**（数组元素 / `events[]` 项）

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `kind` | 对象 | Serde enum payload，含 `kind`（`"PushFrame"`, `"ExtendFrame"`, `"LeafSlice"`, `"EnterChild"`, `"MergePop"`）以及该 variant 的附加字段。未来会在 Rust 端展开为扁平字段，C# loader 目前会在读取时做展平。 |
| `depth` | integer | 当前栈深度（0 = 根 frame）。 |
| `node_height` | integer | 所指节点高度。 |
| `node_len` | integer | 节点 Base 聚合长度。 |
| `node_id` | integer | 调试用途的唯一 ID。 |
| `reuse` | bool | `TreeBuilder` 是否复用了共享节点。 |
| `interval` | `Range` | （LeafSlice）叶片内区间。 |
| `merged_children` | integer | （MergePop）合并的子节点数量。 |
| `requested` / `translated` | `Range` | （EnterChild）请求区间与实际命中区间。 |

**范围结构**：`Range` = 对象 `{ "start": <int>, "end": <int> }`。

**Metadata（对象模式）**

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `sample` / `sample_name` | string | Trace 名称（默认回退到文件名）。 |
| `rust_commit` | string | 生成时 Git SHA。 |
| `generated_at_unix_millis` | integer | UTC 时间戳。 |

> Schema 目前仍是 Rust `TreeBuilderEvent` 的 serde 直接输出；`TreeBuilderSliceTraceLoader` 会兼容数组/对象两种包装，并在读取时将 `kind` payload 展平成 C# 诊断类型。待 `TreeBuilderTracer` 注入完成后，将按 `[TS-B2]` 的规划收集更长的 slice trace 并在 Stage D manifest 中登记。 
| `byte_range` | `RangeSnapshot` | ✅ | Base 偏移 |
| `utf16_range` | `RangeSnapshot` | ✅ | UTF-16 偏移 |
| `scalar_count` | integer | ✅ | Unicode scalar 数量 |
| `contains_zwj` | bool | ✅ | 是否包含 ZWJ |
| `is_ascii` | bool | ✅ | 纯 ASCII 则 true |
| `crosses_leaf` | bool | ✅ | cluster 是否跨叶片 |
| `requires_fallback` | bool | ✅ | 指示 C# Grapheme 导航是否需 fallback（对应降级策略） |
| `tags` | string[] | ✅ | 语义标签（emoji、cross_leaf 等） |
| `context` | [`GraphemeContext`](#fixture-graphemecontext) | ✅ | 24 codepoints 前后窗 |
| `leaf` | [`LeafSnapshot`](#fixture-leafsnapshot) | ✅ | 叶片区间 + path，方便重放 |

**Supporting Types**

| 名称 | 说明 |
| --- | --- |
| `GraphemeContext` <a id="Fixture-GraphemeContext"></a> | `before`/`after` 字段在 code point 边界截断 |
| `LeafSnapshot` <a id="Fixture-LeafSnapshot"></a> | `range: RangeSnapshot` + `path: PathFrameSnapshot[]`，沿用游标帧语义 |

> CLI 示例：`cargo run ... --grapheme-descriptors tests/xi.Core.Tests/Fixtures/grapheme_descriptors`；遥测阈值记录在 `design-divergence-log.md` 与 `[QA-ChunkBench]`。

---

## [Fixture-BreaksSchema] Breaks Descriptor Schema
<a id="Fixture-BreaksSchema"></a>

- **目录**：`tests/xi.Core.Tests/Fixtures/breaks_descriptors/`
- **导出 flag**：`--breaks-descriptors <dir>`（默认文件 `breaks_descriptors.json`）
- **来源**：`BreaksLeaf/BreaksInfo/BreakBuilder`（`rope/src/breaks.rs`）以及 `metrics::break_indices` helper，字段涵盖软换行基准、叶片分布与 `BreaksMetricHelper` 需要的计数。

**Metadata**

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `schema_version` | string | 当前 `1.0.0`，随字段调整递增 |
| `rust_commit` | string | `xi-editor-ph7` 提交 SHA |
| `generated_at_unix_millis` | integer | UTC 毫秒时间戳 |
| `descriptor_count` | integer | `break_sets[]` 数量 |

**BreakSetDescriptor**

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `sample` | string | ✅ | 样本 ID，继承 `breaks_samples()` 约定 |
| `rope_len` | integer | ✅ | Rope 长度（BaseMetric 单位，字节） |
| `wrap_width_units` | integer | ✅ | 软换行宽度（BaseMetric 单位） |
| `metric` | enum(`"BreaksMetric"`) | ✅ | 标识采样所用 metric，便于未来扩展 |
| `break_offsets` | integer[] | ✅ | 软换行断点，按 BaseMetric 偏移升序排列 |
| `break_count` | integer | ✅ | `break_offsets` 长度的重复字段，方便 manifest 统计 |
| `leaf_runs` | [`LeafRunSnapshot`](#fixture-breaks-leafrun)[] | ✅ | 每个叶片的 Base 区间 + break 数量 |
| `text_excerpt` | string | ⛔ | 可选，截取前 160 codepoints 便于调试 |
| `tags` | string[] | ⛔ | 语义标签（如 `emoji`, `wide`, `crlf`） |

**LeafRunSnapshot** <a id="fixture-breaks-leafrun"></a>

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `range` | [`RangeSnapshot`](#Fixture-Range) | 叶片 Base 范围 |
| `break_count` | integer | 该叶片内的断点数量 |
| `path` | [`PathFrameSnapshot`](#Fixture-PathFrame)[] | 指向叶片的路径，用于 C# loader 预热缓存 |

```jsonc
{
  "metadata": {
    "schema_version": "1.0.0",
    "descriptor_count": 1
  },
  "break_sets": [
    {
      "sample": "soft_wrap_utf8",
      "rope_len": 312,
      "wrap_width_units": 80,
      "metric": "BreaksMetric",
      "break_offsets": [80, 160, 240, 312],
      "break_count": 4,
      "leaf_runs": [
        { "range": { "start": 0, "end": 156 }, "break_count": 2 },
        { "range": { "start": 156, "end": 312 }, "break_count": 2 }
      ],
      "tags": ["emoji", "mixed"]
    }
  ]
}
```

> 兼容性：字段与 `BreaksMetricHelper` 输入结构一一对应，可直接驱动 C# soft-break 验证；若未来需要记录 `BreaksBaseMetric` 结果，可在相同 schema 中追加布尔或数值字段并 bump `schema_version`。

---

## [Fixture-DiffSchema] Diff Regions Schema
<a id="Fixture-DiffSchema"></a>

- **目录**：`tests/xi.Core.Tests/Fixtures/diff_regions/`
- **导出 flag**：`--diff-regions <dir>`（默认文件 `diff_regions.json`）
- **来源**：`Diff` trait（`rope/src/diff.rs`）、`LineHashDiff`、`DiffBuilder`/`DiffOp` + `DeltaElement::Insert`，用于在 C# 端重放 diff。

**Metadata**

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `schema_version` | string | 当前 `1.0.0` |
| `rust_commit` | string | `xi-editor-ph7` 提交 SHA |
| `generated_at_unix_millis` | integer | UTC 时间戳 |
| `case_count` | integer | `diff_cases[]` 计数 |

**DiffCase**

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `sample` | string | ✅ | 样本键，例如 `rope_engine_vs_corelib` |
| `base_path` / `target_path` | string | ✅ | 供 QA 查找文本（相对仓库路径） |
| `base_sha` / `target_sha` | string | ⛔ | 可选 git blob 哈希 |
| `line_count` | integer | ⛔ | `target` 行数，用于 sanity check |
| `ops` | [`DiffOpSnapshot`](#fixture-diff-op)[] | ✅ | `DiffBuilder` 输出的序列，含 copy/insert/delete |
| `stats` | object | ⛔ | `{ "copied_bytes": <int>, "inserted_bytes": <int>, "deleted_bytes": <int> }` |
| `notes` | string | ⛔ | 解释样本的 Diff 目的 |

**DiffOpSnapshot** <a id="fixture-diff-op"></a>

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `kind` | enum(`copy`,`insert`,`delete`) | 区分 `DiffOp` 语义 |
| `base_range` | [`RangeSnapshot`](#Fixture-Range) | `copy/delete` 时必填（BaseMetric 单位） |
| `target_range` | `RangeSnapshot` | `copy/insert` 时必填 |
| `byte_len` | integer | 操作涉及的字节数（便于断言） |
| `line_span` | object | `{ "base": [start,end], "target": [start,end] }` 行号区间 |
| `insert_preview` | string | `insert` 时可选的 80 codepoint 预览 |

```jsonc
{
  "metadata": { "schema_version": "1.0.0", "case_count": 1 },
  "diff_cases": [
    {
      "sample": "engine_spellcheck",
      "base_path": "fixtures/diff/base.txt",
      "target_path": "fixtures/diff/target.txt",
      "ops": [
        { "kind": "copy", "base_range": { "start": 0, "end": 1024 }, "target_range": { "start": 0, "end": 1024 }, "byte_len": 1024 },
        { "kind": "delete", "base_range": { "start": 1024, "end": 1150 }, "byte_len": 126 },
        { "kind": "insert", "target_range": { "start": 1024, "end": 1180 }, "byte_len": 156, "insert_preview": "use xi_editor::diff" }
      ],
      "stats": { "copied_bytes": 1024, "inserted_bytes": 156, "deleted_bytes": 126 }
    }
  ]
}
```

> 兼容性：C# 端 `LineHashDiff` 复刻可直接迭代 `ops[]` 重建 `RopeDelta`。新增字段（例如行对齐信息）时需 bump `schema_version` 并同步 Stage D 文档。

---

## [Fixture-SearchSchema] Search Span Schema
<a id="Fixture-SearchSchema"></a>

- **目录**：`tests/xi.Core.Tests/Fixtures/search_spans/`
- **导出 flag**：`--search-spans <dir>`（默认文件 `search_spans.json`）
- **来源**：`find.rs`（`find`, `find_progress`, `CaseMatching`, `FindResult`）与 `spans.rs`（`Spans<T>`, `SpansBuilder`）。

**Metadata**

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `schema_version` | string | 当前 `1.0.0` |
| `rust_commit` | string | `xi-editor-ph7` 提交 SHA |
| `generated_at_unix_millis` | integer | UTC 时间戳 |
| `case_count` | integer | `search_cases[]` 数量 |

**SearchCase**

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `sample` | string | ✅ | 案例 ID（如 `regex_multiline_block`） |
| `query` | string | ✅ | 搜索字符串或正则 |
| `is_regex` | bool | ✅ | 为 true 时 `regex_options` 描述 flags |
| `regex_options` | string | ⛔ | 例如 `"multi_line|case_insensitive"` |
| `case_matching` | enum(`exact`,`case_insensitive`) | ✅ | 映射 `CaseMatching` |
| `text_len` | integer | ✅ | 目标文本 BaseMetric 长度 |
| `hits` | [`SearchHit`](#fixture-search-hit)[] | ✅ | 命中列表 |
| `span_windows` | [`SpanSegment`](#fixture-span-segment)[] | ⛔ | `Spans<T>` 导出的样式窗口 |
| `notes` | string | ⛔ | 调试说明 |

**SearchHit** <a id="fixture-search-hit"></a>

| 字段 | 类型 | 说明 |
| --- | --- |
| `index` | integer | 命中序号（0-based） |
| `range` | [`RangeSnapshot`](#Fixture-Range) | BaseMetric 范围 |
| `line` | integer | 所在逻辑行（0-based） |
| `context_before` / `context_after` | string | 采用 40 codepoint 滑窗，用于验证 UI 高亮 |

**SpanSegment** <a id="fixture-span-segment"></a>

| 字段 | 类型 | 说明 |
| --- | --- |
| `range` | `RangeSnapshot` | 覆盖范围 |
| `style_id` | integer | `Spans<T>` 中的样式键 |
| `style_tag` | string | 语义标签，如 `search-match`、`selection` |
| `priority` | integer | 排序优先级，重现 `SpansBuilder` 行为 |

```jsonc
{
  "metadata": { "schema_version": "1.0.0", "case_count": 1 },
  "search_cases": [
    {
      "sample": "regex_word_boundary",
      "query": "\\brope\\b",
      "is_regex": true,
      "case_matching": "exact",
      "text_len": 2048,
      "hits": [
        { "index": 0, "range": { "start": 128, "end": 132 }, "line": 4, "context_before": "the ", "context_after": " state" }
      ],
      "span_windows": [
        { "range": { "start": 120, "end": 140 }, "style_id": 7, "style_tag": "search-match", "priority": 10 }
      ]
    }
  ]
}
```

> 兼容性：`SearchHit` 内容映射至 C# `Finder`/`Spans` DTO，可驱动 UI 高亮与 `StageDDescriptorLoader` smoke。若导出 regex trace 需要额外状态，可在 `span_windows` 中追加字段并 bump 版本。

---

## [Fixture-Validation] 验证流程
<a id="Fixture-Validation"></a>
1. 启用正确的 feature 组合（`serde` + `cursor_state` 必选；`tree_builder_slice_trace` 仅在需要 slice trace 时追加）。
2. 使用上方“一键命令”或 `scripts/refresh_serialization_fixtures.ps1` 执行全量导出；Stage D 手册在 `[StageD::FixtureFlow]` 记录了 Windows/Linux 两种脚本路径。
3. 导出后运行 `./run_all_checks`（Rust）与 `dotnet test Xi.Editor.sln`（C#）确认回归状态，并把差异写入 `rope-serialization-fixture-playbook.md`。
4. Schema 发生更改时，同步更新此文档、`rope-port-mapping.md`、`m3-implementation-plan.md`（T0 依赖）以及 QA 手册，确保 `[QA-IngestionSmoke]` 能阻止漂移。

---

## [Fixture-ChangeLog] 变更记录
<a id="Fixture-ChangeLog"></a>

| 日期 | 版本 | 作者 | 摘要 |
| --- | --- | --- | --- |
| 2025-11-19 | v1.1 | Architecture Mapper | 套用统一模板：新增 front-matter、anchor、Stage D 引用与 change log；各 schema 摘要化并保留详细字段表；Stage D anchor 链接精确化。 |
| 2025-11-17 | v1.0 | Rust Porter | 初版 schema（cursor/chunk/grapheme）+ feature gate 说明，用于 Stage D 评审。 |

---

[StageD::ParityAssets]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::ParityAssets
[StageD::FixtureFlow]: ../csharp-refactor/rope-serialization-fixture-playbook.md#StageD::FixtureFlow
[QA-IngestionSmoke]: ../csharp-refactor/rope-serialization-fixture-playbook.md#QA-IngestionSmoke
