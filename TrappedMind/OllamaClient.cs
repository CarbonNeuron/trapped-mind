using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace TrappedMind;

public class OllamaClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public OllamaClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    }

    public static (string? Token, bool Done) ParseResponseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return (null, false);

        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            var done = root.TryGetProperty("done", out var d) && d.GetBoolean();
            var token = root.TryGetProperty("response", out var r) ? r.GetString() : null;
            return (token, done);
        }
        catch
        {
            return (null, false);
        }
    }

    public static async IAsyncEnumerable<string> ReadTokensFromStream(
        Stream stream, [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;

            var (token, done) = ParseResponseLine(line);
            if (done) break;
            if (token != null && token.Length > 0)
                yield return token;
        }
    }

    public async IAsyncEnumerable<string> GenerateAsync(
        string model, string prompt, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var requestBody = JsonSerializer.Serialize(new { model, prompt, stream = true });
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/generate") { Content = content };
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var responseStream = await response.Content.ReadAsStreamAsync(ct);
        await foreach (var token in ReadTokensFromStream(responseStream, ct))
            yield return token;
    }

    public void Dispose() => _http.Dispose();
}
