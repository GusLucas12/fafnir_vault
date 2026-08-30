using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace fanfnir_back.Services;

public interface ICdiService
{
    Task<decimal> GetLatestDailyCdiRateAsync();
}

public sealed class CdiService(HttpClient httpClient) : ICdiService
{
    private static decimal _cachedRate = 0.0407m; // 0.0407% standard fallback (based on ~10.75% annual rate)
    private static DateTime? _lastFetch;

    public async Task<decimal> GetLatestDailyCdiRateAsync()
    {
        if (_lastFetch.HasValue && (DateTime.Now - _lastFetch.Value).TotalHours < 24)
        {
            return _cachedRate;
        }

        try
         {
            var response = await httpClient.GetFromJsonAsync<List<CdiApiResponse>>("https://api.bcb.gov.br/dados/serie/bcdata.sgs.12/dados/ultimos/1?formato=json");
            if (response != null && response.Count > 0 && decimal.TryParse(response[0].Valor, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedRate))
            {
                // BCB returns values like "0.040762" which represents 0.040762%
                _cachedRate = parsedRate;
                _lastFetch = DateTime.Now;
                Console.WriteLine($"[INFO] Successfully fetched current CDI rate from BCB API: {_cachedRate}%");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARNING] Failed to fetch CDI rate from BCB API: {ex.Message}. Using fallback: {_cachedRate}%");
        }

        return _cachedRate;
    }

    private sealed class CdiApiResponse
    {
        public string Data { get; set; } = null!;
        public string Valor { get; set; } = null!;
    }
}
