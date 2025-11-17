# Mini Blocking Model Workspace

> 用于抽离阻塞移植问题的 Rust/C# 配对工程骨架，方便在精简上下文中验证类型系统与软件工程策略。

## 目录

 `csharp/`：.NET 9 解决方案，包含 `BlockingModel.Core` 与 `BlockingModel.Tests`，用于建模 C# 端的接口、诊断与最小测试。
 `rust/`：Rust workspace（Cargo），当前提供 `blocking_model_core` crate，既包含阻塞点 registry，也维护 `xi-editor-ph7/rust/rope` 的 `skeleton/` 模块。

## 使用方式

1. 在每个阻塞点前，先在 `docs/architecture/mini-blocking-model-plan.md` 记录实验目标、验证策略与回传路径。
2. Rust/C# 端分别在各自目录中实现最小可编译示例，同时在测试项目中加入 parity/诊断用例。
3. 解出方案后，将关键代码、测试与文档回写主工程并在 `AGENTS.md` / 相关认知档案中记录经验。
