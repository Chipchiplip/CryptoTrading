namespace CryptoTrading.Interfaces
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
        DateTime Now { get; }
        DateOnly Today { get; }
        TimeOnly TimeOfDay { get; }
    }

    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public DateTime Now => DateTime.Now;
        public DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
        public TimeOnly TimeOfDay => TimeOnly.FromDateTime(DateTime.Now);
    }
}
