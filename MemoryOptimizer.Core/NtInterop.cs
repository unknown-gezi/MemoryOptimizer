using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MemoryOptimizer.Core;

/// <summary>
/// Windows NT 内核互操作 — 原样提取自 PCL-CE 的 NtInterop。
/// </summary>
public static class NtInterop
{
    // ── ntdll.dll ──

    [DllImport("ntdll.dll")]
    private static extern ulong RtlNtStatusToDosError(uint status);

    [DllImport("ntdll.dll")]
    private static extern uint RtlAdjustPrivilege(
        uint privilege, [MarshalAs(UnmanagedType.U1)] bool enable,
        [MarshalAs(UnmanagedType.U1)] bool currentThread,
        [MarshalAs(UnmanagedType.U1)] out bool enabled);

    [DllImport("ntdll.dll")]
    private static extern uint NtSetSystemInformation(
        int systemInformationClass, IntPtr systemInformation, int systemInformationLength);

    [DllImport("ntdll.dll")]
    private static extern void RtlGetNtVersionNumbers(out int major, out int minor, out int build);

    // ── kernel32.dll ──

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    // ── 常量 ──

    public const uint SE_INCREASE_QUOTA_PRIVILEGE = 5;

    public enum SystemInformationClass
    {
        SystemMemoryListInformation = 80,
        SystemFileCacheInformationEx = 81,
        SystemCombinePhysicalMemoryInformation = 130,
        SystemRegistryReconciliationInformation = 155
    }

    public enum SystemMemoryListCommand
    {
        MemoryPurgeStandbyList = 1,
        MemoryFlushModifiedList = 2,
        MemoryEmptyWorkingSets = 4,
        MemoryPurgeLowPriorityStandbyList = 8
    }

    // ── 公开方法（与 PCL-CE 一致）──

    public static bool SetPrivilege(uint privilege, bool enable, bool currentThread)
    {
        var result = RtlAdjustPrivilege(privilege, enable, currentThread, out var wasEnabled);
        if (result != 0)
            ThrowLastWin32Error((int)RtlNtStatusToDosError(result));
        return wasEnabled;
    }

    /// <summary>设置系统信息，传入一个 int 值（4 字节）</summary>
    public static void SetSystemInfo(SystemInformationClass infoClass, int value)
    {
        var val = value;
        var ptr = Marshal.AllocHGlobal(sizeof(int));
        try
        {
            Marshal.WriteInt32(ptr, val);
            var result = NtSetSystemInformation((int)infoClass, ptr, sizeof(int));
            if (result != 0)
                ThrowLastWin32Error((int)RtlNtStatusToDosError(result));
        }
        finally { Marshal.FreeHGlobal(ptr); }
    }

    /// <summary>设置系统信息，空缓冲区</summary>
    public static void SetSystemInfo(SystemInformationClass infoClass)
    {
        var result = NtSetSystemInformation((int)infoClass, IntPtr.Zero, 0);
        if (result != 0)
            ThrowLastWin32Error((int)RtlNtStatusToDosError(result));
    }

    /// <summary>设置文件缓存信息（两个 DWORD = 8 字节）</summary>
    public static void SetFileCacheInfo(long value)
    {
        var val = value;
        var ptr = Marshal.AllocHGlobal(sizeof(long));
        try
        {
            Marshal.WriteInt64(ptr, val);
            var result = NtSetSystemInformation((int)SystemInformationClass.SystemFileCacheInformationEx, ptr, sizeof(long));
            if (result != 0)
                ThrowLastWin32Error((int)RtlNtStatusToDosError(result));
        }
        finally { Marshal.FreeHGlobal(ptr); }
    }

    // ── 内存查询 ──

    public static ulong GetAvailableMemoryBytes()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref status)) ThrowLastWin32Error();
        return status.ullAvailPhys;
    }

    public static ulong GetTotalMemoryBytes()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref status)) ThrowLastWin32Error();
        return status.ullTotalPhys;
    }

    public static double GetMemoryLoadPercent()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref status)) ThrowLastWin32Error();
        return status.dwMemoryLoad;
    }

    public static Version GetOsVersion()
    {
        RtlGetNtVersionNumbers(out var major, out var minor, out var build);
        return new Version(major, minor, build & 0xFFFF);
    }

    private static void ThrowLastWin32Error(int? code = null)
        => throw new Win32Exception(code ?? Marshal.GetLastWin32Error());
}
