using CryptoTrading.Models.Market;

namespace CryptoTrading.Services.Market;

/// <summary>
/// Simplified interface for validating market quotes
/// </summary>
public interface ISimplifiedPriceValidator
{
    /// <summary>
    /// Validates a market quote
    /// </summary>
    bool IsValid(MarketQuote quote);

    /// <summary>
    /// Validates a market quote with detailed result
    /// </summary>
    ValidationResult ValidateQuote(MarketQuote quote);
}

/// <summary>
/// Simple validation result
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Fail(string error) => new() { IsValid = false, ErrorMessage = error };
}
