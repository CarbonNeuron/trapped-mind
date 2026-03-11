using System.Globalization;

namespace TrappedMind;

public record SystemStats(
    double? CpuTemp,
    int? BatteryPercent,
    string? BatteryStatus,
    double? CpuUsage,
    double? RamUsedGb,
    double? RamTotalGb,
    double? UptimeSeconds);

public class SystemInfo
{
    private long _prevIdle;
    private long _prevTotal;
    private bool _hasPrev;

    public static double? ParseTemp(string content)
    {
        if (long.TryParse(content.Trim(), out var millideg))
            return millideg / 1000.0;
        return null;
    }

    public static int? ParseBatteryCapacity(string content)
    {
        if (int.TryParse(content.Trim(), out var cap))
            return cap;
        return null;
    }

    public static string? ParseBatteryStatus(string content)
    {
        var s = content.Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    public static double? ParseUptime(string content)
    {
        var parts = content.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1 && double.TryParse(parts[0], CultureInfo.InvariantCulture, out var sec))
            return sec;
        return null;
    }

    public static (double? UsedGb, double? TotalGb) ParseMemInfo(string content)
    {
        long? total = null, available = null;
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("MemTotal:"))
                total = ParseKb(trimmed);
            else if (trimmed.StartsWith("MemAvailable:"))
                available = ParseKb(trimmed);
        }
        if (total == null || available == null)
            return (null, null);

        var totalGb = total.Value / 1024.0 / 1024.0;
        var usedGb = (total.Value - available.Value) / 1024.0 / 1024.0;
        return (Math.Round(usedGb, 1), Math.Round(totalGb, 1));
    }

    private static long? ParseKb(string line)
    {
        var parts = line.Split(':', 2);
        if (parts.Length < 2) return null;
        var numStr = parts[1].Trim().Split(' ')[0];
        return long.TryParse(numStr, out var val) ? val : null;
    }

    public static (long Idle, long Total) ParseCpuStat(string content)
    {
        var line = content.Split('\n')[0];
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5)
            return (0, 0);

        long total = 0, idle = 0;
        for (int i = 1; i < parts.Length; i++)
        {
            if (long.TryParse(parts[i], out var val))
            {
                total += val;
                if (i == 4) idle = val;
            }
        }
        return (idle, total);
    }

    public static double CalculateCpuPercent(long prevIdle, long prevTotal, long currIdle, long currTotal)
    {
        var deltaTotal = currTotal - prevTotal;
        var deltaIdle = currIdle - prevIdle;
        if (deltaTotal == 0) return 0;
        return (1.0 - (double)deltaIdle / deltaTotal) * 100.0;
    }

    public static string FormatUptime(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.Days > 0)
            return $"{ts.Days}d {ts.Hours}h {ts.Minutes}m";
        return $"{ts.Hours}h {ts.Minutes}m";
    }

    private static string? ReadFileSafe(string path)
    {
        try { return File.ReadAllText(path); }
        catch { return null; }
    }

    public SystemStats Read()
    {
        var temp = ReadFileSafe("/sys/class/thermal/thermal_zone0/temp") is { } t ? ParseTemp(t) : null;
        var batCap = ReadFileSafe("/sys/class/power_supply/BAT0/capacity") is { } b ? ParseBatteryCapacity(b) : null;
        var batStatus = ReadFileSafe("/sys/class/power_supply/BAT0/status") is { } s ? ParseBatteryStatus(s) : null;
        var uptime = ReadFileSafe("/proc/uptime") is { } u ? ParseUptime(u) : null;
        var (ramUsed, ramTotal) = ReadFileSafe("/proc/meminfo") is { } m ? ParseMemInfo(m) : (null, null);

        double? cpuUsage = null;
        if (ReadFileSafe("/proc/stat") is { } statContent)
        {
            var (idle, total) = ParseCpuStat(statContent);
            if (_hasPrev)
                cpuUsage = Math.Round(CalculateCpuPercent(_prevIdle, _prevTotal, idle, total), 1);
            _prevIdle = idle;
            _prevTotal = total;
            _hasPrev = true;
        }

        return new SystemStats(temp, batCap, batStatus, cpuUsage, ramUsed, ramTotal, uptime);
    }
}
