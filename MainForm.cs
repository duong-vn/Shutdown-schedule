using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace ShutdownScheduler;

internal sealed class MainForm : Form
{
    private static readonly Color AppBackground = Color.FromArgb(243, 246, 251);
    private static readonly Color SurfaceColor = Color.White;
    private static readonly Color SoftSurfaceColor = Color.FromArgb(236, 243, 255);
    private static readonly Color InkColor = Color.FromArgb(20, 31, 48);
    private static readonly Color MutedColor = Color.FromArgb(88, 105, 129);
    private static readonly Color LineColor = Color.FromArgb(212, 222, 235);
    private static readonly Color PrimaryColor = Color.FromArgb(31, 101, 211);
    private static readonly Color PrimaryHoverColor = Color.FromArgb(20, 83, 183);
    private static readonly Color SuccessColor = Color.FromArgb(23, 130, 79);
    private static readonly Color DangerColor = Color.FromArgb(202, 57, 71);
    private static readonly Color DangerHoverColor = Color.FromArgb(171, 43, 58);
    private static readonly Color CountdownBackground = Color.FromArgb(17, 27, 45);
    private static readonly Color CountdownSurface = Color.FromArgb(27, 42, 67);
    private static readonly Color CountdownMuted = Color.FromArgb(185, 201, 226);
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

    private readonly ShutdownService shutdownService = new();
    private readonly Panel schedulingPanel = new();
    private readonly Panel countdownPanel = new();
    private readonly Label scheduledForLabel = new();
    private readonly Label durationSummaryLabel = new();
    private readonly Label scheduleStatusLabel = new();
    private readonly TextBox commandTextBox = new();
    private readonly Label countdownScheduledForLabel = new();
    private readonly Label countdownLabel = new();
    private readonly Label countdownStatusLabel = new();
    private readonly CountdownProgressRing countdownProgressRing = new();
    private readonly Button scheduleButton = new();
    private readonly Button cancelButton = new();
    private readonly NumericUpDown customHoursInput = new();
    private readonly NumericUpDown customMinutesInput = new();
    private readonly NumericUpDown customSecondsInput = new();
    private readonly List<Button> durationTiles = [];
    private readonly System.Windows.Forms.Timer countdownTimer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer previewTimer = new() { Interval = 1000 };
    private readonly List<DurationOption> durationOptions =
    [
        new("30 phút", 30 * 60), new("1 giờ", 60 * 60), new("1,5 giờ", 90 * 60), new("2 giờ", 2 * 60 * 60),
        new("3 giờ", 3 * 60 * 60), new("4 giờ", 4 * 60 * 60), new("6 giờ", 6 * 60 * 60), new("8 giờ", 8 * 60 * 60)
    ];

    private DurationOption? selectedDuration;
    private bool suppressCustomInputEvents;
    private ShutdownSchedule? activeSchedule;
    private Button? schedulingCloseButton;
    private Button? keepScheduleButton;

    public MainForm()
    {
        selectedDuration = durationOptions[1];
        Text = "Shutdown Control";
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = AppBackground;
        ClientSize = new Size(880, 620);
        MinimumSize = new Size(560, 560);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10F);

        InitializeSchedulingView();
        InitializeCountdownView();
        Controls.Add(schedulingPanel);
        Controls.Add(countdownPanel);

        countdownTimer.Tick += (_, _) => UpdateCountdown();
        previewTimer.Tick += (_, _) => UpdatePreview();
        FormClosing += MainForm_FormClosing;
        ShowSchedulingView();
    }

    private void InitializeSchedulingView()
    {
        schedulingPanel.Dock = DockStyle.Fill;
        schedulingPanel.BackColor = AppBackground;
        schedulingPanel.AutoScroll = true;
        schedulingPanel.Padding = new Padding(32);

        var title = CreateLabel("Hẹn giờ tắt máy", 28F, FontStyle.Bold, InkColor);
        title.Margin = new Padding(0, 0, 0, 6);
        var subtitle = CreateLabel("Chọn thời lượng, kiểm tra thời điểm tắt và xác nhận khi bạn sẵn sàng.", 10.5F, FontStyle.Regular, MutedColor);
        subtitle.MaximumSize = new Size(620, 0);
        subtitle.Margin = new Padding(0, 0, 0, 26);

        var header = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Top };
        header.Controls.Add(title);
        header.Controls.Add(subtitle);

        var durationTitle = CreateLabel("Bạn muốn máy tắt sau bao lâu?", 15F, FontStyle.Bold, InkColor);
        durationTitle.Dock = DockStyle.Fill;
        durationTitle.Margin = new Padding(0, 0, 0, 14);

        var durationGrid = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, RowCount = 4, Dock = DockStyle.Top, Margin = new Padding(0) };
        durationGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        durationGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (var row = 0; row < 4; row++) durationGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        for (var index = 0; index < durationOptions.Count; index++) durationGrid.Controls.Add(CreateDurationTile(durationOptions[index]), index % 2, index / 2);

        var presetScroller = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Height = 170, Margin = new Padding(0, 0, 0, 16) };
        presetScroller.Controls.Add(durationGrid);

        ConfigureCustomDurationInput(customHoursInput, 23, "Số giờ đến khi tắt máy");
        ConfigureCustomDurationInput(customMinutesInput, 59, "Số phút đến khi tắt máy");
        ConfigureCustomDurationInput(customSecondsInput, 59, "Số giây đến khi tắt máy");
        var customCaption = CreateLabel("Bạn muốn Windows tắt sau:", 10F, FontStyle.Bold, InkColor);
        customCaption.Dock = DockStyle.Fill;
        var customInputs = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
        customInputs.Controls.Add(customHoursInput);
        customInputs.Controls.Add(CreateLabel("h", 10F, FontStyle.Bold, MutedColor));
        customInputs.Controls.Add(customMinutesInput);
        customInputs.Controls.Add(CreateLabel("m", 10F, FontStyle.Bold, MutedColor));
        customInputs.Controls.Add(customSecondsInput);
        customInputs.Controls.Add(CreateLabel("s", 10F, FontStyle.Bold, MutedColor));
        var customDuration = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Dock = DockStyle.Fill, Margin = new Padding(0) };
        customDuration.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        customDuration.Controls.Add(customCaption, 0, 0);
        customDuration.Controls.Add(customInputs, 0, 1);

        var durationBody = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, RowCount = 3, Dock = DockStyle.Top, Padding = new Padding(24), BackColor = SurfaceColor };
        durationBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        durationBody.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        durationBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 186));
        durationBody.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        durationBody.Controls.Add(durationTitle, 0, 0);
        durationBody.Controls.Add(presetScroller, 0, 1);
        durationBody.Controls.Add(customDuration, 0, 2);

        var summaryTitle = CreateLabel("Máy sẽ tắt", 15F, FontStyle.Bold, InkColor);
        summaryTitle.Margin = new Padding(0, 0, 0, 12);
        durationSummaryLabel.AutoSize = true;
        durationSummaryLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        durationSummaryLabel.ForeColor = PrimaryColor;
        durationSummaryLabel.Margin = new Padding(0, 0, 0, 4);
        scheduledForLabel.AutoSize = true;
        scheduledForLabel.Font = new Font("Segoe UI", 25F, FontStyle.Bold);
        scheduledForLabel.ForeColor = InkColor;
        scheduledForLabel.Margin = new Padding(0, 0, 0, 4);
        scheduledForLabel.AccessibleName = "Thời điểm tắt máy dự kiến";
        var previewHint = CreateLabel("Nếu xác nhận ngay", 9.5F, FontStyle.Regular, MutedColor);
        previewHint.Margin = new Padding(0, 0, 0, 20);
        var commandCaption = CreateLabel("Lệnh Windows", 9F, FontStyle.Bold, MutedColor);
        commandCaption.Margin = new Padding(0, 0, 0, 6);
        commandTextBox.ReadOnly = true;
        commandTextBox.BackColor = SoftSurfaceColor;
        commandTextBox.BorderStyle = BorderStyle.FixedSingle;
        commandTextBox.ForeColor = MutedColor;
        commandTextBox.Font = new Font(FontFamily.GenericMonospace, 9F);
        commandTextBox.Width = 320;
        commandTextBox.AccessibleName = "Lệnh shutdown sẽ chạy";
        commandTextBox.Margin = new Padding(0, 0, 0, 18);
        var note = CreateLabel("Bạn có thể hủy lịch bất kỳ lúc nào trước khi máy tắt.", 9.5F, FontStyle.Regular, MutedColor);
        note.MaximumSize = new Size(320, 0);

        var summary = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Top, Padding = new Padding(26, 24, 26, 22), BackColor = SurfaceColor };
        summary.Controls.Add(summaryTitle);
        summary.Controls.Add(durationSummaryLabel);
        summary.Controls.Add(scheduledForLabel);
        summary.Controls.Add(previewHint);
        summary.Controls.Add(commandCaption);
        summary.Controls.Add(commandTextBox);
        summary.Controls.Add(note);

        var main = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, Padding = new Padding(0), Margin = new Padding(0, 0, 0, 22) };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
        main.Controls.Add(durationBody, 0, 0);
        main.Controls.Add(summary, 1, 0);
        main.SetColumnSpan(durationBody, 1);

        scheduleStatusLabel.AutoSize = true;
        scheduleStatusLabel.MaximumSize = new Size(480, 0);
        scheduleStatusLabel.ForeColor = MutedColor;
        scheduleStatusLabel.Text = "Lịch chỉ được gửi tới Windows sau khi bạn xác nhận.";
        scheduleStatusLabel.Margin = new Padding(0, 0, 18, 0);
        scheduleStatusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;

        scheduleButton.Text = "&Lên lịch tắt máy";
        StyleButton(scheduleButton, PrimaryColor, PrimaryHoverColor, Color.White, new Padding(22, 11, 22, 11));
        scheduleButton.AccessibleDescription = "Mở xác nhận trước khi gửi lịch tắt máy tới Windows.";
        scheduleButton.Click += ScheduleButton_Click;
        schedulingCloseButton = new Button { Text = "Thoát ứng dụng (&T)", AutoSize = true };
        StyleButton(schedulingCloseButton, SurfaceColor, Color.FromArgb(228, 234, 243), InkColor, new Padding(18, 11, 18, 11));
        schedulingCloseButton.FlatAppearance.BorderSize = 1;
        schedulingCloseButton.FlatAppearance.BorderColor = LineColor;
        schedulingCloseButton.Click += (_, _) => Close();

        var actions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Dock = DockStyle.Fill };
        actions.Controls.Add(schedulingCloseButton);
        actions.Controls.Add(scheduleButton);
        var footer = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, Padding = new Padding(0, 18, 0, 0) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.Controls.Add(scheduleStatusLabel, 0, 0);
        footer.Controls.Add(actions, 1, 0);

        var page = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Width = schedulingPanel.ClientSize.Width - schedulingPanel.Padding.Horizontal };
        schedulingPanel.SizeChanged += (_, _) => page.Width = Math.Max(0, schedulingPanel.ClientSize.Width - schedulingPanel.Padding.Horizontal);
        page.Controls.Add(header);
        page.Controls.Add(main);
        page.Controls.Add(footer);
        schedulingPanel.Controls.Add(page);
    }

    private Button CreateDurationTile(DurationOption duration)
    {
        var tile = new Button
        {
            Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Tag = duration,
            Text = duration.Label, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 10, 0), Margin = new Padding(0, 0, 10, 10),
            AccessibleName = $"Chọn thời lượng {duration.Label}", AccessibleRole = AccessibleRole.RadioButton, UseVisualStyleBackColor = false
        };
        tile.FlatAppearance.BorderSize = 1;
        tile.Click += (_, _) => SelectDuration((DurationOption)tile.Tag!);
        durationTiles.Add(tile);
        return tile;
    }

    private void InitializeCountdownView()
    {
        countdownPanel.Dock = DockStyle.Fill;
        countdownPanel.BackColor = CountdownBackground;
        countdownPanel.Padding = new Padding(32);
        var status = CreateLabel("Lịch tắt máy đang hoạt động", 13F, FontStyle.Bold, Color.White);
        status.TextAlign = ContentAlignment.MiddleCenter;
        status.Dock = DockStyle.Fill;
        status.AccessibleName = "Lịch tắt máy đang hoạt động";
        countdownScheduledForLabel.AutoSize = true;
        countdownScheduledForLabel.Font = new Font("Segoe UI", 11F);
        countdownScheduledForLabel.ForeColor = CountdownMuted;
        countdownScheduledForLabel.TextAlign = ContentAlignment.MiddleCenter;
        countdownScheduledForLabel.Dock = DockStyle.Fill;
        countdownScheduledForLabel.Margin = new Padding(0, 8, 0, 8);

        countdownProgressRing.Dock = DockStyle.Fill;
        countdownProgressRing.BackColor = CountdownSurface;
        countdownProgressRing.Margin = new Padding(56, 12, 56, 12);
        countdownProgressRing.Padding = new Padding(20);
        countdownProgressRing.Controls.Add(countdownLabel);
        countdownLabel.Dock = DockStyle.Fill;
        countdownLabel.Font = new Font(FontFamily.GenericMonospace, 32F, FontStyle.Bold);
        countdownLabel.ForeColor = Color.White;
        countdownLabel.TextAlign = ContentAlignment.MiddleCenter;
        countdownLabel.AccessibleName = "Thời gian còn lại";
        countdownStatusLabel.AutoSize = true;
        countdownStatusLabel.ForeColor = CountdownMuted;
        countdownStatusLabel.Font = new Font("Segoe UI", 10F);
        countdownStatusLabel.TextAlign = ContentAlignment.MiddleCenter;
        countdownStatusLabel.Dock = DockStyle.Fill;
        countdownStatusLabel.MaximumSize = new Size(440, 0);
        countdownStatusLabel.Margin = new Padding(0, 6, 0, 16);

        cancelButton.Text = "Hủy lịch tắt máy";
        cancelButton.UseMnemonic = false;
        cancelButton.AutoSize = true;
        StyleButton(cancelButton, DangerColor, DangerHoverColor, Color.White, new Padding(22, 11, 22, 11));
        cancelButton.Click += CancelButton_Click;
        keepScheduleButton = new Button { Text = "Thoát ứng dụng — vẫn giữ lịch", AutoSize = true };
        StyleButton(keepScheduleButton, CountdownBackground, CountdownSurface, CountdownMuted, new Padding(16, 10, 16, 10));
        keepScheduleButton.Click += (_, _) => Close();

        var actions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Anchor = AnchorStyles.None, Padding = new Padding(0, 4, 0, 0) };
        actions.Controls.Add(cancelButton);
        actions.Controls.Add(keepScheduleButton);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(status, 0, 0);
        layout.Controls.Add(countdownScheduledForLabel, 0, 1);
        layout.Controls.Add(countdownProgressRing, 0, 2);
        layout.Controls.Add(countdownStatusLabel, 0, 3);
        layout.Controls.Add(actions, 0, 4);
        countdownPanel.Controls.Add(layout);
    }

    private static Label CreateLabel(string text, float size, FontStyle style, Color color) => new() { AutoSize = true, Font = new Font("Segoe UI", size, style), ForeColor = color, Text = text };

    private static void StyleButton(Button button, Color background, Color hoverBackground, Color foreground, Padding padding)
    {
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.BackColor = background;
        button.ForeColor = foreground;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = hoverBackground;
        button.FlatAppearance.MouseDownBackColor = hoverBackground;
        button.Padding = padding;
        button.Margin = new Padding(8, 0, 0, 0);
        button.Cursor = Cursors.Hand;
        button.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
    }

    private void ConfigureCustomDurationInput(NumericUpDown input, decimal maximum, string accessibleName)
    {
        input.Minimum = 0;
        input.Maximum = maximum;
        input.Width = 58;
        input.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
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
            var selected = ReferenceEquals(tile.Tag, selectedDuration);
            var label = ((DurationOption)tile.Tag!).Label;
            tile.BackColor = selected ? SoftSurfaceColor : SurfaceColor;
            tile.ForeColor = selected ? PrimaryColor : InkColor;
            tile.FlatAppearance.BorderColor = selected ? PrimaryColor : LineColor;
            tile.Text = selected ? $"✓  {label}" : label;
            tile.AccessibleDescription = selected ? $"{label}, đang được chọn." : $"{label}, chưa được chọn.";
            tile.Enabled = canEdit;
        }

        customHoursInput.Enabled = canEdit;
        customMinutesInput.Enabled = canEdit;
        customSecondsInput.Enabled = canEdit;
    }

    private void UpdatePreview()
    {
        if (activeSchedule is not null) return;
        if (!TryGetSelectedDuration(out var seconds, out var label))
        {
            scheduleButton.Enabled = false;
            commandTextBox.Text = string.Empty;
            durationSummaryLabel.Text = "Chưa chọn thời lượng";
            scheduledForLabel.Text = "Nhập thời lượng để xem giờ tắt";
            scheduledForLabel.AccessibleDescription = "Chưa có thời lượng tắt máy hợp lệ.";
            scheduleStatusLabel.ForeColor = MutedColor;
            scheduleStatusLabel.Text = "Chọn một mốc hoặc nhập thời lượng lớn hơn 0.";
            return;
        }

        scheduleButton.Enabled = true;
        var now = DateTimeOffset.Now;
        var scheduledFor = now.AddSeconds(seconds);
        var formattedScheduledFor = FormatScheduledFor(scheduledFor, now);
        commandTextBox.Text = $"shutdown -s -t {seconds}";
        durationSummaryLabel.Text = $"Sau {label}";
        scheduledForLabel.Text = formattedScheduledFor;
        scheduledForLabel.AccessibleDescription = $"Dự kiến tắt lúc {formattedScheduledFor} nếu xác nhận ngay.";
        if (scheduleStatusLabel.ForeColor != SuccessColor)
        {
            scheduleStatusLabel.ForeColor = MutedColor;
            scheduleStatusLabel.Text = "Lịch chỉ được gửi tới Windows sau khi bạn xác nhận.";
        }
    }

    private static string FormatScheduledFor(DateTimeOffset scheduledFor, DateTimeOffset? referenceTime = null) => scheduledFor.Date == (referenceTime ?? DateTimeOffset.Now).Date
        ? scheduledFor.ToString("HH':'mm':'ss' hôm nay'", VietnameseCulture)
        : scheduledFor.ToString("HH':'mm':'ss',' dd'/'MM'/'yyyy", VietnameseCulture);

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
        if (ShowChoiceDialog("Xác nhận lịch tắt máy", $"Máy sẽ tắt sau {label}, vào lúc {FormatScheduledFor(scheduledFor, now)}.\n\nBạn vẫn có thể hủy lịch trước thời điểm này.", "Lên lịch tắt máy", "Quay lại", PrimaryColor) != DialogResult.OK) return;

        SetBusy(true);
        var result = await shutdownService.ScheduleShutdownAsync(seconds);
        SetBusy(false);
        if (!result.Succeeded)
        {
            scheduleStatusLabel.ForeColor = DangerColor;
            scheduleStatusLabel.Text = $"Không thể lên lịch tắt máy. {result.ErrorMessage}";
            return;
        }

        activeSchedule = new ShutdownSchedule(seconds, scheduledFor, command);
        ShowCountdownView();
    }

    private async void CancelButton_Click(object? sender, EventArgs e)
    {
        if (ShowChoiceDialog("Hủy lịch tắt máy", "Lịch tắt máy đang chờ sẽ bị hủy khỏi Windows.", "Hủy lịch tắt máy", "Giữ lịch", DangerColor) == DialogResult.OK) await AbortScheduleAsync();
    }

    private async Task<bool> AbortScheduleAsync()
    {
        SetBusy(true);
        var result = await shutdownService.AbortShutdownAsync();
        SetBusy(false);
        if (!result.Succeeded)
        {
            countdownStatusLabel.ForeColor = Color.FromArgb(255, 190, 196);
            countdownStatusLabel.Text = $"Không thể hủy lịch. {result.ErrorMessage}";
            return false;
        }

        countdownTimer.Stop();
        activeSchedule = null;
        scheduleStatusLabel.ForeColor = SuccessColor;
        scheduleStatusLabel.Text = "Đã hủy lịch tắt máy.";
        ShowSchedulingView();
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
        countdownScheduledForLabel.Text = $"Máy sẽ tắt lúc {FormatScheduledFor(activeSchedule!.ScheduledFor)}";
        countdownStatusLabel.ForeColor = CountdownMuted;
        countdownStatusLabel.Text = "Bạn có thể hủy lịch trước khi thời gian kết thúc.";
        cancelButton.Text = "Hủy lịch tắt máy";
        cancelButton.Enabled = true;
        countdownTimer.Start();
        UpdateCountdown();
    }

    private void UpdateCountdown()
    {
        if (activeSchedule is null) return;
        var remaining = activeSchedule.ScheduledFor - DateTimeOffset.Now;
        if (remaining <= TimeSpan.Zero)
        {
            countdownTimer.Stop();
            countdownLabel.Text = "00:00:00";
            countdownLabel.AccessibleDescription = "Không còn thời gian.";
            countdownProgressRing.Progress = 0F;
            countdownStatusLabel.Text = "Thời gian đã kết thúc. Windows đang thực hiện tắt máy.";
            cancelButton.Enabled = false;
            return;
        }

        countdownLabel.Text = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        countdownLabel.AccessibleDescription = $"Còn lại {remaining.Hours} giờ, {remaining.Minutes} phút, {remaining.Seconds} giây.";
        countdownProgressRing.Progress = (float)Math.Clamp(remaining.TotalSeconds / activeSchedule.DelaySeconds, 0, 1);
    }

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        scheduleButton.Enabled = !busy && activeSchedule is null;
        cancelButton.Enabled = !busy && activeSchedule is not null;
        if (schedulingCloseButton is not null) schedulingCloseButton.Enabled = !busy;
        if (keepScheduleButton is not null) keepScheduleButton.Enabled = !busy;
        cancelButton.Text = busy && activeSchedule is not null ? "Đang hủy lịch…" : "&Hủy lịch tắt máy";
        scheduleButton.Text = busy && activeSchedule is null ? "Đang lên lịch…" : "&Lên lịch tắt máy";
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
        if (choice == DialogResult.No) return;
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

    private sealed record DurationOption(string Label, int Seconds);

    private sealed class CountdownProgressRing : Control
    {
        private float progress;
        public float Progress { get => progress; set { progress = Math.Clamp(value, 0F, 1F); Invalidate(); } }
        public CountdownProgressRing() { DoubleBuffered = true; MinimumSize = new Size(220, 220); AccessibleName = "Tiến trình thời gian tắt máy"; }
        protected override void OnResize(EventArgs e) { base.OnResize(e); Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var size = Math.Max(1, Math.Min(ClientSize.Width, ClientSize.Height) - 44);
            var bounds = new Rectangle((ClientSize.Width - size) / 2, (ClientSize.Height - size) / 2, size, size);
            using var track = new Pen(Color.FromArgb(55, 78, 114), 8F);
            using var value = new Pen(Color.FromArgb(103, 182, 255), 8F) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            e.Graphics.DrawArc(track, bounds, -90, 360);
            if (progress > 0) e.Graphics.DrawArc(value, bounds, -90, 360 * progress);
        }
    }

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
            ClientSize = new Size(450, 220);
            BackColor = SurfaceColor;
            Padding = new Padding(24);
            var heading = CreateLabel(title, 16F, FontStyle.Bold, InkColor); heading.Margin = new Padding(0, 0, 0, 10);
            var body = CreateLabel(message, 10F, FontStyle.Regular, MutedColor); body.MaximumSize = new Size(390, 0); body.Margin = new Padding(0, 0, 0, 22);
            var primary = new Button { Text = primaryText, DialogResult = DialogResult.OK };
            StyleButton(primary, primaryColor, primaryColor, Color.White, new Padding(16, 10, 16, 10));
            var cancel = new Button { Text = cancelText, DialogResult = DialogResult.Cancel };
            StyleButton(cancel, SurfaceColor, Color.FromArgb(236, 240, 246), InkColor, new Padding(16, 10, 16, 10)); cancel.FlatAppearance.BorderSize = 1; cancel.FlatAppearance.BorderColor = LineColor;
            var actions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom };
            actions.Controls.Add(cancel); actions.Controls.Add(primary);
            var content = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Fill };
            content.Controls.Add(heading); content.Controls.Add(body);
            Controls.Add(content); Controls.Add(actions);
            AcceptButton = null;
            CancelButton = cancel;
        }
    }

    private sealed class CloseScheduleDialog : Form
    {
        public CloseScheduleDialog()
        {
            Text = "Lịch tắt máy vẫn đang hoạt động";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(480, 245);
            BackColor = SurfaceColor;
            Padding = new Padding(24);
            var heading = CreateLabel(Text, 16F, FontStyle.Bold, InkColor); heading.Margin = new Padding(0, 0, 0, 10);
            var body = CreateLabel("Thoát ứng dụng không tự hủy lịch đã gửi tới Windows. Bạn muốn làm gì?", 10F, FontStyle.Regular, MutedColor); body.MaximumSize = new Size(420, 0);
            var abort = new Button { Text = "Hủy lịch rồi thoát", DialogResult = DialogResult.OK };
            StyleButton(abort, DangerColor, DangerHoverColor, Color.White, new Padding(16, 10, 16, 10));
            var keep = new Button { Text = "Thoát, vẫn giữ lịch", DialogResult = DialogResult.No };
            StyleButton(keep, SurfaceColor, Color.FromArgb(236, 240, 246), InkColor, new Padding(16, 10, 16, 10)); keep.FlatAppearance.BorderSize = 1; keep.FlatAppearance.BorderColor = LineColor;
            var back = new Button { Text = "Quay lại ứng dụng", DialogResult = DialogResult.Cancel };
            StyleButton(back, SurfaceColor, Color.FromArgb(236, 240, 246), MutedColor, new Padding(16, 10, 16, 10));
            var actions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Bottom };
            actions.Controls.Add(abort); actions.Controls.Add(keep); actions.Controls.Add(back);
            var content = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Fill };
            content.Controls.Add(heading); content.Controls.Add(body);
            Controls.Add(content); Controls.Add(actions);
            AcceptButton = null;
            CancelButton = back;
        }
    }
}
