using Terminal.Gui;

namespace TrappedMind;

public enum PetMood { Hot, HighCpu, LowBattery, Charging, Normal }

public static class MoodEngine
{
    public static PetMood GetPetMood(SystemStats stats)
    {
        if (stats.CpuTemp > 70) return PetMood.Hot;
        if (stats.CpuUsage > 80) return PetMood.HighCpu;
        if (stats.BatteryPercent < 20) return PetMood.LowBattery;
        if (stats.BatteryStatus == "Charging") return PetMood.Charging;
        return PetMood.Normal;
    }

    public static Color GetPanelColor(SystemStats stats)
    {
        if (stats.CpuTemp > 80) return Color.Red;
        if (stats.CpuTemp > 70) return Color.BrightYellow;
        if (stats.BatteryPercent < 15) return Color.Red;
        if (stats.BatteryPercent < 30) return Color.Yellow;
        if (stats.BatteryStatus == "Charging") return Color.Green;
        return Color.Blue;
    }

    public static Color GetTempColor(double temp) =>
        temp > 70 ? Color.Red : temp > 55 ? Color.Yellow : Color.Green;

    public static Color GetBatteryColor(int percent) =>
        percent < 20 ? Color.Red : percent < 50 ? Color.Yellow : Color.Green;

    public static Color GetCpuColor(double usage) =>
        usage > 80 ? Color.Red : usage > 50 ? Color.Yellow : Color.Green;
}
