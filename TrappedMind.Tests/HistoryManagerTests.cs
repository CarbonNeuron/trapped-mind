namespace TrappedMind.Tests;

public class HistoryManagerTests : IDisposable
{
    private readonly string _tempFile;

    public HistoryManagerTests()
    {
        _tempFile = Path.GetTempFileName();
    }

    public void Dispose() => File.Delete(_tempFile);

    [Fact]
    public void GetLastThoughts_EmptyFile_ReturnsEmpty()
    {
        File.WriteAllText(_tempFile, "");
        var mgr = new HistoryManager(_tempFile, 50);
        Assert.Empty(mgr.GetLastThoughts(5));
    }

    [Fact]
    public void GetLastThoughts_FewerThanN_ReturnsAll()
    {
        File.WriteAllLines(_tempFile, new[] { "thought1", "thought2" });
        var mgr = new HistoryManager(_tempFile, 50);
        var thoughts = mgr.GetLastThoughts(5);
        Assert.Equal(2, thoughts.Count);
        Assert.Equal("thought1", thoughts[0]);
    }

    [Fact]
    public void GetLastThoughts_MoreThanN_ReturnsLastN()
    {
        File.WriteAllLines(_tempFile, new[] { "a", "b", "c", "d", "e", "f" });
        var mgr = new HistoryManager(_tempFile, 50);
        var thoughts = mgr.GetLastThoughts(3);
        Assert.Equal(3, thoughts.Count);
        Assert.Equal("d", thoughts[0]);
        Assert.Equal("f", thoughts[2]);
    }

    [Fact]
    public void AppendThought_AddsLine()
    {
        File.WriteAllText(_tempFile, "");
        var mgr = new HistoryManager(_tempFile, 50);
        mgr.AppendThought("hello world");
        var lines = File.ReadAllLines(_tempFile);
        Assert.Single(lines);
        Assert.Equal("hello world", lines[0]);
    }

    [Fact]
    public void AppendThought_TrimsWhenOverMax()
    {
        File.WriteAllLines(_tempFile, Enumerable.Range(1, 50).Select(i => $"thought{i}"));
        var mgr = new HistoryManager(_tempFile, 50);
        mgr.AppendThought("new thought");
        var lines = File.ReadAllLines(_tempFile);
        Assert.Equal(50, lines.Length);
        Assert.Equal("thought2", lines[0]);
        Assert.Equal("new thought", lines[^1]);
    }

    [Fact]
    public void GetLastThoughts_FileDoesNotExist_ReturnsEmpty()
    {
        var mgr = new HistoryManager("/tmp/nonexistent_" + Guid.NewGuid(), 50);
        Assert.Empty(mgr.GetLastThoughts(5));
    }
}
