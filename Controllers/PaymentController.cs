using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CryptoTrading.Services.Payment;
using CryptoTrading.Data;
using CryptoTrading.Models;
using CryptoTrading.Models.Payment;
using CryptoTrading.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stripe;
using Stripe.Checkout;
using System.Net;

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
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        string payload;
        using (var reader = new StreamReader(HttpContext.Request.Body))
        {
            payload = await reader.ReadToEndAsync();
        }

        var webhookSecret = _configuration["Stripe:WebhookSecret"];
        Event stripeEvent;

        try
        {
            if (!string.IsNullOrWhiteSpace(webhookSecret))
            {
                var signatureHeader = Request.Headers["Stripe-Signature"];
                stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, webhookSecret);
            }
            else
            {
                stripeEvent = EventUtility.ParseEvent(payload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe webhook signature validation failed");
            return BadRequest();
        }

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                if (stripeEvent.Data.Object is Session session)
                {
                    await HandleStripeCheckoutCompletedAsync(session);
                }
                break;
            case "payment_intent.payment_failed":
                if (stripeEvent.Data.Object is PaymentIntent paymentIntent)
                {
                    await HandleStripePaymentFailedAsync(paymentIntent);
                }
                break;
            default:
                _logger.LogDebug("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                break;
        }

        return Ok();
    }

    /// <summary>
    /// Táº¡o payment URL cho deposit qua VNPay (cáº§n Ä‘Äƒng nháº­p)
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

            // Láº¥y thÃ´ng tin user
            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Táº¡o order ID unique (sá»­ dá»¥ng tick nhÆ° trong máº«u)
            var tick = DateTime.Now.Ticks.ToString();

            // Táº¡o deposit transaction record
            var deposit = new DepositTransaction
            {
                UserId = userId.Value,
                OrderId = tick,
                Amount = (decimal)request.Amount,
                Currency = "VND",
                Provider = "VNPAY",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };

            _context.DepositTransactions.Add(deposit);
            await _context.SaveChangesAsync();

            // Táº¡o payment information model
            var paymentModel = new PaymentInformationModel
            {
                OrderType = "other",
                Amount = request.Amount,
                OrderDescription = $"Náº¡p tiá»n vÃ o tÃ i khoáº£n {request.Amount:N0} VND",
                Name = user.FullName ?? user.Email ?? "User",
                UserId = userId.Value
            };

            // Táº¡o payment URL - truyá»n tick (OrderId) vÃ o Ä‘á»ƒ Ä‘áº£m báº£o khá»›p vá»›i database
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
    /// Callback tá»« VNPay sau khi thanh toÃ¡n (khÃ´ng cáº§n Ä‘Äƒng nháº­p)
    /// </summary>
    
    /// <summary>
    /// Create Stripe checkout session for USD deposit
    /// </summary>
    [HttpPost("deposit/stripe")]
    [Authorize]
    public async Task<IActionResult> CreateStripeDeposit([FromBody] CreateStripeDepositRequest request)
    {
        try
        {
            var userId = _currentUser.UserId;
            if (userId == null)
                return Unauthorized();

            if (request.Amount <= 0)
                return BadRequest(new { message = "Amount must be greater than 0" });

            const decimal maxStripeAmount = 999999.99m;
            if (request.Amount > maxStripeAmount)
            {
                return BadRequest(new { message = $"Stripe deposit limit is ${maxStripeAmount:N2} per transaction." });
            }

            var currency = string.IsNullOrWhiteSpace(request.Currency)
                ? "USD"
                : request.Currency.Trim().ToUpperInvariant();

            if (currency != "USD")
            {
                return BadRequest(new { message = "Only USD deposits are supported via Stripe at this time" });
            }

            var orderId = $"STRIPE-{Guid.NewGuid():N}";
            var deposit = new DepositTransaction
            {
                UserId = userId.Value,
                OrderId = orderId,
                Amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
                Currency = currency,
                Provider = "STRIPE",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };

            _context.DepositTransactions.Add(deposit);
            await _context.SaveChangesAsync();

            var metadata = new Dictionary<string, string>
            {
                ["depositId"] = deposit.Id.ToString(),
                ["userId"] = userId.Value.ToString(),
                ["orderId"] = deposit.OrderId
            };

            var sessionOptions = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = $"{GetFrontendUrl()}/deposit?status=success&provider=stripe&sessionId={{CHECKOUT_SESSION_ID}}&orderId={orderId}",
                CancelUrl = $"{GetFrontendUrl()}/deposit?status=failed&provider=stripe&sessionId={{CHECKOUT_SESSION_ID}}&orderId={orderId}",
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency.ToLowerInvariant(),
                            UnitAmountDecimal = deposit.Amount * 100,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Account Deposit",
                                Description = $"Deposit to CryptoTrade wallet ({deposit.OrderId})"
                            }
                        }
                    }
                },
                Metadata = metadata,
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = metadata
                }
            };

            StripeClient stripeClient;
            try
            {
                stripeClient = CreateStripeClient();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Stripe secret key missing");
                return StatusCode(500, new { message = "Stripe is not configured" });
            }

            var sessionService = new SessionService(stripeClient);
            var session = await sessionService.CreateAsync(sessionOptions);

            deposit.StripeSessionId = session.Id;
            deposit.StripePaymentIntentId = session.PaymentIntentId;
            deposit.PaymentMethod = "card";
            await _context.SaveChangesAsync();

            _logger.LogInformation("Stripe checkout session created: DepositId={DepositId}, SessionId={SessionId}", deposit.Id, session.Id);

            return Ok(new
            {
                sessionId = session.Id,
                checkoutUrl = session.Url,
                orderId = deposit.OrderId,
                depositId = deposit.Id
            });
        }
        catch (StripeException stripeEx)
        {
            _logger.LogError(stripeEx, "Stripe error while creating deposit");

            var message = stripeEx.Message ?? "Stripe error";
            var statusCode = message.Contains("must be no more than", StringComparison.OrdinalIgnoreCase)
                ? 400
                : 502;

            return StatusCode(statusCode, new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe deposit");
            return StatusCode(500, new { message = "Failed to create Stripe checkout session" });
        }
    }

    /// <summary>
    /// Retrieve Stripe checkout session info for the current user
    /// </summary>
    [HttpGet("stripe/session/{sessionId}")]
    [Authorize]
    public async Task<IActionResult> GetStripeSession(string sessionId)
    {
        try
        {
            var userId = _currentUser.UserId;
            if (userId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(new { message = "sessionId is required" });

            Session session;
            try
            {
                session = await RetrieveStripeSessionAsync(sessionId);
            }
            catch (StripeException stripeEx)
            {
                _logger.LogWarning(stripeEx, "Failed to fetch Stripe session {SessionId}", sessionId);
                return BadRequest(new { message = "Unable to load Stripe session" });
            }

            if (session.Metadata == null ||
                !session.Metadata.TryGetValue("depositId", out var depositIdValue) ||
                !ulong.TryParse(depositIdValue, out var depositId))
            {
                return BadRequest(new { message = "Stripe session missing deposit reference" });
            }

            var deposit = await _context.DepositTransactions
                .FirstOrDefaultAsync(d => d.Id == depositId && d.UserId == userId.Value);

            if (deposit == null)
            {
                return NotFound(new { message = "Deposit not found" });
            }

            return Ok(new
            {
                sessionId = session.Id,
                sessionStatus = session.Status,
                sessionAmountTotal = session.AmountTotal,
                sessionCurrency = session.Currency,
                depositId = deposit.Id,
                depositStatus = deposit.Status,
                depositAmount = deposit.Amount,
                depositCurrency = deposit.Currency,
                deposit.OrderId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Stripe session {SessionId}", sessionId);
            return StatusCode(500, new { message = "Failed to load Stripe session" });
        }
    }

    /// <summary>
    /// Confirm Stripe deposit and credit wallet
    /// </summary>
    [HttpPost("deposit/stripe/confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmStripeDeposit([FromBody] ConfirmStripeDepositRequest request)
    {
        try
        {
            var userId = _currentUser.UserId;
            if (userId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.SessionId))
                return BadRequest(new { message = "sessionId is required" });

            var deposit = await _context.DepositTransactions
                .FirstOrDefaultAsync(d => d.StripeSessionId == request.SessionId && d.UserId == userId.Value);

            if (deposit == null)
                return NotFound(new { message = "Deposit not found for this session" });

            if (deposit.Status == "SUCCESS")
            {
                return Ok(new
                {
                    depositId = deposit.Id,
                    status = deposit.Status,
                    creditedAmount = deposit.Amount
                });
            }

            var session = await RetrieveStripeSessionAsync(request.SessionId);
            if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Stripe session has not been paid yet" });
            }

            var amountUsd = session.AmountTotal.HasValue
                ? (decimal)session.AmountTotal.Value / 100m
                : deposit.Amount;
            var currency = session.Currency?.ToUpperInvariant() ?? deposit.Currency ?? "USD";

            await CreditDepositAsync(deposit, amountUsd, $"Stripe deposit: {amountUsd:N2} {currency}");

            return Ok(new
            {
                depositId = deposit.Id,
                status = deposit.Status,
                creditedAmount = amountUsd
            });
        }
        catch (StripeException stripeEx)
        {
            _logger.LogError(stripeEx, "Stripe error while confirming session {SessionId}", request.SessionId);
            return StatusCode(502, new { message = stripeEx.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming Stripe deposit {SessionId}", request.SessionId);
            return StatusCode(500, new { message = "Failed to confirm Stripe deposit" });
        }
    }

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

            var deposit = await _context.DepositTransactions
                .FirstOrDefaultAsync(d => d.OrderId == response.OrderId);

            if (deposit == null)
            {
                _logger.LogWarning("Deposit transaction not found: OrderId={OrderId}", response.OrderId);
                return Redirect(GetFrontendUrl() + "/deposit?status=failed&message=transaction_not_found");
            }

            if (deposit.Status != "PENDING")
            {
                return Redirect(GetFrontendUrl() + $"/deposit?status={deposit.Status.ToLower()}");
            }

            if (response.VnPayResponseCode == "00")
            {
                try
                {
                    deposit.VnpayTransactionId = response.TransactionId;
                    deposit.VnpayResponseCode = response.VnPayResponseCode;

                    var exchangeRate = 24000m;
                    var usdAmount = deposit.Amount / exchangeRate;

                    await CreditDepositAsync(
                        deposit,
                        usdAmount,
                        $"VNPay deposit: {deposit.Amount:N0} VND");

                    return Redirect(GetFrontendUrl() + $"/deposit?status=success&amount={deposit.Amount:N0}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing VNPay deposit: OrderId={OrderId}, UserId={UserId}", response.OrderId, deposit.UserId);
                    throw;
                }
            }

            deposit.Status = "FAILED";
            deposit.VnpayResponseCode = response.VnPayResponseCode;
            deposit.VnpayMessage = response.VnPayResponseCode;
            await _context.SaveChangesAsync();

            return Redirect(GetFrontendUrl() + "/deposit?status=failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPay callback");
            return Redirect(GetFrontendUrl() + "/deposit?status=error");
        }
    }
    private StripeClient CreateStripeClient(string? overrideKey = null)
    {
        var key = string.IsNullOrWhiteSpace(overrideKey)
            ? _configuration["Stripe:SecretKey"]
            : overrideKey;

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Stripe secret key is not configured");
        }

        return new StripeClient(key);
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

    private async Task<Session> RetrieveStripeSessionAsync(string sessionId)
    {
        StripeException? restrictedError = null;
        Session? session = null;
        var restrictedKey = _configuration["Stripe:RestrictedKey"];

        if (!string.IsNullOrWhiteSpace(restrictedKey))
        {
            var restrictedClient = CreateStripeClient(restrictedKey);
            var restrictedService = new SessionService(restrictedClient);
            try
            {
                session = await restrictedService.GetAsync(sessionId);
            }
            catch (StripeException ex) when (IsPermissionError(ex))
            {
                restrictedError = ex;
            }
        }

        if (session != null)
        {
            return session;
        }

        try
        {
            var defaultClient = CreateStripeClient();
            var defaultService = new SessionService(defaultClient);
            return await defaultService.GetAsync(sessionId);
        }
        catch (StripeException ex)
        {
            if (restrictedError != null)
            {
                _logger.LogWarning(restrictedError, "Restricted key lacked permission for Stripe session {SessionId}; fallback to secret key also failed.", sessionId);
            }
            _logger.LogError(ex, "Failed to retrieve Stripe session {SessionId} with default secret key", sessionId);
            throw;
        }
    }

    private static bool IsPermissionError(StripeException ex)
    {
        if (ex == null) return false;

        return ex.HttpStatusCode == HttpStatusCode.Forbidden ||
               ex.HttpStatusCode == HttpStatusCode.Unauthorized ||
               string.Equals(ex?.StripeError?.Code, "permission_error", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<WalletMovement> CreditDepositAsync(DepositTransaction deposit, decimal usdAmount, string note)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (_context.Entry(deposit).State == EntityState.Detached)
            {
                _context.DepositTransactions.Attach(deposit);
            }

            deposit.Status = "SUCCESS";
            deposit.CompletedAt = DateTime.UtcNow;

            var usdWallet = await GetOrCreateWalletAsync(deposit.UserId, "FIAT", "USD", null);

            var walletMovement = new WalletMovement
            {
                WalletId = usdWallet.Id,
                RefType = $"{deposit.Provider}_DEPOSIT",
                RefId = deposit.Id,
                Amount = usdAmount,
                Note = note,
                CreatedAt = DateTime.UtcNow
            };

            _context.WalletMovements.Add(walletMovement);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Deposit finalized: Provider={Provider}, DepositId={DepositId}, WalletMovementId={WalletMovementId}, AmountUSD={Amount}",
                deposit.Provider, deposit.Id, walletMovement.Id, usdAmount);

            return walletMovement;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task HandleStripeCheckoutCompletedAsync(Session session)
    {
        if (session.Metadata == null || !session.Metadata.TryGetValue("depositId", out var depositIdValue) || !ulong.TryParse(depositIdValue, out var depositId))
        {
            _logger.LogWarning("Stripe session missing deposit metadata: SessionId={SessionId}", session.Id);
            return;
        }

        var deposit = await _context.DepositTransactions
            .FirstOrDefaultAsync(d => d.Id == depositId);

        if (deposit == null)
        {
            _logger.LogWarning("Stripe session referenced unknown deposit: SessionId={SessionId}, DepositId={DepositId}", session.Id, depositId);
            return;
        }

        if (deposit.Status != "PENDING")
        {
            _logger.LogInformation("Stripe session already processed for deposit: DepositId={DepositId}", deposit.Id);
            return;
        }

        deposit.StripeSessionId = session.Id;
        deposit.StripePaymentIntentId = session.PaymentIntentId;
        deposit.PaymentMethod = session.PaymentMethodTypes?.FirstOrDefault() ?? deposit.PaymentMethod;

        var amountUsd = session.AmountTotal.HasValue
            ? (decimal)session.AmountTotal.Value / 100m
            : deposit.Amount;

        var noteCurrency = session.Currency?.ToUpperInvariant() ?? deposit.Currency;
        await CreditDepositAsync(
            deposit,
            amountUsd,
            $"Stripe deposit: {amountUsd:N2} {noteCurrency}");
    }

    private async Task HandleStripePaymentFailedAsync(PaymentIntent paymentIntent)
    {
        DepositTransaction? deposit = null;

        if (!string.IsNullOrWhiteSpace(paymentIntent.Id))
        {
            deposit = await _context.DepositTransactions
                .FirstOrDefaultAsync(d => d.StripePaymentIntentId == paymentIntent.Id);
        }

        if (deposit == null &&
            paymentIntent.Metadata != null &&
            paymentIntent.Metadata.TryGetValue("depositId", out var depositIdValue) &&
            ulong.TryParse(depositIdValue, out var depositId))
        {
            deposit = await _context.DepositTransactions
                .FirstOrDefaultAsync(d => d.Id == depositId);
        }

        if (deposit == null)
        {
            _logger.LogWarning("Stripe payment failure for unknown deposit: PaymentIntent={PaymentIntent}", paymentIntent.Id);
            return;
        }

        if (deposit.Status == "SUCCESS")
        {
            _logger.LogInformation("Stripe failure received for completed deposit, ignoring: DepositId={DepositId}", deposit.Id);
            return;
        }

        deposit.Status = "FAILED";
        deposit.VnpayMessage = paymentIntent.LastPaymentError?.Message ?? "Stripe payment failed";
        deposit.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private string GetFrontendUrl()
    {
        return _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:3000";
    }
}

// DTOs
public record CreateDepositRequest(double Amount);
public record CreateCheckoutDto(int PlanType, string? SuccessUrl = null, string? CancelUrl = null);
public record CreateStripeDepositRequest(decimal Amount, string Currency = "USD");
public record ConfirmStripeDepositRequest(string SessionId);


