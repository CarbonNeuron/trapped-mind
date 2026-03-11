using System.Globalization;
using Terminal.Gui;
using Point = System.Drawing.Point;

namespace TrappedMind;

public class ChatView : View
{
    private readonly List<MessageCard> _cards = new();
    private MessageCard? _streamingCard;

    private bool _initialized;

    public ChatView()
    {
        CanFocus = true;
        ContentSizeTracksViewport = false;

        Initialized += (_, _) =>
        {
            _initialized = true;
            ScrollToBottom();
        };
    }

    public void AddMessage(ChatMessage message)
    {
        var card = CreateCard(message);
        _cards.Add(card);
        Add(card);
        PositionCard(card);
        ScrollToBottom();
    }

    public void BeginStreaming()
    {
        var placeholder = new ChatMessage(DateTime.Now, "...", MessageSource.Ai);
        _streamingCard = CreateCard(placeholder);
        _cards.Add(_streamingCard);
        Add(_streamingCard);
        PositionCard(_streamingCard);
        ScrollToBottom();
    }

    public void UpdateStreaming(string partialText)
    {
        if (_streamingCard is null) return;

        _streamingCard.UpdateText(partialText);
        ScrollToBottom();
    }

    private MessageCard CreateCard(ChatMessage message)
    {
        return new MessageCard(message)
        {
            X = 0,
            Width = Dim.Fill(),
            Height = Dim.Auto(DimAutoStyle.Content),
        };
    }

    private void PositionCard(MessageCard card)
    {
        var idx = _cards.IndexOf(card);
        card.Y = idx == 0
            ? 0
            : Pos.Bottom(_cards[idx - 1]) + 1;
    }

    private void ScrollToBottom()
    {
        if (!_initialized)
            return; // Will scroll once Initialized fires

        SetNeedsDraw();

        Application.AddIdle(() =>
        {
            var contentHeight = GetContentSize().Height;
            var viewportHeight = Viewport.Height;
            if (contentHeight > viewportHeight)
            {
                Viewport = Viewport with
                {
                    Location = new Point(0, contentHeight - viewportHeight)
                };
            }

            SetNeedsDraw();
            return false;
        });
    }
}

internal class MessageCard : FrameView
{
    private readonly Label _textLabel;
    private readonly bool _isUser;

    public MessageCard(ChatMessage message)
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
    }

    public void UpdateText(string text)
    {
        _textLabel.Text = text;
        Title = DateTime.Now.ToString("MMM d h:mm tt", CultureInfo.InvariantCulture);
        SetNeedsDraw();
    }
}
