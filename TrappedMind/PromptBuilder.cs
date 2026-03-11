namespace TrappedMind;

public static class PromptBuilder
{
    public static string Build(SystemStats stats, List<string> recentThoughts)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Current date and time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"CPU temperature: {(stats.CpuTemp.HasValue ? $"{stats.CpuTemp:F0}C" : "?")}");
        sb.AppendLine($"CPU usage: {(stats.CpuUsage.HasValue ? $"{stats.CpuUsage:F0}%" : "?")}");
        sb.AppendLine($"Battery: {(stats.BatteryPercent.HasValue ? $"{stats.BatteryPercent}% ({stats.BatteryStatus ?? "?"})" : "?")}");
        sb.AppendLine($"RAM: {(stats.RamUsedGb.HasValue ? $"{stats.RamUsedGb}G/{stats.RamTotalGb}G" : "?")}");
        sb.AppendLine($"Uptime: {(stats.UptimeSeconds.HasValue ? SystemInfo.FormatUptime(stats.UptimeSeconds.Value) : "?")}");

        if (recentThoughts.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Your previous thoughts:");
            foreach (var thought in recentThoughts)
                sb.AppendLine($"- {thought}");
        }

        sb.AppendLine();
        sb.AppendLine("Express a new thought. One or two sentences. React to your conditions.");

        return sb.ToString();
    }
}
