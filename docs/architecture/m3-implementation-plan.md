# M3 实施计划：游标系统与泛型接口验证

> **版本**：1.0  
> **创建日期**：2025-11-16  
> **状态**：已批准（2025-11-16 星形会议决策）  
> **负责人**：AI 架构师 + C# Implementer + Rust Porter + Architecture Mapper  
> **预计工期**：15-20 天（约 2-3 周）  
> **测试基线**：114 项测试（M3 前 106 项 ✅ + 8 项泛型接口测试 ✅）

---

## 1. M3 目标概述

### 1.1 核心目标
实现游标系统与泛型节点接口验证，为 M4 完整切换打好基础，同时降级实现 Chunk 迭代器与 Grapheme 导航，确保核心编辑功能可用。

### 1.2 四大任务
1. **游标系统**：基于字符串特化 `Node.cs` 实现 `NodeCursor`，通过 81 项 Rope 测试 + 新增游标回归用例
2. **泛型接口验证**：`TreeBuilder.Generic.cs` + 8 项接口测试通过 ✅，M4 前保持双轨
3. **Chunk 迭代器骨架**：临时返回 `ReadOnlyMemory<char>`，非零拷贝但功能可用（3-4 天）
4. **Grapheme 降级实现**：Surrogate 安全 + 遥测（2 天）

### 1.3 交付物清单
- [ ] `NodeCursor.cs` 完整实现（基于字符串特化）
- [x] `TreeBuilder.Generic.cs` + 8 项泛型接口测试 ✅
- [x] `TypeAliases.cs`：`global using RopeNode = Node;` ✅
- [x] `Node.cs` 警告注释：禁止新增字符串特化 API ✅
- [ ] `RopeChunkEnumerator.cs` 骨架（非零拷贝）
- [ ] `RopeLineEnumerator.cs` 骨架
- [ ] Grapheme 降级实现 + 遥测计数器
- [ ] M3 回归测试套件（游标、泛型接口、Chunk、Grapheme）

### 1.4 成功标准
- **测试验收**：现有 114 项测试 + 新增游标/Chunk/Grapheme 测试全部通过
- **架构管控**：10 项管控措施全部落实（见 §5）
- **文档同步**：`port-blueprint.md`、`rope-port-mapping.md`、`type-system-migration-log.md` 反映最新状态
- **可回退性**：保留 `Node.cs` 字符串特化路径，M3 阶段不强制切换泛型

### 1.5 T0 前置事项与依赖
`T0` 定义为进入 T1-T4 子任务前必须具备的最小运行基线。外部评审确认以下事项需要写入时间线并纳入 owner/工时承诺。

| 项目 | 当前状态 | Owner | 预计工时 | 备注 |
|------|----------|-------|----------|------|
| NodeCursor 回归修复 | ✅ 已完成（2025-11-16 通过 `dotnet test Xi.Editor.sln --filter NodeCursorTests`） | C# Implementer | 已投入 0.5 天 | 解除“游标测试全红”假设，作为 T0 完成项记录。 |
| Rope 编辑版本计数器 | ⏳ 未开始 | C# Implementer + Architecture Mapper（文档约束） | 1 天 | `Rope` 层尚无 `editVersion`/`_versionTicket`，游标失效只能依赖 `ReferenceEquals`。需在 Node 编辑路径写入版本自增并记录到 `rope-port-mapping.md`。 |
| `export-serde-fixtures --cursor-descriptors` CLI | ⏳ 未开始 | Rust Porter | 1.5 天 | 需扩展现有 `export-serde-fixtures` 工具，导出 10 个 CursorDescriptor JSON（见 §3.2.4）。交付到 `tests/xi.Core.Tests/Fixtures/CursorDescriptors/`。 |
| Chunk Enumerator 骨架说明 | ⏳ 未开始 | C# Implementer | 1.5 天 | `RopeChunkEnumerator`/`RopeLineEnumerator` 仍停留在计划状态，缺少公开 skeleton 与 `IChunkEnumerator` 接口说明。 |
| Grapheme 降级骨架 + 遥测挂点 | ⏳ 未开始 | C# Implementer + Architecture Mapper | 1 天 | 需补充 `GraphemeNavigation` skeleton 及遥测计数器定义，才能落实 §2.4 子任务。 |

> **依赖完成顺序**：NodeCursor ✅ → Rope 版本计数器 → CLI/fixture → Chunk/Grapheme skeleton。后续子任务估时均假设这些依赖存在，如未按顺序满足需立即触发风险管理（见 §4）。

---

## 2. 任务分解与工作量估算

### 2.1 任务 1：游标系统实现

#### 2.1.1 子任务拆解
| 子任务 ID | 描述 | 负责人 | 工作量 | 依赖 |
|----------|------|--------|--------|------|
| T1.1 | 设计 `NodeCursor` 结构（基于字符串特化 `Node.cs`） | C# Implementer | 0.5 天 | - |
| T1.2 | 实现 `CursorDescriptor`（路径快照） | C# Implementer | 1 天 | T1.1 |
| T1.3 | 实现游标导航方法（`Next`/`Prev`/`SeekForward`/`SeekBackward`） | C# Implementer | 2 天 | T1.2 |
| T1.4 | 实现 Metric 转换（`ConvertFromBase`/`ConvertToBase`） | C# Implementer | 1 天 | T1.3 |
| T1.5 | 编写游标单元测试（round-trip、深层路径、编辑失效） | C# Implementer | 1.5 天 | T1.4 |
| T1.6 | 对齐 Rust `CursorDescriptor` Parity 样本（解析 10 个 JSON + 对齐状态 + 调试差异） | C# Implementer + Rust Porter | 2-2.5 天 | T1.5 |

**工作量小计**：6-8.5 天（调整理由：JSON 解析与差异调试需要更多时间，见 C# Implementer 评审）  
**风险点**：
- 游标缓存失效检测（需要 `Arc::ptr_eq` 等效实现）
- 深层路径性能（可能需要池化 `ValueListBuilder<int>`）

#### 2.1.2 实施策略
- **拥有型设计**：`NodeCursor` 持有 `SharedNode` 引用 + `List<int>` 路径缓存（父节点索引）
- **缓存失效**：通过版本号或 `ReferenceEquals` 检测编辑后失效
- **Metric 支持**：复用 `IMetric` 接口，通过 `Node.ConvertFromDefaultMetric`/`ConvertToDefaultMetric` 实现

#### 2.1.3 测试计划
- **基础测试**：空 Rope、单叶、多叶导航
- **Metric 对拍**：Base/Lines/Utf16 三类 Metric 的 `Next`/`Prev` 行为对齐 Rust
- **失效测试**：编辑后游标自动失效（抛出异常或返回 `false`）
- **深层路径**：100 层深度树导航性能测试

---

### 2.2 任务 2：泛型接口验证（已完成 ✅）

#### 2.2.1 完成情况
- [x] `TreeBuilder.Generic.cs` 实现 ✅
- [x] 8 项泛型接口测试（`GenericNodeInterfaceTests.cs`）✅
- [x] `TypeAliases.cs` 别名定义 ✅
- [x] `Node.cs` 警告注释 ✅

#### 2.2.2 M3 期间维护工作
| 子任务 ID | 描述 | 负责人 | 工作量 |
|----------|------|--------|--------|
| T2.1 | 监控泛型接口测试（确保不被破坏） | C# Implementer | 持续 |
| T2.2 | 设计 `INodeCursor<TInfo,TLeaf>` 接口 + 在 `NodeCursor` 中预留适配器挂钩 + 1-2 项泛型游标烟雾测试 | C# Implementer | 1 天 |

---

### 2.3 任务 3：Chunk 迭代器骨架

#### 2.3.1 子任务拆解
| 子任务 ID | 描述 | 负责人 | 工作量 | 依赖 |
|----------|------|--------|--------|------|
| T3.1 | 设计 `RopeChunkEnumerator` 结构（返回 `ReadOnlyMemory<char>`） | C# Implementer | 0.5 天 | T1.4 |
| T3.2 | 实现叶节点遍历（复用 `NodeCursor`） | C# Implementer | 1 天 | T3.1 |
| T3.3 | 实现分块逻辑（按 `MAX_LEAF` 上限拆分） | C# Implementer | 1 天 | T3.2 |
| T3.4 | 设计 `RopeLineEnumerator`（基于 `RopeChunkEnumerator`） | C# Implementer | 1 天 | T3.3 |
| T3.5 | 编写 Chunk/Line 迭代器单元测试 | C# Implementer | 0.5-1 天 | T3.4 |

**工作量小计**：3-4 天  
**降级说明**：
- 非零拷贝（叶片复制到 `ReadOnlyMemory<char>`）
- 不实现 Rust 的 `Cow<'a, str>` 借用语义
- M4 优化时再考虑 `Span` 或 P/Invoke Rust façade

#### 2.3.2 实施策略
- **临时方案**：每个叶片调用 `.AsMemory()` 返回 `ReadOnlyMemory<char>`
- **行迭代**：在 `RopeChunkEnumerator` 基础上按 `\n` 拆分
- **性能监控**：记录分配次数，为 M4 优化提供基准

---

### 2.4 任务 4：Grapheme 降级实现

#### 2.4.1 子任务拆解
| 子任务 ID | 描述 | 负责人 | 工作量 | 依赖 |
|----------|------|--------|--------|------|
| T4.1 | 实现 Surrogate 安全边界判定（`IsSurrogateBoundary`） | C# Implementer | 0.5 天 | - |
| T4.2 | 实现单叶 + 相邻叶 Grapheme 导航（`PrevGrapheme`/`NextGrapheme`） | C# Implementer | 1 天 | T4.1 |
| T4.3 | Code Point 回退逻辑（跨叶失败时降级） | C# Implementer | 0.5 天 | T4.2 |
| T4.4 | 添加遥测计数器（补片命中、code point 回退） | C# Implementer | 0.5 天 | T4.3 |
| T4.5 | 编写 Grapheme 导航测试（surrogate、emoji、跨叶） | C# Implementer | 0.5 天 | T4.4 |

**工作量小计**：2 天  
**降级风险**：
- 长 Grapheme 链（跨两片以上）会被拆分
- 需在 API 注释中明确说明限制

#### 2.4.2 实施策略
- **降级策略**（已在 `design-divergence-log.md` 登记）：
  1. 保证 UTF-16 surrogate 对不拆分
  2. 尝试补齐相邻叶片（最多 1 片）
  3. 若仍不足，回退到 code point 导航
- **遥测指标**：
  - `GraphemeNavigationMetrics.AdjacentLeafPatchCount`
  - `GraphemeNavigationMetrics.CodePointFallbackCount`
- **可插拔设计**：预留 `IGraphemeNavigator` 接口，M4+ 可替换为 ICU4N 或 Rust trace

---

## 3. 分工与协作机制

### 3.1 角色职责

| 角色 | 主要职责 | M3 期间关键任务 |
|------|----------|----------------|
| **AI 架构师** | 质量把关、冲突协调、里程碑决策 | - 每周检查进度<br>- 审查 `NodeCursor` 设计<br>- 验收 M3 交付物 |
| **C# Implementer** | 功能实现、单元测试 | - 实现 `NodeCursor`<br>- 实现 Chunk/Line 枚举器<br>- 实现 Grapheme 降级<br>- 编写测试 |
| **Rust Porter** | Rust 端支持、Parity 样本、算法咨询 | - 导出 `CursorDescriptor` 序列化样本（JSON fixture）<br>- 提供深树与多 Metric 导航测试用例<br>- 回答游标算法、Metric 转换、偏移计算疑问<br>- 审查 C# 实现与 Rust 语义的一致性 |
| **Architecture Mapper** | 文档同步、阻塞追踪 | - 更新 `rope-port-mapping.md`<br>- 更新 `type-system-migration-log.md`<br>- 记录新发现的阻塞项 |

> **修改者**：Rust Porter  
> **修改理由**：细化职责描述，明确样本为 JSON 格式，增加算法咨询范围  
> **修改时间**：2025-11-16

### 3.2 协作接口

#### 3.2.1 C# Implementer ↔ Rust Porter
- **输入（Rust → C#）**：
  - `CursorDescriptor` JSON fixture（至少 10 个，见 §3.2.4）
  - Metric 转换算法说明（`ConvertFromBase`/`ConvertToBase` 实现细节）
  - 深树导航样本（路径深度 > 4）
  - 失效检测样本（编辑后游标失效场景）
- **输出（C# → Rust）**：
  - 算法疑问提案（含 Rust 源码自查结果 + 具体场景 + 最小复现用例，见 §5.0 咨询预案）
  - Parity 测试失败报告（差异分析 + Descriptor JSON + 调试日志）
  - 性能观测数据（游标命中率、Chunk 分配次数）
- **同步频率**：每 2-3 天一次（通过认知档案 + 架构师中转）

> **修改者**：Rust Porter  
> **修改理由**：细化协作输入输出，明确 fixture 数量与场景覆盖  
> **修改时间**：2025-11-16

#### 3.2.2 C# Implementer ↔ Architecture Mapper
- **输入**：最新 `rope-port-mapping.md`、阻塞项状态
- **输出**：新增类型/文件、阻塞解除报告
- **同步频率**：每次完成子任务后更新

#### 3.2.3 架构师 ↔ 全员
- **输入**：进度报告、阻塞项、设计冲突
- **输出**：决策、优先级调整、资源调配
- **同步频率**：每 3-5 天一次星形会议

#### 3.2.4 Rust Porter 样本导出规格

**CursorDescriptor JSON Fixture 清单**（M3 期间交付）：

| 样本 ID | 描述 | 场景 | 覆盖点 |
|---------|------|------|--------|
| cursor_empty_rope.json | 空 Rope | `Rope::from("")` 在 pos=0 | 边界：空树 |
| cursor_single_leaf.json | 单叶 Rope | `Rope::from("hello")` 在 pos=0,3,5 | 基础：单节点 |
| cursor_multi_leaf.json | 多叶 Rope | 2-3 层树，多个位置 | 基础：缓存命中 |
| cursor_deep_tree.json | 深树 Rope | 深度 > 4 的树 | 关键：超出缓存深度 |
| cursor_base_metric.json | Base Metric | 各 Metric 在相同 Rope | Metric：BaseMetric |
| cursor_lines_metric.json | Lines Metric | 各 Metric 在相同 Rope | Metric：LinesMetric |
| cursor_utf16_metric.json | UTF-16 Metric | 含 emoji 的 Rope | Metric：Utf16CodeUnitsMetric |
| cursor_invalid_state.json | 无效游标 | 游标在 EOF 后 `next()` 失败 | 失效：`is_valid() == false` |
| cursor_after_edit.json | 编辑后失效 | Delta 应用后游标失效 | 失效：Arc 指针不匹配 |
| cursor_roundtrip.json | 往返测试 | `to_descriptor` → `restore` | 完整性：路径重建 |

**导出工具**：
- 扩展 `xi-editor-ph7/rust/rope/bin/export-serde-fixtures.rs`，新增 `--cursor-descriptors` 子命令
- 使用 `cursor_descriptor.rs` 测试用例生成 JSON（复用现有测试逻辑）
- 输出到 `tests/xi.Core.Tests/Fixtures/CursorDescriptors/*.json`

**时间表**：
- T1.6 任务期间（预计 1-1.5 天）
- 优先级：高（游标实现依赖样本）

> **修改者**：Rust Porter  
> **修改理由**：补充样本导出规格，明确场景覆盖与交付时间  
> **修改时间**：2025-11-16

---

## 4. 风险评估与缓解策略

### 4.1 技术风险

| 风险 ID | 描述 | 严重度 | 概率 | 触发条件 | 缓解措施 | 应急预案 |
|---------|------|--------|------|----------|----------|----------|
| R1 | 游标缓存失效检测不可靠（GC 压力） | 高 | 中 | `NodeCursorTests` 或遥测检测到假失效率 > 1% / 高频 `ReferenceEquals` 失效 | - 优先使用 `ReferenceEquals` + 版本号双重检测<br>- 引入 instrumentation 监控假失效率 | 若 `ReferenceEquals` 不可靠，完全依赖版本号（`Rope._editVersion`）；极端情况下游标不支持编辑失效（API 注释说明限制） |
| R2 | Chunk 迭代器分配过多（性能问题） | 中 | 高 | 压测脚本显示处理 1 MB 文本分配 > 5 MB LOH | - 记录基准数据<br>- M4 优化时再处理 | 接受临时性能损失，M3 不优化 |
| R3 | Grapheme 降级导致编辑行为与 Rust 不一致 | 中 | 中 | 遥测显示 `CodePointFallbackCount` / 交互 bug 超过 0.5% session | - 在 API 注释中明确说明<br>- 收集遥测数据 | 若实际触发频率高，M4 提前引入 ICU4N |
| R4 | Rust `CursorDescriptor` Parity 样本不足 | 低 | 低 | T1.6 启动时 `CursorDescriptors/*.json` < 10 份或缺深树样本 | - Rust Porter 导出 JSON fixture（含深树、多 Metric、边界用例）<br>- 使用 `cursor_descriptor.rs` 测试生成样本<br>- 提供 serde 导出工具（扩展 `export-serde-fixtures` bin） | - 若导出工具延迟，手动构造 Descriptor JSON<br>- C# 侧先用简单场景验证，复杂场景 M3 后期补充 |
| R8 | Rope 版本计数器缺失导致游标无法可靠失效 | 高 | 中 | 2025-11-18 前 `Rope` 编辑路径仍未更新 `_editVersion` | - 由 C# Implementer 在 `Rope.Edit`/`Node.Edit` 管线写入版本自增<br>- Architecture Mapper 在 `rope-port-mapping.md` 中追踪实现状态 | 临时退回到仅 `ReferenceEquals` 判定，并在 API 注释中声明编辑后需手动重建游标 |
| R9 | `export-serde-fixtures --cursor-descriptors` CLI 未落地 | 中 | 中 | 2025-11-19 前工具未合入 `xi-editor-ph7` 主支 | - Rust Porter 先提交最小 CLI patch，并在周同步会上演示输出格式<br>- Architecture Mapper 将 CLI 作为 Stage D 资产登记 | 由 C# 实现者手写 2-3 个最小 JSON 以解锁单测，其余样本延后补全 |
| R10 | Chunk/Grapheme 骨架缺失拖慢 T3/T4 | 中 | 中 | T3.1/T4.1 开始时仍无 `RopeChunkEnumerator` / `GraphemeNavigation` skeleton | - 在 2025-11-18 前提交 skeleton PR，并同步 `docs/skeleton/rope.md`<br>- 通过 Architecture Mapper 追踪 owner/工时 | 将 `Rope.Snapshot()` + `string` 操作作为临时实现，并推迟性能测试至 M4 |

> **修改者**：Rust Porter  
> **修改理由**：补充缓解措施细节，明确样本来源与应急预案  
> **修改时间**：2025-11-16

### 4.2 进度风险

| 风险 ID | 描述 | 严重度 | 概率 | 缓解措施 | 应急预案 |
|---------|------|--------|------|----------|----------|
| R5 | 游标实现超期（复杂度低估） | 高 | 中 | - 细化子任务（见 §2.1）<br>- 每日进度检查 | 削减 Metric 支持范围（仅 Base + Lines） |
| R6 | Chunk 枚举器与游标耦合过紧 | 中 | 低 | - `RopeChunkEnumerator` 依赖 `INodeCursor` 接口而非具体实现<br>- 若游标未完成，临时用 `TraverseLeaves()` 实现 Chunk 枚举<br>- Chunk 测试验证输出正确性，不依赖游标内部实现 | 完全回退到 `Rope.Snapshot()` + `string.Split()` |
| R7 | 测试用例编写不足 | 中 | 中 | - 每个子任务完成后立即补测试<br>- 不允许"先实现后测试" | 延后 1-2 天专项补充测试 |

---

## 5. 架构管控机制

### 5.0 Rust Porter 算法咨询预案

**预期疑问类型与响应策略**（基于 M3 任务范围）：

| 疑问类型 | 典型问题 | 响应方式 | 工作量估算 |
|----------|----------|----------|-----------|
| **游标路径缓存** | - 如何实现 `Arc::ptr_eq` 等效逻辑？<br>- 缓存大小选择（`SmallVec` vs `List`）？<br>- 深层路径性能优化？ | - 提供 Rust 代码注释翻译<br>- 说明 `ReferenceEquals` 语义差异<br>- 提供深树性能测试样本 | 0.5-1 天 |
| **Metric 转换算法** | - `ConvertFromBase`/`ConvertToBase` 边界处理？<br>- UTF-16 vs UTF-8 偏移转换差异？<br>- Lines Metric 在叶边界的行为？ | - 提供算法伪代码<br>- 指向 `metrics/` helper 实现<br>- 提供边界样本（surrogate、换行） | 0.5 天 |
| **游标导航语义** | - `next`/`prev` 在 EOF/BOF 边界的返回值？<br>- `next_leaf`/`prev_leaf` 与 `next`/`prev` 差异？<br>- Metric 驱动导航的累计规则？ | - 引用 `cursor_descriptor.rs` 测试<br>- 绘制状态机图（Metric 导航）<br>- 提供 round-trip 断言模式 | 0.5 天 |
| **失效检测机制** | - 编辑后游标如何失效？<br>- Delta 应用对路径缓存的影响？<br>- `is_valid()` 的判定条件？ | - 解释 COW 触发点（`ensure_unique`）<br>- 提供 Delta 应用前后的 Arc 指针变化样本<br>- 说明 `leaf = None` 失效模式 | 0.5 天 |
| **序列化与 Parity** | - Descriptor JSON 格式规范？<br>- 如何处理跨语言的整数溢出？<br>- 浮点数精度差异？ | - 提供 JSON Schema<br>- 明确字段语义（`position`/`offset_of_leaf`/`frames`）<br>- 说明无浮点数依赖 | 0.25 天 |

**响应时效承诺**：
- **紧急疑问**（阻塞开发）：24 小时内响应
- **常规疑问**：48 小时内响应
- **复杂调研**（需实验验证）：3 天内响应

**升级机制**：
- 若疑问涉及 Rust 端 bug 或设计缺陷，升级至架构师决策
- 若疑问指向 C# 侧降级方案，转交 Architecture Mapper 记录分歧

> **修改者**：Rust Porter  
> **修改理由**：补充算法咨询预案，明确疑问分类与响应时效  
> **修改时间**：2025-11-16

### 5.1 十项管控措施（2025-11-16 会议决策）

#### 文档维度
1. ✅ **`TypeAliases.cs` 全局别名**：`global using RopeNode = Node;` 已创建，M4 一键切换
2. ✅ **`Node.cs` 警告注释**：禁止新增字符串特化 API，引导使用 `RopeNode` 别名

#### 代码维度
3. ✅ **`TreeBuilder.Generic.cs` 泛型实现**：已完成，8 项接口测试通过
4. [ ] **`NodeCursor` 泛型挂钩**：预留 `INodeCursor<TInfo,TLeaf>` 接口（M3 不强制）
5. [ ] **Chunk/Line 枚举器接口化**：预留 `IChunkEnumerator<TLeaf>` 扩展点

#### 进度维度
6. [ ] **每周进度审查**：架构师检查实际进度 vs 计划，调整优先级
7. [ ] **阻塞项日报**：C# Implementer 每日更新阻塞项（通过认知档案）

#### 回退维度
8. [ ] **M3 分支管理**：主分支保持 `Node.cs` 字符串特化，M4 前不合并泛型切换
9. [ ] **回退测试计划**：若 M3 延期，可回退到 M2 基线（106 项测试）
10. [ ] **泛型切换验收门禁**：M4 前必须通过 81 项 Rope 测试 + 泛型接口测试

### 5.2 质量门禁

| 阶段 | 门禁条件 | 验收人 |
|------|----------|--------|
| **游标完成** | - `NodeCursor` 单元测试通过<br>- Rust Parity 样本对拍通过（≥10 个 JSON fixture，覆盖空树/单叶/深树/多 Metric） | AI 架构师 + Rust Porter |
| **Chunk 完成** | - `RopeChunkEnumerator` 测试通过<br>- 性能基准记录完成 | AI 架构师 |
| **Grapheme 完成** | - 遥测计数器验证通过<br>- API 注释完整 | AI 架构师 |
| **M3 验收** | - 114 项 + 新增测试全部通过<br>- 10 项管控措施落实 | AI 架构师 + Architecture Mapper |

### 5.3 现实基线状态（2025-11-16）

**测试现状**
- `dotnet test Xi.Editor.sln --filter NodeCursorTests` ✅，确认游标专项回归已恢复；尚未重新跑完整 114 项套件，因此“114 项 + 游标全绿”仍是目标值而非现状。

**缺失组件与差距**
1. **Rope 编辑版本计数器**：`Rope`/`Node` 编辑路径仍缺 `_editVersion` 或等效票据，M3 任务当前只能依赖 `ReferenceEquals` 检测失效。该缺口直接影响 R1、R8 风险，需要在 T0 阶段补齐。
2. **Cursor Parity CLI**：`export-serde-fixtures` 尚无 `--cursor-descriptors` 子命令，`tests/xi.Core.Tests/Fixtures/CursorDescriptors/` 为空，导致 T1.6 只能手工构造少量样本。
3. **Chunk/Grapheme Skeleton**：`RopeChunkEnumerator.cs`、`RopeLineEnumerator.cs`、`GraphemeNavigation` 遥测骨架仍为 TODO，计划表中的估时基于“骨架已存在”假设，与现实不符。

> 本小节取代原“全量测试已绿”的假设，供后续同步与周会引用；每次重新跑完 114 项测试或补齐依赖后需更新时间戳。

---

## 6. 评审机制

### 6.1 文档修改权限

| 文档类型 | 修改权限 | 审批流程 | 通知机制 |
|---------|---------|----------|----------|
| **本计划书**（`m3-implementation-plan.md`） | AI 架构师独占 | - 重大变更需星形会议<br>- 细节调整直接修改 | 更新后在 `AGENTS.md` 记录变更日志 |
| **映射表**（`rope-port-mapping.md`） | Architecture Mapper | - 新增文件/类型：直接修改<br>- 状态变更：与对应实现者确认后修改 | 每次修改在文档末尾记录修改者 + 理由 |
| **阻塞日志**（`type-system-migration-log.md`） | Architecture Mapper + 实现者 | - 新增阻塞项：实现者提出，Mapper 记录<br>- 解除阻塞：实现者验证，Mapper 更新 | 解除阻塞后通知架构师 |
| **分歧日志**（`design-divergence-log.md`） | Architecture Mapper | - 新增分歧：需架构师批准<br>- 追平决策：需星形会议 | 每次新增在会议纪要中说明 |

### 6.2 代码评审流程

#### 6.2.1 自主评审（C# Implementer）
- 每个子任务完成后：
  1. 运行 `dotnet test Xi.Editor.sln`
  2. 检查测试覆盖率（≥80%）
  3. 更新认知档案"最近完成"章节
  4. 向架构师提交评审请求

#### 6.2.2 交叉评审（架构师 + Rust Porter）
- 架构师触发条件：
  - 游标系统完成
  - 关键算法实现（如 Metric 转换）
  - M3 验收前
- Rust Porter 触发条件：
  - Parity 测试失败（提供 Rust 侧最小复现 + 调试指导）
  - 偏移计算疑问（提供算法注释与边界处理逻辑）
  - 游标缓存策略疑问（解释 `Arc::ptr_eq` 等效实现与失效检测）

> **修改者**：Rust Porter  
> **修改理由**：细化 Rust Porter 评审触发条件与响应方式  
> **修改时间**：2025-11-16

#### 6.2.3 评审要点
- **正确性**：算法逻辑与 Rust 一致（允许实现差异）
- **性能**：无明显性能退化（允许临时降级，需记录）
- **可维护性**：代码风格符合 C# 惯例，注释完整
- **测试充分性**：边界情况覆盖，Parity 样本对拍

---

## 7. 同步机制

### 7.1 认知档案同步

| 文档 | 更新触发条件 | 更新频率 | 责任人 |
|------|-------------|----------|--------|
| `csharp-implementer.md` | 完成子任务、遇到阻塞 | 每日 | C# Implementer |
| `rust-porter.md` | 提供 Parity 样本、回答疑问 | 每 2-3 天 | Rust Porter |
| `architecture-mapper.md` | 文档状态变更、新增阻塞项 | 每次文档修改后 | Architecture Mapper |
| `AGENTS.md` | 重大进展、里程碑完成 | 每周 | AI 架构师 |

### 7.2 进度同步

#### 7.2.1 日常同步
- **C# Implementer**：每日在认知档案记录进度（完成项 + 阻塞项）
- **Architecture Mapper**：每 2 天检查认知档案，更新映射表

#### 7.2.2 周会同步
- **频率**：每 5-7 天一次
- **形式**：星形会议（架构师 + runSubagent 召集各角色）
- **议程**：
  1. 进度对齐（实际 vs 计划）
  2. 阻塞项讨论
  3. 下周任务优先级
  4. 风险评估

#### 7.2.3 里程碑同步
- **触发条件**：游标完成、M3 验收
- **产物**：
  - 更新 `AGENTS.md` 工作日志
  - 更新 `port-blueprint.md` 里程碑状态
  - Architecture Mapper 输出总结报告

---

## 8. 回退策略

### 8.1 回退触发条件

| 场景 | 触发条件 | 决策人 |
|------|----------|--------|
| **游标超期** | T1 子任务总耗时 > 10 天 | AI 架构师 |
| **测试通过率低** | 新增测试通过率 < 80% | AI 架构师 |
| **性能退化严重** | 游标/Chunk 性能比 M2 基线慢 5 倍以上 | AI 架构师 |

### 8.2 回退方案

#### 8.2.1 部分回退
- **游标超期**：
  - 削减 Metric 支持（仅 Base + Lines）
  - 暂不实现 `CursorDescriptor` Parity 对拍
- **Chunk 超期**：
  - 临时使用 `Rope.Snapshot()` + `string.Split('\n')`
  - M4 再实现真正的枚举器

#### 8.2.2 全面回退
- **条件**：M3 总耗时 > 25 天（超期 25%）
- **方案**：
  1. 保留泛型接口测试（8 项 ✅）
  2. 回退到 M2 基线（106 项测试）
  3. 重新规划 M3（拆分为 M3a/M3b）

### 8.3 回退验证
- 运行 `dotnet test Xi.Editor.sln`
- 确认 M2 基线 106 项测试通过
- 记录回退原因与教训（更新 `AGENTS.md`）

---

## 9. 附录

### 9.1 关键文件清单

| 文件路径 | 描述 | 责任人 |
|---------|------|--------|
| `src/xi.Core/Rope/Tree/NodeCursor.cs` | 游标实现 | C# Implementer |
| `src/xi.Core/Rope/Tree/TreeBuilder.Generic.cs` | 泛型 Builder ✅ | C# Implementer |
| `src/xi.Core/Rope/TypeAliases.cs` | 全局别名 ✅ | C# Implementer |
| `src/xi.Core/Rope/RopeChunkEnumerator.cs` | Chunk 枚举器 | C# Implementer |
| `src/xi.Core/Rope/RopeLineEnumerator.cs` | Line 枚举器 | C# Implementer |
| `tests/xi.Core.Tests/NodeCursorTests.cs` | 游标测试 | C# Implementer |
| `tests/xi.Core.Tests/RopeChunkIteratorTests.cs` | Chunk 测试 | C# Implementer |
| `tests/xi.Core.Tests/GraphemeNavigationTests.cs` | Grapheme 测试 | C# Implementer |
| `tests/xi.Core.Tests/GenericNodeInterfaceTests.cs` | 泛型接口测试 ✅ | C# Implementer |

### 9.2 参考文档

| 文档 | 作用 |
|------|------|
| `docs/architecture/port-blueprint.md` | 总体蓝图与里程碑 |
| `docs/architecture/rope-port-mapping.md` | Rust/C# 类型映射 |
| `docs/architecture/type-system-migration-log.md` | 阻塞项追踪 + M3 会议决策 |
| `docs/architecture/design-divergence-log.md` | Rust/C# 分歧记录 |
| `docs/rust-refactor/CursorCache.md` | Rust 游标缓存设计 Phase 0-3 |
| `docs/csharp-refactor/node-generic-refactor-plan.md` | C# 泛型节点调研 |
| `xi-editor-ph7/rust/rope/tests/cursor_descriptor.rs` | Rust 游标 Descriptor 测试（Parity 样本来源）|

> **修改者**：Rust Porter  
> **修改理由**：补充 Rust 测试文件引用，明确 Parity 样本来源  
> **修改时间**：2025-11-16

### 9.3 术语表

| 术语 | 定义 |
|------|------|
| **游标（Cursor）** | 持有路径缓存的高效树遍历结构，避免重复查找 |
| **CursorDescriptor** | 游标的拥有型快照，可在编辑后重建游标 |
| **Chunk** | Rope 的叶片或分段，通常以 `MAX_LEAF` 为上限 |
| **Grapheme** | Unicode 字素簇（grapheme cluster），用户感知的"字符" |
| **Parity** | C#/Rust 行为一致性验证 |
| **降级实现** | 功能简化版，性能或语义与 Rust 有差异但满足基本需求 |

---

## 10. 变更日志

| 日期 | 版本 | 修改者 | 变更内容 |
|------|------|--------|----------|
| 2025-11-16 | 1.0 | Architecture Mapper | 初始版本，基于星形会议决策创建 |
| 2025-11-16 | 1.1 | Rust Porter | 评审修改：<br>- §3.1 细化 Rust Porter 职责（样本格式、算法咨询）<br>- §3.2.1 补充协作接口输入输出规格<br>- §3.2.4 新增 CursorDescriptor JSON 样本清单（10 个 fixture）<br>- §4.1 R4 增强缓解措施（样本导出工具、应急预案）<br>- §5.0 新增算法咨询预案（5 类疑问 + 响应时效）<br>- §5.2 游标验收补充 Parity 样本数量要求<br>- §6.2.2 细化 Rust Porter 评审触发条件<br>- §9.2 补充 Rust 测试文件引用 |
| 2025-11-16 | 1.2 | C# Implementer | 评审修改：<br>- §2.1.1 T1.6 工作量调整（1-1.5天 → 2-2.5天，增加 JSON 解析与调试缓冲）<br>- §2.2.2 T2.2 细化（0.5天 → 1天，补充接口设计与测试要求）<br>- §4.1 R6 增强缓解措施（接口隔离 + 降级备份方案）<br>- §4.1 R1 修正应急预案（版本号降级 + 假失效监控，而非全遍历）<br>- §3.2.1 明确 C# Implementer 输出包括算法疑问提案（含自查结果） |
| 2025-11-16 | 1.3 | Architecture Mapper | 外部评审回填：<br>- §1.5 新增 T0 依赖与 owner/工时<br>- §4.1 扩展风险表（触发条件 + R8-R10）<br>- §5.3 新增现实基线状态，澄清测试与缺失组件<br>- 变更日志同步当前版本 |

---

**维护人**：Architecture Mapper  
**审批人**：AI 架构师  
**最后更新**：2025-11-16  
**下次审查**：M3 中期（预计 2025-11-23）
