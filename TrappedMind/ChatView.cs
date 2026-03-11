using System.Globalization;
using Terminal.Gui;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;

namespace TrappedMind;

public class ChatView : View
{
    private readonly List<MessageCard> _cards = new();
    private MessageCard? _streamingCard;
    private int _totalHeight;
    private bool _scrollPending;

    public ChatView()
    {
        CanFocus = true;
        ContentSizeTracksViewport = false;

        // Re-layout and scroll after every layout pass (this is when
        // Viewport has real dimensions, including the very first time)
        SubviewsLaidOut += (_, _) =>
        {
            if (_cards.Count == 0) return;
            RecomputeLayout();
        };
    }

    public void AddMessage(ChatMessage message)
    {
        var card = new MessageCard(message);
        _cards.Add(card);
        Add(card);
        _scrollPending = true;
        SetNeedsLayout();
    }

    public void BeginStreaming()
    {
        var placeholder = new ChatMessage(DateTime.Now, "...", MessageSource.Ai);
        _streamingCard = new MessageCard(placeholder);
        _cards.Add(_streamingCard);
        Add(_streamingCard);
        _scrollPending = true;
        SetNeedsLayout();
    }

    public void UpdateStreaming(string partialText)
    {
        if (_streamingCard is null) return;

        _streamingCard.UpdateText(partialText);
        _scrollPending = true;
        SetNeedsLayout();
    }

    private void RecomputeLayout()
    {
        var width = Viewport.Width;
        if (width <= 0) return;

        int y = 0;
        foreach (var card in _cards)
        {
            var h = ComputeCardHeight(card, width);
            card.X = 0;
            card.Y = y;
            card.Width = width;
            card.Height = h;
            y += h + 1;
        }

        _totalHeight = y;
        SetContentSize(new Size(width, Math.Max(1, _totalHeight)));

        if (_scrollPending)
        {
            _scrollPending = false;
            var viewportHeight = Viewport.Height;
            if (_totalHeight > viewportHeight && viewportHeight > 0)
            {
                Viewport = Viewport with
                {
                    Location = new Point(0, _totalHeight - viewportHeight)
                };
            }
        }
    }

    private static int ComputeCardHeight(MessageCard card, int viewWidth)
    {
        // FrameView border: 1 left + 1 right
        var innerWidth = Math.Max(1, viewWidth - 2);
        var text = card.MessageText;
        if (string.IsNullOrEmpty(text))
            return 3;

        // WordWrapText strips newlines, but the Label preserves them.
        // Wrap each paragraph separately and sum the lines.
        var totalLines = 0;
        foreach (var paragraph in text.Split('\n'))
        {
            if (string.IsNullOrEmpty(paragraph))
            {
                totalLines++;
                continue;
            }

            var wrapped = TextFormatter.WordWrapText(paragraph, innerWidth);
            totalLines += Math.Max(1, wrapped.Count);
        }

        return Math.Max(3, totalLines + 2);
    }
}

internal class MessageCard : FrameView
{
    private readonly Label _textLabel;

    public string MessageText => _textLabel.Text ?? "";

    public MessageCard(ChatMessage message)
    {
        var isUser = message.Source == MessageSource.User;

        Title = isUser
            ? $"{message.FormattedTimestamp} [you]"
            : message.FormattedTimestamp;

        BorderStyle = LineStyle.Rounded;

        var borderColor = isUser ? Color.Blue : Color.DarkGray;
        ColorScheme = new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(borderColor, Color.Black),
        };

        var textColor = isUser ? Color.BrightCyan : Color.White;
        _textLabel = new Label
        {
            Text = message.Text,
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
        };
        _textLabel.TextFormatter.WordWrap = true;
        _textLabel.ColorScheme = new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(textColor, Color.Black),
        };

        Add(_textLabel);
    }

    public void UpdateText(string text)
    {
        _textLabel.Text = text;
        Title = DateTime.Now.ToString("MMM d h:mm tt", CultureInfo.InvariantCulture);
    }
}
