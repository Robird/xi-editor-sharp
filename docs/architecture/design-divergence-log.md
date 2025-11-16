# C# Port Design Divergence Log

> 维护与 Rust 原版实现刻意不对齐的功能/架构决策，便于后续审查是否需要追平或继续分叉。记录项需说明背景、决策内容、影响范围与复盘点。

| 日期 | 模块 | 决策摘要 | 原版对照 | 影响范围 | 后续观察点 |
|------|------|-----------|-----------|-----------|-------------|
| 2025-11-13 | Rope 叶片存储 | 继续使用 UTF-16 `string` 作为叶片介质，不复刻 Rust 的 UTF-8 `Arc<String>` 存储；`StringLeafOperations` 以 UTF-16 计量，必要时在 helper 中做 UTF-8 对拍 | Rust 叶片以 UTF-8 字节为主，所有偏移与容量计算依赖字节单位 | 影响 `StringLeafOperations`, `LeafSplitter`, Rope 度量及所有以 `char` 为单位的 API；跨语言对拍需额外转换偏移单位 | 监控：若未来需要 byte-level 精准度（如外部 protocol），评估引入 UTF-8 存储或双轨 helper；保持 `leaf_split_parity_samples.json` 用于捕获偏差 |
| 2025-11-15 | Grapheme 导航 | C# 初版仅保证不拆分 surrogate，对上下文最多补一片并在不足时退回 code point；暂不实现完整 `GraphemeCursor` 状态机 | Rust 使用 `unicode_segmentation::GraphemeCursor`，可跨多叶拼接上下文并遵循 UAX #29 | 影响所有基于 grapheme 的光标/退格/选择操作；Stage D 若引入 grapheme fixtures 需条件标注 | 监控：`GraphemeNavigationMetrics` 已统计 fallback/补片命中率，但 0.5% 遥测阈值仍待 AI 架构师裁决；架构师 + Architecture Mapper 需在 2025-11-20 前确定阈值并回写 `m3-implementation-plan.md` §4/§5.3。 |
| 2025-11-16 | Rope Chunk/Line 枚举与 Grapheme Telemetry | 首版 `EnumerateChunks/EnumerateLines` 通过复制叶片/拼接字符串暴露 `ReadOnlyMemory<char>`；`DegradedGraphemeNavigator` 使用 .NET `StringInfo` + 单邻叶上下文 + code point 回退，并记录遥测计数 | Rust 端 chunk/line 迭代器均零拷贝返回叶片切片，Grapheme 依赖 `GraphemeCursor` 完整算法且无需遥测 | 影响 `RopeChunkEnumerator`、`RopeLineEnumerator`、`IGraphemeNavigator`、对应单测；性能暂不达标 | TODO：保持复制语义 + Diagnostics（`RopeChunkEnumeratorDiagnostics`, `GraphemeNavigationMetrics`）降级策略，并在 CLI schema/1 MB 基准/遥测阈值落地后复盘是否要切换到零拷贝与完整 Grapheme 行为。 |
| 2025-11-16 | Rope Parity Fixtures | 新增 chunk/line/grapheme parity 测试消费 Rust Porter 导出的 JSON，但仅覆盖当前复制型迭代器与降级 Grapheme 导航；零拷贝 span 与 ICU 级行为仍未验证 | Rust 端以零拷贝 chunk view 与 `GraphemeCursor` 作为基准 | 影响 `RopeChunkParityTests`、`RopeLineEnumeratorTests`、`GraphemeNavigatorParityTests` 解释结果时需记住仍是降级实现 | TODO：等零拷贝与 ICU 行为落地后扩展 fixture schema 并补充新的 parity 断言 |

## 维护指引
- 仅记录“明确选择与 Rust 差异化且短期不会追平”的决策；临时 workaround 或 Bug 待办不在此列。
- 新增记录时请更新 `AGENTS.md` 决策日志，确保跨会话记忆同步。
- 每个版本或里程碑节点复盘一次此表，判断是否有项需要升级为统一实现。

## C#版与原版的明确不同设计选择

### 关于字符串编码
- C#版用UTF-16编码
- 以 UTF-16 计量
- 保证 UTF-16 surrogate 对不被拆分.

### 关于GraphemeCursor 
- **初版策略**：C# 侧首轮移植仅保证 UTF-16 surrogate 对不被拆分，遇到 grapheme 边界判定时最多向前/向后补足一个相邻叶片；若仍无法确认边界，则回退至单 code point 导航。
- **动机**：
	- **安全性**：受限上下文可防止恶意构造的超长 grapheme 诱发无限拼接或过度内存占用。
	- **覆盖常态场景**：常见文本（中日韩、拉丁、Emoji 组合）通常落在同一叶或毗邻叶片内，单叶 + 相邻叶即可满足大多数人工/LLM 场景。
	- **实现复杂度**：不需要引入 ICU/ICU4N 或复刻 `GraphemeCursor` 状态机，易于在当前 Rope 基础上落地并复用 existing UTF-16 helper。
	- **扩展点保留**：接口会暴露上下文抽象，允许后续替换为更完整的 grapheme 导航实现或接入 Rust trace。
- **约束**：
	- 跨越两片以上的合法 grapheme（长 combining 串、极长 ZWJ 链等）会被拆分，光标行为与 Rust 不一致。
	- Stage D 若引入 Rust 端 grapheme fixtures，需在测试中标记此限制或提供条件跳过。
	- 未来若真实需求覆盖此类长序列，计划再回到“Rust helper + trace”路线重新实现。