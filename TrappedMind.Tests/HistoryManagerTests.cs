using System.Text.Json;

namespace TrappedMind.Tests;

public class HistoryManagerTests : IDisposable
{
    private readonly string _tempDir;

    public HistoryManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "trapped_test_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void AppendMessage_CreatesTodayFile()
    {
        var mgr = new HistoryManager(_tempDir, 10L * 1024 * 1024 * 1024);
        var msg = new ChatMessage(new DateTime(2026, 3, 11, 14, 30, 0), "hello", MessageSource.Ai);
        mgr.AppendMessage(msg);

        var file = Path.Combine(_tempDir, "2026-03-11.jsonl");
        Assert.True(File.Exists(file));
        var line = File.ReadAllLines(file).Single();
        var parsed = JsonSerializer.Deserialize<ChatMessage>(line);
        Assert.Equal("hello", parsed!.Text);
        Assert.Equal(MessageSource.Ai, parsed.Source);
    }

    [Fact]
    public void AppendMessage_AppendsToExistingFile()
    {
        var mgr = new HistoryManager(_tempDir, 10L * 1024 * 1024 * 1024);
        var msg1 = new ChatMessage(new DateTime(2026, 3, 11, 14, 30, 0), "first", MessageSource.Ai);
        var msg2 = new ChatMessage(new DateTime(2026, 3, 11, 14, 31, 0), "second", MessageSource.User);
        mgr.AppendMessage(msg1);
        mgr.AppendMessage(msg2);

        var file = Path.Combine(_tempDir, "2026-03-11.jsonl");
        var lines = File.ReadAllLines(file);
        Assert.Equal(2, lines.Length);
    }

    [Fact]
    public void LoadAllMessages_ReturnsChronological()
    {
        var day1 = Path.Combine(_tempDir, "2026-03-10.jsonl");
        var day2 = Path.Combine(_tempDir, "2026-03-11.jsonl");
        var msg1 = new ChatMessage(new DateTime(2026, 3, 10, 9, 0, 0), "old", MessageSource.Ai);
        var msg2 = new ChatMessage(new DateTime(2026, 3, 11, 10, 0, 0), "new", MessageSource.Ai);
        File.WriteAllText(day1, JsonSerializer.Serialize(msg1) + "\n");
        File.WriteAllText(day2, JsonSerializer.Serialize(msg2) + "\n");

        var mgr = new HistoryManager(_tempDir, 10L * 1024 * 1024 * 1024);
        var messages = mgr.LoadAllMessages();
        Assert.Equal(2, messages.Count);
        Assert.Equal("old", messages[0].Text);
        Assert.Equal("new", messages[1].Text);
    }

    [Fact]
    public void LoadAllMessages_EmptyDir_ReturnsEmpty()
    {
        var mgr = new HistoryManager(_tempDir, 10L * 1024 * 1024 * 1024);
        Assert.Empty(mgr.LoadAllMessages());
    }

    [Fact]
    public void GetLastThoughts_ReturnsOnlyAiMessages()
    {
        var mgr = new HistoryManager(_tempDir, 10L * 1024 * 1024 * 1024);
        mgr.AppendMessage(new ChatMessage(new DateTime(2026, 3, 11, 10, 0, 0), "ai1", MessageSource.Ai));
        mgr.AppendMessage(new ChatMessage(new DateTime(2026, 3, 11, 10, 1, 0), "user1", MessageSource.User));
        mgr.AppendMessage(new ChatMessage(new DateTime(2026, 3, 11, 10, 2, 0), "ai2", MessageSource.Ai));

        var thoughts = mgr.GetLastThoughts(5);
        Assert.Equal(2, thoughts.Count);
        Assert.Equal("ai1", thoughts[0]);
        Assert.Equal("ai2", thoughts[1]);
    }

    [Fact]
    public void TruncateIfNeeded_DeletesOldestFiles()
    {
        var day1 = Path.Combine(_tempDir, "2026-03-01.jsonl");
        var day2 = Path.Combine(_tempDir, "2026-03-02.jsonl");
        File.WriteAllText(day1, new string('x', 600));
        File.WriteAllText(day2, new string('y', 600));

        var mgr = new HistoryManager(_tempDir, 800);
        mgr.TruncateIfNeeded();

        Assert.False(File.Exists(day1));
        Assert.True(File.Exists(day2));
    }

    [Fact]
    public void FormattedTimestamp_CorrectFormat()
    {
        var msg = new ChatMessage(new DateTime(2026, 3, 11, 14, 32, 0), "test", MessageSource.Ai);
        Assert.Equal("Mar 11 2:32 PM", msg.FormattedTimestamp);
    }
}
