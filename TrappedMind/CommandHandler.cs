using System.Diagnostics;

namespace TrappedMind;

public class CommandHandler
{
    private readonly Dictionary<string, Func<string[], Task<string>>> _commands = new(StringComparer.OrdinalIgnoreCase);

    public CommandHandler()
    {
        _commands["update"] = HandleUpdate;
        _commands["help"] = HandleHelp;
    }

    public bool IsCommand(string input) => input.StartsWith('/');

    public async Task<string> ExecuteAsync(string input, CancellationToken ct)
    {
        var parts = input.TrimStart('/').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return "Empty command. Type /help for available commands.";

        var name = parts[0];
        var args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

        if (_commands.TryGetValue(name, out var handler))
            return await handler(args);

        return $"Unknown command: /{name}. Type /help for available commands.";
    }

    private async Task<string> HandleUpdate(string[] args)
    {
        const string script = "/usr/local/bin/trapped-mind-update.sh";
        if (!File.Exists(script))
            return "Update script not found. Is ansible provisioning complete?";

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = script,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using var proc = Process.Start(psi);
            if (proc is null)
                return "Failed to start update process.";

            var stdout = await proc.StandardOutput.ReadToEndAsync();
            var stderr = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            if (proc.ExitCode == 0)
                return string.IsNullOrWhiteSpace(stdout)
                    ? "Update complete. Already up to date."
                    : $"Update complete.\n{stdout.Trim()}";

            return $"Update failed (exit {proc.ExitCode}).\n{stderr.Trim()}";
        }
        catch (Exception ex)
        {
            return $"Update error: {ex.Message}";
        }
    }

    private Task<string> HandleHelp(string[] args)
    {
        var help = """
            Available commands:
              /update  - Pull latest from GitHub and re-run ansible
              /help    - Show this help message
            """;
        return Task.FromResult(help);
    }
}
