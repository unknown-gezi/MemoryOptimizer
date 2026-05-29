namespace MemoryOptimizer.Gui;

partial class MainForm
{
    private System.ComponentModel.IContainer? components;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        // ── 窗口设置 ──
        Text = "MemoryOptimizer";
        ClientSize = new Size(420, 480);
        MinimumSize = new Size(420, 480);
        MaximumSize = new Size(420, 480);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(18, 18, 18);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        Padding = new Padding(0);

        // ── 顶部标题栏 ──
        var pnlTitle = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Color.FromArgb(24, 24, 24),
            Padding = new Padding(20, 0, 20, 0),
            ColumnCount = 2,
            RowCount = 1
        };
        pnlTitle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pnlTitle.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        lblTitle = new Label
        {
            Text = "Memory Optimizer",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent
        };

        lblStatus = new Label
        {
            Text = "● 就绪",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(100, 200, 100),
            BackColor = Color.Transparent,
            AutoSize = true
        };

        pnlTitle.Controls.Add(lblTitle, 0, 0);
        pnlTitle.Controls.Add(lblStatus, 1, 0);

        // ── 内存指示器 ──
        pnlGauge = new Panel
        {
            Size = new Size(160, 160),
            BackColor = Color.Transparent
        };
        pnlGauge.Paint += DrawGauge;

        lblGaugePct = new Label
        {
            Text = "--%",
            Font = new Font("Segoe UI", 32, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(160, 44),
            Location = new Point(0, 52)
        };
        lblGaugeLabel = new Label
        {
            Text = "内存负载",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(160, 160, 160),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(160, 22),
            Location = new Point(0, 96)
        };
        pnlGauge.Controls.Add(lblGaugePct);
        pnlGauge.Controls.Add(lblGaugeLabel);

        // ── 内存详情卡片 ──
        var pnlDetails = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 3,
            AutoSize = true,
            BackColor = Color.FromArgb(28, 28, 30)
        };
        pnlDetails.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pnlDetails.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pnlDetails.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        lblTotal = MakeDetailRow("总内存", "--");
        lblUsed = MakeDetailRow("已用", "--");
        lblAvail = MakeDetailRow("可用", "--");

        pnlDetails.Controls.Add(lblTotal, 0, 0);
        pnlDetails.Controls.Add(lblUsed, 0, 1);
        pnlDetails.Controls.Add(lblAvail, 0, 2);

        // ── 中部布局：gauge + details ──
        var pnlCenter = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new Padding(30, 30, 30, 10)
        };
        pnlCenter.Controls.Add(pnlGauge);

        var pnlDetailWrap = new Panel
        {
            Size = new Size(180, 160),
            BackColor = Color.Transparent,
            Padding = new Padding(20, 20, 0, 0)
        };
        pnlDetails.Location = new Point(0, 0);
        pnlDetailWrap.Controls.Add(pnlDetails);
        pnlCenter.Controls.Add(pnlDetailWrap);

        // ── 结果摘要 ──
        lblResult = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 200, 120),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false,
            Size = new Size(360, 30),
            Visible = false
        };

        // ── 优化按钮 ──
        btnOptimize = new Button
        {
            Text = "优化内存",
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            BackColor = Color.FromArgb(50, 140, 240),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Size = new Size(360, 50)
        };
        btnOptimize.Click += BtnOptimize_Click;
        btnOptimize.Enabled = false;

        // ── 底部布局 ──
        var pnlBottom = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new Padding(30, 0, 30, 0)
        };
        pnlBottom.Controls.Add(lblResult);
        pnlBottom.Controls.Add(btnOptimize);

        // ── 组装 ──
        Controls.Add(pnlTitle);
        Controls.Add(pnlCenter);
        Controls.Add(pnlBottom);

        // ── 底部面板（居中 btn + result） ──
        pnlCenter.Location = new Point(0, 48);
        lblResult.Location = new Point(0, 0);
        btnOptimize.Location = new Point(0, lblResult.Visible ? 38 : 0);
        pnlBottom.Location = new Point(0, 260);

        ResumeLayout(false);
        PerformLayout();
    }

    // ── 控件字段 ──
    private Label lblTitle = null!, lblStatus = null!;
    private Label lblGaugePct = null!, lblGaugeLabel = null!;
    private Label lblTotal = null!, lblUsed = null!, lblAvail = null!;
    private Label lblResult = null!;
    private Button btnOptimize = null!;
    private System.Windows.Forms.Timer? timer;
    private Panel pnlGauge = null!;
    private double _memLoad;
    private bool _isRunning;

    // ── 辅助 ──
    private static Label MakeDetailRow(string key, string val)
    {
        var lbl = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            AutoSize = true,
            Padding = new Padding(0, 6, 0, 6)
        };
        lbl.Paint += (s, e) =>
        {
            var g = e.Graphics!;
            using var keyBrush = new SolidBrush(Color.FromArgb(140, 140, 140));
            using var valBrush = new SolidBrush(Color.White);
            using var keyFont = new Font("Segoe UI", 10);
            using var valFont = new Font("Segoe UI", 10, FontStyle.Bold);
            g.DrawString(key, keyFont, keyBrush, 0, 8);
            var keyW = g.MeasureString(key, keyFont).Width;
            g.DrawString(val, valFont, valBrush, keyW + 12, 8);
        };
        lbl.Tag = new { Key = key, Value = val };
        lbl.Size = new Size(160, 30);
        return lbl;
    }

    private void DrawGauge(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics!;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var rect = new Rectangle(10, 10, 140, 140);
        var load = (float)(_memLoad / 100.0);
        var color = load switch
        {
            > 0.85f => Color.FromArgb(240, 80, 60),
            > 0.70f => Color.FromArgb(240, 180, 40),
            _ => Color.FromArgb(50, 140, 240)
        };

        // 背景环
        using var bgPen = new Pen(Color.FromArgb(50, 50, 55), 12);
        g.DrawArc(bgPen, rect, 135, 270);

        // 前景弧
        using var fgPen = new Pen(color, 12);
        g.DrawArc(fgPen, rect, 135, 270 * load);
    }
}
