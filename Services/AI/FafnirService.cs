using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using fanfnir_back.DTOs;
using Microsoft.Extensions.Options;

namespace fanfnir_back.Services.AI;

public sealed class FafnirService : IFafnirService
{
    private readonly IFafnirContextBuilder _contextBuilder;
    private readonly IAiProvider _aiProvider;
    private readonly IOptions<AiOptions> _options;
    private readonly ILogger<FafnirService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FafnirService(
        IFafnirContextBuilder contextBuilder,
        IAiProvider aiProvider,
        IOptions<AiOptions> options,
        ILogger<FafnirService> logger)
    {
        _contextBuilder = contextBuilder;
        _aiProvider = aiProvider;
        _options = options;
        _logger = logger;
    }

    public async Task<ServiceResult<FafnirChatResponseDto>> ProcessQuestionAsync(
        int usuarioId,
        FafnirChatRequestDto request,
        CancellationToken ct)
    {
        if (usuarioId <= 0)
        {
            return ServiceResult<FafnirChatResponseDto>.Unauthorized("Sessão inválida. Faça login novamente.");
        }

        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return ServiceResult<FafnirChatResponseDto>.BadRequest("A pergunta não pode ser vazia.");
        }

        var maxQuestionLength = _options.Value.Limits.MaxQuestionLength > 0 ? _options.Value.Limits.MaxQuestionLength : 500;
        var sanitizedQuestion = request.Question.Trim();
        if (sanitizedQuestion.Length > maxQuestionLength)
        {
            sanitizedQuestion = sanitizedQuestion[..maxQuestionLength];
        }

        // Truncate and sanitize history
        var maxHistoryMessages = _options.Value.Limits.MaxHistoryMessages > 0 ? _options.Value.Limits.MaxHistoryMessages : 6;
        var sanitizedHistory = request.History?
            .Where(h => !string.IsNullOrWhiteSpace(h.Content))
            .TakeLast(maxHistoryMessages)
            .Select(h => new FafnirChatMessageDto(
                Role: h.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) || h.Role.Equals("model", StringComparison.OrdinalIgnoreCase) ? "model" : "user",
                Content: h.Content.Trim()
            ))
            .ToList() ?? new List<FafnirChatMessageDto>();

        // 1. Build minimal sanitized financial context
        var context = await _contextBuilder.GetRelevantContextAsync(
            usuarioId,
            sanitizedQuestion,
            sanitizedHistory,
            request.Mes,
            request.Ano,
            ct);

        var contextJson = JsonSerializer.Serialize(context, JsonOpts);

        // 2. Generate warnings and suggestions based on backend facts
        var warnings = ExtractWarnings(context);
        var suggestions = ExtractSuggestions(context);

        // 3. Prepare AI Prompt
        var promptRequest = new AiPromptRequest(
            SystemInstruction: FafnirPrompts.GetSystemPrompt(),
            ContextJson: contextJson,
            History: sanitizedHistory,
            UserPrompt: sanitizedQuestion,
            Temperature: _options.Value.Gemini.Temperature,
            MaxTokens: _options.Value.Gemini.MaxOutputTokens);

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Enviando requisição para IA [Provedor: {Provider}, Intent: {Intent}, Period: {Period}]",
            _aiProvider.ProviderName, context.Intent, context.Period);

        AiResponseDto aiResponse;
        try
        {
            aiResponse = await _aiProvider.GenerateResponseAsync(promptRequest, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao chamar IA provider {Provider}: {Message}", _aiProvider.ProviderName, ex.Message);
            aiResponse = new AiResponseDto(
                Content: string.Empty,
                PromptTokens: 0,
                CandidatesTokens: 0,
                TotalTokens: 0,
                FinishReason: "EXCEPTION",
                Success: false,
                ErrorMessage: ex.Message);
        }
        stopwatch.Stop();

        _logger.LogInformation("Resposta da IA concluída em {ElapsedMs}ms. Sucesso: {Success}, Tokens: {Tokens}",
            stopwatch.ElapsedMilliseconds, aiResponse.Success, aiResponse.TotalTokens);

        string finalMessage;
        if (aiResponse.Success && !string.IsNullOrWhiteSpace(aiResponse.Content))
        {
            finalMessage = aiResponse.Content;
        }
        else
        {
            // Build resilient rule-based fallback response from calculated facts
            finalMessage = BuildFallbackResponse(context, sanitizedQuestion);
            _logger.LogWarning("Usando resposta de fallback para o usuário devido a indisponibilidade da IA. Motivo: {Reason}",
                aiResponse.ErrorMessage ?? aiResponse.FinishReason);
        }

        var responseDto = new FafnirChatResponseDto(
            Message: finalMessage,
            Type: context.Intent,
            Warnings: warnings,
            Suggestions: suggestions,
            MinimalContextSummary: context,
            GeneratedAt: DateTime.UtcNow,
            Provider: _aiProvider.ProviderName,
            TokensUsed: aiResponse.TotalTokens);

        return ServiceResult<FafnirChatResponseDto>.Ok(responseDto);
    }

    #region Rule-based Fallback & Warnings
    private static string BuildFallbackResponse(FafnirFinancialContext ctx, string question)
    {
        if (ctx.Intent == "general_conversation")
        {
            return "Olá! Sou o Fafnir, seu assistente pessoal de finanças. Posso te ajudar a analisar seus gastos, acompanhar metas, calcular a viabilidade de compras e fornecer resumos financeiros. Como posso te ajudar hoje?";
        }

        if (ctx.Affordability != null)
        {
            var aff = ctx.Affordability;
            var canAfford = aff.CanAffordImmediately ? "Sim, você possui saldo suficiente" : "Atenção: seu saldo disponível é insuficiente";
            return $"{canAfford} para a compra de {aff.ItemDescription} no valor de R$ {aff.PurchaseAmount:N2}.\n\n" +
                   $"• Saldo disponível atual: R$ {aff.CurrentAvailableBalance:N2}\n" +
                   $"• Saldo restante previsto: R$ {aff.BalanceAfterPurchase:N2}\n" +
                   $"• Avaliação de impacto: {aff.ImpactSeverity}\n" +
                   $"• Recomendação: {aff.InstallmentRecommendation}";
        }

        if (ctx.Category != null)
        {
            var cat = ctx.Category;
            var budgetInfo = cat.BudgetLimit.HasValue
                ? $"\n• Limite orçado: R$ {cat.BudgetLimit.Value:N2} (Saldo disponível no orçamento: R$ {cat.BudgetRemaining:N2})"
                : "";
            var comparisonInfo = cat.MonthlyChangePercent.HasValue
                ? $"\n• Variação em relação ao mês anterior: {(cat.MonthlyChangePercent.Value >= 0 ? "+" : "")}{cat.MonthlyChangePercent.Value:N1}%"
                : "";

            return $"No período {ctx.Period}, seus gastos com {cat.CategoryName} somaram R$ {cat.CurrentMonthAmount:N2}, " +
                   $"representando {cat.ShareOfExpensesPercent:N1}% do total de despesas do mês.{comparisonInfo}{budgetInfo}";
        }

        if (ctx.Goals != null && ctx.Goals.Count > 0)
        {
            var goalsSummary = string.Join("\n", ctx.Goals.Select(g =>
                $"• {g.Name}: R$ {g.CurrentAmount:N2} acumulados de R$ {g.TargetAmount:N2} ({g.ProgressPercent:N1}% concluído. Falta: R$ {g.RemainingAmount:N2})"));

            return $"Acompanhamento das suas metas ativas:\n{goalsSummary}";
        }

        if (ctx.Debts != null)
        {
            var d = ctx.Debts;
            return $"Resumo de compromissos e dívidas:\n" +
                   $"• Total em metas de quitação: R$ {d.TotalDebtGoals:N2}\n" +
                   $"• Saldo devedor restante: R$ {d.RemainingDebt:N2}\n" +
                   $"• Compromissos fixos mensais (assinaturas): R$ {d.MonthlyFixedCommitments:N2}";
        }

        if (ctx.Income != null)
        {
            var inc = ctx.Income;
            return $"No período {ctx.Period}, suas receitas totalizam R$ {inc.TotalIncome:N2}.";
        }

        if (ctx.Expenses != null)
        {
            var exp = ctx.Expenses;
            return $"No período {ctx.Period}, suas despesas totalizam R$ {exp.TotalExpenses:N2} (incluindo R$ {exp.TotalSubscriptions:N2} em assinaturas fixas).";
        }

        if (ctx.Summary != null)
        {
            var s = ctx.Summary;
            return $"Resumo financeiro de {ctx.Period}:\n" +
                   $"• Receitas totais: R$ {s.TotalIncome:N2}\n" +
                   $"• Despesas totais: R$ {s.TotalExpenses:N2}\n" +
                   $"• Saldo líquido: R$ {s.NetBalance:N2}\n" +
                   $"• Taxa de poupança: {s.SavingsRatePercent:N1}%\n" +
                   $"• Aportes em metas no mês: R$ {s.TotalSavedGoals:N2}";
        }

        return "Não foi possível carregar os detalhes financeiros no momento. Por favor, tente novamente em instantes.";
    }

    private static List<string> ExtractWarnings(FafnirFinancialContext ctx)
    {
        var list = new List<string>();

        if (ctx.Category?.IsOverBudget == true)
        {
            list.Add($"Você ultrapassou o orçamento definido para a categoria {ctx.Category.CategoryName}.");
        }

        if (ctx.Affordability != null)
        {
            if (!ctx.Affordability.CanAffordImmediately)
                list.Add("O valor da compra excede seu saldo disponível no momento.");
            else if (ctx.Affordability.ImpactSeverity == "ALTO")
                list.Add("Esta compra consumirá mais da metade de todo o seu saldo disponível.");
        }

        if (ctx.Summary != null && ctx.Summary.NetBalance < 0)
        {
            list.Add("Suas despesas deste mês estão superiores às suas receitas.");
        }

        return list;
    }

    private static List<string> ExtractSuggestions(FafnirFinancialContext ctx)
    {
        var list = new List<string>();

        if (ctx.Summary != null && ctx.Summary.NetBalance > 0 && ctx.Summary.TotalSavedGoals == 0)
        {
            list.Add("Você possui saldo positivo este mês. Que tal destinar uma parte para suas metas ou reserva de emergência?");
        }

        if (ctx.Goals != null && ctx.Goals.Any(g => !g.IsCompleted && g.MonthlyTargetNeeded.HasValue))
        {
            var firstIncomplete = ctx.Goals.First(g => !g.IsCompleted && g.MonthlyTargetNeeded.HasValue);
            list.Add($"Para atingir a meta '{firstIncomplete.Name}' no prazo, reserve R$ {firstIncomplete.MonthlyTargetNeeded:N2} este mês.");
        }

        if (ctx.Affordability?.ImpactSeverity == "ALTO" || ctx.Affordability?.ImpactSeverity == "MEDIO")
        {
            list.Add("Considere verificar se há opções de parcelamento sem juros ou aguardar o próximo ciclo de receitas.");
        }

        return list;
    }
    #endregion
}
