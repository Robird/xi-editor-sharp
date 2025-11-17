# Tree Builder Trace Schema (Draft)

> 起草自 2025-11-18 设计脑暴会，目标是在 11-20 前锁定 `tree_builder_slice_trace`/`export-tree-builder-trace` 输出的稳定结构，为 SourceGen/Schema 校验与 QA 验证提供契约。当前为草案，待 Rust Porter/Architecture Mapper/QA/C# 协作敲定。

## 目标
1. 为 `TreeBuilderTrace::to_json_string()` 与 CLI `export-tree-builder-trace` 提供版本化 schema。
2. 支持 Catalog T1/T2/S3/S4 与 R9 风险管控：任何字段漂移必须通过 schema_version bump + 文档更新。
3. 便于 C# Source Generator (`TreeTraceSchemaKit`) 与 QA Schema 校验脚本自动验证。

## 顶层结构（建议）
```json
{
  "schema_version": "1.0.0",
  "rust_commit": "<sha>",
  "tree_depth": 4,
  "leaf_span_policy": "ReadOnlySpan<char>",
  "nodes": [ ... ],
  "events": [ ... ]
}
```

| 字段 | 类型 | 说明 | 备注 |
|------|------|------|------|
| `schema_version` | string | 语义化版本（x.y.z）。每次新增/修改字段需 bump。 | 由 Rust CLI 写入，C#/QA 校验必需 |
| `rust_commit` | string | 导出 CLI 构建所基于的 Rust git SHA | 供 parity fixture 跟踪 |
| `tree_depth` | number | 导出时 Rope/TreeBuilder 的最大深度 | 方便 Span/Stackalloc 策略选择 |
| `leaf_span_policy` | string | 指明叶片序列的测量单位（例如 `Utf16`/`Utf8`） | C# `CursorEditSession` 可据此决定 Span 长度 |
| `nodes` | array | （可选）树结构摘要，如节点 ID、子节点计数、byte offset | 便于 C# 验证 SharedNode/Span 行为 |
| `events` | array | 事件流，包含 push/pop/merge 等 detail | QA/SourceGen 校验重点 |

### `nodes[]` 草案
```json
{
  "id": "n7",
  "level": 2,
  "child_count": 3,
  "byte_span": [0, 512],
  "utf16_span": [0, 480]
}
```

### `events[]` 草案
```json
{
  "tick": 15,
  "kind": "PushLeaf",
  "node_id": "n12",
  "byte_len": 256,
  "utf16_len": 240,
  "path_frames": [0,1,2]
}
```

## 待确认清单
- [ ] Rust Porter：确认 exporter 能在无需额外 roundtrip 的情况下获取 `tree_depth` 与 Span 指标。
- [ ] Architecture Mapper：与 Information Researcher 共同梳理既有 trace 字段，补充文档的字段定义与示例。
- [ ] C# Implementer：评估 `TreeTraceSchemaKit` Source Generator 的输入文件命名、包含路径。
- [ ] QA Engineer：定义 Schema 校验脚本（`Invoke-TreeTraceSchemaGuard.ps1`）与 CI 钩子。
- [ ] 所有角色：会签最终 schema 后，将 `schema_version=1.0.0` 记入 `docs/architecture/porting-issues-catalog.md` 与 `scripts/refresh_serialization_fixtures.ps1`。

## 时间线
| 截止 | 项 | Owner |
|------|----|-------|
| 2025-11-19 20:00 | CLI 输出新字段 & 示例 JSON | Rust Porter |
| 2025-11-20 12:00 | 文档定稿 + SourceGen/Schema 校验实现 | Architecture Mapper + C# Implementer + QA |
| 2025-11-20 18:00 | QA 在 运行 schema guard + 发布结果 | QA Engineer |

---
*本文件为草案，更新时请在变更日志注明日期与主要修改。*
