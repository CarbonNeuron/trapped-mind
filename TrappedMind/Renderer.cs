using Terminal.Gui;

namespace TrappedMind;

public class Renderer
{
    private readonly FrameView _chatFrame;
    private readonly ChatView _chatView;
    private readonly TextField _inputField;
    private readonly FrameView _petFrame;
    private readonly Label _petLabel;
    private readonly FrameView _statsFrame;
    private readonly TableView _statsTable;
    private int _petFrameIndex;
    private Action<string>? _onUserInput;

    public Renderer()
    {
        // Left side: chat + input
        _chatFrame = new FrameView
        {
            Title = "Trapped Mind",
            X = 0,
            Y = 0,
            Width = Dim.Percent(75),
            Height = Dim.Fill(),
        };

        _chatView = new ChatView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
        };

        var inputPrompt = new Label
        {
            Text = "> ",
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = 2,
            ColorScheme = new ColorScheme
            {
                Normal = new Terminal.Gui.Attribute(Color.Green, Color.Black),
            },
        };

        _inputField = new TextField
        {
            X = 2,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Text = "",
        };
        _inputField.Accepting += (s, e) =>
        {
            var text = _inputField.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(text))
            {
                _onUserInput?.Invoke(text);
                _inputField.Text = "";
            }
            e.Cancel = true;
        };

        _chatFrame.Add(_chatView, inputPrompt, _inputField);

        // Right top: pet
        _petFrame = new FrameView
        {
            Title = "Pet",
            X = Pos.Percent(75),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(50),
        };

        _petLabel = new Label
        {
            X = Pos.Center(),
            Y = Pos.Center(),
            Text = "",
        };
        _petFrame.Add(_petLabel);

        // Right bottom: stats
        _statsFrame = new FrameView
        {
            Title = "Stats",
            X = Pos.Percent(75),
            Y = Pos.Percent(50),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        _statsTable = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true,
        };
        var dt = new System.Data.DataTable();
        dt.Columns.Add("Metric");
        dt.Columns.Add("Value");
        _statsTable.Table = new DataTableSource(dt);
        _statsFrame.Add(_statsTable);
    }

    public View[] GetViews() => new View[] { _chatFrame, _petFrame, _statsFrame };

    public void OnUserInput(Action<string> handler) => _onUserInput = handler;

    public void FocusInput() => _inputField.SetFocus();

    public void AddChatMessage(ChatMessage message) => _chatView.AddMessage(message);

    public void BeginStreamingMessage() => _chatView.BeginStreaming();

    public void UpdateStreamingMessage(string partialText) => _chatView.UpdateStreaming(partialText);

    public void AdvancePetFrame() => _petFrameIndex++;

    public void UpdatePet(PetMood mood)
    {
        var (frames, color) = Pet.GetFrames(mood);
        var currentFrame = frames[_petFrameIndex % frames.Length];
        _petLabel.Text = string.Join("\n", currentFrame);
        _petLabel.ColorScheme = new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(color, Color.Black),
        };
    }

    public void UpdateStats(SystemStats stats)
    {
        var dt = new System.Data.DataTable();
        dt.Columns.Add("Metric");
        dt.Columns.Add("Value");

        dt.Rows.Add("TEMP", stats.CpuTemp.HasValue ? $"{stats.CpuTemp:F0}C" : "?");
        dt.Rows.Add("CPU", stats.CpuUsage.HasValue ? $"{stats.CpuUsage:F0}%" : "?");
        dt.Rows.Add("RAM", stats.RamUsedGb.HasValue ? $"{stats.RamUsedGb}G/{stats.RamTotalGb}G" : "?");
        dt.Rows.Add("BAT", stats.BatteryPercent.HasValue
            ? $"{stats.BatteryPercent}% {stats.BatteryStatus ?? "?"}"
            : "?");
        dt.Rows.Add("UP", stats.UptimeSeconds.HasValue
            ? SystemInfo.FormatUptime(stats.UptimeSeconds.Value)
            : "?");
        dt.Rows.Add("FAN", stats.FanSpeedRpm.HasValue ? $"{stats.FanSpeedRpm} RPM" : "?");

        if (stats.IpAddresses != null)
        {
            foreach (var kv in stats.IpAddresses)
                dt.Rows.Add(kv.Key, kv.Value);
        }

        _statsTable.Table = new DataTableSource(dt);
    }

    public void UpdatePanelBorder(SystemStats stats)
    {
        var color = MoodEngine.GetPanelColor(stats);
        _chatFrame.ColorScheme = new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(color, Color.Black),
        };
    }
}
