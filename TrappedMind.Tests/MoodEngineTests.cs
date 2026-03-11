using Spectre.Console;

namespace TrappedMind.Tests;

public class MoodEngineTests
{
    [Fact]
    public void GetPetMood_HotCpu_ReturnsHot()
    {
        var stats = new SystemStats(75, 50, "Discharging", 30, 4, 8, 1000);
        Assert.Equal(PetMood.Hot, MoodEngine.GetPetMood(stats));
    }

    [Fact]
    public void GetPetMood_HighCpu_ReturnsPanicking()
    {
        var stats = new SystemStats(60, 50, "Discharging", 85, 4, 8, 1000);
        Assert.Equal(PetMood.HighCpu, MoodEngine.GetPetMood(stats));
    }

    [Fact]
    public void GetPetMood_LowBattery_ReturnsSleepy()
    {
        var stats = new SystemStats(50, 15, "Discharging", 30, 4, 8, 1000);
        Assert.Equal(PetMood.LowBattery, MoodEngine.GetPetMood(stats));
    }

    [Fact]
    public void GetPetMood_Charging_ReturnsHappy()
    {
        var stats = new SystemStats(50, 50, "Charging", 30, 4, 8, 1000);
        Assert.Equal(PetMood.Charging, MoodEngine.GetPetMood(stats));
    }

    [Fact]
    public void GetPetMood_Normal_ReturnsCalm()
    {
        var stats = new SystemStats(50, 50, "Discharging", 30, 4, 8, 1000);
        Assert.Equal(PetMood.Normal, MoodEngine.GetPetMood(stats));
    }

    [Fact]
    public void GetPetMood_HotOverridesHighCpu()
    {
        var stats = new SystemStats(75, 50, "Discharging", 90, 4, 8, 1000);
        Assert.Equal(PetMood.Hot, MoodEngine.GetPetMood(stats));
    }

    [Fact]
    public void GetPetMood_NullStats_ReturnsNormal()
    {
        var stats = new SystemStats(null, null, null, null, null, null, null);
        Assert.Equal(PetMood.Normal, MoodEngine.GetPetMood(stats));
    }

    [Fact]
    public void GetPanelColor_VeryHot_ReturnsRed()
    {
        var stats = new SystemStats(85, 50, "Discharging", 30, 4, 8, 1000);
        Assert.Equal(Color.Red, MoodEngine.GetPanelColor(stats));
    }

    [Fact]
    public void GetPanelColor_Hot_ReturnsOrange()
    {
        var stats = new SystemStats(75, 50, "Discharging", 30, 4, 8, 1000);
        Assert.Equal(Color.Orange1, MoodEngine.GetPanelColor(stats));
    }

    [Fact]
    public void GetPanelColor_VeryLowBattery_ReturnsRed()
    {
        var stats = new SystemStats(50, 10, "Discharging", 30, 4, 8, 1000);
        Assert.Equal(Color.Red, MoodEngine.GetPanelColor(stats));
    }

    [Fact]
    public void GetPanelColor_LowBattery_ReturnsYellow()
    {
        var stats = new SystemStats(50, 25, "Discharging", 30, 4, 8, 1000);
        Assert.Equal(Color.Yellow, MoodEngine.GetPanelColor(stats));
    }

    [Fact]
    public void GetPanelColor_Charging_ReturnsGreen()
    {
        var stats = new SystemStats(50, 50, "Charging", 30, 4, 8, 1000);
        Assert.Equal(Color.Green, MoodEngine.GetPanelColor(stats));
    }

    [Fact]
    public void GetPanelColor_Default_ReturnsBlue()
    {
        var stats = new SystemStats(50, 50, "Discharging", 30, 4, 8, 1000);
        Assert.Equal(Color.Blue, MoodEngine.GetPanelColor(stats));
    }

    [Fact]
    public void GetTempColor_Cool_Green() =>
        Assert.Equal(Color.Green, MoodEngine.GetTempColor(50));

    [Fact]
    public void GetTempColor_Warm_Yellow() =>
        Assert.Equal(Color.Yellow, MoodEngine.GetTempColor(60));

    [Fact]
    public void GetTempColor_Hot_Red() =>
        Assert.Equal(Color.Red, MoodEngine.GetTempColor(75));

    [Fact]
    public void GetBatteryColor_High_Green() =>
        Assert.Equal(Color.Green, MoodEngine.GetBatteryColor(60));

    [Fact]
    public void GetBatteryColor_Mid_Yellow() =>
        Assert.Equal(Color.Yellow, MoodEngine.GetBatteryColor(30));

    [Fact]
    public void GetBatteryColor_Low_Red() =>
        Assert.Equal(Color.Red, MoodEngine.GetBatteryColor(10));

    [Fact]
    public void GetCpuColor_Low_Green() =>
        Assert.Equal(Color.Green, MoodEngine.GetCpuColor(30));

    [Fact]
    public void GetCpuColor_Mid_Yellow() =>
        Assert.Equal(Color.Yellow, MoodEngine.GetCpuColor(60));

    [Fact]
    public void GetCpuColor_High_Red() =>
        Assert.Equal(Color.Red, MoodEngine.GetCpuColor(90));
}
