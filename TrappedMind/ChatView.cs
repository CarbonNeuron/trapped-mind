using System.Globalization;
using Terminal.Gui;

namespace TrappedMind;

public class ChatView : View
{
    private readonly List<ChatLine> _lines = new();
    private int _scrollOffset;

    private int ContentWidth => Math.Max(10, Viewport.Width - 4); // "| " prefix + " |" suffix space

    private List<string> WrapText(string text, int maxWidth)
    {
        var result = new List<string>();
        foreach (var line in text.Split('\n'))
        {
            if (line.Length <= maxWidth)
            {
                result.Add(line);
                continue;
            }

            var remaining = line;
            while (remaining.Length > maxWidth)
            {
                // Try to break at a space
                var breakAt = remaining.LastIndexOf(' ', maxWidth);
                if (breakAt <= 0) breakAt = maxWidth;
                result.Add(remaining[..breakAt]);
                remaining = remaining[breakAt..].TrimStart();
            }
            if (remaining.Length > 0)
                result.Add(remaining);
        }
        return result;
    }

    public void AddMessage(ChatMessage message)
    {
        var isUser = message.Source == MessageSource.User;
        var timestampText = isUser
            ? $" {message.FormattedTimestamp} [you]"
            : $" {message.FormattedTimestamp}";

        var width = ContentWidth;
        _lines.Add(new ChatLine("+" + new string('-', width + 2) + "+", Color.DarkGray));
        _lines.Add(new ChatLine("| " + timestampText, isUser ? Color.Blue : Color.DarkGray));
        foreach (var wrapped in WrapText(message.Text, width))
            _lines.Add(new ChatLine("| " + wrapped, isUser ? Color.BrightCyan : Color.White));
        _lines.Add(new ChatLine("+" + new string('-', width + 2) + "+", Color.DarkGray));
        _lines.Add(new ChatLine("", Color.Black));

        ScrollToBottom();
    }

    public void BeginStreaming()
    {
        var now = DateTime.Now.ToString("MMM d h:mm tt", CultureInfo.InvariantCulture);
        var width = ContentWidth;
        _lines.Add(new ChatLine("+" + new string('-', width + 2) + "+", Color.DarkGray));
        _lines.Add(new ChatLine("| " + now, Color.DarkGray));
        _lines.Add(new ChatLine("| ...", Color.White));
        _lines.Add(new ChatLine("+" + new string('-', width + 2) + "+", Color.DarkGray));
        _lines.Add(new ChatLine("", Color.Black));

        ScrollToBottom();
    }

    public void UpdateStreaming(string partialText)
    {
        var lastBlank = -1;
        for (var i = _lines.Count - 1; i >= 0; i--)
        {
            if (_lines[i].Text == "")
            {
                lastBlank = i;
                break;
            }
        }

        if (lastBlank < 0) return;

        var boxBottom = lastBlank - 1;
        if (boxBottom < 0) return;

        var boxTop = -1;
        for (var i = boxBottom - 1; i >= 0; i--)
        {
            if (_lines[i].Text.StartsWith("+"))
            {
                boxTop = i;
                break;
            }
        }

        if (boxTop < 0) return;

        _lines.RemoveRange(boxTop, lastBlank - boxTop + 1);

        var now = DateTime.Now.ToString("MMM d h:mm tt", CultureInfo.InvariantCulture);
        var width = ContentWidth;
        _lines.Add(new ChatLine("+" + new string('-', width + 2) + "+", Color.DarkGray));
        _lines.Add(new ChatLine("| " + now, Color.DarkGray));
        foreach (var wrapped in WrapText(partialText, width))
            _lines.Add(new ChatLine("| " + wrapped, Color.White));
        _lines.Add(new ChatLine("+" + new string('-', width + 2) + "+", Color.DarkGray));
        _lines.Add(new ChatLine("", Color.Black));

        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        var visibleRows = Viewport.Height;
        _scrollOffset = Math.Max(0, _lines.Count - visibleRows);
        SetNeedsDraw();
    }

    protected override bool OnDrawingContent(DrawContext? context)
    {
        ClearViewport();
        var viewport = Viewport;
        var visibleRows = viewport.Height;
        for (var row = 0; row < visibleRows; row++)
        {
            var lineIdx = _scrollOffset + row;
            if (lineIdx >= _lines.Count) break;

            var chatLine = _lines[lineIdx];
            var attr = new Terminal.Gui.Attribute(chatLine.Color, Color.Black);
            Application.Driver!.SetAttribute(attr);
            Move(0, row);

            var text = chatLine.Text;
            if (text.Length > viewport.Width)
                text = text[..viewport.Width];
            Application.Driver.AddStr(text);
        }

        return true;
    }

    private record ChatLine(string Text, Color Color);
}
