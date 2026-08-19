using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DgSales.Api.Integrations.WhatsApp;

public sealed record WhatsAppInboundText(string Id, string From, string Text);

public sealed class WhatsAppWebhookService(IConfiguration config)
{
    public bool Verify(ReadOnlySpan<byte> body, string? supplied)
    {
        var secret = config["WhatsApp:AppSecret"];
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(supplied) || !supplied.StartsWith("sha256=", StringComparison.Ordinal)) return false;
        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body);
        var actual = new byte[expected.Length];
        return Convert.TryFromHexString(supplied[7..], actual, out var written)
            && written == expected.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public IReadOnlyList<WhatsAppInboundText> ReadMessages(byte[] body)
    {
        using var json = JsonDocument.Parse(body);
        var result = new List<WhatsAppInboundText>();
        if (!json.RootElement.TryGetProperty("entry", out var entries)) return result;
        foreach (var entry in entries.EnumerateArray())
        foreach (var change in entry.GetProperty("changes").EnumerateArray())
        {
            var value = change.GetProperty("value");
            if (!value.TryGetProperty("messages", out var messages)) continue;
            foreach (var message in messages.EnumerateArray())
                if (message.TryGetProperty("text", out var text) && text.TryGetProperty("body", out var content))
                    result.Add(new(message.GetProperty("id").GetString()!, message.GetProperty("from").GetString()!, content.GetString() ?? ""));
        }
        return result;
    }
}
