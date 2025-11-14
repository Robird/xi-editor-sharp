# Rope 序列化重构对映计划（C# 镜像）

*最后更新：2025-11-14*

## 1. 背景与目标
- Rust 端已完成 `Subset`、`Delta`、`Engine` 四阶段 serde 拆分，`xi-rope` 可在 `--no-default-features` 与 `--features serde` 双轨下稳定构建并通过测试。
- 文档 `docs/rust-refactor/delta-subset-serialization.md` 已记录黄金 JSON 基线与辅助 API 名称，`engine_serialization_regression` 等回归测试提供跨语言对照。
- 现阶段重点转向 **C# 端镜像实现**，同步落地与 Rust 端等价的 Helper、序列化/反序列化逻辑及测试资产，为后续高阶重构（游标、SIMD）提供稳定基线。
- 保留 Rust 侧 `serde` 可选特性，继续作为 C# 行为对照与黄金样本生成工具。

## 2. 已完成成果梳理
| 模块 | Rust 现状 | 验证手段 |
| --- | --- | --- |
| Subset | `Subset::segment_triples()` / `from_segment_triples()` / `segment_count()` 等 `pub(crate)` helper；`subset_serialization_regression` 锁定 JSON | `cargo test -p xi-rope --features serde subset_serialization_regression` |
| Delta | `Delta::base_len()` / `iter_elements()` / `element_triples()`；`serde_impls.rs` 手写 serde；`delta_serialization_regression` | 同上 |
| Engine | `Engine::revision_log()` / `Engine::from_serialized_state()` / `RevisionRef` 等 helper；`engine::serde_impl` 手写 serde；`engine_serialization_regression` | 同上 |
| 工具链 | `run_all_checks` 新增两条 xi-rope 双轨测试命令；`Cargo.toml` 明确 `serde` 特性显式开启 | 手动/CI |

## 3. C# 镜像落地总览
### 3.1 阶段划分
1. **Stage A – Subset 对映与回归**（已完成，2025-11-14 Session A）
   - 新建 `Subset`/`SubsetBuilder` C# 实现，围绕 `segment_triples`、`from_segment_triples` 接口构建。
   - 导入 `subset_serialization_regression` JSON 作为黄金样本，编写 xUnit 回归测试（序列化与反序列化）。
   - 复用现有 `StringLeafOperations` 与不变量检查，确保 `Subset` 在编辑路径中行为一致。

2. **Stage B – Delta 对映**（已完成，本次会话）
   - 引入 `Delta<TInfo, TLeaf>`、`DeltaElement`、`CopyElement`、`InsertElement` 与 `EnumerateElementTriples()`，对齐 Rust `Delta::base_len()`/`iter_elements()`/`element_triples()` helper。
   - 迁移 `delta_serialization_regression` 黄金 JSON，补充序列化/反序列化验证与枚举回归测试，保持输出与 Rust 一致。
   - 在 `delta.Factor()` 预留 stub（抛出 `NotImplementedException`），为后续 Stage C/D 组合操作做好接口准备，同时维持现有 81 项 + 新增回归测试全绿。

3. **Stage C – Engine 镜像**（已完成，2025-11-14 Session C）
   - 引入不可变 `Engine`/`Revision`/`RevisionOperation`（含 `RevisionEdit`、`RevisionUndo`）类型，对齐 Rust `revision_log()`/`from_serialized_state()` helper surface。
   - 新增 `EngineJson`（`System.Text.Json`）序列化器，复用 `SubsetJson` 语义还原 `engine_serialization_regression` 黄金串，并在反序列化阶段补充字段合法性校验。
   - 添加 `engine_regression.json` fixture 与 `EngineSerializationTests`（构造→序列化、反序列化回写、`RevisionLog` 验证三项），`dotnet test` 全量通过。

4. **Stage D – 共享资产同步**（进行中，2025-11-14 Session D）
    - 交付项：
       - 建立并记录黄金 fixture 维护流程（刷新命令、审核步骤、回归验证）。
       - 定义 Rust `run_all_checks` serde/无 serde 双轨与 `dotnet test` 组合的 CI 集成策略。
       - 明确文档同步节奏与责任矩阵，涵盖 `rope-port-mapping.md`、`rope-cs-mirror-plan.md`、`AGENTS.md` 与新建 `rope-serialization-fixture-playbook.md`。
    - 立即行动：
       - 发布 fixture 刷新作业手册并链接至相关文档。
       - 规划 CI 作业节点（Rust 双轨、.NET 测试）与本地验证顺序，形成执行 checklist。
       - 标注 Stage D 文档同步期望，确保每次刷新/发布均触发文档更新。
    - 验收标准：
       - 任意协作者可依据手册在 Windows PowerShell 下刷新 fixture，并通过 `dotnet test`/`run_all_checks` 验证。
       - CI 或本地自动化脚本具备串连 Rust 双轨测试与 .NET 回归的方案说明并可落地执行。
       - `AGENTS.md`、`rope-port-mapping.md` 与 Stage 计划文档在刷新后均同步记录状态与输出。

### 3.2 里程碑验收标准
- 每个 Stage 完成后：
  1. 所有 `xi.Core.Tests` 现有测试 + 新增回归测试通过。
  2. Rust 黄金测试仍然通过（`run_all_checks` 双轨）。
  3. 文档（`rope-port-mapping.md`、本计划、`AGENTS.md`）同步更新。

## 4. 近期排程
> 当前仅有“人类开发者 + AI Coder”协作，执行节奏按 **AI 会话** 划分。每次会话需完成表格所列交付，并在收尾前更新本计划、`AGENTS.md` 与相关回归结果。

| 会话序号 | 目标内容 | 主要输出 | 入场条件 |
| --- | --- | --- | --- |
| Session A | Stage A (Subset) 实装与测试 | C# `Subset`/`SubsetBuilder` + JSON 回归测试 + 文档同步（已完成，2025-11-14） | Rust helper 已就绪（当前状态） |
| Session B | Stage B (Delta) 实装与测试 | C# `Delta` 泛型骨架 + `delta` 黄金回归 + 81 项测试保持通过 | Session A 完成 → **已完成（2025-11-14）** |
| Session C | Stage C (Engine) 实装与测试 | **已完成（2025-11-14 Session C）**：C# `Engine`/`Revision`/`RevisionOperation`、`EngineJson` 回归套件、撤销/重做 helper 验证 | Session B 完成 |
| Session D (滚动) | Stage D 文档&资产同步、CI 接入 | 文档更新、`run_all_checks` 双轨 + `dotnet test` 集成策略、fixture 刷新流程手册（进行中） | Sessions A-C 交付稳定 |

## 5. Rust 侧后续待办（与 C# 并行监控）
- **CI 补强**：将 `run_all_checks` 纳入自动流水线，确保 serde 双轨测试自动执行。
- **iterator-facade-export**：待 C# 镜像完成后，再评估迭代器门面导出需求，结合 C# 反馈优化接口。
- **cursor-lifetime-refactor**：高风险任务，须在 C# 有足够回归资产后再启动。
- **SIMD optionalization**：暂不动，优先保持稳定基线；如未来确有性能需求，可再评估。

## 6. 风险与缓解
| 风险 | 描述 | 缓解策略 |
| --- | --- | --- |
| 黄金串漂移 | C# 序列化实现若与 Rust 结果不一致，难以定位问题 | 按 Stage 引入回归测试，必要时通过脚本自动生成 Rust 对照 |
| CI 漏跑 | 未将“双轨测试”纳入流水线可能导致回归漏检 | 尽快在 CI Pipeline 中调用 `run_all_checks` 或显式添加两条命令 |
| 文档脱节 | helper 演进但映射文档未更新 | Stage 结束时检查 `rope-port-mapping.md`、`AGENTS.md`、本计划 |
| 泛型差异 | C# 泛型结构与 Rust 对不齐造成 API 震荡 | 在 Stage A/B 中优先落地 `ILeafOperations` 对映，确保两侧契约一致 |

## 7. 额外提醒
- 继续保留 Rust 侧 `serde` 开关，作为 C# 回归参照与黄金数据源。
- 镜像过程中若发现缺少 helper，优先在 Rust 端补小型改动（避免再开大规模重构）。
- 复用现有脚本 `scripts/refresh_skeleton_docs.py` 保持骨架文档最新，防止 C# 实现偏离。

---
如需调整计划，请在 Stage 里程碑验收后更新本文件，并同步 `AGENTS.md` 与相关架构文档。