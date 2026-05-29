using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using MemoryOptimizer.Core;

// ═══ Worker 模式 ═══
if (args.Length >= 1 && args[0] == "--worker")
{
    bool silent = args.Length < 2 || args[1] != "-v";
    try
    {
        NtInterop.AcquirePrivileges();
        var before = NtInterop.GetAvailableMemoryBytes();
        var loadBefore = NtInterop.GetMemoryLoadPercent();

        int ok = 0, fail = 0;
        foreach (var (_, _, action) in MemSwap.AllOperations)
        {
            try { action(); ok++; }
            catch { fail++; }
        }

        var after = NtInterop.GetAvailableMemoryBytes();
        var freed = (long)after - (long)before;
        var loadAfter = NtInterop.GetMemoryLoadPercent();

        if (silent)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"释放 {FormatHelper.BytesToReadable(freed)}  |  "
                + $"{FormatHelper.BytesToReadable((long)before)} → {FormatHelper.BytesToReadable((long)after)}"
                + $"  |  负载 {loadBefore:F0}% → {loadAfter:F0}%");
            Console.ResetColor();
            if (fail > 0) Console.WriteLine($"{fail} 项失败");
        }
        else
        {
            Console.WriteLine($"优化前: {FormatHelper.BytesToReadable((long)before)} ({loadBefore:F0}%)");
            Console.WriteLine($"优化后: {FormatHelper.BytesToReadable((long)after)} ({loadAfter:F0}%)");
            Console.WriteLine($"释放:   {FormatHelper.BytesToReadable(freed)}  |  {ok} 成功, {fail} 失败");
        }

        if (fail > 0) { Console.ReadKey(true); }
        return fail > 0 ? 4 : 0;
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"错误：{ex.Message}");
        Console.ResetColor();
        Console.ReadKey(true);
        return 4;
    }
}

// ═══ 主模式 ═══
if (!OperatingSystem.IsWindows()) return 1;

string self = Environment.ProcessPath!;
var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
    .IsInRole(WindowsBuiltInRole.Administrator);

// 双击或非管理员：直接弹出 UAC
if (args.Length == 0 || !isAdmin)
{
    try
    {
        var psi = new ProcessStartInfo(self, "--worker")
        {
            UseShellExecute = true,
            Verb = "runas"
        };
        Process.Start(psi)!.WaitForExit();
        return 0;
    }
    catch (Win32Exception) { return 2; }
}

// 管理员终端 + 有参数：保持原有命令行交互
Console.WriteLine("MemoryOptimizer CLI — 管理员模式");
Console.WriteLine("双击 exe 即可一键静默优化");
Console.WriteLine("用法: [--all] [-y] [-v] [操作名...]");
return 0;
