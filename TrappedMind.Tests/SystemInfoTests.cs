namespace TrappedMind.Tests;

public class SystemInfoTests
{
    [Fact]
    public void ParseTemp_ValidMillidegrees_ReturnsDegrees()
    {
        Assert.Equal(58.0, SystemInfo.ParseTemp("58000\n"));
    }

    [Fact]
    public void ParseTemp_InvalidContent_ReturnsNull()
    {
        Assert.Null(SystemInfo.ParseTemp("not a number"));
        Assert.Null(SystemInfo.ParseTemp(""));
    }

    [Fact]
    public void ParseBatteryCapacity_Valid_ReturnsInt()
    {
        Assert.Equal(72, SystemInfo.ParseBatteryCapacity("72\n"));
    }

    [Fact]
    public void ParseBatteryCapacity_Invalid_ReturnsNull()
    {
        Assert.Null(SystemInfo.ParseBatteryCapacity(""));
    }

    [Fact]
    public void ParseBatteryStatus_Valid_ReturnsString()
    {
        Assert.Equal("Discharging", SystemInfo.ParseBatteryStatus("Discharging\n"));
    }

    [Fact]
    public void ParseUptime_Valid_ReturnsSeconds()
    {
        Assert.Equal(12345.67, SystemInfo.ParseUptime("12345.67 23456.78\n"));
    }

    [Fact]
    public void ParseUptime_Invalid_ReturnsNull()
    {
        Assert.Null(SystemInfo.ParseUptime(""));
    }

    [Fact]
    public void ParseMemInfo_Valid_ReturnsUsedAndTotal()
    {
        var content = """
            MemTotal:        7864320 kB
            MemFree:         1234567 kB
            MemAvailable:    3456789 kB
            Buffers:          123456 kB
            """;
        var (usedGb, totalGb) = SystemInfo.ParseMemInfo(content);
        Assert.NotNull(usedGb);
        Assert.NotNull(totalGb);
        Assert.InRange(totalGb!.Value, 7.4, 7.6);
        Assert.InRange(usedGb!.Value, 4.1, 4.3);
    }

    [Fact]
    public void ParseMemInfo_Invalid_ReturnsNulls()
    {
        var (used, total) = SystemInfo.ParseMemInfo("");
        Assert.Null(used);
        Assert.Null(total);
    }

    [Fact]
    public void CalculateCpuUsage_TwoSnapshots_ReturnsPercentage()
    {
        var snap1 = "cpu  100 0 50 850 0 0 0 0 0 0\n";
        var snap2 = "cpu  200 0 100 1700 0 0 0 0 0 0\n";

        var (prevIdle, prevTotal) = SystemInfo.ParseCpuStat(snap1);
        var (currIdle, currTotal) = SystemInfo.ParseCpuStat(snap2);
        var usage = SystemInfo.CalculateCpuPercent(prevIdle, prevTotal, currIdle, currTotal);

        Assert.InRange(usage, 14.0, 16.0);
    }

    [Fact]
    public void FormatUptime_VariousDurations()
    {
        Assert.Equal("0h 5m", SystemInfo.FormatUptime(300));
        Assert.Equal("2h 30m", SystemInfo.FormatUptime(9000));
        Assert.Equal("1d 3h 15m", SystemInfo.FormatUptime(98100));
    }

    [Fact]
    public void ParseFanSpeed_Valid_ReturnsRpm()
    {
        Assert.Equal(2100, SystemInfo.ParseFanSpeed("2100\n"));
    }

    [Fact]
    public void ParseFanSpeed_Invalid_ReturnsNull()
    {
        Assert.Null(SystemInfo.ParseFanSpeed(""));
        Assert.Null(SystemInfo.ParseFanSpeed("not a number"));
    }

    [Fact]
    public void ParseFanSpeed_Zero_ReturnsNull()
    {
        Assert.Null(SystemInfo.ParseFanSpeed("0"));
    }

    [Fact]
    public void GetIpAddresses_ReturnsNonLoopback()
    {
        var ips = SystemInfo.GetIpAddresses();
        Assert.IsType<Dictionary<string, string>>(ips);
        Assert.DoesNotContain(ips, kv => kv.Value == "127.0.0.1");
    }
}
