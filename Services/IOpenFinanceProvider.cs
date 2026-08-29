using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using fanfnir_back.DTOs;

namespace fanfnir_back.Services;

public interface IOpenFinanceProvider
{
    Task<string> GetConnectTokenAsync(string? itemId, string? clientUserId, string? webhookUrl, string? redirectUri, CancellationToken ct);
    Task<OpenFinanceItemDto> GetItemAsync(string itemId, CancellationToken ct);
    Task<IReadOnlyList<OpenFinanceAccountDto>> GetAccountsAsync(string itemId, CancellationToken ct);
    Task<OpenFinanceTransactionsResponseDto> GetTransactionsAsync(string accountId, string? cursor, DateTime? fromDate, CancellationToken ct);
    Task DeleteItemAsync(string itemId, CancellationToken ct);
}
