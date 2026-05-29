namespace MemoryOptimizer.Core;

/// <summary>格式化工具</summary>
public static class FormatHelper
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    /// <summary>将字节数格式化为可读字符串（如 "1.5 GB"）</summary>
    public static string BytesToReadable(long bytes)
    {
        if (bytes == 0) return "0 B";
        var sign = bytes < 0 ? "-" : "";
        var abs = (decimal)(bytes < 0 ? -bytes : bytes);
        var unit = 0;
        while (abs >= 1024 && unit < Units.Length - 1)
        {
            abs /= 1024;
            unit++;
        }
        return $"{sign}{abs:0.##} {Units[unit]}";
    }
}
