# VQZD

**Versatile Quorum Zone Dispatcher** — 机房设备管理系统（WinForms，C# / .NET）

> ⚠️ **开源范围声明 / Open-source scope**
>
> 本仓库（`main` 分支）只开源 **框架与外壳**（插件协议、打包工具、示例插件），
> **不开源核心业务**（设备管理、工单、告警、数据库访问等核心实现）。
> 核心功能以编译产物随正式发布包分发。
>
> This repository (`main` branch) only open-sources the **framework/shell**:
> the plugin protocol, the packer tool and sample plugins. The **core business
> logic** (device management, work orders, alarms, DB access, etc.) is **not**
> open-sourced and is shipped as compiled binaries in releases.

---

## 仓库结构 / Repository layout

```
├── PluginPacker/            # 插件打包工具源码（生成 .vqzcmod）
├── PluginSamples/HelloWorld # 示例插件源码
├── docs/vqzcmod-protocol.md # 通用第三方插件协议 v1
└── LICENSE
```

## 插件生态 / Plugin ecosystem

- **协议**：[通用第三方插件协议 v1](docs/vqzcmod-protocol.md)
- **打包**：用 `PluginPacker` 将 C# 插件打包为 `.vqzcmod`
- **市场**：插件市场仓库分支 [`vqzcmod`](https://github.com/mrc-sk/VQZD/tree/vqzcmod)，
  将 `.vqzcmod` 提交到该分支根目录即可上架

## 构建 / Build

```bash
# PluginPacker（net48 / net10.0-windows 双目标）
dotnet build PluginPacker/PluginPacker.csproj -c Release

# 示例插件
dotnet build PluginSamples/HelloWorld/HelloWorld.csproj -c Release
```

## 打包插件 / Pack a plugin

```bash
PluginPacker.exe MyPlugin.dll out.vqzcmod ^
  --id com.vqzd.demo.hello ^
  --name "HelloWorld 示例" ^
  --version 1.0.0 ^
  --author VQZD ^
  --type other ^
  --class VQZD.Plugin.HelloWorld.HelloWorldPlugin ^
  --method Run
```

详见 [插件协议文档](docs/vqzcmod-protocol.md)。
