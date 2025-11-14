# Rope Generic Simplification (Plan G)

_草案建立：2025-11-15（最新修订同日）_

## 1. 背景与现状复盘
- 在 `reference/rust/rope/src/rope.rs` 中，字符串叶片逻辑（`const MIN_LEAF: usize = 511;`、`const MAX_LEAF: usize = 1024;`、`fn find_leaf_split_for_bulk`、`fn find_leaf_split_for_merge`、`fn find_leaf_split`、`fn count_utf16_code_units`）仍为内联自由函数。`impl Leaf for String`、`TreeBuilder::push_str`、`RopeInfo::compute_info` 以及 `Utf16CodeUnitsMetric::from_base_units` 直接依赖这些符号，缺乏集中式 helper。
- 通过 `runSubAgent`（2025-11-15）与 `rg "find_leaf_split" reference/rust/rope/src` 验证：上述函数皆仅在 `rope.rs` 内部引用，迁移到 helper 模块不会影响其他 crate。
- Rust 端 Rope 默认使用 **byte offset** 作为 Base Metric；`Metric<BaseMetric>` 路径中的偏移均为 UTF-8 字节位置。C# 端（`src/xi.Core/Rope/Tree/StringLeafOperations.cs`、`LeafSplitter.cs`）则以 **UTF-16 code unit**（`char`）为基准，并在 `StringLeafOperations.SplitByCapacity` 内提供换行偏好与代理对安全逻辑。
- C# 侧已有 `tests/xi.Core.Tests/StringLeafOperationsTests.cs` 覆盖克隆、插入、删除、换行偏好、代理对安全等行为；Rust 侧尚缺乏独立的 helper 测试，需要在模块化后补齐，以支撑跨语言对拍。
- Skeleton 文档目前由 `scripts/refresh_skeleton_docs.py` 驱动，尚未覆盖未来新增的 `helpers/string_leaf.rs`；在迁移完成后需刷新并确认输出。
- 因 fork 的目标是为 C# 迁移提供范本，warning 管控可按需调整，但仍优先保持简洁、易读、易复用的布局。

## 2. 设计目标
1. **稳定单一来源**：将字符串叶片的容量常量、拆分逻辑、UTF-16 统计统一放入 `helpers/string_leaf.rs`，提供唯一事实来源。
2. **拆分算法对齐**：保持 Rust/C# 叶片拆分流程在策略上等价（换行优先、代理对安全、容量阈值一致），供后续对拍脚本验证。
3. **跨语言说明清晰**：在文档、脚本输出及测试基线中显式标注 Base Metric 差异，避免混淆 byte 与 char 索引。
4. **自动化验证补强**：为 helper 新增轻量单元测试与（可选）属性测试，覆盖换行偏好窗口、代理对、极端容量、UTF-16 统计等场景。
5. **文档与工具同步**：更新 skeleton、蓝图与映射文档，确保 `scripts/refresh_skeleton_docs.py` 与 `docs/architecture/rope-port-mapping.md` 等资料同步反映结构调整。

## 3. 设计原则
- **模块化分层**：在 Rust 端新增 `rope/src/helpers/mod.rs` 与 `rope/src/helpers/string_leaf.rs`，对外仅暴露 `pub(crate)` API；`rope.rs` 仅作为对外 facade，保持原有入口函数名称。
- **窗口策略显式化**：统一在 helper 中声明单一常量（例如 `pub(crate) const NEWLINE_WINDOW: usize = MAX_LEAF - MIN_LEAF;`），C# 继续复用 `LeafSplitter.NewlinePreferenceWindow`，方便对拍脚本读取。
- **Base Metric 差异入档**：在文档、日志、脚本输出中附加“Rust offset == byte、C# offset == char”的显式提示。
- **测试无条件编译**：确保 helper 单元测试不依赖 `serde` 等可选特性，使 `cargo test -p xi-rope --no-default-features` 可持续通过。
- **未来扩展兼容**：保留潜在 `LeafSplitPolicy` 或 `SplitStrategy` 钩子，便于未来支持 `ArrayPool<char>` 等替代叶片实现。

## 4. 实施阶段
### Phase 0 — 基线确认与引用清点
1. 运行并保存基线测试输出：
   ```powershell
   cargo test -p xi-rope
   cargo test -p xi-rope --features serde subset_serialization_regression delta_serialization_regression engine_serialization_regression
   ```
   日志中需出现 `test result: ok`。
2. 执行调用者扫描：
   ```powershell
   rg "find_leaf_split" reference/rust/rope/src
   rg "count_utf16_code_units" reference/rust/rope/src
   ```
   或调用 `runSubAgent` 收集函数定义与引用，确认未超出 `rope.rs`。
3. 记录 C# 对应策略：在 `src/xi.Core/Rope/Tree/StringLeafOperations.cs` 与 `LeafSplitter.cs` 中摘录窗口大小（64）与代理对处理逻辑，写入本文附录。
4. 更新 `docs/architecture/rope-port-mapping.md`：在 Base Metric 章节添加“Rust 偏移=byte、C# 偏移=char”说明，并注明参考文件。

### Phase 1 — Rust Helper 落地 ✅（2025-11-15）
- [x] 在 `rope/src/helpers/` 下新增 `mod.rs` 与 `string_leaf.rs`，集中 `MIN_LEAF`/`MAX_LEAF`/`NEWLINE_WINDOW` 常量与拆分、UTF-16 计数逻辑。
- [x] `rope.rs` 改为通过 `helpers::string_leaf` 复用常量与函数，`TreeBuilder::push_str`、`Leaf::push_maybe_split`、`RopeInfo::compute_info` 等调用保留原行为。
- [x] `rope/src/lib.rs` 声明 `helpers` 模块，维持 crate 内访问路径不变。

### Phase 2 — 测试扩展 ✅（2025-11-15）
- [x] 在 `helpers/string_leaf.rs` 引入 `#[cfg(test)]` 模块，覆盖换行优先窗口、代理对安全、容量边界与混合 BMP/SMP UTF-16 计数场景。
- [x] 保持测试仅依赖标准库，默认与 `--no-default-features` 兼容。
- [x] `cargo test -p xi-rope` 及携带 `serde subset_serialization_regression delta_serialization_regression engine_serialization_regression` 特性的测试均通过。

### Phase 3 — 文档与骨架同步 ✅（2025-11-15）
- [x] 更新 `docs/architecture/rope-port-mapping.md`，记录 `helpers/string_leaf.rs` 映射关系与 UTF-8 byte vs UTF-16 `char` 偏移提示。
- [x] 更新 `docs/csharp-refactor/node-generic-refactor-plan.md` 与 `AGENTS.md`，同步 helper 抽离进度及对拍注意事项。
- [x] 运行 `python scripts/refresh_skeleton_docs.py --verbose`，确认 skeleton 文档纳入新模块（若脚本输出提示无差异亦记录结果）。

### Phase 4 — C# 策略对齐
> 维持“BaseMetric=char”的设计，但需显式注记与 Rust 的差异。
- [x] 在 `src/xi.Core/Rope/Tree/StringLeafOperations.cs`：
   - 直接复用 `LeafSplitter.NewlinePreferenceWindow` 作为唯一窗口常量，必要时通过内部转发属性暴露，无需新增副本。
   - 根据需要调整窗口值（例如与 Rust 一致或保留 64），并在 docstring 中说明依据。
   - 将代理对修正逻辑封装为独立方法，便于跨语言对拍脚本读取。
- [x] 复查 `tests/xi.Core.Tests/StringLeafOperationsTests.cs`，确认现有用例覆盖换行偏好与代理对安全；如需新增测试，确保断言描述包括期望拆分点。
- [x] 若需长期维护样本，可在 `tests/xi.Core.Tests/Fixtures/` 添加 `leaf_split_parity_samples.json`，记录输入文本、Rust 拆分点（byte）、C# 拆分点（char）、备注，并在 README 中描述更新流程。
- [x] 在 `docs/csharp-refactor/rope-cow-rebalance-plan.md` 或新文档中加入 Base Metric 差异说明，并引用本计划（新增 `Base Metric 对齐` 小节，引用 parity fixture 与 Plan G）。

Phase 4 parity fixture：`tests/xi.Core.Tests/Fixtures/leaf_split_parity_samples.json` 收录“newline_outside_char_window”（换行优先窗口差异）与“surrogate_guard_post_truncation”（代理对退让）两组跨语言拆分样本。

> 2025-11-15 更新：C# helper docstring 现显式说明“UTF-16 code unit vs UTF-8 byte”差异，并通过 `StringLeafOperations.NewlinePreferenceWindow` 转发 `LeafSplitter` 常量以保持单一来源。

### Phase 5 — 自动化与监测
1. 编写最小化对拍脚本（Rust/C#，可先行放置为独立工具）：
   - Rust 侧可通过 `cargo test -p xi-rope -- --ignored leaf_split_parity` 等形式临时导出拆分结果，避免新增常驻 bin；后续若确需独立 CLI 再行推广。
   - C# 侧使用 `dotnet test --filter LeafSplitParity` 或临时控制台应用打印结果，两端输出统一包含“offset unit”说明。
2. 在 `scripts/refresh_serialization_fixtures.ps1` 中仅留可选 hook（例如 `-RunLeafSplitParity`），默认不触发，防止常规流水线被冗长校验拖慢。
3. 若执行对拍脚本，记录关键指标（如换行偏好命中次数、平均叶片长度、最小/最大拆分点）并写入 `docs/architecture/rope-port-mapping.md` 的诊断章节。未执行时可留空，避免增加强制流程。

## 5. 产出清单
- Rust：`rope/src/helpers/mod.rs`、`rope/src/helpers/string_leaf.rs`、更新后的 `rope.rs` / `lib.rs` / `Cargo.toml`（若需）。
- 测试：`helpers/string_leaf.rs` 内部单元测试，必要时新增属性测试依赖。
- C#：`StringLeafOperations` 调整及扩展 UT，对拍脚本（可放入 `scripts/`）。
- 文档：`docs/architecture/rope-port-mapping.md`、`docs/csharp-refactor/node-generic-refactor-plan.md`、`AGENTS.md`、本 Plan G。
- 脚本：对拍/诊断脚本及在 `run_all_checks`/`refresh_serialization_fixtures.ps1` 中的挂钩。

## 6. 测试矩阵
| 阶段 | Rust | Script | C# |
| --- | --- | --- | --- |
| Phase 0 | `cargo test -p xi-rope`<br>`cargo test -p xi-rope --features serde subset_serialization_regression delta_serialization_regression engine_serialization_regression` | – | – |
| Phase 1/2 | 同 Phase 0 | – | – |
| Phase 3 | 同 Phase 0 | `python scripts/refresh_skeleton_docs.py` | – |
| Phase 4 | 同 Phase 0 | 对拍脚本（可选） | `dotnet test tests/xi.Core.Tests`（含新增 UT） |
| Phase 5 | 同 Phase 0 | `scripts/leaf_split_parity.ps1`（或等效 CLI） | `dotnet test` + 对拍 CLI |

## 7. 风险与缓解
| 风险 | 影响 | 缓解 |
| --- | --- | --- |
| Base Metric 差异引发误判 | 对拍结果显示 offset 不一致 | 文档/脚本明确声明 byte vs char；对拍时主要比较文本片段与切分边界字符。 |
| 新 helper 模块被 skeleton 脚本忽略 | 文档缺失、后续维护困难 | 在 Phase 3 验证 `refresh_skeleton_docs.py` 输出；必要时更新脚本过滤。 |
| 调试字段导致 `dead_code` 警告 | 破坏示例的“零额外噪音”目标 | 延后 `SplitOutcome` 等结构，引入时使用可选 feature 或 `#[cfg(debug_assertions)]` 包裹。 |
| C# 窗口常量重复定义 | 未来调参时漂移 | 复用 `LeafSplitter.NewlinePreferenceWindow`，如需不同常量则改为转发属性。 |
| 对拍脚本维护成本高 | 测试链路复杂化 | 将脚本纳入 `scripts/` 标准目录，并在 `AGENTS.md` 记录使用场景与维护人；默认不在常规流水线执行。 |

## 附录 A — 关键文件与命令
- Rust 当前实现：`reference/rust/rope/src/rope.rs`（约 200–350 行包含常量与拆分函数）。
- C# 对应实现：`src/xi.Core/Rope/Tree/StringLeafOperations.cs`、`src/xi.Core/Rope/Tree/LeafSplitter.cs`。
- 调用者扫描：`rg "find_leaf_split" reference/rust/rope/src`、`rg "count_utf16_code_units" reference/rust/rope/src`。
- 基线测试：`cargo test -p xi-rope`、`cargo test -p xi-rope --features serde subset_serialization_regression delta_serialization_regression engine_serialization_regression`。
- C# 单元测试：`dotnet test tests/xi.Core.Tests --filter StringLeafOperations`。
- Skeleton 刷新：`python scripts/refresh_skeleton_docs.py --verbose`。
- 可选对拍脚本占位：`cargo test -p xi-rope -- --ignored leaf_split_parity`、`dotnet test --filter LeafSplitParity`（待实现）。

## 附录 B — Base Metric 差异说明
- Rust：所有 `Metric<BaseMetric>` 调用以字节偏移为准，`find_leaf_split_*` 处理的索引即为 UTF-8 字节位置。
- C#：`StringLeafOperations` 与 `LeafSplitter` 使用 `string` 的 `char` 索引；拆分窗口以 UTF-16 code unit 计数。
- 对拍策略：
   - 输出对比时需标注单位（byte vs char）。
   - 若两侧拆分点不同但对应字符边界一致，应视为预期差异。
   - 关键指标对比（如换行优先是否生效、代理对是否安全）应关注逻辑结果而非绝对偏移值。

## 8. 后续展望
- 基于 helper 模块，可继续探索 `LeafSplitPolicy` trait，将字符串叶片实现与未来的 `ArrayPool<char>`/`RopeSlice` 等候选解耦。
- 若性能调优需要，可在 helper 中加入统计计数器（配合 `SharedNode` instrumentation）以评估拆分/合并频率。
- 在 Stage D 序列化资产之外，可考虑加入叶片拆分基准 JSON，支撑跨语言回归。
