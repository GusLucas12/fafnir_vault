using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using fanfnir_back.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace fanfnir_back.Services;

public sealed class PluggyProvider : IOpenFinanceProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _apiUrl;
    private readonly ILogger<PluggyProvider> _logger;

    public PluggyProvider(HttpClient httpClient, IConfiguration configuration, ILogger<PluggyProvider> logger)
    {
        _httpClient = httpClient;
        _clientId = configuration["OpenFinance:Pluggy:ClientId"] ?? "";
        _clientSecret = configuration["OpenFinance:Pluggy:ClientSecret"] ?? "";
        _apiUrl = configuration["OpenFinance:Pluggy:ApiUrl"] ?? "https://api.pluggy.ai";
        _logger = logger;
    }

    private async Task<string> AuthenticateAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_clientId) || string.IsNullOrWhiteSpace(_clientSecret))
        {
            throw new InvalidOperationException("Pluggy ClientId e ClientSecret não estão configurados.");
        }

        var url = $"{_apiUrl.TrimEnd('/')}/auth";
        var payload = new { clientId = _clientId, clientSecret = _clientSecret };

        _logger.LogInformation("Autenticando com a API do Pluggy...");
        var response = await _httpClient.PostAsJsonAsync(url, payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: ct);
        if (result == null || string.IsNullOrWhiteSpace(result.ApiKey))
        {
            throw new Exception("Falha ao obter apiKey da resposta de autenticação do Pluggy.");
        }

        return result.ApiKey;
    }

    public async Task<string> GetConnectTokenAsync(string? itemId, string? clientUserId, string? webhookUrl, string? redirectUri, CancellationToken ct)
    {
        var apiKey = await AuthenticateAsync(ct);

        var url = $"{_apiUrl.TrimEnd('/')}/connect_token";
        
        var options = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(clientUserId)) options["clientUserId"] = clientUserId;
        if (!string.IsNullOrWhiteSpace(webhookUrl)) options["webhookUrl"] = webhookUrl;
        if (!string.IsNullOrWhiteSpace(redirectUri)) options["webhookUrl"] = webhookUrl; // In Pluggy Connect, option webhookUrl sets callback

        var payload = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(itemId)) payload["itemId"] = itemId;
        if (options.Count > 0) payload["options"] = options;

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("X-API-KEY", apiKey);

        _logger.LogInformation("Gerando connect token no Pluggy para ItemId={ItemId}, ClientUserId={ClientUserId}...", itemId, clientUserId);
        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ConnectTokenResponse>(cancellationToken: ct);
        if (result == null || string.IsNullOrWhiteSpace(result.AccessToken))
        {
            throw new Exception("Falha ao obter accessToken (connect token) do Pluggy.");
        }

        return result.AccessToken;
    }

    public async Task<OpenFinanceItemDto> GetItemAsync(string itemId, CancellationToken ct)
    {
        var apiKey = await AuthenticateAsync(ct);
        var url = $"{_apiUrl.TrimEnd('/')}/items/{itemId}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-API-KEY", apiKey);

        _logger.LogInformation("Buscando detalhes do Item {ItemId} no Pluggy...", itemId);
        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var rawItem = await response.Content.ReadFromJsonAsync<PluggyItemResponse>(cancellationToken: ct);
        if (rawItem == null) throw new Exception($"Resposta vazia ao buscar o item {itemId}.");

        return new OpenFinanceItemDto(
            rawItem.Id,
            rawItem.Status,
            rawItem.Connector?.Id?.ToString(),
            rawItem.Connector?.Name,
            rawItem.Error?.Code,
            rawItem.Error?.Message
        );
    }

    public async Task<IReadOnlyList<OpenFinanceAccountDto>> GetAccountsAsync(string itemId, CancellationToken ct)
    {
        var apiKey = await AuthenticateAsync(ct);
        var url = $"{_apiUrl.TrimEnd('/')}/accounts?itemId={itemId}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-API-KEY", apiKey);

        _logger.LogInformation("Buscando contas para o Item {ItemId} no Pluggy...", itemId);
        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var res = await response.Content.ReadFromJsonAsync<PluggyAccountsResponse>(cancellationToken: ct);
        var accountsList = new List<OpenFinanceAccountDto>();

        if (res?.Results != null)
        {
            foreach (var acc in res.Results)
            {
                accountsList.Add(new OpenFinanceAccountDto(
                    acc.Id,
                    acc.ItemId,
                    acc.Type,
                    acc.Number ?? "",
                    acc.Balance,
                    acc.CurrencyCode,
                    acc.Name,
                    acc.Provider?.Name ?? "Instituição"
                ));
            }
        }

        return accountsList;
    }

    public async Task<OpenFinanceTransactionsResponseDto> GetTransactionsAsync(string accountId, string? cursor, DateTime? fromDate, CancellationToken ct)
    {
        var apiKey = await AuthenticateAsync(ct);
        var url = $"{_apiUrl.TrimEnd('/')}/v2/transactions?accountId={accountId}";
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            url += $"&next={Uri.EscapeDataString(cursor)}";
        }
        if (fromDate.HasValue)
        {
            url += $"&from={fromDate.Value.ToString("yyyy-MM-dd")}";
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-API-KEY", apiKey);

        _logger.LogInformation("Buscando transações para a conta {AccountId} (cursor={Cursor}, fromDate={FromDate}) no Pluggy...", accountId, cursor, fromDate);
        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var res = await response.Content.ReadFromJsonAsync<PluggyTransactionsResponse>(cancellationToken: ct);
        var list = new List<OpenFinanceTransactionDto>();

        if (res?.Results != null)
        {
            foreach (var tx in res.Results)
            {
                list.Add(new OpenFinanceTransactionDto(
                    tx.Id,
                    tx.AccountId,
                    tx.Date,
                    tx.Description,
                    tx.Amount,
                    tx.CurrencyCode,
                    tx.Status,
                    tx.Type,
                    tx.Category,
                    tx.Merchant?.Name ?? tx.Merchant?.BusinessName
                ));
            }
        }

        return new OpenFinanceTransactionsResponseDto(list, res?.Next);
    }

    public async Task DeleteItemAsync(string itemId, CancellationToken ct)
    {
        var apiKey = await AuthenticateAsync(ct);
        var url = $"{_apiUrl.TrimEnd('/')}/items/{itemId}";

        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Add("X-API-KEY", apiKey);

        _logger.LogInformation("Deletando Item {ItemId} no Pluggy...", itemId);
        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    // Helper classes for Pluggy API deserialization
    private class AuthResponse
    {
        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = null!;
    }

    private class ConnectTokenResponse
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = null!;
    }

    private class PluggyItemResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("status")]
        public string Status { get; set; } = null!;

        [JsonPropertyName("connector")]
        public PluggyConnector? Connector { get; set; }

        [JsonPropertyName("error")]
        public PluggyItemError? Error { get; set; }
    }

    private class PluggyConnector
    {
        [JsonPropertyName("id")]
        public object? Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
    }

    private class PluggyItemError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = null!;

        [JsonPropertyName("message")]
        public string Message { get; set; } = null!;
    }

    private class PluggyAccountsResponse
    {
        [JsonPropertyName("results")]
        public List<PluggyAccount>? Results { get; set; }
    }

    private class PluggyAccount
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("itemId")]
        public string ItemId { get; set; } = null!;

        [JsonPropertyName("type")]
        public string Type { get; set; } = null!;

        [JsonPropertyName("number")]
        public string? Number { get; set; }

        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("currencyCode")]
        public string CurrencyCode { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("provider")]
        public PluggyAccountProvider? Provider { get; set; }
    }

    private class PluggyAccountProvider
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
    }

    private class PluggyTransactionsResponse
    {
        [JsonPropertyName("results")]
        public List<PluggyTransaction>? Results { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }
    }

    private class PluggyTransaction
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("accountId")]
        public string AccountId { get; set; } = null!;

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = null!;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currencyCode")]
        public string CurrencyCode { get; set; } = null!;

        [JsonPropertyName("status")]
        public string Status { get; set; } = null!;

        [JsonPropertyName("type")]
        public string Type { get; set; } = null!;

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("merchant")]
        public PluggyMerchant? Merchant { get; set; }
    }

    private class PluggyMerchant
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("businessName")]
        public string? BusinessName { get; set; }
    }
}
