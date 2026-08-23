using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;

namespace ShutdownScheduler;

internal sealed class MainForm : Form
{
    // ==========================================
    // THEME PALETTE: SOLID FLUENT DARK STUDIO
    // ==========================================
    private static readonly Color AppBackground = Color.FromArgb(13, 17, 26);          // #0D111A Deep Obsidian
    private static readonly Color CardBackground = Color.FromArgb(22, 31, 48);         // #161F30 Elevated Slate
    private static readonly Color CardHoverBackground = Color.FromArgb(30, 42, 66);    // #1E2A42 Hover Slate
    private static readonly Color CardActiveBackground = Color.FromArgb(24, 52, 90);   // #18345A Active Slate
    private static readonly Color CardBorder = Color.FromArgb(38, 52, 78);             // #26344E Subtle Border
    private static readonly Color CardBorderHover = Color.FromArgb(60, 82, 118);       // #3C5276 Hover Border
    private static readonly Color InputBackground = Color.FromArgb(16, 23, 36);        // #101724 Dark Input Box

    private static readonly Color TextPrimary = Color.FromArgb(248, 250, 252);         // #F8FAFC Crisp White
    private static readonly Color TextSecondary = Color.FromArgb(148, 163, 184);       // #94A3B8 Cool Grey
    private static readonly Color TextMuted = Color.FromArgb(100, 116, 139);           // #64748B Slate Muted

    private static readonly Color AccentCyan = Color.FromArgb(56, 189, 248);           // #38BDF8 Sky Blue
    private static readonly Color AccentBlue = Color.FromArgb(59, 130, 246);           // #3B82F6 Electric Blue
    private static readonly Color AccentRed = Color.FromArgb(239, 68, 68);             // #EF4444 Crimson Power
    private static readonly Color AccentRedHover = Color.FromArgb(248, 113, 113);      // #F87171 Crimson Hover
    private static readonly Color AccentRedDark = Color.FromArgb(220, 38, 38);         // #DC2626 Deep Red
    private static readonly Color AccentEmerald = Color.FromArgb(16, 185, 129);        // #10B981 Emerald
    private static readonly Color AccentAmber = Color.FromArgb(245, 158, 11);          // #F59E0B Amber
    private static readonly Color AccentPurple = Color.FromArgb(129, 140, 248);        // #818CF8 Soft Violet

    // Solid Pre-blended Accent Backgrounds (Zero Alpha Ghosting)
    private static readonly Color BadgeCyanBg = Color.FromArgb(20, 48, 72);
    private static readonly Color BadgeEmeraldBg = Color.FromArgb(18, 48, 40);
    private static readonly Color BadgeRedBg = Color.FromArgb(48, 22, 30);
    private static readonly Color BadgeAmberBg = Color.FromArgb(48, 38, 20);
    private static readonly Color ChipNormalBg = Color.FromArgb(28, 38, 58);
    private static readonly Color ChipNormalHover = Color.FromArgb(38, 52, 78);
    private static readonly Color ChipDangerBg = Color.FromArgb(48, 24, 32);
    private static readonly Color ChipDangerHover = Color.FromArgb(68, 28, 38);

    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

    // ==========================================
    // SERVICES & STATE
    // ==========================================
    private readonly ShutdownService shutdownService = new();
    private readonly Panel schedulingPanel = new();
    private readonly Panel countdownPanel = new();

    // Scheduling View Controls
    private readonly Label currentClockLabel = new();
    private readonly Label scheduledForLabel = new();
    private readonly Label durationSummaryLabel = new();
    private readonly Label scheduleStatusLabel = new();
    private readonly TextBox commandTextBox = new();
    private readonly ModernButton commandCopyButton = new();
    private readonly Panel commandBoxContainer = new();
    private readonly NumericUpDown customHoursInput = new();
    private readonly NumericUpDown customMinutesInput = new();
    private readonly NumericUpDown customSecondsInput = new();
    private readonly List<DurationTileButton> durationTiles = [];
    private readonly ModernButton scheduleButton = new();
    private readonly ModernButton schedulingCloseButton = new();

    // Countdown View Controls
    private readonly Label countdownScheduledForLabel = new();
    private readonly Label countdownLabel = new();
    private readonly Label countdownSubLabel = new();
    private readonly Label countdownStatusLabel = new();
    private readonly CountdownProgressRing countdownProgressRing = new();
    private readonly FlowLayoutPanel ringCenterText = new();
    private readonly Label statStartTimeLabel = new();
    private readonly Label statTargetTimeLabel = new();
    private readonly Label statElapsedLabel = new();
    private readonly ModernButton cancelButton = new();
    private readonly ModernButton keepScheduleButton = new();

    // Timers
    private readonly System.Windows.Forms.Timer countdownTimer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer previewTimer = new() { Interval = 1000 };

    // Duration Options
    private readonly List<DurationOption> durationOptions =
    [
        new("30 phút", 30 * 60, "Nhanh", TileIconType.Lightning),
        new("1 giờ", 60 * 60, "Chuẩn", TileIconType.Clock),
        new("1,5 giờ", 90 * 60, "Nghỉ", TileIconType.Coffee),
        new("2 giờ", 2 * 60 * 60, "Phim", TileIconType.Film),
        new("3 giờ", 3 * 60 * 60, "Game", TileIconType.Gamepad),
        new("4 giờ", 4 * 60 * 60, "Tối", TileIconType.Moon),
        new("6 giờ", 6 * 60 * 60, "Ngủ", TileIconType.Zzz),
        new("8 giờ", 8 * 60 * 60, "Sâu", TileIconType.Bed)
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
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = AppBackground;
        ForeColor = TextPrimary;
        ClientSize = new Size(860, 525);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular);

        // Load Icon if exists
        try
        {
            if (File.Exists("app.ico"))
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
        schedulingPanel.Padding = new Padding(22, 16, 22, 18);

        // Main 3-Row Grid: [Header: 48px] / [Content: 100%] / [Footer: 44px]
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
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Header
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Content Cards
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); // Footer

        // --- ROW 0: HEADER ---
        var headerTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = AppBackground
        };
        headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var headerLeft = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = AppBackground
        };

        var logoIcon = new IconBadge(TileIconType.Power, AccentRed, BadgeRedBg, 38);
        logoIcon.Margin = new Padding(0, 1, 10, 0);

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
            Text = "Hẹn giờ tắt máy",
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            ForeColor = TextPrimary,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 1)
        };

        var subtitleLabel = new Label
        {
            Text = "Chọn mốc thời gian có sẵn hoặc tùy chỉnh chính xác theo nhu cầu.",
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = TextSecondary,
            AutoSize = true,
            Margin = Padding.Empty
        };

        titleBox.Controls.Add(titleLabel);
        titleBox.Controls.Add(subtitleLabel);

        headerLeft.Controls.Add(logoIcon);
        headerLeft.Controls.Add(titleBox);

        // Header Right: Live Clock Badge
        currentClockLabel.AutoSize = true;
        currentClockLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        currentClockLabel.ForeColor = AccentCyan;
        currentClockLabel.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        currentClockLabel.Margin = new Padding(0, 8, 0, 0);

        headerTable.Controls.Add(headerLeft, 0, 0);
        headerTable.Controls.Add(currentClockLabel, 1, 0);

        // --- ROW 1: 2-COLUMN CARDS GRID ---
        var centerGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 2, 0, 6),
            Padding = Padding.Empty,
            BackColor = AppBackground
        };
        centerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
        centerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));

        // LEFT CARD: Quick Options & Custom Input
        var leftCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14, 12, 14, 12),
            BackColor = CardBackground,
            BorderColor = CardBorder,
            CornerRadius = 12,
            TopAccentColor = AccentPurple,
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
        leftCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20)); // Header 1
        leftCardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 2x4 Bento Grid
        leftCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); // Header 2
        leftCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); // Inputs
        leftCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // Quick Chips

        // Section 1 Header
        var quickHeader = CreateSectionHeader("⚡ MỐC THỜI GIAN NHANH", AccentCyan);

        // 2x4 Bento Grid
        var durationGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Margin = new Padding(0, 2, 0, 4),
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
        var customHeader = CreateSectionHeader("⏱ HOẶC TÙY CHỈNH CHÍNH XÁC", AccentPurple);

        // Inputs Flow
        ConfigureCustomDurationInput(customHoursInput, 99, "Giờ");
        ConfigureCustomDurationInput(customMinutesInput, 59, "Phút");
        ConfigureCustomDurationInput(customSecondsInput, 59, "Giây");

        var customInputsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = CardBackground
        };
        customInputsFlow.Controls.Add(CreateTimeInputGroup(customHoursInput, "h"));
        customInputsFlow.Controls.Add(CreateTimeInputGroup(customMinutesInput, "m"));
        customInputsFlow.Controls.Add(CreateTimeInputGroup(customSecondsInput, "s"));

        // Quick Chips Flow
        var quickChips = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 2, 0, 0),
            BackColor = CardBackground
        };
        quickChips.Controls.Add(CreateQuickChip("+15p", () => AddCustomMinutes(15)));
        quickChips.Controls.Add(CreateQuickChip("+30p", () => AddCustomMinutes(30)));
        quickChips.Controls.Add(CreateQuickChip("+1h", () => AddCustomMinutes(60)));
        quickChips.Controls.Add(CreateQuickChip("+2h", () => AddCustomMinutes(120)));
        quickChips.Controls.Add(CreateQuickChip("Đặt lại", ResetCustomDuration, isDanger: true));

        leftCardLayout.Controls.Add(quickHeader, 0, 0);
        leftCardLayout.Controls.Add(durationGrid, 0, 1);
        leftCardLayout.Controls.Add(customHeader, 0, 2);
        leftCardLayout.Controls.Add(customInputsFlow, 0, 3);
        leftCardLayout.Controls.Add(quickChips, 0, 4);

        leftCard.Controls.Add(leftCardLayout);

        // RIGHT CARD: Preview & Summary
        var rightCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 14, 18, 14),
            BackColor = CardBackground,
            BorderColor = CardBorder,
            CornerRadius = 12,
            TopAccentColor = AccentCyan,
            Margin = new Padding(6, 0, 0, 0)
        };

        var rightCardLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = Padding.Empty,
            AutoScroll = false,
            BackColor = CardBackground
        };

        var summaryBadge = new PillBadge("DỰ KIẾN TẮT MÁY", AccentCyan, BadgeCyanBg)
        {
            Margin = new Padding(0, 0, 0, 8)
        };

        durationSummaryLabel.AutoSize = true;
        durationSummaryLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        durationSummaryLabel.ForeColor = AccentCyan;
        durationSummaryLabel.Margin = new Padding(0, 0, 0, 1);

        scheduledForLabel.AutoSize = true;
        scheduledForLabel.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
        scheduledForLabel.ForeColor = TextPrimary;
        scheduledForLabel.Margin = new Padding(0, 0, 0, 2);

        var previewHint = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = TextSecondary,
            Text = "Tính toán tự động theo thời gian thực.",
            Margin = new Padding(0, 0, 0, 10)
        };

        // System Command Box
        var commandToggle = new ModernButton
        {
            Text = "  Lệnh hệ thống Windows",
            IconType = TileIconType.Terminal,
            IconColor = TextSecondary,
            AutoSize = true,
            CornerRadius = 6,
            BackColor = InputBackground,
            BorderColor = CardBorder,
            ForeColor = TextSecondary,
            Padding = new Padding(8, 4, 10, 4),
            Margin = new Padding(0, 0, 0, 6),
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
        };

        commandBoxContainer.Visible = false;
        commandBoxContainer.AutoSize = true;
        commandBoxContainer.Margin = new Padding(0, 0, 0, 8);
        commandBoxContainer.BackColor = CardBackground;

        var commandInnerCard = new CardPanel
        {
            AutoSize = true,
            Width = 280,
            BackColor = InputBackground,
            BorderColor = CardBorder,
            CornerRadius = 6,
            Padding = new Padding(6, 3, 6, 3),
            Margin = Padding.Empty
        };

        var commandInnerTable = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Dock = DockStyle.Fill,
            BackColor = InputBackground,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        commandInnerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        commandInnerTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        commandTextBox.ReadOnly = true;
        commandTextBox.BorderStyle = BorderStyle.None;
        commandTextBox.BackColor = InputBackground;
        commandTextBox.ForeColor = AccentCyan;
        commandTextBox.Font = new Font(FontFamily.GenericMonospace, 9F, FontStyle.Regular);
        commandTextBox.Dock = DockStyle.Fill;
        commandTextBox.Margin = new Padding(2, 3, 2, 2);

        commandCopyButton.Text = "Copy";
        commandCopyButton.IconType = TileIconType.Copy;
        commandCopyButton.IconColor = TextSecondary;
        commandCopyButton.CornerRadius = 4;
        commandCopyButton.BackColor = CardBackground;
        commandCopyButton.BorderColor = CardBorder;
        commandCopyButton.ForeColor = TextSecondary;
        commandCopyButton.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        commandCopyButton.Padding = new Padding(6, 2, 6, 2);
        commandCopyButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(commandTextBox.Text))
            {
                Clipboard.SetText(commandTextBox.Text);
                commandCopyButton.Text = "Đã chép!";
                commandCopyButton.IconColor = AccentEmerald;
                Task.Delay(1500).ContinueWith(_ =>
                {
                    if (!IsDisposed && commandCopyButton.IsHandleCreated)
                    {
                        Invoke(() =>
                        {
                            commandCopyButton.Text = "Copy";
                            commandCopyButton.IconColor = TextSecondary;
                        });
                    }
                });
            }
        };

        commandInnerTable.Controls.Add(commandTextBox, 0, 0);
        commandInnerTable.Controls.Add(commandCopyButton, 1, 0);
        commandInnerCard.Controls.Add(commandInnerTable);
        commandBoxContainer.Controls.Add(commandInnerCard);

        commandToggle.Click += (_, _) =>
        {
            commandBoxContainer.Visible = !commandBoxContainer.Visible;
            commandToggle.Text = commandBoxContainer.Visible ? "  Ẩn lệnh hệ thống ▴" : "  Lệnh hệ thống Windows ▾";
        };

        // Safety Notice Badge
        var safetyNotice = new CardPanel
        {
            AutoSize = true,
            Padding = new Padding(8, 6, 8, 6),
            BackColor = InputBackground,
            BorderColor = CardBorder,
            CornerRadius = 8,
            Margin = new Padding(0, 2, 0, 0)
        };

        var safetyTable = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Dock = DockStyle.Fill,
            BackColor = InputBackground
        };
        safetyTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        safetyTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var shieldIcon = new IconBadge(TileIconType.Shield, AccentEmerald, BadgeEmeraldBg, 22);
        shieldIcon.Margin = new Padding(0, 0, 6, 0);

        var safetyLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 8F, FontStyle.Regular),
            ForeColor = TextSecondary,
            Text = "An toàn: Có thể hủy bất kỳ lúc nào.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        safetyTable.Controls.Add(shieldIcon, 0, 0);
        safetyTable.Controls.Add(safetyLabel, 1, 0);
        safetyNotice.Controls.Add(safetyTable);

        rightCardLayout.Controls.Add(summaryBadge);
        rightCardLayout.Controls.Add(durationSummaryLabel);
        rightCardLayout.Controls.Add(scheduledForLabel);
        rightCardLayout.Controls.Add(previewHint);
        rightCardLayout.Controls.Add(commandToggle);
        rightCardLayout.Controls.Add(commandBoxContainer);
        rightCardLayout.Controls.Add(safetyNotice);

        rightCard.Controls.Add(rightCardLayout);

        centerGrid.Controls.Add(leftCard, 0, 0);
        centerGrid.Controls.Add(rightCard, 1, 0);

        // --- ROW 2: FOOTER (Comfortable Chin with margin) ---
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
        scheduleStatusLabel.ForeColor = TextSecondary;
        scheduleStatusLabel.Text = "Lịch chỉ được gửi tới Windows sau khi bạn xác nhận.";
        scheduleStatusLabel.Anchor = AnchorStyles.Left | AnchorStyles.None;

        var actionsFlow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            BackColor = AppBackground
        };

        scheduleButton.Text = "  Xác nhận tắt máy";
        scheduleButton.IconType = TileIconType.Power;
        scheduleButton.IconColor = Color.White;
        scheduleButton.CornerRadius = 8;
        scheduleButton.BackColor = AccentRed;
        scheduleButton.HoverBackColor = AccentRedHover;
        scheduleButton.PressedBackColor = AccentRedDark;
        scheduleButton.ForeColor = Color.White;
        scheduleButton.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        scheduleButton.Padding = new Padding(16, 7, 16, 7);
        scheduleButton.AutoSize = true;
        scheduleButton.Cursor = Cursors.Hand;
        scheduleButton.Click += ScheduleButton_Click;

        schedulingCloseButton.Text = "  Thoát ứng dụng";
        schedulingCloseButton.IconType = TileIconType.Close;
        schedulingCloseButton.IconColor = TextSecondary;
        schedulingCloseButton.CornerRadius = 8;
        schedulingCloseButton.BackColor = CardBackground;
        schedulingCloseButton.HoverBackColor = CardHoverBackground;
        schedulingCloseButton.BorderColor = CardBorder;
        schedulingCloseButton.ForeColor = TextPrimary;
        schedulingCloseButton.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        schedulingCloseButton.Padding = new Padding(12, 7, 12, 7);
        schedulingCloseButton.AutoSize = true;
        schedulingCloseButton.Cursor = Cursors.Hand;
        schedulingCloseButton.Click += (_, _) => Close();

        actionsFlow.Controls.Add(scheduleButton);
        actionsFlow.Controls.Add(schedulingCloseButton);

        footerTable.Controls.Add(scheduleStatusLabel, 0, 0);
        footerTable.Controls.Add(actionsFlow, 1, 0);

        mainLayout.Controls.Add(headerTable, 0, 0);
        mainLayout.Controls.Add(centerGrid, 0, 1);
        mainLayout.Controls.Add(footerTable, 0, 2);

        schedulingPanel.Controls.Add(mainLayout);
    }

    // ==========================================
    // DURATION TILE COMPONENT BUILDER
    // ==========================================
    private Button CreateDurationTile(DurationOption duration)
    {
        var tile = new DurationTileButton(duration)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(2)
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
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            ForeColor = accentColor,
            AutoSize = true
        };
    }

    private Control CreateTimeInputGroup(NumericUpDown input, string labelText)
    {
        var card = new CardPanel
        {
            AutoSize = true,
            BackColor = InputBackground,
            BorderColor = CardBorder,
            CornerRadius = 6,
            Padding = new Padding(4, 2, 4, 2),
            Margin = new Padding(0, 0, 6, 0)
        };

        var layout = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Dock = DockStyle.Fill,
            BackColor = InputBackground
        };

        layout.Controls.Add(input);

        var lbl = new Label
        {
            Text = labelText,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            ForeColor = TextSecondary,
            AutoSize = true,
            Anchor = AnchorStyles.None,
            Margin = new Padding(1, 3, 2, 0)
        };
        layout.Controls.Add(lbl);

        card.Controls.Add(layout);
        return card;
    }

    private ModernButton CreateQuickChip(string text, Action onClick, bool isDanger = false)
    {
        var chip = new ModernButton
        {
            Text = text,
            AutoSize = true,
            CornerRadius = 6,
            BackColor = isDanger ? ChipDangerBg : ChipNormalBg,
            HoverBackColor = isDanger ? ChipDangerHover : ChipNormalHover,
            BorderColor = isDanger ? AccentRed : CardBorder,
            ForeColor = isDanger ? AccentRedHover : TextSecondary,
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
        var currentTotalMinutes = (int)(customHoursInput.Value * 60 + customMinutesInput.Value + (customSecondsInput.Value > 0 ? 1 : 0));
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
        countdownPanel.Padding = new Padding(24, 16, 24, 18);

        var outerContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = AppBackground
        };
        outerContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        outerContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // 0: Top Status Badge
        outerContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 26)); // 1: Scheduled Time Subtitle
        outerContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 2: Circular Progress Hero
        outerContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); // 3: Bento Stats (Start, End, Elapsed)
        outerContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); // 4: Actions (Cancel, Keep)

        // Top Status Badge
        var topBadgeContainer = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Anchor = AnchorStyles.None,
            Margin = Padding.Empty,
            BackColor = AppBackground
        };
        var liveBadge = new PillBadge("● LỊCH TẮT MÁY ĐANG HOẠT ĐỘNG", AccentEmerald, BadgeEmeraldBg);
        topBadgeContainer.Controls.Add(liveBadge);

        // Subtitle
        countdownScheduledForLabel.AutoSize = true;
        countdownScheduledForLabel.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        countdownScheduledForLabel.ForeColor = AccentCyan;
        countdownScheduledForLabel.TextAlign = ContentAlignment.MiddleCenter;
        countdownScheduledForLabel.Dock = DockStyle.Fill;

        // Progress Ring Container
        var ringContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppBackground,
            Margin = new Padding(6)
        };

        countdownProgressRing.Dock = DockStyle.Fill;
        countdownProgressRing.BackColor = AppBackground;

        // Inside the ring: Big Digital Clock & Subtitle
        ringCenterText.AutoSize = true;
        ringCenterText.FlowDirection = FlowDirection.TopDown;
        ringCenterText.WrapContents = false;
        ringCenterText.Anchor = AnchorStyles.None;
        ringCenterText.BackColor = AppBackground;

        countdownLabel.AutoSize = true;
        countdownLabel.Font = new Font("Segoe UI", 28F, FontStyle.Bold);
        countdownLabel.ForeColor = TextPrimary;
        countdownLabel.TextAlign = ContentAlignment.MiddleCenter;
        countdownLabel.Margin = new Padding(0, 0, 0, 1);

        countdownSubLabel.AutoSize = true;
        countdownSubLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        countdownSubLabel.ForeColor = TextSecondary;
        countdownSubLabel.Text = "Thời gian còn lại";
        countdownSubLabel.TextAlign = ContentAlignment.MiddleCenter;
        countdownSubLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;

        ringCenterText.Controls.Add(countdownLabel);
        ringCenterText.Controls.Add(countdownSubLabel);

        countdownProgressRing.Controls.Add(ringCenterText);
        void CenterRingText()
        {
            ringCenterText.Location = new Point(
                Math.Max(0, (countdownProgressRing.Width - ringCenterText.Width) / 2),
                Math.Max(0, (countdownProgressRing.Height - ringCenterText.Height) / 2)
            );
        }
        countdownProgressRing.Resize += (_, _) => CenterRingText();
        ringCenterText.SizeChanged += (_, _) => CenterRingText();

        ringContainer.Controls.Add(countdownProgressRing);

        // 3-Metric Bento Grid (Bắt đầu, Tắt lúc, Đã trôi qua)
        var statsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.None,
            Anchor = AnchorStyles.None,
            ColumnCount = 3,
            RowCount = 1,
            Width = 460,
            Height = 50,
            Margin = Padding.Empty,
            BackColor = AppBackground
        };
        statsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        statsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        statsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

        statsGrid.Controls.Add(CreateStatCard("BẮT ĐẦU", statStartTimeLabel, AccentPurple), 0, 0);
        statsGrid.Controls.Add(CreateStatCard("TẮT LÚC", statTargetTimeLabel, AccentCyan), 1, 0);
        statsGrid.Controls.Add(CreateStatCard("ĐÃ TRÔI QUA", statElapsedLabel, AccentEmerald), 2, 0);

        // Action Buttons
        cancelButton.Text = "  Hủy lịch tắt máy";
        cancelButton.IconType = TileIconType.Close;
        cancelButton.IconColor = Color.White;
        cancelButton.CornerRadius = 8;
        cancelButton.BackColor = AccentRed;
        cancelButton.HoverBackColor = AccentRedHover;
        cancelButton.PressedBackColor = AccentRedDark;
        cancelButton.ForeColor = Color.White;
        cancelButton.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        cancelButton.Padding = new Padding(18, 7, 18, 7);
        cancelButton.AutoSize = true;
        cancelButton.Cursor = Cursors.Hand;
        cancelButton.Click += CancelButton_Click;

        keepScheduleButton.Text = "  Thoát ứng dụng (Lịch vẫn tiếp tục)";
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

    private static Control CreateStatCard(string caption, Label valueLabel, Color accent)
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(6, 4, 6, 4),
            BackColor = CardBackground,
            BorderColor = CardBorder,
            CornerRadius = 8,
            Margin = new Padding(2)
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
            ForeColor = accent,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 1)
        };

        valueLabel.AutoSize = true;
        valueLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
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
    private void ConfigureCustomDurationInput(NumericUpDown input, decimal maximum, string accessibleName)
    {
        input.Minimum = 0;
        input.Maximum = maximum;
        input.Width = 48;
        input.BorderStyle = BorderStyle.None;
        input.BackColor = InputBackground;
        input.ForeColor = TextPrimary;
        input.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        input.TextAlign = HorizontalAlignment.Center;
        input.AccessibleName = accessibleName;
        input.Enter += (_, _) => SelectCustomDuration();
        input.ValueChanged += (_, _) => SelectCustomDuration();
    }

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

        seconds = decimal.ToInt32(customHoursInput.Value * 3600 + customMinutesInput.Value * 60 + customSecondsInput.Value);
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
        currentClockLabel.Text = $"⏱ Hiện tại: {DateTimeOffset.Now.ToString("HH:mm:ss", VietnameseCulture)}";

        if (activeSchedule is not null) return;
        if (!TryGetSelectedDuration(out var seconds, out var label))
        {
            scheduleButton.Enabled = false;
            commandTextBox.Text = string.Empty;
            durationSummaryLabel.Text = "Chưa chọn thời lượng";
            scheduledForLabel.Text = "--:--:--";
            scheduleStatusLabel.ForeColor = TextSecondary;
            scheduleStatusLabel.Text = "Chọn một mốc thời gian hoặc nhập thời lượng lớn hơn 0.";
            return;
        }

        scheduleButton.Enabled = true;
        var now = DateTimeOffset.Now;
        var scheduledFor = now.AddSeconds(seconds);
        var formattedScheduledFor = FormatScheduledFor(scheduledFor, now);

        commandTextBox.Text = $"shutdown -s -t {seconds}";
        durationSummaryLabel.Text = $"Tắt sau {label}";
        scheduledForLabel.Text = formattedScheduledFor;

        if (scheduleStatusLabel.ForeColor != AccentEmerald)
        {
            scheduleStatusLabel.ForeColor = TextSecondary;
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
        var command = $"shutdown -s -t {seconds}";

        if (ShowChoiceDialog(
            "Xác nhận hẹn giờ tắt máy",
            $"Máy tính sẽ tự động tắt sau {label}, vào lúc {FormatScheduledFor(scheduledFor, now)}.\n\nBạn có thể hủy lịch bất kỳ lúc nào trước thời điểm này.",
            "Lên lịch tắt máy",
            "Quay lại",
            AccentRed) != DialogResult.OK) return;

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
        activeSchedule = new ShutdownSchedule(seconds, scheduledFor, command);
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
        countdownScheduledForLabel.Text = $"Máy sẽ tắt lúc {FormatScheduledFor(activeSchedule!.ScheduledFor)}";
        countdownStatusLabel.ForeColor = TextSecondary;
        countdownStatusLabel.Text = "Hệ thống Windows đang thực hiện đếm ngược.";
        cancelButton.Text = "  Hủy lịch tắt máy";
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
            countdownLabel.Text = "00:00:00";
            countdownProgressRing.Progress = 0F;
            countdownStatusLabel.ForeColor = AccentRedHover;
            countdownStatusLabel.Text = "Thời gian đã kết thúc. Windows đang thực hiện tắt máy...";
            cancelButton.Enabled = false;
            return;
        }

        countdownLabel.Text = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        countdownProgressRing.Progress = (float)Math.Clamp(remaining.TotalSeconds / activeSchedule.DelaySeconds, 0, 1);
    }

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        scheduleButton.Enabled = !busy && activeSchedule is null;
        cancelButton.Enabled = !busy && activeSchedule is not null;
        schedulingCloseButton.Enabled = !busy;
        keepScheduleButton.Enabled = !busy;
        cancelButton.Text = busy && activeSchedule is not null ? "  Đang hủy lịch…" : "  Hủy lịch tắt máy";
        scheduleButton.Text = busy && activeSchedule is null ? "  Đang lên lịch…" : "  Xác nhận tắt máy";
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
    private sealed record DurationOption(string Label, int Seconds, string Tag, TileIconType IconType);

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
        public static void DrawIcon(Graphics g, TileIconType type, Rectangle bounds, Color color, float strokeWidth = 1.8f)
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
            using var pen = new Pen(c, stroke + 0.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
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

        private static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
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

            // Solid Fill
            var bg = isSelected ? CardActiveBackground : hovered ? CardHoverBackground : CardBackground;
            using (var brush = new SolidBrush(bg)) g.FillPath(brush, path);

            // Border
            var borderColor = isSelected ? AccentCyan : hovered ? CardBorderHover : CardBorder;
            var borderWidth = isSelected ? 1.6f : 1f;
            using (var pen = new Pen(borderColor, borderWidth)) g.DrawPath(pen, path);

            // Left Icon Badge
            int iconBoxSize = 22;
            var iconBox = new Rectangle(8, (ClientSize.Height - iconBoxSize) / 2, iconBoxSize, iconBoxSize);
            var iconColor = isSelected ? AccentCyan : hovered ? TextPrimary : TextSecondary;
            VectorIconDrawer.DrawIcon(g, Duration.IconType, iconBox, iconColor, 1.6f);

            // Right Indicator (Tag OR Checkmark)
            int rightReserved = 8;
            if (isSelected)
            {
                rightReserved = 28;
                var checkRect = new Rectangle(ClientSize.Width - 24, (ClientSize.Height - 16) / 2, 16, 16);
                VectorIconDrawer.DrawIcon(g, TileIconType.Check, checkRect, AccentCyan, 2f);
            }
            else if (!string.IsNullOrEmpty(Duration.Tag))
            {
                rightReserved = 46;
                var tagRect = new Rectangle(ClientSize.Width - 42, (ClientSize.Height - 18) / 2, 36, 18);
                using (var tagBg = new SolidBrush(InputBackground))
                using (var tagPath = CreateRoundedPath(new RectangleF(tagRect.X, tagRect.Y, tagRect.Width, tagRect.Height), 4))
                {
                    g.FillPath(tagBg, tagPath);
                }
                using var tagFont = new Font("Segoe UI", 7.5F, FontStyle.Bold);
                TextRenderer.DrawText(g, Duration.Tag, tagFont, tagRect, TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            // Duration Label
            int textX = 34;
            int textWidth = Math.Max(10, ClientSize.Width - textX - rightReserved);
            var textRect = new Rectangle(textX, 0, textWidth, ClientSize.Height);
            var textColor = isSelected ? TextPrimary : hovered ? TextPrimary : Color.FromArgb(226, 232, 240);
            using (var font = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, Duration.Label, font, textRect, textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
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

    // 2. Modern Rounded Button with Vector Icon
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
                ? Color.FromArgb(40, 50, 70)
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

            // Draw Icon + Text
            int textOffset = 0;
            if (IconType.HasValue)
            {
                int iconSz = Math.Min(16, ClientSize.Height - 10);
                var iconRect = new Rectangle(Padding.Left > 0 ? Padding.Left - 4 : 8, (ClientSize.Height - iconSz) / 2, iconSz, iconSz);
                var icColor = Enabled ? IconColor : TextMuted;
                VectorIconDrawer.DrawIcon(g, IconType.Value, iconRect, icColor, 1.8f);
                textOffset = iconSz + 4;
            }

            var textRect = new Rectangle(textOffset, 0, ClientSize.Width - textOffset, ClientSize.Height);
            var txtColor = Enabled ? ForeColor : TextMuted;
            TextRenderer.DrawText(g, Text, Font, textRect, txtColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
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
        public Color BorderColor { get; set; } = Color.FromArgb(38, 52, 78);
        public Color TopAccentColor { get; set; } = Color.Transparent;

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

            if (TopAccentColor != Color.Transparent)
            {
                using var accentPen = new Pen(TopAccentColor, 3f);
                g.DrawLine(accentPen, bounds.X + CornerRadius, bounds.Y + 1.5f, bounds.Right - CornerRadius, bounds.Y + 1.5f);
            }
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

    // 4. Pill Badge
    private sealed class PillBadge : Control
    {
        private readonly Color textColor;
        private readonly Color bgColor;

        public PillBadge(string text, Color textColor, Color bgColor)
        {
            Text = text;
            this.textColor = textColor;
            this.bgColor = bgColor;
            AutoSize = false;
            Height = 24;
            Width = 150;
            DoubleBuffered = true;
            Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
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
            using (var pen = new Pen(Color.FromArgb(80, textColor), 1f)) g.DrawPath(pen, path);

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

    // 5. Icon Badge
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
            using (var pen = new Pen(Color.FromArgb(80, iconColor), 1f)) g.DrawPath(pen, path);

            var iconBounds = new Rectangle(4, 4, ClientSize.Width - 8, ClientSize.Height - 8);
            VectorIconDrawer.DrawIcon(g, iconType, iconBounds, iconColor, 1.8f);
        }
    }

    // 6. Countdown Progress Ring
    private sealed class CountdownProgressRing : Control
    {
        private float progress = 1f;
        public float Progress
        {
            get => progress;
            set { progress = Math.Clamp(value, 0F, 1F); Invalidate(); }
        }

        public CountdownProgressRing()
        {
            DoubleBuffered = true;
            MinimumSize = new Size(180, 180);
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

            var size = Math.Max(1, Math.Min(ClientSize.Width, ClientSize.Height) - 24);
            var bounds = new Rectangle((ClientSize.Width - size) / 2, (ClientSize.Height - size) / 2, size, size);

            // Track Pen
            using (var trackPen = new Pen(Color.FromArgb(28, 38, 58), 7F))
            {
                g.DrawArc(trackPen, bounds, -90, 360);
            }

            if (progress > 0)
            {
                Color arcColor = progress > 0.3f ? AccentCyan : progress > 0.1f ? AccentAmber : AccentRed;
                using var valuePen = new Pen(arcColor, 7F)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                };
                g.DrawArc(valuePen, bounds, -90, 360 * progress);
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
            ClientSize = new Size(420, 200);
            BackColor = AppBackground;
            ForeColor = TextPrimary;
            Padding = new Padding(18);

            var icon = new IconBadge(TileIconType.Power, primaryColor, BadgeRedBg, 34);
            icon.Margin = new Padding(0, 0, 10, 0);

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 4)
            };

            var body = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = TextSecondary,
                AutoSize = true,
                MaximumSize = new Size(370, 0),
                Margin = new Padding(0, 0, 0, 14)
            };

            var primary = new ModernButton
            {
                Text = primaryText,
                DialogResult = DialogResult.OK,
                CornerRadius = 8,
                BackColor = primaryColor,
                HoverBackColor = Color.FromArgb(248, 113, 113),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(14, 7, 14, 7),
                AutoSize = true,
                Cursor = Cursors.Hand
            };

            var cancel = new ModernButton
            {
                Text = cancelText,
                DialogResult = DialogResult.Cancel,
                CornerRadius = 8,
                BackColor = CardBackground,
                HoverBackColor = CardHoverBackground,
                BorderColor = CardBorder,
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Padding = new Padding(12, 7, 12, 7),
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
            ClientSize = new Size(460, 230);
            BackColor = AppBackground;
            ForeColor = TextPrimary;
            Padding = new Padding(18);

            var icon = new IconBadge(TileIconType.Moon, AccentAmber, BadgeAmberBg, 34);
            icon.Margin = new Padding(0, 0, 10, 0);

            var heading = new Label
            {
                Text = "Lịch tắt máy vẫn đang hoạt động",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 4)
            };

            var body = new Label
            {
                Text = "Thoát ứng dụng sẽ không tự động hủy lịch đã gửi tới Windows. Bạn muốn làm gì?",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = TextSecondary,
                AutoSize = true,
                MaximumSize = new Size(410, 0),
                Margin = new Padding(0, 0, 0, 12)
            };

            var abort = new ModernButton
            {
                Text = "  Hủy lịch rồi thoát",
                IconType = TileIconType.Close,
                IconColor = Color.White,
                DialogResult = DialogResult.OK,
                CornerRadius = 8,
                BackColor = AccentRed,
                HoverBackColor = AccentRedHover,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(12, 7, 12, 7),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4),
                Cursor = Cursors.Hand
            };

            var keep = new ModernButton
            {
                Text = "  Thoát, vẫn giữ lịch tắt máy",
                IconType = TileIconType.Moon,
                IconColor = TextSecondary,
                DialogResult = DialogResult.No,
                CornerRadius = 8,
                BackColor = CardBackground,
                HoverBackColor = CardHoverBackground,
                BorderColor = CardBorder,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Padding = new Padding(12, 7, 12, 7),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4),
                Cursor = Cursors.Hand
            };

            var back = new ModernButton
            {
                Text = "  Quay lại ứng dụng",
                DialogResult = DialogResult.Cancel,
                CornerRadius = 8,
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
