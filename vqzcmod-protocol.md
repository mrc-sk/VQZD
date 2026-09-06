# VQZCMod 通用第三方插件协议 (v1)

> VQZD 机房管理系统的插件协议。插件使用 C# 编写，通过官方打包工具
> `PluginPacker` 打包为 `.vqzcmod` 文件分发、安装。
>
> Plugin protocol for the VQZD machine-room management system. Plugins are
> written in C#, packed into a `.vqzcmod` container by the official
> `PluginPacker` tool, then distributed and installed.

---

## 1. 容器格式 / Container format

`.vqzcmod` 是一个标准 ZIP 容器，根目录包含：

```
myplugin.vqzcmod
├── config.json            # 插件清单（必须，根目录）
└── MyPlugin.dll           # 插件入口程序集（必须，名称与 config.json 的 entryAssembly 一致）
└── (可选) 其他资源文件      # 插件运行时需要的附属文件
```

- ZIP 压缩：`Deflate`。
- `config.json` 编码：UTF-8（无 BOM）。

## 2. config.json 字段 / Manifest fields

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| `id` | string | 是 | 插件唯一标识，建议反向域名，如 `com.vqzd.demo.hello` |
| `name` | string | 是 | 插件显示名称 |
| `version` | string | 是 | 语义化版本，如 `1.0.0` |
| `author` | string | 否 | 作者 |
| `description` | string | 否 | 描述 |
| `type` | string | 是 | 插件类型：`ui` / `data` / `network` / `system` / `other` |
| `entryAssembly` | string | 是 | 入口程序集文件名（包内 DLL 文件名） |
| `entryClass` | string | 是 | 入口类型全名（含命名空间） |
| `entryMethod` | string | 否 | 入口方法名；留空自动尝试 `Run` → `OnLoad` → `Execute` |
| `permissions` | string[] | 否 | 权限声明（见 §4） |
| `minAppVersion` | string | 否 | 所需主程序最低版本 |

### 示例 / Example

```json
{
  "id": "com.vqzd.demo.hello",
  "name": "HelloWorld 示例",
  "version": "1.0.0",
  "author": "VQZD",
  "description": "示例插件",
  "type": "other",
  "entryAssembly": "VQZD.Plugin.HelloWorld.dll",
  "entryClass": "VQZD.Plugin.HelloWorld.HelloWorldPlugin",
  "entryMethod": "Run",
  "permissions": [],
  "minAppVersion": ""
}
```

## 3. 入口约定 / Entry conventions

主程序在插件被"运行"时动态加载入口程序集并**自由反射**调用入口方法：

1. 加载 `entryAssembly` 程序集；
2. 定位 `entryClass` 类型（公开类型）；
3. 确定方法：优先 `entryMethod`；为空则依次尝试 `Run` / `OnLoad` / `Execute`；
4. 方法签名支持两种：
   - **无参**：`public string Run()` 或 `public void Run()`
   - **单 string 参数**：`public string Run(string installDir)` —— 参数为插件安装目录
5. 静态方法直接调用；实例方法自动创建实例后调用。

> 入口方法无固定接口要求（纯反射），插件可以自由调用主程序程序集
> （`VQZD.dll`）中公开的任何 API——**插件可定制万物**。
> 但注意：主程序不开源，公开 API 以发布文档为准。

## 4. 类型与权限 / Types & permissions

### 类型 `type`

| 值 | 含义 |
|---|---|
| `ui` | 界面扩展（菜单、页面、控件） |
| `data` | 数据处理（导入导出、报表） |
| `network` | 网络协议（扫描、探测、下发） |
| `system` | **系统底层（高风险）**：注册表、驱动、服务、内核等 |
| `other` | 其他 |

### 权限 `permissions`

建议在 `permissions` 中如实声明插件所需能力，例如：

```
["filesystem", "network", "clipboard"]
```

主程序安装时会对 `permissions` 与 `type` 做**高风险关键字扫描**
（`registry` / `driver` / `service` / `kernel` / `root` / `修改系统` /
`系统底层` / `底层` / `注册表` / `驱动` / `内核` / `提权` / `hook` /
`inject` / `patch` 等，英文整词匹配）。命中即判定为"修改系统底层"类
插件，安装前弹出**高风险警告**，要求用户确认来源可信后才可继续安装。

## 5. 安装 / 卸载 / 生命周期

- **安装**：主程序「插件管理 → 添加插件」选择 `.vqzcmod`；或从「插件市场」
  下载安装。安装目录：主程序目录 `plugins/<id>/`。
- **启用/禁用**：通过标记文件实现，无需卸载重装。
- **卸载**：删除 `plugins/<id>/` 目录，不影响主程序与其他插件。
- **首次进入**：插件管理界面首次打开时显示《通用第三方插件协议》，
  用户同意后不再提示。

## 6. 插件市场 / Plugin market

插件市场仓库：`https://github.com/mrc-sk/VQZD` 分支 `vqzcmod`。

- 将打包好的 `.vqzcmod` 文件提交到该分支根目录即上架；
- 主程序「插件市场」通过 GitHub API 列出该分支下的 `.vqzcmod` 文件，
  双击即可下载安装；
- 插件也可以外部分发（例如直接发送 `.vqzcmod` 文件），不影响使用，
  但建议统一上架以获得可信度。

## 7. 使用打包工具 / Using PluginPacker

`PluginPacker.exe` 支持两种模式：

### GUI 模式

双击运行，拖入 DLL → 自动读取程序集名称/版本/公司/描述，并推荐入口类 →
填写或确认清单字段 → 点击「打包 .vqzcmod」。

### 命令行模式

```bat
PluginPacker.exe MyPlugin.dll out.vqzcmod ^
  --id com.vqzd.demo.hello ^
  --name "HelloWorld 示例" ^
  --version 1.0.0 ^
  --author VQZD ^
  --description "示例插件" ^
  --type other ^
  --class VQZD.Plugin.HelloWorld.HelloWorldPlugin ^
  --method Run ^
  --permission filesystem,network
```

### 开发插件的最小示例

```csharp
// HelloWorldPlugin.cs —— 编译为类库（net48 / net10 均可）
namespace VQZD.Plugin.HelloWorld
{
    public sealed class HelloWorldPlugin
    {
        public string Run()
        {
            return "你好，我是示例插件！";
        }
    }
}
```

## 8. 版本与兼容 / Versioning

- 协议版本：`v1`（协议字段变更时递增主版本）。
- `minAppVersion`：插件可声明所需主程序最低版本，主程序低于该版本时
  提示不兼容。
