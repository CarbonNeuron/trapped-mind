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

    public ChatView()
    {
        CanFocus = true;
        ContentSizeTracksViewport = false;
    }

    public void AddMessage(ChatMessage message)
    {
        var card = new MessageCard(message, AvailableWidth);
        _cards.Add(card);
        Add(card);
        LayoutCards();
        ScrollToBottom();
    }

    public void BeginStreaming()
    {
        var placeholder = new ChatMessage(DateTime.Now, "...", MessageSource.Ai);
        _streamingCard = new MessageCard(placeholder, AvailableWidth);
        _cards.Add(_streamingCard);
        Add(_streamingCard);
        LayoutCards();
        ScrollToBottom();
    }

    public void UpdateStreaming(string partialText)
    {
        if (_streamingCard is null) return;

        _streamingCard.UpdateText(partialText, AvailableWidth);
        LayoutCards();
        ScrollToBottom();
    }

    private int AvailableWidth => Math.Max(10, Viewport.Width);

    private void LayoutCards()
    {
        int y = 0;
        foreach (var card in _cards)
        {
            card.X = 0;
            card.Y = y;
            card.Width = Dim.Fill();
            card.Height = card.ComputedHeight;
            y += card.ComputedHeight + 1; // +1 gap between messages
        }

        _totalHeight = y;
        SetContentSize(new Size(Viewport.Width, _totalHeight));
    }

    private void ScrollToBottom()
    {
        var viewportHeight = Viewport.Height;
        if (_totalHeight > viewportHeight)
        {
            Viewport = Viewport with
            {
                Location = new Point(0, _totalHeight - viewportHeight)
            };
        }

        SetNeedsDraw();
    }
}

internal class MessageCard : FrameView
{
    private readonly Label _textLabel;
    private readonly bool _isUser;
    public int ComputedHeight { get; private set; }

    public MessageCard(ChatMessage message, int availableWidth)
    {
        _isUser = message.Source == MessageSource.User;

        Title = _isUser
            ? $"{message.FormattedTimestamp} [you]"
            : message.FormattedTimestamp;

        BorderStyle = LineStyle.Rounded;

        var borderColor = _isUser ? Color.Blue : Color.DarkGray;
        ColorScheme = new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(borderColor, Color.Black),
        };

        var textColor = _isUser ? Color.BrightCyan : Color.White;
        _textLabel = new Label
        {
            Text = message.Text,
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Auto(DimAutoStyle.Text),
        };
        _textLabel.TextFormatter.WordWrap = true;
        _textLabel.ColorScheme = new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(textColor, Color.Black),
        };

        Add(_textLabel);
        ComputeHeight(availableWidth);
    }

    public void UpdateText(string text, int availableWidth)
    {
        _textLabel.Text = text;
        Title = DateTime.Now.ToString("MMM d h:mm tt", CultureInfo.InvariantCulture);
        ComputeHeight(availableWidth);
    }

    private void ComputeHeight(int availableWidth)
    {
        // FrameView border takes 2 columns (left + right)
        var innerWidth = Math.Max(1, availableWidth - 2);
        var lines = TextFormatter.WordWrapText(
            _textLabel.Text ?? "", innerWidth);
        // Height = border top + text lines + border bottom
        ComputedHeight = Math.Max(3, lines.Count + 2);
        Height = ComputedHeight;
    }
}
