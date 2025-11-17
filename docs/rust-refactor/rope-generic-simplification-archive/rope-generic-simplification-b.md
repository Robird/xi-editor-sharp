# Rope Generic Simplification (Plan B)

_Last updated: 2025-11-14_

## 1. Context & Motivation
- C# 侧已经引入 `Node<TInfo, TLeaf, TLeafOps>`，而 Rust 端依旧暴露 `Node<N, L>`/`Delta<N, L>` 等高度泛型接口。跨语言映射时，Rust 模块对外暴露的泛型会迅速堆叠，让调用方与测试都需要掌握完整 trait 网才能推进。
- 现有瘦身方案（Plan A）尝试重写核心 trait（`NodeInfo`、`Leaf`、`Metric` 等），但评估后发现改动面极大：需要在多处重写节点编辑逻辑、度量换算与 OT 变换，风险与回归成本远超收益。
- 目标改为“在不拆换核心 trait 的前提下，降低**对外可见的泛型复杂度**，并将重复逻辑收敛到稳定 helper”。该策略能大幅减少 API 暴露面，同时保持现有实现与测试的稳定性。

### 成功判据
1. `rope`/`breaks`/`spans` 等模块对外暴露 **concrete 包装类型** （例如 `RopeNode`），外部调用无需再直接依赖 `Node<N, L>` 泛型别名。
2. Rope 相关操作（叶拆分、度量换算、常用统计）集中在 helper 模块，C# 迁移或其他调用方只需关注小规模函数集。
3. 现有 `Metric<N, L>`、`Delta<N, L>`、`Cursor<'a, N, L>` 等核心 trait 保持原样，`cargo test -p xi-rope`、`cargo test --workspace` 与 serde 回归测试在各阶段均保持通过。
4. 文档（skeleton、port-blueprint、风险记录）同步体现 wrapper 与 helper 的新结构。

---

## 2. 当前痛点速写
- `Node`/`TreeBuilder` 对外依旧是完全泛型，`rope.rs`、`breaks.rs`、`spans.rs` 只是 `type Rope = Node<RopeInfo, String>` 的裸别名；任何深入调用都需要掌握泛型约束。
- 叶操作逻辑（如 `String::push_maybe_split`、`find_leaf_split_for_merge` 等）分散在多个模块与自由函数中，协同实现难以通过单一入口复用。
- 度量 helper (`count_newlines`, `count_utf16_code_units` 等) 与 `Metric` 实现混杂在 `rope.rs`、`metrics/` 模块，静态派发虽高效但不便迁移。
- `Delta/Transformer` 虽保持泛型，却缺乏 Rope 专用封装，导致高层在序列化或测试时仍需要显式写 `<RopeInfo, String>`。

---

## 3. Plan B 总览
Plan B 聚焦“收敛对外接口 + 模块化 helper”，分 4 个阶段推进：

1. **模块包装层（Module Wrappers）**：为 Rope、Breaks、Spans 引入小型 newtype wrapper（如 `pub struct RopeNode(Node<RopeInfo, String>);`），实现 `Deref<Target = Node<…>>` 与常用构造器，对外屏蔽泛型细节。
2. **Rope Helper 抽离**：将 Rope 叶片拆合、换行统计、UTF-16 计数等自由函数统一搬到 `rope::helpers`；节点内部依旧使用泛型 `Node`，调用 helper 时通过 `RopeNode` 包装，使逻辑集中、易复用。
3. **Metric Helper 模块化**：保留 `Metric<N, L>` trait，但整理共通逻辑到 `metrics/helpers.rs`，对外暴露 `RopeMetrics::{bytes, lines, utf16}` 等纯函数 API，以减少直接泛型调用。
4. **Delta/Transformer 封装**：为 Rope 导出专用别名/封装（`pub struct RopeDelta(Delta<RopeInfo, String>);`、`pub struct RopeTransformer<'a>(Transformer<'a, RopeInfo, String>);`），提供便捷构造与常用查询，隐藏泛型符号。

每个阶段均限定在“包装 + 抽离 helper”范围，不触动核心 trait 或内部编辑算法，方便随时回滚。

---

## 4. 详细实施步骤

### Phase A — 模块包装层
- **工作内容**
  - 在 `rope.rs` 中新增 `pub struct RopeNode(Node<RopeInfo, String>);` 并实现：
    - `impl From<Node<RopeInfo, String>> for RopeNode`、`impl From<RopeNode> for Node<…>`
    - `impl Deref`/`DerefMut`
    - 常见构造器（`from_str`, `concat`, `len`, `is_empty` 等）直接委托给内部节点。
  - Breaks/Spans 等模块同样引入 `BreakTree`, `SpanTree<T>` 等包装。
  - 更新对外 API：示例、`pub type Rope = RopeNode`（或保留旧类型并 re-export 新包装）。
  - 对外文档/示例改用新 wrapper。
- **验证**
  - `cargo test -p xi-rope`
  - 运行 `cargo test -p xi-rope --features serde`（验证 serde regression 未受影响）
- **迁移策略**
  - 保留原 `type Rope = Node<…>` 一段过渡期（deprecated 注释），新代码只引用 `RopeNode`。

### Phase B — Rope Helper 抽离
- **工作内容**
  - 创建 `rope/helpers.rs`，迁移以下逻辑：
    - `find_leaf_split_for_merge`, `count_newlines`, `count_utf16_code_units`、`count_newlines_bytes`, `find_prev_newline` 等自由函数。
    - string 叶片拆分/合并的细节函数（如 `push_str` 后的容量判断、split point 计算）。
  - 在 `String::push_maybe_split` 内改为调用 helper（包裹在 `rope::leaf_ops::split_string_leaf` 等函数中）。
  - 为 helper 添加 `#[cfg(test)]` 单元测试以及 rope 集成测试覆盖。
  - 更新 `docs/skeleton/rope.md` 以展示 helper 模块。
- **验证**
  - `cargo test -p xi-rope`
  - Rope 相关 property/回归测试：`cargo test -p xi-rope rope_info`, `cargo test -p xi-rope rope_edit` 等（可在 `run_all_checks` 中自动化）。

### Phase C — Metric Helper 模块化
- **工作内容**
  - 新建 `metrics/helpers.rs`，集中 `count_newlines_bytes`, `count_utf16_code_units_bytes`, `is_newline_boundary`, `is_codepoint_boundary` 等函数。
  - 在 `Metric` 实现中引用 helper，减少重复代码；`metrics/mod.rs` 对外 re-export `RopeMetrics`（静态函数集合）。
  - 对外暴露纯函数 API（例如 `RopeMetrics::lines(root: &RopeNode) -> usize`），内部仍调用 `Metric` trait，确保性能。
  - 添加微基准（Criterion 可选）或对比测试，保证 helper 封装后无明显性能回退。
- **验证**
  - `cargo test -p xi-rope` / `cargo test -p xi-rope --features serde`
  - 若启用基准：`cargo bench -p xi-rope metrics::*`（可选）

### Phase D — Delta/Transformer 封装
- **工作内容**
  - 在 `delta.rs` 暴露 `pub struct RopeDelta(Delta<RopeInfo, String>);`，实现 `Deref<Target = Delta<…>>` 与常见 helper（`base_len`, `iter_elements`, `apply_to_rope`, `is_simple_delete` 等）。
  - 同步封装 `InsertDelta`、`Transformer` 等 Rope 专用结构体，提供 `RopeDelta::simple_edit`, `RopeTransformer::new(&RopeNode)` 等便捷函数。
  - 调整 `rope.rs`, `engine`, `spans` 引用路径，优先搬到新封装。
  - serde 访问点（`serde_impls.rs`, `serde_fixtures`）改用 wrapper。
- **验证**
  - `cargo test -p xi-rope --features serde subset_serialization_regression`
  - `cargo test -p xi-rope --features serde delta_serialization_regression`
  - `cargo test -p xi-rope --features serde engine_serialization_regression`
  - `dotnet test tests/xi.Core.Tests`（确认 C# 侧夹具仍匹配）

---

## 5. 文档与协同要求
- 每个阶段完成后：
  - 同步刷新 `docs/skeleton/rope.md` 与 `docs/skeleton/xi.Core.decompiled.cs`（脚本 `python scripts/refresh_skeleton_docs.py`）。
  - 更新 `docs/architecture/rope-port-mapping.md` 对照表，记录新 wrapper/helper。
  - 在 `AGENTS.md` 记录阶段完成情况与风险。
- Plan A 文档（`rope-generic-simplification.md`）保留历史记录，本 Plan B 作为现行策略。

---

## 6. 风险与缓解
| 风险 | 描述 | 缓解措施 |
|------|------|----------|
| Wrapper 引入破坏现有 API | 外部 crate 可能直接使用 `type Rope = Node<…>` | 逐步迁移：先提供 wrapper + 旧 type，后期通过 `#[deprecated]` 提示，并在 release note 说明 |
| Helper 抽离时漏掉测试覆盖 | Rope/Metric helper 重写易引入边界 bug | 每步迁移后运行 `run_all_checks`，并在 helper 模块内部新增 focused 单元测试 |
| serde fixture 不一致 | Delta/Rope wrapper 可能改变 `Serialize` 输出 | 在 Phase D 后执行 `scripts/refresh_serialization_fixtures.ps1`，审阅 diff，必要时更新文档与黄金串 |
| 性能退化 | helper 层额外的函数调用可能影响热路径 | 保持 `#[inline]` 注解，必要时用 `cargo bench rope_edit_small` 等比较前后差异 |

---

## 7. 后续扩展（可选）
- **更细的 RopeBuilder 封装**：在 wrapper 基础上暴露 `RopeBuilder`，封装 `TreeBuilder<Node<…>>` 常用场景，进一步减少泛型直接接触面。
- **面向对象 Metric**：若后续仍需动态选择 metric，可基于 wrapper 增加 `RopeNode::count_lines()` 等具体方法，而非暴露 trait。
- **跨语言共享 helper**：将 Rope helper 模块整理为 FFI 友好的函数集合，为将来可能的 Rust ↔ C# 互操作做准备。

---

## 8. 里程碑追踪
| 阶段 | 预计完成指标 | 负责人 | 状态 |
|------|---------------|---------|------|
| Phase A | Wrapper 导出、示例迁移、`cargo test -p xi-rope` | TBD | Pending |
| Phase B | helper 模块就绪、rope tests 补齐 | TBD | Pending |
| Phase C | metric helper 落地、性能验证 | TBD | Pending |
| Phase D | RopeDelta 封装、serde 回归通过 | TBD | Pending |

完成每个阶段后，在此表单中更新状态，并在 `AGENTS.md` 写入对应日期的工作日志。

---

## 9. 总结
Plan B 主打“对外瘦身、内部稳定”的策略：
- 利用 newtype wrapper 把核心泛型限制在模块内部，实现平滑 API；
- 通过 helper 模块集中业务逻辑，为跨语言迁移提供清晰入口；
- 保持核心 trait、树算法与 serde 行为不动，确保回归风险可控。

该方案可分阶段落地，每一步都能通过现有测试基线确认安全性，也更方便后续扩展或回滚。请在执行时持续记录产物与风险，确保 Rust/C# 文档同步。

**可行性评估**
- rope.rs、engine.rs 等大量模块当前把 `Rope` 当作 `Node<RopeInfo, String>` 的别名直接调用节点级 API（`len`/`subseq`/`concat`/`TreeBuilder::<RopeInfo, String>::new()` 等成百上千次）。若改成 `pub struct Rope(Node<…>)` 并不再 `Deref`，需要重新实现或转发几乎整套节点接口，连带 `Add`/`Display`/`From<&Rope>` 等 trait 也要重写，工作量远超“包装层”所宣称的轻量封装。
- 计划里期望借 newtype wrapper 隐藏泛型，但 tree.rs 的核心 API 仍暴露在外，`TreeBuilder`、`Cursor`、`Delta::simple_edit` 等都继续要求显式的 `<N, L>`；即使 wrapper 到位，调用方仍需处理 `NodeInfo`/`Leaf` 约束，泛型网不会实质收敛。
- Phase B 想把叶片拆分、换行计数等搬去 `rope::helpers`，但这些函数深度依赖 `MIN_LEAF/MAX_LEAF`、`TreeBuilder` 和 `String` 专属逻辑。迁出后仍要在 rope.rs 与 `String` 的 `Leaf` 实现之间大量穿梭，收益主要是文件分拆，未触及跨语言痛点。
- Phase C 把度量 helper 模块化已在现有代码完成：mod.rs 早已拆出 `codepoint.rs`、`lines.rs`、`break_indices.rs` 等专用 helper。继续执行该阶段只是重复整理。
- Phase D 为 `Delta`/`Transformer` 增加 Rope 专用包装会非常沉重：delta.rs 里公共方法几十个（`factor`/`transform_expand`/`synthesize`/`iter_inserts` …），都要在 `RopeDelta` 等新类型里逐一转发，否则用户马上掉回泛型接口；同时 serde_impls.rs 需要整版同步调整，潜在回归面巨大。

- 根本问题在于 tree.rs 的节点/构建/游标结构本质上就是泛型框架。Plan B 侧重 cosmetically 包一层 newtype，但没有改变需要掌握的 trait 网，也没有提出如何让 `TreeBuilder`/`Cursor` 变得“具体”。投入与预期回报失衡，风险大于收益。

1. 若仍想降低外部可见复杂度，建议先定义具体的高层操作接口（例如 `RopeBuilder`, `RopeCursor`）并评估是否能完全覆盖现有调用，再决定是否引入 newtype；否则维持 type alias 更稳妥。
2. 收敛叶片/度量 helper 的诉求可以对现有 `metrics/`、`String` 叶操作进行增量整理，而无需引入大规模 wrapper 改造。

**改良思路**
- 维持 `pub type Rope = Node<RopeInfo, String>` 等别名，避免 newtype 包装触发的全面重写，把“瘦身”重点放在暴露接口的组织方式而非结构体替换上。
- 针对常用的 `Rope` / `Breaks` / `Spans` 行为，增设扩展 trait（例如 `pub trait RopeOps`），在 `impl RopeOps for Node<RopeInfo, String>` 中集中公开稳定的高层方法；调用方 `use rope::prelude::*` 后即可使用具体 API，不再需要理解 `Metric<N, L>` 等泛型约束。
- 提供“外层门面”模块（`rope::api`, `breaks::api`…），只 re-export type alias 与扩展 trait，并导出精选 helper（叶片拆分、度量查询、序列化助手）。原始泛型 API 继续留在 `tree` 等内部模块，必要时通过 `#[doc(hidden)]` 降低外部检索噪声。
- 将 string 叶片相关逻辑继续拆分进 `rope/leaf_ops.rs` 等文件，但保持 `impl Leaf for String` 内部调用 helper，避免反复穿梭；helper 模块只暴露面向 C#/FFI 迁移需要的纯函数，测试也配套迁移，降低散落自由函数的维护成本。
- 对 `Delta` / `Transformer` 不做 newtype，而是新增 `pub trait RopeDeltaOps`/`TransformerOps` 提供“只关心 Rope”的便利方法与构造器，并在 serde 模块中统一调用 trait；这样 serde/引擎部件几乎零改动即可获得更清晰的 API。
- 在文档侧同步：`docs/skeleton/*.md` 用脚本刷新并标出新 prelude/ops trait；`port-blueprint.md` 更新“对外接口层”与“内部泛型层”的分层说明。

**可行性评估**
- 扩展 trait 基于专用类型别名实现，不改变数据表示，兼容现有代码；迁移可分功能逐步开展，每完成一组方法就新增 trait & 测试。
- Prelude 模块仅重新组织 re-export，不需要触碰 `TreeBuilder`/`Cursor` 复杂逻辑；与 Plan B 相比，改动面小、回滚简单。
- helper 拆分聚焦在 “string 叶片” 与 “metrics” 已有模块上，依赖关系明确，可一边迁移一边运行 `cargo test -p xi-rope`/serde 回归。
- `Delta`/`Transformer` ops trait 只是一层方法收口，不参与泛型推导，serde 代码继续编译通过；未来若仍想引入 newtype，可在 trait 基础上平滑升级。
- 文档/脚本同步已有自动化（`refresh_skeleton_docs.py`），只需在阶段完成后运行并更新 AGENTS.md 记录。

**实施步骤建议**
- 先定义 `rope::prelude`，内含 `Rope`, `RopeInfo`, `RopeOps`; 将 rope.rs 中高频方法搬到 trait，并把现有测试改为 `use crate::rope::prelude::*` 验证。
- 扩展 `metrics` 与 `leaf_ops` 模块：把 `count_newlines`、`find_leaf_split_for_merge` 等搬入新 helper，伴随单元测试转移。
- 在 delta.rs 中提炼 `RopeDeltaOps`（含 `base_len`, `iter_elements`, `apply_to_rope` 等），并让 serde/engine 使用 trait 方法；同理补 `TransformerOps` 的 `transform_rope_offset` 等高层函数。
- 文档与外部记忆更新：完成每阶段后运行 skeleton 刷新、调整 `rope-port-mapping.md` 与 AGENTS.md 的阶段状态、记录测试结果。
- 长期可评估在 prelude 模块中导出 `TreeBuilder<RopeInfo, String>` 的包装函数（如 `RopeBuilder::from_str`），进一步减少外界接触底层泛型的机会。

**后续关注**
- Trait 重组后，需保持现有泛型 API 仍可访问（避免 break change）；可在 prelude 文档中明确“必要时仍可从 `rope::tree` 访问泛型层”。
- 扩展 trait 要求方法名稳定、覆盖率足够；建议先统计现有调用点（`runSubAgent` 搜索 Node<RopeInfo,String> 调用）以确认优先迁移的 API。
- Prelude/ops trait 新结构需在 C# 文档中同步，便于移植方理解 Rust 端协作方式。
