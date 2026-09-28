using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

namespace AttendanceAdvisor;

public class MainForm : Form
{
    private readonly string[] subjects =
    {
        "Behavioural Psychology",
        "Wireless Communication and Antenna Systems",
        "Computer Communication and Network Security",
        "Semiconductor Memory Design",
        "Scripting Language for Electronic Design Automation",
        "Machine Learning for All"
    };

    private readonly string[] slots = { "A", "B", "C", "D", "E", "F" };
    private readonly Dictionary<string, int> attended = new();
    private readonly Dictionary<string, int> conducted = new();
    private readonly Dictionary<string, NumericUpDown> attendedInputs = new();
    private readonly Dictionary<string, NumericUpDown> conductedInputs = new();

    private Panel chartPanel = null!;
    private Label overallLabel = null!;
    private Label statusLabel = null!;
    private ComboBox leaveSubject = null!;
    private DateTimePicker leaveDate = null!;
    private NumericUpDown leaveDays = null!;
    private TextBox advisorInput = null!;
    private RichTextBox advisorOutput = null!;
    private ComboBox advisorSubject = null!;
    private Label simulationLabel = null!;

    private readonly Dictionary<DayOfWeek, string[]> timetable = new()
    {
        [DayOfWeek.Monday] = new[] { "C", "A", "E", "F" },
        [DayOfWeek.Tuesday] = new[] { "C", "E", "F", "B" },
        [DayOfWeek.Wednesday] = new[] { "C", "D", "A", "B" },
        [DayOfWeek.Thursday] = new[] { "D", "B", "LAB", "A" },
        [DayOfWeek.Friday] = new[] { "E", "D", "F" }
    };

    public MainForm()
    {
        Text = "Attendance Advisor — SRM IST Tiruchirappalli";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 700);
        Size = new Size(1400, 850);
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(245, 247, 251);

        foreach (var s in subjects) { attended[s] = 0; conducted[s] = 0; }
        BuildUi();
        UpdateDashboard();
    }

    private void BuildUi()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Color.FromArgb(24, 35, 65) };
        Controls.Add(header);
        var title = new Label { Text = "ATTENDANCE ADVISOR", ForeColor = Color.White, Font = new Font("Segoe UI", 21, FontStyle.Bold), AutoSize = true, Location = new Point(28, 13) };
        header.Controls.Add(title);
        var sub = new Label { Text = "SRM Institute of Science and Technology — Tiruchirappalli", ForeColor = Color.FromArgb(210, 220, 240), AutoSize = true, Location = new Point(31, 49) };
        header.Controls.Add(sub);

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(18, 6) };
        Controls.Add(tabs);
        tabs.BringToFront();

        tabs.TabPages.Add(BuildDashboardTab());
        tabs.TabPages.Add(BuildSubjectsTab());
        tabs.TabPages.Add(BuildTimetableTab());
        tabs.TabPages.Add(BuildLeaveTab());
        tabs.TabPages.Add(BuildAdvisorTab());
    }

    private TabPage BuildDashboardTab()
    {
        var page = new TabPage("Dashboard") { BackColor = Color.FromArgb(245, 247, 251) };
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 105, Padding = new Padding(20), WrapContents = false };
        page.Controls.Add(top);

        overallLabel = CardLabel(top, "Overall Attendance\n0.0%", Color.FromArgb(35, 105, 190));
        statusLabel = CardLabel(top, "Status\n—", Color.FromArgb(46, 130, 89));
        CardLabel(top, "Target\n90%", Color.FromArgb(110, 84, 170));
        CardLabel(top, "Minimum\n75%", Color.FromArgb(205, 126, 45));

        chartPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(20), BorderStyle = BorderStyle.FixedSingle };
        chartPanel.Paint += DrawChart;
        page.Controls.Add(chartPanel);

        var note = new Label { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(22, 10, 10, 5), Text = "Enter your conducted and attended classes in Subjects. The dashboard updates automatically.", ForeColor = Color.DimGray };
        page.Controls.Add(note);
        return page;
    }

    private Label CardLabel(FlowLayoutPanel parent, string text, Color accent)
    {
        var p = new Panel { Width = 245, Height = 72, BackColor = Color.White, Margin = new Padding(0, 0, 15, 0), BorderStyle = BorderStyle.FixedSingle };
        var bar = new Panel { Dock = DockStyle.Left, Width = 6, BackColor = accent };
        p.Controls.Add(bar);
        var l = new Label { Text = text, Dock = DockStyle.Fill, Padding = new Padding(15, 8, 5, 3), Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.FromArgb(35, 45, 65) };
        p.Controls.Add(l);
        parent.Controls.Add(p);
        return l;
    }

    private TabPage BuildSubjectsTab()
    {
        var page = new TabPage("Subjects") { BackColor = Color.FromArgb(245, 247, 251) };
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, Padding = new Padding(20), AutoScroll = true };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        page.Controls.Add(panel);
        string[] headers = { "Subject", "Attended", "Conducted", "Attendance", "Action" };
        for (int c = 0; c < headers.Length; c++) panel.Controls.Add(Header(headers[c]), c, 0);

        int r = 1;
        foreach (var s in subjects)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            panel.Controls.Add(new Label { Text = s, Dock = DockStyle.Fill, Padding = new Padding(6, 18, 4, 4) }, 0, r);
            var a = new NumericUpDown { Minimum = 0, Maximum = 999, Width = 85, Dock = DockStyle.None, Margin = new Padding(4, 15, 4, 4) };
            var c = new NumericUpDown { Minimum = 0, Maximum = 999, Width = 85, Dock = DockStyle.None, Margin = new Padding(4, 15, 4, 4) };
            attendedInputs[s] = a; conductedInputs[s] = c;
            a.ValueChanged += (_, _) => UpdateDashboard(); c.ValueChanged += (_, _) => UpdateDashboard();
            panel.Controls.Add(a, 1, r); panel.Controls.Add(c, 2, r);
            var pct = new Label { Name = "pct_" + s, Text = "0.0%", Dock = DockStyle.Fill, Padding = new Padding(4, 18, 4, 4), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            panel.Controls.Add(pct, 3, r);
            var btn = new Button { Text = "Set sample 8/10", AutoSize = true, Margin = new Padding(4, 12, 4, 4) };
            btn.Click += (_, _) => { a.Value = 8; c.Value = 10; UpdateDashboard(); };
            panel.Controls.Add(btn, 4, r);
            r++;
        }
        return page;
    }

    private Label Header(string text) => new() { Text = text, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(44, 62, 96), Padding = new Padding(6, 8, 4, 4) };

    private TabPage BuildTimetableTab()
    {
        var page = new TabPage("Timetable") { BackColor = Color.FromArgb(245, 247, 251) };
        var title = new Label { Text = "IV ECE-B — VII Semester — Old Semester 2026–27", Dock = DockStyle.Top, Height = 55, Font = new Font("Segoe UI", 15, FontStyle.Bold), Padding = new Padding(22, 16, 0, 0) };
        page.Controls.Add(title);
        var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle, RowHeadersVisible = false };
        grid.Columns.Add("Day", "Day");
        grid.Columns.Add("P1", "09:00–09:50"); grid.Columns.Add("P2", "09:50–10:40"); grid.Columns.Add("P3", "10:50–11:40"); grid.Columns.Add("P4", "11:40–12:30"); grid.Columns.Add("P5", "12:30–01:20");
        grid.Columns.Add("P6", "01:20–02:10"); grid.Columns.Add("P7", "02:10–03:00"); grid.Columns.Add("P8", "03:10–04:00"); grid.Columns.Add("P9", "04:00–04:50");
        var rows = new[]
        {
            new[]{"Monday","C","A","E","F","LUNCH","—","—","—","—"},
            new[]{"Tuesday","C","E","F","B","LUNCH","—","—","—","—"},
            new[]{"Wednesday","C","D","A","B","LUNCH","—","—","—","—"},
            new[]{"Thursday","D","B","LAB","A","LUNCH","—","—","—","—"},
            new[]{"Friday","E","D","F","—","LUNCH","—","—","—","—"}
        };
        foreach (var row in rows) grid.Rows.Add(row);
        page.Controls.Add(grid);

        var legend = new Label { Dock = DockStyle.Bottom, Height = 95, Padding = new Padding(22, 8, 8, 8), Text = "Slot mapping:\nA — Behavioural Psychology     B — Wireless Communication and Antenna Systems\nC — Computer Communication and Network Security     D — Semiconductor Memory Design\nE — Scripting Language for Electronic Design Automation     F — Machine Learning for All     LAB — Computer Communication and Network Security Lab", ForeColor = Color.DimGray };
        page.Controls.Add(legend);
        return page;
    }

    private TabPage BuildLeaveTab()
    {
        var page = new TabPage("OD / Medical Leave") { BackColor = Color.FromArgb(245, 247, 251) };
        var box = new GroupBox { Text = "Leave Simulator", Location = new Point(30, 30), Size = new Size(650, 330), Font = new Font("Segoe UI", 11, FontStyle.Bold) };
        page.Controls.Add(box);
        box.Controls.Add(MakeLabel("Subject", 30, 45));
        leaveSubject = new ComboBox { Location = new Point(190, 40), Width = 400, DropDownStyle = ComboBoxStyle.DropDownList };
        leaveSubject.Items.Add("All subjects"); foreach (var s in subjects) leaveSubject.Items.Add(s); leaveSubject.SelectedIndex = 0; box.Controls.Add(leaveSubject);
        box.Controls.Add(MakeLabel("Start date", 30, 95));
        leaveDate = new DateTimePicker { Location = new Point(190, 90), Width = 180 }; box.Controls.Add(leaveDate);
        box.Controls.Add(MakeLabel("Leave days", 30, 145));
        leaveDays = new NumericUpDown { Location = new Point(190, 140), Minimum = 1, Maximum = 30, Value = 1 }; box.Controls.Add(leaveDays);
        var b = new Button { Text = "Simulate Leave", Location = new Point(190, 195), Width = 180, Height = 38 }; b.Click += (_, _) => SimulateLeave(); box.Controls.Add(b);
        simulationLabel = new Label { Text = "Simulation result will appear here.", Location = new Point(30, 245), Size = new Size(570, 60), Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(40, 70, 100) }; box.Controls.Add(simulationLabel);
        var info = new Label { Text = "OD/Medical leave is treated as an approved absence in this simulator: it is shown separately and does not add an attended class. Verify your institution's actual attendance policy before relying on a projection.", Location = new Point(30, 390), Size = new Size(900, 70), ForeColor = Color.DimGray };
        page.Controls.Add(info);
        return page;
    }

    private Label MakeLabel(string text, int x, int y) => new() { Text = text, Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };

    private void SimulateLeave()
    {
        int days = (int)leaveDays.Value;
        var chosen = leaveSubject.SelectedItem?.ToString() ?? "All subjects";
        var affected = chosen == "All subjects" ? subjects : new[] { chosen };
        double sumA = 0, sumC = 0;
        foreach (var s in subjects)
        {
            int extra = affected.Contains(s) ? EstimateClassesOnDays(s, leaveDate.Value.Date, days) : 0;
            sumA += attended[s]; sumC += conducted[s] + extra;
        }
        double pct = sumC == 0 ? 0 : sumA / sumC * 100;
        simulationLabel.Text = $"Projected overall attendance: {pct:0.0}%\nAffected subject(s): {chosen}. Estimated classes during leave: {affected.Sum(s => EstimateClassesOnDays(s, leaveDate.Value.Date, days))}.";
        simulationLabel.ForeColor = pct < 75 ? Color.Firebrick : pct < 90 ? Color.DarkOrange : Color.DarkGreen;
    }

    private int EstimateClassesOnDays(string subject, DateTime start, int days)
    {
        string slot = slots[Array.IndexOf(subjects, subject)];
        int count = 0;
        for (int i = 0; i < days; i++)
        {
            var d = start.AddDays(i).DayOfWeek;
            if (d == DayOfWeek.Saturday || d == DayOfWeek.Sunday) continue;
            if (timetable.TryGetValue(d, out var entries) && entries.Contains(slot)) count++;
        }
        return count;
    }

    private TabPage BuildAdvisorTab()
    {
        var page = new TabPage("Attendance Advisor") { BackColor = Color.FromArgb(245, 247, 251) };
        var top = new Label { Text = "Ask about your current attendance, missed classes, or a leave plan.", Dock = DockStyle.Top, Height = 50, Padding = new Padding(22, 15, 10, 5), Font = new Font("Segoe UI", 12, FontStyle.Bold) };
        page.Controls.Add(top);
        advisorOutput = new RichTextBox { ReadOnly = true, Dock = DockStyle.Fill, BackColor = Color.White, Font = new Font("Segoe UI", 10), BorderStyle = BorderStyle.FixedSingle };
        page.Controls.Add(advisorOutput);
        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 105, Padding = new Padding(18) };
        page.Controls.Add(bottom);
        advisorSubject = new ComboBox { Location = new Point(18, 15), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
        advisorSubject.Items.Add("Overall"); foreach (var s in subjects) advisorSubject.Items.Add(s); advisorSubject.SelectedIndex = 0; bottom.Controls.Add(advisorSubject);
        advisorInput = new TextBox { Location = new Point(295, 15), Width = 700, Height = 35, Anchor = AnchorStyles.Left | AnchorStyles.Right, PlaceholderText = "Example: If I miss 3 Chemistry classes, will I go below 75%?" }; bottom.Controls.Add(advisorInput);
        var ask = new Button { Text = "Ask Advisor", Location = new Point(1010, 13), Width = 120, Height = 34, Anchor = AnchorStyles.Top | AnchorStyles.Right }; ask.Click += (_, _) => AnswerAdvisor(); bottom.Controls.Add(ask);
        var quick = new Button { Text = "Refresh", Location = new Point(18, 57), Width = 100 }; quick.Click += (_, _) => { UpdateDashboard(); advisorOutput.AppendText("Advisor: Dashboard refreshed.\n\n"); }; bottom.Controls.Add(quick);
        advisorOutput.Text = "Advisor: Hello! I can calculate attendance using the numbers currently entered in the Subjects tab.\n\n";
        return page;
    }

    private void AnswerAdvisor()
    {
        string q = advisorInput.Text.Trim();
        if (q.Length == 0) return;
        string chosen = advisorSubject.SelectedItem?.ToString() ?? "Overall";
        string targetSubject = chosen == "Overall" ? FindMentionedSubject(q) ?? "" : chosen;
        int leave = ExtractLeaveCount(q);
        string answer;
        if (targetSubject.Length > 0)
        {
            int a = attended[targetSubject], c = conducted[targetSubject];
            int futureClasses = Math.Max(0, leave);
            double now = c == 0 ? 0 : 100.0 * a / c;
            double projected = c + futureClasses == 0 ? 0 : 100.0 * a / (c + futureClasses);
            int maxMiss = MaxCanMiss(a, c, 75);
            answer = $"{targetSubject}\nCurrent: {now:0.0}% ({a}/{c})\n" + (leave > 0 ? $"After {leave} missed class(es): {projected:0.0}%\n" : "") + $"At the current record, you can miss about {maxMiss} more class(es) and remain at or above 75%.";
            if (leave > 0 && projected < 75) answer += "\nWARNING: this projection falls below 75%.";
            else if (leave > 0 && projected < 90) answer += "\nNotice: this projection is below the 90% target.";
            else if (leave > 0) answer += "\nThis projection remains at or above the 90% target.";
        }
        else
        {
            var (a, c) = Overall();
            double now = c == 0 ? 0 : 100.0 * a / c;
            answer = $"Overall attendance: {now:0.0}% ({a}/{c}).\n75% minimum: {(now >= 75 ? "currently met" : "currently not met")}.\n90% target: {(now >= 90 ? "currently met" : "currently not met")}.";
        }
        advisorOutput.AppendText($"You: {q}\nAdvisor: {answer}\n\n");
        advisorInput.Clear();
    }

    private string? FindMentionedSubject(string q)
    {
        var lower = q.ToLowerInvariant();
        foreach (var s in subjects) if (lower.Contains(s.ToLowerInvariant())) return s;
        if (lower.Contains("psychology")) return subjects[0];
        if (lower.Contains("wireless") || lower.Contains("antenna")) return subjects[1];
        if (lower.Contains("network") || lower.Contains("computer communication")) return subjects[2];
        if (lower.Contains("memory")) return subjects[3];
        if (lower.Contains("scripting") || lower.Contains("eda")) return subjects[4];
        if (lower.Contains("machine learning") || lower.Contains("ml")) return subjects[5];
        return null;
    }

    private int ExtractLeaveCount(string q)
    {
        var m = Regex.Match(q.ToLowerInvariant(), @"(\d+)\s*(?:-?day|days|classes|class)");
        return m.Success && int.TryParse(m.Groups[1].Value, out var n) ? n : 0;
    }

    private int MaxCanMiss(int a, int c, int min)
    {
        if (c <= 0) return 0;
        int n = 0; while (100.0 * a / (c + n + 1) >= min) n++; return n;
    }

    private (int a, int c) Overall()
    {
        return (attended.Values.Sum(), conducted.Values.Sum());
    }

    private void UpdateDashboard()
    {
        foreach (var s in subjects)
        {
            attended[s] = (int)(attendedInputs.TryGetValue(s, out var a) ? a.Value : attended[s]);
            conducted[s] = (int)(conductedInputs.TryGetValue(s, out var c) ? c.Value : conducted[s]);
            if (attendedInputs.ContainsKey(s) && attendedInputs[s].Value > conductedInputs[s].Value) attendedInputs[s].Value = conductedInputs[s].Value;
        }
        var (aTotal, cTotal) = Overall();
        double overall = cTotal == 0 ? 0 : 100.0 * aTotal / cTotal;
        overallLabel.Text = $"Overall Attendance\n{overall:0.0}%";
        statusLabel.Text = "Status\n" + (cTotal == 0 ? "Enter data" : overall < 75 ? "Below 75%" : overall < 90 ? "Below 90% target" : "Target met");
        statusLabel.ForeColor = overall < 75 ? Color.Firebrick : overall < 90 ? Color.DarkOrange : Color.DarkGreen;
        chartPanel.Invalidate();
        foreach (var s in subjects)
        {
            var pct = conducted[s] == 0 ? 0 : 100.0 * attended[s] / conducted[s];
            var label = Controls.Find("pct_" + s, true).FirstOrDefault() as Label;
            if (label != null) label.Text = $"{pct:0.0}%";
            if (label != null) label.ForeColor = pct < 75 ? Color.Firebrick : pct < 90 ? Color.DarkOrange : Color.DarkGreen;
        }
    }

    private void DrawChart(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = chartPanel.ClientRectangle; int left = 65, bottom = 55, top = 45, right = 30;
        using var gridPen = new Pen(Color.FromArgb(225, 230, 238));
        using var textBrush = new SolidBrush(Color.FromArgb(55, 65, 80));
        using var barBrush = new SolidBrush(Color.FromArgb(52, 115, 210));
        using var warnBrush = new SolidBrush(Color.FromArgb(220, 150, 55));
        using var dangerBrush = new SolidBrush(Color.FromArgb(205, 75, 75));
        g.DrawString("Subject Attendance Health", new Font("Segoe UI", 14, FontStyle.Bold), textBrush, left, 15);
        int w = Math.Max(100, rect.Width - left - right), h = Math.Max(100, rect.Height - top - bottom);
        for (int p = 0; p <= 100; p += 25)
        {
            int y = top + h - (int)(h * p / 100.0); g.DrawLine(gridPen, left, y, left + w, y); g.DrawString(p + "%", new Font("Segoe UI", 8), textBrush, 18, y - 8);
        }
        int n = subjects.Length, gap = 18, bw = Math.Max(30, (w - gap * (n + 1)) / n);
        for (int i = 0; i < n; i++)
        {
            var s = subjects[i]; double pct = conducted[s] == 0 ? 0 : 100.0 * attended[s] / conducted[s];
            int bh = (int)(h * Math.Min(100, pct) / 100.0); int x = left + gap + i * (bw + gap); int y = top + h - bh;
            var brush = pct < 75 ? dangerBrush : pct < 90 ? warnBrush : barBrush;
            g.FillRectangle(brush, x, y, bw, bh);
            g.DrawString($"{pct:0}%", new Font("Segoe UI", 8, FontStyle.Bold), textBrush, x + 2, Math.Max(top, y - 20));
            string label = slots[i]; g.DrawString(label, new Font("Segoe UI", 10, FontStyle.Bold), textBrush, x + bw / 2 - 5, top + h + 8);
        }
        using var targetPen = new Pen(Color.FromArgb(100, 90, 170), 2); targetPen.DashStyle = DashStyle.Dash;
        int y90 = top + h - (int)(h * .9); g.DrawLine(targetPen, left, y90, left + w, y90); g.DrawString("90% target", new Font("Segoe UI", 8), textBrush, left + w - 75, y90 - 18);
        using var minPen = new Pen(Color.FromArgb(180, 90, 60), 2); minPen.DashStyle = DashStyle.Dash;
        int y75 = top + h - (int)(h * .75); g.DrawLine(minPen, left, y75, left + w, y75); g.DrawString("75% minimum", new Font("Segoe UI", 8), textBrush, left + w - 90, y75 + 3);
    }
}
