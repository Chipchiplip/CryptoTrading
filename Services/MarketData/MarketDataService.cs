using CryptoTrading.Interfaces;

namespace CryptoTrading.Services.MarketData;

public class MarketDataService : IMarketDataService
{
    private readonly IUnitOfWork _unitOfWork;

    public MarketDataService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CryptocurrencyDto>> GetCryptocurrenciesAsync()
    {
        // TODO: Implement get cryptocurrencies logic
        throw new NotImplementedException("Get cryptocurrencies functionality will be implemented by team member");
    }

    public async Task<CryptocurrencyDto?> GetCryptocurrencyAsync(string symbol)
    {
        // TODO: Implement get cryptocurrency by symbol logic
        throw new NotImplementedException("Get cryptocurrency functionality will be implemented by team member");
    }

    public async Task<IEnumerable<PriceDto>> GetPricesAsync(string[] symbols)
    {
        // TODO: Implement get prices logic
        throw new NotImplementedException("Get prices functionality will be implemented by team member");
    }

    public async Task<MarketStatsDto> GetMarketStatsAsync()
    {
        // TODO: Implement get market stats logic
        throw new NotImplementedException("Get market stats functionality will be implemented by team member");
    }

    public async Task UpdatePricesFromApiAsync()
    {
        // TODO: Implement price update from external API logic
        throw new NotImplementedException("Price update functionality will be implemented by team member");
    }

    public async Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAsync(string symbol, int days = 30)
    {
        // TODO: Implement get price history logic
        throw new NotImplementedException("Get price history functionality will be implemented by team member");
    }
}
