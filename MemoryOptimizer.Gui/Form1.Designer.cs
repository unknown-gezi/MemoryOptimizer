#nullable enable
namespace MemoryOptimizer.Gui;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(520, 620);
        MinimumSize = new Size(536, 660);
        Text = "MemoryOptimizer";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);

        // ── 标题 ──
        var lblTitle = NewLabel("MemoryOptimizer", new Point(20, 18), 16, FontStyle.Bold, Color.Cyan);
        lblTitle.AutoSize = true;

        var lblSub = NewLabel("从 PCL-CE 提取的 Windows 内存优化工具", new Point(20, 48), 9,
            FontStyle.Regular, Color.Gray);
        lblSub.AutoSize = true;

        // ── 内存状态面板 ──
        var pnlStatus = new Panel
        {
            Location = new Point(20, 78),
            Size = new Size(480, 90),
            BackColor = Color.FromArgb(45, 45, 48),
            BorderStyle = BorderStyle.FixedSingle
        };
        lblMemTotal = NewLabel("总内存: --", new Point(14, 10), 10, FontStyle.Regular, Color.LightGray);
        lblMemUsed = NewLabel("已用: --", new Point(14, 30), 10, FontStyle.Regular, Color.LightGray);
        lblMemAvail = NewLabel("可用: --", new Point(250, 10), 10, FontStyle.Regular, Color.LightGray);
        lblMemLoad = NewLabel("负载: --", new Point(250, 30), 10, FontStyle.Regular, Color.LightGray);
        progressMem = new ProgressBar
        {
            Location = new Point(14, 56),
            Size = new Size(452, 20),
            Style = ProgressBarStyle.Continuous,
            ForeColor = Color.SteelBlue,
            BackColor = Color.FromArgb(60, 60, 60)
        };
        pnlStatus.Controls.AddRange([lblMemTotal, lblMemUsed, lblMemAvail, lblMemLoad, progressMem]);

        // ── 操作选择区域 ──
        var grpOps = new GroupBox
        {
            Text = "优化操作",
            Location = new Point(20, 185),
            Size = new Size(480, 240),
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White
        };

        // Select All / Deselect All
        var btnSelAll = NewButton("全选", new Point(14, 22), new Size(70, 26), 9, Color.DimGray);
        btnSelAll.Click += (_, _) => SetAllChecked(true);
        var btnSelNone = NewButton("取消", new Point(90, 22), new Size(70, 26), 9, Color.DimGray);
        btnSelNone.Click += (_, _) => SetAllChecked(false);

        // 7 个操作的 CheckBox
        int y = 56;
        int i = 0;
        foreach (var (name, desc, _) in Core.MemSwap.AllOperations)
        {
            var cb = new CheckBox
            {
                Text = $"{desc} ({name})",
                Location = new Point(18, y),
                AutoSize = true,
                Checked = true,
                Tag = name,
                ForeColor = Color.White
            };
            cb.CheckedChanged += (_, _) => UpdateButtonState();
            checkOps.Add(cb);
            grpOps.Controls.Add(cb);
            y += 26;
            i++;
        }

        grpOps.Controls.AddRange([btnSelAll, btnSelNone]);

        // ── 执行按钮 ──
        btnOptimize = new Button
        {
            Text = "⚡ 执行优化",
            Location = new Point(20, 440),
            Size = new Size(480, 44),
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnOptimize.FlatAppearance.BorderSize = 0;
        btnOptimize.Click += BtnOptimize_Click;

        // ── 日志区域 ──
        var grpLog = new GroupBox
        {
            Text = "执行日志",
            Location = new Point(20, 496),
            Size = new Size(480, 106),
            ForeColor = Color.White
        };
        txtLog = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Location = new Point(10, 22),
            Size = new Size(460, 74),
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.LimeGreen,
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 9)
        };
        grpLog.Controls.Add(txtLog);

        Controls.AddRange([
            lblTitle, lblSub, pnlStatus, grpOps, btnOptimize, grpLog
        ]);
    }

    #endregion

    // ── 控件字段 ──
    private Label lblMemTotal = null!, lblMemUsed = null!, lblMemAvail = null!, lblMemLoad = null!;
    private ProgressBar progressMem = null!;
    private Button btnOptimize = null!;
    private TextBox txtLog = null!;
    private readonly List<CheckBox> checkOps = [];
    private System.Windows.Forms.Timer? refreshTimer;

    // ── Helper ──
    private static Label NewLabel(string text, Point loc, float size, FontStyle style, Color color) => new()
    {
        Text = text, Location = loc,
        Font = new Font("Segoe UI", size, style),
        ForeColor = color, BackColor = Color.Transparent
    };

    private static Button NewButton(string text, Point loc, Size sz, float size, Color back) => new()
    {
        Text = text, Location = loc, Size = sz,
        Font = new Font("Segoe UI", size),
        FlatStyle = FlatStyle.Flat,
        BackColor = back, ForeColor = Color.White,
        Cursor = Cursors.Hand
    };
}
