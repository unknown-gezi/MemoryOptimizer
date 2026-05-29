using System.ComponentModel;
using MemoryOptimizer.Core;

bool isDoubleClick = args.Length == 0;
int exitCode = Run(args, isDoubleClick);
if (isDoubleClick && exitCode != 0) WaitForExit(); // 仅在出错时暂停
return exitCode;

static int Run(string[] args, bool isDoubleClick)
{
    if (!OperatingSystem.IsWindows())
    {
        Console.WriteLine("错误：此工具仅支持 Windows。");
        return 1;
    }

    if (!IsAdministrator())
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("错误：需要管理员权限！");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("请右键点击此程序 → \u201c以管理员身份运行\u201d");
        Console.WriteLine();
        Console.WriteLine("提示：固定到任务栏后，按住 Ctrl+Shift 点击 = 管理员运行");
        return 2;
    }

    // ── 双击启动：静默执行全部操作 ──
    if (isDoubleClick)
    {
        try
        {
            var availBefore = NtInterop.GetAvailableMemoryBytes();
            MemSwap.RunAll();
            var availAfter = NtInterop.GetAvailableMemoryBytes();
            var freed = (long)availAfter - (long)availBefore;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"内存优化完成 — 释放 {FormatHelper.BytesToReadable(freed)}");
            Console.ResetColor();
            return 0;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 5)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("错误：访问被拒绝 — 请以管理员身份运行。");
            Console.ResetColor();
            return 3;
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

    if (argsList.Contains("--help") || argsList.Contains("-h"))
    {
        PrintHelp();
        return 0;
    }

    bool showBefore = argsList.Contains("--show-before") || argsList.Contains("-v");
    bool skipConfirm = argsList.Contains("--yes") || argsList.Contains("-y");
    bool runAll = argsList.Contains("--all") || argsList.Contains("-a");

    var opFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "--yes", "-y", "--show-before", "-v", "--all", "-a" };
    var opNames = argsList.Where(a => !opFlags.Contains(a)).ToList();
    if (opNames.Count == 0 && !runAll)
    {
        PrintHelp();
        return 0;
    }

    try
    {
        if (showBefore) ShowMemoryStatus("优化前");

        var availBefore = NtInterop.GetAvailableMemoryBytes();

        if (!skipConfirm)
        {
            var load = NtInterop.GetMemoryLoadPercent();
            Console.Write($"当前内存负载 {load:F0}%，可用 {FormatHelper.BytesToReadable((long)availBefore)}。");
            Console.Write(" 确认执行？[y/N] ");
            var key = Console.ReadKey();
            Console.WriteLine();
            if (key.Key != ConsoleKey.Y) { Console.WriteLine("已取消。"); return 0; }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("══════ 开始内存优化 ══════");
        Console.ResetColor();

        if (runAll)
        {
            MemSwap.RunAll();
            Console.WriteLine("✓ 已执行全部 7 项操作");
        }
        else
        {
            foreach (var name in opNames)
            {
                var desc = MemSwap.AllOperations
                    .FirstOrDefault(o => string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase)).Description;
                if (desc == null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"⚠ 未知操作: {name}（已跳过）");
                    Console.ResetColor();
                    continue;
                }
                Console.Write($"执行 {desc} ({name})... ");
                MemSwap.RunByName(name);
                Console.WriteLine("✓");
            }
        }

        Console.WriteLine();
        ShowMemoryStatus("优化后");

        var delta = (long)NtInterop.GetAvailableMemoryBytes() - (long)availBefore;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"释放内存: {FormatHelper.BytesToReadable(delta)}");
        Console.ResetColor();

        return 0;
    }
    catch (Win32Exception ex) when (ex.NativeErrorCode == 5)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("错误：访问被拒绝 — 请以管理员身份运行。");
        Console.ResetColor();
        return 3;
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"错误：{ex.Message}");
        Console.ResetColor();
        return 4;
    }
}

// ── 辅助 ──

static void WaitForExit()
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.Write("按任意键退出...");
    Console.ResetColor();
    Console.ReadKey(intercept: true);
}

static bool IsAdministrator()
{
    using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
    var principal = new System.Security.Principal.WindowsPrincipal(identity);
    return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
}

static void ShowMemoryStatus(string label)
{
    var total = NtInterop.GetTotalMemoryBytes();
    var avail = NtInterop.GetAvailableMemoryBytes();
    var used = total - avail;
    var load = NtInterop.GetMemoryLoadPercent();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"[{label}] 总计 {FormatHelper.BytesToReadable((long)total)} | "
        + $"已用 {FormatHelper.BytesToReadable((long)used)} ({load:F0}%) | "
        + $"可用 {FormatHelper.BytesToReadable((long)avail)}");
    Console.ResetColor();
}

static void PrintHelp()
{
    Console.WriteLine("MemoryOptimizer CLI — 从 PCL-CE 提取的内存优化工具");
    Console.WriteLine("需要管理员权限 | 仅支持 Windows");
    Console.WriteLine();
    Console.WriteLine("用法: MemoryOptimizer.Cli.exe [选项] [操作名...]");
    Console.WriteLine();
    Console.WriteLine("选项:");
    Console.WriteLine("  --all, -a          执行全部 7 项操作");
    Console.WriteLine("  --yes, -y          跳过确认提示");
    Console.WriteLine("  --show-before, -v  显示执行前后内存对比");
    Console.WriteLine("  --help, -h         显示帮助");
    Console.WriteLine();
    Console.WriteLine("可用操作:");
    foreach (var (name, desc, _) in MemSwap.AllOperations)
        Console.WriteLine($"  {name,-28} {desc}");
    Console.WriteLine();
    Console.WriteLine("示例:");
    Console.WriteLine("  MemoryOptimizer.Cli.exe --all -y      命令行静默执行");
    Console.WriteLine("  MemoryOptimizer.Cli.exe -v            显示内存对比并确认");
    Console.WriteLine("  MemoryOptimizer.Cli.exe PurgeStandbyList FlushFileCache");
}
