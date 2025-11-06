using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CryptoTrading.Models;
using Microsoft.AspNetCore.SignalR;
using CryptoTrading.Hubs;

namespace CryptoTrading.Services
{
    public interface ICoinGeckoService
    {
        Task<List<Crypto>> GetMarketDataAsync();
        Task<List<PriceHistory>> GetPriceHistoryAsync(string coinId, int days = 7);
        Task<MarketStats> GetMarketStatsAsync();
    }

    public class CoinGeckoService : ICoinGeckoService
    {
        private readonly HttpClient _httpClient;
        private readonly ICryptoCacheService _cacheService;
        private readonly ILogger<CoinGeckoService> _logger;
        private readonly string? _apiKey;
        private readonly IHubContext<MarketHub>? _hubContext;

        public CoinGeckoService(HttpClient httpClient, ICryptoCacheService cacheService, ILogger<CoinGeckoService> logger, IConfiguration configuration, IHubContext<MarketHub>? hubContext = null)
        {
            _httpClient = httpClient;
            _cacheService = cacheService;
            _logger = logger;
            _apiKey = configuration["CoinGecko:ApiKey"];
            _hubContext = hubContext;
        }

        public async Task<List<Crypto>> GetMarketDataAsync()
        {
            // Try to get from cache first
            if (_cacheService.TryGetCryptoData(out var cachedData))
            {
                _logger.LogInformation($"Using cached crypto data ({cachedData?.Count ?? 0} coins)");
                // Even when using cache, broadcast so clients keep receiving updates
                if (_hubContext != null && cachedData != null)
                {
                    await _hubContext.Clients.All.SendAsync("ReceivePriceList", cachedData);
                }
                return cachedData ?? new List<Crypto>();
            }

            try
            {
                _logger.LogInformation("Fetching market data from CoinGecko API...");
                
                // Add small delay to respect rate limits
                await Task.Delay(1000);
                
                var url = "coins/markets?vs_currency=usd&order=market_cap_desc&per_page=50&page=1&sparkline=false&price_change_percentage=1h,24h,7d";
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    url += $"&x_cg_demo_api_key={_apiKey}";
                }
                
                // Get raw JSON to log and ensure proper deserialization
                var httpResponse = await _httpClient.GetAsync(url);
                httpResponse.EnsureSuccessStatusCode();
                
                var jsonString = await httpResponse.Content.ReadAsStringAsync();
                
                // Log first coin to debug image field
                if (!string.IsNullOrEmpty(jsonString))
                {
                    var jsonDoc = JsonDocument.Parse(jsonString);
                    if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array && jsonDoc.RootElement.GetArrayLength() > 0)
                    {
                        var firstCoin = jsonDoc.RootElement[0];
                        _logger.LogInformation($"Sample coin from API - Has 'image' field: {firstCoin.TryGetProperty("image", out var img)}, Image value: {img.GetString() ?? "null"}");
                    }
                }
                
                // Deserialize with case-insensitive matching
                // Model uses [JsonPropertyName("image")] which should map correctly
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var response = JsonSerializer.Deserialize<List<Crypto>>(jsonString, jsonOptions);
                
                _logger.LogInformation($"Received {response?.Count ?? 0} coins from API");
                
                if (response != null && response.Count > 0)
                {
                    // Ensure image URLs are populated - CoinGecko API should return "image" field
                    // If missing, we can construct from coin ID (fallback)
                    foreach (var crypto in response)
                    {
                        if (string.IsNullOrEmpty(crypto.Image) && !string.IsNullOrEmpty(crypto.Id))
                        {
                            // Fallback: Construct CoinGecko image URL from coin ID
                            // Format: https://assets.coingecko.com/coins/images/{coin_id_number}/standard/{coin_id}.png
                            // But we don't have coin_id_number, so we'll leave it null and let frontend handle fallback
                            // CoinGecko markets endpoint should include image field, so this is just a safety check
                        }
                    }
                    
                    // Log first coin to verify image field is populated
                    var firstCoin = response[0];
                    _logger.LogInformation($"First coin - Id: {firstCoin.Id}, Name: {firstCoin.Name}, Image: {firstCoin.Image ?? "NULL"}");
                    
                    // Cache the successful response
                    _cacheService.SetCryptoData(response);
                    // Broadcast price list to SignalR clients if hub available
                    if (_hubContext != null)
                    {
                        await _hubContext.Clients.All.SendAsync("ReceivePriceList", response);
                    }
                    return response;
                }
                
                return new List<Crypto>();
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("403") || httpEx.Message.Contains("429"))
            {
                _logger.LogWarning("Rate limited by CoinGecko API. Falling back to cache.");
                if (_cacheService.TryGetCryptoData(out var last))
                {
                    return last ?? new List<Crypto>();
                }
                var mockData = GetMockCryptoData();
                _cacheService.SetCryptoData(mockData);
                return mockData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching market data");
                if (_cacheService.TryGetCryptoData(out var last))
                {
                    return last ?? new List<Crypto>();
                }
                return new List<Crypto>();
            }
        }

        private List<Crypto> GetMockCryptoData()
        {
            var random = new Random();
            return new List<Crypto>
            {
                new Crypto
                {
                    Id = "bitcoin",
                    Symbol = "btc",
                    Name = "Bitcoin",
                    CurrentPrice = 107000 + random.Next(-2000, 2000),
                    MarketCap = 2100000000000,
                    PriceChangePercentage1h = (decimal)(random.NextDouble() * 2 - 1), // -1% to +1%
                    PriceChangePercentage24h = (decimal)(random.NextDouble() * 6 - 3), // -3% to +3%
                    PriceChangePercentage7d = (decimal)(random.NextDouble() * 20 - 10), // -10% to +10%
                    TotalVolume = 15000000000,
                    CirculatingSupply = 19000000,
                    LastUpdated = DateTime.UtcNow
                },
                new Crypto
                {
                    Id = "ethereum",
                    Symbol = "eth",
                    Name = "Ethereum",
                    CurrentPrice = 3900 + random.Next(-200, 200),
                    MarketCap = 470000000000,
                    PriceChangePercentage1h = (decimal)(random.NextDouble() * 2 - 1),
                    PriceChangePercentage24h = (decimal)(random.NextDouble() * 6 - 3),
                    PriceChangePercentage7d = (decimal)(random.NextDouble() * 20 - 10),
                    TotalVolume = 8000000000,
                    CirculatingSupply = 120000000,
                    LastUpdated = DateTime.UtcNow
                },
                new Crypto
                {
                    Id = "binancecoin",
                    Symbol = "bnb",
                    Name = "BNB",
                    CurrentPrice = 650 + random.Next(-50, 50),
                    MarketCap = 100000000000,
                    PriceChangePercentage1h = (decimal)(random.NextDouble() * 2 - 1),
                    PriceChangePercentage24h = (decimal)(random.NextDouble() * 6 - 3),
                    PriceChangePercentage7d = (decimal)(random.NextDouble() * 20 - 10),
                    TotalVolume = 2000000000,
                    CirculatingSupply = 150000000,
                    LastUpdated = DateTime.UtcNow
                },
                new Crypto
                {
                    Id = "ripple",
                    Symbol = "xrp",
                    Name = "XRP",
                    CurrentPrice = 2.3m + (decimal)(random.NextDouble() * 0.4 - 0.2),
                    MarketCap = 120000000000,
                    PriceChangePercentage1h = (decimal)(random.NextDouble() * 2 - 1),
                    PriceChangePercentage24h = (decimal)(random.NextDouble() * 6 - 3),
                    PriceChangePercentage7d = (decimal)(random.NextDouble() * 20 - 10),
                    TotalVolume = 5000000000,
                    CirculatingSupply = 53000000000,
                    LastUpdated = DateTime.UtcNow
                }, 
                new Crypto
                {
                    Id = "solana",
                    Symbol = "sol",
                    Name = "Solana",
                    CurrentPrice = 180 + random.Next(-20, 20),
                    MarketCap = 85000000000,
                    PriceChangePercentage1h = (decimal)(random.NextDouble() * 2 - 1),
                    PriceChangePercentage24h = (decimal)(random.NextDouble() * 6 - 3),
                    PriceChangePercentage7d = (decimal)(random.NextDouble() * 20 - 10),
                    TotalVolume = 3000000000,
                    CirculatingSupply = 475000000,
                    LastUpdated = DateTime.UtcNow
                }
            };
        }

        public async Task<List<PriceHistory>> GetPriceHistoryAsync(string coinId, int days = 7)
        {
            try
            {
                // Add delay to respect rate limits
                await Task.Delay(500);
                
                var url = $"coins/{coinId}/market_chart?vs_currency=usd&days={days}";
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    url += $"&x_cg_demo_api_key={_apiKey}";
                }
                
                // Get raw JSON string first
                var httpResponse = await _httpClient.GetAsync(url);
                httpResponse.EnsureSuccessStatusCode();
                
                var jsonString = await httpResponse.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    _logger.LogWarning("Empty response from CoinGecko API for {CoinId}", coinId);
                    return new List<PriceHistory>();
                }
                
                var jsonDoc = JsonDocument.Parse(jsonString);
                var priceHistory = new List<PriceHistory>();
                
                if (jsonDoc.RootElement.TryGetProperty("prices", out var pricesElement) && pricesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var priceElement in pricesElement.EnumerateArray())
                    {
                        try
                        {
                            if (priceElement.ValueKind == JsonValueKind.Array && priceElement.GetArrayLength() >= 2)
                            {
                                var timestamp = priceElement[0].GetInt64();
                                var price = priceElement[1].GetDecimal();
                                
                                priceHistory.Add(new PriceHistory
                                {
                                    CoinId = coinId,
                                    Price = price,
                                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error parsing price element for {CoinId}", coinId);
                            // Continue with next element
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Response from CoinGecko API for {CoinId} does not contain 'prices' array. Response: {Response}", coinId, jsonString.Substring(0, Math.Min(200, jsonString.Length)));
                }
                
                _logger.LogInformation("Parsed {Count} price history items for {CoinId}", priceHistory.Count, coinId);
                return priceHistory;
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("429") || httpEx.Message.Contains("Too Many Requests"))
            {
                _logger.LogWarning("Rate limited by CoinGecko API for price history {CoinId}. Returning empty list.", coinId);
                return new List<PriceHistory>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching price history for {CoinId}", coinId);
                return new List<PriceHistory>();
            }
        }

        public async Task<MarketStats> GetMarketStatsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching market stats from CoinGecko API...");
                var url = "global";
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    url += $"?x_cg_demo_api_key={_apiKey}";
                }
                var response = await _httpClient.GetFromJsonAsync<GlobalApiResponse>(url);
                
                _logger.LogInformation($"Market stats response received: {response != null}");
                
                var stats = new MarketStats();
                if (response?.Data != null)
                {
                    var data = response.Data;
                    stats.TotalMarketCap = data.TotalMarketCap?.GetValueOrDefault("usd", 0) ?? 0;
                    stats.TotalVolume = data.TotalVolume?.GetValueOrDefault("usd", 0) ?? 0;
                    stats.ActiveCryptocurrencies = data.ActiveCryptocurrencies;
                    stats.MarketCapChangePercentage24h = data.MarketCapChangePercentage24h;
                    // Map dominance
                    if (data.MarketCapPercentage != null)
                    {
                        if (data.MarketCapPercentage.TryGetValue("btc", out var btc))
                        {
                            stats.BtcDominance = btc;
                        }
                        if (data.MarketCapPercentage.TryGetValue("eth", out var eth))
                        {
                            stats.EthDominance = eth;
                        }
                    }
                    
                    _logger.LogInformation($"Market cap: {stats.TotalMarketCap}, Volume: {stats.TotalVolume}");
                }
                // Cache and broadcast stats
                _cacheService.SetMarketStats(stats);
                if (_hubContext != null)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveMarketStats", stats);
                }
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching market stats");
                if (_cacheService.TryGetMarketStats(out var last) && last != null)
                {
                    return last;
                }
                return new MarketStats();
            }
        }
    }
}
