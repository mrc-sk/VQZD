# VQZCMod — 插件市场 / Plugin Market

**Versatile Quorum Zone Dispatcher** — 通用第三方插件分发仓库。

> 本分支（`vqzcmod`）即 **插件市场**：主程序「插件管理 → 插件市场」会通过
> GitHub API 列出本分支根目录下的 `.vqzcmod` 文件，双击即可下载安装。
>
> This branch IS the plugin market: the app's "Plugin Market" page lists every
> `.vqzcmod` file at the root of this branch and lets users install them with a
> double-click.

---

## 上架插件 / Publishing a plugin

1. 用 [PluginPacker](https://github.com/mrc-sk/VQZD/tree/main/PluginPacker)
   将你的 C# 插件打包为 `.vqzcmod`（详见
   [插件协议 v1](https://github.com/mrc-sk/VQZD/blob/main/docs/vqzcmod-protocol.md)）。
2. 将 `.vqzcmod` 文件 **直接放在本分支根目录**（不要放子目录），提交推送即可上架。

> 插件也可以外部分发（直接发送 `.vqzcmod` 文件），不影响使用；
> 但建议统一上架以获得可信度。

## 当前插件 / Available plugins

| 插件 | ID | 版本 | 类型 | 说明 |
|---|---|---|---|---|
| [HelloWorld.vqzcmod](HelloWorld.vqzcmod) | `com.vqzd.demo.hello` | 1.0.0 | other | 官方示例插件：运行后写入问候日志并返回问候语 |

## 安全提示 / Security

安装插件时，主程序会读取 `config.json` 中声明的**类型与权限**；若插件声明了
"修改系统底层"类权限（注册表/驱动/服务/内核等），会弹出**高风险警告**。
请只安装来源可信的插件。

## 协议 / Protocol

插件清单格式、入口约定与权限规则见
[通用第三方插件协议 v1](https://github.com/mrc-sk/VQZD/blob/main/docs/vqzcmod-protocol.md)。
