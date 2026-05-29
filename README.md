# MemoryOptimizer

从 [PCL-CE (Plain Craft Launcher Community Edition)](https://github.com/PCL-Community/PCL-CE) 提取的独立 Windows 内存优化工具。

通过调用 Windows NT 内核未文档化 API（`ntdll.dll` → `NtSetSystemInformation`），执行底层内存清理操作，快速释放物理内存。

> ⚠️ **需要管理员权限** — 内核级内存操作必须以管理员身份运行。

## 项目结构

```
MemoryOptimizer/
├── MemoryOptimizer.Core/    ← 核心类库 (MemSwap + NtInterop)
├── MemoryOptimizer.Cli/     ← 命令行工具
├── MemoryOptimizer.Gui/     ← WinForms 图形界面
└── publish/
    ├── cli/                 ← 编译好的 CLI 可执行文件
    └── gui/                 ← 编译好的 GUI 可执行文件
```

## 7 种优化操作

| 操作 | 说明 |
|------|------|
| **EmptyWorkingSets** | 清空所有进程的工作集，强制将不活跃内存页移到备用列表（效果最明显） |
| **FlushModifiedList** | 将已修改页面立即写入磁盘后释放 |
| **PurgeStandbyList** | 释放系统预留在备用列表中的内存页 |
| **PurgeLowPriorityStandby** | 清空低优先级备用列表 |
| **FlushFileCache** | 释放被文件系统缓存占用的内存 |
| **CombinePhysicalMemory** | 合并/去重相同的物理内存页面 |
| **RegistryReconciliation** | 注册表协调（Win10 1703 后可能不可用） |

## CLI 使用方法

```bash
# 在管理员终端中运行：

# 交互模式（显示内存状态，确认后执行全部）
MemoryOptimizer.Cli.exe -v

# 静默执行全部操作
MemoryOptimizer.Cli.exe --all -y

# 仅执行指定操作
MemoryOptimizer.Cli.exe PurgeStandbyList FlushFileCache

# 查看帮助
MemoryOptimizer.Cli.exe --help
```

选项：
- `--all, -a` — 执行全部 7 项操作
- `--yes, -y` — 跳过确认提示
- `--show-before, -v` — 显示优化前后内存状态
- `--help, -h` — 显示帮助

## GUI 使用方法

右键 `MemoryOptimizer.Gui.exe` → **以管理员身份运行**。

界面说明：
- **顶部面板** — 实时显示内存状态（总容量、已用、可用、负载百分比）
- **中间面板** — 勾选要执行的操作（默认全选），支持"全选/取消"
- **执行按钮** — 点击开始优化
- **底部日志** — 显示每项操作的执行结果和释放内存量

## 构建

需要 .NET 10 SDK。

```bash
git clone <repo> && cd MemoryOptimizer
dotnet build -c Release

# 发布
dotnet publish MemoryOptimizer.Cli -c Release -r win-x64 -o publish/cli
dotnet publish MemoryOptimizer.Gui -c Release -r win-x64 -o publish/gui

# 如需独立部署（无需安装 .NET 运行时）
dotnet publish MemoryOptimizer.Cli -c Release -r win-x64 --self-contained true -o publish/cli-self
```

## 技术说明

所有操作通过 `ntdll.dll` 的 `NtSetSystemInformation` 执行。该 API 未文档化，不同 Windows 版本行为可能不同。具体实现参考 [PCL-CE MemSwapWorks.cs](https://github.com/PCL-Community/PCL-CE)。

相比原版 PCL-CE，提取后的版本：
- 去掉了 `PromoteService` 提权框架（直接要求管理员运行）
- 去掉了 DI 容器、生命周期、日志等 PCL 专属基础设施
- 核心逻辑（MemSwap + NtInterop）完全保留

## 许可证

本工具代码提取自 [PCL-CE](https://github.com/PCL-Community/PCL-CE)，遵循其许可证条款。
