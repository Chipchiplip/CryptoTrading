namespace CryptoTrading.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
    DateOnly Today { get; }
    DateOnly UtcToday { get; }
}
