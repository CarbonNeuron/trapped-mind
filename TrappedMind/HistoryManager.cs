namespace TrappedMind;

public class HistoryManager
{
    private readonly string _path;
    private readonly int _maxLines;

    public HistoryManager(string path, int maxLines)
    {
        _path = path;
        _maxLines = maxLines;
    }

    public List<string> GetLastThoughts(int count)
    {
        try
        {
            if (!File.Exists(_path)) return new List<string>();
            var lines = File.ReadAllLines(_path)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();
            return lines.Skip(Math.Max(0, lines.Count - count)).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    public void AppendThought(string thought)
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        List<string> lines;
        try
        {
            lines = File.Exists(_path)
                ? File.ReadAllLines(_path).Where(l => !string.IsNullOrWhiteSpace(l)).ToList()
                : new List<string>();
        }
        catch
        {
            lines = new List<string>();
        }

        lines.Add(thought);

        if (lines.Count > _maxLines)
            lines = lines.Skip(lines.Count - _maxLines).ToList();

        File.WriteAllLines(_path, lines);
    }
}
