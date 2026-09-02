using fanfnir_back.DTOs;
using fanfnir_back.Services.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace test;

public class FafnirServiceTests
{
    private IOptions<AiOptions> CreateAiOptions()
    {
        return Options.Create(new AiOptions
        {
            Gemini = new GeminiOptions
            {
                ApiKey = "fake-key",
                Model = "gemini-1.5-flash"
            },
            Limits = new AiLimitsOptions
            {
                MaxHistoryMessages = 4,
                MaxQuestionLength = 100,
                MaxRecentTransactions = 5
            }
        });
    }

    [Fact]
    public async Task ProcessQuestionAsync_WhenAiProviderSucceeds_ReturnsAiContent()
    {
        // Arrange
        var mockContextBuilder = new Mock<IFafnirContextBuilder>();
        var mockAiProvider = new Mock<IAiProvider>();

        mockAiProvider.Setup(p => p.ProviderName).Returns("Gemini");

        var dummyContext = new FafnirFinancialContext
        {
            Period = "2026-08",
            Intent = "financial_summary",
            Summary = new FinancialSummaryContext("2026-08", 5000m, 3000m, 2000m, 150m, 500m, 40.0m)
        };

        mockContextBuilder
            .Setup(b => b.GetRelevantContextAsync(1, It.IsAny<string>(), It.IsAny<IReadOnlyList<FafnirChatMessageDto>>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyContext);

        mockAiProvider
            .Setup(p => p.GenerateResponseAsync(It.IsAny<AiPromptRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponseDto(
                Content: "Parabéns! Suas finanças estão com um superávit de R$ 2.000 este mês.",
                PromptTokens: 150,
                CandidatesTokens: 45,
                TotalTokens: 195,
                FinishReason: "STOP",
                Success: true));

        var service = new FafnirService(
            mockContextBuilder.Object,
            mockAiProvider.Object,
            CreateAiOptions(),
            NullLogger<FafnirService>.Instance);

        // Act
        var result = await service.ProcessQuestionAsync(1, new FafnirChatRequestDto("Como estão minhas finanças?"), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Parabéns! Suas finanças estão com um superávit de R$ 2.000 este mês.", result.Data.Message);
        Assert.Equal("financial_summary", result.Data.Type);
        Assert.Equal("Gemini", result.Data.Provider);
        Assert.Equal(195, result.Data.TokensUsed);
        Assert.NotNull(result.Data.MinimalContextSummary);
    }

    [Fact]
    public async Task ProcessQuestionAsync_WhenAiProviderFails_ReturnsResilientCalculatedFallback()
    {
        // Arrange
        var mockContextBuilder = new Mock<IFafnirContextBuilder>();
        var mockAiProvider = new Mock<IAiProvider>();

        mockAiProvider.Setup(p => p.ProviderName).Returns("Gemini");

        var dummyContext = new FafnirFinancialContext
        {
            Period = "2026-08",
            Intent = "category_analysis",
            Category = new CategoryAnalysisContext(
                Period: "2026-08",
                CategoryName: "Alimentação",
                CurrentMonthAmount: 850.50m,
                TotalMonthExpenses: 2500m,
                ShareOfExpensesPercent: 34.0m,
                ShareOfIncomePercent: 20.0m,
                PreviousMonthAmount: 700m,
                MonthlyChangePercent: 21.5m,
                BudgetLimit: 800m,
                BudgetRemaining: -50.50m,
                IsOverBudget: true)
        };

        mockContextBuilder
            .Setup(b => b.GetRelevantContextAsync(1, It.IsAny<string>(), It.IsAny<IReadOnlyList<FafnirChatMessageDto>>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyContext);

        // Simulate AI provider outage / rate limit
        mockAiProvider
            .Setup(p => p.GenerateResponseAsync(It.IsAny<AiPromptRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponseDto(
                Content: string.Empty,
                PromptTokens: 0,
                CandidatesTokens: 0,
                TotalTokens: 0,
                FinishReason: "RATE_LIMIT",
                Success: false,
                ErrorMessage: "Rate limit exceeded"));

        var service = new FafnirService(
            mockContextBuilder.Object,
            mockAiProvider.Object,
            CreateAiOptions(),
            NullLogger<FafnirService>.Instance);

        // Act
        var result = await service.ProcessQuestionAsync(1, new FafnirChatRequestDto("Quanto gastei com alimentação?"), CancellationToken.None);

        // Assert - verify system returns 200 with fallback content instead of failing
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Contains("Alimentação", result.Data.Message);
        Assert.Contains("850,50", result.Data.Message);
        Assert.Contains("34,0%", result.Data.Message);
        Assert.Single(result.Data.Warnings);
        Assert.Contains("ultrapassou o orçamento", result.Data.Warnings[0]);
    }

    [Fact]
    public async Task ProcessQuestionAsync_Truncates_LongQuestion_And_LimitsHistory()
    {
        // Arrange
        var mockContextBuilder = new Mock<IFafnirContextBuilder>();
        var mockAiProvider = new Mock<IAiProvider>();
        mockAiProvider.Setup(p => p.ProviderName).Returns("Gemini");

        AiPromptRequest? capturedRequest = null;
        mockAiProvider
            .Setup(p => p.GenerateResponseAsync(It.IsAny<AiPromptRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AiPromptRequest, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new AiResponseDto("Ok", 10, 10, 20, "STOP", true));

        mockContextBuilder
            .Setup(b => b.GetRelevantContextAsync(1, It.IsAny<string>(), It.IsAny<IReadOnlyList<FafnirChatMessageDto>>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FafnirFinancialContext { Period = "2026-08", Intent = "financial_summary" });

        var service = new FafnirService(
            mockContextBuilder.Object,
            mockAiProvider.Object,
            CreateAiOptions(), // MaxQuestionLength = 100, MaxHistoryMessages = 4
            NullLogger<FafnirService>.Instance);

        var longQuestion = new string('A', 250);
        var manyHistoryItems = Enumerable.Range(1, 10)
            .Select(i => new FafnirChatMessageDto(i % 2 == 0 ? "assistant" : "user", $"Message {i}"))
            .ToList();

        // Act
        var result = await service.ProcessQuestionAsync(1, new FafnirChatRequestDto(longQuestion, manyHistoryItems), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(capturedRequest);
        Assert.Equal(100, capturedRequest.UserPrompt.Length); // Truncated to 100 chars
        Assert.Equal(4, capturedRequest.History.Count); // Capped to 4 items
    }
}
