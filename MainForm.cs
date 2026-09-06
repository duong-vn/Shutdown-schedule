using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;

namespace ShutdownScheduler;

internal sealed class MainForm : Form
{
    // ==========================================
    // THEME PALETTE: MINIMAL SOFT DARK (SLATE / ZINC)
    // ==========================================
    private static readonly Color AppBackground = Color.FromArgb(15, 17, 23);          // #0F1117 Deep Neutral Canvas
    private static readonly Color CardBackground = Color.FromArgb(22, 26, 36);         // #161A24 Elevated Surface
    private static readonly Color CardHoverBackground = Color.FromArgb(28, 34, 48);    // #1C2230 Hover Surface
    private static readonly Color CardActiveBackground = Color.FromArgb(26, 42, 70);   // #1A2A46 Selected Surface
    private static readonly Color CardBorder = Color.FromArgb(38, 44, 60);             // #262C3C Soft Border
    private static readonly Color CardBorderHover = Color.FromArgb(56, 66, 92);       // #38425C Hover Border
    private static readonly Color CardBorderActive = Color.FromArgb(59, 130, 246);     // #3B82F6 Active Accent Border
    private static readonly Color InputBackground = Color.FromArgb(17, 20, 28);        // #11141C Recessed Input

    private static readonly Color TextPrimary = Color.FromArgb(248, 250, 252);         // #F8FAFC Crisp Pure
    private static readonly Color TextSecondary = Color.FromArgb(148, 163, 184);       // #94A3B8 Balanced Grey
    private static readonly Color TextMuted = Color.FromArgb(100, 116, 139);           // #64748B Subtle Muted

    private static readonly Color AccentPrimary = Color.FromArgb(59, 130, 246);        // #3B82F6 Vibrant Soft Blue
    private static readonly Color AccentPrimaryHover = Color.FromArgb(96, 165, 250);   // #60A5FA Blue Hover
    private static readonly Color AccentPrimaryDark = Color.FromArgb(37, 99, 235);     // #2563EB Blue Pressed
    private static readonly Color AccentPrimaryBg = Color.FromArgb(24, 36, 60);        // #18243C Soft Accent Badge

    private static readonly Color AccentEmerald = Color.FromArgb(16, 185, 129);        // #10B981 Emerald Green
    private static readonly Color AccentEmeraldBg = Color.FromArgb(18, 44, 34);        // Emerald Badge Bg

    private static readonly Color AccentRed = Color.FromArgb(239, 68, 68);             // #EF4444 Crimson
    private static readonly Color AccentRedHover = Color.FromArgb(248, 113, 113);      // #F87171 Crimson Hover
    private static readonly Color AccentRedDark = Color.FromArgb(220, 38, 38);         // #DC2626 Deep Red
    private static readonly Color AccentRedBg = Color.FromArgb(44, 22, 28);            // Red Badge Bg

    private static readonly Color ChipNormalBg = Color.FromArgb(24, 29, 40);
    private static readonly Color ChipNormalHover = Color.FromArgb(34, 42, 58);

    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

    // ==========================================
    // SERVICES & STATE
    // ==========================================
    private readonly ShutdownService shutdownService = new();
    private readonly Panel schedulingPanel = new();
    private readonly Panel countdownPanel = new();

    // Scheduling View Controls
    private readonly PillBadge liveClockBadge = new("● --:--:--", TextSecondary, CardBackground);
    private readonly Label scheduledForLabel = new();
    private readonly Label durationSummaryLabel = new();
    private readonly Label scheduleStatusLabel = new();
    private readonly Label detailTimeLabel = new();
    private readonly Label detailDurationLabel = new();
    private readonly TextBox commandTextBox = new();
    private readonly ModernButton commandCopyButton = new();
    private readonly TimeStepperInput customHoursInput = new(99, "Giờ");
    private readonly TimeStepperInput customMinutesInput = new(59, "Phút");
    private readonly TimeStepperInput customSecondsInput = new(59, "Giây");
    private readonly List<DurationTileButton> durationTiles = [];
    private readonly ModernButton scheduleButton = new();
    private readonly ModernButton schedulingCloseButton = new();

    // Countdown View Controls
    private readonly PillBadge countdownLiveBadge = new("● LỊCH TẮT MÁY ĐANG HOẠT ĐỘNG", AccentEmerald, AccentEmeraldBg);
    private readonly Label countdownScheduledForLabel = new();
    private readonly Label countdownStatusLabel = new();
    private readonly CountdownProgressRing countdownProgressRing = new();
    private readonly Label statStartTimeLabel = new();
    private readonly Label statTargetTimeLabel = new();
    private readonly Label statElapsedLabel = new();
    private readonly ModernButton cancelButton = new();
    private readonly ModernButton keepScheduleButton = new();

    // Timers
    private readonly System.Windows.Forms.Timer countdownTimer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer previewTimer = new() { Interval = 1000 };

    // Duration Presets (Clean, Balanced)
    private readonly List<DurationOption> durationOptions =
    [
        new("30 phút", 30 * 60, TileIconType.Lightning),
        new("1 giờ", 60 * 60, TileIconType.Clock),
        new("1,5 giờ", 90 * 60, TileIconType.Coffee),
        new("2 giờ", 2 * 60 * 60, TileIconType.Film),
        new("3 giờ", 3 * 60 * 60, TileIconType.Gamepad),
        new("4 giờ", 4 * 60 * 60, TileIconType.Moon),
        new("6 giờ", 6 * 60 * 60, TileIconType.Zzz),
        new("8 giờ", 8 * 60 * 60, TileIconType.Bed)
    ];

    private DurationOption? selectedDuration;
    private bool suppressCustomInputEvents;
    private ShutdownSchedule? activeSchedule;
    private DateTimeOffset scheduleStartTime;

    public MainForm()
    {
        selectedDuration = durationOptions[1]; // Default: 1 hour
        Text = "Shutdown Scheduler — Hẹn Giờ Tắt Máy";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimumSize = new Size(860, 545);
        ClientSize = new Size(860, 545);
        BackColor = AppBackground;
        ForeColor = TextPrimary;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

        // Load Icon if exists
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(iconPath))
            {
                Icon = new Icon(iconPath);
            }
            else if (File.Exists("app.ico"))
            {
                Icon = new Icon("app.ico");
            }
        }
        catch
        {
            // Fallback gracefully
        }

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        InitializeSchedulingView();
        InitializeCountdownView();

        Controls.Add(schedulingPanel);
        Controls.Add(countdownPanel);

        countdownTimer.Tick += (_, _) => UpdateCountdown();
        previewTimer.Tick += (_, _) => UpdatePreview();
        FormClosing += MainForm_FormClosing;

        ShowSchedulingView();
    }

    // ==========================================
    // SCHEDULING VIEW INITIALIZATION
    // ==========================================
    private void InitializeSchedulingView()
    {
        schedulingPanel.Dock = DockStyle.Fill;
        schedulingPanel.BackColor = AppBackground;
        schedulingPanel.Padding = new Padding(20, 12, 20, 12);

        // Main 3-Row Grid: [Header: 46px] / [Content: 100%] / [Footer: 44px]
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = AppBackground
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); // Header
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 2 Balanced Cards
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); // Footer

        // --- ROW 0: HEADER ---
        var headerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = AppBackground
        };

        var headerLeft = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = AppBackground
        };

        var logoIcon = new IconBadge(TileIconType.Power, AccentPrimary, AccentPrimaryBg, 36)
        {
            Margin = new Padding(0, 2, 12, 0)
        };

        var titleBox = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = AppBackground
        };

        var titleLabel = new Label
        {
            Text = "Hẹn Giờ Tắt Máy",
            Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
            ForeColor = TextPrimary,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 1)
        };

        var subtitleLabel = new Label
        {
            Text = "Tự động tắt máy tính an toàn theo thời gian định trước",
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = TextSecondary,
            AutoSize = true,
            Margin = Padding.Empty
        };

        titleBox.Controls.Add(titleLabel);
        titleBox.Controls.Add(subtitleLabel);

        headerLeft.Controls.Add(logoIcon);
        headerLeft.Controls.Add(titleBox);

        // Header Right: Live Clock Pill Badge
        void PositionClockBadge()
        {
            liveClockBadge.Location = new Point(
                Math.Max(0, headerPanel.Width - liveClockBadge.Width - 2),
                Math.Max(0, (headerPanel.Height - liveClockBadge.Height) / 2)
            );
        }
        headerPanel.Resize += (_, _) => PositionClockBadge();
        liveClockBadge.SizeChanged += (_, _) => PositionClockBadge();

        headerPanel.Controls.Add(headerLeft);
        headerPanel.Controls.Add(liveClockBadge);

        // --- ROW 1: 2-COLUMN BALANCED GRID ---
        var centerGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 6, 0, 6),
            Padding = Padding.Empty,
            BackColor = AppBackground
        };
        centerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        centerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        // ==========================================
        // LEFT CARD: Time Selection & Custom Stepper
        // ==========================================
        var leftCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 14, 16, 14),
            BackColor = CardBackground,
            BorderColor = CardBorder,
            CornerRadius = 12,
            Margin = new Padding(0, 0, 6, 0)
        };

        var leftCardLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = CardBackground
        };
        leftCardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        leftCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); // Header 1
        leftCardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 2x4 Presets Grid
        leftCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); // Header 2
        leftCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // Stepper Inputs
        leftCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // Quick Chips

        // Section 1 Header
        var quickHeader = CreateSectionHeader("MỐC GỢI Ý", TextSecondary);

        // 2x4 Bento Presets Grid
        var durationGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Margin = new Padding(0, 2, 0, 6),
            Padding = Padding.Empty,
            BackColor = CardBackground
        };
        durationGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        durationGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (var r = 0; r < 4; r++) durationGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

        for (var i = 0; i < durationOptions.Count; i++)
        {
            var opt = durationOptions[i];
            var tile = CreateDurationTile(opt);
            durationGrid.Controls.Add(tile, i % 2, i / 2);
        }

        // Section 2 Header
        var customHeader = CreateSectionHeader("HOẶC NHẬP TÙY CHỈNH", TextSecondary);

        // Inputs Flow
        customHoursInput.ValueChanged += (_, _) => SelectCustomDuration();
        customMinutesInput.ValueChanged += (_, _) => SelectCustomDuration();
        customSecondsInput.ValueChanged += (_, _) => SelectCustomDuration();

        var customInputsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 1, 0, 0),
            BackColor = CardBackground
        };
        customInputsFlow.Controls.Add(customHoursInput);
        customInputsFlow.Controls.Add(customMinutesInput);
        customInputsFlow.Controls.Add(customSecondsInput);

        // Quick Chips Flow
        var quickChips = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 3, 0, 0),
            BackColor = CardBackground
        };
        quickChips.Controls.Add(CreateQuickChip("+15p", () => AddCustomMinutes(15)));
        quickChips.Controls.Add(CreateQuickChip("+30p", () => AddCustomMinutes(30)));
        quickChips.Controls.Add(CreateQuickChip("+1h", () => AddCustomMinutes(60)));
        quickChips.Controls.Add(CreateQuickChip("+2h", () => AddCustomMinutes(120)));
        quickChips.Controls.Add(CreateQuickChip("Đặt lại", ResetCustomDuration));

        leftCardLayout.Controls.Add(quickHeader, 0, 0);
        leftCardLayout.Controls.Add(durationGrid, 0, 1);
        leftCardLayout.Controls.Add(customHeader, 0, 2);
        leftCardLayout.Controls.Add(customInputsFlow, 0, 3);
        leftCardLayout.Controls.Add(quickChips, 0, 4);

        leftCard.Controls.Add(leftCardLayout);

        // ==========================================
        // RIGHT CARD: Summary, Details & Confirmation
        // ==========================================
        var rightCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 16, 18, 16),
            BackColor = CardBackground,
            BorderColor = CardBorder,
            CornerRadius = 12,
            Margin = new Padding(6, 0, 0, 0)
        };

        var rightCardLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = CardBackground
        };
        rightCardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rightCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // Pill Badge
        rightCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68)); // Hero Time Display
        rightCardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Bento Details Card
        rightCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Windows Command Snippet
        rightCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); // Primary Confirmation Button

        // 1. Pill Badge
        var summaryBadge = new PillBadge("DỰ KIẾN TẮT MÁY", AccentPrimary, AccentPrimaryBg)
        {
            Margin = new Padding(0, 0, 0, 4)
        };

        // 2. Hero Target Time Stack
        var heroBox = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = CardBackground
        };

        scheduledForLabel.AutoSize = true;
        scheduledForLabel.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
        scheduledForLabel.ForeColor = TextPrimary;
        scheduledForLabel.Margin = new Padding(0, 0, 0, 2);

        durationSummaryLabel.AutoSize = true;
        durationSummaryLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        durationSummaryLabel.ForeColor = AccentPrimary;
        durationSummaryLabel.Margin = Padding.Empty;

        heroBox.Controls.Add(scheduledForLabel);
        heroBox.Controls.Add(durationSummaryLabel);

        // 3. Bento Details Card
        var detailsCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 10, 12, 10),
            BackColor = InputBackground,
            BorderColor = CardBorder,
            CornerRadius = 8,
            Margin = new Padding(0, 6, 0, 8)
        };

        var detailsTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = InputBackground
        };
        detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        detailsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        detailsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        detailsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));

        var lbl1 = CreateDetailLabel("Thời điểm tắt:", TextSecondary);
        detailTimeLabel.Text = "--:--:--";
        detailTimeLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        detailTimeLabel.ForeColor = TextPrimary;
        detailTimeLabel.Anchor = AnchorStyles.Left | AnchorStyles.None;
        detailTimeLabel.AutoSize = true;

        var lbl2 = CreateDetailLabel("Thời gian chờ:", TextSecondary);
        detailDurationLabel.Text = "1 giờ";
        detailDurationLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        detailDurationLabel.ForeColor = TextPrimary;
        detailDurationLabel.Anchor = AnchorStyles.Left | AnchorStyles.None;
        detailDurationLabel.AutoSize = true;

        var lbl3 = CreateDetailLabel("Chế độ:", TextSecondary);
        var detailSafetyLabel = new Label
        {
            Text = "Tắt an toàn (Có thể hủy bất cứ lúc nào)",
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = AccentEmerald,
            Anchor = AnchorStyles.Left | AnchorStyles.None,
            AutoSize = true
        };

        detailsTable.Controls.Add(lbl1, 0, 0);
        detailsTable.Controls.Add(detailTimeLabel, 1, 0);
        detailsTable.Controls.Add(lbl2, 0, 1);
        detailsTable.Controls.Add(detailDurationLabel, 1, 1);
        detailsTable.Controls.Add(lbl3, 0, 2);
        detailsTable.Controls.Add(detailSafetyLabel, 1, 2);

        detailsCard.Controls.Add(detailsTable);

        // 4. Windows Command Snippet Bar
        var commandCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            BackColor = InputBackground,
            BorderColor = CardBorder,
            CornerRadius = 6,
            Padding = new Padding(8, 4, 6, 4),
            Margin = new Padding(0, 0, 0, 8)
        };

        var commandTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = InputBackground
        };
        commandTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22));
        commandTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        commandTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68));

        var termIcon = new Control { Size = new Size(18, 18), BackColor = InputBackground };
        termIcon.Paint += (_, pe) => VectorIconDrawer.DrawIcon(pe.Graphics, TileIconType.Terminal, new Rectangle(0, 0, 18, 18), TextMuted, 1.6f);

        commandTextBox.ReadOnly = true;
        commandTextBox.BorderStyle = BorderStyle.None;
        commandTextBox.BackColor = InputBackground;
        commandTextBox.ForeColor = TextSecondary;
        commandTextBox.Font = new Font(FontFamily.GenericMonospace, 8.5F, FontStyle.Regular);
        commandTextBox.Dock = DockStyle.Fill;
        commandTextBox.Margin = new Padding(4, 5, 4, 2);

        commandCopyButton.Text = "Sao chép";
        commandCopyButton.CornerRadius = 4;
        commandCopyButton.BackColor = CardBackground;
        commandCopyButton.BorderColor = CardBorder;
        commandCopyButton.ForeColor = TextSecondary;
        commandCopyButton.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        commandCopyButton.Dock = DockStyle.Fill;
        commandCopyButton.Cursor = Cursors.Hand;
        commandCopyButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(commandTextBox.Text))
            {
                Clipboard.SetText(commandTextBox.Text);
                commandCopyButton.Text = "Đã chép!";
                commandCopyButton.ForeColor = AccentEmerald;
                Task.Delay(1500).ContinueWith(_ =>
                {
                    if (!IsDisposed && commandCopyButton.IsHandleCreated)
                    {
                        Invoke(() =>
                        {
                            commandCopyButton.Text = "Sao chép";
                            commandCopyButton.ForeColor = TextSecondary;
                        });
                    }
                });
            }
        };

        commandTable.Controls.Add(termIcon, 0, 0);
        commandTable.Controls.Add(commandTextBox, 1, 0);
        commandTable.Controls.Add(commandCopyButton, 2, 0);
        commandCard.Controls.Add(commandTable);

        // 5. Primary Confirmation Button
        scheduleButton.Text = "Xác nhận hẹn giờ tắt máy";
        scheduleButton.IconType = TileIconType.Power;
        scheduleButton.IconColor = Color.White;
        scheduleButton.CornerRadius = 8;
        scheduleButton.BackColor = AccentPrimary;
        scheduleButton.HoverBackColor = AccentPrimaryHover;
        scheduleButton.PressedBackColor = AccentPrimaryDark;
        scheduleButton.ForeColor = Color.White;
        scheduleButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        scheduleButton.Dock = DockStyle.Fill;
        scheduleButton.Cursor = Cursors.Hand;
        scheduleButton.Click += ScheduleButton_Click;

        rightCardLayout.Controls.Add(summaryBadge, 0, 0);
        rightCardLayout.Controls.Add(heroBox, 0, 1);
        rightCardLayout.Controls.Add(detailsCard, 0, 2);
        rightCardLayout.Controls.Add(commandCard, 0, 3);
        rightCardLayout.Controls.Add(scheduleButton, 0, 4);

        rightCard.Controls.Add(rightCardLayout);

        centerGrid.Controls.Add(leftCard, 0, 0);
        centerGrid.Controls.Add(rightCard, 1, 0);

        // --- ROW 2: FOOTER (Clean, Minimal, Anchored) ---
        var footerTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = AppBackground
        };
        footerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footerTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        scheduleStatusLabel.AutoSize = true;
        scheduleStatusLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        scheduleStatusLabel.ForeColor = TextMuted;
        scheduleStatusLabel.Text = "Lịch chỉ được gửi tới Windows sau khi bạn xác nhận.";
        scheduleStatusLabel.Anchor = AnchorStyles.Left | AnchorStyles.None;

        schedulingCloseButton.Text = "Thoát ứng dụng";
        schedulingCloseButton.CornerRadius = 6;
        schedulingCloseButton.BackColor = CardBackground;
        schedulingCloseButton.HoverBackColor = CardHoverBackground;
        schedulingCloseButton.BorderColor = CardBorder;
        schedulingCloseButton.ForeColor = TextSecondary;
        schedulingCloseButton.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        schedulingCloseButton.Size = new Size(120, 30);
        schedulingCloseButton.AutoSize = false;
        schedulingCloseButton.Anchor = AnchorStyles.Right | AnchorStyles.None;
        schedulingCloseButton.Margin = new Padding(0, 0, 2, 0);
        schedulingCloseButton.Cursor = Cursors.Hand;
        schedulingCloseButton.Click += (_, _) => Close();

        footerTable.Controls.Add(scheduleStatusLabel, 0, 0);
        footerTable.Controls.Add(schedulingCloseButton, 1, 0);

        mainLayout.Controls.Add(headerPanel, 0, 0);
        mainLayout.Controls.Add(centerGrid, 0, 1);
        mainLayout.Controls.Add(footerTable, 0, 2);

        schedulingPanel.Controls.Add(mainLayout);
    }

    // ==========================================
    // DURATION TILE BUILDER
    // ==========================================
    private Button CreateDurationTile(DurationOption duration)
    {
        var tile = new DurationTileButton(duration)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(3)
        };

        tile.Click += (_, _) => SelectDuration((DurationOption)tile.Tag!);
        durationTiles.Add(tile);
        return tile;
    }

    private static Label CreateSectionHeader(string text, Color accentColor)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
            ForeColor = accentColor,
            AutoSize = true,
            Margin = new Padding(2, 0, 0, 4)
        };
    }

    private static Label CreateDetailLabel(string text, Color color)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = color,
            Anchor = AnchorStyles.Left | AnchorStyles.None,
            AutoSize = true
        };
    }

    private ModernButton CreateQuickChip(string text, Action onClick)
    {
        var chip = new ModernButton
        {
            Text = text,
            AutoSize = true,
            CornerRadius = 6,
            BackColor = ChipNormalBg,
            HoverBackColor = ChipNormalHover,
            BorderColor = CardBorder,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            Padding = new Padding(8, 3, 8, 3),
            Margin = new Padding(0, 0, 4, 0),
            Cursor = Cursors.Hand
        };
        chip.Click += (_, _) => onClick();
        return chip;
    }

    private void AddCustomMinutes(int minutes)
    {
        if (activeSchedule is not null) return;
        var currentTotalMinutes = customHoursInput.Value * 60 + customMinutesInput.Value + (customSecondsInput.Value > 0 ? 1 : 0);
        var newTotalMinutes = Math.Min(99 * 60 + 59, currentTotalMinutes + minutes);

        suppressCustomInputEvents = true;
        selectedDuration = null;
        customHoursInput.Value = newTotalMinutes / 60;
        customMinutesInput.Value = newTotalMinutes % 60;
        customSecondsInput.Value = 0;
        suppressCustomInputEvents = false;

        UpdateDurationTileStates();
        UpdatePreview();
    }

    private void ResetCustomDuration()
    {
        if (activeSchedule is not null) return;
        suppressCustomInputEvents = true;
        customHoursInput.Value = 0;
        customMinutesInput.Value = 0;
        customSecondsInput.Value = 0;
        suppressCustomInputEvents = false;

        SelectDuration(durationOptions[1]); // back to 1 hour default
    }

    // ==========================================
    // COUNTDOWN VIEW INITIALIZATION
    // ==========================================
    private void InitializeCountdownView()
    {
        countdownPanel.Dock = DockStyle.Fill;
        countdownPanel.BackColor = AppBackground;
        countdownPanel.Padding = new Padding(24, 10, 24, 12);

        var outerContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = AppBackground
        };
        outerContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        outerContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // 0: Top Status Badge
        outerContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // 1: Scheduled Time Subtitle
        outerContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 2: Circular Progress Hero
        outerContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); // 3: Bento Stats (Start, End, Elapsed)
        outerContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); // 4: Actions (Cancel, Keep)

        // Top Status Badge Container
        var topBadgeContainer = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Anchor = AnchorStyles.None,
            Margin = Padding.Empty,
            BackColor = AppBackground
        };
        topBadgeContainer.Controls.Add(countdownLiveBadge);

        // Subtitle
        countdownScheduledForLabel.AutoSize = true;
        countdownScheduledForLabel.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        countdownScheduledForLabel.ForeColor = TextSecondary;
        countdownScheduledForLabel.TextAlign = ContentAlignment.MiddleCenter;
        countdownScheduledForLabel.Dock = DockStyle.Fill;

        // Progress Ring Container
        var ringContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppBackground,
            Margin = new Padding(4)
        };

        countdownProgressRing.Dock = DockStyle.Fill;
        countdownProgressRing.BackColor = AppBackground;
        ringContainer.Controls.Add(countdownProgressRing);

        // 3-Metric Bento Grid (Bắt đầu, Tắt lúc, Đã trôi qua)
        var statsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.None,
            Anchor = AnchorStyles.None,
            ColumnCount = 3,
            RowCount = 1,
            Width = 520,
            Height = 54,
            Margin = Padding.Empty,
            BackColor = AppBackground
        };
        statsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        statsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        statsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

        statsGrid.Controls.Add(CreateStatCard("BẮT ĐẦU LÚC", statStartTimeLabel), 0, 0);
        statsGrid.Controls.Add(CreateStatCard("DỰ KIẾN TẮT", statTargetTimeLabel), 1, 0);
        statsGrid.Controls.Add(CreateStatCard("ĐÃ TRÔI QUA", statElapsedLabel), 2, 0);

        // Action Buttons
        cancelButton.Text = "Hủy lịch tắt máy";
        cancelButton.IconType = TileIconType.Close;
        cancelButton.IconColor = Color.White;
        cancelButton.CornerRadius = 8;
        cancelButton.BackColor = AccentRed;
        cancelButton.HoverBackColor = AccentRedHover;
        cancelButton.PressedBackColor = AccentRedDark;
        cancelButton.ForeColor = Color.White;
        cancelButton.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        cancelButton.Padding = new Padding(16, 7, 16, 7);
        cancelButton.AutoSize = true;
        cancelButton.Cursor = Cursors.Hand;
        cancelButton.Click += CancelButton_Click;

        keepScheduleButton.Text = "Thoát ứng dụng (Lịch vẫn tiếp tục)";
        keepScheduleButton.IconType = TileIconType.Moon;
        keepScheduleButton.IconColor = TextSecondary;
        keepScheduleButton.CornerRadius = 8;
        keepScheduleButton.BackColor = CardBackground;
        keepScheduleButton.HoverBackColor = CardHoverBackground;
        keepScheduleButton.BorderColor = CardBorder;
        keepScheduleButton.ForeColor = TextSecondary;
        keepScheduleButton.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        keepScheduleButton.Padding = new Padding(14, 7, 14, 7);
        keepScheduleButton.AutoSize = true;
        keepScheduleButton.Cursor = Cursors.Hand;
        keepScheduleButton.Click += (_, _) => Close();

        var actionsLayout = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Anchor = AnchorStyles.None,
            Margin = Padding.Empty,
            BackColor = AppBackground
        };
        actionsLayout.Controls.Add(cancelButton);
        actionsLayout.Controls.Add(keepScheduleButton);

        outerContainer.Controls.Add(topBadgeContainer, 0, 0);
        outerContainer.Controls.Add(countdownScheduledForLabel, 0, 1);
        outerContainer.Controls.Add(ringContainer, 0, 2);
        outerContainer.Controls.Add(statsGrid, 0, 3);
        outerContainer.Controls.Add(actionsLayout, 0, 4);

        countdownPanel.Controls.Add(outerContainer);
    }

    private static Control CreateStatCard(string caption, Label valueLabel)
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 6, 8, 6),
            BackColor = CardBackground,
            BorderColor = CardBorder,
            CornerRadius = 8,
            Margin = new Padding(3)
        };

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = CardBackground
        };

        var cap = new Label
        {
            Text = caption,
            Font = new Font("Segoe UI", 7F, FontStyle.Bold),
            ForeColor = TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2)
        };

        valueLabel.AutoSize = true;
        valueLabel.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        valueLabel.ForeColor = TextPrimary;
        valueLabel.Margin = Padding.Empty;

        layout.Controls.Add(cap);
        layout.Controls.Add(valueLabel);
        card.Controls.Add(layout);

        return card;
    }

    // ==========================================
    // LOGIC & EVENT HANDLERS
    // ==========================================
    private void SelectDuration(DurationOption duration)
    {
        if (activeSchedule is not null) return;
        selectedDuration = duration;
        suppressCustomInputEvents = true;
        customHoursInput.Value = 0;
        customMinutesInput.Value = 0;
        customSecondsInput.Value = 0;
        suppressCustomInputEvents = false;
        UpdateDurationTileStates();
        UpdatePreview();
    }

    private void SelectCustomDuration()
    {
        if (suppressCustomInputEvents || activeSchedule is not null) return;
        if (selectedDuration is not null)
        {
            selectedDuration = null;
            UpdateDurationTileStates();
        }
        UpdatePreview();
    }

    private bool TryGetSelectedDuration(out int seconds, out string label)
    {
        if (selectedDuration is not null)
        {
            seconds = selectedDuration.Seconds;
            label = selectedDuration.Label;
            return true;
        }

        seconds = customHoursInput.Value * 3600 + customMinutesInput.Value * 60 + customSecondsInput.Value;
        if (seconds <= 0)
        {
            label = string.Empty;
            return false;
        }

        var parts = new List<string>();
        if (customHoursInput.Value > 0) parts.Add($"{customHoursInput.Value:0} giờ");
        if (customMinutesInput.Value > 0) parts.Add($"{customMinutesInput.Value:0} phút");
        if (customSecondsInput.Value > 0) parts.Add($"{customSecondsInput.Value:0} giây");
        label = string.Join(" ", parts);
        return true;
    }

    private void UpdateDurationTileStates()
    {
        var canEdit = activeSchedule is null && !UseWaitCursor;
        foreach (var tile in durationTiles)
        {
            var isSelected = ReferenceEquals(tile.Duration, selectedDuration);
            tile.IsSelected = isSelected;
            tile.Enabled = canEdit;
        }

        customHoursInput.Enabled = canEdit;
        customMinutesInput.Enabled = canEdit;
        customSecondsInput.Enabled = canEdit;
    }

    private void UpdatePreview()
    {
        liveClockBadge.Text = $"● {DateTimeOffset.Now.ToString("HH:mm:ss", VietnameseCulture)}";

        if (activeSchedule is not null) return;
        if (!TryGetSelectedDuration(out var seconds, out var label))
        {
            scheduleButton.Enabled = false;
            commandTextBox.Text = string.Empty;
            durationSummaryLabel.Text = "Chưa chọn thời lượng";
            scheduledForLabel.Text = "--:--:--";
            detailTimeLabel.Text = "--:--:--";
            detailDurationLabel.Text = "--";
            scheduleStatusLabel.ForeColor = TextMuted;
            scheduleStatusLabel.Text = "Chọn một mốc thời gian hoặc nhập thời lượng lớn hơn 0.";
            return;
        }

        scheduleButton.Enabled = true;
        var now = DateTimeOffset.Now;
        var scheduledFor = now.AddSeconds(seconds);
        var formattedScheduledFor = FormatScheduledFor(scheduledFor, now);

        commandTextBox.Text = $"shutdown -s -t {seconds}";
        durationSummaryLabel.Text = $"Tắt sau {label}";
        scheduledForLabel.Text = scheduledFor.ToString("HH:mm:ss", VietnameseCulture);
        detailTimeLabel.Text = formattedScheduledFor;
        detailDurationLabel.Text = $"{label} ({seconds:N0} giây)";

        if (scheduleStatusLabel.ForeColor != AccentEmerald)
        {
            scheduleStatusLabel.ForeColor = TextMuted;
            scheduleStatusLabel.Text = "Lịch chỉ được gửi tới Windows sau khi bạn xác nhận.";
        }
    }

    private static string FormatScheduledFor(DateTimeOffset scheduledFor, DateTimeOffset? referenceTime = null)
    {
        var refDate = (referenceTime ?? DateTimeOffset.Now).Date;
        return scheduledFor.Date == refDate
            ? scheduledFor.ToString("HH':'mm':'ss' (Hôm nay)'", VietnameseCulture)
            : scheduledFor.ToString("HH':'mm':'ss',' dd'/'MM'/'yyyy", VietnameseCulture);
    }

    private async void ScheduleButton_Click(object? sender, EventArgs e)
    {
        if (!TryGetSelectedDuration(out var seconds, out var label))
        {
            UpdatePreview();
            return;
        }

        var now = DateTimeOffset.Now;
        var scheduledFor = now.AddSeconds(seconds);

        if (ShowChoiceDialog(
            "Xác nhận hẹn giờ tắt máy",
            $"Máy tính sẽ tự động tắt sau {label}, vào lúc {FormatScheduledFor(scheduledFor, now)}.\n\nBạn có thể hủy lịch bất kỳ lúc nào trước thời điểm này.",
            "Lên lịch tắt máy",
            "Quay lại",
            AccentPrimary) != DialogResult.OK) return;

        SetBusy(true);
        var result = await shutdownService.ScheduleShutdownAsync(seconds);
        SetBusy(false);

        if (!result.Succeeded)
        {
            scheduleStatusLabel.ForeColor = AccentRed;
            scheduleStatusLabel.Text = $"Không thể lên lịch tắt máy: {result.ErrorMessage}";
            return;
        }

        scheduleStartTime = DateTimeOffset.Now;
        activeSchedule = new ShutdownSchedule(seconds, scheduledFor, $"shutdown -s -t {seconds}");
        ShowCountdownView();
    }

    private async void CancelButton_Click(object? sender, EventArgs e)
    {
        if (ShowChoiceDialog(
            "Hủy lịch tắt máy",
            "Lịch tắt máy đang chờ sẽ bị hủy hoàn toàn khỏi hệ thống Windows.",
            "Hủy lịch ngay",
            "Tiếp tục giữ lịch",
            AccentRed) == DialogResult.OK)
        {
            await AbortScheduleAsync();
        }
    }

    private async Task<bool> AbortScheduleAsync()
    {
        SetBusy(true);
        var result = await shutdownService.AbortShutdownAsync();
        SetBusy(false);

        if (!result.Succeeded)
        {
            countdownStatusLabel.ForeColor = AccentRedHover;
            countdownStatusLabel.Text = $"Không thể hủy lịch: {result.ErrorMessage}";
            return false;
        }

        countdownTimer.Stop();
        activeSchedule = null;
        scheduleStatusLabel.ForeColor = AccentEmerald;
        scheduleStatusLabel.Text = "✓ Đã hủy lịch tắt máy thành công.";
        ShowSchedulingView();
        scheduleButton.Focus();
        return true;
    }

    private void ShowSchedulingView()
    {
        countdownTimer.Stop();
        countdownPanel.Visible = false;
        schedulingPanel.Visible = true;
        scheduleButton.Enabled = true;
        AcceptButton = scheduleButton;
        CancelButton = schedulingCloseButton;
        UpdateDurationTileStates();
        UpdatePreview();
        previewTimer.Start();
    }

    private void ShowCountdownView()
    {
        previewTimer.Stop();
        schedulingPanel.Visible = false;
        countdownPanel.Visible = true;
        AcceptButton = null;
        CancelButton = keepScheduleButton;

        statStartTimeLabel.Text = scheduleStartTime.ToString("HH:mm:ss", VietnameseCulture);
        statTargetTimeLabel.Text = activeSchedule!.ScheduledFor.ToString("HH:mm:ss", VietnameseCulture);
        countdownScheduledForLabel.Text = $"Máy tính sẽ tắt lúc {FormatScheduledFor(activeSchedule!.ScheduledFor)}";
        countdownProgressRing.SubtitleText = "Thời gian còn lại";
        cancelButton.Text = "Hủy lịch tắt máy";
        cancelButton.Enabled = true;

        countdownTimer.Start();
        UpdateCountdown();
        cancelButton.Focus();
    }

    private void UpdateCountdown()
    {
        if (activeSchedule is null) return;
        var now = DateTimeOffset.Now;
        var remaining = activeSchedule.ScheduledFor - now;
        var elapsed = now - scheduleStartTime;

        statElapsedLabel.Text = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";

        if (remaining <= TimeSpan.Zero)
        {
            countdownTimer.Stop();
            countdownProgressRing.TimeText = "00:00:00";
            countdownProgressRing.SubtitleText = "Đang tắt máy...";
            countdownProgressRing.Progress = 0F;
            countdownStatusLabel.ForeColor = AccentRedHover;
            countdownStatusLabel.Text = "Thời gian đã kết thúc. Windows đang thực hiện tắt máy...";
            cancelButton.Enabled = false;
            return;
        }

        countdownProgressRing.TimeText = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        countdownProgressRing.SubtitleText = "Thời gian còn lại";
        countdownProgressRing.Progress = (float)Math.Clamp(remaining.TotalSeconds / activeSchedule.DelaySeconds, 0, 1);
    }

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        scheduleButton.Enabled = !busy && activeSchedule is null;
        cancelButton.Enabled = !busy && activeSchedule is not null;
        schedulingCloseButton.Enabled = !busy;
        keepScheduleButton.Enabled = !busy;
        cancelButton.Text = busy && activeSchedule is not null ? "Đang hủy lịch…" : "Hủy lịch tắt máy";
        scheduleButton.Text = busy && activeSchedule is null ? "Đang lên lịch…" : "Xác nhận hẹn giờ tắt máy";
        UpdateDurationTileStates();
    }

    private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        previewTimer.Stop();
        if (activeSchedule is null) return;

        using var dialog = new CloseScheduleDialog();
        var choice = dialog.ShowDialog(this);
        if (choice == DialogResult.Cancel)
        {
            e.Cancel = true;
            previewTimer.Start();
            return;
        }

        if (choice == DialogResult.No) return; // Keep schedule and exit

        e.Cancel = true;
        if (await AbortScheduleAsync())
        {
            FormClosing -= MainForm_FormClosing;
            Close();
        }
    }

    private DialogResult ShowChoiceDialog(string title, string message, string primaryText, string cancelText, Color primaryColor)
    {
        using var dialog = new ChoiceDialog(title, message, primaryText, cancelText, primaryColor);
        return dialog.ShowDialog(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            countdownTimer.Dispose();
            previewTimer.Dispose();
        }
        base.Dispose(disposing);
    }

    // ==========================================
    // DATA RECORDS & ENUMS
    // ==========================================
    private sealed record DurationOption(string Label, int Seconds, TileIconType IconType);

    public enum TileIconType
    {
        Lightning,
        Clock,
        Coffee,
        Film,
        Gamepad,
        Moon,
        Zzz,
        Bed,
        Power,
        Terminal,
        Check,
        Close,
        Copy,
        Shield,
        Info
    }

    // ==========================================
    // VECTOR ICON RENDERING ENGINE
    // ==========================================
    public static class VectorIconDrawer
    {
        public static void DrawIcon(Graphics g, TileIconType type, Rectangle bounds, Color color, float strokeWidth = 1.6f)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            switch (type)
            {
                case TileIconType.Power:
                    DrawPower(g, bounds, color, strokeWidth);
                    break;
                case TileIconType.Clock:
                    DrawClock(g, bounds, color, strokeWidth);
                    break;
                case TileIconType.Lightning:
                    DrawLightning(g, bounds, color);
                    break;
                case TileIconType.Coffee:
                    DrawCoffee(g, bounds, color, strokeWidth);
                    break;
                case TileIconType.Film:
                    DrawFilm(g, bounds, color, strokeWidth);
                    break;
                case TileIconType.Gamepad:
                    DrawGamepad(g, bounds, color, strokeWidth);
                    break;
                case TileIconType.Moon:
                    DrawMoon(g, bounds, color);
                    break;
                case TileIconType.Zzz:
                    DrawZzz(g, bounds, color);
                    break;
                case TileIconType.Bed:
                    DrawBed(g, bounds, color, strokeWidth);
                    break;
                case TileIconType.Terminal:
                    DrawTerminal(g, bounds, color, strokeWidth);
                    break;
                case TileIconType.Check:
                    DrawCheck(g, bounds, color, strokeWidth);
                    break;
                case TileIconType.Close:
                    DrawClose(g, bounds, color, strokeWidth);
                    break;
                case TileIconType.Copy:
                    DrawCopy(g, bounds, color, strokeWidth);
                    break;
                case TileIconType.Shield:
                    DrawShield(g, bounds, color, strokeWidth);
                    break;
                case TileIconType.Info:
                    DrawInfo(g, bounds, color, strokeWidth);
                    break;
            }
        }

        private static void DrawPower(Graphics g, Rectangle b, Color c, float stroke)
        {
            float pad = b.Width * 0.18f;
            var arcRect = new RectangleF(b.X + pad, b.Y + pad + b.Height * 0.08f, b.Width - 2 * pad, b.Height - 2 * pad);
            using var pen = new Pen(c, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawArc(pen, arcRect, -55, 290);
            float cx = b.X + b.Width / 2f;
            g.DrawLine(pen, cx, b.Y + pad, cx, b.Y + b.Height * 0.48f);
        }

        private static void DrawClock(Graphics g, Rectangle b, Color c, float stroke)
        {
            float pad = b.Width * 0.16f;
            var rect = new RectangleF(b.X + pad, b.Y + pad, b.Width - 2 * pad, b.Height - 2 * pad);
            using var pen = new Pen(c, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawEllipse(pen, rect);
            float cx = b.X + b.Width / 2f;
            float cy = b.Y + b.Height / 2f;
            g.DrawLine(pen, cx, cy, cx, b.Y + pad + rect.Height * 0.24f);
            g.DrawLine(pen, cx, cy, cx + rect.Width * 0.25f, cy);
        }

        private static void DrawLightning(Graphics g, Rectangle b, Color c)
        {
            float cx = b.X + b.Width / 2f;
            float cy = b.Y + b.Height / 2f;
            float w = b.Width * 0.22f;
            float h = b.Height * 0.34f;

            PointF[] points =
            [
                new(cx + w * 0.2f, cy - h),
                new(cx - w, cy + h * 0.1f),
                new(cx, cy + h * 0.1f),
                new(cx - w * 0.2f, cy + h),
                new(cx + w, cy - h * 0.1f),
                new(cx, cy - h * 0.1f)
            ];

            using var brush = new SolidBrush(c);
            g.FillPolygon(brush, points);
        }

        private static void DrawCoffee(Graphics g, Rectangle b, Color c, float stroke)
        {
            float pad = b.Width * 0.22f;
            using var pen = new Pen(c, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            var cupRect = new RectangleF(b.X + pad, b.Y + pad + 2, b.Width * 0.44f, b.Height * 0.4f);
            g.DrawRectangle(pen, cupRect.X, cupRect.Y, cupRect.Width, cupRect.Height);
            g.DrawArc(pen, cupRect.Right - 1, cupRect.Y + 2, b.Width * 0.16f, cupRect.Height * 0.55f, -90, 180);
            g.DrawLine(pen, cupRect.X + 3, cupRect.Y - 3, cupRect.X + 3, cupRect.Y - 1);
            g.DrawLine(pen, cupRect.X + cupRect.Width / 2, cupRect.Y - 4, cupRect.X + cupRect.Width / 2, cupRect.Y - 1);
        }

        private static void DrawFilm(Graphics g, Rectangle b, Color c, float stroke)
        {
            float pad = b.Width * 0.2f;
            var rect = new RectangleF(b.X + pad, b.Y + pad, b.Width - 2 * pad, b.Height - 2 * pad);
            using var pen = new Pen(c, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            using var brush = new SolidBrush(c);
            float cx = b.X + b.Width / 2f;
            float cy = b.Y + b.Height / 2f;
            PointF[] tri = [new(cx - 2.5f, cy - 3.5f), new(cx + 3.5f, cy), new(cx - 2.5f, cy + 3.5f)];
            g.FillPolygon(brush, tri);
        }

        private static void DrawGamepad(Graphics g, Rectangle b, Color c, float stroke)
        {
            float pad = b.Width * 0.18f;
            var rect = new RectangleF(b.X + pad, b.Y + pad + 2, b.Width - 2 * pad, (b.Height - 2 * pad) * 0.72f);
            using var pen = new Pen(c, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var path = CreateRoundedRectanglePath(rect, 4);
            g.DrawPath(pen, path);
            float cx = rect.X + rect.Width * 0.28f;
            float cy = rect.Y + rect.Height * 0.5f;
            g.DrawLine(pen, cx - 2.5f, cy, cx + 2.5f, cy);
            g.DrawLine(pen, cx, cy - 2.5f, cx, cy + 2.5f);

            using var brush = new SolidBrush(c);
            g.FillEllipse(brush, rect.Right - rect.Width * 0.3f, cy - 2, 3.5f, 3.5f);
        }

        private static void DrawMoon(Graphics g, Rectangle b, Color c)
        {
            float pad = b.Width * 0.2f;
            var rect = new RectangleF(b.X + pad, b.Y + pad, b.Width - 2 * pad, b.Height - 2 * pad);
            using var path = new GraphicsPath();
            path.AddArc(rect, 45, 270);
            path.AddArc(rect.X + rect.Width * 0.25f, rect.Y, rect.Width * 0.8f, rect.Height * 0.8f, 315, -180);
            path.CloseFigure();
            using var brush = new SolidBrush(c);
            g.FillPath(brush, path);
        }

        private static void DrawZzz(Graphics g, Rectangle b, Color c)
        {
            using var brush = new SolidBrush(c);
            using var font = new Font("Segoe UI", b.Width * 0.32f, FontStyle.Bold);
            g.DrawString("Z", font, brush, b.X + b.Width * 0.35f, b.Y + b.Height * 0.12f);
            using var smallFont = new Font("Segoe UI", b.Width * 0.22f, FontStyle.Bold);
            g.DrawString("z", smallFont, brush, b.X + b.Width * 0.18f, b.Y + b.Height * 0.44f);
        }

        private static void DrawBed(Graphics g, Rectangle b, Color c, float stroke)
        {
            float pad = b.Width * 0.18f;
            var rect = new RectangleF(b.X + pad, b.Y + pad, b.Width - 2 * pad, b.Height - 2 * pad);
            using var pen = new Pen(c, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(pen, rect.X, rect.Y + 2, rect.X, rect.Bottom);
            g.DrawLine(pen, rect.X, rect.Y + rect.Height * 0.6f, rect.Right, rect.Y + rect.Height * 0.6f);
            g.DrawLine(pen, rect.Right, rect.Y + rect.Height * 0.35f, rect.Right, rect.Bottom);
            g.DrawLine(pen, rect.X + 3, rect.Y + rect.Height * 0.45f, rect.X + 8, rect.Y + rect.Height * 0.45f);
        }

        private static void DrawTerminal(Graphics g, Rectangle b, Color c, float stroke)
        {
            float pad = b.Width * 0.18f;
            var rect = new RectangleF(b.X + pad, b.Y + pad, b.Width - 2 * pad, b.Height - 2 * pad);
            using var pen = new Pen(c, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            g.DrawLine(pen, rect.X + 3, rect.Y + 4, rect.X + 6, rect.Y + 7);
            g.DrawLine(pen, rect.X + 6, rect.Y + 7, rect.X + 3, rect.Y + 10);
            g.DrawLine(pen, rect.X + 8, rect.Y + 10, rect.X + 12, rect.Y + 10);
        }

        private static void DrawCheck(Graphics g, Rectangle b, Color c, float stroke)
        {
            using var pen = new Pen(c, stroke + 0.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            float cx = b.X + b.Width / 2f;
            float cy = b.Y + b.Height / 2f;
            g.DrawLine(pen, cx - 4, cy, cx - 1, cy + 3.5f);
            g.DrawLine(pen, cx - 1, cy + 3.5f, cx + 5, cy - 3.5f);
        }

        private static void DrawClose(Graphics g, Rectangle b, Color c, float stroke)
        {
            using var pen = new Pen(c, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            float pad = b.Width * 0.28f;
            g.DrawLine(pen, b.X + pad, b.Y + pad, b.Right - pad, b.Bottom - pad);
            g.DrawLine(pen, b.Right - pad, b.Y + pad, b.X + pad, b.Bottom - pad);
        }

        private static void DrawCopy(Graphics g, Rectangle b, Color c, float stroke)
        {
            float pad = b.Width * 0.2f;
            using var pen = new Pen(c, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawRectangle(pen, b.X + pad + 2.5f, b.Y + pad, b.Width * 0.44f, b.Height * 0.48f);
            using var fillBrush = new SolidBrush(CardBackground);
            var frontRect = new RectangleF(b.X + pad, b.Y + pad + 2.5f, b.Width * 0.44f, b.Height * 0.48f);
            g.FillRectangle(fillBrush, frontRect);
            g.DrawRectangle(pen, frontRect.X, frontRect.Y, frontRect.Width, frontRect.Height);
        }

        private static void DrawShield(Graphics g, Rectangle b, Color c, float stroke)
        {
            float pad = b.Width * 0.18f;
            var rect = new RectangleF(b.X + pad, b.Y + pad, b.Width - 2 * pad, b.Height - 2 * pad);
            using var path = new GraphicsPath();
            float cx = rect.X + rect.Width / 2f;
            path.AddLine(rect.X, rect.Y, rect.Right, rect.Y);
            path.AddLine(rect.Right, rect.Y, rect.Right, rect.Y + rect.Height * 0.45f);
            path.AddBezier(rect.Right, rect.Y + rect.Height * 0.45f, rect.Right - 1, rect.Bottom - 2, cx + 2, rect.Bottom, cx, rect.Bottom);
            path.AddBezier(cx, rect.Bottom, cx - 2, rect.Bottom, rect.X + 1, rect.Y + rect.Height * 0.45f, rect.X, rect.Y + rect.Height * 0.45f);
            path.CloseFigure();
            using var pen = new Pen(c, stroke);
            g.DrawPath(pen, path);
        }

        private static void DrawInfo(Graphics g, Rectangle b, Color c, float stroke)
        {
            float pad = b.Width * 0.18f;
            var rect = new RectangleF(b.X + pad, b.Y + pad, b.Width - 2 * pad, b.Height - 2 * pad);
            using var pen = new Pen(c, stroke);
            g.DrawEllipse(pen, rect);
            using var brush = new SolidBrush(c);
            float cx = b.X + b.Width / 2f;
            g.FillEllipse(brush, cx - 1.5f, rect.Y + rect.Height * 0.24f, 3, 3);
            using var stemPen = new Pen(c, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(stemPen, cx, rect.Y + rect.Height * 0.44f, cx, rect.Y + rect.Height * 0.74f);
        }

        public static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // ==========================================
    // CUSTOM UI CONTROLS
    // ==========================================

    // 1. Duration Tile Button
    private sealed class DurationTileButton : Button
    {
        public DurationOption Duration { get; }
        private bool isSelected;
        public bool IsSelected { get => isSelected; set { isSelected = value; Invalidate(); } }
        private bool hovered;

        public DurationTileButton(DurationOption duration)
        {
            Duration = duration;
            Tag = duration;
            FlatStyle = FlatStyle.Flat;
            UseVisualStyleBackColor = false;
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? CardBackground);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var bounds = new RectangleF(1, 1, ClientSize.Width - 2, ClientSize.Height - 2);
            using var path = CreateRoundedPath(bounds, 8);

            // Background fill
            var bg = isSelected ? CardActiveBackground : hovered ? CardHoverBackground : InputBackground;
            using (var brush = new SolidBrush(bg)) g.FillPath(brush, path);

            // Border
            var borderColor = isSelected ? CardBorderActive : hovered ? CardBorderHover : CardBorder;
            var borderWidth = isSelected ? 1.5f : 1f;
            using (var pen = new Pen(borderColor, borderWidth)) g.DrawPath(pen, path);

            // Left Icon Badge
            int iconBoxSize = 18;
            var iconBox = new Rectangle(10, (ClientSize.Height - iconBoxSize) / 2, iconBoxSize, iconBoxSize);
            var iconColor = isSelected ? AccentPrimary : hovered ? TextPrimary : TextSecondary;
            VectorIconDrawer.DrawIcon(g, Duration.IconType, iconBox, iconColor, 1.6f);

            // Right Checkmark if selected
            int rightReserved = 10;
            if (isSelected)
            {
                rightReserved = 28;
                var checkRect = new Rectangle(ClientSize.Width - 24, (ClientSize.Height - 16) / 2, 16, 16);
                VectorIconDrawer.DrawIcon(g, TileIconType.Check, checkRect, AccentPrimary, 1.8f);
            }

            // Duration Label
            int textX = 36;
            int textWidth = Math.Max(10, ClientSize.Width - textX - rightReserved);
            var textRect = new Rectangle(textX, 0, textWidth, ClientSize.Height);
            var textColor = isSelected ? TextPrimary : hovered ? TextPrimary : TextSecondary;
            using var font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            TextRenderer.DrawText(g, Duration.Label, font, textRect, textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static GraphicsPath CreateRoundedPath(RectangleF bounds, int radius)
        {
            var d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // 2. Modern Rounded Button (Centered Icon + Text)
    private sealed class ModernButton : Button
    {
        public int CornerRadius { get; set; } = 8;
        public TileIconType? IconType { get; set; }
        public Color IconColor { get; set; } = Color.White;
        public Color BorderColor { get; set; } = Color.Transparent;
        public Color HoverBackColor { get; set; } = Color.Empty;
        public Color PressedBackColor { get; set; } = Color.Empty;

        private bool hovered;
        private bool pressed;

        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat;
            UseVisualStyleBackColor = false;
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? AppBackground);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var bounds = new RectangleF(0.5f, 0.5f, ClientSize.Width - 1, ClientSize.Height - 1);
            using var path = CreateRoundedPath(bounds, CornerRadius);

            var fill = !Enabled
                ? Color.FromArgb(32, 38, 52)
                : pressed && !PressedBackColor.IsEmpty
                    ? PressedBackColor
                    : hovered && !HoverBackColor.IsEmpty
                        ? HoverBackColor
                        : BackColor;

            using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);

            if (BorderColor != Color.Transparent)
            {
                using var pen = new Pen(BorderColor);
                g.DrawPath(pen, path);
            }

            // Balanced Co-centered Icon and Text
            var txtColor = Enabled ? ForeColor : TextMuted;
            if (IconType.HasValue)
            {
                int iconSz = Math.Min(16, ClientSize.Height - 12);
                int spacing = 6;
                var textSize = TextRenderer.MeasureText(g, Text, Font);
                int contentWidth = iconSz + spacing + textSize.Width;
                int startX = Math.Max(6, (ClientSize.Width - contentWidth) / 2);

                var iconRect = new Rectangle(startX, (ClientSize.Height - iconSz) / 2, iconSz, iconSz);
                var icColor = Enabled ? IconColor : TextMuted;
                VectorIconDrawer.DrawIcon(g, IconType.Value, iconRect, icColor, 1.8f);

                var textRect = new Rectangle(startX + iconSz + spacing, 0, ClientSize.Width - (startX + iconSz + spacing), ClientSize.Height);
                TextRenderer.DrawText(g, Text, Font, textRect, txtColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
            else
            {
                TextRenderer.DrawText(g, Text, Font, ClientRectangle, txtColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private static GraphicsPath CreateRoundedPath(RectangleF bounds, int radius)
        {
            var d = Math.Max(2, radius * 2);
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // 3. Card Panel Container
    private sealed class CardPanel : Panel
    {
        public int CornerRadius { get; set; } = 12;
        public Color BorderColor { get; set; } = CardBorder;

        public CardPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? AppBackground);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new RectangleF(0.5f, 0.5f, ClientSize.Width - 1, ClientSize.Height - 1);
            using var path = CreateRoundedPath(bounds, CornerRadius);

            using (var brush = new SolidBrush(BackColor)) g.FillPath(brush, path);
            using (var pen = new Pen(BorderColor, 1f)) g.DrawPath(pen, path);
        }

        private static GraphicsPath CreateRoundedPath(RectangleF bounds, int radius)
        {
            var d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // 4. Pill Badge (Auto-width, No Clipping)
    private sealed class PillBadge : Control
    {
        private readonly Color textColor;
        private readonly Color bgColor;

        public PillBadge(string text, Color textColor, Color bgColor)
        {
            Text = text;
            this.textColor = textColor;
            this.bgColor = bgColor;
            Height = 24;
            Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            UpdateSize();
        }

        public void UpdateSize()
        {
            var size = TextRenderer.MeasureText(Text, Font);
            Width = size.Width + 20;
            Invalidate();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            UpdateSize();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? CardBackground);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var bounds = new RectangleF(1, 1, ClientSize.Width - 2, ClientSize.Height - 2);
            float radius = bounds.Height / 2f;
            using var path = CreateRoundedPath(bounds, radius);

            using (var brush = new SolidBrush(bgColor)) g.FillPath(brush, path);
            using (var pen = new Pen(Color.FromArgb(70, textColor), 1f)) g.DrawPath(pen, path);

            TextRenderer.DrawText(g, Text, Font, Rectangle.Round(bounds), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private static GraphicsPath CreateRoundedPath(RectangleF bounds, float radius)
        {
            var d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // 5. Custom Dark Time Stepper Input
    private sealed class TimeStepperInput : Control
    {
        private int val;
        public int Value
        {
            get => val;
            set
            {
                int clamped = Math.Clamp(value, 0, Maximum);
                if (val != clamped)
                {
                    val = clamped;
                    Invalidate();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public int Maximum { get; }
        public string UnitLabel { get; }
        public event EventHandler? ValueChanged;

        private bool hoveredUp;
        private bool hoveredDown;
        private bool isFocused;

        public TimeStepperInput(int maximum, string unitLabel)
        {
            Maximum = maximum;
            UnitLabel = unitLabel;
            Size = new Size(78, 34);
            DoubleBuffered = true;
            Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            SetStyle(ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnGotFocus(EventArgs e) { isFocused = true; Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { isFocused = false; Invalidate(); base.OnLostFocus(e); }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            Value += e.Delta > 0 ? 1 : -1;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Up) { Value++; e.Handled = true; }
            else if (e.KeyCode == Keys.Down) { Value--; e.Handled = true; }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var wasUp = hoveredUp;
            var wasDown = hoveredDown;
            hoveredUp = e.X >= 31 && e.X <= 47 && e.Y < 17;
            hoveredDown = e.X >= 31 && e.X <= 47 && e.Y >= 17;
            if (wasUp != hoveredUp || wasDown != hoveredDown) Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hoveredUp = false;
            hoveredDown = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.X >= 31 && e.X <= 47)
            {
                if (e.Y < 17) Value++;
                else Value--;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? CardBackground);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Box for number & steppers
            var boxBounds = new RectangleF(0.5f, 0.5f, 48, ClientSize.Height - 1);
            using var path = new GraphicsPath();
            float radius = 6;
            float d = radius * 2;
            path.AddArc(boxBounds.X, boxBounds.Y, d, d, 180, 90);
            path.AddArc(boxBounds.Right - d, boxBounds.Y, d, d, 270, 90);
            path.AddArc(boxBounds.Right - d, boxBounds.Bottom - d, d, d, 0, 90);
            path.AddArc(boxBounds.X, boxBounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            using (var brush = new SolidBrush(InputBackground)) g.FillPath(brush, path);
            using (var pen = new Pen(isFocused ? CardBorderActive : CardBorder, 1f)) g.DrawPath(pen, path);

            // Value text
            var textRect = new Rectangle(1, 0, 30, ClientSize.Height);
            TextRenderer.DrawText(g, val.ToString("D2"), Font, textRect, TextPrimary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // Up / Down Stepper Arrows
            var upColor = hoveredUp ? AccentPrimary : TextMuted;
            var downColor = hoveredDown ? AccentPrimary : TextMuted;

            using (var upPen = new Pen(upColor, 1.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(upPen, 35, 11, 39, 7);
                g.DrawLine(upPen, 39, 7, 43, 11);
            }

            using (var downPen = new Pen(downColor, 1.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(downPen, 35, 21, 39, 25);
                g.DrawLine(downPen, 39, 25, 43, 21);
            }

            // Unit Label outside
            var unitRect = new Rectangle(52, 0, ClientSize.Width - 52, ClientSize.Height);
            using var unitFont = new Font("Segoe UI", 8F, FontStyle.Bold);
            TextRenderer.DrawText(g, UnitLabel, unitFont, unitRect, TextSecondary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }

    // 7. Icon Badge
    private sealed class IconBadge : Control
    {
        private readonly TileIconType iconType;
        private readonly Color iconColor;
        private readonly Color bgColor;

        public IconBadge(TileIconType iconType, Color iconColor, Color bgColor, int size = 36)
        {
            this.iconType = iconType;
            this.iconColor = iconColor;
            this.bgColor = bgColor;
            Size = new Size(size, size);
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? AppBackground);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new RectangleF(1, 1, ClientSize.Width - 2, ClientSize.Height - 2);
            using var path = new GraphicsPath();
            path.AddEllipse(bounds);

            using (var brush = new SolidBrush(bgColor)) g.FillPath(brush, path);
            using (var pen = new Pen(Color.FromArgb(60, iconColor), 1f)) g.DrawPath(pen, path);

            var iconBounds = new Rectangle(4, 4, ClientSize.Width - 8, ClientSize.Height - 8);
            VectorIconDrawer.DrawIcon(g, iconType, iconBounds, iconColor, 1.8f);
        }
    }

    // 8. Countdown Progress Ring (Unified GDI+ vector painting — No black box overlay)
    private sealed class CountdownProgressRing : Control
    {
        private float progress = 1f;
        private string timeText = "00:00:00";
        private string subtitleText = "Thời gian còn lại";

        public float Progress
        {
            get => progress;
            set { progress = Math.Clamp(value, 0F, 1F); Invalidate(); }
        }

        public string TimeText
        {
            get => timeText;
            set
            {
                if (timeText != value)
                {
                    timeText = value;
                    Invalidate();
                }
            }
        }

        public string SubtitleText
        {
            get => subtitleText;
            set
            {
                if (subtitleText != value)
                {
                    subtitleText = value;
                    Invalidate();
                }
            }
        }

        public CountdownProgressRing()
        {
            DoubleBuffered = true;
            MinimumSize = new Size(160, 160);
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = AppBackground;
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? AppBackground);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var size = Math.Max(1, Math.Min(ClientSize.Width, ClientSize.Height) - 20);
            size = Math.Clamp(size, 160, 260);
            var bounds = new Rectangle((ClientSize.Width - size) / 2, (ClientSize.Height - size) / 2, size, size);

            // Subtle Outer Track (Unbroken full circle)
            using (var trackPen = new Pen(Color.FromArgb(28, 34, 48), 8F))
            {
                g.DrawArc(trackPen, bounds, -90, 360);
            }

            // Progress Arc
            if (progress > 0)
            {
                Color arcColor = progress > 0.25f ? AccentPrimary : progress > 0.1f ? Color.FromArgb(245, 158, 11) : AccentRed;
                using var valuePen = new Pen(arcColor, 8F)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                };
                g.DrawArc(valuePen, bounds, -90, 360 * progress);
            }

            // Digital Time & Subtitle rendered directly in vector buffer (Eliminates child panel black box clipping)
            float timeFontSize = Math.Clamp(size * 0.135f, 22f, 34f);
            float subFontSize = Math.Clamp(size * 0.042f, 8.5f, 10.5f);

            using var timeFont = new Font("Segoe UI", timeFontSize, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", subFontSize, FontStyle.Regular);

            var timeSize = g.MeasureString(timeText, timeFont);
            var subSize = g.MeasureString(subtitleText, subFont);

            float totalTextHeight = timeSize.Height + subSize.Height + 2f;
            float startY = bounds.Y + (bounds.Height - totalTextHeight) / 2f;

            using (var timeBrush = new SolidBrush(TextPrimary))
            {
                var timeRect = new RectangleF(bounds.X, startY, bounds.Width, timeSize.Height);
                using var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(timeText, timeFont, timeBrush, timeRect, sf);
            }

            using (var subBrush = new SolidBrush(TextSecondary))
            {
                var subRect = new RectangleF(bounds.X, startY + timeSize.Height + 2f, bounds.Width, subSize.Height);
                using var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(subtitleText, subFont, subBrush, subRect, sf);
            }
        }
    }

    // ==========================================
    // DIALOGS
    // ==========================================
    private sealed class ChoiceDialog : Form
    {
        public ChoiceDialog(string title, string message, string primaryText, string cancelText, Color primaryColor)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(420, 190);
            BackColor = AppBackground;
            ForeColor = TextPrimary;
            Padding = new Padding(20);

            var icon = new IconBadge(TileIconType.Power, primaryColor, AccentPrimaryBg, 34)
            {
                Margin = new Padding(0, 0, 12, 0)
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 4)
            };

            var body = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = TextSecondary,
                AutoSize = true,
                MaximumSize = new Size(380, 0),
                Margin = new Padding(0, 8, 0, 16)
            };

            var primary = new ModernButton
            {
                Text = primaryText,
                DialogResult = DialogResult.OK,
                CornerRadius = 7,
                BackColor = primaryColor,
                HoverBackColor = AccentPrimaryHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(14, 6, 14, 6),
                AutoSize = true,
                Cursor = Cursors.Hand
            };

            var cancel = new ModernButton
            {
                Text = cancelText,
                DialogResult = DialogResult.Cancel,
                CornerRadius = 7,
                BackColor = CardBackground,
                HoverBackColor = CardHoverBackground,
                BorderColor = CardBorder,
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Padding = new Padding(12, 6, 12, 6),
                AutoSize = true,
                Cursor = Cursors.Hand
            };

            var actions = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Bottom,
                BackColor = AppBackground
            };
            actions.Controls.Add(primary);
            actions.Controls.Add(cancel);

            var header = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 4),
                BackColor = AppBackground
            };
            header.Controls.Add(icon);
            header.Controls.Add(titleLabel);

            var content = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Dock = DockStyle.Fill,
                BackColor = AppBackground
            };
            content.Controls.Add(header);
            content.Controls.Add(body);

            Controls.Add(content);
            Controls.Add(actions);

            AcceptButton = primary;
            CancelButton = cancel;
        }
    }

    private sealed class CloseScheduleDialog : Form
    {
        public CloseScheduleDialog()
        {
            Text = "Lịch tắt máy đang hoạt động";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(460, 220);
            BackColor = AppBackground;
            ForeColor = TextPrimary;
            Padding = new Padding(20);

            var icon = new IconBadge(TileIconType.Moon, AccentPrimary, AccentPrimaryBg, 34)
            {
                Margin = new Padding(0, 0, 12, 0)
            };

            var heading = new Label
            {
                Text = "Lịch tắt máy vẫn đang hoạt động",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 4)
            };

            var body = new Label
            {
                Text = "Thoát ứng dụng sẽ không tự động hủy lịch đã gửi tới Windows. Bạn muốn làm gì?",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = TextSecondary,
                AutoSize = true,
                MaximumSize = new Size(410, 0),
                Margin = new Padding(0, 8, 0, 12)
            };

            var abort = new ModernButton
            {
                Text = "Hủy lịch rồi thoát",
                IconType = TileIconType.Close,
                IconColor = Color.White,
                DialogResult = DialogResult.OK,
                CornerRadius = 7,
                BackColor = AccentRed,
                HoverBackColor = AccentRedHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(12, 6, 12, 6),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4),
                Cursor = Cursors.Hand
            };

            var keep = new ModernButton
            {
                Text = "Thoát, vẫn giữ lịch tắt máy",
                IconType = TileIconType.Moon,
                IconColor = TextSecondary,
                DialogResult = DialogResult.No,
                CornerRadius = 7,
                BackColor = CardBackground,
                HoverBackColor = CardHoverBackground,
                BorderColor = CardBorder,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Padding = new Padding(12, 6, 12, 6),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4),
                Cursor = Cursors.Hand
            };

            var back = new ModernButton
            {
                Text = "Quay lại ứng dụng",
                DialogResult = DialogResult.Cancel,
                CornerRadius = 7,
                BackColor = CardBackground,
                HoverBackColor = CardHoverBackground,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                Padding = new Padding(12, 5, 12, 5),
                AutoSize = true,
                Cursor = Cursors.Hand
            };

            var actions = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Dock = DockStyle.Bottom,
                BackColor = AppBackground
            };
            actions.Controls.Add(abort);
            actions.Controls.Add(keep);
            actions.Controls.Add(back);

            var header = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 4),
                BackColor = AppBackground
            };
            header.Controls.Add(icon);
            header.Controls.Add(heading);

            var content = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Dock = DockStyle.Fill,
                BackColor = AppBackground
            };
            content.Controls.Add(header);
            content.Controls.Add(body);

            Controls.Add(content);
            Controls.Add(actions);

            AcceptButton = null;
            CancelButton = back;
        }
    }
}
