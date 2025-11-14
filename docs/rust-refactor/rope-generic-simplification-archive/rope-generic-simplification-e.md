# Rope Generic Simplification (Plan E)

_草案建立：2025-11-15_

## 1. 背景与复盘
- 过往尝试曾试图通过新增 `rope::api`、`prelude` re-export、`delta::api` 等门面层来推动泛型化；这些做法要求对 `rope.rs` 内部的 inherent 方法逐一包装，测试与文档的同步成本高昂，却难以提供与投入相称的收益。
- 当前移植痛点主要集中在“将字符串叶片相关的自由函数抽离成 helper，同时保持对外 API 不变”，以便与 C# 侧的 `StringLeafOperations` 形成一一对应；其他 inherent 方法（如 `iter_chunks`、`lines_raw` 等）尚未出现跨 crate 复用障碍。
- Rust `metrics` 模块已于 2025-11-14 完成拆分（`metrics/{codepoint,lines,break_indices}`），未来的简化方案应复用这一成果，而不是重复规划新的度量 helper 重构。

## 2. 核心目标
1. **抽离字符串叶片 helper，实现跨语言对照**：将 `find_leaf_split_*`、`count_utf16_code_units` 等自由函数迁移到新模块，并保持 `rope.rs` 中的薄 shim（`pub fn count_newlines` 等）以兼容既有调用者。
2. **巩固常量与实现不变量**：在 helper 中集中引用 `MIN_LEAF`/`MAX_LEAF` 等约束，避免多处散落；补充 `#[cfg(test)]` 单元测试覆盖叶拆分、合并与 UTF-16 统计路径。
3. **保持对外 API 稳定**：不引入新的 `rope::api`/`prelude` 层，不调整 `Delta`/`Transformer` 可见性，现有调用者无需改动。
4. **同步文档与 C# 对应资产**：完成 helper 重构后刷新 skeleton / blueprint 文档，确保 C#/Rust 双端语义对齐。

## 3. 设计要点
- 新建 `rope/src/helpers/leaf.rs`（可与 `metrics/` 结构对齐），暴露 `pub(crate)` 函数供 `rope.rs` 与未来的 `tree` 模块复用；`MIN_LEAF` / `MAX_LEAF` 仍由 `rope.rs` 定义并通过 `pub(crate)` 传入 helper，避免公开常量。
- `count_utf16_code_units`、`find_leaf_split_for_merge` 等改为 `helpers::leaf` 内部函数；`rope.rs` 中保留相同签名的 `pub`/`fn`，实现仅做转调。
- `len_utf8_from_first_byte` / `count_newlines` 依旧向外暴露 `pub fn`（兼容 `xi-core-lib::editor`、`xi-plugin-lib::state_cache`），内部调用 `metrics::codepoint::len_utf8_from_first_byte` 或 `helpers::leaf::count_newlines`。
- 维持 `impl Leaf for String`、`TreeBuilder::push_str` 具体逻辑，只改为调用 helper；确保没有新特性门控或可见性破坏。

## 4. 实施阶段
### Phase 0 — 基线校验与库存
- 运行 `cargo test -p xi-rope`（含 `--features serde`）确认当前基线稳定。
- 记录所有对外 `pub fn`（`count_newlines`, `len_utf8_from_first_byte` 等）被哪些 crate 使用（参照 `docs/rust-refactor/breaks-metrics-templating.md` 现有条目），作为后续 shim 的回归清单。

### Phase 1 — 叶片 Helper 模块化
- 新增 `rope/src/helpers/mod.rs` 与 `helpers/leaf.rs`：
  - `pub(crate) fn find_leaf_split_for_bulk(...)`、`find_leaf_split_for_merge(...)`、`find_leaf_split(...)`、`count_utf16_code_units(...)`。
  - 负责维护 `MIN_LEAF`/`MAX_LEAF` 偏好及 UTF-8 边界逻辑。
- `rope.rs` 中原自由函数改为调用 helper，并将 `impl Leaf for String` 与 `TreeBuilder::push_str` 调整为依赖 helper。
- 添加针对 helper 的单元测试（`#[cfg(test)] mod leaf_helpers_tests`）：覆盖换行优先拆分、UTF-16 统计与超过最大叶片时的拆分路径。

### Phase 2 — Shim 与度量对齐
- `pub fn count_newlines` / `pub fn len_utf8_from_first_byte` 在 `rope.rs` 中统一转调 helper/metrics。
- 检查 `xi-core-lib`、`xi-plugin-lib`、`xi-rpc` 等 crate 是否需要更新导入路径；若仅 shim 行为变化无需改动。
- 扩充现有 `LinesMetric`/`Utf16CodeUnitsMetric` 单元测试，确保 helper 抽离后仍覆盖同样的边界场景。

### Phase 3 — 生态同步
- 运行 `python scripts/refresh_skeleton_docs.py` 刷新 `docs/skeleton/*.md`。
- 更新 `docs/architecture/rope-port-mapping.md`、`docs/csharp-refactor/node-generic-refactor-plan.md`、`AGENTS.md`，记录 helper 模块化完成与 shim 策略。
- 视需要补充 C# 侧 `StringLeafOperations` 与 Rust helper 的对照表，确保跨语言保持统一实现细节。

## 5. 输出清单
- `rope/src/helpers/mod.rs`、`helpers/leaf.rs` 两个新文件。
- `rope.rs` 内 helper 调用与 shim。
- `rope.rs` 或新模块内的 `#[cfg(test)]` 单元测试。
- 刷新后的 skeleton / blueprint / AGENTS 文档条目。

## 6. 测试流程
1. `cargo test -p xi-rope`。
2. `cargo test -p xi-rope --features serde subset_serialization_regression delta_serialization_regression engine_serialization_regression`。
3. `scripts/refresh_serialization_fixtures.ps1 -SkipRust -SkipDotnet`（校验 CLI 输出未受影响）。
4. `dotnet test tests/xi.Core.Tests`（确认 C# 黄金串未变化）。

## 7. 风险与缓解
| 风险 | 影响 | 缓解措施 |
| --- | --- | --- |
| helper 拆分遗漏 shim | 破坏下游 (`xi-core-lib`) | Phase 0 列表 + Phase 2 shim 检查；PR 自检引用。 |
| `MIN_LEAF`/`MAX_LEAF` 定义重复 | 常量漂移 | 将常量继续集中在 `rope.rs`，helper 通过参数或引入 `pub(crate)` 常量访问。 |
| helper UT 缺失导致回归难发现 | 行为退化 | Phase 1 必须补足最小测试，覆盖 newline 优先、UTF-16 surrogate、极限长度等场景。 |
| 文档不同步 | C#/Rust 对照失真 | Phase 3 将文档刷新列为 DoD，确保代码与对照文档始终一致。 |

## 8. 后续展望
- 若未来出现真实的函数式门面需求（跨 crate 复用困难），再发起专门提案评估 `rope::api` 的可行性与收益。
- Delta/Transformer 包装层暂不推进；待 C# 端明确镜像所需的最小公共 API 后，再评估是否开放 `pub(crate)` 方法或额外 shim。

**评估结论**
- 方案 E 的核心目标（把 `find_leaf_split_*` / `count_utf16_code_units` 等字符串叶片逻辑下沉到统一 helper、保留原 API shim）在现有代码结构下是可实现的，但需要补充若干细节才能降低回归风险。
- 目前 rope.rs 内的相关函数已经是薄封装（`count_newlines` / `len_utf8_from_first_byte` 直接转调 `metrics::*`），Phase 2 所描述的“Shim 与度量对齐”基本已达成，实施前可先删去重复工作或改写为验证步骤。
- 方案未触及的语义差异（特别是换行窗口与断点策略）会妨碍与 C# `StringLeafOperations` 真正对齐，需要在落地前明确是沿用 Rust 逻辑还是同步 C# 的 `NewlinePreferenceWindow`/代理对修正。
- helper 模块计划只覆盖 `find_leaf_split_*` 与 `count_utf16_code_units`，但 `StringLeafOperations` 还承载插入/删除/替换/叶片合并的更多语义；如果最终目标是跨语言共享叶片行为，这次拆分应当明确后续接口演进路线，否则易陷入“只移走一半逻辑”的尴尬状态。

**关键分析**
- **现状确认**
  - rope.rs：`find_leaf_split_*` 私有函数依赖 `MIN_LEAF`/`MAX_LEAF`，`impl Leaf for String` 与 `TreeBuilder::push_str` 是唯一调用者；`count_newlines`/`count_utf16_code_units` 已是对 `metrics::*` 的转调，`len_utf8_from_first_byte` 亦复用 `metrics::codepoint`.
  - 外部调用：editor.rs 直接通过 `xi_rope::rope::count_newlines` 计算行号，因此必须保留 `pub fn count_newlines` 兼容层。
  - C# 侧 StringLeafOperations.cs 的拆分能力显著多于 Rust 现有 helper，且换行偏好窗口固定为 64，和 Rust 目前“全区间回溯搜索”策略不同。

- **施行要点与缺口**
  - Phase 1 需要在 lib.rs 新增 `pub(crate) mod helpers;` 并建立 mod.rs+`helpers/leaf.rs`，但必须决定 `MIN_LEAF`/`MAX_LEAF` 的归属：若继续放在 rope.rs，需改成 `pub(crate) const` 以供模块引用，同时注意避免循环依赖；另一选择是把常量与 helper 一并迁入 `helpers::leaf`，再在 rope.rs 中 `pub(crate) use` 只读常量。
  - 单元测试：rope.rs 现有测试覆盖面集中在 API 行为。若 helper 独立，应补齐针对换行偏好、UTF-8 边界、极端容量等情景的细粒度测试，并确保构造输入不依赖 `TreeBuilder` 才能真正覆盖纯函数逻辑。
  - `find_leaf_split_for_bulk`/`for_merge` 的签名目前是 `&str -> usize`，若 helper 将来要给泛型节点使用，需要考虑是否顺带提供 `&String`/`&mut String` 版本或抽象出“叶类型 trait”；否则泛型化时仍要包一层字符串专用 shim。
  - 方案未提及的相关调用点：`impl Leaf for String` 内部还处理 `push_maybe_split`、`shrink_to_fit` 等操作；若只拆分查找逻辑，仍有部分字符串专属细节留在 rope.rs，与 C# helper 的对照表需要同步记录未覆盖的差异。

- **风险评估**
  - **语义偏差**：C# 的 `SplitByCapacity`/`TryComputeBalancedSplit` 对换行窗口、Unicode 代理对的处理与 Rust 不一致；在没有统一的预期前直接抽离 helper 可能把“错位”的语义固化下来。
  - **常量漂移**：若 `MIN_LEAF`/`MAX_LEAF` 被复制到多个模块，将来调整容量策略时容易漏改，建议在 helper 内集中定义并通过公开常量或函数暴露给现有调用者。
  - **回归可见度**：计划中的 helper UT 尚未具体化；若仍只依赖顶层 Rope 测试，很难侦测“helper 细节被改坏”的问题，特别是 UTF-16 计数/换行搜索这类不易通过对外 API 直接观测的行为。
  - **文档同步**：Plan E 将刷新 skeleton / blueprint 作为最后阶段，但 skeleton 生成脚本 refresh_skeleton_docs.py 会抹去函数体，只保留签名；若 helper 放在新模块，需要确保脚本配置覆盖它，否则文档里可能缺少最新接口。

- **建议调整**
  - 在执行 Phase 1 前，先决定是否要把换行窗口/代理对策略与 C# 对齐；如果目标是共享语义，应当趁抽离之际把策略拉到同一个定义，再补测试证明行为一致。
  - 将 `MIN_LEAF`/`MAX_LEAF` 与 helper 同置，并新增 `pub(crate) fn min_leaf()` / `max_leaf()` 或直接 `pub(crate) use`, 避免 rope.rs 与 helper 相互引用导致模块编译顺序问题。
  - 把 `helper` 拆分成“拆分策略”和“度量工具”两个文件（或模块），保持职责单一：`find_leaf_split_*` 与 `push_maybe_split` 相关逻辑归一个模块，`count_utf16_code_units` 这类度量计算可以继续留在 `metrics` 系列或单独 helper，避免同一模块出现互不相关的功能。
  - 在 Phase 2 清单中加入“确认下游 `xi_core_lib`/`xi_plugin_lib` 等 crate 编译通过”的步骤，并通过 `cargo test -p xi-core-lib` 等命令验证；这比单纯检查导入路径更具保障。
  - 若最终确认为 Plan E 可行，执行完代码变更后务必运行文档与 skeleton 刷新脚本，并在 `docs/rust-refactor/rope-port-mapping.md` 记录 Rust/C# helper 的字段/函数对照表，确保跨语言团队能快速溯源。

- **后续关注**
  - 若下一步还计划把 `impl Leaf for String` 的其它逻辑（合并、借叶、Unicode 边界处理）迁移到 helper，需要提前设计 trait/API 形态，否则会不断重复“再拆一次”的工作量。
  - 在 Rust 端完成 helper 抽离后，建议与 C# `StringLeafOperations` 进行一轮实际数据对拍（字符界、换行、超长输入），确保两边输出一致，再考虑进入文档更新与测试固化阶段。

**总体评估**
- 方案 E 可以成立：叶片拆分/度量逻辑确实集中于 rope.rs，抽离到 `helpers::leaf` 后仍能保持外部 API 稳定，同时降低跨语言对齐门槛。
- 现状里 `count_newlines`/`len_utf8_from_first_byte` 已经是薄 shim，Phase 2 需要改成验证环节，而非重复构建；这样能把精力集中在真正未抽离的拆分逻辑。
- 由于 C# 可自由重写，完全可以配合 Plan E 重塑 `StringLeafOperations`，把换行窗口、代理对处理等策略与 Rust helper 保持一致。
- 如果目的是为后续泛型节点搭建共享 helper，这轮拆分应明确“字符串叶片 API 的完整集合”，避免只迁出查分函数却遗留其它字符串特化逻辑。
- 成功落地的关键在于：统一常量来源、补齐 helper 层的专用测试、串联文档与 skeleton 刷新，确保未来演进不会再次分叉。

**Rust 实施细节**
- 在 `rope/src` 下新增 mod.rs 与 `helpers/leaf.rs`，将 `find_leaf_split_*`、`count_utf16_code_units` 及相关常量迁入该模块，通过 `pub(crate)` 常量或 getter 向 rope.rs 暴露。
- 将 `impl Leaf for String`、`TreeBuilder::push_str` 中的拆分判断改为依赖 helper，并保留 `pub fn count_newlines` 等 shim 直接调用 helper/metrics。
- 新增 `#[cfg(test)] mod leaf_helpers_tests` 覆盖换行优先策略、代理对安全、极端容量与 UTF-16 计数等场景，避免只依赖顶层 Rope 行为测试。
- 检查 lib.rs、tree.rs 对常量和函数的引用，确保模块重组不会形成循环依赖；必要时通过 `pub(crate) use helpers::leaf::{…}` 暴露统一入口。
- 在完成迁移后运行 `cargo test -p xi-rope` 以及带 serde 功能的回归，确认拆分没有影响黄金夹具导出或现有序列化路径。
- 刷新 `docs/skeleton/*.md` 与 rope-port-mapping.md，让新模块进入自动化骨架与协作文档。

**C# 协同策略**
- 重写 `StringLeafOperations` 为 Plan E 的镜像：与 Rust helper 共用 `Min/MaxLeaf` 常量、换行偏好窗口与代理对处理；必要时通过泛型接口 `ILeafOperations<TLeaf>` 承载所有叶片操作。
- 在 C# 侧拆分逻辑中引入与 Rust 同步的 helper（例如 `LeafHelper.FindBalancedSplit`），并让 `Node.Generic`、`TreeBuilder` 等只通过该 helper 操作叶片。
- 补充 xUnit 测试覆盖拆分、合并、UTF-16 统计等路径，同时准备跨语言黄金序列或 property-based 测试，确保语义一致。
- 文档层面更新 node-generic-refactor-plan.md，记录 C# 新 helper 接口及与 Rust 映射；保证后续泛型化阶段引用同一套约束。
- 借助 `BenchmarkDotNet` 或最小性能测试，观察重写后叶片操作的 GC/分配情况，及时调整实现细节（如是否保留 `string.Create`）。
- 若计划将来支持不同叶类型，可把 `StringLeafOperations` 拆分为通用 `LeafPolicy<TLeaf>` + 字符串特化，提前为泛型节点扩展留出空间。

**风险与缓解**
- **语义漂移**：Rust 与 C# 的换行、代理对策略若不统一，未来仍会分叉；通过共享常量与跨语言测试对撞缓解。
- **常量漂移**：`MIN_LEAF`/`MAX_LEAF` 在多个模块重复，建议集中定义并通过 helper 暴露，确保调整时只改一个地方。
- **测试盲区**：若 helper 没有专属单元测试，小变动易漏测；引入 property-based 或参数化测试提升覆盖面。
- **生态影响**：`count_newlines` 等 shim 若意外改签名会破坏 `xi-core-lib` 等下游；完成迁移后记得运行核心 crate 编译与测试链路验证。
- **文档不同步**：若不刷新 skeleton 和 blueprint，跨团队共享知识会失效；把文档更新纳入 Definition of Done。
- **后续扩展**：只抽离部分字符串逻辑可能阻碍未来泛型化；应在设计上为其它叶操作（合并、借用、拆分）预留统一 helper 入口。

**建议的后续动作**
- 调整 Plan E 文档，将 Phase 2 定义为“验证 shim 与下游引用”并补充常量集中化、C# 对照的说明。
- 在 Rust 侧实施前先写出 helper 目标接口与测试用例草稿，确保落地时有明确验收标准。
- 同步更新 port-blueprint.md，把叶片 helper 模块列入跨语言协同清单，供后续会话追踪。
- 设计跨语言行为对拍（简单脚本或 JSON 基线），在两个实现都完成后执行，锁定未来回归检测机制。
- 将这轮拆分纳入 Stage D/C 的自动化脚本（如刷新 skeleton、运行回归测试），确保 CI/脚本能自动识别新结构。
