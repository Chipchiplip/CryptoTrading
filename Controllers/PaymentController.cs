using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Services.Payment;
using CryptoTrading.Data;
using CryptoTrading.Models;
using CryptoTrading.Models.Payment;
using CryptoTrading.Interfaces;
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

    public PaymentController(
        IVnPayService vnPayService,
        ApplicationDbContext context,
        ICurrentUser currentUser,
        ILogger<PaymentController> logger,
        IConfiguration configuration)
    {
        _vnPayService = vnPayService;
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
        _configuration = configuration;
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
    public IActionResult GetPlans()
    {
        // TODO: Implement payment service
        var plans = new object[]
        {
            new { id = 0, name = "Free", price = 0, features = new[] { "Basic trading", "1 watchlist" } },
            new { id = 1, name = "Plus", price = 9.99m, features = new[] { "Advanced trading", "5 watchlists", "Basic bots" } },
            new { id = 2, name = "Pro", price = 29.99m, features = new[] { "Professional trading", "Unlimited watchlists", "Advanced bots", "Priority support" } }
        };
        
        return Ok(new { message = "Get plans endpoint - to be implemented by team member", plans });
    }

    /// <summary>
    /// Create checkout session
    /// </summary>
    [HttpPost("checkout")]
    public IActionResult CreateCheckoutSession([FromBody] CreateCheckoutDto dto)
    {
        // TODO: Implement payment service
        return Ok(new { message = "Create checkout session endpoint - to be implemented by team member", planType = dto.PlanType });
    }

    /// <summary>
    /// Get user subscription
    /// </summary>
    [HttpGet("subscription")]
    public IActionResult GetSubscription()
    {
        // TODO: Implement payment service
        return Ok(new { message = "Get subscription endpoint - to be implemented by team member" });
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
