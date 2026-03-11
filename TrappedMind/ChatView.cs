using System.Globalization;
using Terminal.Gui;

namespace TrappedMind;

public class ChatView : View
{
    private readonly List<ChatLine> _lines = new();
    private int _scrollOffset;

    public void AddMessage(ChatMessage message)
    {
        var isUser = message.Source == MessageSource.User;
        var timestampText = isUser
            ? $" {message.FormattedTimestamp} [you]"
            : $" {message.FormattedTimestamp}";

        // Box top
        _lines.Add(new ChatLine("+" + new string('-', 60) + "+", Color.DarkGray));
        // Timestamp
        _lines.Add(new ChatLine("| " + timestampText, isUser ? Color.Blue : Color.DarkGray));
        // Message text
        foreach (var textLine in message.Text.Split('\n'))
            _lines.Add(new ChatLine("| " + textLine, isUser ? Color.BrightCyan : Color.White));
        // Box bottom
        _lines.Add(new ChatLine("+" + new string('-', 60) + "+", Color.DarkGray));
        // Blank separator
        _lines.Add(new ChatLine("", Color.Black));

        ScrollToBottom();
    }

    public void BeginStreaming()
    {
        var now = DateTime.Now.ToString("MMM d h:mm tt", CultureInfo.InvariantCulture);
        _lines.Add(new ChatLine("+" + new string('-', 60) + "+", Color.DarkGray));
        _lines.Add(new ChatLine("| " + now, Color.DarkGray));
        _lines.Add(new ChatLine("| ...", Color.White));
        _lines.Add(new ChatLine("+" + new string('-', 60) + "+", Color.DarkGray));
        _lines.Add(new ChatLine("", Color.Black));

        ScrollToBottom();
    }

    public void UpdateStreaming(string partialText)
    {
        // Find last box: search backwards for the last blank separator
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

        // Find the box-top before this blank
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

        // Remove from boxTop to lastBlank (inclusive)
        _lines.RemoveRange(boxTop, lastBlank - boxTop + 1);

        // Re-add the box with updated content
        var now = DateTime.Now.ToString("MMM d h:mm tt", CultureInfo.InvariantCulture);
        _lines.Add(new ChatLine("+" + new string('-', 60) + "+", Color.DarkGray));
        _lines.Add(new ChatLine("| " + now, Color.DarkGray));
        foreach (var line in partialText.Split('\n'))
            _lines.Add(new ChatLine("| " + line, Color.White));
        _lines.Add(new ChatLine("+" + new string('-', 60) + "+", Color.DarkGray));
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
