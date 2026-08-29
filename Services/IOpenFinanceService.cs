using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using fanfnir_back.DTOs;

namespace fanfnir_back.Services;

public interface IOpenFinanceService
{
    Task<OpenFinanceConnectResponseDto> StartConnectionAsync(int userId, string? itemId, CancellationToken ct);
    Task<IReadOnlyList<OpenFinanceConnectionDto>> GetConnectionsAsync(int userId, CancellationToken ct);
    Task<OpenFinanceConnectionDto?> GetConnectionByIdAsync(int userId, int connectionId, CancellationToken ct);
    Task<bool> DeleteConnectionAsync(int userId, int connectionId, CancellationToken ct);
    Task<IReadOnlyList<BankAccountResponseDto>> GetBankAccountsAsync(int userId, CancellationToken ct);
    Task<BankAccountResponseDto?> GetBankAccountByIdAsync(int userId, int accountId, CancellationToken ct);
    Task<IReadOnlyList<BankTransactionResponseDto>> GetTransactionsAsync(int userId, int? bankAccountId, int? limit, int? offset, CancellationToken ct);
    Task<BankTransactionResponseDto?> GetTransactionByIdAsync(int userId, int transactionId, CancellationToken ct);
    Task<BankAccountResponseDto?> SyncBankAccountAsync(int userId, int accountId, CancellationToken ct);
    Task SyncUserAccountsAsync(int userId, CancellationToken ct);
    Task ProcessWebhookAsync(PluggyWebhookPayloadDto payload, CancellationToken ct);
}
