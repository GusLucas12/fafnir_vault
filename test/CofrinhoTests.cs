using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using fanfnir_back.Models;
using fanfnir_back.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace fanfnir_back.Tests;

public class CofrinhoTests
{
    private readonly DbContextOptions<FafnirContext> _dbOptions;
    private readonly Mock<ICdiService> _cdiServiceMock;

    public CofrinhoTests()
    {
        _dbOptions = new DbContextOptionsBuilder<FafnirContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _cdiServiceMock = new Mock<ICdiService>();
        _cdiServiceMock.Setup(x => x.GetLatestDailyCdiRateAsync()).ReturnsAsync(0.0407m); // 0.0407%
    }

    private FafnirContext GetDbContext() => new FafnirContext(_dbOptions);

    [Fact]
    public async Task ProcessarRendimentoCofrinhosAsync_ShouldCalculateAndApplyYieldForBusinessDays()
    {
        // Arrange
        using var db = GetDbContext();
        var service = new CarteirasService(db, _cdiServiceMock.Object);

        // Create a user
        var user = new Usuarios { Id = 1, Nome = "Test", Email = "test@test.com", SenhaHash = "hash" };
        db.Usuarios.Add(user);

        // Create a piggy bank that yields return
        // We'll set the last processed date to a Thursday, so it should process Friday (business day),
        // skip Saturday/Sunday, and not process Monday (if today is Monday) because Monday is not complete yet.
        var creationDate = new DateTime(2026, 8, 20); // Thursday
        var lastProcessed = new DateTime(2026, 8, 20); // Thursday
        
        var wallet = new Carteiras
        {
            Id = 1,
            FkIdUsuario = 1,
            Nome = "Meu Cofrinho",
            Tipo = "COFRINHO",
            SaldoInicial = 1000.00m,
            Ativo = true,
            Rende = true,
            TipoRendimento = "CDI",
            TaxaRendimento = 100.00m,
            UltimoProcessamentoRendimento = lastProcessed,
            DataCriacao = creationDate,
            DataAtualizacao = creationDate
        };
        db.Carteiras.Add(wallet);
        await db.SaveChangesAsync();

        // Act - Mocking system time to Monday, 2026-08-24. Completed days up to yesterday (Sunday, 2026-08-23).
        // The days to process are Friday (2026-08-21), Saturday (2026-08-22), and Sunday (2026-08-23).
        // Only Friday is a business day. So only 1 day of yield should be processed.
        // CDI rate = 0.0407%. 1000 * 0.0407% = 0.407 -> Math.Round(0.407, 2) = 0.41.
        
        // Let's modify the service method slightly or we can just mock DateTime.Today?
        // Since ProcessarRendimentoCofrinhosAsync uses DateTime.Today, we can simulate dates by adjusting
        // the wallet's UltimoProcessamentoRendimento relative to actual DateTime.Today.
        // Let's adjust lastProcessed so that the range [lastProcessed + 1, Today - 1] contains exactly 2 business days.
        // Today is 2026-08-30 (Sunday). Yesterday was 2026-08-29 (Saturday).
        // Let's set UltimoProcessamentoRendimento to 2026-08-26 (Wednesday).
        // Completed days: Thursday (2026-08-27) and Friday (2026-08-28). Both are business days!
        // Day 1 (Thursday): Saldo 1000. Yield = 1000 * 0.000407 = 0.407 -> R$ 0.41. New Saldo = 1000.41
        // Day 2 (Friday): Saldo 1000.41. Yield = 1000.41 * 0.000407 = 0.40716 -> R$ 0.41. New Saldo = 1000.82
        // Total transactions = 2.
        
        wallet.UltimoProcessamentoRendimento = DateTime.Today.AddDays(-4); // e.g. 4 days ago
        // Let's calculate exactly how many business days are in the range [Today-3, Today-1].
        // We'll calculate it dynamically in the test assertion.
        var startDate = wallet.UltimoProcessamentoRendimento.Value.Date;
        var endDate = DateTime.Today.AddDays(-1);
        int expectedBusinessDays = 0;
        for (var date = startDate.AddDays(1); date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                expectedBusinessDays++;
            }
        }

        await service.ProcessarRendimentoCofrinhosAsync(CancellationToken.None);

        // Assert
        var transactions = await db.Transacoes.Where(t => t.FkIdCarteira == wallet.Id).ToListAsync();
        Assert.Equal(expectedBusinessDays, transactions.Count);
        
        if (expectedBusinessDays > 0)
        {
            Assert.True(wallet.SaldoInicial > 1000.00m);
            Assert.Equal(DateTime.Today.AddDays(-1), wallet.UltimoProcessamentoRendimento);
            
            // Check first transaction details
            var firstTx = transactions.OrderBy(t => t.DataTransacao).First();
            Assert.Equal("RECEITA", firstTx.Tipo);
            Assert.Contains("Rendimento CDI", firstTx.Descricao);
            Assert.Null(firstTx.FkIdCategoria);
        }
    }
}
