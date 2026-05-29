# MemoryOptimizer

从 [PCL-CE](https://github.com/PCL-Community/PCL-CE) 提取的独立 Windows 内存优化工具。

通过 Windows NT 内核 API（`ntdll.dll` → `NtSetSystemInformation`）执行底层内存清理，快速释放物理内存。

> ⚠️ 需要管理员权限。双击自动弹出 UAC 提权。

## 下载

在 [Releases](https://github.com/unknown-gezi/MemoryOptimizer/releases) 下载最新版本：

| 版本 | 文件 | 说明 |
|------|------|------|
| **CLI** | `MemoryOptimizer.Cli.exe` | 命令行，双击静默优化 |
| **GUI** | `MemoryOptimizer.Gui.exe` | 图形界面，圆环仪表实时监控 |

自包含单文件，无需安装 .NET 运行时。

## CLI 使用

### 双击（推荐）

右键 → **以管理员身份运行**（或直接双击自动提权），一闪完成：

```
释放 481 MB | 3.07 GB → 3.54 GB | 负载 23% → 11%
```

### 命令行

```bash
# 静默全优化
MemoryOptimizer.Cli.exe --all -y

# 显示内存对比
MemoryOptimizer.Cli.exe -v

# 指定操作
MemoryOptimizer.Cli.exe PurgeStandbyList FlushFileCache
```

## GUI 使用

右键 → **以管理员身份运行**。

- 圆环仪表实时显示内存负载
- 左侧百分比 + 右侧详细数据
- 点击「优化内存」一键执行全部 7 项操作
- 释放量显示在按钮上方

## 7 种优化操作

| 操作 | 说明 |
|------|------|
| EmptyWorkingSets | 清空所有进程的工作集（效果最明显） |
| FlushFileCache | 释放文件系统缓存 |
| FlushModifiedList | 将已修改页面写入磁盘后释放 |
| PurgeStandbyList | 释放备用页面列表 |
| PurgeLowPriorityStandby | 清空低优先级备用列表 |
| RegistryReconciliation | 注册表协调 |
| CombinePhysicalMemory | 合并/去重相同物理内存页面 |

## 架构

```
双击 CLI/GUI
    │
    ▼
主进程 ──runas──▶ Worker 子进程     ← 与 PCL-CE PromoteService 架构一致
                      │
                      ▼
              AcquirePrivileges()
              ├── SeProfileSingleProcessPrivilege
              └── SeIncreaseQuotaPrivilege
                      │
                      ▼
              NtSetSystemInformation() × 7
```

## 构建

需要 .NET 10 SDK。

```bash
git clone https://github.com/unknown-gezi/MemoryOptimizer.git
cd MemoryOptimizer
dotnet build -c Release

# 发布自包含单文件
dotnet publish MemoryOptimizer.Cli -c Release -r win-x64 \
    --self-contained true -p:PublishSingleFile=true -o publish
dotnet publish MemoryOptimizer.Gui -c Release -r win-x64 \
    --self-contained true -p:PublishSingleFile=true -o publish
```

## 技术细节

- 核心代码完全照搬 PCL-CE 的 `MemSwapWorks` 和 `NtInterop`
- 通过 `runas` 启动 worker 子进程，获得完整管理员令牌（与 PCL-CE `PromoteService` 机制一致）
- 同时启用 `SeProfileSingleProcessPrivilege` + `SeIncreaseQuotaPrivilege` 两个特权
- 缓冲区格式与 PCL-CE 完全一致（`SYSTEM_FILECACHE_INFORMATION` 60 字节结构体等）

## 许可证

[Apache 2.0](LICENSE) — 与 PCL-CE 保持一致。
