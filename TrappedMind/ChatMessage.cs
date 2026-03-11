using System.Globalization;
using System.Text.Json.Serialization;

namespace TrappedMind;

public enum MessageSource { Ai, User }

public record ChatMessage(
    [property: JsonPropertyName("timestamp")] DateTime Timestamp,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("source")] MessageSource Source)
{
    public string FormattedTimestamp =>
        Timestamp.ToString("MMM d h:mm tt", CultureInfo.InvariantCulture);
}
