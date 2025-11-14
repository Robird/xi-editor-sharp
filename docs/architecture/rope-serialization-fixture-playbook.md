# Rope 序列化夹具维护手册 (Stage D)

> 本手册服务于 Stage D “共享资产同步”，指导协作者在 Windows PowerShell 环境下刷新 Rust → C# 的黄金 JSON 夹具，并对接测试与文档流程。所有命令均假定仓库根目录为 `E:\repos\Atelia-org\xi-editor-sharp`。

## 1. 黄金夹具来源
- 基准产物来自 Rust `xi-editor-ph7` 工作区内的回归测试：`subset_serialization_regression`、`delta_serialization_regression`、`engine_serialization_regression`。
- 黄金 JSON 全量集中在 `rope/src/serde_fixtures.rs` 常量 (`Fixture` 数组) 中，测试与导出工具共用同一来源，确保唯一事实。
- C# 侧镜像保存在 `tests/xi.Core.Tests/Fixtures/`，测试项目通过内置 helper 直接读取这些文件。

## 2. 夹具刷新流程（Windows PowerShell）
> 推荐使用 `scripts/refresh_serialization_fixtures.ps1` 统一驱动 Rust 校验、夹具导出与 `dotnet test`；脚本已内置 `-SkipRust`、`-SkipDotnet`、`-DryRun`、`-Verbose` 等开关以适配不同需求。
1. **同步 Rust 子仓库（按需）**
  ```powershell
  git -C "E:\repos\Atelia-org\xi-editor-sharp\xi-editor-ph7" fetch origin;
  git -C "E:\repos\Atelia-org\xi-editor-sharp\xi-editor-ph7" checkout feature/generic-node-refactor-experiment;
  git -C "E:\repos\Atelia-org\xi-editor-sharp\xi-editor-ph7" pull --ff-only
  ```
2. **验证 Rust 基线（建议）**
  ```powershell
  Set-Location "E:\repos\Atelia-org\xi-editor-sharp\xi-editor-ph7\rust";
  .\run_all_checks --filter serde-fixtures;
  cargo test -p xi-rope --features serde subset_serialization_regression -- --nocapture;
  cargo test -p xi-rope --features serde delta_serialization_regression -- --nocapture;
  cargo test -p xi-rope --features serde engine_serialization_regression -- --nocapture;
  Set-Location "E:\repos\Atelia-org\xi-editor-sharp"
  ```
  - `run_all_checks --filter serde-fixtures` 复用现有脚本缓存，覆盖 serde/非 serde 双轨测试。
  - 三个 `cargo test` 入口直接对比 `serde_fixtures` 常量，与 exporter 共用唯一来源；若想更新 JSON 结构，请先修改这些测试的断言与常量。
3. **导出并覆写 C# 夹具**
  - 默认：运行 `scripts/refresh_serialization_fixtures.ps1`（例如 `.\\scripts\\refresh_serialization_fixtures.ps1 -Verbose`），脚本会调用 `cargo run -p xi-rope --features serde --bin export-serde-fixtures -- --dir tests\xi.Core.Tests\Fixtures` 由 Rust 侧直接写入目标目录，并在未使用 `-SkipDotnet` 时自动执行 `dotnet test`。
  - 手动备选：进入 `xi-editor-ph7/rust` 后执行 `cargo run -p xi-rope --features serde --bin export-serde-fixtures -- --dir <自定义目录>`，随后按需复制结果。
4. **格式与差异检查**
   ```powershell
   git status --short tests/xi.Core.Tests/Fixtures;
   git diff tests/xi.Core.Tests/Fixtures/*.json
   ```
   - 确认仅有预期字段变化，若出现结构差异需先在 Rust 侧更新文档与 helper。

## 3. 变更验证清单
- **Rust**：
  - `cargo test -p xi-rope --features serde subset_serialization_regression delta_serialization_regression engine_serialization_regression`
  - `cargo test -p xi-rope --no-default-features`
  - `E:\repos\Atelia-org\xi-editor-sharp\xi-editor-ph7\rust\run_all_checks`
- **C#**：
  - `dotnet test Xi.Editor.sln`
- **Diff 审核要点**：
  - 树结构字段（`els`, `base_len`, `revs` 等）是否保持顺序与 Rust 输出一致。
  - 数值字段（`pos`, `len`, `priority`）变化是否与预期回归场景对应。
  - 校验空数组/可选字段在 serde::skip 场景下是否正常缺省。

## 4. 文档与同步要求
- 每次刷新后更新以下文件：
  - `docs/architecture/rope-cs-mirror-plan.md`：记录 Stage D 进展与交付状态。
  - `docs/architecture/rope-port-mapping.md`：标注新增/调整的 helper 对照与夹具来源。
  - `docs/architecture/rope-serialization-fixture-playbook.md`（本文）：若流程更新需及时修订。
  - `AGENTS.md`：在“当前聚焦事项”“下一步行动”“工作日志”记录本次刷新成果与测试结论。
- 将新的命令或脚本引用写入文档，保持跨团队同步的唯一事实来源。

## 5. 自动化展望与开放问题
- **自动化钩子候选**：
  - 在 `scripts/refresh_skeleton_docs.py` 追加可选子命令，串连 Rust 夹具刷新与 C# 复制步骤。
  - 基于已集成的 `scripts/refresh_serialization_fixtures.ps1`（调用 `export-serde-fixtures`），完善模式开关与日志输出，并在 CI 中复用相同脚本。
  - 将 Rust `run_all_checks` 结果与 `dotnet test` 组合至 CI pipeline，输出夹具漂移报告。
- **开放问题**：
  - 是否需要在 Rust 端提供专门的 `cargo xtask fixtures` 以保证输出路径稳定。
  - 大型夹具是否应采用压缩或分片策略以减轻仓库体积。
  - 如何在多分支并行时协调夹具更新，避免回归覆盖错误。
