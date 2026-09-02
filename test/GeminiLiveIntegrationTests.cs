using fanfnir_back.DTOs;
using fanfnir_back.Services.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace test;

public class GeminiLiveIntegrationTests
{
    [Fact(Skip = "Teste de integração ao vivo para quando uma chave com cota ativa da Google AI Studio for informada")]
    public async Task LiveTest_GeminiGeneratesResponse()
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "fake-api-key-for-testing";
        var options = Options.Create(new AiOptions
        {
            Gemini = new GeminiOptions
            {
                ApiKey = apiKey,
                Model = "gemini-1.5-flash",
                TimeoutSeconds = 30
            }
        });

        using var httpClient = new HttpClient();
        var provider = new GeminiProvider(httpClient, options, NullLogger<GeminiProvider>.Instance);

        var request = new AiPromptRequest(
            SystemInstruction: FafnirPrompts.GetSystemPrompt(),
            ContextJson: "{\"period\":\"2026-09\",\"income\":5000,\"expenses\":3200,\"netBalance\":1800}",
            History: new List<FafnirChatMessageDto>(),
            UserPrompt: "Quanto sobrou do meu salário esse mês?");

        var response = await provider.GenerateResponseAsync(request, CancellationToken.None);

        if (!response.Success)
        {
            throw new Exception($"Gemini Live Failure: Reason={response.FinishReason}, Error={response.ErrorMessage}");
        }

        Assert.True(response.Success);
        Assert.NotEmpty(response.Content);
        Assert.True(response.TotalTokens > 0);
    }
}
