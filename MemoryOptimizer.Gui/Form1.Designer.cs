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

        // ── 窗口 ──
        Text = "MemoryOptimizer";
        ClientSize = new Size(400, 400);
        MinimumSize = new Size(400, 400);
        MaximumSize = new Size(400, 400);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(18, 18, 18);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);

        // ── 顶部栏 ──
        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Color.FromArgb(24, 24, 24)
        };

        lblTitle = new Label
        {
            Text = "Memory Optimizer",
            Location = new Point(20, 0),
            Size = new Size(260, 48),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent
        };

        lblStatus = new Label
        {
            Text = "● 就绪",
            Location = new Point(280, 0),
            Size = new Size(100, 48),
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(100, 200, 100),
            BackColor = Color.Transparent
        };

        pnlTop.Controls.Add(lblTitle);
        pnlTop.Controls.Add(lblStatus);

        // ── 圆环仪表 ──
        pnlGauge = new Panel
        {
            Location = new Point(20, 70),
            Size = new Size(140, 140),
            BackColor = Color.Transparent
        };
        pnlGauge.Paint += DrawGauge;

        lblGaugePct = new Label
        {
            Text = "--%",
            Font = new Font("Segoe UI", 30, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 30),
            Size = new Size(140, 56)
        };
        lblGaugeLabel = new Label
        {
            Text = "内存负载",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(140, 140, 140),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 88),
            Size = new Size(140, 20)
        };
        pnlGauge.Controls.Add(lblGaugePct);
        pnlGauge.Controls.Add(lblGaugeLabel);

        // ── 详情 ──
        lblDetailTotal = new Label
        {
            Text = "总内存",
            Location = new Point(190, 88),
            Size = new Size(190, 24),
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(140, 140, 140),
            BackColor = Color.Transparent
        };
        lblDetailUsed = new Label
        {
            Text = "已用",
            Location = new Point(190, 120),
            Size = new Size(190, 24),
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(140, 140, 140),
            BackColor = Color.Transparent
        };
        lblDetailAvail = new Label
        {
            Text = "可用",
            Location = new Point(190, 152),
            Size = new Size(190, 24),
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(140, 140, 140),
            BackColor = Color.Transparent
        };

        // ── 结果 ──
        lblResult = new Label
        {
            Text = "",
            Location = new Point(20, 240),
            Size = new Size(360, 30),
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 200, 100),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false
        };

        // ── 按钮 ──
        btnOptimize = new Button
        {
            Text = "优化内存",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(50, 140, 240),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Location = new Point(20, 310),
            Size = new Size(360, 52),
            Enabled = false
        };
        btnOptimize.FlatAppearance.BorderSize = 0;
        btnOptimize.Click += BtnOptimize_Click;

        // ── 分隔线 ──
        var line = new Panel
        {
            Location = new Point(20, 230),
            Size = new Size(360, 1),
            BackColor = Color.FromArgb(50, 50, 55)
        };

        // ── 组装 ──
        Controls.AddRange([
            pnlTop, pnlGauge,
            lblDetailTotal, lblDetailUsed, lblDetailAvail,
            line, lblResult, btnOptimize
        ]);

        ResumeLayout(false);
    }

    private Label lblTitle = null!, lblStatus = null!;
    private Label lblGaugePct = null!, lblGaugeLabel = null!;
    private Label lblDetailTotal = null!, lblDetailUsed = null!, lblDetailAvail = null!;
    private Label lblResult = null!;
    private Button btnOptimize = null!;
    private System.Windows.Forms.Timer? timer;
    private Panel pnlGauge = null!;
    private double _memLoad;
    private bool _isRunning;

    private void DrawGauge(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics!;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var rect = new Rectangle(8, 8, 124, 124);
        var load = (float)(_memLoad / 100.0);
        var color = load switch
        {
            > 0.85f => Color.FromArgb(240, 80, 60),
            > 0.70f => Color.FromArgb(240, 180, 40),
            _ => Color.FromArgb(50, 140, 240)
        };

        using var bgPen = new Pen(Color.FromArgb(50, 50, 55), 11);
        g.DrawArc(bgPen, rect, 135, 270);

        using var fgPen = new Pen(color, 11);
        g.DrawArc(fgPen, rect, 135, 270 * load);
    }
}
