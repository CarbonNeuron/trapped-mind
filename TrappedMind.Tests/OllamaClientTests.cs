namespace TrappedMind.Tests;

public class OllamaClientTests
{
    [Fact]
    public void ParseResponseLine_ValidToken_ReturnsToken()
    {
        var line = """{"response": "Hello", "done": false}""";
        var (token, done) = OllamaClient.ParseResponseLine(line);
        Assert.Equal("Hello", token);
        Assert.False(done);
    }

    [Fact]
    public void ParseResponseLine_DoneLine_ReturnsDone()
    {
        var line = """{"response": "", "done": true}""";
        var (token, done) = OllamaClient.ParseResponseLine(line);
        Assert.Equal("", token);
        Assert.True(done);
    }

    [Fact]
    public void ParseResponseLine_EmptyLine_ReturnsNull()
    {
        var (token, done) = OllamaClient.ParseResponseLine("");
        Assert.Null(token);
        Assert.False(done);
    }

    [Fact]
    public void ParseResponseLine_InvalidJson_ReturnsNull()
    {
        var (token, done) = OllamaClient.ParseResponseLine("not json");
        Assert.Null(token);
        Assert.False(done);
    }

    [Fact]
    public async Task StreamTokens_FromSampleNdjson_YieldsAllTokens()
    {
        var ndjson = """
            {"response": "The ", "done": false}
            {"response": "fan ", "done": false}
            {"response": "screams.", "done": false}
            {"response": "", "done": true}
            """;
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ndjson));
        var tokens = new List<string>();
        await foreach (var token in OllamaClient.ReadTokensFromStream(stream))
            tokens.Add(token);

        Assert.Equal(3, tokens.Count);
        Assert.Equal("The ", tokens[0]);
        Assert.Equal("fan ", tokens[1]);
        Assert.Equal("screams.", tokens[2]);
    }
}
