using Xunit;

namespace TrappedMind.Tests;

public class ConfigTests
{
    [Fact]
    public void DefaultConfig_HasCorrectValues()
    {
        var config = Config.Default();
        Assert.Equal("http://localhost:11434", config.OllamaUrl);
        Assert.Equal("trapped", config.Model);
        Assert.Equal(10, config.HoldSeconds);
        Assert.Equal(50, config.MaxHistory);
        Assert.Equal(70, config.PanelWidth);
        Assert.NotNull(config.HistoryPath);
    }

    [Fact]
    public void LoadFromJson_OverridesDefaults()
    {
        var json = """{"ollamaUrl":"http://other:11434","model":"test","holdSeconds":5}""";
        var config = Config.FromJson(json);
        Assert.Equal("http://other:11434", config.OllamaUrl);
        Assert.Equal("test", config.Model);
        Assert.Equal(5, config.HoldSeconds);
        // Non-overridden values stay default
        Assert.Equal(50, config.MaxHistory);
    }

    [Fact]
    public void ApplyCliArgs_OverridesConfig()
    {
        var config = Config.Default();
        config.ApplyCliArgs(new[] { "--model", "qwen2.5:0.5b", "--ollama-url", "http://remote:11434" });
        Assert.Equal("qwen2.5:0.5b", config.Model);
        Assert.Equal("http://remote:11434", config.OllamaUrl);
    }

    [Fact]
    public void ApplyCliArgs_NoArgs_KeepsDefaults()
    {
        var config = Config.Default();
        config.ApplyCliArgs(Array.Empty<string>());
        Assert.Equal("trapped", config.Model);
    }
}
