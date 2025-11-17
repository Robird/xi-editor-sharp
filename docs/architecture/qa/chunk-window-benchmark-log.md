# Chunk & Grapheme Benchmark Log (Draft)

> 建立自 2025-11-18 设计脑暴会，用于记录每次 ChunkWindow/Grapheme diagnostics 基准测试结果及其来源（BenchmarkDotNet + Rust 比对），支撑 Catalog T3/F2 与 R9/R10 质量门槛。

## 记录模板
| 日期 | 分支/提交 | Payload | Benchmark 命令 | 平均耗时 (ms) | Chunk/Grapheme 指标 | Rust 对照 | 备注 |
|------|-----------|---------|----------------|----------------|--------------------|----------|------|
| YYYY-MM-DD | `mini-blocking-model@abc123` | 1MB Cursor JSON | `dotnet run --project tests/xi.Core.Tests/Benchmarks/Diagnostics/ChunkWindowBenchmarks.csproj -c Release --filter ChunkWindow-1MB` | TBD | ChunkCount=??, MaxChunk=?, UTF16CopyBytes=? | Rust chunk_window=? ms | 初版记录 |

## 数据采集规范
1. **环境**：Windows 11 / 32GB RAM（或更高），CPU 型号、频率写入备注；需要禁用节能模式，固定性能计划。
2. **输入**：使用 Rust Porter 提供的 `chunk_descriptors.json`、`grapheme_descriptors.json` 以及暂无 32MB payload 时需注明模拟方式。
3. **命令**：统一使用 BenchmarkDotNet 生成的 `BenchmarkDotNet.Artifacts/results/*.md`，并将关键指标粘贴到本表，同时保存 artifacts 以备审计。
4. **对照**：Rust 端需运行对应 `cargo bench chunk_window` / `cargo bench grapheme_window`，提供平均耗时与 commit。
5. **阈值**：ChunkWindow 延迟需 ≤ Rust * 1.3；Grapheme fallback 率 ≤ 0.5%（待主持人确认后写入 `docs/architecture/design-divergence-log.md`）。

## 待办
- [ ] QA：填入首批 1MB Chunk/Grapheme 数据（计划 2025-11-19 晚）。
- [ ] C# Implementer：在 Benchmark 项目输出 `metrics.json`，方便自动导入本日志。
- [ ] Rust Porter：附上 `chunk_window`/`grapheme_window` Criterion 结果与 CLI 导出的 telemetry。
- [ ] Architecture Mapper：在 `docs/architecture/m3-implementation-plan.md §5.3` 引用本日志。

---
*添加新记录时，请更新本文件并在变更日志注明。*
