using System.ComponentModel;
using System.Security.Principal;
using MemoryOptimizer.Core;

namespace MemoryOptimizer.Gui;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();

        // 管理员检查
        Load += (_, _) =>
        {
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator))
            {
                MessageBox.Show("需要管理员权限！\n请右键 → 以管理员身份运行。",
                    "权限不足", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Close();
                return;
            }

            NtInterop.AcquirePrivileges();
            btnOptimize.Enabled = true;
            UpdateDisplay();
        };

        // 每秒刷新
        timer = new System.Windows.Forms.Timer { Interval = 1000 };
        timer.Tick += (_, _) => UpdateDisplay();
        timer.Start();
    }

    // ── 更新显示 ──
    private void UpdateDisplay()
    {
        try
        {
            var total = NtInterop.GetTotalMemoryBytes();
            var avail = NtInterop.GetAvailableMemoryBytes();
            var used = total - avail;
            _memLoad = NtInterop.GetMemoryLoadPercent();

            lblGaugePct.Text = $"{_memLoad:F0}%";
            UpdateDetailRow(lblTotal, FormatHelper.BytesToReadable((long)total));
            UpdateDetailRow(lblUsed, FormatHelper.BytesToReadable((long)used));
            UpdateDetailRow(lblAvail, FormatHelper.BytesToReadable((long)avail));

            // 负载颜色
            lblGaugePct.ForeColor = _memLoad switch
            {
                > 85 => Color.FromArgb(240, 80, 60),
                > 70 => Color.FromArgb(240, 180, 40),
                _ => Color.White
            };

            pnlGauge.Invalidate();
        }
        catch { }
    }

    private static void UpdateDetailRow(Label lbl, string value)
    {
        lbl.Tag = new { Key = ((dynamic)lbl.Tag!).Key, Value = value };
        lbl.Invalidate();
    }

    // ── 优化按钮 ──
    private async void BtnOptimize_Click(object? sender, EventArgs e)
    {
        if (_isRunning) return;
        _isRunning = true;
        btnOptimize.Enabled = false;
        lblResult.Visible = false;
        lblStatus.Text = "● 优化中...";
        lblStatus.ForeColor = Color.FromArgb(240, 180, 40);

        var before = NtInterop.GetAvailableMemoryBytes();

        await Task.Run(() =>
        {
            foreach (var (_, _, action) in MemSwap.AllOperations)
            {
                try { action(); } catch { }
            }
        });

        var after = NtInterop.GetAvailableMemoryBytes();
        var freed = (long)after - (long)before;

        lblResult.Text = $"释放 {FormatHelper.BytesToReadable(freed)}";
        lblResult.Visible = true;
        lblStatus.Text = "● 就绪";
        lblStatus.ForeColor = Color.FromArgb(100, 200, 100);

        _isRunning = false;
        btnOptimize.Enabled = true;
        UpdateDisplay();
    }
}
