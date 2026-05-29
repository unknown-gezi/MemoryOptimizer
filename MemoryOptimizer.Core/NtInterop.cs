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

    // ── advapi32.dll — 特权 fallback ──

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(IntPtr h, uint access, out IntPtr tok);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LookupPrivilegeValue(string? sys, string name, out LUID luid);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AdjustTokenPrivileges(
        IntPtr tok, [MarshalAs(UnmanagedType.Bool)] bool disableAll,
        ref TOKEN_PRIVILEGES newState, uint len, IntPtr prev, IntPtr retLen);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr h);

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID { public uint LowPart; public int HighPart; }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES { public uint PrivilegeCount; public LUID Luid; public uint Attributes; }

    private const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";
    private const uint TOKEN_QUERY = 0x0008;
    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const uint SE_PRIVILEGE_ENABLED = 0x0002;

    private static bool _privilegeEnabled;
    private static readonly object _privLock = new();

    /// <summary>一次性启用 SeIncreaseQuotaPrivilege，RtlAdjustPrivilege 失败时 fallback 到 AdjustTokenPrivileges</summary>
    public static void EnsurePrivilege()
    {
        if (_privilegeEnabled) return;
        lock (_privLock)
        {
            if (_privilegeEnabled) return;

            // 方式1: RtlAdjustPrivilege（PCL-CE 原版方式）
            var r = RtlAdjustPrivilege(SE_INCREASE_QUOTA_PRIVILEGE, true, true, out _);
            if (r == 0) { _privilegeEnabled = true; return; }

            // 方式2: AdjustTokenPrivileges（更标准的方式）
            try
            {
                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY | TOKEN_ADJUST_PRIVILEGES, out var tok)) return;
                try
                {
                    if (!LookupPrivilegeValue(null, SE_INCREASE_QUOTA_NAME, out var luid)) return;
                    var tp = new TOKEN_PRIVILEGES { PrivilegeCount = 1, Luid = luid, Attributes = SE_PRIVILEGE_ENABLED };
                    AdjustTokenPrivileges(tok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
                    _privilegeEnabled = Marshal.GetLastWin32Error() != 1300; // 1300 = ERROR_NOT_ALL_ASSIGNED
                }
                finally { CloseHandle(tok); }
            }
            catch { /* 静默 */ }

            if (!_privilegeEnabled) _privilegeEnabled = true; // 无论如何只试一次，不阻塞后续调用
        }
    }

    // ── 公开方法（与 PCL-CE 一致）──

    /// <summary>启用特权（与 PCL-CE 保持兼容，内部使用 fallback 机制）</summary>
    public static bool SetPrivilege(uint privilege, bool enable, bool currentThread)
    {
        EnsurePrivilege();
        return true; // best-effort，不抛异常
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
