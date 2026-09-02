using fanfnir_back.DTOs;

namespace fanfnir_back.Services.AI;

public record AiPromptRequest(
    string SystemInstruction,
    string ContextJson,
    IReadOnlyList<FafnirChatMessageDto> History,
    string UserPrompt,
    double? Temperature = null,
    int? MaxTokens = null);

public record AiResponseDto(
    string Content,
    int PromptTokens,
    int CandidatesTokens,
    int TotalTokens,
    string? FinishReason,
    bool Success,
    string? ErrorMessage = null);

public interface IAiProvider
{
    string ProviderName { get; }
    Task<AiResponseDto> GenerateResponseAsync(AiPromptRequest request, CancellationToken ct);
}
