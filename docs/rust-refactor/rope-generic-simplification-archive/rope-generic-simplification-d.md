# Rope Generic Simplification (Plan D)

_草案建立：2025-11-15_

## 1. 背景与问题
- `Node<RopeInfo, String>` 既承担底层 B-Tree 结构，又直接暴露字符串操作，调用者必须深入泛型层才能完成常见需求，学习成本高。
- Rope 专用的叶片拆分、度量统计等辅助函数散落在 `rope.rs`，同时被其他 crate 直接引用，导致内部重构难以收敛并且容易破坏兼容性。
- 迭代器与游标类型（`ChunkIter`、`LinesRaw`、`Lines`、`Cursor` 扩展）与 Rope 的实现紧耦合，若直接迁出会遇到可见性与生命周期问题，因此需要一种不打破现有布局的门面化策略。
- `Delta` 与 `Transformer` 的核心逻辑大量依赖 `pub(crate)` 函数和 `cfg(feature = "serde")` 分支，缺乏清晰的对外封装，跨 feature 使用体验差。

## 2. 核心目标
1. **保持 Node 固有 API 稳定**：`Node<RopeInfo, String>` 继续提供结构性操作，避免对 COW/树逻辑的侵入式重构。
2. **提供函数式门面**：为外部调用者输出一组易发现的顶层函数，减少直接面对 `tree` 泛型的负担。
3. **抽离 helper，但保留 shim**：将字符串叶片、度量统计等逻辑收敛到 `helpers` 子模块，同时在 `rope.rs` 暴露 `pub` 前向函数以保持跨 crate 兼容。
4. **分层同步工具链**：脚本、文档、测试随重构同步更新，确保 C#/Rust 双端可快速对照。

## 3. 方案总览
- **API 门面模块**：新增 `rope::api`，集中导出 `fn line_of_offset(&Rope, usize)`, `fn iter_chunks(&Rope, impl IntervalBounds)` 等高频函数，并提供 `prelude` re-export，供外部 `use rope::prelude::*;` 后直接调用。
- **Helper 整理**：拆分 `helpers::leaf`、`helpers::metrics` 等内部模块，聚合字符串拆分、UTF-16/换行统计等逻辑；`rope.rs` 保留 `pub fn count_newlines(...)` 等 shim 函数调用 helper，实现内部复用 + 外部兼容。
- **Delta/Transformer 包装层**：在 `delta::api`、`transformer::api` 中提供与 `RopeDelta`、`Transformer` 相关的公开函数式封装（如 `rope_delta_base_len`, `transform_offset`），内部继续调用现有 `pub(crate)` 方法并遵循 serde 的 feature guard。
- **文档与脚本**：`docs/skeleton/rope.md`、`docs/skeleton/xi.Core.decompiled.cs`、`docs/architecture/rope-port-mapping.md` 在每阶段刷新；`scripts/refresh_skeleton_docs.py`、`scripts/refresh_serialization_fixtures.ps1` 保持无改动但新增 runbook 步骤。

## 4. 模块拓扑提案
```
rope/
├── api.rs              # 函数式门面，re-export 常用操作
├── helpers/
│   ├── leaf.rs         # 叶片拆分/借用/容量逻辑
│   └── metrics.rs      # 纯函数度量工具（UTF-16, newline, grapheme 辅助）
├── prelude.rs          # pub use Rope / RopeInfo / api::*
├── delta/
│   └── api.rs          # RopeDelta/RopeTransformer 函数门面
├── transformer/
│   └── api.rs          # 若保持拆分，可共享 delta::api
├── rope.rs             # 结构类型与 shim（继续作为事实来源）
└── tree.rs             # 泛型层维持现状
```
> `api` 模块均为 Rust-only 调整；最终命名可在实现时微调。

## 5. 实施阶段
### Phase 0 — 预备与基准
- 补回或确认 `Rope::edit` 及其他测试所需的固有方法存在，`cargo test -p xi-rope` / `--features serde` 必须通过。
- 统计跨 crate 对 `count_newlines`、`len_utf8_from_first_byte` 等函数的引用，记录在 `docs/rust-refactor/breaks-metrics-templating.md`。

### Phase 1 — API 门面落地
- 新增 `rope::api` 与 `rope::prelude`，对外暴露 `Rope`/`RopeInfo`/顶层函数；内部调用仍复用 `Rope` inherent 方法或 helper。
- 调整 `rope.rs` 内部测试改用 `use crate::rope::api::*;` 确认门面完整。
- 不移除原有 inherent 方法，仅在文档中推荐使用门面。

### Phase 2 — Helper 拆分
- 引入 `helpers::leaf`、`helpers::metrics` 模块，迁移 `find_leaf_split_for_merge`、`count_utf16_code_units_bytes` 等实现。
- 在 `rope.rs` 保留 `pub fn count_newlines(s: &str)` 等 shim，内部调用 helper。
- 为 helper 增加细粒度单元测试（`#[cfg(test)]`），覆盖拆分/UTF-16 统计/代理对边界。

### Phase 3 — Delta/Transformer 包装
- 在 `delta::api`/`transformer::api` 中提供 `rope_delta_base_len`, `rope_delta_iter_elements`, `transform_offset_utf16` 等函数。
- 通过 `cfg(feature = "serde")` 保持与现有实现一致，并在 `rope::api` re-export 关键函数。
- 更新 `engine`, `serde_impls`、测试等调用点优先使用包装函数。

### Phase 4 — 文档 & 验证
- 运行 `python scripts/refresh_skeleton_docs.py` 同步骨架；更新 `docs/architecture/rope-port-mapping.md` 映射表。
- 在 `AGENTS.md` 记录完成状态与风险；如有需要，同步相关历史记录文档，保持信息一致。

## 6. 兼容策略
- 所有对外 `pub` API 保持函数签名不变，通过 shim 转调新 helper。
- `rope::api` 仅新增入口，不强制迁移旧代码；若后续要 deprecate 原方法，需要单独评估。
- `delta::api` / `transformer::api` 在非 serde 模式下提供 no-op 或有限 API，避免 feature 组合断裂。
- `rope.rs` 内部 `use helpers::*` 保持 `pub(crate)`，防止外部直接引用内部模块。

## 7. 测试与验证
1. `cargo test -p xi-rope`（默认 features）。
2. `cargo test -p xi-rope --features serde subset_serialization_regression delta_serialization_regression engine_serialization_regression`。
3. `scripts/refresh_serialization_fixtures.ps1 -SkipRust -SkipDotnet` 验证 CLI 不受影响。
4. 视情况运行 `dotnet test tests/xi.Core.Tests` 确认黄金串未变化。

## 8. 风险与缓解
| 风险 | 影响 | 缓解 |
|---|---|---|
| 门面与固有方法语义偏差 | 行为回归 | 保持门面调用原有方法；在测试中断言等价性 |
| helper 拆分遗漏 shim | 破坏跨 crate 兼容 | Phase 0 审计引用，拆分时同步添加 `pub fn` shim |
| serde feature 不一致 | 构建失败 | 在 `delta::api` 内 mirror 现有 `cfg`，并在 CI 运行双轨测试 |
| 文档不同步 | 协同失真 | Phase 4 将 skeleton/蓝图刷新列为 DoD |

## 9. 里程碑与下一步
- **M0**：API 门面初版合入（Phase 1 完成）。
- **M1**：helper 拆分 + shim 完成并通过测试（Phase 2）。
- **M2**：Delta/Transformer 包装上线（Phase 3）。
- **M3**：文档/脚本同步，Plan D 收官（Phase 4）。

## 10. TODO 清单（滚动维护）
- [ ] Phase 0：补全 `Rope::edit` 入口并确认基线测试通过。
- [ ] Phase 1：引入 `rope::api`/`prelude`，将 `rope/src/tests` 调整为使用新门面。
- [ ] Phase 2：迁移 helper 并补齐单元测试，保留 shim。
- [ ] Phase 3：实现 `delta::api`/`transformer::api`，更新调用点。
- [ ] Phase 4：刷新 skeleton、蓝图、AGENTS 记录。
- [ ] 评估是否需要为 `Breaks`/`Spans` 提供同构门面，纳入后续会话。

**Plan D 评估**  
- 方案里最有价值的部分是把叶节点/度量逻辑继续抽成 helper，与 C# 侧的 `StringLeafOperations` 对齐，这一点和 rope.rs 里散落的 `find_leaf_split_*`、`count_utf16_code_units` 等自由函数确实存在协同潜力。  
- 其余三大目标（API 门面、Delta/Transformer 包装、prelude re-export）收益有限却要同时动到 lib.rs、rope.rs、delta.rs、`serde_impls.rs` 乃至所有调用点，维护和回归成本远超预期收益。  
- 结论：保留 helper 拆分方向，API 门面与 Delta/Transformer 包装应暂缓；真实需求出现前不建议投入 Phase 1/3。  
- 建议把文档重新聚焦在“叶 helper 拆分 + shim 保持兼容”这一条，并补上对 Rust 现状（metrics 已模块化）的校准。

**主要阻碍**  
- rope.rs 里的大部分功能（`line_of_offset`、`iter_chunks`、`lines_raw`、`lines`、`slice_to_cow`……）都是 inherent method。要在 `rope::api` 中补齐同名函数，需要 1:1 包装二十余个入口，还要维护 doc test，基本是在造第二套 API，长期来看很难保证行为不会漂移。  
- 创建 `rope::api` / `rope::prelude` 意味着 rope.rs 必须引入子模块，现有文件将额外承担 `pub mod api;` 的模块树管理，并与 lib.rs 现有的 `pub use crate::rope::{LinesMetric, Rope, RopeDelta, RopeInfo};` 互相耦合。重构完成后还得同步更新外部示例和文档，风险集中、收益有限。  
- 计划的 Phase 2 只提到把 helper 拆出来，但没有处理 `MIN_LEAF`/`MAX_LEAF` 常量，也没考虑 `impl Leaf for String`（同文件）如何引用新模块；若直接迁移会引入循环依赖或迫使常量提升到公共模块。  
- delta.rs 中 `base_len`、`iter_elements` 等方法故意保持 `pub(crate)`；Plan D 的 `delta::api`/`transformer::api` 会把这些实现暴露为新公共 API，相当于在 0.x 版本里新增稳定承诺，且必须同时在 `serde_impls.rs`、`engine` 等调用点更新导入路径，缺乏足够理由。  
- `metrics` 相关 helper 已于 2025-11-14 拆分到 `xi-editor-ph7/rust/rope/src/metrics/*.rs`，Plan D 再次规划“metrics helper 拆分”会重复造轮子；若按文件结构重新落地一次，还需解决与现有模块的命名冲突。  
- 运行脚本／测试的 DoD（refresh_serialization_fixtures.ps1、`cargo test -p xi-rope --features serde ...`）在 Phase 4 才出现，意味着前期多个阶段会在无完整回归的情况下大面积重排文件结构，风险偏高。

**可行微调**  
- 先把 Plan D 缩减为“Leaf/字符串 helper 拆分 + shim 兼容”，明确常量归属和 `impl Leaf for String` 的引用方式，再把阶段目标写进 breaks-metrics-templating.md 或等效文档。  
- 若未来确实需要函数式门面，可先列一份真实调用痛点（例如某些 crate 无法直接引用 `Node<RopeInfo, String>`）的清单，再评估是否值得新增 `api` 层，而不是预先铺设。  
- Delta/Transformer 的新包装若要推进，需结合 Stage D/C# 端的具体缺口，弄清楚哪些 `pub(crate)` 能被安全开放，并在文档里约定兼容策略，否则建议保持现状。

**改良建议**  
- 先聚焦在“叶/度量 helper 抽离 + shim 保持兼容”这一条：把 `find_leaf_split_*`、`count_utf16_code_units`、`count_newlines`、`len_utf8_from_first_byte` 等自由函数放进新的 `helpers::{leaf,metrics}`（或直接复用现有 `metrics::` 子模块），rope.rs 仅保留薄 `pub fn` 转调。这样既能给 C# 侧提供清晰对照，也不会引入额外 API 面。  
- 为 `impl Leaf for String`、`TreeBuilder::push_str` 等内部逻辑增加面向 helper 的 UT（`#[cfg(test)]`）覆盖；按 `FindLeafSplit`、`CountUtf16`、`CountNewlines` 三类场景写最小测试，确保拆分不回归。  
- 若确有“函数式门面”的真实需要，建议在 helper 重构完成后再调研：先列出外部 crate 无法直接复用 inherent method 的具体痛点，再评估是否用 `rope::api::{line_of_offset, iter_chunks}` 层包裹；否则维持现状。  
- Delta/Transformer 包装层目前缺乏明确消费者，同时会破坏 `pub(crate)` 封装；建议先记录潜在需求（例如 C# 端 mirror 需要的最小不变式）并在 delta.rs 内标注 `pub(crate)` → `pub` 的条件，再决定是否开放。  
- 文档层面把计划压缩为两段：1) helper 抽离实施步骤与 shim 策略；2) 后续可能评估的 API 门面/Delta 包装。同步更新 breaks-metrics-templating.md、AGENTS.md，保持与实际代码状态一致。
