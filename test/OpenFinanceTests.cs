using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using fanfnir_back.DTOs;
using fanfnir_back.Models;
using fanfnir_back.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fanfnir_back.Tests;

public class OpenFinanceTests
{
    private readonly DbContextOptions<FafnirContext> _dbOptions;
    private readonly Mock<IOpenFinanceProvider> _providerMock;
    private readonly Mock<ILogger<OpenFinanceService>> _loggerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public OpenFinanceTests()
    {
        // Setup unique in-memory database per test run
        _dbOptions = new DbContextOptionsBuilder<FafnirContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _providerMock = new Mock<IOpenFinanceProvider>();
        _loggerMock = new Mock<ILogger<OpenFinanceService>>();
        
        _configMock = new Mock<IConfiguration>();
        _configMock.SetupGet(x => x["OpenFinance:WebhookUrl"]).Returns("https://api.test/webhook");
        _configMock.SetupGet(x => x["OpenFinance:RedirectUri"]).Returns("https://api.test/redirect");
        _configMock.SetupGet(x => x["Auth:TokenSecret"]).Returns("my-very-long-test-token-secret-that-has-min-length-32-chars");

        _serviceProviderMock = new Mock<IServiceProvider>();
    }

    private FafnirContext GetDbContext() => new FafnirContext(_dbOptions);

    [Fact]
    public async Task StartConnectionAsync_ShouldReturnConnectToken()
    {
        // Arrange
        using var db = GetDbContext();
        var service = new OpenFinanceService(db, _providerMock.Object, _loggerMock.Object, _configMock.Object, _serviceProviderMock.Object);
        var userId = 1;
        var itemId = "item-123";
        var expectedToken = "pluggy-connect-token-xyz";

        _providerMock.Setup(p => p.GetConnectTokenAsync(itemId, userId.ToString(), "https://api.test/webhook", "https://api.test/redirect", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await service.StartConnectionAsync(userId, itemId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedToken, result.ConnectToken);
    }

    [Fact]
    public void TokenSigner_Verify_ShouldValidateAndDecodeToken()
    {
        // Arrange
        var userId = 42;
        var email = "user@test.com";
        var expires = DateTime.UtcNow.AddMinutes(15);
        
        var inMemorySettings = new Dictionary<string, string?> {
            {"Auth:TokenSecret", "my-very-long-test-token-secret-that-has-min-length-32-chars"}
        };
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        // Act
        var token = TokenSigner.Sign(userId, email, expires, config);
        var isValid = TokenSigner.Verify(token, config, out var verifiedUserId);

        // Assert
        Assert.True(isValid);
        Assert.Equal(userId, verifiedUserId);
    }

    [Fact]
    public void TokenSigner_Verify_ExpiredToken_ShouldBeInvalid()
    {
        // Arrange
        var userId = 42;
        var email = "user@test.com";
        var expires = DateTime.UtcNow.AddMinutes(-5); // expired
        
        var inMemorySettings = new Dictionary<string, string?> {
            {"Auth:TokenSecret", "my-very-long-test-token-secret-that-has-min-length-32-chars"}
        };
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        // Act
        var token = TokenSigner.Sign(userId, email, expires, config);
        var isValid = TokenSigner.Verify(token, config, out var verifiedUserId);

        // Assert
        Assert.False(isValid);
        Assert.Equal(0, verifiedUserId);
    }

    [Fact]
    public async Task ProcessWebhookAsync_ShouldCreateConnectionAndActiveAccounts()
    {
        // Arrange
        using var db = GetDbContext();
        
        // Setup User
        var user = new Usuarios { Id = 10, Nome = "Gustavo", Email = "gustavo@test.com", SenhaHash = "hash" };
        db.Usuarios.Add(user);
        await db.SaveChangesAsync();

        var service = new OpenFinanceService(db, _providerMock.Object, _loggerMock.Object, _configMock.Object, _serviceProviderMock.Object);
        var itemId = Guid.NewGuid();
        
        var payload = new PluggyWebhookPayloadDto(
            Event: "item/created",
            EventId: Guid.NewGuid(),
            ItemId: itemId,
            ClientUserId: "10"
        );

        _providerMock.Setup(p => p.GetItemAsync(itemId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenFinanceItemDto(itemId.ToString(), "UPDATED", "conn-id", "Banco Exemplo", null, null));

        _providerMock.Setup(p => p.GetAccountsAsync(itemId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpenFinanceAccountDto> {
                new OpenFinanceAccountDto("acc-111", itemId.ToString(), "CHECKING_ACCOUNT", "12345", 250.50m, "BRL", "Conta Corrente", "Banco Exemplo")
            });

        // Act
        await service.ProcessWebhookAsync(payload, CancellationToken.None);

        // Assert
        var conn = await db.OpenFinanceConexoes.FirstOrDefaultAsync(x => x.ProvedorItemId == itemId.ToString());
        Assert.NotNull(conn);
        Assert.Equal("UPDATED", conn.Status);
        Assert.Equal(10, conn.FkIdUsuario);

        var acc = await db.ContasBancarias.FirstOrDefaultAsync(x => x.ProvedorContaId == "acc-111");
        Assert.NotNull(acc);
        Assert.Equal("ACTIVE", acc.Status);
        Assert.Equal(250.50m, acc.SaldoAtual);
    }

    [Fact]
    public async Task ProcessWebhookAsync_Idempotency_ShouldNotDuplicateConnectionOrAccounts()
    {
        // Arrange
        using var db = GetDbContext();
        
        var user = new Usuarios { Id = 10, Nome = "Gustavo", Email = "gustavo@test.com", SenhaHash = "hash" };
        db.Usuarios.Add(user);
        await db.SaveChangesAsync();

        var service = new OpenFinanceService(db, _providerMock.Object, _loggerMock.Object, _configMock.Object, _serviceProviderMock.Object);
        var itemId = Guid.NewGuid();
        
        var payload = new PluggyWebhookPayloadDto(
            Event: "item/updated",
            EventId: Guid.NewGuid(),
            ItemId: itemId,
            ClientUserId: "10"
        );

        _providerMock.Setup(p => p.GetItemAsync(itemId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenFinanceItemDto(itemId.ToString(), "UPDATED", "conn-id", "Banco Exemplo", null, null));

        _providerMock.Setup(p => p.GetAccountsAsync(itemId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpenFinanceAccountDto> {
                new OpenFinanceAccountDto("acc-111", itemId.ToString(), "CHECKING_ACCOUNT", "12345", 300.00m, "BRL", "Conta Corrente", "Banco Exemplo")
            });

        // Act - execute twice
        await service.ProcessWebhookAsync(payload, CancellationToken.None);
        await service.ProcessWebhookAsync(payload, CancellationToken.None);

        // Assert
        var connectionsCount = await db.OpenFinanceConexoes.CountAsync(x => x.ProvedorItemId == itemId.ToString());
        Assert.Equal(1, connectionsCount);

        var accountsCount = await db.ContasBancarias.CountAsync(x => x.ProvedorContaId == "acc-111");
        Assert.Equal(1, accountsCount);
    }

    [Fact]
    public async Task SyncBankAccountAsync_ShouldImportTransactionsAndPerformCategorizationAndUpsert()
    {
        // Arrange
        using var db = GetDbContext();

        var user = new Usuarios { Id = 5, Nome = "Gus", Email = "gus@test.com", SenhaHash = "hash" };
        db.Usuarios.Add(user);

        // Add standard categories for categorization mapping test
        db.Categorias.Add(new Categorias { Id = 1, FkIdUsuario = null, Nome = "Alimentação", Tipo = "DESPESA", Ativo = true });
        db.Categorias.Add(new Categorias { Id = 2, FkIdUsuario = null, Nome = "Transporte", Tipo = "DESPESA", Ativo = true });
        
        var conn = new OpenFinanceConnection { Id = 100, FkIdUsuario = 5, Provedor = "Pluggy", ProvedorItemId = "item-xyz", Status = "UPDATED" };
        db.OpenFinanceConexoes.Add(conn);

        var acc = new BankAccount { Id = 50, FkIdUsuario = 5, FkIdConexao = 100, Provedor = "Pluggy", ProvedorContaId = "acc-999", Nome = "Conta Teste", Tipo = "CHECKING_ACCOUNT", Moeda = "BRL", Status = "ACTIVE", InstituicaoId = "inst", InstituicaoNome = "Bank" };
        db.ContasBancarias.Add(acc);
        await db.SaveChangesAsync();

        var service = new OpenFinanceService(db, _providerMock.Object, _loggerMock.Object, _configMock.Object, _serviceProviderMock.Object);

        // Mock 2 transactions (one Alimentação/iFood despesa, one credit)
        var transactions = new List<OpenFinanceTransactionDto> {
            new OpenFinanceTransactionDto("tx-001", "acc-999", DateTime.Today.AddDays(-1), "IFOOD RESTAURANTE", -45.50m, "BRL", "POSTED", "DEBIT", "Food & Drink", "iFood"),
            new OpenFinanceTransactionDto("tx-002", "acc-999", DateTime.Today, "SALARIO RECEBIDO", 2500.00m, "BRL", "POSTED", "CREDIT", "Salary", "Company")
        };

        _providerMock.Setup(p => p.GetTransactionsAsync("acc-999", null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenFinanceTransactionsResponseDto(transactions, null));

        _providerMock.Setup(p => p.GetAccountsAsync("item-xyz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpenFinanceAccountDto> {
                new OpenFinanceAccountDto("acc-999", "item-xyz", "CHECKING_ACCOUNT", "12345", 5000.00m, "BRL", "Conta Teste", "Bank")
            });

        // Act
        var result = await service.SyncBankAccountAsync(5, 50, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5000.00m, result.SaldoAtual);

        // Verify transactions inside local DB
        var txs = await db.TransacoesBancarias.Where(x => x.FkIdContaBancaria == 50).ToListAsync();
        Assert.Equal(2, txs.Count);

        var foodTx = txs.FirstOrDefault(x => x.ProvedorTransacaoId == "tx-001");
        Assert.NotNull(foodTx);
        Assert.Equal("DESPESA", foodTx.Tipo);
        Assert.Equal(45.50m, foodTx.Valor);
        Assert.Equal(1, foodTx.FkIdCategoria); // Categorized as Alimentação (Id=1) based on "IFOOD"

        var salaryTx = txs.FirstOrDefault(x => x.ProvedorTransacaoId == "tx-002");
        Assert.NotNull(salaryTx);
        Assert.Equal("RECEITA", salaryTx.Tipo);
        Assert.Null(salaryTx.FkIdCategoria); // No keyword match
    }

    [Fact]
    public async Task SyncBankAccountAsync_Pagination_ShouldIterateUsingCursor()
    {
        // Arrange
        using var db = GetDbContext();
        
        var user = new Usuarios { Id = 5, Nome = "Gus", Email = "gus@test.com", SenhaHash = "hash" };
        db.Usuarios.Add(user);
        var conn = new OpenFinanceConnection { Id = 100, FkIdUsuario = 5, Provedor = "Pluggy", ProvedorItemId = "item-xyz", Status = "UPDATED" };
        db.OpenFinanceConexoes.Add(conn);
        var acc = new BankAccount { Id = 50, FkIdUsuario = 5, FkIdConexao = 100, Provedor = "Pluggy", ProvedorContaId = "acc-999", Nome = "Conta Teste", Tipo = "CHECKING_ACCOUNT", Moeda = "BRL", Status = "ACTIVE", InstituicaoId = "inst", InstituicaoNome = "Bank" };
        db.ContasBancarias.Add(acc);
        await db.SaveChangesAsync();

        var service = new OpenFinanceService(db, _providerMock.Object, _loggerMock.Object, _configMock.Object, _serviceProviderMock.Object);

        // Page 1: returns tx-1 and cursor "page-2"
        _providerMock.Setup(p => p.GetTransactionsAsync("acc-999", null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenFinanceTransactionsResponseDto(new List<OpenFinanceTransactionDto> {
                new OpenFinanceTransactionDto("tx-1", "acc-999", DateTime.Today.AddDays(-1), "Tx 1", -10m, "BRL", "POSTED", "DEBIT", "Cat", "M1")
            }, "page-2"));

        // Page 2: returns tx-2 and null cursor
        _providerMock.Setup(p => p.GetTransactionsAsync("acc-999", "page-2", It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenFinanceTransactionsResponseDto(new List<OpenFinanceTransactionDto> {
                new OpenFinanceTransactionDto("tx-2", "acc-999", DateTime.Today, "Tx 2", -20m, "BRL", "POSTED", "DEBIT", "Cat", "M2")
            }, null));

        _providerMock.Setup(p => p.GetAccountsAsync("item-xyz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OpenFinanceAccountDto>());

        // Act
        await service.SyncBankAccountAsync(5, 50, CancellationToken.None);

        // Assert
        var txsCount = await db.TransacoesBancarias.CountAsync(x => x.FkIdContaBancaria == 50);
        Assert.Equal(2, txsCount);
    }

    [Fact]
    public async Task DeleteConnectionAsync_ShouldRevokeAndCascadeDelete()
    {
        // Arrange
        using var db = GetDbContext();

        var user = new Usuarios { Id = 5, Nome = "Gus", Email = "gus@test.com", SenhaHash = "hash" };
        db.Usuarios.Add(user);
        var conn = new OpenFinanceConnection { Id = 100, FkIdUsuario = 5, Provedor = "Pluggy", ProvedorItemId = "item-delete-me", Status = "UPDATED" };
        db.OpenFinanceConexoes.Add(conn);
        var acc = new BankAccount { Id = 50, FkIdUsuario = 5, FkIdConexao = 100, Provedor = "Pluggy", ProvedorContaId = "acc-999", Nome = "Conta Teste", Tipo = "CHECKING_ACCOUNT", Moeda = "BRL", Status = "ACTIVE", InstituicaoId = "inst", InstituicaoNome = "Bank" };
        db.ContasBancarias.Add(acc);
        var tx = new BankTransaction { Id = 1, FkIdUsuario = 5, FkIdContaBancaria = 50, Provedor = "Pluggy", ProvedorTransacaoId = "tx-1", DataTransacao = DateTime.Now, Valor = 10m, Descricao = "Test", Tipo = "DESPESA", Moeda = "BRL" };
        db.TransacoesBancarias.Add(tx);
        await db.SaveChangesAsync();

        var service = new OpenFinanceService(db, _providerMock.Object, _loggerMock.Object, _configMock.Object, _serviceProviderMock.Object);

        _providerMock.Setup(p => p.DeleteItemAsync("item-delete-me", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.DeleteConnectionAsync(5, 100, CancellationToken.None);

        // Assert
        Assert.True(result);
        
        // Verify provider deletion was called
        _providerMock.Verify(p => p.DeleteItemAsync("item-delete-me", It.IsAny<CancellationToken>()), Times.Once);

        // Verify database is cleared
        Assert.Null(await db.OpenFinanceConexoes.FindAsync(100));
        
        // In InMemory Database cascade delete is handled manually or depends on context configuration.
        // EF Core InMemory database simulates cascade deleting if relationships are defined. Let's verify:
        var accountsLeft = await db.ContasBancarias.Where(x => x.FkIdConexao == 100).ToListAsync();
        Assert.Empty(accountsLeft);
    }
}
