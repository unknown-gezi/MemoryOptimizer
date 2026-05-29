using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MemoryOptimizer.Core;

/// <summary>
/// Windows NT 内核互操作 — 用于内存优化的底层 API 调用。
/// 需要管理员权限。
/// </summary>
public static class NtInterop
{
    // ── ntdll.dll ──

    [DllImport("ntdll.dll")]
    private static extern uint NtSetSystemInformation(
        int systemInformationClass, IntPtr systemInformation, int systemInformationLength);

    [DllImport("ntdll.dll")]
    private static extern ulong RtlNtStatusToDosError(uint status);

    // ── advapi32.dll — 特权管理 ──

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(
        IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LookupPrivilegeValue(
        string? lpSystemName, string lpName, out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AdjustTokenPrivileges(
        IntPtr tokenHandle, [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
        ref TOKEN_PRIVILEGES newState, uint bufferLength,
        IntPtr previousState, IntPtr returnLength);

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID Luid;
        public uint Attributes;
    }

    private const uint TOKEN_QUERY = 0x0008;
    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const uint SE_PRIVILEGE_ENABLED = 0x0002;
    private const string SE_INCREASE_QUOTA_PRIVILEGE_NAME = "SeIncreaseQuotaPrivilege";

    // ── kernel32.dll ──

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

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

    public enum SystemInformationClass
    {
        SystemMemoryListInformation = 80,
        SystemFileCacheInformationEx = 81,
        SystemFileCacheInformation = 21,
        SystemCombinePhysicalMemoryInformation = 130,
        SystemRegistryReconciliationInformation = 155
    }

    // ── 特权管理 ──

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    // ... other kernel32 imports ...

    /// <summary>
    /// 使用标准 AdjustTokenPrivileges API 启用 SeIncreaseQuotaPrivilege。
    /// 这是执行内存操作的前置条件。失败时允许静默忽略（部分系统不支持此特权）。
    /// </summary>
    public static bool EnableIncreaseQuotaPrivilege()
    {
        try
        {
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY | TOKEN_ADJUST_PRIVILEGES, out var token))
                return false;

            try
            {
                if (!LookupPrivilegeValue(null, SE_INCREASE_QUOTA_PRIVILEGE_NAME, out var luid))
                    return false;

                var tp = new TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Luid = luid,
                    Attributes = SE_PRIVILEGE_ENABLED
                };

                if (!AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
                    return false;

                // AdjustTokenPrivileges 可能返回 true 但特权并未启用
                return Marshal.GetLastWin32Error() != 0x514; // ERROR_NOT_ALL_ASSIGNED
            }
            finally
            {
                CloseHandle(token);
            }
        }
        catch
        {
            return false;
        }
    }

    // ── 系统信息操作 ──

    /// <summary>调用 NtSetSystemInformation（long 值）</summary>
    public static void SetSystemInfo(SystemInformationClass infoClass, long value = 0)
    {
        var val = value;
        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(val));
        try
        {
            Marshal.WriteInt64(ptr, val);
            var result = NtSetSystemInformation((int)infoClass, ptr, Marshal.SizeOf(val));
            if (result != 0)
                ThrowLastWin32Error((int)RtlNtStatusToDosError(result));
        }
        finally { Marshal.FreeHGlobal(ptr); }
    }

    /// <summary>调用 NtSetSystemInformation（原始指针）</summary>
    public static void SetSystemInfoRaw(int infoClass, IntPtr ptr, int len)
    {
        var result = NtSetSystemInformation(infoClass, ptr, len);
        if (result != 0)
            ThrowLastWin32Error((int)RtlNtStatusToDosError(result));
    }

    // ── 内存状态查询 ──

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

    // ── OS 版本 ──

    [DllImport("ntdll.dll")]
    private static extern void RtlGetNtVersionNumbers(out int major, out int minor, out int build);

    public static Version GetOsVersion()
    {
        RtlGetNtVersionNumbers(out var major, out var minor, out var build);
        return new Version(major, minor, build & 0xFFFF);
    }

    // ── 错误处理 ──

    private static void ThrowLastWin32Error(int? code = null)
        => throw new Win32Exception(code ?? Marshal.GetLastWin32Error());
}
