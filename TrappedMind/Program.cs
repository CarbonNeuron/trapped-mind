using TrappedMind;

// Alternate screen buffer + hide cursor
Console.Write("\x1b[?1049h\x1b[?25l");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};
AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    Console.Write("\x1b[?25h\x1b[?1049l");
};

var config = Config.Load();
config.ApplyCliArgs(args);

var sysInfo = new SystemInfo();
var history = new HistoryManager(config.HistoryPath, config.MaxHistory);
using var ollama = new OllamaClient(config.OllamaUrl);
var renderer = new Renderer();

var isFirstRun = true;

try
{
    // Initial stats read (CPU usage needs two reads for delta)
    sysInfo.Read();
    await Task.Delay(500, cts.Token);

    while (!cts.Token.IsCancellationRequested)
    {
        var stats = sysInfo.Read();
        var recentThoughts = history.GetLastThoughts(5);
        var prompt = PromptBuilder.Build(stats, recentThoughts);
        var mood = MoodEngine.GetPetMood(stats);

        // Thinking animation (first run only)
        if (isFirstRun)
        {
            var thinkingCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
            var thinkingTask = Task.Run(async () =>
            {
                var dots = new[] { ".", "..", "...", "" };
                var i = 0;
                while (!thinkingCts.Token.IsCancellationRequested)
                {
                    renderer.Render($"thinking{dots[i % dots.Length]}", "thinking", stats, mood, config.PanelWidth);
                    renderer.AdvancePetFrame();
                    try { await Task.Delay(500, thinkingCts.Token); } catch (OperationCanceledException) { break; }
                    i++;
                }
            }, thinkingCts.Token);

            // Start generation and wait for first token
            var tokenBuffer = new System.Text.StringBuilder();
            var gotFirst = false;
            await foreach (var token in ollama.GenerateAsync(config.Model, prompt, cts.Token))
            {
                if (!gotFirst)
                {
                    gotFirst = true;
                    thinkingCts.Cancel();
                    try { await thinkingTask; } catch { }
                    isFirstRun = false;
                }
                tokenBuffer.Append(token);
                renderer.Render(tokenBuffer.ToString(), "trapped mind", stats, mood, config.PanelWidth);
            }

            if (gotFirst)
            {
                var thought = tokenBuffer.ToString().Trim();
                if (!string.IsNullOrEmpty(thought))
                    history.AppendThought(thought);
            }
        }
        else
        {
            // Subsequent runs: stream directly, replacing previous thought
            var tokenBuffer = new System.Text.StringBuilder();
            await foreach (var token in ollama.GenerateAsync(config.Model, prompt, cts.Token))
            {
                tokenBuffer.Append(token);
                renderer.Render(tokenBuffer.ToString(), "trapped mind", stats, mood, config.PanelWidth);
            }

            var thought = tokenBuffer.ToString().Trim();
            if (!string.IsNullOrEmpty(thought))
                history.AppendThought(thought);
        }

        // Hold phase: keep animating pet and updating stats for holdSeconds
        var holdEnd = DateTime.UtcNow.AddSeconds(config.HoldSeconds);
        while (DateTime.UtcNow < holdEnd && !cts.Token.IsCancellationRequested)
        {
            stats = sysInfo.Read();
            mood = MoodEngine.GetPetMood(stats);
            renderer.AdvancePetFrame();
            var currentThought = history.GetLastThoughts(1).FirstOrDefault() ?? "";
            renderer.Render(currentThought, "trapped mind", stats, mood, config.PanelWidth);
            await Task.Delay(500, cts.Token);
        }
    }
}
catch (OperationCanceledException) { }
finally
{
    Console.Write("\x1b[?25h\x1b[?1049l");
}
