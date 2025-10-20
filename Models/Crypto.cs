using System.Text.Json.Serialization;

namespace CryptoTrading.Models
{
    public class Crypto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("current_price")]
        public decimal? CurrentPrice { get; set; }
        
        [JsonPropertyName("market_cap")]
        public decimal? MarketCap { get; set; }
        
        [JsonPropertyName("price_change_24h")]
        public decimal? PriceChange24h { get; set; }
        
        [JsonPropertyName("price_change_percentage_1h_in_currency")]
        public decimal? PriceChangePercentage1h { get; set; }
        
        [JsonPropertyName("price_change_percentage_24h")]
        public decimal? PriceChangePercentage24h { get; set; }
        
        [JsonPropertyName("price_change_percentage_7d_in_currency")]
        public decimal? PriceChangePercentage7d { get; set; }
        
        [JsonPropertyName("total_volume")]
        public decimal? TotalVolume { get; set; }
        
        [JsonPropertyName("circulating_supply")]
        public decimal? CirculatingSupply { get; set; }
        
        [JsonPropertyName("last_updated")]
        public DateTime? LastUpdated { get; set; }
    }

    public class PriceHistory
    {
        public string CoinId { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class MarketStats
    {
        [JsonPropertyName("total_market_cap")]
        public decimal TotalMarketCap { get; set; }
        
        [JsonPropertyName("total_volume")]
        public decimal TotalVolume { get; set; }
        
        [JsonPropertyName("active_cryptocurrencies")]
        public int ActiveCryptocurrencies { get; set; }
        
        [JsonPropertyName("market_cap_change_percentage_24h_usd")]
        public decimal MarketCapChangePercentage24h { get; set; }
    }

    public class GlobalApiResponse
    {
        [JsonPropertyName("data")]
        public GlobalData? Data { get; set; }
    }

    public class GlobalData
    {
        [JsonPropertyName("total_market_cap")]
        public Dictionary<string, decimal>? TotalMarketCap { get; set; }
        
        [JsonPropertyName("total_volume")]
        public Dictionary<string, decimal>? TotalVolume { get; set; }
        
        [JsonPropertyName("active_cryptocurrencies")]
        public int ActiveCryptocurrencies { get; set; }
        
        [JsonPropertyName("market_cap_change_percentage_24h_usd")]
        public decimal MarketCapChangePercentage24h { get; set; }
    }
}
