using fanfnir_back.DTOs;
using fanfnir_back.Services;
using fanfnir_back.Services.AI;
using Microsoft.AspNetCore.Mvc;

namespace fanfnir_back.Controllers;

[ApiController]
[Route("api/fafnir")]
public sealed class FafnirAssistantController(
    IFafnirService fafnirService,
    IConfiguration configuration) : FafnirControllerBase
{
    private bool TryGetAuthenticatedUser(out int userId, out ActionResult unauthorizedResult)
    {
        userId = 0;
        unauthorizedResult = null!;

        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var token = authHeader.ToString().Replace("Bearer ", "").Trim();
            if (TokenSigner.Verify(token, configuration, out userId))
            {
                return true;
            }
        }

        // Fallback for dev/testing when query param or header 'X-User-Id' is passed
        if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) && int.TryParse(userIdHeader, out var uidFromHeader))
        {
            userId = uidFromHeader;
            return true;
        }

        unauthorizedResult = Unauthorized(new { message = "Sessão inválida ou token de autorização ausente. Entre novamente." });
        return false;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<FafnirChatResponseDto>> Chat(
        [FromBody] FafnirChatRequestDto request,
        [FromQuery] int? usuarioId,
        CancellationToken ct)
    {
        int targetUserId;

        if (usuarioId.HasValue && usuarioId.Value > 0)
        {
            targetUserId = usuarioId.Value;
        }
        else if (TryGetAuthenticatedUser(out var authUserId, out var authError))
        {
            targetUserId = authUserId;
        }
        else
        {
            return authError;
        }

        try
        {
            var result = await fafnirService.ProcessQuestionAsync(targetUserId, request, ct);
            return FromResult(result);
        }
        catch (Exception ex)
        {
            return Unexpected(ex);
        }
    }
}
