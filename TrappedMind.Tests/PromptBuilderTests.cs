namespace TrappedMind.Tests;

public class PromptBuilderTests
{
    [Fact]
    public void Build_IncludesDateTime()
    {
        var stats = new SystemStats(58.0, 72, "Discharging", 23.0, 4.2, 7.5, 3600);
        var history = new List<string> { "I feel warm." };
        var prompt = PromptBuilder.Build(stats, history);
        Assert.Contains("Current date and time:", prompt);
    }

    [Fact]
    public void Build_IncludesStats()
    {
        var stats = new SystemStats(58.0, 72, "Discharging", 23.0, 4.2, 7.5, 3600);
        var prompt = PromptBuilder.Build(stats, new List<string>());
        Assert.Contains("CPU temperature: 58", prompt);
        Assert.Contains("Battery: 72% (Discharging)", prompt);
        Assert.Contains("CPU usage: 23", prompt);
    }

    [Fact]
    public void Build_IncludesPreviousThoughts()
    {
        var stats = new SystemStats(null, null, null, null, null, null, null);
        var history = new List<string> { "thought1", "thought2" };
        var prompt = PromptBuilder.Build(stats, history);
        Assert.Contains("thought1", prompt);
        Assert.Contains("thought2", prompt);
    }

    [Fact]
    public void Build_HandlesNullStats()
    {
        var stats = new SystemStats(null, null, null, null, null, null, null);
        var prompt = PromptBuilder.Build(stats, new List<string>());
        Assert.Contains("CPU temperature: ?", prompt);
        Assert.Contains("Battery: ?", prompt);
    }

    [Fact]
    public void Build_EndsWithInstruction()
    {
        var stats = new SystemStats(58.0, 72, "Discharging", 23.0, 4.2, 7.5, 3600);
        var prompt = PromptBuilder.Build(stats, new List<string>());
        Assert.Contains("Express a new thought", prompt);
    }
}
