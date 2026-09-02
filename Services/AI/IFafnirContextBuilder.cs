using fanfnir_back.DTOs;

namespace fanfnir_back.Services.AI;

public interface IFafnirContextBuilder
{
    Task<FafnirFinancialContext> GetRelevantContextAsync(
        int usuarioId,
        string question,
        IReadOnlyList<FafnirChatMessageDto>? history,
        int? mes,
        int? ano,
        CancellationToken ct);

    Task<FinancialSummaryContext> GetFinancialSummaryAsync(int usuarioId, int mes, int ano, CancellationToken ct);
    Task<IncomeSummaryContext> GetIncomeSummaryAsync(int usuarioId, int mes, int ano, CancellationToken ct);
    Task<ExpenseSummaryContext> GetExpenseSummaryAsync(int usuarioId, int mes, int ano, CancellationToken ct);
    Task<CategoryAnalysisContext?> GetCategoryAnalysisAsync(int usuarioId, int mes, int ano, string categoryName, CancellationToken ct);
    Task<DebtSummaryContext> GetDebtSummaryAsync(int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<GoalContextItem>> GetGoalsSummaryAsync(int usuarioId, string? goalNameFilter, CancellationToken ct);
    Task<AffordabilityContext> GetAffordabilityAnalysisAsync(int usuarioId, decimal purchaseAmount, string itemDescription, int mes, int ano, CancellationToken ct);
    Task<IReadOnlyList<SanitizedTransactionDto>> GetRecentRelevantTransactionsAsync(int usuarioId, int? categoriaId, int limit, CancellationToken ct);
}
