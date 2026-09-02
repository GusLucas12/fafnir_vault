namespace fanfnir_back.DTOs;

public record FafnirChatRequestDto(
    string Question,
    IReadOnlyList<FafnirChatMessageDto>? History = null,
    int? Mes = null,
    int? Ano = null);

public record FafnirChatMessageDto(
    string Role,
    string Content);

public record FafnirChatResponseDto(
    string Message,
    string Type,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Suggestions,
    FafnirFinancialContext? MinimalContextSummary,
    DateTime GeneratedAt,
    string? Provider = null,
    int? TokensUsed = null);

public class FafnirFinancialContext
{
    public string Period { get; set; } = string.Empty;
    public string Intent { get; set; } = "financial_summary";
    public FinancialSummaryContext? Summary { get; set; }
    public IncomeSummaryContext? Income { get; set; }
    public ExpenseSummaryContext? Expenses { get; set; }
    public CategoryAnalysisContext? Category { get; set; }
    public IReadOnlyList<GoalContextItem>? Goals { get; set; }
    public DebtSummaryContext? Debts { get; set; }
    public AffordabilityContext? Affordability { get; set; }
    public IReadOnlyList<SanitizedTransactionDto>? RecentTransactions { get; set; }
}

public record FinancialSummaryContext(
    string Period,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetBalance,
    decimal TotalSubscriptions,
    decimal TotalSavedGoals,
    decimal SavingsRatePercent);

public record IncomeSummaryContext(
    string Period,
    decimal TotalIncome,
    IReadOnlyList<CategoryAmountDto> TopSources);

public record ExpenseSummaryContext(
    string Period,
    decimal TotalExpenses,
    decimal TotalSubscriptions,
    IReadOnlyList<CategoryAmountDto> TopCategories);

public record CategoryAnalysisContext(
    string Period,
    string CategoryName,
    decimal CurrentMonthAmount,
    decimal TotalMonthExpenses,
    decimal ShareOfExpensesPercent,
    decimal ShareOfIncomePercent,
    decimal? PreviousMonthAmount,
    decimal? MonthlyChangePercent,
    decimal? BudgetLimit,
    decimal? BudgetRemaining,
    bool IsOverBudget);

public record GoalContextItem(
    string Name,
    string Type,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal RemainingAmount,
    decimal ProgressPercent,
    decimal? MonthlyTargetNeeded,
    string? TargetDate,
    bool IsCompleted);

public record DebtSummaryContext(
    decimal TotalDebtGoals,
    decimal RemainingDebt,
    int ActiveDebtGoalsCount,
    decimal MonthlyFixedCommitments);

public record AffordabilityContext(
    string ItemDescription,
    decimal PurchaseAmount,
    decimal CurrentAvailableBalance,
    decimal MonthlyNetIncome,
    decimal MonthlyAverageExpenses,
    decimal FixedMonthlyCommitments,
    decimal BalanceAfterPurchase,
    bool CanAffordImmediately,
    string ImpactSeverity,
    string? InstallmentRecommendation);

public record SanitizedTransactionDto(
    string Date,
    string Category,
    decimal Amount,
    string Type);

public record CategoryAmountDto(
    string Category,
    decimal Amount,
    decimal Percentage);
