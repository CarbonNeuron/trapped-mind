using Spectre.Console;
using Spectre.Console.Rendering;

namespace TrappedMind;

public class Renderer
{
    private int _petFrame;

    public void AdvancePetFrame() => _petFrame++;

    public void Render(
        string thoughtText,
        string panelHeader,
        SystemStats stats,
        PetMood petMood,
        int panelWidth)
    {
        var panelColor = MoodEngine.GetPanelColor(stats);
        var (petFrames, petColor) = Pet.GetFrames(petMood);
        var currentPetFrame = petFrames[_petFrame % petFrames.Length];

        var termWidth = AnsiConsole.Profile.Width;
        var termHeight = AnsiConsole.Profile.Height;
        var effectiveWidth = Math.Min(panelWidth, termWidth - 4);

        // Build thought panel
        var thoughtPanel = new Panel(new Markup(Markup.Escape(thoughtText)))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(panelColor),
            Header = new PanelHeader($" {panelHeader} ", Justify.Center),
            Width = effectiveWidth,
            Padding = new Padding(2, 1),
        };

        // Build pet
        var petText = string.Join("\n", currentPetFrame.Select(
            line => $"[{petColor.ToMarkup()}]{Markup.Escape(line)}[/]"));

        // Build stats bar
        var statsMarkup = BuildStatsMarkup(stats);
        var statsPanel = new Panel(new Markup(statsMarkup))
        {
            Border = BoxBorder.Heavy,
            BorderStyle = new Style(Color.Grey),
            Padding = new Padding(1, 0),
        };

        // Calculate top padding to vertically center
        var contentHeight = 8 + currentPetFrame.Length + 5;
        var topPad = Math.Max(1, (termHeight - contentHeight) / 3);

        AnsiConsole.Cursor.SetPosition(0, 0);

        var rows = new List<IRenderable>();
        for (int i = 0; i < topPad; i++)
            rows.Add(new Text(new string(' ', termWidth)));

        rows.Add(new Padder(thoughtPanel, new Padding((termWidth - effectiveWidth) / 2, 0)));
        rows.Add(new Text(""));
        rows.Add(new Padder(new Markup(petText), new Padding(termWidth - 20, 0, 0, 0)));
        rows.Add(new Text(""));
        rows.Add(statsPanel);

        AnsiConsole.Write(new Rows(rows));

        // Fill remaining with blanks
        var rendered = topPad + contentHeight;
        for (var i = rendered; i < termHeight; i++)
            AnsiConsole.Write(new Text(new string(' ', termWidth)));
    }

    private static string BuildStatsMarkup(SystemStats stats)
    {
        var parts = new List<string>();

        if (stats.CpuTemp.HasValue)
        {
            var c = MoodEngine.GetTempColor(stats.CpuTemp.Value).ToMarkup();
            parts.Add($"[{c}]TEMP {stats.CpuTemp:F0}C[/]");
        }
        else parts.Add("[grey]TEMP ?[/]");

        if (stats.BatteryPercent.HasValue)
        {
            var c = MoodEngine.GetBatteryColor(stats.BatteryPercent.Value).ToMarkup();
            parts.Add($"[{c}]BAT {stats.BatteryPercent}% {stats.BatteryStatus ?? "?"}[/]");
        }
        else parts.Add("[grey]BAT ?[/]");

        if (stats.CpuUsage.HasValue)
        {
            var c = MoodEngine.GetCpuColor(stats.CpuUsage.Value).ToMarkup();
            parts.Add($"[{c}]CPU {stats.CpuUsage:F0}%[/]");
        }
        else parts.Add("[grey]CPU ?[/]");

        if (stats.RamUsedGb.HasValue)
            parts.Add($"[cyan]RAM {stats.RamUsedGb}G/{stats.RamTotalGb}G[/]");
        else parts.Add("[grey]RAM ?[/]");

        if (stats.UptimeSeconds.HasValue)
            parts.Add($"[fuchsia]UP {SystemInfo.FormatUptime(stats.UptimeSeconds.Value)}[/]");
        else parts.Add("[grey]UP ?[/]");

        return string.Join(" | ", parts);
    }
}
