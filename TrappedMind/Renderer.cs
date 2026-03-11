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
        var effectiveWidth = Math.Min(panelWidth, termWidth - 4);

        // Thought panel
        var thoughtPanel = new Panel(new Markup(Markup.Escape(thoughtText)))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(panelColor),
            Header = new PanelHeader($" {panelHeader} ", Justify.Center),
            Width = effectiveWidth,
            Padding = new Padding(2, 1),
        };

        // Pet markup
        var petText = string.Join("\n", currentPetFrame.Select(
            line => $"[{petColor.ToMarkup()}]{Markup.Escape(line)}[/]"));

        // Stats bar
        var statsPanel = new Panel(new Markup(BuildStatsMarkup(stats)))
        {
            Border = BoxBorder.Heavy,
            BorderStyle = new Style(Color.Grey),
            Padding = new Padding(1, 0),
        };

        // Full-screen layout using Spectre Layout
        var layout = new Layout("root")
            .SplitRows(
                new Layout("main"),
                new Layout("stats").Size(3));

        // Main area: centered thought + pet on the right
        var mainContent = new Rows(
            new Text(""),
            new Padder(thoughtPanel, new Padding((termWidth - effectiveWidth) / 2, 0)),
            new Text(""),
            new Padder(new Markup(petText), new Padding(termWidth - 20, 0, 0, 0)));

        layout["main"].Update(mainContent).Ratio(1);
        layout["stats"].Update(statsPanel);

        // Move cursor home and render the full layout
        Console.Write("\x1b[H");
        AnsiConsole.Write(layout);
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
