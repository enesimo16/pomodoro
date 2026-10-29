namespace Focus.Infrastructure.Common;

public class AiSettings
{
    public const string SectionName = "Ai";

    public GeminiSettings Gemini { get; set; } = new();
    public int DailyTokenBudgetPerUser { get; set; } = 20000;

    public bool IsGeminiConfigured => !string.IsNullOrWhiteSpace(Gemini.ApiKey);
}

public class GeminiSettings
{
    public const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/";

    public string ApiKey { get; set; } = string.Empty;
    public string ChatModelId { get; set; } = "gemini-flash-latest";
    public string EmbeddingModelId { get; set; } = "gemini-embedding-001";
    public int EmbeddingDimensions { get; set; } = 768;
    public int TimeoutSeconds { get; set; } = 15;
}
