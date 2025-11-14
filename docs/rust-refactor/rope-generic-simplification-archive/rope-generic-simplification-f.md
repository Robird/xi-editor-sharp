# Rope Generic Simplification (Plan F)

_草案建立：2025-11-15_

## 1. 背景与目标
- 先前的 Plan B/C/D/E 依次尝试：B 期望在 `rope.rs` 与 `metrics/` 之间重新拆分 helper，C/D 探讨引入 `helpers` 子模块并重排 `Leaf` 相关 trait，E 规划以 shim 形式保留旧 API。但这些草案均停留在概念层面，尚未交付可运行的 `helpers::leaf` 模块，也未建立统一的常量管理与测试机制。
- 现有 `rope.rs` 中的 `find_leaf_split_*`、`count_utf16_code_units` 等私有函数仍散落各处，而 `count_newlines` / `len_utf8_from_first_byte` 已经是指向 `metrics::*` 的薄 shim，Phase 2 的计划应转为“验证兼容性”而不是再次重构。
- C# 侧可完全重写，无历史负担；Rust 端应先给出清晰的 Helper 模块，再驱动 C# 与之对齐，确保跨语言长期维护更简单。
- Plan F 旨在用更小、更明确的增量完成“字符串叶片 Helper 模块化”，并同步巩固测试、常量管理与文档流程。

## 2. 当前状态快照
1. `rope.rs` 中仍保有：
   - 常量 `MIN_LEAF` / `MAX_LEAF`。
   - 私有函数 `find_leaf_split_for_bulk` / `find_leaf_split_for_merge` / `find_leaf_split`。
   - `count_utf16_code_units`（调用 `metrics::codepoint::count_utf16_code_units_bytes`）。
   - `impl Leaf for String` 与 `TreeBuilder::push_str` 直接依赖上述函数。
2. `count_newlines` / `len_utf8_from_first_byte` 已经转调 `metrics::*`，只需保留 shim。
3. C# `StringLeafOperations` 额外引入换行偏好窗口与代理对安全处理，与 Rust version 尚未统一，需要在计划中明确对齐策略。
4. 文档与 skeleton 刷新脚本就绪（`scripts/refresh_skeleton_docs.py`），但现有骨架文档不会自动包含未来的 `helpers::*` 模块，需要确保增量覆盖。

## 3. 核心原则
- **单一来源**：`MIN_LEAF` / `MAX_LEAF` 必须集中定义，不再复制到多个模块；Rust helper 对常量提供只读访问，C# 直接 mirror 即可。
- **职责划分**：拆分逻辑与 UTF-16 统计属于同一“字符串叶片工具箱”；换行统计仍放在 `metrics::lines` 模块，通过 shim 暴露。
- **测试优先**：在 helper 模块内补充针对换行偏好、代理对、极端长度、UTF-16 计数的单元测试，确保未来可以在不动顶层 API 的情况下验证行为。
- **跨语言一致**：计划明确 C# 对齐步骤（换行窗口、代理对策略、常量），并约定完成后进行跨语言对拍。
- **文档跟进**：骨架、蓝图、port-mapping 文档是 DoD 必选项，避免再出现文档缺口。

## 4. 实施阶段
### Phase 0 — 基线确认与引用清点
1. 运行 `cargo test -p xi-rope` 与 `cargo test -p xi-rope --features serde subset_serialization_regression delta_serialization_regression engine_serialization_regression`，确认当前基线稳定。
2. 搜集 `count_newlines`、`len_utf8_from_first_byte` 等 shim 的下游引用（`xi-core-lib::editor`、`xi-plugin-lib::state_cache` 等），建立兼容清单。
3. 记下 C# `StringLeafOperations` 中与 Rust 相冲突的策略：换行窗口（当前 64）、代理对调整、`SplitByCapacity` 差异，作为后续对齐的输入。

### Phase 1 — Helper 模块落地
1. 新增 `rope/src/helpers/mod.rs` 与 `rope/src/helpers/leaf.rs`，内容包括：
   - `pub(crate) const MIN_LEAF: usize` / `MAX_LEAF`（或提供 `pub(crate) fn min_leaf()` 等访问器）。
   - `pub(crate) fn find_leaf_split_for_bulk(...)`、`find_leaf_split_for_merge(...)`、`find_leaf_split(...)`。
   - `pub(crate) fn count_utf16_code_units(...)`。
   - 如需支持后续扩展，可预留 `pub(crate) struct LeafSplitDecision` 等中间数据结构。
2. 调整 `rope.rs`：
   - 删除原常量与函数定义，改为 `use crate::helpers::leaf::{...};`。
   - `impl Leaf for String` 与 `TreeBuilder::push_str` 改用 helper。
   - `pub fn count_newlines` / `pub fn len_utf8_from_first_byte` 保持现有 shim 实现。
3. 更新 `lib.rs`，增加 `pub(crate) mod helpers;`。
4. 同步更新 `rope/src/bin` 或其它引用文件（若存在）以使用新常量。

### Phase 2 — 测试与验证强化
1. 在 `rope/src/helpers/leaf.rs` 内加 `#[cfg(test)] mod tests`，覆盖：
   - 超过 `MAX_LEAF` 的字符串拆分，验证换行优先与代理对安全。
   - 仅字符边界可分割的场景。
   - `find_leaf_split_for_merge` 对称性（左右交换、极端长度）。
   - `count_utf16_code_units` 的代理对与 Emoji 样例。
2. 现有 `LinesMetric` / `Utf16CodeUnitsMetric` 测试需验证 helper 拆分后仍能通过。必要时扩充测试数据，确保 `TreeBuilder::push_str` 在大文本输入下保持叶片容量约束。
3. Phase 0 中的引用清单全部编译通过，确保 shim 未破坏下游。
4. 再次执行 Phase 0 的测试命令确认无回归。

### Phase 3 — 文档与骨架更新
1. 运行 `python scripts/refresh_skeleton_docs.py`，确保新 helper 模块纳入 `docs/skeleton/*.md`。
2. 更新以下文档：
   - `docs/architecture/rope-port-mapping.md`: 新增 “helpers::leaf” 小节，列出暴露函数与 C# 对应。
   - `docs/csharp-refactor/node-generic-refactor-plan.md`: 标注字符串叶片 Helper 任务状态、常量对齐策略。
   - `AGENTS.md`: 同步“当前聚焦事项”或“下一步行动”。
3. 若脚本未捕获 helper 函数签名，需要手动补充 skeleton 条目或调整脚本过滤规则。

### Phase 4 — C# 对齐与跨语言校验
> C# 目前可重写，因此此阶段可视为后续动作，但需在计划中明确。
1. 重写 `StringLeafOperations`：
   - 直接复用 `MIN_LEAF` / `MAX_LEAF` 常量值。
   - 拆分逻辑与 Rust helper 一致，必要时引入共享测试数据。
   - 将换行窗口与代理对策略同步为 Rust 版本（若 Rust 也更新窗口值，保持一致来源）。
2. 增加 C# 单元测试，覆盖拆分、合并、UTF-16 统计、代理对等路径。
3. 设计跨语言回归：通过导出/解析测试数据（JSON 或 CSV），验证两侧对相同输入的拆分结果一致。
4. 文档更新：在 `docs/architecture/port-blueprint.md` 与 `docs/csharp-refactor` 系列中记录完成状态与对照表。

## 5. 产出清单
- 新增 `rope/src/helpers/mod.rs`、`rope/src/helpers/leaf.rs`。
- 更新后的 `rope.rs`、`lib.rs` 等文件。
- helper 专属单元测试。
- 刷新后的 `docs/skeleton/*.md`、`docs/architecture/rope-port-mapping.md`、`docs/csharp-refactor/node-generic-refactor-plan.md`、`AGENTS.md`。
- C# 同步作业（阶段 4 完成后）：重写 `StringLeafOperations`、增量测试、跨语言对拍脚本。

## 6. 测试矩阵
| 阶段 | Rust | Script | C# |
| --- | --- | --- | --- |
| Phase 0 | `cargo test -p xi-rope`<br>`cargo test -p xi-rope --features serde subset_serialization_regression delta_serialization_regression engine_serialization_regression` | – | – |
| Phase 1/2 | 同 Phase 0 | – | – |
| Phase 3 | 同 Phase 0 | `python scripts/refresh_skeleton_docs.py` | – |
| Phase 4 | 同 Phase 0 | `python scripts/refresh_serialization_fixtures.py`（如需验证 JSON 未变化） | `dotnet test tests/xi.Core.Tests`（新增拆分测试） |

## 7. 风险与缓解
| 风险 | 影响 | 缓解措施 |
| --- | --- | --- |
| 常量漂移（MIN/MAX_LEAF 在多处定义） | 后续改动易漏掉某一端 | Helper 内集中定义常量，C# mirror；写入文档标注唯一来源。 |
| Helper UT 缺失 | 拆分逻辑回归难以捕捉 | 在 Helper 模块内增加高覆盖度单元测试，必要时使用 property-based 测试。 |
| Shim 兼容性被破坏 | `xi-core-lib` 等下游编译失败 | Phase 0 记录引用，Phase 2 验证编译与功能；CI 运行核心 crate 测试。 |
| 文档未同步 | 团队无法快速理解新模块 | 将文档刷新作为 DoD；使用 `AGENTS.md` 跟踪完成状态。 |
| 跨语言语义不一致 | C# 与 Rust 行为继续分叉 | Phase 4 设计跨语言对拍，明确窗口/代理策略来源一致。 |
| future 型特化需求（泛型叶片） | Helper 无法复用 | 在 Helper 中保留 `pub(crate)` API 形式，后续可通过 trait 封装；Plan F 结束后评估是否继续抽象。 |

## 8. 后续展望
- 完成 Plan F 后，下一步可评估将 `impl Leaf for String` 进一步精简，使其只承载 `Leaf` trait 所需的 glue 逻辑，其余全部委托 Helper。
- 若未来要支持多叶类型，可在 Helper 模块基础上扩展 `trait LeafPolicy`，并把字符串实现迁移到 `StringLeafPolicy`，为泛型节点最终合流做好准备。
- 针对跨语言对拍，建议在阶段性 CI 中加入 JSON 基线或小型脚本，持续验证拆分结果；这可作为 Stage D（共享夹具刷新）之后的新自动化项目。

读取 [](file:///e%3A/repos/Atelia-org/xi-editor-sharp/xi-editor-ph7/rust/rope/src/rope.rs)

搜索文本 `MIN_LEAF` (`**/xi-editor-ph7/rust/rope/**`)，无结果

搜索文本 `MAX_LEAF` (`**/xi-editor-ph7/**`)，无结果

搜索正则表达式 `MIN_LEAF` (`**/xi-editor-ph7/**`)，无结果

读取 [](file:///e%3A/repos/Atelia-org/xi-editor-sharp/xi-editor-ph7/rust/rope/src/lib.rs)

搜索文本 `MAX_LEAF` (`**/xi-editor-ph7/rust/rope/src/**`)，无结果

读取 [](file:///e%3A/repos/Atelia-org/xi-editor-sharp/xi-editor-ph7/rust/rope/src/tree.rs#1-1)，行 1 到 201

搜索文本 `count_utf16_code_units`，20 个结果

读取 [](file:///e%3A/repos/Atelia-org/xi-editor-sharp/xi-editor-ph7/rust/rope/src/metrics/mod.rs)

已在 代码库 中搜索 "find_leaf_split_for_merge"、17 结果

读取 [](file:///e%3A/repos/Atelia-org/xi-editor-sharp/xi-editor-ph7/rust/rope/src/breaks.rs#1-1)，行 1 到 201

搜索匹配 `**/MAX_LEAF*` 的文件，无匹配项

读取 [](file:///e%3A/repos/Atelia-org/xi-editor-sharp/xi-editor-ph7/rust/rope/src/metrics/codepoint.rs)

读取 [](file:///e%3A/repos/Atelia-org/xi-editor-sharp/src/xi.Core/Rope)

搜索与 `**/StringLeafOperations*.cs` 匹配的文件，2 匹配项

读取 [](file:///e%3A/repos/Atelia-org/xi-editor-sharp/src/xi.Core/Rope/Tree/StringLeafOperations.cs)

**调研结论**
- 已确认 rope.rs 内的 `MIN_LEAF`、`MAX_LEAF` 与 `find_leaf_split_*`、`count_utf16_code_units` 都是模块私有，当前只在该文件内被调用，抽离到 `helpers::leaf` 不会影响对外 API。
- lib.rs 已 `pub(crate) mod metrics;`，新建 `helpers::leaf` 后可直接复用 `metrics::codepoint::count_utf16_code_units_bytes` 与 `memchr`，不会产生循环依赖。
- rope.rs 的内联与 serde 测试通过 `use super::*;` 访问常量，迁移后需在文件顶部 `pub(crate) use crate::helpers::leaf::{MAX_LEAF, MIN_LEAF, ...};` 以保证测试继续编译。
- Helper 单测可覆盖换行优先与代理对边界，但须留意 Rust 当前窗口等同于 `splitpoint - minsplit`（约 500+ 字节），与 C# `StringLeafOperations.SplitByCapacity` 的 64 字符窗口存在差异，需要在用例或文档中声明。
- refresh_skeleton_docs.py 会遍历 `xi-editor-ph7/rust/rope/src/**`，新增的 `helpers/` 将被收录；仍建议执行一次验证脚本过滤规则不会误删该模块。

**主要风险**
- C# 侧的换行窗口与代理对逻辑仍与 Rust 不同，若计划在 Phase 4 对拍，需要先决定是否同步 Rust 策略，否则 parity 目标会落空。
- 若在 rope.rs 之外仍保留旧常量定义（如漏改 `TreeBuilder::push_str`），将破坏“单一来源”，后续调整阈值仍会发生漂移。
- Helper 测试需保持 feature 无关，以免在 `cargo test -p xi-rope --no-default-features` 或 `--features serde ...` 组合下编译失败。
- 若未来考虑公开 helper API，应提前确定命名空间与可见性，避免与 `metrics` 中的同名函数或外部调用习惯冲突。

**建议动作**
1. 在 Phase 1 实现时于 rope.rs 使用 `pub(crate) use crate::helpers::leaf::{MAX_LEAF, MIN_LEAF, find_leaf_split_for_bulk, ...};`，确保子模块测试零改动通过。
2. 为 helper 新增单测同时记录“换行窗口 = splitpoint - minsplit”并在 rope-port-mapping.md 内注记，方便后续决定是否收敛到固定窗口策略。
3. 先执行 `python scripts/refresh_skeleton_docs.py` 检查 Markdown 骨架是否正确包含 `helpers::leaf`，若被过滤需同步调整脚本。
4. 评估是否保留 `count_utf16_code_units` 这一薄封装，或直接在 `RopeInfo::compute_info` 等位置调用 `metrics::codepoint::count_utf16_code_units_bytes`，并在文档中明确 C# mirror 应该跟随的入口。
