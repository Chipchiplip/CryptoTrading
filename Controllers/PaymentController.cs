using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Services.Payment;
using CryptoTrading.Data;
using CryptoTrading.Models;
using CryptoTrading.Models.Payment;
using CryptoTrading.Interfaces;
using CryptoTrading.Services;
using Microsoft.EntityFrameworkCore;

namespace CryptoTrading.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IVnPayService _vnPayService;
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<PaymentController> _logger;
    private readonly IConfiguration _configuration;
    private readonly ISubscriptionService _subscriptionService;

    public PaymentController(
        IVnPayService vnPayService,
        ApplicationDbContext context,
        ICurrentUser currentUser,
        ILogger<PaymentController> logger,
        IConfiguration configuration,
        ISubscriptionService subscriptionService)
    {
        _vnPayService = vnPayService;
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
        _configuration = configuration;
        _subscriptionService = subscriptionService;
    }

    /// <summary>
    /// Get payment info
    /// </summary>
    [HttpGet]
    public IActionResult GetPaymentInfo()
    {
        return Ok(new { message = "Payment API - Ready for implementation", endpoints = new[] { "plans", "transactions", "deposits", "withdrawals" } });
    }

    /// <summary>
    /// Get subscription plans
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public IActionResult GetPlans()
    {
        var plans = new object[]
        {
            new
            {
                id = 0,
                name = "Free",
                price = 0m,
                priceVnd = 0,
                period = "month",
                features = new[]
                {
                    "Basic trading features",
                    "10 trades per day",
                    "Email support",
                    "Standard trading fees (0.2%)"
                }
            },
            new
            {
                id = 1,
                name = "Pro",
                price = 29m,
                priceVnd = 696000, // ~29 USD * 24000
                period = "month",
                features = new[]
                {
                    "All Free features",
                    "Unlimited trades",
                    "Priority support",
                    "Reduced fees (0.1%)",
                    "Advanced charts",
                    "API access"
                }
            },
            new
            {
                id = 2,
                name = "Premium",
                price = 99m,
                priceVnd = 2376000, // ~99 USD * 24000
                period = "month",
                features = new[]
                {
                    "All Pro features",
                    "24/7 dedicated support",
                    "Lowest fees (0.05%)",
                    "Advanced analytics",
                    "Custom trading bots",
                    "Priority withdrawals",
                    "Personal account manager"
                }
            }
        };
        
        return Ok(new { plans });
    }

    /// <summary>
    /// Create checkout session for subscription via VNPay
    /// </summary>
    [HttpPost("subscription/checkout")]
    [Authorize]
    public async Task<IActionResult> CreateSubscriptionCheckout([FromBody] CreateSubscriptionCheckoutDto dto)
    {
        try
        {
            var userId = _currentUser.UserId;
            if (userId == null)
                return Unauthorized();

            // Validate plan type
            if (dto.PlanType < 0 || dto.PlanType > 2)
                return BadRequest(new { message = "Invalid plan type" });

            // Free plan không cần thanh toán
            if (dto.PlanType == 0)
            {
                var periodStart = DateTime.UtcNow;
                var periodEnd = periodStart.AddMonths(1);
                await _subscriptionService.CreateOrUpdateSubscriptionAsync(userId.Value, 0, periodStart, periodEnd);
                return Ok(new { message = "Free plan activated", planType = 0 });
            }

            // Lấy giá theo plan
            var planPrices = new Dictionary<int, double>
            {
                { 1, 696000 },  // Pro: ~29 USD
                { 2, 2376000 } // Premium: ~99 USD
            };

            var amount = planPrices[dto.PlanType];
            var planNames = new Dictionary<int, string>
            {
                { 1, "Pro" },
                { 2, "Premium" }
            };

            // Lấy thông tin user
            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Tạo order ID unique
            var orderId = $"SUB_{userId}_{DateTime.UtcNow.Ticks}";

            // Tạo payment history record
            var paymentHistory = new PaymentHistory
            {
                UserId = userId.Value,
                Amount = (decimal)amount,
                Currency = "VND",
                Status = "pending",
                VnpayOrderId = orderId,
                PaymentMethod = "VNPay",
                PlanType = dto.PlanType,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.PaymentHistories.Add(paymentHistory);
            await _context.SaveChangesAsync();

            // Tạo payment information model
            var paymentModel = new PaymentInformationModel
            {
                OrderType = "subscription",
                Amount = amount,
                OrderDescription = $"Đăng ký gói {planNames[dto.PlanType]} - {amount:N0} VND/tháng",
                Name = user.FullName ?? user.Email ?? "User",
                UserId = userId.Value
            };

            // Tạo payment URL với subscription callback URL
            var subscriptionCallbackUrl = _configuration["Vnpay:SubscriptionCallbackUrl"]
                ?? _configuration["Vnpay:PaymentBackReturnUrl"]
                ?? "http://localhost:5299/api/payment/subscription/vnpay/callback";
            var paymentUrl = _vnPayService.CreatePaymentUrl(paymentModel, HttpContext, orderId, subscriptionCallbackUrl);

            return Ok(new
            {
                paymentUrl,
                orderId,
                paymentId = paymentHistory.Id,
                planType = dto.PlanType,
                amount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription checkout");
            return StatusCode(500, new { message = "Failed to create checkout session" });
        }
    }

    /// <summary>
    /// Get user subscription
    /// </summary>
    [HttpGet("subscription")]
    [Authorize]
    public async Task<IActionResult> GetSubscription()
    {
        try
        {
            var userId = _currentUser.UserId;
            if (userId == null)
                return Unauthorized();

            var subscription = await _subscriptionService.GetUserSubscriptionAsync(userId.Value);
            var isActive = await _subscriptionService.IsSubscriptionActiveAsync(userId.Value);
            var planType = await _subscriptionService.GetUserPlanTypeAsync(userId.Value);

            if (subscription == null)
            {
                return Ok(new
                {
                    planType = 0,
                    status = "free",
                    isActive = true,
                    currentPeriodStart = DateTime.UtcNow,
                    currentPeriodEnd = DateTime.UtcNow.AddYears(100) // Free plan không hết hạn
                });
            }

            return Ok(new
            {
                planType = subscription.PlanType,
                status = subscription.Status,
                isActive = isActive && subscription.Status == "active",
                currentPeriodStart = subscription.CurrentPeriodStartUtc,
                currentPeriodEnd = subscription.CurrentPeriodEndUtc,
                canceledAt = subscription.CanceledAtUtc
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription");
            return StatusCode(500, new { message = "Failed to get subscription" });
        }
    }

    /// <summary>
    /// Cancel subscription
    /// </summary>
    [HttpPost("subscription/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelSubscription()
    {
        try
        {
            var userId = _currentUser.UserId;
            if (userId == null)
                return Unauthorized();

            var success = await _subscriptionService.CancelSubscriptionAsync(userId.Value);
            if (!success)
                return BadRequest(new { message = "No active subscription to cancel" });

            return Ok(new { message = "Subscription canceled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling subscription");
            return StatusCode(500, new { message = "Failed to cancel subscription" });
        }
    }

    /// <summary>
    /// Callback từ VNPay cho subscription payment
    /// </summary>
    [HttpGet("subscription/vnpay/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> SubscriptionPaymentCallback()
    {
        try
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (!response.Success)
            {
                _logger.LogWarning("VNPay subscription callback failed: OrderId={OrderId}", response.OrderId);
                return Redirect(GetFrontendUrl() + "/subscription?status=failed&message=invalid_signature");
            }

            // Tìm payment history
            var paymentHistory = await _context.PaymentHistories
                .FirstOrDefaultAsync(p => p.VnpayOrderId == response.OrderId);

            if (paymentHistory == null)
            {
                _logger.LogWarning("Payment history not found: OrderId={OrderId}", response.OrderId);
                return Redirect(GetFrontendUrl() + "/subscription?status=failed&message=transaction_not_found");
            }

            // Nếu đã xử lý rồi thì không xử lý lại
            if (paymentHistory.Status != "pending")
            {
                return Redirect(GetFrontendUrl() + $"/subscription?status={paymentHistory.Status.ToLower()}");
            }

            // Kiểm tra response code (00 = success)
            if (response.VnPayResponseCode == "00")
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Cập nhật payment history
                    paymentHistory.Status = "success";
                    paymentHistory.VnpayTransactionId = response.TransactionId;
                    paymentHistory.UpdatedAtUtc = DateTime.UtcNow;

                    // Lấy plan type từ payment history
                    int planType = paymentHistory.PlanType ?? 1; // Default Pro nếu không có

                    // Tạo hoặc cập nhật subscription
                    var periodStart = DateTime.UtcNow;
                    var periodEnd = periodStart.AddMonths(1);

                    var subscription = await _subscriptionService.CreateOrUpdateSubscriptionAsync(
                        paymentHistory.UserId,
                        planType,
                        periodStart,
                        periodEnd,
                        response.TransactionId
                    );

                    // Cập nhật payment history với subscription ID
                    paymentHistory.SubscriptionId = subscription.Id;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Subscription payment successful: UserId={UserId}, PlanType={PlanType}, OrderId={OrderId}",
                        paymentHistory.UserId, planType, response.OrderId);

                    return Redirect(GetFrontendUrl() + $"/subscription?status=success&plan={planType}");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error processing subscription payment - Transaction rolled back: OrderId={OrderId}",
                        response.OrderId);
                    throw;
                }
            }
            else
            {
                paymentHistory.Status = "failed";
                paymentHistory.UpdatedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Redirect(GetFrontendUrl() + "/subscription?status=failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing subscription payment callback");
            return Redirect(GetFrontendUrl() + "/subscription?status=error");
        }
    }

    /// <summary>
    /// Stripe webhook endpoint
    /// </summary>
    [HttpPost("webhook")]
    public IActionResult StripeWebhook()
    {
        // TODO: Implement payment service
        return Ok(new { message = "Stripe webhook endpoint - to be implemented by team member" });
    }

    /// <summary>
    /// Tạo payment URL cho deposit qua VNPay (cần đăng nhập)
    /// </summary>
    [HttpPost("deposit/vnpay")]
    [Authorize]
    public async Task<IActionResult> CreatePaymentUrlVnpay([FromBody] CreateDepositRequest request)
    {
        try
        {
            var userId = _currentUser.UserId;
            if (userId == null)
                return Unauthorized();

            // Validate amount
            if (request.Amount <= 0 || request.Amount < 10000) // Minimum 10,000 VND
                return BadRequest(new { message = "Amount must be at least 10,000 VND" });

            // Lấy thông tin user
            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Tạo order ID unique (sử dụng tick như trong mẫu)
            var tick = DateTime.Now.Ticks.ToString();

            // Tạo deposit transaction record
            var deposit = new DepositTransaction
            {
                UserId = userId.Value,
                OrderId = tick,
                Amount = (decimal)request.Amount,
                Currency = "VND",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };

            _context.DepositTransactions.Add(deposit);
            await _context.SaveChangesAsync();

            // Tạo payment information model
            var paymentModel = new PaymentInformationModel
            {
                OrderType = "other",
                Amount = request.Amount,
                OrderDescription = $"Nạp tiền vào tài khoản {request.Amount:N0} VND",
                Name = user.FullName ?? user.Email ?? "User",
                UserId = userId.Value
            };

            // Tạo payment URL - truyền tick (OrderId) vào để đảm bảo khớp với database
            var paymentUrl = _vnPayService.CreatePaymentUrl(paymentModel, HttpContext, tick);

            return Ok(new
            {
                paymentUrl,
                orderId = tick,
                depositId = deposit.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating VNPay deposit");
            return StatusCode(500, new { message = "Failed to create payment URL" });
        }
    }

    /// <summary>
    /// Callback từ VNPay sau khi thanh toán (không cần đăng nhập)
    /// </summary>
    [HttpGet("vnpay/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentCallbackVnpay()
    {
        try
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (!response.Success)
            {
                _logger.LogWarning("VNPay callback failed: OrderId={OrderId}", response.OrderId);
                return Redirect(GetFrontendUrl() + "/deposit?status=failed&message=invalid_signature");
            }

            // Tìm deposit transaction
            var deposit = await _context.DepositTransactions
                .FirstOrDefaultAsync(d => d.OrderId == response.OrderId);

            if (deposit == null)
            {
                _logger.LogWarning("Deposit transaction not found: OrderId={OrderId}", response.OrderId);
                return Redirect(GetFrontendUrl() + "/deposit?status=failed&message=transaction_not_found");
            }

            // Nếu đã xử lý rồi thì không xử lý lại
            if (deposit.Status != "PENDING")
            {
                return Redirect(GetFrontendUrl() + $"/deposit?status={deposit.Status.ToLower()}");
            }

            // Kiểm tra response code (00 = success)
            if (response.VnPayResponseCode == "00")
            {
                // Sử dụng transaction để đảm bảo atomicity
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Đảm bảo deposit được track
                    if (_context.Entry(deposit).State == EntityState.Detached)
                    {
                        _context.DepositTransactions.Attach(deposit);
                    }

                    deposit.Status = "SUCCESS";
                    deposit.VnpayTransactionId = response.TransactionId;
                    deposit.VnpayResponseCode = response.VnPayResponseCode;
                    deposit.CompletedAt = DateTime.UtcNow;

                    // Convert VND to USD (tỷ giá tạm thời, nên lấy từ API thực tế)
                    var exchangeRate = 24000m; // 1 USD = 24,000 VND
                    var usdAmount = deposit.Amount / exchangeRate;

                    _logger.LogInformation("Processing deposit: UserId={UserId}, OrderId={OrderId}, DepositId={DepositId}, VND={VndAmount}, USD={UsdAmount}", 
                        deposit.UserId, response.OrderId, deposit.Id, deposit.Amount, usdAmount);

                    // Get or create USD wallet (trong transaction, SaveChangesAsync sẽ được gọi nhưng không commit)
                    var usdWallet = await GetOrCreateWalletAsync(deposit.UserId, "FIAT", "USD", null);
                    
                    _logger.LogInformation("USD Wallet found/created: WalletId={WalletId}, UserId={UserId}", 
                        usdWallet.Id, deposit.UserId);

                    // Create wallet movement
                    var walletMovement = new WalletMovement
                    {
                        WalletId = usdWallet.Id,
                        RefType = "DEPOSIT",
                        RefId = deposit.Id,
                        Amount = usdAmount,
                        Note = $"VNPay deposit: {deposit.Amount:N0} VND",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.WalletMovements.Add(walletMovement);
                    _logger.LogInformation("WalletMovement added: WalletId={WalletId}, Amount={Amount}, RefId={RefId}, RefType={RefType}", 
                        walletMovement.WalletId, walletMovement.Amount, walletMovement.RefId, walletMovement.RefType);

                    // Save cả deposit và wallet movement (và wallet nếu mới tạo)
                    var saveResult = await _context.SaveChangesAsync();
                    _logger.LogInformation("Database saved successfully: {SaveResult} entities changed", saveResult);

                    // Commit transaction
                    await transaction.CommitAsync();
                    _logger.LogInformation("Transaction committed: Deposit successful - OrderId={OrderId}, DepositId={DepositId}, Amount={Amount} VND, USD={UsdAmount}, WalletMovementId={MovementId}", 
                        response.OrderId, deposit.Id, deposit.Amount, usdAmount, walletMovement.Id);

                    return Redirect(GetFrontendUrl() + $"/deposit?status=success&amount={deposit.Amount:N0}");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error processing deposit - Transaction rolled back: OrderId={OrderId}, UserId={UserId}, Exception={Exception}", 
                        response.OrderId, deposit.UserId, ex.ToString());
                    throw; // Re-throw để catch bên ngoài xử lý
                }
            }
            else
            {
                deposit.Status = "FAILED";
                deposit.VnpayResponseCode = response.VnPayResponseCode;
                deposit.VnpayMessage = response.VnPayResponseCode;
                await _context.SaveChangesAsync();

                return Redirect(GetFrontendUrl() + "/deposit?status=failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPay callback");
            return Redirect(GetFrontendUrl() + "/deposit?status=error");
        }
    }

    private async Task<Wallet> GetOrCreateWalletAsync(int userId, string assetType, string? currencyCode, int? cryptoId)
    {
        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w =>
                w.UserId == userId &&
                w.AssetType == assetType &&
                w.CurrencyCode == currencyCode &&
                w.CryptocurrencyId == cryptoId);

        if (wallet == null)
        {
            wallet = new Wallet
            {
                UserId = userId,
                AssetType = assetType,
                CurrencyCode = currencyCode,
                CryptocurrencyId = cryptoId
            };
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();
        }

        return wallet;
    }

    private string GetFrontendUrl()
    {
        return _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:3000";
    }
}

// DTOs
public record CreateDepositRequest(double Amount);
public record CreateCheckoutDto(int PlanType, string? SuccessUrl = null, string? CancelUrl = null);
public record CreateSubscriptionCheckoutDto(int PlanType);
