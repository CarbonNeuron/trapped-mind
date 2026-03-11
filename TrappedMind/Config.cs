using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrappedMind;

public class Config
{
    [JsonPropertyName("ollamaUrl")]
    public string OllamaUrl { get; set; } = "http://localhost:11434";

    [JsonPropertyName("model")]
    public string Model { get; set; } = "trapped";

    [JsonPropertyName("holdSeconds")]
    public int HoldSeconds { get; set; } = 10;

    [JsonPropertyName("maxHistory")]
    public int MaxHistory { get; set; } = 50;

    [JsonPropertyName("historyPath")]
    public string HistoryPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "trapped_history.txt");

    [JsonPropertyName("panelWidth")]
    public int PanelWidth { get; set; } = 70;

    [JsonPropertyName("systemPrompt")]
    public string? SystemPrompt { get; set; }

    [JsonPropertyName("historyDir")]
    public string HistoryDir { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".trapped-mind", "history");

    [JsonPropertyName("maxHistoryBytes")]
    public long MaxHistoryBytes { get; set; } = 10L * 1024 * 1024 * 1024;

    public static Config Default() => new();

    public static Config FromJson(string json)
    {
        var config = Default();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("ollamaUrl", out var url)) config.OllamaUrl = url.GetString()!;
        if (root.TryGetProperty("model", out var model)) config.Model = model.GetString()!;
        if (root.TryGetProperty("holdSeconds", out var hold)) config.HoldSeconds = hold.GetInt32();
        if (root.TryGetProperty("maxHistory", out var max)) config.MaxHistory = max.GetInt32();
        if (root.TryGetProperty("panelWidth", out var pw)) config.PanelWidth = pw.GetInt32();
        if (root.TryGetProperty("historyPath", out var hp)) config.HistoryPath = hp.GetString()!;
        if (root.TryGetProperty("systemPrompt", out var sp)) config.SystemPrompt = sp.GetString();
        if (root.TryGetProperty("historyDir", out var hd)) config.HistoryDir = hd.GetString()!;
        if (root.TryGetProperty("maxHistoryBytes", out var mhb)) config.MaxHistoryBytes = mhb.GetInt64();

        return config;
    }

    public static Config Load()
    {
        var paths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config", "trapped-mind", "config.json"),
            Path.Combine(AppContext.BaseDirectory, "appsettings.json")
        };

        foreach (var path in paths)
        {
            if (File.Exists(path))
                return FromJson(File.ReadAllText(path));
        }

        return Default();
    }

    public void ApplyCliArgs(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--model":
                    Model = args[++i];
                    break;
                case "--ollama-url":
                    OllamaUrl = args[++i];
                    break;
            }
        }
    }
}
