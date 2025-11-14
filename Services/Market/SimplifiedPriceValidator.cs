using System;
using CryptoTradingApp.Models.Market;
using Microsoft.Extensions.Logging;

namespace CryptoTradingApp.Services.Market;

/// <summary>
/// Simplified price validator for market quotes
/// </summary>
public class SimplifiedPriceValidator : ISimplifiedPriceValidator
{
    private readonly ILogger<SimplifiedPriceValidator> _logger;

    public SimplifiedPriceValidator(ILogger<SimplifiedPriceValidator> logger)
    {
        _logger = logger;
    }

    public bool IsValid(MarketQuote quote)
    {
        return ValidateQuote(quote).IsValid;
    }

    public ValidationResult ValidateQuote(MarketQuote quote)
    {
        if (quote == null)
        {
            return ValidationResult.Fail("Quote is null");
        }

        if (string.IsNullOrWhiteSpace(quote.Symbol))
        {
            return ValidationResult.Fail("Symbol is empty");
        }

        if (quote.Bid <= 0)
        {
            _logger.LogWarning("Invalid Bid price for {Symbol}: {Bid}", quote.Symbol, quote.Bid);
            return ValidationResult.Fail($"Bid price must be positive: {quote.Bid}");
        }

        if (quote.Ask <= 0)
        {
            _logger.LogWarning("Invalid Ask price for {Symbol}: {Ask}", quote.Symbol, quote.Ask);
            return ValidationResult.Fail($"Ask price must be positive: {quote.Ask}");
        }

        if (quote.Bid >= quote.Ask)
        {
            _logger.LogWarning(
                "Invalid Bid/Ask spread for {Symbol}: Bid={Bid} >= Ask={Ask}",
                quote.Symbol, quote.Bid, quote.Ask);
            return ValidationResult.Fail($"Bid ({quote.Bid}) must be less than Ask ({quote.Ask})");
        }

        if (quote.Last <= 0)
        {
            _logger.LogWarning("Invalid Last price for {Symbol}: {Last}", quote.Symbol, quote.Last);
            return ValidationResult.Fail($"Last price must be positive: {quote.Last}");
        }

        // Check if quote is recent (within last 5 minutes)
        var age = DateTime.UtcNow - quote.Timestamp;
        if (age > TimeSpan.FromMinutes(5))
        {
            _logger.LogWarning(
                "Stale quote for {Symbol}: Age={Age:F1} minutes",
                quote.Symbol, age.TotalMinutes);
            // Don't fail, just warn
        }

        return ValidationResult.Success();
    }
}
