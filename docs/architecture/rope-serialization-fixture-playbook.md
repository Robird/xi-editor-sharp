# Rope 序列化夹具维护手册 (Stage D)

> 本手册服务于 Stage D “共享资产同步”，指导协作者在 Windows PowerShell 环境下刷新 Rust → C# 的黄金 JSON 夹具，并对接测试与文档流程。所有命令均假定仓库根目录为 `E:\repos\Atelia-org\xi-editor-sharp`。

## 1. 黄金夹具来源
- 基准产物来自 Rust `xi-editor-ph7` 工作区内的回归测试：`subset_serialization_regression`、`delta_serialization_regression`、`engine_serialization_regression`。
- 相关 JSON 输出位于 `xi-editor-ph7/rust/rope/tests/regressions/serde/`，对应文件名保持与 C# 夹具一致（`subset_regression.json`、`delta_regression.json`、`engine_regression.json`）。
- C# 侧镜像保存在 `tests/xi.Core.Tests/Fixtures/`，测试项目通过内置 helper 直接读取这些文件。

## 2. 夹具刷新流程（Windows PowerShell）
> 可通过 `scripts/refresh_serialization_fixtures.ps1` 自动化执行以下命令，脚本默认串联 Rust 校验、夹具复制与 `dotnet test`。
1. **同步 Rust 子仓库**
   ```powershell
   git -C "E:\repos\Atelia-org\xi-editor-sharp\xi-editor-ph7" fetch origin;
   git -C "E:\repos\Atelia-org\xi-editor-sharp\xi-editor-ph7" checkout feature/generic-node-refactor-experiment;
   git -C "E:\repos\Atelia-org\xi-editor-sharp\xi-editor-ph7" pull --ff-only
   ```
2. **执行 Rust 回归测试（生成黄金输出）**
   ```powershell
   Set-Location "E:\repos\Atelia-org\xi-editor-sharp\xi-editor-ph7\rust";
   .\run_all_checks --filter serde-fixtures;
   cargo test -p xi-rope --features serde subset_serialization_regression -- --nocapture;
   cargo test -p xi-rope --features serde delta_serialization_regression -- --nocapture;
   cargo test -p xi-rope --features serde engine_serialization_regression -- --nocapture;
   Set-Location "E:\repos\Atelia-org\xi-editor-sharp"
   ```
   - `run_all_checks --filter serde-fixtures` 收敛到 serde 相关任务，可复用现有脚本的缓存配置。
   - 各 `cargo test` 命令以 `--nocapture` 输出最新 JSON；Rust 测试负责在 `tests/regressions/serde/` 下覆盖旧文件。
3. **复制黄金文件至 C# 夹具目录**
   ```powershell
   Copy-Item "E:\repos\Atelia-org\xi-editor-sharp\xi-editor-ph7\rust\rope\tests\regressions\serde\subset_regression.json" "E:\repos\Atelia-org\xi-editor-sharp\tests\xi.Core.Tests\Fixtures\subset_regression.json";
   Copy-Item "E:\repos\Atelia-org\xi-editor-sharp\xi-editor-ph7\rust\rope\tests\regressions\serde\delta_regression.json" "E:\repos\Atelia-org\xi-editor-sharp\tests\xi.Core.Tests\Fixtures\delta_regression.json";
   Copy-Item "E:\repos\Atelia-org\xi-editor-sharp\xi-editor-ph7\rust\rope\tests\regressions\serde\engine_regression.json" "E:\repos\Atelia-org\xi-editor-sharp\tests\xi.Core.Tests\Fixtures\engine_regression.json"
   ```
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
  - 扩展现有 `scripts/refresh_serialization_fixtures.ps1`（原型已提供），完善模式开关并接入 CI。
  - 将 Rust `run_all_checks` 结果与 `dotnet test` 组合至 CI pipeline，输出夹具漂移报告。
- **开放问题**：
  - 是否需要在 Rust 端提供专门的 `cargo xtask fixtures` 以保证输出路径稳定。
  - 大型夹具是否应采用压缩或分片策略以减轻仓库体积。
  - 如何在多分支并行时协调夹具更新，避免回归覆盖错误。
