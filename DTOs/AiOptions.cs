namespace fanfnir_back.DTOs;

public class AiOptions
{
    public const string SectionName = "Ai";

    public string Provider { get; set; } = "Gemini";
    public GeminiOptions Gemini { get; set; } = new();
    public AiLimitsOptions Limits { get; set; } = new();
}

public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-3.1-flash-lite";
    public string ApiUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";
    public int TimeoutSeconds { get; set; } = 30;
    public double Temperature { get; set; } = 0.3;
    public int MaxOutputTokens { get; set; } = 1024;
}

public class AiLimitsOptions
{
    public int MaxHistoryMessages { get; set; } = 6;
    public int MaxQuestionLength { get; set; } = 500;
    public int MaxRecentTransactions { get; set; } = 5;
    public int MaxContextSummaryLength { get; set; } = 2000;
}
