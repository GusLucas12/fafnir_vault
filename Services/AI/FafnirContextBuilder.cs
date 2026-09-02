using System.Globalization;
using System.Text.RegularExpressions;
using fanfnir_back.DTOs;
using fanfnir_back.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace fanfnir_back.Services.AI;

public sealed class FafnirContextBuilder : IFafnirContextBuilder
{
    private readonly FafnirContext _db;
    private readonly IOptions<AiOptions> _options;
    private static readonly CultureInfo PtBrCulture = new("pt-BR");

    public FafnirContextBuilder(FafnirContext db, IOptions<AiOptions> options)
    {
        _db = db;
        _options = options;
    }

    public async Task<FafnirFinancialContext> GetRelevantContextAsync(
        int usuarioId,
        string question,
        IReadOnlyList<FafnirChatMessageDto>? history,
        int? mes,
        int? ano,
        CancellationToken ct)
    {
        var resolvedDate = ResolveTargetPeriod(question, history, mes, ano);
        var targetMes = resolvedDate.Month;
        var targetAno = resolvedDate.Year;
        var periodStr = $"{targetAno:D4}-{targetMes:D2}";

        var normalizedQuestion = question.Trim().ToLowerInvariant();

        // 1. Check for Affordability Intent (e.g. "Posso comprar um celular de R$ 2.000?")
        if (IsAffordabilityIntent(normalizedQuestion, out var purchaseAmount, out var itemDesc))
        {
            var affordability = await GetAffordabilityAnalysisAsync(usuarioId, purchaseAmount, itemDesc, targetMes, targetAno, ct);
            return new FafnirFinancialContext
            {
                Period = periodStr,
                Intent = "affordability",
                Affordability = affordability
            };
        }

        // 2. Check for Goals Intent (e.g. "Quanto falta para minha reserva?", "Minhas metas")
        if (IsGoalsIntent(normalizedQuestion, history, out var goalFilter))
        {
            var goals = await GetGoalsSummaryAsync(usuarioId, goalFilter, ct);
            return new FafnirFinancialContext
            {
                Period = periodStr,
                Intent = "goals",
                Goals = goals
            };
        }

        // 3. Check for Debt Intent (e.g. "Quais são minhas dívidas?", "Compromissos a pagar")
        if (IsDebtIntent(normalizedQuestion))
        {
            var debts = await GetDebtSummaryAsync(usuarioId, ct);
            return new FafnirFinancialContext
            {
                Period = periodStr,
                Intent = "debt_summary",
                Debts = debts
            };
        }

        // 4. Check for Category Intent (e.g. "Quanto gastei com alimentação?", "E no mês passado?")
        var matchedCategory = await FindCategoryInQuestionOrHistoryAsync(usuarioId, normalizedQuestion, history, ct);
        if (!string.IsNullOrWhiteSpace(matchedCategory))
        {
            var categoryAnalysis = await GetCategoryAnalysisAsync(usuarioId, targetMes, targetAno, matchedCategory, ct);
            if (categoryAnalysis != null)
            {
                return new FafnirFinancialContext
                {
                    Period = periodStr,
                    Intent = "category_analysis",
                    Category = categoryAnalysis
                };
            }
        }

        // 5. Check for Income-specific Intent (e.g. "Quanto ganhei?", "Minhas receitas")
        if (IsIncomeIntent(normalizedQuestion))
        {
            var income = await GetIncomeSummaryAsync(usuarioId, targetMes, targetAno, ct);
            return new FafnirFinancialContext
            {
                Period = periodStr,
                Intent = "income_summary",
                Income = income
            };
        }

        // 6. Check for Expense-specific Intent (e.g. "Quais meus maiores gastos?")
        if (IsExpenseIntent(normalizedQuestion))
        {
            var expenses = await GetExpenseSummaryAsync(usuarioId, targetMes, targetAno, ct);
            return new FafnirFinancialContext
            {
                Period = periodStr,
                Intent = "expense_summary",
                Expenses = expenses
            };
        }

        // 7. Default: General Financial Summary
        var summary = await GetFinancialSummaryAsync(usuarioId, targetMes, targetAno, ct);
        return new FafnirFinancialContext
        {
            Period = periodStr,
            Intent = "financial_summary",
            Summary = summary
        };
    }

    public async Task<FinancialSummaryContext> GetFinancialSummaryAsync(int usuarioId, int mes, int ano, CancellationToken ct)
    {
        var periodStr = $"{ano:D4}-{mes:D2}";
        var transacoes = _db.Transacoes.AsNoTracking()
            .Where(x => x.FkIdUsuario == usuarioId && x.MesReferencia == mes && x.AnoReferencia == ano);

        var totalIncome = await transacoes.Where(x => x.Tipo == "RECEITA").SumAsync(x => (decimal?)x.Valor, ct) ?? 0m;
        var transExpenses = await transacoes.Where(x => x.Tipo == "DESPESA").SumAsync(x => (decimal?)x.Valor, ct) ?? 0m;

        var activeSubs = await _db.Assinaturas.AsNoTracking()
            .Where(x => x.FkIdUsuario == usuarioId && x.Ativa)
            .SumAsync(x => (decimal?)x.Valor, ct) ?? 0m;

        var totalExpenses = transExpenses + activeSubs;
        var netBalance = totalIncome - totalExpenses;

        var savedGoals = await _db.AportesMetas.AsNoTracking()
            .Where(x => x.FkIdUsuario == usuarioId && x.DataAporte.Month == mes && x.DataAporte.Year == ano)
            .SumAsync(x => (decimal?)x.Valor, ct) ?? 0m;

        var savingsRate = totalIncome > 0
            ? Math.Round((netBalance / totalIncome) * 100m, 1)
            : 0m;

        return new FinancialSummaryContext(
            Period: periodStr,
            TotalIncome: totalIncome,
            TotalExpenses: totalExpenses,
            NetBalance: netBalance,
            TotalSubscriptions: activeSubs,
            TotalSavedGoals: savedGoals,
            SavingsRatePercent: savingsRate);
    }

    public async Task<IncomeSummaryContext> GetIncomeSummaryAsync(int usuarioId, int mes, int ano, CancellationToken ct)
    {
        var periodStr = $"{ano:D4}-{mes:D2}";
        var incomeTrans = await _db.Transacoes.AsNoTracking()
            .Where(x => x.FkIdUsuario == usuarioId && x.MesReferencia == mes && x.AnoReferencia == ano && x.Tipo == "RECEITA")
            .GroupBy(x => new { x.FkIdCategoria, CategoriaNome = x.FkIdCategoriaNavigation != null ? x.FkIdCategoriaNavigation.Nome : "Outras Receitas" })
            .Select(g => new { g.Key.CategoriaNome, Total = g.Sum(x => x.Valor) })
            .OrderByDescending(x => x.Total)
            .ToListAsync(ct);

        var totalIncome = incomeTrans.Sum(x => x.Total);
        var sources = incomeTrans.Select(x => new CategoryAmountDto(
            x.CategoriaNome,
            x.Total,
            totalIncome > 0 ? Math.Round((x.Total / totalIncome) * 100m, 1) : 0m
        )).ToList();

        return new IncomeSummaryContext(periodStr, totalIncome, sources);
    }

    public async Task<ExpenseSummaryContext> GetExpenseSummaryAsync(int usuarioId, int mes, int ano, CancellationToken ct)
    {
        var periodStr = $"{ano:D4}-{mes:D2}";
        var expensesGrouped = await _db.Transacoes.AsNoTracking()
            .Where(x => x.FkIdUsuario == usuarioId && x.MesReferencia == mes && x.AnoReferencia == ano && x.Tipo == "DESPESA")
            .GroupBy(x => new { x.FkIdCategoria, CategoriaNome = x.FkIdCategoriaNavigation != null ? x.FkIdCategoriaNavigation.Nome : "Sem Categoria" })
            .Select(g => new { g.Key.CategoriaNome, Total = g.Sum(x => x.Valor) })
            .ToListAsync(ct);

        var activeSubs = await _db.Assinaturas.AsNoTracking()
            .Where(x => x.FkIdUsuario == usuarioId && x.Ativa)
            .SumAsync(x => (decimal?)x.Valor, ct) ?? 0m;

        var categoryTotals = expensesGrouped.ToDictionary(x => x.CategoriaNome, x => x.Total, StringComparer.OrdinalIgnoreCase);
        if (activeSubs > 0)
        {
            if (categoryTotals.ContainsKey("Assinaturas"))
                categoryTotals["Assinaturas"] += activeSubs;
            else
                categoryTotals["Assinaturas"] = activeSubs;
        }

        var totalExpenses = categoryTotals.Values.Sum();
        var topCategories = categoryTotals
            .OrderByDescending(kv => kv.Value)
            .Take(8)
            .Select(kv => new CategoryAmountDto(
                kv.Key,
                kv.Value,
                totalExpenses > 0 ? Math.Round((kv.Value / totalExpenses) * 100m, 1) : 0m
            )).ToList();

        return new ExpenseSummaryContext(periodStr, totalExpenses, activeSubs, topCategories);
    }

    public async Task<CategoryAnalysisContext?> GetCategoryAnalysisAsync(int usuarioId, int mes, int ano, string categoryName, CancellationToken ct)
    {
        var periodStr = $"{ano:D4}-{mes:D2}";

        var lowerCatName = categoryName.ToLowerInvariant();

        // Find category by name (case-insensitive)
        var category = await _db.Categorias.AsNoTracking()
            .FirstOrDefaultAsync(c => (c.FkIdUsuario == usuarioId || c.FkIdUsuario == null) &&
                                      c.Nome.ToLower().Contains(lowerCatName), ct);

        int? catId = category?.Id;
        var resolvedCategoryName = category?.Nome ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(categoryName);

        // Current month category spending
        var currentTransSum = await _db.Transacoes.AsNoTracking()
            .Where(t => t.FkIdUsuario == usuarioId && t.MesReferencia == mes && t.AnoReferencia == ano && t.Tipo == "DESPESA" &&
                        (catId.HasValue ? t.FkIdCategoria == catId.Value : t.Descricao.ToLower().Contains(lowerCatName)))
            .SumAsync(t => (decimal?)t.Valor, ct) ?? 0m;

        var currentSubsSum = catId.HasValue
            ? await _db.Assinaturas.AsNoTracking()
                .Where(s => s.FkIdUsuario == usuarioId && s.Ativa && s.FkIdCategoria == catId.Value)
                .SumAsync(s => (decimal?)s.Valor, ct) ?? 0m
            : 0m;

        var currentAmount = currentTransSum + currentSubsSum;

        // Total month expenses & income
        var allTransExpenses = await _db.Transacoes.AsNoTracking()
            .Where(t => t.FkIdUsuario == usuarioId && t.MesReferencia == mes && t.AnoReferencia == ano && t.Tipo == "DESPESA")
            .SumAsync(t => (decimal?)t.Valor, ct) ?? 0m;

        var allSubs = await _db.Assinaturas.AsNoTracking()
            .Where(s => s.FkIdUsuario == usuarioId && s.Ativa)
            .SumAsync(s => (decimal?)s.Valor, ct) ?? 0m;

        var totalMonthExpenses = allTransExpenses + allSubs;

        var totalMonthIncome = await _db.Transacoes.AsNoTracking()
            .Where(t => t.FkIdUsuario == usuarioId && t.MesReferencia == mes && t.AnoReferencia == ano && t.Tipo == "RECEITA")
            .SumAsync(t => (decimal?)t.Valor, ct) ?? 0m;

        var shareOfExpenses = totalMonthExpenses > 0 ? Math.Round((currentAmount / totalMonthExpenses) * 100m, 1) : 0m;
        var shareOfIncome = totalMonthIncome > 0 ? Math.Round((currentAmount / totalMonthIncome) * 100m, 1) : 0m;

        // Previous month calculation
        var prevDate = new DateTime(ano, mes, 1).AddMonths(-1);
        int prevMes = prevDate.Month;
        int prevAno = prevDate.Year;

        var prevTransSum = await _db.Transacoes.AsNoTracking()
            .Where(t => t.FkIdUsuario == usuarioId && t.MesReferencia == prevMes && t.AnoReferencia == prevAno && t.Tipo == "DESPESA" &&
                        (catId.HasValue ? t.FkIdCategoria == catId.Value : t.Descricao.ToLower().Contains(lowerCatName)))
            .SumAsync(t => (decimal?)t.Valor, ct) ?? 0m;

        decimal? prevAmount = prevTransSum > 0 || currentSubsSum > 0 ? prevTransSum + currentSubsSum : null;
        decimal? monthlyChangePercent = null;
        if (prevAmount.HasValue && prevAmount.Value > 0)
        {
            monthlyChangePercent = Math.Round(((currentAmount - prevAmount.Value) / prevAmount.Value) * 100m, 1);
        }

        // Budget info
        decimal? budgetLimit = null;
        decimal? budgetRemaining = null;
        bool isOverBudget = false;

        if (catId.HasValue)
        {
            var budget = await _db.OrcamentosMensais.AsNoTracking()
                .FirstOrDefaultAsync(o => o.FkIdUsuario == usuarioId && o.FkIdCategoria == catId.Value && o.MesReferencia == mes && o.AnoReferencia == ano, ct);

            if (budget != null)
            {
                budgetLimit = budget.ValorLimite;
                budgetRemaining = budget.ValorLimite - currentAmount;
                isOverBudget = currentAmount > budget.ValorLimite;
            }
        }

        return new CategoryAnalysisContext(
            Period: periodStr,
            CategoryName: resolvedCategoryName,
            CurrentMonthAmount: currentAmount,
            TotalMonthExpenses: totalMonthExpenses,
            ShareOfExpensesPercent: shareOfExpenses,
            ShareOfIncomePercent: shareOfIncome,
            PreviousMonthAmount: prevAmount,
            MonthlyChangePercent: monthlyChangePercent,
            BudgetLimit: budgetLimit,
            BudgetRemaining: budgetRemaining,
            IsOverBudget: isOverBudget);
    }

    public async Task<DebtSummaryContext> GetDebtSummaryAsync(int usuarioId, CancellationToken ct)
    {
        var debtGoals = await _db.Metas.AsNoTracking()
            .Where(m => m.FkIdUsuario == usuarioId && m.Ativa && m.TipoMeta == "quitar_divida")
            .ToListAsync(ct);

        var totalDebtTarget = debtGoals.Sum(m => m.ValorAlvo);
        var totalDebtCurrent = debtGoals.Sum(m => m.ValorAtual);
        var remainingDebt = Math.Max(0, totalDebtTarget - totalDebtCurrent);

        var monthlyFixed = await _db.Assinaturas.AsNoTracking()
            .Where(s => s.FkIdUsuario == usuarioId && s.Ativa)
            .SumAsync(s => (decimal?)s.Valor, ct) ?? 0m;

        return new DebtSummaryContext(
            TotalDebtGoals: totalDebtTarget,
            RemainingDebt: remainingDebt,
            ActiveDebtGoalsCount: debtGoals.Count,
            MonthlyFixedCommitments: monthlyFixed);
    }

    public async Task<IReadOnlyList<GoalContextItem>> GetGoalsSummaryAsync(int usuarioId, string? goalNameFilter, CancellationToken ct)
    {
        var query = _db.Metas.AsNoTracking().Where(m => m.FkIdUsuario == usuarioId && m.Ativa);

        if (!string.IsNullOrWhiteSpace(goalNameFilter))
        {
            var lowerGoalFilter = goalNameFilter.ToLowerInvariant();
            query = query.Where(m => m.Nome.ToLower().Contains(lowerGoalFilter) ||
                                     (m.Descricao != null && m.Descricao.ToLower().Contains(lowerGoalFilter)) ||
                                     m.TipoMeta.ToLower().Contains(lowerGoalFilter));
        }

        var goals = await query.ToListAsync(ct);
        var now = DateTime.Today;

        return goals.Select(g =>
        {
            var remaining = Math.Max(0m, g.ValorAlvo - g.ValorAtual);
            var progress = g.ValorAlvo > 0 ? Math.Round((g.ValorAtual / g.ValorAlvo) * 100m, 1) : 0m;

            decimal? monthlyNeeded = null;
            if (g.DataFim.HasValue && remaining > 0)
            {
                int months = ((g.DataFim.Value.Year - now.Year) * 12) + g.DataFim.Value.Month - now.Month + 1;
                if (months <= 0) months = 1;
                monthlyNeeded = Math.Round(remaining / months, 2);
            }

            return new GoalContextItem(
                Name: g.Nome,
                Type: g.TipoMeta,
                TargetAmount: g.ValorAlvo,
                CurrentAmount: g.ValorAtual,
                RemainingAmount: remaining,
                ProgressPercent: progress,
                MonthlyTargetNeeded: monthlyNeeded,
                TargetDate: g.DataFim?.ToString("yyyy-MM-dd"),
                IsCompleted: g.Concluida || g.ValorAtual >= g.ValorAlvo);
        }).ToList();
    }

    public async Task<AffordabilityContext> GetAffordabilityAnalysisAsync(
        int usuarioId,
        decimal purchaseAmount,
        string itemDescription,
        int mes,
        int ano,
        CancellationToken ct)
    {
        // 1. Current available balance across all active wallets
        var totalWalletBalance = await _db.Carteiras.AsNoTracking()
            .Where(c => c.FkIdUsuario == usuarioId && c.Ativo)
            .SumAsync(c => (decimal?)c.SaldoInicial, ct) ?? 0m;

        // 2. Monthly income & expenses
        var monthlyIncome = await _db.Transacoes.AsNoTracking()
            .Where(t => t.FkIdUsuario == usuarioId && t.MesReferencia == mes && t.AnoReferencia == ano && t.Tipo == "RECEITA")
            .SumAsync(t => (decimal?)t.Valor, ct) ?? 0m;

        var monthlyTransExpenses = await _db.Transacoes.AsNoTracking()
            .Where(t => t.FkIdUsuario == usuarioId && t.MesReferencia == mes && t.AnoReferencia == ano && t.Tipo == "DESPESA")
            .SumAsync(t => (decimal?)t.Valor, ct) ?? 0m;

        var fixedCommitments = await _db.Assinaturas.AsNoTracking()
            .Where(s => s.FkIdUsuario == usuarioId && s.Ativa)
            .SumAsync(s => (decimal?)s.Valor, ct) ?? 0m;

        var totalMonthlyExpenses = monthlyTransExpenses + fixedCommitments;
        var monthlySurplus = monthlyIncome - totalMonthlyExpenses;

        var balanceAfter = totalWalletBalance - purchaseAmount;
        var canAffordImmediately = balanceAfter >= 0;

        // Severity evaluation
        string impactSeverity;
        string? recommendation = null;

        if (!canAffordImmediately)
        {
            impactSeverity = "CRITICO";
            recommendation = "Saldo total atual insuficiente para compra à vista sem comprometer o fluxo de caixa.";
        }
        else if (purchaseAmount > (totalWalletBalance * 0.5m) || (monthlySurplus > 0 && purchaseAmount > monthlySurplus * 2))
        {
            impactSeverity = "ALTO";
            recommendation = "A compra consome mais de 50% da sua liquidez atual. Recomenda-se planejar aportes ou parcelar sem juros mantendo a reserva.";
        }
        else if (purchaseAmount > (totalWalletBalance * 0.2m))
        {
            impactSeverity = "MEDIO";
            recommendation = "Impacto moderado nas reservas. Certifique-se de que não haverá despesas emergenciais nos próximos dias.";
        }
        else
        {
            impactSeverity = "BAIXO";
            recommendation = "Compra perfeitamente compatível com seu saldo disponível e margem mensal.";
        }

        return new AffordabilityContext(
            ItemDescription: itemDescription,
            PurchaseAmount: purchaseAmount,
            CurrentAvailableBalance: totalWalletBalance,
            MonthlyNetIncome: monthlyIncome,
            MonthlyAverageExpenses: totalMonthlyExpenses,
            FixedMonthlyCommitments: fixedCommitments,
            BalanceAfterPurchase: balanceAfter,
            CanAffordImmediately: canAffordImmediately,
            ImpactSeverity: impactSeverity,
            InstallmentRecommendation: recommendation);
    }

    public async Task<IReadOnlyList<SanitizedTransactionDto>> GetRecentRelevantTransactionsAsync(
        int usuarioId,
        int? categoriaId,
        int limit,
        CancellationToken ct)
    {
        var maxLimit = Math.Min(limit, _options.Value.Limits.MaxRecentTransactions);
        var query = _db.Transacoes.AsNoTracking()
            .Where(t => t.FkIdUsuario == usuarioId);

        if (categoriaId.HasValue)
        {
            query = query.Where(t => t.FkIdCategoria == categoriaId.Value);
        }

        var list = await query
            .OrderByDescending(t => t.DataTransacao)
            .Take(maxLimit)
            .Select(t => new
            {
                t.DataTransacao,
                CategoriaNome = t.FkIdCategoriaNavigation != null ? t.FkIdCategoriaNavigation.Nome : "Sem Categoria",
                t.Valor,
                t.Tipo
            })
            .ToListAsync(ct);

        return list.Select(t => new SanitizedTransactionDto(
            Date: t.DataTransacao.ToString("yyyy-MM-dd"),
            Category: t.CategoriaNome,
            Amount: t.Valor,
            Type: t.Tipo.ToLowerInvariant()
        )).ToList();
    }

    #region Helper Intent Parsers
    private static DateTime ResolveTargetPeriod(string question, IReadOnlyList<FafnirChatMessageDto>? history, int? mes, int? ano)
    {
        var now = DateTime.Today;

        if (mes.HasValue && ano.HasValue && mes.Value >= 1 && mes.Value <= 12 && ano.Value >= 2000)
        {
            return new DateTime(ano.Value, mes.Value, 1);
        }

        var lower = question.ToLowerInvariant();

        if (lower.Contains("mês passado") || lower.Contains("mes passado"))
        {
            return now.AddMonths(-1);
        }
        if (lower.Contains("ano passado"))
        {
            return now.AddYears(-1);
        }

        // Detect named months
        var monthsMap = new Dictionary<string, int>
        {
            { "janeiro", 1 }, { "fevereiro", 2 }, { "março", 3 }, { "marco", 3 },
            { "abril", 4 }, { "maio", 5 }, { "junho", 6 }, { "julho", 7 },
            { "agosto", 8 }, { "setembro", 9 }, { "outubro", 10 }, { "novembro", 11 }, { "dezembro", 12 }
        };

        foreach (var (monthName, monthNum) in monthsMap)
        {
            if (lower.Contains(monthName))
            {
                var year = now.Year;
                var yearMatch = Regex.Match(lower, @"\b(20\d{2})\b");
                if (yearMatch.Success && int.TryParse(yearMatch.Value, out var parsedYear))
                {
                    year = parsedYear;
                }
                return new DateTime(year, monthNum, 1);
            }
        }

        return now;
    }

    private static bool IsAffordabilityIntent(string lowerQuestion, out decimal amount, out string itemDescription)
    {
        amount = 0;
        itemDescription = "item";

        var keywords = new[] { "posso comprar", "dá pra comprar", "da pra comprar", "consigo comprar", "comprar um", "comprar uma", "vale a pena comprar", "parcelar", "posso gastar" };
        var hasKeyword = keywords.Any(lowerQuestion.Contains);

        // Regex for amount: R$ 2.000,00 | R$2000 | 2000 reais | 1.500 | 500
        var match = Regex.Match(lowerQuestion, @"(?:r\$\s*|reais\s*)?(\d{1,3}(?:\.\d{3})*(?:,\d{2})?|\d+(?:,\d{2})?)(?:\s*reais)?", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var rawVal = match.Groups[1].Value.Replace(".", "").Replace(",", ".");
            if (decimal.TryParse(rawVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
            {
                amount = parsed;
            }
        }

        if (hasKeyword && amount > 0)
        {
            // Extract item description
            var itemMatch = Regex.Match(lowerQuestion, @"comprar\s+(?:um|uma|o|a)?\s*([a-zA-ZÀ-ÿ0-9\s]+?)(?:\s+(?:de|por|custando|no valor)|\s*r\$|\s*\d|$)", RegexOptions.IgnoreCase);
            if (itemMatch.Success && !string.IsNullOrWhiteSpace(itemMatch.Groups[1].Value))
            {
                itemDescription = itemMatch.Groups[1].Value.Trim();
            }
            return true;
        }

        return false;
    }

    private static bool IsGoalsIntent(string lowerQuestion, IReadOnlyList<FafnirChatMessageDto>? history, out string? goalFilter)
    {
        goalFilter = null;
        var goalKeywords = new[] { "meta", "metas", "reserva", "reserva de emergência", "reserva de emergencia", "objetivo", "quanto falta para", "guardar dinheiro" };

        if (goalKeywords.Any(lowerQuestion.Contains))
        {
            if (lowerQuestion.Contains("reserva")) goalFilter = "reserva";
            else if (lowerQuestion.Contains("viagem")) goalFilter = "viagem";
            else if (lowerQuestion.Contains("carro")) goalFilter = "carro";
            else if (lowerQuestion.Contains("casa") || lowerQuestion.Contains("imovel")) goalFilter = "casa";
            return true;
        }

        return false;
    }

    private static bool IsDebtIntent(string lowerQuestion)
    {
        var keywords = new[] { "dívida", "divida", "dividas", "dívidas", "empréstimo", "emprestimo", "parcela", "compromissos", "quitar" };
        return keywords.Any(lowerQuestion.Contains);
    }

    private static bool IsIncomeIntent(string lowerQuestion)
    {
        var keywords = new[] { "quanto ganhei", "minha renda", "minhas receitas", "total de receitas", "entradas do mês", "meu salário", "salario" };
        return keywords.Any(lowerQuestion.Contains);
    }

    private static bool IsExpenseIntent(string lowerQuestion)
    {
        var keywords = new[] { "maiores gastos", "onde mais gastei", "despesas do mês", "total de despesas", "principais gastos" };
        return keywords.Any(lowerQuestion.Contains);
    }

    private async Task<string?> FindCategoryInQuestionOrHistoryAsync(
        int usuarioId,
        string lowerQuestion,
        IReadOnlyList<FafnirChatMessageDto>? history,
        CancellationToken ct)
    {
        // Common category keywords
        var commonCategories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "alimentação", "Alimentação" },
            { "alimentacao", "Alimentação" },
            { "comida", "Alimentação" },
            { "mercado", "Alimentação" },
            { "restaurante", "Alimentação" },
            { "ifood", "Alimentação" },
            { "transporte", "Transporte" },
            { "uber", "Transporte" },
            { "combustível", "Transporte" },
            { "combustivel", "Transporte" },
            { "gasolina", "Transporte" },
            { "moradia", "Moradia" },
            { "aluguel", "Moradia" },
            { "condomínio", "Moradia" },
            { "condominio", "Moradia" },
            { "lazer", "Lazer" },
            { "saúde", "Saúde" },
            { "saude", "Saúde" },
            { "farmácia", "Saúde" },
            { "farmacia", "Saúde" },
            { "educação", "Educação" },
            { "educacao", "Educação" },
            { "assinatura", "Assinaturas" },
            { "assinaturas", "Assinaturas" },
            { "streaming", "Assinaturas" }
        };

        foreach (var (kw, cat) in commonCategories)
        {
            if (lowerQuestion.Contains(kw)) return cat;
        }

        // Query user's registered categories
        var userCategories = await _db.Categorias.AsNoTracking()
            .Where(c => (c.FkIdUsuario == usuarioId || c.FkIdUsuario == null) && c.Ativo)
            .Select(c => c.Nome)
            .ToListAsync(ct);

        foreach (var uc in userCategories)
        {
            if (lowerQuestion.Contains(uc.ToLowerInvariant())) return uc;
        }

        // Multi-turn context resolution: if user asks "E no mês passado?", check history
        if (history != null && (lowerQuestion.Contains("e no mês passado") || lowerQuestion.Contains("e no mes passado") || lowerQuestion.Contains("e no anterior")))
        {
            var lastUserMsg = history.LastOrDefault(h => h.Role.Equals("user", StringComparison.OrdinalIgnoreCase))?.Content.ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(lastUserMsg))
            {
                foreach (var (kw, cat) in commonCategories)
                {
                    if (lastUserMsg.Contains(kw)) return cat;
                }
                foreach (var uc in userCategories)
                {
                    if (lastUserMsg.Contains(uc.ToLowerInvariant())) return uc;
                }
            }
        }

        return null;
    }
    #endregion
}
