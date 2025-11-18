# StageDDescriptorInspector

`StageDDescriptorInspector` 是一个轻量级 CLI，用于读取 `tests/xi.Core.Tests/Fixtures` 下的 Stage D manifest、Breaks/Diff/Search 描述符并输出可读摘要。
它复用 `StageDDescriptorLoader` 与 `StageDDescriptorHydrator`，因此 QA/脚本无需重复解析 JSON。

## 快速开始

```bash
cd /repos/xi-editor-sharp
dotnet run --project tools/StageDDescriptorInspector -- --fixtures tests/xi.Core.Tests/Fixtures
```

### 常用参数

| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `--fixtures <path>` | Stage D 夹具根目录 | 自动定位 `tests/xi.Core.Tests/Fixtures` |
| `--categories <list>` | 需要水合的类别，`breaks,diff,search,all` | `all` |
| `--format <text|json>` | 输出格式 | `text` |
| `--help` | 打印使用说明 | — |

示例：

```bash
# 仅总结 Breaks + Search，输出 JSON
cd /repos/xi-editor-sharp
DOTNET_CLI_UI_LANGUAGE=en dotnet run --project tools/StageDDescriptorInspector -- \
  --fixtures tests/xi.Core.Tests/Fixtures \
  --categories breaks,search \
  --format json
```

该 CLI 在 `scripts/refresh_serialization_fixtures.ps1` 中默认运行，生成的摘要可直接记录到 Stage D Playbook / QA 日志中。