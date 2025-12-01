using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using CryptoTrading.Services;
using CryptoTrading.Interfaces;

namespace CryptoTrading.Attributes;

/// <summary>
/// Authorization attribute that restricts access to Premium (PlanType = 2) users only.
/// Free users (PlanType = 0) will receive a 403 Forbidden response with upgrade info.
/// </summary>
public class RequireProOrPremiumAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Get services from DI container
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
        var subscriptionService = context.HttpContext.RequestServices.GetRequiredService<ISubscriptionService>();

        var userId = currentUser.UserId;
        if (userId == null)
        {
            context.Result = new UnauthorizedObjectResult(new 
            { 
                message = "User not authenticated",
                requiresAuth = true
            });
            return;
        }

        var planType = await subscriptionService.GetUserPlanTypeAsync(userId.Value);
        
        // PlanType: 0 = Free, 2 = Premium (legacy PlanType 1 is no longer used)
        if (planType != 2)
        {
            context.Result = new ForbidObjectResult(new 
            { 
                message = "Tính năng này chỉ dành cho gói Premium. Vui lòng nâng cấp để sử dụng AI chat, bot AI và portfolio chi tiết.",
                requiresUpgrade = true,
                currentPlan = planType == 0 ? "Free" : "Unknown",
                availablePlans = new[] { "Premium" }
            });
            return;
        }
    }
}

/// <summary>
/// Custom result class that returns 403 Forbidden with JSON body
/// </summary>
public class ForbidObjectResult : ObjectResult
{
    public ForbidObjectResult(object? value) : base(value)
    {
        StatusCode = 403;
    }
}

