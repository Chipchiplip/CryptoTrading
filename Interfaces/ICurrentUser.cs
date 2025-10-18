using System.Security.Claims;

namespace CryptoTrading.Interfaces
{
    public interface ICurrentUser
    {
        int? UserId { get; }
        string? Email { get; }
        bool IsAuthenticated { get; }
        ClaimsPrincipal? User { get; }
        bool IsInRole(string role);
        string? GetClaim(string claimType);
    }
}
