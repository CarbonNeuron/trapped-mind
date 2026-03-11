using System.Text.Json;

namespace TrappedMind;

public class HistoryManager
{
    private readonly string _directory;
    private readonly long _maxBytes;

    public HistoryManager(string directory, long maxBytes)
    {
        _directory = directory;
        _maxBytes = maxBytes;
        Directory.CreateDirectory(_directory);
    }

    public void AppendMessage(ChatMessage message)
    {
        var file = Path.Combine(_directory, message.Timestamp.ToString("yyyy-MM-dd") + ".jsonl");
        var line = JsonSerializer.Serialize(message);
        File.AppendAllText(file, line + "\n");
        TruncateIfNeeded();
    }

    public List<ChatMessage> LoadAllMessages()
    {
        var messages = new List<ChatMessage>();
        if (!Directory.Exists(_directory))
            return messages;

        var files = Directory.GetFiles(_directory, "*.jsonl")
            .OrderBy(f => Path.GetFileName(f))
            .ToArray();

        foreach (var file in files)
        {
            foreach (var line in File.ReadAllLines(file))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var msg = JsonSerializer.Deserialize<ChatMessage>(line);
                    if (msg != null) messages.Add(msg);
                }
                catch { }
            }
        }

        return messages;
    }

    public List<string> GetLastThoughts(int count)
    {
        var all = LoadAllMessages();
        return all
            .Where(m => m.Source == MessageSource.Ai)
            .Select(m => m.Text)
            .TakeLast(count)
            .ToList();
    }

    public void TruncateIfNeeded()
    {
        if (!Directory.Exists(_directory)) return;

        var files = Directory.GetFiles(_directory, "*.jsonl")
            .OrderBy(f => Path.GetFileName(f))
            .ToList();

        var totalSize = files.Sum(f => new FileInfo(f).Length);

        while (totalSize > _maxBytes && files.Count > 1)
        {
            var oldest = files[0];
            totalSize -= new FileInfo(oldest).Length;
            File.Delete(oldest);
            files.RemoveAt(0);
        }
    }
}
