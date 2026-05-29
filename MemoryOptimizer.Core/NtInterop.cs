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
    private static extern uint RtlAdjustPrivilege(
        uint privilege, [MarshalAs(UnmanagedType.U1)] bool enable,
        [MarshalAs(UnmanagedType.U1)] bool currentThread,
        [MarshalAs(UnmanagedType.U1)] out bool enabled);

    [DllImport("ntdll.dll")]
    private static extern ulong RtlNtStatusToDosError(uint status);

    [DllImport("ntdll.dll")]
    private static extern uint NtSetSystemInformation(
        int systemInformationClass, IntPtr systemInformation, int systemInformationLength);

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

    private const uint SE_INCREASE_QUOTA_PRIVILEGE = 5;

    public enum SystemInformationClass
    {
        SystemMemoryListInformation = 80,
        SystemFileCacheInformationEx = 81,
        SystemFileCacheInformation = 21,
        SystemCombinePhysicalMemoryInformation = 130,
        SystemRegistryReconciliationInformation = 155
    }

    public static bool SetPrivilege(bool enable)
    {
        var result = RtlAdjustPrivilege(SE_INCREASE_QUOTA_PRIVILEGE, enable, true, out var wasEnabled);
        if (result != 0)
            ThrowLastWin32Error((int)RtlNtStatusToDosError(result));
        return wasEnabled;
    }

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

    public static void SetSystemInfoRaw(int infoClass, IntPtr ptr, int len)
    {
        var result = NtSetSystemInformation(infoClass, ptr, len);
        if (result != 0)
            ThrowLastWin32Error((int)RtlNtStatusToDosError(result));
    }

    public static ulong GetAvailableMemoryBytes()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref status))
            ThrowLastWin32Error();
        return status.ullAvailPhys;
    }

    public static ulong GetTotalMemoryBytes()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref status))
            ThrowLastWin32Error();
        return status.ullTotalPhys;
    }

    public static double GetMemoryLoadPercent()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref status))
            ThrowLastWin32Error();
        return status.dwMemoryLoad;
    }

    [DllImport("ntdll.dll")]
    private static extern void RtlGetNtVersionNumbers(out int major, out int minor, out int build);

    public static Version GetOsVersion()
    {
        RtlGetNtVersionNumbers(out var major, out var minor, out var build);
        return new Version(major, minor, build & 0xFFFF);
    }

    private static void ThrowLastWin32Error(int? code = null)
        => throw new Win32Exception(code ?? Marshal.GetLastWin32Error());
}
