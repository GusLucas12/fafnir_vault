using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using fanfnir_back.DTOs;
using fanfnir_back.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fanfnir_back.Services;

public sealed class OpenFinanceService : IOpenFinanceService
{
    private readonly FafnirContext _db;
    private readonly IOpenFinanceProvider _provider;
    private readonly ILogger<OpenFinanceService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider; // Used to create scopes for background sync tasks

    public OpenFinanceService(
        FafnirContext db,
        IOpenFinanceProvider provider,
        ILogger<OpenFinanceService> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _db = db;
        _provider = provider;
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    private static OpenFinanceConnectionDto MapToDto(OpenFinanceConnection e) => new(
        e.Id,
        e.FkIdUsuario,
        e.Provedor,
        e.ProvedorItemId,
        e.Status,
        e.InstituicaoId,
        e.InstituicaoNome,
        e.DataCriacao,
        e.DataAtualizacao
    );

    private static BankAccountResponseDto MapToDto(BankAccount e) => new(
        e.Id,
        e.FkIdUsuario,
        e.FkIdConexao,
        e.Provedor,
        e.ProvedorContaId,
        e.InstituicaoId,
        e.InstituicaoNome,
        e.Tipo,
        e.Nome,
        e.Moeda,
        e.SaldoAtual,
        e.SaldoDisponivel,
        e.UltimaSincronizacao,
        e.Status,
        e.DataCriacao,
        e.DataAtualizacao
    );

    private static BankTransactionResponseDto MapToDto(BankTransaction e) => new(
        e.Id,
        e.FkIdUsuario,
        e.FkIdContaBancaria,
        e.Provedor,
        e.ProvedorTransacaoId,
        e.DataTransacao,
        e.Valor,
        e.Descricao,
        e.EstabelecimentoNome,
        e.Tipo,
        e.Moeda,
        e.FkIdCategoria,
        e.Metadata,
        e.DataCriacao,
        e.DataAtualizacao
    );

    public async Task<OpenFinanceConnectResponseDto> StartConnectionAsync(int userId, string? itemId, CancellationToken ct)
    {
        var webhookUrl = _configuration["OpenFinance:WebhookUrl"];
        var redirectUri = _configuration["OpenFinance:RedirectUri"];

        _logger.LogInformation("Iniciando conexão bancária para o usuário {UserId} (Item={ItemId})", userId, itemId);
        var token = await _provider.GetConnectTokenAsync(itemId, userId.ToString(), webhookUrl, redirectUri, ct);

        return new OpenFinanceConnectResponseDto(token);
    }

    public async Task<IReadOnlyList<OpenFinanceConnectionDto>> GetConnectionsAsync(int userId, CancellationToken ct)
    {
        var list = await _db.OpenFinanceConexoes
            .AsNoTracking()
            .Where(x => x.FkIdUsuario == userId)
            .OrderByDescending(x => x.DataCriacao)
            .ToListAsync(ct);

        return list.Select(MapToDto).ToList();
    }

    public async Task<OpenFinanceConnectionDto?> GetConnectionByIdAsync(int userId, int connectionId, CancellationToken ct)
    {
        var conn = await _db.OpenFinanceConexoes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == connectionId && x.FkIdUsuario == userId, ct);

        return conn == null ? null : MapToDto(conn);
    }

    public async Task<bool> DeleteConnectionAsync(int userId, int connectionId, CancellationToken ct)
    {
        var conn = await _db.OpenFinanceConexoes
            .FirstOrDefaultAsync(x => x.Id == connectionId && x.FkIdUsuario == userId, ct);

        if (conn == null) return false;

        _logger.LogInformation("Removendo conexão {ConnectionId} (Item={ItemId}) do usuário {UserId}...", connectionId, conn.ProvedorItemId, userId);

        try
        {
            await _provider.DeleteItemAsync(conn.ProvedorItemId, ct);
        }
        catch (Exception ex)
        {
            // If the item doesn't exist on the provider anymore, we can still proceed with local deletion
            _logger.LogWarning("Não foi possível excluir o item {ItemId} no provedor: {Error}", conn.ProvedorItemId, ex.Message);
        }

        _db.OpenFinanceConexoes.Remove(conn);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Conexão {ConnectionId} removida com sucesso localmente.", connectionId);
        return true;
    }

    public async Task<IReadOnlyList<BankAccountResponseDto>> GetBankAccountsAsync(int userId, CancellationToken ct)
    {
        var list = await _db.ContasBancarias
            .AsNoTracking()
            .Where(x => x.FkIdUsuario == userId)
            .OrderBy(x => x.Nome)
            .ToListAsync(ct);

        return list.Select(MapToDto).ToList();
    }

    public async Task<BankAccountResponseDto?> GetBankAccountByIdAsync(int userId, int accountId, CancellationToken ct)
    {
        var acc = await _db.ContasBancarias
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == accountId && x.FkIdUsuario == userId, ct);

        return acc == null ? null : MapToDto(acc);
    }

    public async Task<IReadOnlyList<BankTransactionResponseDto>> GetTransactionsAsync(int userId, int? bankAccountId, int? limit, int? offset, CancellationToken ct)
    {
        var query = _db.TransacoesBancarias
            .AsNoTracking()
            .Where(x => x.FkIdUsuario == userId);

        if (bankAccountId.HasValue)
        {
            query = query.Where(x => x.FkIdContaBancaria == bankAccountId.Value);
        }

        query = query.OrderByDescending(x => x.DataTransacao);

        if (offset.HasValue) query = query.Skip(offset.Value);
        if (limit.HasValue) query = query.Take(limit.Value);

        var list = await query.ToListAsync(ct);
        return list.Select(MapToDto).ToList();
    }

    public async Task<BankTransactionResponseDto?> GetTransactionByIdAsync(int userId, int transactionId, CancellationToken ct)
    {
        var tx = await _db.TransacoesBancarias
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == transactionId && x.FkIdUsuario == userId, ct);

        return tx == null ? null : MapToDto(tx);
    }

    public async Task<BankAccountResponseDto?> SyncBankAccountAsync(int userId, int accountId, CancellationToken ct)
    {
        var acc = await _db.ContasBancarias
            .Include(x => x.FkIdConexaoNavigation)
            .FirstOrDefaultAsync(x => x.Id == accountId && x.FkIdUsuario == userId, ct);

        if (acc == null) return null;

        _logger.LogInformation("Iniciando sincronização para a conta {AccountId} ({AccountName}) do usuário {UserId}...", accountId, acc.Nome, userId);

        // Fetch transactions from the provider using cursor-based pagination
        var fromDate = acc.UltimaSincronizacao?.AddDays(-3) ?? DateTime.UtcNow.AddDays(-90);
        var cursor = (string?)null;
        var importedTransactions = new List<OpenFinanceTransactionDto>();

        do
        {
            var pageResult = await _provider.GetTransactionsAsync(acc.ProvedorContaId, cursor, fromDate, ct);
            if (pageResult.Results == null || pageResult.Results.Count == 0) break;

            importedTransactions.AddRange(pageResult.Results);
            cursor = pageResult.Next;
        }
        while (!string.IsNullOrWhiteSpace(cursor));

        _logger.LogInformation("Importadas {Count} transações do provedor para a conta {AccountId}.", importedTransactions.Count, accountId);

        // Load valid categories to use in mapping classification
        var categories = await _db.Categorias
            .AsNoTracking()
            .Where(x => x.FkIdUsuario == userId || x.FkIdUsuario == null)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var newCount = 0;
        var updateCount = 0;

        foreach (var txDto in importedTransactions)
        {
            var dbTx = await _db.TransacoesBancarias
                .FirstOrDefaultAsync(x => x.Provedor == acc.Provedor && x.ProvedorTransacaoId == txDto.Id && x.FkIdContaBancaria == accountId, ct);

            var catId = ClassificarTransacao(txDto.Description, categories);

            if (dbTx == null)
            {
                dbTx = new BankTransaction
                {
                    FkIdUsuario = userId,
                    FkIdContaBancaria = accountId,
                    Provedor = acc.Provedor,
                    ProvedorTransacaoId = txDto.Id,
                    DataTransacao = txDto.Date,
                    Valor = Math.Abs(txDto.Amount),
                    Descricao = txDto.Description,
                    EstabelecimentoNome = txDto.MerchantName,
                    Tipo = txDto.Type.ToUpperInvariant() == "CREDIT" ? "RECEITA" : "DESPESA",
                    Moeda = txDto.CurrencyCode,
                    FkIdCategoria = catId,
                    Metadata = JsonSerializer.Serialize(new { categoryFromBank = txDto.Category, status = txDto.Status }),
                    DataCriacao = now,
                    DataAtualizacao = now
                };
                _db.TransacoesBancarias.Add(dbTx);
                newCount++;
            }
            else
            {
                dbTx.DataTransacao = txDto.Date;
                dbTx.Valor = Math.Abs(txDto.Amount);
                dbTx.Descricao = txDto.Description;
                dbTx.EstabelecimentoNome = txDto.MerchantName;
                dbTx.Tipo = txDto.Type.ToUpperInvariant() == "CREDIT" ? "RECEITA" : "DESPESA";
                dbTx.Moeda = txDto.CurrencyCode;
                dbTx.FkIdCategoria ??= catId; // Preserve custom categorization if already classified
                dbTx.Metadata = JsonSerializer.Serialize(new { categoryFromBank = txDto.Category, status = txDto.Status });
                dbTx.DataAtualizacao = now;
                updateCount++;
            }
        }

        // Retrieve latest account balance from the provider
        try
        {
            var provAccounts = await _provider.GetAccountsAsync(acc.FkIdConexaoNavigation.ProvedorItemId, ct);
            var matchingAcc = provAccounts.FirstOrDefault(x => x.Id == acc.ProvedorContaId);
            if (matchingAcc != null)
            {
                acc.SaldoAtual = matchingAcc.Balance;
                acc.SaldoDisponivel = matchingAcc.Balance; // Set available if needed
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Não foi possível atualizar o saldo da conta {AccountId} na sincronização: {Error}", accountId, ex.Message);
        }

        acc.UltimaSincronizacao = now;
        acc.DataAtualizacao = now;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Sincronização concluída para a conta {AccountId}. Novas: {New}, Atualizadas: {Updated}.", accountId, newCount, updateCount);

        return MapToDto(acc);
    }

    public async Task SyncUserAccountsAsync(int userId, CancellationToken ct)
    {
        var accounts = await _db.ContasBancarias
            .Where(x => x.FkIdUsuario == userId && x.Status == "ACTIVE")
            .ToListAsync(ct);

        _logger.LogInformation("Iniciando sincronização em lote para o usuário {UserId} ({Count} contas encontradas).", userId, accounts.Count);
        foreach (var acc in accounts)
        {
            try
            {
                await SyncBankAccountAsync(userId, acc.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao sincronizar conta {AccountId} do usuário {UserId}.", acc.Id, userId);
            }
        }
    }

    public async Task ProcessWebhookAsync(PluggyWebhookPayloadDto payload, CancellationToken ct)
    {
        _logger.LogInformation("Recebido webhook do Pluggy: Event={Event}, ItemId={ItemId}, ClientUserId={ClientUserId}", payload.Event, payload.ItemId, payload.ClientUserId);

        var itemId = payload.ItemId.ToString();
        var userIdVal = 0;
        if (!string.IsNullOrWhiteSpace(payload.ClientUserId))
        {
            int.TryParse(payload.ClientUserId, out userIdVal);
        }

        if (userIdVal <= 0)
        {
            // Try to find the connection in the DB to extract the user
            var existingConn = await _db.OpenFinanceConexoes.FirstOrDefaultAsync(x => x.ProvedorItemId == itemId, ct);
            if (existingConn != null)
            {
                userIdVal = existingConn.FkIdUsuario;
            }
        }

        if (userIdVal <= 0)
        {
            _logger.LogWarning("Ignorando webhook: não foi possível identificar o usuário para o ItemId={ItemId}", itemId);
            return;
        }

        // Fetch the Item state from Pluggy
        var itemDto = await _provider.GetItemAsync(itemId, ct);

        // Fetch or create the connection in DB
        var conn = await _db.OpenFinanceConexoes.FirstOrDefaultAsync(x => x.ProvedorItemId == itemId, ct);
        var now = DateTime.UtcNow;

        if (conn == null)
        {
            conn = new OpenFinanceConnection
            {
                FkIdUsuario = userIdVal,
                Provedor = "Pluggy",
                ProvedorItemId = itemId,
                Status = itemDto.Status.ToUpperInvariant(),
                InstituicaoId = itemDto.ConnectorId,
                InstituicaoNome = itemDto.ConnectorName,
                DataCriacao = now,
                DataAtualizacao = now
            };
            _db.OpenFinanceConexoes.Add(conn);
        }
        else
        {
            conn.Status = itemDto.Status.ToUpperInvariant();
            conn.InstituicaoId ??= itemDto.ConnectorId;
            conn.InstituicaoNome ??= itemDto.ConnectorName;
            conn.DataAtualizacao = now;
        }

        await _db.SaveChangesAsync(ct);

        // Handle specific statuses
        if (conn.Status == "UPDATED")
        {
            // Sync accounts
            var importedAccounts = await _provider.GetAccountsAsync(itemId, ct);
            var localAccounts = await _db.ContasBancarias.Where(x => x.FkIdConexao == conn.Id).ToListAsync(ct);

            foreach (var accDto in importedAccounts)
            {
                var localAcc = localAccounts.FirstOrDefault(x => x.ProvedorContaId == accDto.Id);
                if (localAcc == null)
                {
                    localAcc = new BankAccount
                    {
                        FkIdUsuario = userIdVal,
                        FkIdConexao = conn.Id,
                        Provedor = "Pluggy",
                        ProvedorContaId = accDto.Id,
                        InstituicaoId = conn.InstituicaoId ?? "unknown",
                        InstituicaoNome = conn.InstituicaoNome ?? "Instituição",
                        Tipo = accDto.Type,
                        Nome = accDto.Name,
                        Moeda = accDto.CurrencyCode,
                        SaldoAtual = accDto.Balance,
                        SaldoDisponivel = accDto.Balance,
                        Status = "ACTIVE",
                        DataCriacao = now,
                        DataAtualizacao = now
                    };
                    _db.ContasBancarias.Add(localAcc);
                }
                else
                {
                    localAcc.Nome = accDto.Name;
                    localAcc.SaldoAtual = accDto.Balance;
                    localAcc.SaldoDisponivel = accDto.Balance;
                    localAcc.Status = "ACTIVE";
                    localAcc.DataAtualizacao = now;
                }
            }

            await _db.SaveChangesAsync(ct);

            // Trigger sync for each active account asynchronously in a background task
            TriggerBackgroundSync(userIdVal, conn.Id);
        }
        else if (conn.Status == "LOGIN_ERROR" || conn.Status == "OUTDATED")
        {
            _logger.LogWarning("Conexão {ItemId} está com status {Status}. Ação do usuário é requerida.", itemId, conn.Status);
        }
    }

    private void TriggerBackgroundSync(int userId, int connectionId)
    {
        // Execute the synchronization in a separate thread/Task to not block webhook response
        Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<FafnirContext>();
            
            // Reload connection and accounts inside scope DB context
            var accounts = await dbContext.ContasBancarias
                .Where(x => x.FkIdConexao == connectionId && x.FkIdUsuario == userId)
                .ToListAsync();

            var openFinanceProvider = scope.ServiceProvider.GetRequiredService<IOpenFinanceProvider>();
            
            _logger.LogInformation("[Background Sync] Iniciando sincronização em lote para {Count} contas.", accounts.Count);

            foreach (var acc in accounts)
            {
                try
                {
                    var fromDate = acc.UltimaSincronizacao?.AddDays(-3) ?? DateTime.UtcNow.AddDays(-90);
                    var cursor = (string?)null;
                    var importedTransactions = new List<OpenFinanceTransactionDto>();

                    do
                    {
                        var pageResult = await openFinanceProvider.GetTransactionsAsync(acc.ProvedorContaId, cursor, fromDate, CancellationToken.None);
                        if (pageResult.Results == null || pageResult.Results.Count == 0) break;

                        importedTransactions.AddRange(pageResult.Results);
                        cursor = pageResult.Next;
                    }
                    while (!string.IsNullOrWhiteSpace(cursor));

                    var categories = await dbContext.Categorias
                        .AsNoTracking()
                        .Where(x => x.FkIdUsuario == userId || x.FkIdUsuario == null)
                        .ToListAsync();

                    var syncTime = DateTime.UtcNow;
                    var inserted = 0;

                    foreach (var txDto in importedTransactions)
                    {
                        var dbTx = await dbContext.TransacoesBancarias
                            .FirstOrDefaultAsync(x => x.Provedor == acc.Provedor && x.ProvedorTransacaoId == txDto.Id && x.FkIdContaBancaria == acc.Id);

                        var catId = ClassificarTransacao(txDto.Description, categories);

                        if (dbTx == null)
                        {
                            dbTx = new BankTransaction
                            {
                                FkIdUsuario = userId,
                                FkIdContaBancaria = acc.Id,
                                Provedor = acc.Provedor,
                                ProvedorTransacaoId = txDto.Id,
                                DataTransacao = txDto.Date,
                                Valor = Math.Abs(txDto.Amount),
                                Descricao = txDto.Description,
                                EstabelecimentoNome = txDto.MerchantName,
                                Tipo = txDto.Type.ToUpperInvariant() == "CREDIT" ? "RECEITA" : "DESPESA",
                                Moeda = txDto.CurrencyCode,
                                FkIdCategoria = catId,
                                Metadata = JsonSerializer.Serialize(new { categoryFromBank = txDto.Category, status = txDto.Status }),
                                DataCriacao = syncTime,
                                DataAtualizacao = syncTime
                            };
                            dbContext.TransacoesBancarias.Add(dbTx);
                            inserted++;
                        }
                    }

                    // Reload balance
                    try
                    {
                        var provAccounts = await openFinanceProvider.GetAccountsAsync(acc.ProvedorContaId, CancellationToken.None); // In Pluggy getAccounts parameter is itemId
                        // Wait, acc.FkIdConexaoNavigation is not loaded, but we have acc.FkIdConexao which we can load
                        var conn = await dbContext.OpenFinanceConexoes.FindAsync(connectionId);
                        if (conn != null)
                        {
                            var provAccountsList = await openFinanceProvider.GetAccountsAsync(conn.ProvedorItemId, CancellationToken.None);
                            var matchingAcc = provAccountsList.FirstOrDefault(x => x.Id == acc.ProvedorContaId);
                            if (matchingAcc != null)
                            {
                                acc.SaldoAtual = matchingAcc.Balance;
                                acc.SaldoDisponivel = matchingAcc.Balance;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("[Background Sync] Erro ao carregar saldo para conta {AccountId}: {Error}", acc.Id, ex.Message);
                    }

                    acc.UltimaSincronizacao = syncTime;
                    acc.DataAtualizacao = syncTime;

                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("[Background Sync] Conta {AccountId} sincronizada com sucesso. Novas transações: {Count}.", acc.Id, inserted);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Background Sync] Falha ao sincronizar conta {AccountId}.", acc.Id);
                }
            }
        });
    }

    public static int? ClassificarTransacao(string descricao, List<Categorias> categoriasValidas)
    {
        var descUpper = descricao.ToUpperInvariant();
        string? nomeCategoriaAlvo = null;

        if (descUpper.Contains("UBER") || descUpper.Contains("99TAXIS") || descUpper.Contains("99 POP") || descUpper.Contains("99TÁXIS") || descUpper.Contains("CABIFY") || descUpper.Contains("METRO") || descUpper.Contains("POSTO") || descUpper.Contains("COMBUS"))
            nomeCategoriaAlvo = "Transporte";
        else if (descUpper.Contains("IFOOD") || descUpper.Contains("RAPPI") || descUpper.Contains("RESTAURANTE") || descUpper.Contains("BURGER KING") || descUpper.Contains("MCDONALD") || descUpper.Contains("LANCHE") || descUpper.Contains("PIZZA") || descUpper.Contains("DELIVERY"))
            nomeCategoriaAlvo = "Alimentação";
        else if (descUpper.Contains("NETFLIX") || descUpper.Contains("SPOTIFY") || descUpper.Contains("PRIME VIDEO") || descUpper.Contains("STEAM") || descUpper.Contains("CINEMA") || descUpper.Contains("HBO"))
            nomeCategoriaAlvo = "Entretenimento";
        else if (descUpper.Contains("SUPERMERCADO") || descUpper.Contains("MERCADO") || descUpper.Contains("CARREFOUR") || descUpper.Contains("EXTRA") || descUpper.Contains("PÃO DE AÇÚCAR") || descUpper.Contains("DIA%") || descUpper.Contains("ASSAI"))
            nomeCategoriaAlvo = "Mercado";

        if (nomeCategoriaAlvo == null) return null;

        var cat = categoriasValidas.FirstOrDefault(c => c.Nome.Equals(nomeCategoriaAlvo, StringComparison.OrdinalIgnoreCase));
        return cat?.Id;
    }
}
