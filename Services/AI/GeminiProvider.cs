using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using fanfnir_back.DTOs;
using Microsoft.Extensions.Options;

namespace fanfnir_back.Services.AI;

public sealed class GeminiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<AiOptions> _options;
    private readonly ILogger<GeminiProvider> _logger;

    public string ProviderName => "Gemini";

    public GeminiProvider(
        HttpClient httpClient,
        IOptions<AiOptions> options,
        ILogger<GeminiProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<AiResponseDto> GenerateResponseAsync(AiPromptRequest request, CancellationToken ct)
    {
        var geminiConfig = _options.Value.Gemini;
        var apiKey = geminiConfig.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("Gemini API key não configurada.");
            return new AiResponseDto(
                Content: string.Empty,
                PromptTokens: 0,
                CandidatesTokens: 0,
                TotalTokens: 0,
                FinishReason: "CONFIG_ERROR",
                Success: false,
                ErrorMessage: "Chave da API Gemini não configurada.");
        }

        var primaryModel = string.IsNullOrWhiteSpace(geminiConfig.Model) ? "gemini-3.1-flash-lite" : geminiConfig.Model.Trim();
        var candidateModels = new[] { primaryModel, "gemini-3.1-flash-lite", "gemini-3.1-flash-lite-preview", "gemini-3-flash-preview", "gemini-3.7-flash" }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var baseUrl = string.IsNullOrWhiteSpace(geminiConfig.ApiUrl) 
            ? "https://generativelanguage.googleapis.com/v1beta/models" 
            : geminiConfig.ApiUrl.TrimEnd('/');

        // Build contents list ensuring valid alternating 'user' / 'model' turns
        var contents = new List<GeminiContent>();

        if (request.History != null)
        {
            foreach (var msg in request.History)
            {
                if (string.IsNullOrWhiteSpace(msg.Content)) continue;

                var role = msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ||
                           msg.Role.Equals("model", StringComparison.OrdinalIgnoreCase)
                    ? "model"
                    : "user";

                // Gemini API requires the first turn to be 'user'
                if (contents.Count == 0 && role == "model") continue;

                // Merge consecutive messages with the same role
                if (contents.Count > 0 && contents[^1].Role == role)
                {
                    contents[^1].Parts.Add(new GeminiPart { Text = msg.Content });
                }
                else
                {
                    contents.Add(new GeminiContent
                    {
                        Role = role,
                        Parts = new List<GeminiPart> { new() { Text = msg.Content } }
                    });
                }
            }
        }

        // Add the current prompt with context
        var formattedUserMessage = FafnirPrompts.FormatUserPromptWithContext(request.ContextJson, request.UserPrompt);
        if (contents.Count > 0 && contents[^1].Role == "user")
        {
            contents[^1].Parts.Add(new GeminiPart { Text = formattedUserMessage });
        }
        else
        {
            contents.Add(new GeminiContent
            {
                Role = "user",
                Parts = new List<GeminiPart> { new() { Text = formattedUserMessage } }
            });
        }

        var payload = new GeminiGenerateRequest
        {
            SystemInstruction = string.IsNullOrWhiteSpace(request.SystemInstruction)
                ? null
                : new GeminiContent
                {
                    Parts = new List<GeminiPart> { new() { Text = request.SystemInstruction } }
                },
            Contents = contents,
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = request.Temperature ?? geminiConfig.Temperature,
                MaxOutputTokens = request.MaxTokens ?? geminiConfig.MaxOutputTokens,
                ThinkingConfig = new GeminiThinkingConfig { ThinkingBudget = 0 }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var perAttemptTimeoutSeconds = Math.Clamp(geminiConfig.TimeoutSeconds > 0 ? geminiConfig.TimeoutSeconds : 12, 5, 20);

        foreach (var model in candidateModels)
        {
            var requestUrl = $"{baseUrl}/{model}:generateContent?key={apiKey}";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(perAttemptTimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                var response = await _httpClient.SendAsync(httpRequest, linkedCts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(CancellationToken.None);
                    _logger.LogWarning("Gemini API ({Model}) retornou erro HTTP {StatusCode}: {ErrorBody}", model, response.StatusCode, errorBody);

                    // If model not found (404) or high demand (503), try next candidate model
                    if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        continue;
                    }

                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        return new AiResponseDto(
                            Content: string.Empty,
                            PromptTokens: 0,
                            CandidatesTokens: 0,
                            TotalTokens: 0,
                            FinishReason: "RATE_LIMIT",
                            Success: false,
                            ErrorMessage: "Limite de requisições excedido temporariamente no provedor de IA.");
                    }

                    if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        return new AiResponseDto(
                            Content: string.Empty,
                            PromptTokens: 0,
                            CandidatesTokens: 0,
                            TotalTokens: 0,
                            FinishReason: "AUTH_ERROR",
                            Success: false,
                            ErrorMessage: "Erro de autenticação com a API da Gemini.");
                    }

                    return new AiResponseDto(
                        Content: string.Empty,
                        PromptTokens: 0,
                        CandidatesTokens: 0,
                        TotalTokens: 0,
                        FinishReason: "PROVIDER_ERROR",
                        Success: false,
                        ErrorMessage: $"Erro no provedor de IA (HTTP {response.StatusCode}): {errorBody}");
                }

            var responseJson = await response.Content.ReadAsStringAsync(linkedCts.Token);
            var geminiResponse = JsonSerializer.Deserialize<GeminiGenerateResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (geminiResponse == null)
            {
                return new AiResponseDto(
                    Content: string.Empty,
                    PromptTokens: 0,
                    CandidatesTokens: 0,
                    TotalTokens: 0,
                    FinishReason: "DESERIALIZATION_ERROR",
                    Success: false,
                    ErrorMessage: "Resposta do provedor de IA vazia ou inválida.");
            }

            var firstCandidate = geminiResponse.Candidates?.FirstOrDefault();
            var text = firstCandidate?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
            var finishReason = firstCandidate?.FinishReason ?? "STOP";

            var promptTokens = geminiResponse.UsageMetadata?.PromptTokenCount ?? 0;
            var candidatesTokens = geminiResponse.UsageMetadata?.CandidatesTokenCount ?? 0;
            var totalTokens = geminiResponse.UsageMetadata?.TotalTokenCount ?? (promptTokens + candidatesTokens);

            if (string.IsNullOrWhiteSpace(text) && firstCandidate?.FinishReason == "SAFETY")
            {
                return new AiResponseDto(
                    Content: "Não foi possível gerar uma resposta para esta solicitação devido a filtros de segurança.",
                    PromptTokens: promptTokens,
                    CandidatesTokens: candidatesTokens,
                    TotalTokens: totalTokens,
                    FinishReason: "SAFETY",
                    Success: true);
            }

            return new AiResponseDto(
                Content: text.Trim(),
                PromptTokens: promptTokens,
                CandidatesTokens: candidatesTokens,
                TotalTokens: totalTokens,
                FinishReason: finishReason,
                Success: true);
        }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                _logger.LogWarning("Timeout no modelo Gemini ({Model}) após {Seconds}s. Tentando próximo modelo...", model, perAttemptTimeoutSeconds);
                continue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exceção ao comunicar com a Gemini API ({Model}): {Message}", model, ex.Message);
                continue;
            }
        } // end foreach model

        return new AiResponseDto(
            Content: string.Empty,
            PromptTokens: 0,
            CandidatesTokens: 0,
            TotalTokens: 0,
            FinishReason: "MODELS_UNAVAILABLE",
            Success: false,
            ErrorMessage: "Nenhum modelo Gemini disponível no momento.");
    }

    #region Internal Gemini DTOs
    public class GeminiGenerateRequest
    {
        public GeminiContent? SystemInstruction { get; set; }
        public List<GeminiContent> Contents { get; set; } = new();
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    public class GeminiContent
    {
        public string? Role { get; set; }
        public List<GeminiPart> Parts { get; set; } = new();
    }

    public class GeminiPart
    {
        public string Text { get; set; } = string.Empty;
    }

    public class GeminiGenerationConfig
    {
        public double? Temperature { get; set; }
        public int? MaxOutputTokens { get; set; }
        public GeminiThinkingConfig? ThinkingConfig { get; set; }
    }

    public class GeminiThinkingConfig
    {
        public int ThinkingBudget { get; set; } = 0;
    }

    public class GeminiGenerateResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
        public GeminiUsageMetadata? UsageMetadata { get; set; }
    }

    public class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
        public string? FinishReason { get; set; }
    }

    public class GeminiUsageMetadata
    {
        public int PromptTokenCount { get; set; }
        public int CandidatesTokenCount { get; set; }
        public int TotalTokenCount { get; set; }
    }
    #endregion
}
