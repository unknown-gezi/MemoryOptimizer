using System.ComponentModel;
using System.Security.Principal;
using MemoryOptimizer.Core;

namespace MemoryOptimizer.Gui;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();

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

        timer = new System.Windows.Forms.Timer { Interval = 1000 };
        timer.Tick += (_, _) => UpdateDisplay();
        timer.Start();
    }

    private void UpdateDisplay()
    {
        try
        {
            var total = NtInterop.GetTotalMemoryBytes();
            var avail = NtInterop.GetAvailableMemoryBytes();
            var used = total - avail;
            _memLoad = NtInterop.GetMemoryLoadPercent();

            lblGaugePct.Text = $"{_memLoad:F0}%";
            lblGaugePct.ForeColor = _memLoad switch
            {
                > 85 => Color.FromArgb(240, 80, 60),
                > 70 => Color.FromArgb(240, 180, 40),
                _ => Color.White
            };

            lblDetailTotal.Text = $"总内存    {FormatHelper.BytesToReadable((long)total)}";
            lblDetailUsed.Text  = $"已用       {FormatHelper.BytesToReadable((long)used)}";
            lblDetailAvail.Text = $"可用       {FormatHelper.BytesToReadable((long)avail)}";

            pnlGauge.Invalidate();
        }
        catch { }
    }

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
