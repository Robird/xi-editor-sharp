# AI 协作工具参考文档

本文档列出 AI 助手可用的所有工具及其使用方法，便于人类协作者理解 AI 的能力边界与最佳协作模式。

---

## 1. PowerShell 命令执行 (`powershell`)

### 功能概述
在交互式 PowerShell 会话中运行命令，支持同步/异步执行、会话持久化与超时控制。

### 核心参数
- **command** (必需): 要执行的 PowerShell 命令字符串
- **description** (必需): 命令的简短描述（≤100 字符），如 "List files in the current directory"
- **sessionId** (必需): 会话标识符，同一 ID 的多次调用共享状态（环境变量、工作目录等）
- **async** (必需): 布尔值
  - `false`: 同步等待命令完成，适合短时或需立即结果的操作
  - `true`: 异步执行，适合长时运行、交互式工具或需多步输入的场景
- **timeout** (可选): 同步模式下的最大等待秒数（默认 30 秒，最大 600 秒）

### 使用指南

#### 同步模式 (`async: false`)
适用场景：
- 快速查询（`git status`、`ls`、`cat`）
- 构建/测试任务（需配合足够的 `timeout`）
- 单次命令执行

示例：
```json
{
  "command": "dotnet test Xi.Editor.sln",
  "description": "Run all solution tests",
  "sessionId": "test-session",
  "async": false,
  "timeout": 300
}
```

**注意事项**：
- 长时运行命令（如 `dotnet restore`、`npm install`）务必设置足够的 `timeout`
- 若超时，可用 `read_powershell` 继续等待同一 `sessionId` 的输出
- **必须禁用分页器**（如 `git --no-pager`），否则会挂起等待交互

#### 异步模式 (`async: true`)
适用场景：
- 交互式工具（如 CLI 向导、REPL）
- 长时运行服务（如 `dotnet watch`、开发服务器）
- 需要多步输入的命令
- 调试器（GDB、LLDB）

工作流：
1. 用 `powershell` 启动异步命令
2. 用 `write_powershell` 发送输入（文本或键盘事件如 `{enter}`、`{down}`）
3. 用 `read_powershell` 读取最新输出
4. 必要时用 `stop_powershell` 终止会话

示例（交互式选择）：
```json
// 步骤 1: 启动工具
{"command": "npx create-react-app", "sessionId": "interactive", "async": true}
// 步骤 2: 发送向下箭头 + 回车选择选项
// (使用 write_powershell)
```

#### 命令链
支持 `&&` 串联多个依赖命令：
```powershell
git --no-pager status && git --no-pager diff
```

### 最佳实践
- 为每类任务使用独特的 `sessionId`（如 `"build"`、`"test"`、`"git-ops"`）
- Windows 环境务必用反斜杠路径（`\`）
- 优先使用 PowerShell 原生命令（`Get-ChildItem` 而非 `dir`）
- 禁用交互式分页器以避免挂起

---

## 2. PowerShell 输入写入 (`write_powershell`)

### 功能概述
向异步 PowerShell 会话或正在运行的命令发送输入（文本或键盘事件）。

### 核心参数
- **sessionId** (必需): 必须匹配已启动的异步 `powershell` 会话 ID
- **input** (必需): 要发送的内容，支持：
  - 普通文本（如 `"y"`, `"hello world"`）
  - 键盘事件：`{enter}`, `{up}`, `{down}`, `{left}`, `{right}`, `{backspace}`
  - 组合（如 `"my input{enter}"` 表示输入文本后按回车）
- **delay** (可选): 发送后等待的秒数，再返回输出（默认值未明确，建议 ≥10 秒）

### 典型用例
1. **确认提示**：Maven 安装需要确认时发送 `"y"`
2. **菜单导航**：CLI 工具展示列表时用 `{down}{down}{enter}` 选择第三项
3. **表单填写**：逐字段输入并用 `{enter}` 提交

### 注意事项
- 只能用于 `async: true` 的会话
- `delay` 应根据工具响应速度调整（交互式向导建议 ≥10 秒）
- 输出在 `delay` 后返回，可能需要后续 `read_powershell` 继续读取

---

## 3. PowerShell 输出读取 (`read_powershell`)

### 功能概述
读取异步 PowerShell 会话自上次读取以来产生的新输出。

### 核心参数
- **sessionId** (必需): 匹配异步会话 ID
- **delay** (可选): 等待指定秒数后再读取（用于给命令更多执行时间）

### 使用策略
- **增量读取**：每次调用只返回新产生的输出（非累积）
- **指数退避**：若连续读取无输出，逐步增加 `delay`（如 5s → 10s → 20s）
- **合理 `delay`**：根据任务性质设定（编译可能需 30-60s，快速命令 5-10s）

### 典型场景
- 检查长时构建的进度
- 等待测试套件完成
- 监控开发服务器日志

---

## 4. PowerShell 会话终止 (`stop_powershell`)

### 功能概述
强制终止 PowerShell 会话及其所有子进程。

### 核心参数
- **sessionId** (必需): 要终止的会话 ID

### 重要提示
- **破坏性操作**：会销毁整个会话，包括环境变量、工作目录等状态
- 若需重用同一 `sessionId`，必须重新定义环境
- 仅在命令无法自然退出或需紧急终止时使用

---

## 5. 文件编辑器 (`str_replace_editor`)

### 功能概述
查看、创建、编辑文件内容，状态跨调用持久化。

### 可用命令

#### `view` - 查看
- **path**: 绝对路径
  - 文件：显示带行号的内容（类似 `cat -n`）
  - 目录：递归列出 2 层内的非隐藏项
- **view_range** (可选): 行号范围 `[start, end]`（从 1 开始）
  - `[11, 12]`: 仅显示 11-12 行
  - `[50, -1]`: 从第 50 行到文件末尾

#### `create` - 创建
- **path**: 目标文件的绝对路径
- **file_text**: 完整文件内容
- **限制**：
  - 路径不能已存在
  - 父目录必须存在

#### `str_replace` - 替换
- **path**: 要编辑的文件
- **old_str**: 要替换的**精确**原始字符串（必须完全匹配，包括空白字符）
- **new_str**: 替换后的新字符串
- **关键规则**：
  - `old_str` 必须在文件中**唯一**，否则拒绝操作
  - 应包含足够上下文确保唯一性（多行更佳）
  - 保留所有前导/尾随空白

#### `insert` - 插入
- **path**: 目标文件
- **insert_line**: 在此行号**之后**插入
- **new_str**: 要插入的内容

### 最佳实践
- **原子化修改**：每次 `str_replace` 只改动最小必要范围
- **上下文充足**：`old_str` 应包含周围代码以避免歧义
- **验证修改**：编辑后用 `view` 确认结果
- **并行查看**：多个独立文件可同时 `view`

### 限制
- 输出过长会截断（标记为 `<file too long...`）
- 必须使用绝对路径

---

## 6. 思考工具 (`think`)

### 功能概述
记录复杂推理或头脑风暴过程，不执行任何操作或获取新信息。

### 核心参数
- **thought** (必需): 思考内容字符串

### 适用场景
- 分析 bug 根因后，列举多种修复方案并评估优劣
- 收到测试结果后，规划修复失败用例的策略
- 面对复杂需求时，拆解子任务并排定优先级

### 价值
- 透明化决策过程
- 减少盲目试错
- 便于人类协作者介入调整方向

---

## 工具组合策略

### 高效并行调用
**原则**：独立操作必须在同一响应中并行发起，避免无谓的轮次消耗。

**示例**：
- ✅ 同时读取 3 个不同文件
- ✅ 并行 `view` 目录结构 + `powershell` 查询 Git 状态
- ❌ 依赖前一结果的调用（如先读文件再根据内容编辑）必须分步

### 典型工作流

#### 代码修改流程
1. **探索**：并行 `view` 相关文件 + `powershell` 搜索引用
2. **思考**：用 `think` 规划修改方案
3. **编辑**：`str_replace_editor` 精确修改
4. **验证**：`powershell` 运行测试（同步模式 + 足够 timeout）
5. **确认**：`view` 检查最终结果

#### 构建与测试
```json
// 1. 长时任务用足够的 timeout
{"command": "dotnet build", "timeout": 180, "async": false}

// 2. 失败时并行查看日志 + 相关代码
// (多个 view 调用)

// 3. 修复后重新测试
{"command": "dotnet test --filter FailedTest", "timeout": 120}
```

#### 交互式工具
```json
// 1. 异步启动
{"command": "npx create-app", "sessionId": "setup", "async": true}

// 2. 发送选择（等待足够时间）
{"sessionId": "setup", "input": "{down}{enter}", "delay": 15}

// 3. 继续读取进度
{"sessionId": "setup", "delay": 30}
```

---

## 常见陷阱与解决

### 问题 1：Git 命令挂起
**原因**：分页器等待用户交互  
**解决**：始终用 `git --no-pager <subcommand>`

### 问题 2：长时命令超时
**原因**：默认 30 秒 timeout 不足  
**解决**：
- 设置合理 `timeout`（构建 180-300s，安装 300-600s）
- 或使用异步模式 + `read_powershell` 轮询

### 问题 3：路径错误
**原因**：Windows 上使用 Unix 风格路径  
**解决**：统一用反斜杠（`e:\repos\...`）

### 问题 4：编辑冲突
**原因**：`str_replace` 的 `old_str` 不唯一  
**解决**：扩大上下文范围，包含更多周围代码

### 问题 5：环境丢失
**原因**：使用 `stop_powershell` 后 `sessionId` 状态被清空  
**解决**：重新设置环境变量或使用新的 `sessionId`

---

## 工具能力边界

### 可以做到
- 执行本地命令行工具（Git、npm、dotnet、Python 等）
- 读写文件系统（绝对路径）
- 安装包（pip、npm、go get）
- 运行并交互 CLI 向导
- 并行处理多个独立任务

### 不能做到
- 访问互联网（无网络权限）
- 操作父目录（需聚焦当前工作目录）
- 删除文件（环境限制，需用重命名替代）
- 直接操作 GUI 应用
- 跨 `sessionId` 共享状态

---

## 协作建议

### 给人类协作者
1. **明确任务边界**：AI 聚焦当前目录，跨仓库操作需显式指引
2. **提供充分上下文**：复杂需求附上相关文件路径或代码片段
3. **验收标准清晰**：明确"通过测试"、"无警告"等具体指标
4. **善用 IDE 加速**：批量重命名、复杂重构等可人工配合 AI 完成

### 给 AI 助手
1. **最小化修改**：仅改动必要部分，不重构无关代码
2. **测试先行**：变更前先跑基线，确认新增失败归属当前任务
3. **文档同步**：修改设计相关代码时更新对应文档
4. **增量验证**：每完成一小步立即测试，避免大规模返工

---

## 版本信息
- **文档版本**: 1.0
- **AI 助手版本**: 0.0.343
- **更新日期**: 2025-11-15

---

## 附录：快速参考

| 工具 | 主要用途 | 关键参数 | 典型 timeout/delay |
|------|---------|---------|-------------------|
| `powershell` | 执行命令 | `async`, `timeout` | 构建 180s，测试 120s |
| `write_powershell` | 发送输入 | `input`, `delay` | 交互 ≥10s |
| `read_powershell` | 读取输出 | `delay` | 编译 30-60s |
| `stop_powershell` | 终止会话 | `sessionId` | - |
| `str_replace_editor` | 文件操作 | `command`, `old_str` | - |
| `think` | 记录推理 | `thought` | - |

---

**提示**：本文档应随工具更新或协作经验积累持续迭代。
