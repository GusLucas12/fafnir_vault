using fanfnir_back.DTOs;

namespace fanfnir_back.Services.AI;

public interface IFafnirService
{
    Task<ServiceResult<FafnirChatResponseDto>> ProcessQuestionAsync(
        int usuarioId,
        FafnirChatRequestDto request,
        CancellationToken ct);
}
