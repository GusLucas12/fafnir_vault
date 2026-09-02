using System.Text.Json;
using fanfnir_back.DTOs;
using fanfnir_back.Models;
using fanfnir_back.Services.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace test;

public class FafnirContextBuilderTests
{
    private FafnirContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<FafnirContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new FafnirContext(options);
    }

    private IOptions<AiOptions> CreateAiOptions()
    {
        return Options.Create(new AiOptions
        {
            Limits = new AiLimitsOptions
            {
                MaxHistoryMessages = 6,
                MaxQuestionLength = 500,
                MaxRecentTransactions = 5
            }
        });
    }

    [Fact]
    public async Task GetRelevantContextAsync_NeverLeaks_SensitivePIIOrCredentials()
    {
        // Arrange
        using var db = CreateInMemoryContext(Guid.NewGuid().ToString());
        var user = new Usuarios
        {
            Id = 1,
            Nome = "Carlos da Silva Sauro",
            Email = "carlos.sauro@exemplo.com.br",
            SenhaHash = "PBKDF2$210000$supersecretpasswordsalt$hashvalue"
        };
        db.Usuarios.Add(user);

        var wallet = new Carteiras
        {
            Id = 10,
            FkIdUsuario = 1,
            Nome = "Conta Principal Itaú",
            Tipo = "CONTA_CORRENTE",
            SaldoInicial = 3500.00m,
            Ativo = true
        };
        db.Carteiras.Add(wallet);

        var cat = new Categorias
        {
            Id = 100,
            FkIdUsuario = 1,
            Nome = "Alimentação",
            Tipo = "DESPESA",
            Ativo = true
        };
        db.Categorias.Add(cat);

        var now = DateTime.Today;
        db.Transacoes.Add(new Transacoes
        {
            Id = 500,
            FkIdUsuario = 1,
            FkIdCarteira = 10,
            FkIdCategoria = 100,
            Descricao = "Supermercado Extra",
            Tipo = "DESPESA",
            Valor = 250.00m,
            DataTransacao = now,
            MesReferencia = (short)now.Month,
            AnoReferencia = now.Year
        });

        await db.SaveChangesAsync();

        var builder = new FafnirContextBuilder(db, CreateAiOptions());

        // Act
        var context = await builder.GetRelevantContextAsync(1, "Quanto gastei esse mês?", null, now.Month, now.Year, CancellationToken.None);
        var json = JsonSerializer.Serialize(context);

        // Assert - strictly ensure no PII or credentials are present
        Assert.DoesNotContain("carlos.sauro@exemplo.com.br", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("supersecretpasswordsalt", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SenhaHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FkIdUsuario", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FkIdCarteira", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FkIdCategoria", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("financial_summary", context.Intent);
        Assert.NotNull(context.Summary);
        Assert.Equal(250.00m, context.Summary.TotalExpenses);
    }

    [Fact]
    public async Task CategoryQuestion_Generates_CategoryContextOnly()
    {
        // Arrange
        using var db = CreateInMemoryContext(Guid.NewGuid().ToString());
        var catFood = new Categorias { Id = 1, FkIdUsuario = 1, Nome = "Alimentação", Tipo = "DESPESA", Ativo = true };
        var catTransport = new Categorias { Id = 2, FkIdUsuario = 1, Nome = "Transporte", Tipo = "DESPESA", Ativo = true };
        db.Categorias.AddRange(catFood, catTransport);

        var now = DateTime.Today;
        var prevMonth = now.AddMonths(-1);

        // Current month transactions
        db.Transacoes.Add(new Transacoes { Id = 1, FkIdUsuario = 1, FkIdCarteira = 1, FkIdCategoria = 1, Descricao = "Mercado", Tipo = "DESPESA", Valor = 600m, DataTransacao = now, MesReferencia = (short)now.Month, AnoReferencia = now.Year });
        db.Transacoes.Add(new Transacoes { Id = 2, FkIdUsuario = 1, FkIdCarteira = 1, FkIdCategoria = 2, Descricao = "Combustivel", Tipo = "DESPESA", Valor = 300m, DataTransacao = now, MesReferencia = (short)now.Month, AnoReferencia = now.Year });
        db.Transacoes.Add(new Transacoes { Id = 3, FkIdUsuario = 1, FkIdCarteira = 1, FkIdCategoria = null, Descricao = "Salário", Tipo = "RECEITA", Valor = 3000m, DataTransacao = now, MesReferencia = (short)now.Month, AnoReferencia = now.Year });

        // Previous month transaction for food
        db.Transacoes.Add(new Transacoes { Id = 4, FkIdUsuario = 1, FkIdCarteira = 1, FkIdCategoria = 1, Descricao = "Mercado Passado", Tipo = "DESPESA", Valor = 500m, DataTransacao = prevMonth, MesReferencia = (short)prevMonth.Month, AnoReferencia = prevMonth.Year });

        // Budget for food: 500
        db.OrcamentosMensais.Add(new OrcamentosMensais { Id = 1, FkIdUsuario = 1, FkIdCategoria = 1, MesReferencia = (short)now.Month, AnoReferencia = now.Year, ValorLimite = 500m });

        await db.SaveChangesAsync();

        var builder = new FafnirContextBuilder(db, CreateAiOptions());

        // Act
        var context = await builder.GetRelevantContextAsync(1, "Estou gastando muito com alimentação?", null, now.Month, now.Year, CancellationToken.None);

        // Assert
        Assert.Equal("category_analysis", context.Intent);
        Assert.NotNull(context.Category);
        Assert.Equal("Alimentação", context.Category.CategoryName);
        Assert.Equal(600m, context.Category.CurrentMonthAmount);
        Assert.Equal(900m, context.Category.TotalMonthExpenses);
        Assert.Equal(500m, context.Category.PreviousMonthAmount);
        Assert.Equal(20.0m, context.Category.MonthlyChangePercent); // (600 - 500) / 500 = +20%
        Assert.True(context.Category.IsOverBudget); // 600 > 500
        Assert.Equal(-100m, context.Category.BudgetRemaining);
        Assert.Null(context.Affordability);
        Assert.Null(context.Debts);
    }

    [Fact]
    public async Task GoalsQuestion_Generates_GoalsContext()
    {
        // Arrange
        using var db = CreateInMemoryContext(Guid.NewGuid().ToString());
        var futureDate = DateTime.Today.AddMonths(10);
        db.Metas.Add(new Metas
        {
            Id = 1,
            FkIdUsuario = 1,
            Nome = "Reserva de Emergência",
            TipoMeta = "reserva_emergencia",
            ValorAlvo = 10000m,
            ValorAtual = 4000m,
            DataFim = futureDate,
            Ativa = true
        });
        await db.SaveChangesAsync();

        var builder = new FafnirContextBuilder(db, CreateAiOptions());

        // Act
        var context = await builder.GetRelevantContextAsync(1, "Quanto falta para minha reserva de emergência?", null, null, null, CancellationToken.None);

        // Assert
        Assert.Equal("goals", context.Intent);
        Assert.NotNull(context.Goals);
        Assert.Single(context.Goals);
        var goal = context.Goals[0];
        Assert.Equal("Reserva de Emergência", goal.Name);
        Assert.Equal(10000m, goal.TargetAmount);
        Assert.Equal(4000m, goal.CurrentAmount);
        Assert.Equal(6000m, goal.RemainingAmount);
        Assert.Equal(40.0m, goal.ProgressPercent);
        Assert.NotNull(goal.MonthlyTargetNeeded);
        Assert.False(goal.IsCompleted);
    }

    [Fact]
    public async Task AffordabilityQuestion_Calculates_MetricsAndSeverity()
    {
        // Arrange
        using var db = CreateInMemoryContext(Guid.NewGuid().ToString());
        db.Carteiras.Add(new Carteiras { Id = 1, FkIdUsuario = 1, Nome = "Conta Corrente", Tipo = "CONTA_CORRENTE", SaldoInicial = 5000m, Ativo = true });
        db.Carteiras.Add(new Carteiras { Id = 2, FkIdUsuario = 1, Nome = "Poupança", Tipo = "POUPANCA", SaldoInicial = 1000m, Ativo = true });

        var now = DateTime.Today;
        db.Transacoes.Add(new Transacoes { Id = 1, FkIdUsuario = 1, FkIdCarteira = 1, Descricao = "Salário", Tipo = "RECEITA", Valor = 4000m, DataTransacao = now, MesReferencia = (short)now.Month, AnoReferencia = now.Year });
        db.Transacoes.Add(new Transacoes { Id = 2, FkIdUsuario = 1, FkIdCarteira = 1, Descricao = "Contas", Tipo = "DESPESA", Valor = 2000m, DataTransacao = now, MesReferencia = (short)now.Month, AnoReferencia = now.Year });
        db.Assinaturas.Add(new Assinaturas { Id = 1, FkIdUsuario = 1, FkIdCarteira = 1, Nome = "Internet", Valor = 150m, Ativa = true, DiaCobranca = 10 });

        await db.SaveChangesAsync();

        var builder = new FafnirContextBuilder(db, CreateAiOptions());

        // Act
        var context = await builder.GetRelevantContextAsync(1, "Posso comprar um celular de R$ 2.000?", null, now.Month, now.Year, CancellationToken.None);

        // Assert
        Assert.Equal("affordability", context.Intent);
        Assert.NotNull(context.Affordability);
        var aff = context.Affordability;
        Assert.Equal(2000m, aff.PurchaseAmount);
        Assert.Equal("celular", aff.ItemDescription);
        Assert.Equal(6000m, aff.CurrentAvailableBalance); // 5000 + 1000
        Assert.Equal(4000m, aff.BalanceAfterPurchase); // 6000 - 2000
        Assert.True(aff.CanAffordImmediately);
        Assert.Equal(4000m, aff.MonthlyNetIncome);
        Assert.Equal(2150m, aff.MonthlyAverageExpenses); // 2000 + 150
        Assert.Equal(150m, aff.FixedMonthlyCommitments);
    }

    [Fact]
    public async Task MultiTurnHistory_PreservesCategoryIntent_OnFollowUpQuestion()
    {
        // Arrange
        using var db = CreateInMemoryContext(Guid.NewGuid().ToString());
        var catFood = new Categorias { Id = 1, FkIdUsuario = 1, Nome = "Alimentação", Tipo = "DESPESA", Ativo = true };
        db.Categorias.Add(catFood);

        var now = DateTime.Today;
        var prevMonth = now.AddMonths(-1);

        db.Transacoes.Add(new Transacoes { Id = 1, FkIdUsuario = 1, FkIdCarteira = 1, FkIdCategoria = 1, Descricao = "Restaurante", Tipo = "DESPESA", Valor = 800m, DataTransacao = prevMonth, MesReferencia = (short)prevMonth.Month, AnoReferencia = prevMonth.Year });

        await db.SaveChangesAsync();

        var builder = new FafnirContextBuilder(db, CreateAiOptions());

        var history = new List<FafnirChatMessageDto>
        {
            new("user", "Quanto gastei com alimentação?"),
            new("assistant", "Você gastou R$ 950 com alimentação este mês.")
        };

        // Act - User asks follow-up
        var context = await builder.GetRelevantContextAsync(1, "E no mês passado?", history, null, null, CancellationToken.None);

        // Assert
        Assert.Equal("category_analysis", context.Intent);
        Assert.NotNull(context.Category);
        Assert.Equal("Alimentação", context.Category.CategoryName);
        Assert.Equal($"{prevMonth.Year:D4}-{prevMonth.Month:D2}", context.Period);
    }

    [Fact]
    public async Task DebtQuestion_Generates_DebtSummaryContext()
    {
        // Arrange
        using var db = CreateInMemoryContext(Guid.NewGuid().ToString());
        db.Metas.Add(new Metas
        {
            Id = 1,
            FkIdUsuario = 1,
            Nome = "Quitar Cartão de Crédito",
            TipoMeta = "quitar_divida",
            ValorAlvo = 5000m,
            ValorAtual = 2000m,
            Ativa = true
        });

        db.Assinaturas.Add(new Assinaturas
        {
            Id = 1,
            FkIdUsuario = 1,
            FkIdCarteira = 1,
            Nome = "Parcelamento Seguro",
            Valor = 300m,
            Ativa = true,
            DiaCobranca = 5
        });

        await db.SaveChangesAsync();

        var builder = new FafnirContextBuilder(db, CreateAiOptions());

        // Act
        var context = await builder.GetRelevantContextAsync(1, "Quais são minhas dívidas e compromissos?", null, null, null, CancellationToken.None);

        // Assert
        Assert.Equal("debt_summary", context.Intent);
        Assert.NotNull(context.Debts);
        Assert.Equal(5000m, context.Debts.TotalDebtGoals);
        Assert.Equal(3000m, context.Debts.RemainingDebt); // 5000 - 2000
        Assert.Equal(1, context.Debts.ActiveDebtGoalsCount);
        Assert.Equal(300m, context.Debts.MonthlyFixedCommitments);
    }

    [Fact]
    public async Task GetRecentRelevantTransactionsAsync_Respects_ConfiguredLimitAndSanitizes()
    {
        // Arrange
        using var db = CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = DateTime.Today;
        for (int i = 1; i <= 10; i++)
        {
            db.Transacoes.Add(new Transacoes
            {
                Id = i,
                FkIdUsuario = 1,
                FkIdCarteira = 1,
                Descricao = $"Compra {i} de Fulano",
                Tipo = "DESPESA",
                Valor = i * 10m,
                DataTransacao = now.AddDays(-i),
                MesReferencia = (short)now.Month,
                AnoReferencia = now.Year
            });
        }
        await db.SaveChangesAsync();

        var builder = new FafnirContextBuilder(db, CreateAiOptions()); // MaxRecentTransactions = 5

        // Act
        var transactions = await builder.GetRecentRelevantTransactionsAsync(1, null, 10, CancellationToken.None);

        // Assert
        Assert.Equal(5, transactions.Count); // Capped at 5
        foreach (var tx in transactions)
        {
            Assert.NotEmpty(tx.Date);
            Assert.True(tx.Amount > 0);
            Assert.Equal("despesa", tx.Type);
        }
    }
}
