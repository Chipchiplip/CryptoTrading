namespace CryptoTrading.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    int SubscriptionTier { get; }
    IEnumerable<string> Roles { get; }
}
