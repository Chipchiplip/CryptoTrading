using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using CryptoTrading.Services;
using CryptoTrading.Interfaces;

namespace CryptoTrading.Attributes;

/// <summary>
/// Authorization attribute that restricts access to Pro (PlanType = 1) and Premium (PlanType = 2) users only.
/// Free users (PlanType = 0) will receive a 403 Forbidden response.
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
        
        // PlanType: 0 = Free, 1 = Pro, 2 = Premium
        if (planType == 0) // Free user
        {
            context.Result = new ForbidObjectResult(new 
            { 
                message = "AI features are only available for Pro and Premium users. Please upgrade your subscription.",
                requiresUpgrade = true,
                currentPlan = "Free",
                availablePlans = new[] { "Pro", "Premium" }
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

