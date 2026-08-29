using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using fanfnir_back.DTOs;
using fanfnir_back.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace fanfnir_back.Controllers;

[ApiController]
public sealed class OpenFinanceController(
    IOpenFinanceService service,
    IConfiguration configuration) : FafnirControllerBase
{
    private bool TryGetAuthenticatedUser(out int userId, out ActionResult unauthorizedResult)
    {
        userId = 0;
        unauthorizedResult = null!;

        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            unauthorizedResult = Unauthorized(new { message = "Token de autorização ausente." });
            return false;
        }

        var token = authHeader.ToString().Replace("Bearer ", "").Trim();
        if (!TokenSigner.Verify(token, configuration, out userId))
        {
            unauthorizedResult = Unauthorized(new { message = "Sessão inválida ou expirada. Entre novamente." });
            return false;
        }

        return true;
    }

    [HttpPost("api/open-finance/connect")]
    public async Task<ActionResult<OpenFinanceConnectResponseDto>> StartConnection([FromBody] OpenFinanceConnectRequest request, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUser(out var userId, out var authError)) return authError;

        try
        {
            var result = await service.StartConnectionAsync(userId, request.ItemId, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Unexpected(ex);
        }
    }

    [HttpGet("api/open-finance/connections")]
    public async Task<ActionResult<IReadOnlyList<OpenFinanceConnectionDto>>> GetConnections(CancellationToken ct)
    {
        if (!TryGetAuthenticatedUser(out var userId, out var authError)) return authError;

        try
        {
            var result = await service.GetConnectionsAsync(userId, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Unexpected(ex);
        }
    }

    [HttpGet("api/open-finance/connections/{id:int}")]
    public async Task<ActionResult<OpenFinanceConnectionDto>> GetConnectionById(int id, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUser(out var userId, out var authError)) return authError;

        try
        {
            var conn = await service.GetConnectionByIdAsync(userId, id, ct);
            if (conn == null) return NotFound(new { message = "Conexão não encontrada." });
            return Ok(conn);
        }
        catch (Exception ex)
        {
            return Unexpected(ex);
        }
    }

    [HttpDelete("api/open-finance/connections/{id:int}")]
    public async Task<IActionResult> DeleteConnection(int id, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUser(out var userId, out var authError)) return authError;

        try
        {
            var deleted = await service.DeleteConnectionAsync(userId, id, ct);
            if (!deleted) return NotFound(new { message = "Conexão não encontrada." });
            return NoContent();
        }
        catch (Exception ex)
        {
            return Unexpected(ex);
        }
    }

    [HttpGet("api/bank-accounts")]
    public async Task<ActionResult<IReadOnlyList<BankAccountResponseDto>>> GetBankAccounts(CancellationToken ct)
    {
        if (!TryGetAuthenticatedUser(out var userId, out var authError)) return authError;

        try
        {
            var result = await service.GetBankAccountsAsync(userId, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Unexpected(ex);
        }
    }

    [HttpGet("api/bank-accounts/{id:int}")]
    public async Task<ActionResult<BankAccountResponseDto>> GetBankAccountById(int id, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUser(out var userId, out var authError)) return authError;

        try
        {
            var acc = await service.GetBankAccountByIdAsync(userId, id, ct);
            if (acc == null) return NotFound(new { message = "Conta bancária não encontrada." });
            return Ok(acc);
        }
        catch (Exception ex)
        {
            return Unexpected(ex);
        }
    }

    [HttpGet("api/transactions")]
    public async Task<ActionResult<IReadOnlyList<BankTransactionResponseDto>>> GetTransactions(
        [FromQuery] int? bankAccountId,
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUser(out var userId, out var authError)) return authError;

        try
        {
            var result = await service.GetTransactionsAsync(userId, bankAccountId, limit, offset, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Unexpected(ex);
        }
    }

    [HttpGet("api/transactions/{id:int}")]
    public async Task<ActionResult<BankTransactionResponseDto>> GetTransactionById(int id, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUser(out var userId, out var authError)) return authError;

        try
        {
            var tx = await service.GetTransactionByIdAsync(userId, id, ct);
            if (tx == null) return NotFound(new { message = "Transação não encontrada." });
            return Ok(tx);
        }
        catch (Exception ex)
        {
            return Unexpected(ex);
        }
    }

    [HttpPost("api/bank-accounts/{id:int}/sync")]
    public async Task<ActionResult<BankAccountResponseDto>> SyncBankAccount(int id, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUser(out var userId, out var authError)) return authError;

        try
        {
            var result = await service.SyncBankAccountAsync(userId, id, ct);
            if (result == null) return NotFound(new { message = "Conta bancária não encontrada." });
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Unexpected(ex);
        }
    }

    [HttpPost("api/open-finance/webhook")]
    public async Task<IActionResult> Webhook([FromBody] PluggyWebhookPayloadDto payload, CancellationToken ct)
    {
        try
        {
            // Webhook endpoints are unauthenticated calls from Pluggy
            await service.ProcessWebhookAsync(payload, ct);
            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Erro ao processar webhook do Pluggy: {ex.Message}");
            return Ok(); // Always return 200 to aggregator so they don't retry repeatedly on validation issues
        }
    }
}

public record OpenFinanceConnectRequest(string? ItemId);
