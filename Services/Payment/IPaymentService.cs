using CryptoTrading.Controllers;

namespace CryptoTrading.Services.Payment;

public interface IPaymentService
{
    Task<IEnumerable<SubscriptionPlanDto>> GetPlansAsync();
    Task<CheckoutSessionDto> CreateCheckoutSessionAsync(CreateCheckoutDto dto);
    Task<SubscriptionDto?> GetSubscriptionAsync();
    Task HandleWebhookAsync(Microsoft.AspNetCore.Http.HttpRequest request);
    Task<IEnumerable<PaymentHistoryDto>> GetPaymentHistoryAsync();
    Task<bool> CancelSubscriptionAsync();
    Task<bool> IsFeatureEnabledAsync(string featureName);
}

// DTOs
public record SubscriptionPlanDto(int PlanType, string Name, string Description, decimal Price, string Currency, IEnumerable<string> Features);
public record PaymentHistoryDto(Guid Id, decimal Amount, string Currency, string Status, string? Description, DateTime CreatedAtUtc);
