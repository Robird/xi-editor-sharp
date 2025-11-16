# Parity Fixture Schema for `export-serde-fixtures`

> **Scope**: Track the JSON schemas consumed by Stage D parity assets (`cursor`, `chunk/line`, `grapheme`).
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
| `cursor_state` | ❌ | 在 `CursorDescriptor` 导出期间捕获更丰富的调试快照，字段保持兼容 | 仅在调试游标失效时开启 |
| `tree_builder_slice_trace` | ❌ | 启用 `--tree-builder-trace`（与 parity 资产共用 exporter，可并行生成 slice trace） | 与 parity schema 解耦，仅记录在 Stage D 附录 |

**组合命令（刷新全部 parity 资产）**

```powershell
cargo run -p xi-rope --features serde --bin export-serde-fixtures -- `
  --dir tests/xi.Core.Tests/Fixtures `
  --cursor-descriptors tests/xi.Core.Tests/Fixtures/cursor_descriptors `
  --chunk-descriptors tests/xi.Core.Tests/Fixtures/chunk_descriptors `
  --grapheme-descriptors tests/xi.Core.Tests/Fixtures/grapheme_descriptors
```

> 调试场景：追加 `--features serde,cursor_state` 或 `--features serde,tree_builder_slice_trace --tree-builder-trace <dir>`，并在 `[StageD::FixtureFlow]` 记录用途。

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

> Metadata：schema 调整需在 `m3-implementation-plan.md` §1.5 与 `[StageD::ParityAssets]` 同步；`cursor_state` gate 不改变字段，仅扩充调试 payload。

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

## [Fixture-Validation] 验证流程
<a id="Fixture-Validation"></a>
1. 启用正确的 feature 组合（`serde` 必选；`cursor_state`、`tree_builder_slice_trace` 仅在诊断场景使用）。
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
