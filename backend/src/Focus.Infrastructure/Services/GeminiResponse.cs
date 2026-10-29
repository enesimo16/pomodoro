using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Focus.Infrastructure.Services;

internal static class GeminiResponse
{
    private const int MaxLoggedBodyLength = 500;

    // Basarisiz yanitlari durum kodu ve kisaltilmis govde ile loglar; basarili yanitta JsonDocument dondurur
    public static async Task<JsonDocument?> ReadAsync(
        HttpResponseMessage response,
        ILogger logger,
        string operation,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (body.Length > MaxLoggedBodyLength)
            {
                body = body[..MaxLoggedBodyLength];
            }

            logger.LogWarning("Gemini {Operation} istegi basarisiz: {StatusCode} {Body}", operation, (int)response.StatusCode, body);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    public static string? ExtractText(JsonElement root)
    {
        if (root.TryGetProperty("candidates", out var candidates)
            && candidates.ValueKind == JsonValueKind.Array
            && candidates.GetArrayLength() > 0
            && candidates[0].TryGetProperty("content", out var content)
            && content.TryGetProperty("parts", out var parts)
            && parts.ValueKind == JsonValueKind.Array
            && parts.GetArrayLength() > 0
            && parts[0].TryGetProperty("text", out var text))
        {
            return text.GetString();
        }

        return null;
    }
}
