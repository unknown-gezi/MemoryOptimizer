using System.ComponentModel;
using MemoryOptimizer.Core;

bool isDoubleClick = args.Length == 0;
int exitCode = Run(args, isDoubleClick);
if (isDoubleClick && exitCode != 0) WaitForExit();
return exitCode;

static int Run(string[] args, bool isDoubleClick)
{
    if (!OperatingSystem.IsWindows()) { Console.WriteLine("仅支持 Windows。"); return 1; }

    if (!IsAdministrator())
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("需要管理员权限！请右键 → 以管理员身份运行。");
        Console.ResetColor();
        return 2;
    }

    // ── 双击 = 静默全优化 ──
    if (isDoubleClick)
    {
        try
        {
            var before = NtInterop.GetAvailableMemoryBytes();
            MemSwap.RunAll();
            var after = NtInterop.GetAvailableMemoryBytes();
            var freed = (long)after - (long)before;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"内存优化完成 — 释放 {FormatHelper.BytesToReadable(freed)}");
            Console.ResetColor();
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"错误：{ex.Message}");
            Console.ResetColor();
            return 4;
        }
    }

    // ── 命令行模式 ──
    var argsList = args.ToList();
    if (argsList.Contains("--help") || argsList.Contains("-h")) { PrintHelp(); return 0; }

    bool showBefore = argsList.Contains("--show-before") || argsList.Contains("-v");
    bool skipConfirm = argsList.Contains("--yes") || argsList.Contains("-y");
    bool runAll = argsList.Contains("--all") || argsList.Contains("-a");
    var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "--yes", "-y", "--show-before", "-v", "--all", "-a" };
    var opNames = argsList.Where(a => !flags.Contains(a)).ToList();
    if (opNames.Count == 0 && !runAll) { PrintHelp(); return 0; }

    try
    {
        if (showBefore) ShowStatus("优化前");
        var availBefore = NtInterop.GetAvailableMemoryBytes();

        if (!skipConfirm)
        {
            Console.Write($"内存负载 {NtInterop.GetMemoryLoadPercent():F0}%，确认执行？[y/N] ");
            if (Console.ReadKey().Key != ConsoleKey.Y) { Console.WriteLine("\n已取消。"); return 0; }
            Console.WriteLine();
        }

        Console.WriteLine();
        if (runAll) { MemSwap.RunAll(); Console.WriteLine("✓ 全部 7 项完成"); }
        else foreach (var n in opNames)
            {
                var d = MemSwap.AllOperations.FirstOrDefault(o => string.Equals(o.Name, n, StringComparison.OrdinalIgnoreCase));
                if (d.Description == null) { Console.WriteLine($"⚠ 未知: {n}"); continue; }
                Console.Write($"{d.Description}... "); MemSwap.RunByName(n); Console.WriteLine("✓");
            }

        Console.WriteLine();
        ShowStatus("优化后");
        var delta = (long)NtInterop.GetAvailableMemoryBytes() - (long)availBefore;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"释放: {FormatHelper.BytesToReadable(delta)}");
        Console.ResetColor();
        return 0;
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"错误：{ex.Message}");
        Console.ResetColor();
        return 4;
    }
}

static void WaitForExit() { try { Console.Write("\n按任意键退出..."); Console.ReadKey(true); } catch { } }
static bool IsAdministrator() => new System.Security.Principal.WindowsPrincipal(
    System.Security.Principal.WindowsIdentity.GetCurrent())
    .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
static void ShowStatus(string label)
{
    var t = NtInterop.GetTotalMemoryBytes(); var a = NtInterop.GetAvailableMemoryBytes();
    Console.WriteLine($"[{label}] 总计 {FormatHelper.BytesToReadable((long)t)} | 已用 {FormatHelper.BytesToReadable((long)(t-a))} ({NtInterop.GetMemoryLoadPercent():F0}%) | 可用 {FormatHelper.BytesToReadable((long)a)}");
}
static void PrintHelp()
{
    Console.WriteLine("MemoryOptimizer CLI — 从 PCL-CE 提取\n");
    Console.WriteLine("用法: MemoryOptimizer.Cli.exe [选项] [操作名...]");
    Console.WriteLine("  --all -a    全操作    --yes -y  跳过确认    --show-before -v  显示状态");
    Console.WriteLine("\n双击 exe（管理员）= 一键静默优化");
}
