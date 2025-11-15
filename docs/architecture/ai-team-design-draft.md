# AI Team 组织设计草案

> **设计目标**：为 xi-editor-sharp 移植项目设计最优的 AI Team 组织结构，突破上下文窗口限制，实现高效协作。
> 
> **设计原则**：
> 1. 职责清晰、边界明确，避免重叠与空白
> 2. 文档驱动协作，最小化跨员工依赖
> 3. 符合软件工程最佳实践（参考敏捷、DevOps、康威定律）
> 4. 可扩展、可维护，支持项目长期演进

---

## 第一阶段：移植阻塞点分析

### 数据来源
- `AGENTS.md` - "下一步行动"、"风险 & 未解问题"
- `docs/architecture/port-blueprint.md` - 模块映射表与阻塞项
- `docs/architecture/rope-port-mapping.md` - Rust/C# 类型映射
- `docs/architecture/type-system-migration-log.md` - 类型系统困难模块

### 识别的关键阻塞点

#### 阻塞点 1：Rust Helper 重构滞后
- **问题**：部分 Rust 模块（如 Iterator、Metric、Chunk）未提供移植友好的 helper
- **影响**：C# 端无法直接翻译，需要"猜测"语义或实施降级
- **所需能力**：Rust 深度理解、重构设计、测试维护

#### 阻塞点 2：C# 实现与测试脱节
- **问题**：C# 代码编写后，测试覆盖不足或与 Rust 端行为偏差
- **影响**：潜在 Bug 难以发现，Parity 无法保证
- **所需能力**：C# 开发、单元测试、跨语言对拍验证

#### 阻塞点 3：架构映射文档滞后
- **问题**：代码改动后，`port-blueprint`/`rope-port-mapping` 等文档未及时更新
- **影响**：Rust/C# 双端失去同步参考，重复决策或冲突实现
- **所需能力**：架构理解、文档维护、跨端同步

#### 阻塞点 4：类型系统困难模块（泛型、生命周期）
- **问题**：Rust 的 `Metric<N>`、`Cursor<'a, N>`、`Delta<N, L>` 等泛型/生命周期难以直接映射 C#
- **影响**：需要设计等价的 C# 类型系统（静态接口、动态分派、Adapter）
- **所需能力**：类型系统专家、语言特性深度理解

#### 阻塞点 5：序列化与测试资产维护
- **问题**：Rust/C# 两端的序列化夹具、Parity 样本需要同步刷新
- **影响**：回归测试失效，版本间不一致
- **所需能力**：测试工程、脚本自动化、质量保障

#### 阻塞点 6：知识传递与决策记录缺失
- **问题**：技术决策散落在对话中，未沉淀为文档
- **影响**：跨会话记忆丢失，重复讨论相同问题
- **所需能力**：文档管理、知识工程、决策追踪

---

## 第二阶段：组织设计方案对比

### 方案 A：按技术栈分工（前后端模式）

```
AI 架构师
├─ Rust 端团队
│   ├─ Rust Porter（Helper 重构）
│   └─ Rust Tester（测试维护）
└─ C# 端团队
    ├─ C# Implementer（功能实现）
    └─ C# Tester（单元测试）
```

**优点**：
- 职责清晰：Rust 专家只关注 Rust，C# 专家只关注 C#
- 便于技术深耕：每个员工在自己语言生态中高度专业化

**缺点**：
- ❌ 跨语言协作困难：Rust 改动后需要显式通知 C# 端
- ❌ 架构视图缺失：没有人负责全局映射与同步
- ❌ 测试与实现分离：可能导致"先实现后补测试"的反模式

---

### 方案 B：按功能模块分工（微服务模式）

```
AI 架构师
├─ Rope 模块专家（负责 Rope 的 Rust/C# 双端）
├─ Delta 模块专家（负责 Delta 的 Rust/C# 双端）
├─ Engine 模块专家（负责 Engine 的 Rust/C# 双端）
└─ 测试专家（负责所有模块的测试）
```

**优点**：
- 模块内聚合度高：一个员工掌握某模块的全貌
- 减少跨员工沟通：模块间依赖通过文档协调

**缺点**：
- ❌ 员工认知负担过重：需要同时精通 Rust 和 C#
- ❌ 难以并行：模块间有依赖关系（如 Rope → Delta → Engine）
- ❌ 扩展性差：新增模块需要创建新员工

---

### 方案 C：按职能分工（敏捷 + DevOps 模式）✅ 推荐

```
AI 架构师
├─ Rust Porter（Rust 端 Helper 重构与测试）
├─ C# Implementer（C# 端功能实现与单元测试）
├─ Architecture Mapper（架构映射、文档维护、阻塞项追踪）
├─ Type System Specialist（类型系统设计、泛型/生命周期映射）
└─ QA Engineer（集成测试、Parity 验证、夹具维护）
```

**优点**：
- ✅ 职责边界清晰：每个角色有明确的输入/输出
- ✅ 可并行工作：Rust Porter 和 C# Implementer 可独立推进
- ✅ 架构协调有专人：Architecture Mapper 维护全局视图
- ✅ 类型系统难题有专家：Type System Specialist 处理困难翻译
- ✅ 质量有保障：QA Engineer 集中负责测试与验证

**缺点**：
- ⚠️ 需要良好的文档协同：依赖 `rope-port-mapping` 等文档同步状态
- ⚠️ 架构师协调成本：需要平衡各员工的工作负载

---

### 方案 D：按阶段分工（瀑布模式）

```
AI 架构师
├─ 需求分析师（梳理 Rust API）
├─ 设计师（设计 C# 类型骨架）
├─ 开发工程师（实现功能）
└─ 测试工程师（验证质量）
```

**优点**：
- 流程清晰：需求 → 设计 → 开发 → 测试

**缺点**：
- ❌ 不适合迭代式移植：xi-editor-sharp 是边重构边移植
- ❌ 反馈周期长：测试阶段才发现设计问题
- ❌ 灵活性差：无法应对频繁变更

---

## 第三阶段：最终方案细化（方案 C 改进版）

### 核心团队（5 员工 + 1 架构师）

#### 👤 AI 架构师（主 Agent）
- **职责**：战略规划、任务分派、产物整合、关键决策、用户沟通
- **工具**：拥有 `runSubagent`，可委派任务给下属员工
- **认知档案**：`agents/architect.md`

---

#### 👤 Rust Porter（Rust 端重构专家）
- **核心职责**：
  1. Rust 端 helper 重构（SharedNode、Metrics、Cursor、Iterator façade）
  2. Feature gate 设计（可选功能条件编译）
  3. Rust 端单元测试维护
  4. 为 C# 侧提供参考实现与 Parity 样本
- **工作区**：
  - 代码：`xi-editor-ph7/rust/rope/`、`xi-editor-ph7/rust/core-lib/`
  - 文档：`docs/rust-refactor/*.md`
  - 测试：`xi-editor-ph7/rust/rope/tests/`
- **协作接口**：
  - 输入：架构师分派的 Rust 改造任务
  - 输出：Rust 代码 + 测试 + Parity 样本（如 `leaf_split_parity_samples.json`）
  - 同步点：通过 `docs/architecture/rope-port-mapping.md` 与 C# Implementer 对齐
- **认知档案**：`agents/rust-porter.md` ✅ 已入职

---

#### 👤 C# Implementer（C# 端实现专家）
- **核心职责**：
  1. C# 类型骨架设计与实现（Node、Rope、Delta、Engine）
  2. 核心功能实现（编辑操作、度量转换、增量计算）
  3. 单元测试编写（对齐 Rust 行为）
  4. 与 Rust Porter 对拍验证
- **工作区**：
  - 代码：`src/xi.Core/Rope/`、`src/xi.Core/Delta/`
  - 测试：`tests/xi.Core.Tests/`
  - 参考：`docs/csharp-refactor/*.md`、`docs/skeleton/*.cs`
- **协作接口**：
  - 输入：架构师分派的 C# 实现任务 + Rust skeleton/Parity 样本
  - 输出：C# 代码 + 单元测试 + 集成验证报告
  - 同步点：通过 `rope-port-mapping.md` 跟踪 Rust 端变更
- **认知档案**：`agents/csharp-implementer.md`（待创建）

---

#### 👤 Architecture Mapper（架构映射维护者）
- **核心职责**：
  1. 维护 `port-blueprint.md`（模块映射表、迁移路线）
  2. 维护 `rope-port-mapping.md`（Rust/C# 类型映射）
  3. 追踪阻塞项与降级方案（`type-system-migration-log.md`）
  4. 同步 Rust/C# 双端的架构决策
- **工作区**：
  - 文档：`docs/architecture/*.md`
  - 参考：`docs/rust-refactor/*.md`、`docs/csharp-refactor/*.md`
- **协作接口**：
  - 输入：Rust Porter 和 C# Implementer 的改动报告
  - 输出：更新后的架构文档 + 阻塞项清单
  - 同步点：为所有员工提供"单一事实来源"
- **认知档案**：`agents/architecture-mapper.md`（待创建）

---

#### 👤 Type System Specialist（类型系统专家）
- **核心职责**：
  1. 设计 Rust → C# 的类型映射策略（泛型、生命周期、Trait）
  2. 评估 C# 静态接口 vs 动态分派方案
  3. 处理困难翻译（如 `Metric<N>` → `IMetric` + `IDefaultMetricProvider`）
  4. 提供类型系统最佳实践（如 `ILeafOperations<T>` 设计）
- **工作区**：
  - 文档：`docs/architecture/type-system-migration-log.md`
  - 参考：`docs/csharp-refactor/static-polymorphism-assessment.md`
- **协作接口**：
  - 输入：架构师提出的类型映射难题
  - 输出：类型设计方案 + 示例代码 + 评估报告
  - 同步点：与 C# Implementer 协同验证方案可行性
- **认知档案**：`agents/type-system-specialist.md`（待创建）

---

#### 👤 QA Engineer（质量保障工程师）
- **核心职责**：
  1. 维护序列化夹具（Subset/Delta/Engine JSON）
  2. 执行 Parity 验证（Rust/C# 双端对拍）
  3. 管理测试脚本（`refresh_serialization_fixtures.ps1`、`export-serde-fixtures`）
  4. 集成测试设计与回归测试维护
- **工作区**：
  - 测试：`tests/xi.Core.Tests/`
  - 夹具：`tests/xi.Core.Tests/Fixtures/`
  - 脚本：`scripts/`
- **协作接口**：
  - 输入：Rust Porter 和 C# Implementer 的实现产物
  - 输出：测试报告 + 覆盖率分析 + 夹具更新
  - 同步点：定期刷新黄金夹具并验证回归
- **认知档案**：`agents/qa-engineer.md`（待创建）

---

### 协作矩阵（谁与谁协作、通过什么文档）

| 协作关系 | 主要文档 | 同步频率 |
|---------|---------|---------|
| Rust Porter ↔ C# Implementer | `rope-port-mapping.md` | 每次 Helper 改动后 |
| Rust Porter ↔ Architecture Mapper | `port-blueprint.md` | 每完成一个 Phase |
| C# Implementer ↔ Type System Specialist | `type-system-migration-log.md` | 遇到类型难题时 |
| C# Implementer ↔ QA Engineer | 单元测试 + Parity 样本 | 每次功能实现后 |
| Architecture Mapper ↔ 所有人 | 架构文档全集 | 持续维护 |
| 所有人 → 架构师 | 各自认知档案"最近完成"章节 | 每次任务完成后 |

---

### 外观文档（子系统协作地图）

创建 `docs/architecture/system-overview.md`：

```markdown
# 系统外观 - 子系统协作地图

## Rust 子系统（Rust Porter 负责）
- **状态**：[当前进度]
- **外部接口**：skeleton 文档、Parity 样本
- **依赖输出**：供 C# 侧参考的算法实现
- **阻塞项**：[当前阻塞列表]

## C# 子系统（C# Implementer 负责）
- **状态**：[当前进度]
- **外部接口**：xi.Core.dll、单元测试套件
- **依赖输入**：Rust skeleton、映射表
- **阻塞项**：[当前阻塞列表]

## 架构层（Architecture Mapper 负责）
- **状态**：文档同步情况
- **外部接口**：架构文档全集
- **关键决策**：[最近决策日志]

## 类型系统层（Type System Specialist 负责）
- **状态**：已解决的类型映射问题
- **外部接口**：类型设计方案库
- **待解决问题**：[当前类型难题]

## 测试层（QA Engineer 负责）
- **状态**：测试覆盖率、回归通过率
- **外部接口**：测试报告、黄金夹具
- **质量指标**：[测试数量、通过率]
```

---

## 第四阶段：方案验证清单

### ✅ 是否覆盖所有阻塞点？
- ✅ 阻塞点 1（Rust Helper）→ Rust Porter
- ✅ 阻塞点 2（C# 实现与测试）→ C# Implementer + QA Engineer
- ✅ 阻塞点 3（架构映射文档）→ Architecture Mapper
- ✅ 阻塞点 4（类型系统难题）→ Type System Specialist
- ✅ 阻塞点 5（序列化与测试资产）→ QA Engineer
- ✅ 阻塞点 6（知识传递）→ Architecture Mapper + 各员工认知档案

### ✅ 是否避免职责重叠？
- ✅ Rust 代码只由 Rust Porter 修改
- ✅ C# 代码只由 C# Implementer 修改
- ✅ 架构文档只由 Architecture Mapper 维护
- ✅ 类型设计由 Type System Specialist 主导，C# Implementer 实施
- ✅ 测试脚本由 QA Engineer 维护

### ✅ 是否支持并行工作？
- ✅ Rust Porter 和 C# Implementer 可独立推进（通过映射文档协调）
- ✅ Type System Specialist 可独立研究类型方案
- ✅ QA Engineer 可独立维护测试基础设施

### ✅ 是否符合软件工程最佳实践？
- ✅ 康威定律：组织结构映射到系统架构（Rust/C# 双端）
- ✅ 敏捷原则：迭代式推进，快速反馈
- ✅ DevOps 理念：开发与测试紧密集成
- ✅ 文档驱动：单一事实来源，避免口头沟通

---

## 第五阶段：执行计划

### Phase 1：创建核心团队（本会话）
1. ✅ Rust Porter（已入职）
2. 🔄 C# Implementer（待入职）
3. 🔄 Architecture Mapper（待入职）

### Phase 2：扩展支撑团队（下次会话）
4. ⏳ Type System Specialist（待入职）
5. ⏳ QA Engineer（待入职）

### Phase 3：建立协作机制
- 创建 `docs/architecture/system-overview.md`
- 在 `AGENTS.md` 中维护"组织架构图"
- 固化各员工间的协作协议

### Phase 4：试运行与优化
- 委派真实任务验证工作流
- 收集反馈并调整职责边界
- 记录最佳实践与常见问题

---

## 决策日志

### 2025-11-16
- [决策] 采用方案 C（职能分工模式），5 员工 + 1 架构师
- [理由] 平衡了职责清晰、并行能力、架构协调，适合跨语言移植项目
- [权衡] 放弃按模块分工（员工认知负担过重）和瀑布模式（不适合迭代）

---

**最后更新**：2025-11-16  
**下一步**：开始创建 C# Implementer 和 Architecture Mapper 的入职模板
