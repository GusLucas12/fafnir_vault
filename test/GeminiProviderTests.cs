using System.Net;
using System.Text.Json;
using fanfnir_back.DTOs;
using fanfnir_back.Services.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace test;

public class GeminiProviderTests
{
    private IOptions<AiOptions> CreateOptions(string apiKey = "test-gemini-key")
    {
        return Options.Create(new AiOptions
        {
            Gemini = new GeminiOptions
            {
                ApiKey = apiKey,
                Model = "gemini-1.5-flash",
                ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models",
                TimeoutSeconds = 5
            }
        });
    }

    [Fact]
    public async Task GenerateResponseAsync_WhenApiKeyIsMissing_ReturnsConfigError()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new GeminiProvider(httpClient, CreateOptions(apiKey: ""), NullLogger<GeminiProvider>.Instance);

        var request = new AiPromptRequest("System", "{}", new List<FafnirChatMessageDto>(), "Oi");

        // Act
        var response = await provider.GenerateResponseAsync(request, CancellationToken.None);

        // Assert
        Assert.False(response.Success);
        Assert.Equal("CONFIG_ERROR", response.FinishReason);
        Assert.Contains("não configurada", response.ErrorMessage);
    }

    [Fact]
    public async Task GenerateResponseAsync_WhenGeminiReturns200_ParsesContentAndTokens()
    {
        // Arrange
        var fakeJsonResponse = @"
        {
            ""candidates"": [
                {
                    ""content"": {
                        ""parts"": [
                            { ""text"": ""Seus gastos totais foram de R$ 1.500."" }
                        ],
                        ""role"": ""model""
                    },
                    ""finishReason"": ""STOP""
                }
            ],
            ""usageMetadata"": {
                ""promptTokenCount"": 110,
                ""candidatesTokenCount"": 35,
                ""totalTokenCount"": 145
            }
        }";

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(fakeJsonResponse)
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new GeminiProvider(httpClient, CreateOptions(), NullLogger<GeminiProvider>.Instance);

        var request = new AiPromptRequest(
            SystemInstruction: "Instrução de teste",
            ContextJson: "{\"total\":1500}",
            History: new List<FafnirChatMessageDto>(),
            UserPrompt: "Quanto gastei?");

        // Act
        var result = await provider.GenerateResponseAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Seus gastos totais foram de R$ 1.500.", result.Content);
        Assert.Equal(110, result.PromptTokens);
        Assert.Equal(35, result.CandidatesTokens);
        Assert.Equal(145, result.TotalTokens);
        Assert.Equal("STOP", result.FinishReason);
    }

    [Fact]
    public async Task GenerateResponseAsync_WhenGeminiReturns429_ReturnsRateLimitError()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.TooManyRequests,
                Content = new StringContent("Quota exceeded")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new GeminiProvider(httpClient, CreateOptions(), NullLogger<GeminiProvider>.Instance);

        var request = new AiPromptRequest("System", "{}", new List<FafnirChatMessageDto>(), "Pergunta");

        // Act
        var result = await provider.GenerateResponseAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("RATE_LIMIT", result.FinishReason);
        Assert.Contains("Limite de requisições excedido", result.ErrorMessage);
    }

    [Fact]
    public async Task GenerateResponseAsync_WhenBlockedBySafety_ReturnsSafetyExplanation()
    {
        // Arrange
        var fakeJsonResponse = @"
        {
            ""candidates"": [
                {
                    ""content"": {
                        ""parts"": [],
                        ""role"": ""model""
                    },
                    ""finishReason"": ""SAFETY""
                }
            ],
            ""usageMetadata"": {
                ""promptTokenCount"": 50,
                ""candidatesTokenCount"": 0,
                ""totalTokenCount"": 50
            }
        }";

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(fakeJsonResponse)
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new GeminiProvider(httpClient, CreateOptions(), NullLogger<GeminiProvider>.Instance);

        var request = new AiPromptRequest("System", "{}", new List<FafnirChatMessageDto>(), "Pergunta");

        // Act
        var result = await provider.GenerateResponseAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("SAFETY", result.FinishReason);
        Assert.Contains("filtros de segurança", result.Content);
    }
}
