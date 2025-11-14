# Rope Generic Simplification (Plan C)

_草案建立：2025-11-14_

## 1. 背景与问题复盘
- 现有 Plan A 试图改写 `NodeInfo`/`Leaf`/`Metric` 等基石 trait，风险与回归面巨大；Plan B 引入 `RopeNode` 等 newtype wrapper，会牵一发动全身，既要重写 `Add`/`Display` 等实现，也无法回避 `TreeBuilder`、`Cursor` 的泛型约束。
- 进一步调查表明：`Rope`/`Breaks`/`Spans` 各模块已经以 `type` 别名形式公开混合结构，主要痛点来自**对外 API 缺乏分层**，调用方直接面对 `tree` 层的泛型海洋。
- 因此，本方案聚焦于“接口分层 + helper 收敛”，不改变数据布局，通过**扩展 trait + prelude 门面**把常用操作抽出，既降低学习成本，也为 C# 迁移提供可对照的 API 子集。

## 2. 目标与收益
1. **保持数据与泛型骨架不动**：`Node<N, L>`、`TreeBuilder<N, L>`、`Cursor<'a, N, L>` 继续作为内部事实来源，兼容已有测试与 serde 镜像。
2. **导出精简门面**：为 `Rope`/`Breaks`/`Spans`/`Delta` 等提供 `*-Ops` 扩展 trait 与 `prelude` 模块，凸显惯用 API；外部用户默认只需导入 `use rope::prelude::*` 即可操作常见场景。
3. **集中 helper**：将 string 叶片处理、度量统计等杂散自由函数迁移到专门模块，实现跨语言共享的稳定函数集合。
4. **文档与自动化同步**：`docs/skeleton/*.md` 与 `port-blueprint.md` 始终映射新层次，脚本与回归测试流程不变。

## 3. 设计准则
- **零数据重排**：所有新 API 都以 `impl Trait for Node<ConcreteInfo, ConcreteLeaf>` 形式实现，避免触碰 `Node` 的内部结构与 serde 序列化输出。
- **正交分层**：划分为 “Core 泛型层（tree.rs 等）→ Concrete 类型别名层（rope.rs 等）→ Prelude/Ops 层（新模块）”。每一层只向上提供必要接口。
- **渐进式落地**：每个阶段可独立合并，均以 `cargo test -p xi-rope` 与 serde 回归为验收；失败可单独回滚。
- **测试驱动收敛**：helper 模块迁移时同步转移或新增针对性单元测试，保持分层后覆盖率不倒退。

## 4. 模块拓扑提案
```
rope/
├── tree.rs            # 维持泛型核心
├── rope.rs            # Concrete 类型定义与 Leaf impl
├── helpers/
│   ├── leaf_ops.rs    # 字符串叶片拆分/合并/容量约束
│   └── metrics.rs     # Rope 特有的统计纯函数
├── prelude.rs         # re-export Rope / RopeInfo / RopeOps / helper 别名
└── ops/
    ├── rope_ops.rs    # impl RopeOps for Rope
    ├── breaks_ops.rs  # impl BreaksOps for Breaks
    ├── spans_ops.rs   # impl SpansOps for Spans
    └── delta_ops.rs   # impl RopeDeltaOps / TransformerOps
```
> `helpers/` 与 `ops/` 均为 Rust-only 组织方案；最终命名可随实现微调。

## 5. 分阶段实施计划
### Phase 1 — Prelude 与 RopeOps 基线
- 新增 `rope::prelude`：导出 `Rope`, `RopeInfo`, `RopeOps`, 常用 metric 类型与 builder/游标别名。
- `rope::ops::rope_ops`：把 `rope.rs` 中高频且对外暴露的行为（`len`、`is_empty`、`slice`、`edit`、`lines`、`iter_chunks`、`line_of_offset`、`offset_of_line` 等）迁入 `pub trait RopeOps`，通过 `impl RopeOps for Node<RopeInfo, String>` 实现。
- `tree.rs` 仍保留原方法（以 `pub(crate)` 或内部调用为主），`rope.rs` 负责 `pub use crate::rope::ops::RopeOps`，避免 break change。
- 测试更新：`rope/src/tests` 与 `rope/src/engine.rs` 等使用 `use crate::rope::prelude::*` 验证门面可用。

### Phase 2 — 叶片与度量 helper 整理
- 将 `find_leaf_split_for_merge`、`count_newlines`、`count_utf16_code_units` 等函数迁入 `helpers` 子模块，`impl Leaf for String` 改为调用 helper。
- 为 helper 增加独立单元测试（`#[cfg(test)] mod tests`）覆盖拆分、代理对、UTF-16 统计等场景。
- `metrics/mod.rs` 保留现有模块拆分，新增 `helpers::metrics` 以面向 RopeOps trait 暴露纯函数（如 `fn lines_in_range(rope: &Rope, range)`）。

### Phase 3 — Delta / Transformer 门面化
- 定义 `pub trait RopeDeltaOps`，封装 `Delta<RopeInfo, String>` 的常用入口：`base_len`、`iter_elements`、`apply_to_rope`、`factor`、`is_simple_delete` 等。
- 定义 `pub trait TransformerOps<'a>` 针对 `Transformer<'a, RopeInfo, String>`，提供 `transform_rope_offset`、`interval_untouched` 等易用方法。
- `serde_impls.rs`、`engine.rs` 等改用 trait 调用；保持内部逻辑不变。
- 追加覆盖测试：保证 trait 方法等价于原泛型函数。

### Phase 4 — 文档、脚本与发布清单
- 运行 `python scripts/refresh_skeleton_docs.py`，同步 `docs/skeleton/rope.md` 与 `xi.Core.Rope.cs`。
- 更新 `docs/architecture/rope-port-mapping.md` 与 `docs/csharp-refactor/rope-delta-notes.md`，映射新的 ops/helper 模块。
- `run_all_checks` 与 `scripts/refresh_serialization_fixtures.ps1` 流程不变；记录在 `AGENTS.md` 的工作日志与风险板块。

## 6. 核心交付件
| 交付物 | 描述 | 验收标准 |
|---|---|---|
| Prelude/ops 模块 | `rope::prelude`、`RopeOps` trait 与实现 | `cargo test -p xi-rope` & serde 回归通过；外部模块仅导入 prelude 即可完成基本操作 |
| Helpers 重构 | `helpers::leaf_ops`, `helpers::metrics` 单元测试覆盖 | Rope 编辑/度量相关测试全部通过； helper 测试 >95% 分支覆盖目标 |
| Delta/Transformer Ops | `RopeDeltaOps`, `TransformerOps` trait Plus 文档 | 引擎、subset/delta serde 回归无 diff；新增 trait API 文档与示例 |
| 文档同步 | Plan C 文档、蓝图、skeleton 刷新 | `AGENTS.md` 记录阶段完成；`docs/architecture` 对照表更新 |

## 7. 验证流程
1. `cargo test -p xi-rope`（含默认/serde feature 双轨）。
2. `cargo test -p xi-rope --features serde subset_serialization_regression delta_serialization_regression engine_serialization_regression`。
3. `scripts/refresh_serialization_fixtures.ps1 -SkipRust -SkipDotnet`（确认无意外变化）。
4. 若必要，追加 `dotnet test tests/xi.Core.Tests` 以对照黄金串。

## 8. 风险与缓解
| 风险 | 影响 | 缓解 |
|---|---|---|
| Trait 方法与原 API 语义不一致 | 行为回归、serde diff | 迁移时保持 trait 内部直接调用原函数；在测试中做等价断言 |
| Prelude 引发命名冲突 | 影响外部调用 | prelude 采用最小可行导出集合，并提供显式模块路径作为后备 |
| helper 拆分导致覆盖不足 | 难以及时发现边界 bug | helper 模块建立细粒度测试，必要时在 `RopeOps` 中留守备 `#[cfg(test)]` 断言 |
| 文档不同步 | C#/Rust 协作失真 | 每阶段结束列为 DoD，提交前运行 skeleton 脚本并更新蓝图 |

## 9. 后续展望
- Prelude 层可继续扩展到 `TreeBuilder`（如 `RopeBuilder`）与 `Cursor`（`RopeCursor`）的特化别名，进一步降低泛型噪声。
- 分层结构为未来潜在的 newtype 或 FFI 封装打下基础：若后续性能评估显示需要引入 wrapper，可在 ops/trait 完善后平滑迁移。
- 可将 helper 模块整理成文档化 API 列表，供 C# 侧直接翻译或参考，实现 Rust/C# 双向 helper 对齐。

## 10. TODO 清单（滚动维护）
- [ ] Phase 1：定义 `rope::prelude` 与 `RopeOps`，迁移核心方法与测试引用。
- [ ] Phase 2：落地 `helpers::leaf_ops`/`helpers::metrics`，补充单元测试。
- [ ] Phase 3：实现 `RopeDeltaOps` 与 `TransformerOps`，更新 serde/engine 调用。
- [ ] Phase 4：刷新 skeleton 与蓝图、记录 AGENTS 日志。
- [ ] 评估是否需要为 `Breaks`/`Spans` 衍生对等 prelude 与 ops trait，并安排跟进会话。

---
_备注：本文档为 Plan C 初稿，后续阶段完成后需更新“TODO 清单”与“背景”部分，确保历史沿革清晰可追踪。_

**Feasibility Review**
- **Major – RopeOps trait overlaps with Node**: `Node<RopeInfo, String>` already exposes `len`, `is_empty`, `subseq`, etc.; moving these into an extension trait adds no real API simplification but forces every caller to import that trait, creating churn without payoff. The plan should focus the trait on Rope-specific conveniences (metrics, grapheme helpers) instead of re-wrapping `Node` core.
- **Major – Iterator return types need re-homing**: methods like `iter_chunks`, `lines_raw`, `lines`, and the cursor grapheme helpers return `ChunkIter`, `LinesRaw`, `Lines`, or extend `Cursor`. Relocating the methods while leaving these structs in rope.rs will not compile—either those types move into the `ops` module (with public visibility) or the trait lives alongside them. The proposal should spell out how these types migrate.
- **Major – Public helper shims must survive**: `len_utf8_from_first_byte`, `count_newlines`, and `count_utf16_code_units` are consumed by downstream crates (notably `xi-core-lib` and `xi-plugin-lib`). The plan currently omits the need to leave `pub` forwarders in rope.rs (or an equivalent public module); moving the functions outright into `helpers` would be a breaking change.
- **Major – Delta/Transformer ops need feature gating coverage**: `Delta::base_len`, `iter_elements`, etc. are `pub(crate)` today and several Transformer helpers sit behind `#[cfg(feature = "serde")]`. The new `RopeDeltaOps` / `TransformerOps` traits have to live in modules that can see those private items and mirror the existing cfg layout; otherwise serde builds and no-serde builds will diverge.
- **Moderate – Plan references missing APIs**: the current rope.rs snapshot lacks a `Rope::edit` inherent method (tests still call it, so this probably needs reinstatement or the plan updated). Phase 1 should acknowledge the actual entry point it intends to expose.

**Key Adjustments**
1. Narrow `RopeOps` to Rope-only conveniences, keep core structural methods inherent on `Node`, and document how iterator/cursor types relocate or get re-exported so the trait compiles cleanly.
2. Add an explicit compatibility step that keeps `pub` shims (or re-exports) for helper functions already used outside the crate, including a quick audit of other cross-crate consumers.
3. Extend Phase 3 to cover cfg alignment (`serde` vs. default) and outline the call-site updates/import strategy so the churn is predictable; note separately that `Rope::edit` either has to be restored or removed from the scope.

自然的下一步是先在 rope 做一次原样 `cargo test -p xi-rope`，确认缺失的 `Rope::edit` 现状并评估拆分后需要补的 `use` 语句规模，再据此调整 Plan C 的任务拆分。

**关键问题**
- RopeOps 抽象与现有 `Node<RopeInfo, String>` 固有方法高度重叠，会迫使所有调用方额外导入 trait，带来“只为换皮”的改动且无显著收益。
- 迭代器与游标相关类型 (`ChunkIter`、`LinesRaw`、`Lines`、`Cursor` 扩展) 目前仍定义在 rope.rs，在不调整可见性的情况下无法被新 trait 实现直接引用，迁移步骤缺乏可行路径。
- `len_utf8_from_first_byte`、`count_newlines` 等 Helfer 已被其他 crate 调用，直接平移到 `helpers` 会造成跨 crate 断裂，需要保留 `pub` shim。
- `Delta`/`Transformer` 以及 serde 模块存在 `pub(crate)`、`cfg(feature = "serde")` 的访问约束，尚未规划新 trait 与现有可见性/feature 组合的兼容策略。

**改良方案**
- 将“对外门面”聚焦到 free function / wrapper 模式：新增 `rope::api` 模块，以 `pub use rope::api::*` 方式提供 `fn line_of_offset(&Rope, usize)`, `fn iter_chunks(&Rope, RangeBounds)` 等常用操作，同时保留现有 inherent 方法，避免 trait 侵入式重构。
- 拆出 `helpers::{leaf, metrics}` 但在 rope.rs 内保留 `pub fn count_newlines` 等薄 shim（调用 helper 内部实现），并同步审计其他 crate 的调用点，确保迁移时引入 `use crate::helpers::metrics::count_newlines_bytes` 等内部引用即可。
- 为 `Delta`/`Transformer` 增设 `api` 子模块，以 `pub(crate)` 函数封装现有私有逻辑，再在 `rope::api` 内提供 `pub fn delta_base_len(delta: &RopeDelta) -> usize` 等公开 wrapper，从而兼容 serde/no-serde 的现有结构且不破坏可见性。
- 若仍希望有“trait 风味”，可保留一个极轻量的 `RopeSummaryExt` trait（仅包含 metrics 与文本视图相关的增量方法），并直接位于 `rope` 模块内部，与 `ChunkIter` 等类型同级，解决可见性问题。
- 在执行上述调整前，先补回缺失的 `Rope::edit` 或在 wrapper 中提供 `fn rope_edit(rope: &mut Rope, range, new_text)` 以维持测试可用，再运行 `cargo test -p xi-rope` / `cargo test -p xi-rope --features serde` 确认重构后的稳定性。

**后续动作**
- 先评估并恢复 `Rope::edit` 的入口（或将 Plan 中的引用替换为实际存在的 API）。
- 给出 `rope::api` + helper shim 的最小 POC，跑通双轨测试确认结构无回归，随后再考虑是否需要 trait 化的增量扩展。
