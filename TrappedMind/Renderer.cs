using Spectre.Console;

namespace TrappedMind;

public class Renderer
{
    private int _petFrame;
    private readonly Layout _layout;

    public Renderer()
    {
        _layout = new Layout("root")
            .SplitRows(
                new Layout("thought").Ratio(1),
                new Layout("pet").Size(4),
                new Layout("stats").Size(3));
    }

    public Layout Layout => _layout;

    public void AdvancePetFrame() => _petFrame++;

    public void Update(
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

        // Thought panel — centered
        var thoughtPanel = new Panel(new Markup(Markup.Escape(thoughtText)))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(panelColor),
            Header = new PanelHeader($" {panelHeader} ", Justify.Center),
            Width = effectiveWidth,
            Padding = new Padding(2, 1),
        };

        // Pet — right-aligned above stats
        var petText = string.Join("\n", currentPetFrame.Select(
            line => $"[{petColor.ToMarkup()}]{Markup.Escape(line)}[/]"));

        // Stats bar — full width, evenly spaced columns
        var statsTable = BuildStatsTable(stats, termWidth);

        var statsPanel = new Panel(statsTable)
        {
            Border = BoxBorder.Heavy,
            BorderStyle = new Style(Color.Grey),
            Padding = new Padding(0, 0),
            Expand = true,
        };

        _layout["thought"].Update(
            new Padder(thoughtPanel, new Padding((termWidth - effectiveWidth) / 2, 1, 0, 0)));
        _layout["pet"].Update(
            new Padder(new Markup(petText), new Padding(Math.Max(0, termWidth - 16), 0, 0, 0)));
        _layout["stats"].Update(statsPanel);
    }

    private static Table BuildStatsTable(SystemStats stats, int termWidth)
    {
        var table = new Table { Border = TableBorder.None, Expand = true };
        table.AddColumn(new TableColumn("") { Alignment = Justify.Center });
        table.AddColumn(new TableColumn("") { Alignment = Justify.Center });
        table.AddColumn(new TableColumn("") { Alignment = Justify.Center });
        table.AddColumn(new TableColumn("") { Alignment = Justify.Center });
        table.AddColumn(new TableColumn("") { Alignment = Justify.Center });

        string temp, bat, cpu, ram, up;

        if (stats.CpuTemp.HasValue)
        {
            var c = MoodEngine.GetTempColor(stats.CpuTemp.Value).ToMarkup();
            temp = $"[{c}]TEMP {stats.CpuTemp:F0}C[/]";
        }
        else temp = "[grey]TEMP ?[/]";

        if (stats.BatteryPercent.HasValue)
        {
            var c = MoodEngine.GetBatteryColor(stats.BatteryPercent.Value).ToMarkup();
            bat = $"[{c}]BAT {stats.BatteryPercent}% {stats.BatteryStatus ?? "?"}[/]";
        }
        else bat = "[grey]BAT ?[/]";

        if (stats.CpuUsage.HasValue)
        {
            var c = MoodEngine.GetCpuColor(stats.CpuUsage.Value).ToMarkup();
            cpu = $"[{c}]CPU {stats.CpuUsage:F0}%[/]";
        }
        else cpu = "[grey]CPU ?[/]";

        ram = stats.RamUsedGb.HasValue
            ? $"[cyan]RAM {stats.RamUsedGb}G/{stats.RamTotalGb}G[/]"
            : "[grey]RAM ?[/]";

        up = stats.UptimeSeconds.HasValue
            ? $"[fuchsia]UP {SystemInfo.FormatUptime(stats.UptimeSeconds.Value)}[/]"
            : "[grey]UP ?[/]";

        table.AddRow(new Markup(temp), new Markup(bat), new Markup(cpu), new Markup(ram), new Markup(up));
        return table;
    }
}
