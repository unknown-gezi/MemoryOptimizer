using System.Runtime.InteropServices;

namespace MemoryOptimizer.Core;

/// <summary>
/// 内存优化引擎 — 原样提取自 PCL-CE 的 MemSwapWorks。
/// 所有方法均需要管理员权限。
/// </summary>
public static class MemSwap
{
    /// <summary>1. 清空所有进程的工作集</summary>
    public static void EmptyWorkingSets()
    {
        NtInterop.SetPrivilege(NtInterop.SE_INCREASE_QUOTA_PRIVILEGE, true, true);
        NtInterop.SetSystemInfo(NtInterop.SystemInformationClass.SystemMemoryListInformation,
            (int)NtInterop.SystemMemoryListCommand.MemoryEmptyWorkingSets);
    }

    /// <summary>2. 清空已修改页面列表</summary>
    public static void FlushModifiedList()
    {
        NtInterop.SetPrivilege(NtInterop.SE_INCREASE_QUOTA_PRIVILEGE, true, true);
        NtInterop.SetSystemInfo(NtInterop.SystemInformationClass.SystemMemoryListInformation,
            (int)NtInterop.SystemMemoryListCommand.MemoryFlushModifiedList);
    }

    /// <summary>3. 清空备用页面列表</summary>
    public static void PurgeStandbyList()
    {
        NtInterop.SetPrivilege(NtInterop.SE_INCREASE_QUOTA_PRIVILEGE, true, true);
        NtInterop.SetSystemInfo(NtInterop.SystemInformationClass.SystemMemoryListInformation,
            (int)NtInterop.SystemMemoryListCommand.MemoryPurgeStandbyList);
    }

    /// <summary>4. 清空低优先级备用列表</summary>
    public static void PurgeLowPriorityStandbyList()
    {
        NtInterop.SetPrivilege(NtInterop.SE_INCREASE_QUOTA_PRIVILEGE, true, true);
        NtInterop.SetSystemInfo(NtInterop.SystemInformationClass.SystemMemoryListInformation,
            (int)NtInterop.SystemMemoryListCommand.MemoryPurgeLowPriorityStandbyList);
    }

    /// <summary>5. 清空文件系统缓存</summary>
    public static void FlushFileCache()
    {
        NtInterop.SetPrivilege(NtInterop.SE_INCREASE_QUOTA_PRIVILEGE, true, true);
        var os = NtInterop.GetOsVersion();
        if (os.Major == 10 && os.Build >= 17763)
            NtInterop.SetFileCacheInfo(1);
        else
            NtInterop.SetSystemInfo(NtInterop.SystemInformationClass.SystemFileCacheInformationEx, 0);
    }

    /// <summary>6. 合并物理内存</summary>
    public static void CombinePhysicalMemory()
    {
        NtInterop.SetPrivilege(NtInterop.SE_INCREASE_QUOTA_PRIVILEGE, true, true);
        NtInterop.SetSystemInfo(NtInterop.SystemInformationClass.SystemCombinePhysicalMemoryInformation);
    }

    /// <summary>7. 注册表协调</summary>
    public static void RegistryReconciliation()
    {
        NtInterop.SetPrivilege(NtInterop.SE_INCREASE_QUOTA_PRIVILEGE, true, true);
        NtInterop.SetSystemInfo(NtInterop.SystemInformationClass.SystemRegistryReconciliationInformation);
    }

    // ── 组合操作 ──

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

    public static void RunAll()
    {
        foreach (var (_, _, action) in AllOperations)
            action();
    }

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
