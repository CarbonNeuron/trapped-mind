using Terminal.Gui;
using TrappedMind;

var cts = new CancellationTokenSource();

var config = Config.Load();
config.ApplyCliArgs(args);

var sysInfo = new SystemInfo();
var history = new HistoryManager(config.HistoryDir, config.MaxHistoryBytes);
using var ollama = new OllamaClient(config.OllamaUrl);
var renderer = new Renderer();

var inputLock = new object();
string? pendingUserMessage = null;
var holdCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
var commandHandler = new CommandHandler();

renderer.OnUserInput(msg =>
{
    if (commandHandler.IsCommand(msg))
    {
        // Show the command in chat
        var cmdMsg = new ChatMessage(DateTime.Now, msg, MessageSource.User);
        renderer.AddChatMessage(cmdMsg);

        // Run command in background, show result in chat
        _ = Task.Run(async () =>
        {
            var result = await commandHandler.ExecuteAsync(msg, cts.Token);
            var resultMsg = new ChatMessage(DateTime.Now, result, MessageSource.Ai);
            Application.Invoke(() => renderer.AddChatMessage(resultMsg));
        });
        return;
    }

    var chatMsg = new ChatMessage(DateTime.Now, msg, MessageSource.User);
    history.AppendMessage(chatMsg);
    renderer.AddChatMessage(chatMsg);
    lock (inputLock)
    {
        pendingUserMessage = msg;
    }
    // Interrupt hold phase
    try { holdCts.Cancel(); } catch (ObjectDisposedException) { }
});

// Load existing history into chat
foreach (var msg in history.LoadAllMessages())
    renderer.AddChatMessage(msg);

// Initial stats read (CPU needs two reads for delta)
sysInfo.Read();

Application.Init();

var top = new Toplevel();
foreach (var view in renderer.GetViews())
    top.Add(view);

// Background worker for LLM loop
_ = Task.Run(async () =>
{
    await Task.Delay(500, cts.Token); // initial CPU delta wait

    var isFirstRun = true;

    while (!cts.Token.IsCancellationRequested)
    {
        try
        {
            var stats = sysInfo.Read();
            Application.Invoke(() =>
            {
                renderer.UpdateStats(stats);
                renderer.UpdatePet(MoodEngine.GetPetMood(stats));
                renderer.UpdatePanelBorder(stats);
            });

            var recentThoughts = history.GetLastThoughts(5);
            var prompt = PromptBuilder.Build(stats, recentThoughts);

            lock (inputLock)
            {
                if (pendingUserMessage != null)
                {
                    prompt += $"\nThe user says: {pendingUserMessage}\n";
                    pendingUserMessage = null;
                }
            }

            // Thinking animation on first run
            if (isFirstRun)
            {
                var thinkingCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
                var thinkingTask = Task.Run(async () =>
                {
                    var dots = new[] { ".", "..", "...", "" };
                    var i = 0;
                    while (!thinkingCts.Token.IsCancellationRequested)
                    {
                        Application.Invoke(() =>
                        {
                            renderer.AdvancePetFrame();
                            renderer.UpdatePet(MoodEngine.GetPetMood(stats));
                        });
                        try { await Task.Delay(500, thinkingCts.Token); }
                        catch (OperationCanceledException) { break; }
                        i++;
                    }
                }, thinkingCts.Token);

                Application.Invoke(() => renderer.BeginStreamingMessage());

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
                    var text = tokenBuffer.ToString();
                    Application.Invoke(() => renderer.UpdateStreamingMessage(text));
                }

                var thought = tokenBuffer.ToString().Trim();
                if (!string.IsNullOrEmpty(thought))
                {
                    var chatMsg = new ChatMessage(DateTime.Now, thought, MessageSource.Ai);
                    history.AppendMessage(chatMsg);
                }
            }
            else
            {
                Application.Invoke(() => renderer.BeginStreamingMessage());

                var tokenBuffer = new System.Text.StringBuilder();
                await foreach (var token in ollama.GenerateAsync(config.Model, prompt, cts.Token))
                {
                    tokenBuffer.Append(token);
                    var text = tokenBuffer.ToString();
                    Application.Invoke(() => renderer.UpdateStreamingMessage(text));
                }

                var thought = tokenBuffer.ToString().Trim();
                if (!string.IsNullOrEmpty(thought))
                {
                    var chatMsg = new ChatMessage(DateTime.Now, thought, MessageSource.Ai);
                    history.AppendMessage(chatMsg);
                }
            }

            // Hold phase -- interruptible by user input
            holdCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
            var holdEnd = DateTime.UtcNow.AddSeconds(config.HoldSeconds);
            while (DateTime.UtcNow < holdEnd && !holdCts.Token.IsCancellationRequested)
            {
                stats = sysInfo.Read();
                var mood = MoodEngine.GetPetMood(stats);
                Application.Invoke(() =>
                {
                    renderer.AdvancePetFrame();
                    renderer.UpdatePet(mood);
                    renderer.UpdateStats(stats);
                    renderer.UpdatePanelBorder(stats);
                });
                try { await Task.Delay(500, holdCts.Token); }
                catch (OperationCanceledException) { break; }
            }
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            // Read stats outside UI thread, then update UI
            var errorStats = sysInfo.Read();
            Application.Invoke(() => renderer.UpdateStats(errorStats));
            try { await Task.Delay(config.HoldSeconds * 1000, cts.Token); }
            catch (OperationCanceledException) { break; }
        }
    }
}, cts.Token);

top.Closing += (s, e) =>
{
    cts.Cancel();
};

Application.Run(top);
Application.Shutdown();
