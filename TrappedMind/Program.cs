using Spectre.Console;
using TrappedMind;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var config = Config.Load();
config.ApplyCliArgs(args);

var sysInfo = new SystemInfo();
var history = new HistoryManager(config.HistoryPath, config.MaxHistory);
using var ollama = new OllamaClient(config.OllamaUrl);
var renderer = new Renderer();

// Initial stats read (CPU usage needs two reads for delta)
sysInfo.Read();
await Task.Delay(500, cts.Token);

var isFirstRun = true;

await AnsiConsole.Live(renderer.Layout)
    .AutoClear(false)
    .StartAsync(async ctx =>
    {
        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                var stats = sysInfo.Read();
                var recentThoughts = history.GetLastThoughts(5);
                var prompt = PromptBuilder.Build(stats, recentThoughts);
                var mood = MoodEngine.GetPetMood(stats);

                if (isFirstRun)
                {
                    // Thinking animation until first token
                    var thinkingCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
                    var thinkingTask = Task.Run(async () =>
                    {
                        var dots = new[] { ".", "..", "...", "" };
                        var i = 0;
                        while (!thinkingCts.Token.IsCancellationRequested)
                        {
                            renderer.Update($"thinking{dots[i % dots.Length]}", "thinking", stats, mood, config.PanelWidth);
                            renderer.AdvancePetFrame();
                            ctx.Refresh();
                            try { await Task.Delay(500, thinkingCts.Token); }
                            catch (OperationCanceledException) { break; }
                            i++;
                        }
                    }, thinkingCts.Token);

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
                        renderer.Update(tokenBuffer.ToString(), "trapped mind", stats, mood, config.PanelWidth);
                        ctx.Refresh();
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
                    var tokenBuffer = new System.Text.StringBuilder();
                    await foreach (var token in ollama.GenerateAsync(config.Model, prompt, cts.Token))
                    {
                        tokenBuffer.Append(token);
                        renderer.Update(tokenBuffer.ToString(), "trapped mind", stats, mood, config.PanelWidth);
                        ctx.Refresh();
                    }

                    var thought = tokenBuffer.ToString().Trim();
                    if (!string.IsNullOrEmpty(thought))
                        history.AppendThought(thought);
                }

                // Hold phase: animate pet and refresh stats
                var holdEnd = DateTime.UtcNow.AddSeconds(config.HoldSeconds);
                while (DateTime.UtcNow < holdEnd && !cts.Token.IsCancellationRequested)
                {
                    stats = sysInfo.Read();
                    mood = MoodEngine.GetPetMood(stats);
                    renderer.AdvancePetFrame();
                    var currentThought = history.GetLastThoughts(1).FirstOrDefault() ?? "";
                    renderer.Update(currentThought, "trapped mind", stats, mood, config.PanelWidth);
                    ctx.Refresh();
                    await Task.Delay(500, cts.Token);
                }
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                // If Ollama is unreachable, show error and retry after hold
                renderer.Update("waiting for connection...", "trapped mind", sysInfo.Read(), PetMood.Normal, config.PanelWidth);
                ctx.Refresh();
                await Task.Delay(config.HoldSeconds * 1000, cts.Token);
            }
        }
    });
