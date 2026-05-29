using System.Runtime.InteropServices;

namespace MemoryOptimizer.Core;

/// <summary>
/// 内存优化引擎 — 完全照搬 PCL-CE 的 MemSwapWorks。
/// </summary>
public static class MemSwap
{
    // ── PCL-CE 原版结构体 ──

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_FILECACHE_INFORMATION
    {
        public UIntPtr CurrentSize;
        public UIntPtr PeakSize;
        public uint PageFaultCount;
        public UIntPtr MinimumWorkingSet;
        public UIntPtr MaximumWorkingSet;
        public UIntPtr CurrentSizeIncludingTransitionInPages;
        public UIntPtr PeakSizeIncludingTransitionInPages;
        public uint TransitionRePurposeCount;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORY_COMBINE_INFORMATION_EX
    {
        public IntPtr Handle;
        public UIntPtr PagesCombined;
        public uint Flags;
    }

    // ── PCL-CE 原版辅助方法 ──

    private static void _ExecuteListOp(int infoValue)
    {
        var handle = GCHandle.Alloc(infoValue, GCHandleType.Pinned);
        try
        {
            NtInterop.SetSystemInformation(
                NtInterop.SystemInformationClass.SystemMemoryListInformation,
                handle.AddrOfPinnedObject(),
                (uint)sizeof(int));
        }
        finally { if (handle.IsAllocated) handle.Free(); }
    }

    private static void _ExecuteStructOp<T>(T structure, NtInterop.SystemInformationClass infoClass)
    {
        var handle = GCHandle.Alloc(structure, GCHandleType.Pinned);
        try
        {
            NtInterop.SetSystemInformation(
                infoClass,
                handle.AddrOfPinnedObject(),
                (uint)Marshal.SizeOf(structure!));
        }
        finally { if (handle.IsAllocated) handle.Free(); }
    }

    // ── 7 种操作（与 PCL-CE 完全一致）──

    /// <summary>1. 清空所有进程的工作集</summary>
    public static void EmptyWorkingSets() => _ExecuteListOp(2);

    /// <summary>2. 清空文件系统缓存</summary>
    public static void FlushFileCache()
    {
        var scfi = new SYSTEM_FILECACHE_INFORMATION
        {
            MaximumWorkingSet = UIntPtr.MaxValue,
            MinimumWorkingSet = UIntPtr.MaxValue
        };
        _ExecuteStructOp(scfi, NtInterop.SystemInformationClass.SystemFileCacheInformationEx);
    }

    /// <summary>3. 清空已修改页面列表</summary>
    public static void FlushModifiedList() => _ExecuteListOp(3);

    /// <summary>4. 清空备用页面列表</summary>
    public static void PurgeStandbyList() => _ExecuteListOp(4);

    /// <summary>5. 清空低优先级备用列表</summary>
    public static void PurgeLowPriorityStandbyList() => _ExecuteListOp(5);

    /// <summary>6. 注册表协调</summary>
    public static void RegistryReconciliation()
    {
        NtInterop.SetSystemInformation(
            NtInterop.SystemInformationClass.SystemRegistryReconciliationInformation,
            IntPtr.Zero, 0);
    }

    /// <summary>7. 合并物理内存</summary>
    public static void CombinePhysicalMemory()
    {
        var combineInfoEx = new MEMORY_COMBINE_INFORMATION_EX();
        _ExecuteStructOp(combineInfoEx, NtInterop.SystemInformationClass.SystemCombinePhysicalMemoryInformation);
    }

    // ── 组合操作 ──

    public static IReadOnlyList<(string Name, string Description, Action Action)> AllOperations { get; } =
    [
        ("EmptyWorkingSets",         "清空工作集",           EmptyWorkingSets),
        ("FlushFileCache",           "清空文件缓存",          FlushFileCache),
        ("FlushModifiedList",        "清空修改页面列表",      FlushModifiedList),
        ("PurgeStandbyList",         "清空备用列表",          PurgeStandbyList),
        ("PurgeLowPriorityStandby",  "清空低优先级备用列表",   PurgeLowPriorityStandbyList),
        ("RegistryReconciliation",   "注册表协调",            RegistryReconciliation),
        ("CombinePhysicalMemory",    "合并物理内存",          CombinePhysicalMemory),
    ];

    public static void RunAll()
    {
        foreach (var (_, _, action) in AllOperations) action();
    }

    public static bool RunByName(string name)
    {
        foreach (var (n, _, action) in AllOperations)
        {
            if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
            { action(); return true; }
        }
        return false;
    }
}
