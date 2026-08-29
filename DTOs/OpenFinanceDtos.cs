using System;
using System.Collections.Generic;

namespace fanfnir_back.DTOs;

public record OpenFinanceConnectResponseDto(string ConnectToken);

public record OpenFinanceConnectionDto(
    int Id,
    int FkIdUsuario,
    string Provedor,
    string ProvedorItemId,
    string Status,
    string? InstituicaoId,
    string? InstituicaoNome,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);

public record BankAccountResponseDto(
    int Id,
    int FkIdUsuario,
    int FkIdConexao,
    string Provedor,
    string ProvedorContaId,
    string InstituicaoId,
    string InstituicaoNome,
    string Tipo,
    string Nome,
    string Moeda,
    decimal SaldoAtual,
    decimal? SaldoDisponivel,
    DateTime? UltimaSincronizacao,
    string Status,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);

public record BankTransactionResponseDto(
    int Id,
    int FkIdUsuario,
    int FkIdContaBancaria,
    string Provedor,
    string ProvedorTransacaoId,
    DateTime DataTransacao,
    decimal Valor,
    string Descricao,
    string? EstabelecimentoNome,
    string Tipo,
    string Moeda,
    int? FkIdCategoria,
    string? Metadata,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);

public record PluggyWebhookPayloadDto(
    string Event,
    Guid EventId,
    Guid ItemId,
    string? ClientUserId
);

public record OpenFinanceItemDto(
    string Id,
    string Status,
    string? ConnectorId,
    string? ConnectorName,
    string? ErrorCode,
    string? ErrorMessage
);

public record OpenFinanceAccountDto(
    string Id,
    string ItemId,
    string Type,
    string Number,
    decimal Balance,
    string CurrencyCode,
    string Name,
    string InstitutionName
);

public record OpenFinanceTransactionDto(
    string Id,
    string AccountId,
    DateTime Date,
    string Description,
    decimal Amount,
    string CurrencyCode,
    string Status,
    string Type,
    string? Category,
    string? MerchantName
);

public record OpenFinanceTransactionsResponseDto(
    IReadOnlyList<OpenFinanceTransactionDto> Results,
    string? Next
);
