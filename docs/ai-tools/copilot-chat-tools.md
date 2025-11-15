下面以中文列出每个工具（含命名空间），并用一句话描述典型用途和注意点。若你需要示例或想要我实际调用某些工具，请告诉我。

1. functions.apply_patch  
   - 用途：对工作区已有文件应用补丁(diff/patch)并编辑内容（增删改）。  
   - 注意：用于修改现有文件，请在修改前确认目标路径与变更内容。

2. functions.create_directory  
   - 用途：按路径递归创建目录（类似 mkdir -p）。  
   - 注意：如果路径已存在不会报错，常与 create_file 配合使用。

3. functions.create_file  
   - 用途：创建新文件并写入内容（如果路径不存在，目录会自动创建）。  
   - 注意：不要用于替换已存在文件（若要修改，请用 apply_patch）。

4. functions.fetch_webpage  
   - 用途：抓取网页主要内容（例如阅读文档或 API 说明）。  
   - 注意：适合查找外部参考资料或示例代码。

5. functions.file_search  
   - 用途：按 glob 模式在工作区查找文件路径（例如 "**/*.cs"）。  
   - 注意：返回文件路径列表，常用于批量定位文件。

6. functions.grep_search  
   - 用途：在工作区内做快速文本搜索（字符串或正则），可限制 includePattern。  
   - 注意：支持 isRegexp 查询，适合查找符号或关键字用法。

7. functions.get_changed_files  
   - 用途：获取 Git 仓库当前的变更文件列表（staged/unstaged/merge-conflicts）。  
   - 注意：可用于在提交变更前查看差异或生成补丁。

8. functions.get_errors  
   - 用途：获取指定文件或整个代码库的编译/lint 错误信息。  
   - 注意：用于诊断构建/测试失败或编辑后验证。

9. functions.get_search_view_results  
   - 用途：返回编辑器搜索视图（查找面板）结果的摘要或最后一次搜索结果。  
   - 注意：可用于整合用户在编辑器里的搜索上下文。

10. functions.github_repo  
   - 用途：在指定 GitHub 仓库中搜索相关源码/片段（格式：'<owner>/<repo>'）。  
   - 注意：适用于查找公开仓库的实现示例或匹配代码片段。

11. functions.list_code_usages  
   - 用途：列出工作区内某个符号（函数/类/变量等）的引用、定义和用法。  
   - 注意：可用于重构或查找影响面。

12. functions.list_dir  
   - 用途：列出给定目录的子项（文件和子目录名）。  
   - 注意：简单的目录内容浏览器。

13. functions.read_file  
   - 用途：读取并返回文件内容（支持偏移与行数限制）。  
   - 注意：用于获取源文件或文档的具体内容以便分析。

14. functions.runSubagent  
   - 用途：启动子代理处理复杂、可拆分的任务（会运行独立 agent）。  
   - 注意：子代理是一次性、独立运行，其结果返回给主 agent。

15. functions.manage_todo_list  
   - 用途：管理结构化 TODO 列表（读/写）。  
   - 注意：用于规划多步任务、在任务间切换时保持可视化进度（必须使用）。

16. functions.run_in_terminal  
   - 用途：在持久终端（PowerShell）中执行命令（支持后台/前台执行）。  
   - 注意：在 Windows PowerShell 环境下运行时要以分号 ; 链接命令。

17. functions.get_terminal_output  
   - 用途：获取上一个通过 run_in_terminal 启动命令的输出。  
   - 注意：用于检查命令执行结果和长跑任务的输出。

18. functions.runTests  
   - 用途：在指定测试文件上运行单元测试，支持覆盖模式。  
   - 注意：可指定部分测试或文件，对于 CI / 本地验证很有用。

19. functions.terminal_last_command  
   - 用途：返回当前活动终端中最近执行的命令（便于回顾或再运行）。  
   - 注意：只读查询。

20. functions.terminal_selection  
   - 用途：获取当前终端的文本选择内容（如果有）。  
   - 注意：常用于捕获命令输入或剪切板替代。

21. functions.vscode-websearchforcopilot_webSearch  
   - 用途：在 Web 上执行通用搜索（适合查找文档或示例）。  
   - 注意：不是仓库内搜索，适合外部资料查询。

22. functions.test_failure  
   - 用途：返回单元测试失败信息（包含上下文），便于诊断。  
   - 注意：通常与 runTests 结合使用。

23. multi_tool_use.parallel  
   - 用途：同时并行启动多个工具调用（用于可并行执行的场景）。  
   - 注意：仅限 functions 命名空间的工具，适合并行化只读查询或 IO 密集操作。
