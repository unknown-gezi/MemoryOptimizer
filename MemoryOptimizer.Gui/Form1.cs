using System.ComponentModel;
using System.Security.Principal;
using MemoryOptimizer.Core;

namespace MemoryOptimizer.Gui;

public partial class MainForm : Form
{
    private bool _isRunning;

    public MainForm()
    {
        InitializeComponent();
        UpdateMemoryDisplay();
        UpdateButtonState();

        // 每秒刷新内存状态
        refreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        refreshTimer.Tick += (_, _) => UpdateMemoryDisplay();
        refreshTimer.Start();

        Load += MainForm_Load;
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        if (!IsAdministrator())
        {
            MessageBox.Show(
                "需要管理员权限！\n\n请右键点击程序 → \u201c以管理员身份运行\u201d。",
                "权限不足",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            Close();
        }
    }

    private void UpdateMemoryDisplay()
    {
        try
        {
            var total = NtInterop.GetTotalMemoryBytes();
            var avail = NtInterop.GetAvailableMemoryBytes();
            var used = total - avail;
            var load = NtInterop.GetMemoryLoadPercent();

            lblMemTotal.Text = $"总内存: {FormatHelper.BytesToReadable((long)total)}";
            lblMemUsed.Text = $"已用:   {FormatHelper.BytesToReadable((long)used)}";
            lblMemAvail.Text = $"可用:   {FormatHelper.BytesToReadable((long)avail)}";
            lblMemLoad.Text = $"负载:   {load:F0}%";

            progressMem.Value = (int)Math.Clamp(load, 0, 100);
            progressMem.ForeColor = load switch
            {
                > 85 => Color.OrangeRed,
                > 70 => Color.Orange,
                _ => Color.SteelBlue
            };
        }
        catch { /* 忽略定时器刷新时的错误 */ }
    }

    private void SetAllChecked(bool check)
    {
        foreach (var cb in checkOps) cb.Checked = check;
    }

    private void UpdateButtonState()
    {
        btnOptimize.Enabled = checkOps.Any(cb => cb.Checked) && !_isRunning;
    }

    private async void BtnOptimize_Click(object? sender, EventArgs e)
    {
        var selected = checkOps.Where(cb => cb.Checked).Select(cb => cb.Tag!.ToString()!).ToList();
        if (selected.Count == 0) return;

        _isRunning = true;
        btnOptimize.Text = "执行中...";
        btnOptimize.Enabled = false;
        txtLog.Clear();

        var availBefore = NtInterop.GetAvailableMemoryBytes();
        Log($"══════ 开始 {selected.Count} 项优化 ══════");
        Log($"优化前可用: {FormatHelper.BytesToReadable((long)availBefore)}");

        var errors = 0;

        await Task.Run(() =>
        {
            foreach (var name in selected)
            {
                var desc = MemSwap.AllOperations.First(o => o.Name == name).Description;
                try
                {
                    Invoke(() => Log($"  {desc} ({name})..."));
                    MemSwap.RunByName(name);
                    Invoke(() => AppendLog(" ✓"));
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 5)
                {
                    errors++;
                    Invoke(() => AppendLog(" ✗ 拒绝访问"));
                }
                catch (Exception ex)
                {
                    errors++;
                    Invoke(() => AppendLog($" ✗ {ex.Message.Split('\n')[0]}"));
                }
            }
        });

        var availAfter = NtInterop.GetAvailableMemoryBytes();
        var freed = (long)availAfter - (long)availBefore;

        Log("──────────────────────────────");
        Log($"优化后可用: {FormatHelper.BytesToReadable((long)availAfter)}");
        Log($"释放: {FormatHelper.BytesToReadable(freed)}  |  错误: {errors}");
        Log("══════ 完成 ══════");

        _isRunning = false;
        btnOptimize.Text = "⚡ 执行优化";
        UpdateButtonState();
        UpdateMemoryDisplay();
    }

    private void Log(string msg)
    {
        if (InvokeRequired) { Invoke(() => Log(msg)); return; }
        txtLog.AppendText(msg + Environment.NewLine);
    }

    private void AppendLog(string msg)
    {
        if (InvokeRequired) { Invoke(() => AppendLog(msg)); return; }
        // 追加到当前最后一行末尾
        var lines = txtLog.Lines;
        if (lines.Length > 0)
        {
            lines[^1] += msg;
            txtLog.Lines = lines;
        }
        else
        {
            txtLog.AppendText(msg + Environment.NewLine);
        }
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
