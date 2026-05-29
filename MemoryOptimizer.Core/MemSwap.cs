using System.Runtime.InteropServices;

namespace MemoryOptimizer.Core;

/// <summary>
/// 内存优化引擎 — 从 PCL-CE 提取的核心逻辑。
/// 所有方法均需要**管理员权限**，否则抛出 Win32Exception (ERROR_ACCESS_DENIED)。
/// </summary>
public static class MemSwap
{
    // ── 7 种内存优化操作 ──

    /// <summary>
    /// 1. 清空所有进程的工作集 (Working Set)。
    /// 这是效果最明显的操作 — 强制将各进程内存中暂不活跃的页面移到备用列表。
    /// </summary>
    public static void EmptyWorkingSets()
    {
        NtInterop.EnableIncreaseQuotaPrivilege();
        NtInterop.SetSystemInfo(NtInterop.SystemInformationClass.SystemMemoryListInformation, 0x00000004);
    }

    /// <summary>
    /// 2. 清空文件系统缓存 (File Cache)。
    /// 释放被文件缓存占用的物理内存。
    /// Win10 1809+ 使用扩展版本。
    /// </summary>
    public static void FlushFileCache()
    {
        NtInterop.EnableIncreaseQuotaPrivilege();
        var os = NtInterop.GetOsVersion();
        if (os is { Major: 10, Build: >= 17763 })
        {
            // Win10 1809+ , 使用 SystemFileCacheInformationEx
            NtInterop.SetSystemInfo(NtInterop.SystemInformationClass.SystemFileCacheInformationEx, 1);
        }
        else
        {
            // 旧版 OS, 使用 SystemFileCacheInformation
            NtInterop.SetSystemInfoRaw((int)NtInterop.SystemInformationClass.SystemFileCacheInformation, IntPtr.Zero, 0);
        }
    }

    /// <summary>
    /// 3. 清空已修改页面列表 (Modified Page List)。
    /// 将修改过的页面立即写入磁盘，释放它们占用的物理内存。
    /// </summary>
    public static void FlushModifiedList()
    {
        NtInterop.EnableIncreaseQuotaPrivilege();
        NtInterop.SetSystemInfo(NtInterop.SystemInformationClass.SystemMemoryListInformation, 0x00000002);
    }

    /// <summary>
    /// 4. 清空备用页面列表 (Standby List)。
    /// 释放操作系统预留在备用列表中的内存页。
    /// </summary>
    public static void PurgeStandbyList()
    {
        NtInterop.EnableIncreaseQuotaPrivilege();
        NtInterop.SetSystemInfo(NtInterop.SystemInformationClass.SystemMemoryListInformation, 0x00000001);
    }

    /// <summary>
    /// 5. 清空低优先级备用列表 (Low Priority Standby List)。
    /// </summary>
    public static void PurgeLowPriorityStandbyList()
    {
        NtInterop.EnableIncreaseQuotaPrivilege();
        NtInterop.SetSystemInfo(NtInterop.SystemInformationClass.SystemMemoryListInformation, 0x00000008);
    }

    /// <summary>
    /// 6. 注册表协调 (Registry Reconciliation)。
    /// 在 Win10 1703 (RS2) 之后可能不可用。
    /// </summary>
    public static void RegistryReconciliation()
    {
        NtInterop.EnableIncreaseQuotaPrivilege();
        NtInterop.SetSystemInfo(NtInterop.SystemInformationClass.SystemRegistryReconciliationInformation, 0);
    }

    /// <summary>
    /// 7. 合并物理内存 (Combine Physical Memory)。
    /// 去重相同的物理内存页面，释放冗余。
    /// </summary>
    public static void CombinePhysicalMemory()
    {
        NtInterop.EnableIncreaseQuotaPrivilege();
        NtInterop.SetSystemInfo(NtInterop.SystemInformationClass.SystemCombinePhysicalMemoryInformation, 0);
    }

    // ── 组合操作 ──

    /// <summary>获取所有操作及其名称和描述</summary>
    public static IReadOnlyList<(string Name, string Description, Action Action)> AllOperations { get; } =
    [
        ("EmptyWorkingSets",         "清空工作集",           EmptyWorkingSets),
        ("FlushModifiedList",        "清空修改页面列表",      FlushModifiedList),
        ("PurgeStandbyList",         "清空备用列表",          PurgeStandbyList),
        ("PurgeLowPriorityStandby",  "清空低优先级备用列表",   PurgeLowPriorityStandbyList),
        ("FlushFileCache",           "清空文件缓存",          FlushFileCache),
        ("CombinePhysicalMemory",    "合并物理内存",          CombinePhysicalMemory),
        ("RegistryReconciliation",   "注册表协调",            RegistryReconciliation),
    ];

    /// <summary>执行全部 7 项操作</summary>
    public static void RunAll()
    {
        foreach (var (_, _, action) in AllOperations)
            action();
    }

    /// <summary>按名称执行单次操作（不区分大小写）</summary>
    public static bool RunByName(string name)
    {
        foreach (var (n, _, action) in AllOperations)
        {
            if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
            {
                action();
                return true;
            }
        }
        return false;
    }
}
