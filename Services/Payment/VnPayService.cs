using CryptoTrading.Models.Payment;

namespace CryptoTrading.Services.Payment
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreatePaymentUrl(PaymentInformationModel model, HttpContext context, string orderId, string? returnUrl = null)
        {
            try
            {
                var timeZone = ResolveTimeZone();
                var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                var pay = new VnPayLibrary();
                // Sử dụng returnUrl được truyền vào, nếu không có thì dùng default
                var urlCallBack = returnUrl ?? _configuration["PaymentCallBack:ReturnUrl"] ?? _configuration["Vnpay:PaymentBackReturnUrl"];

                var baseUrl = _configuration["Vnpay:BaseUrl"];
                var hashSecret = _configuration["Vnpay:HashSecret"];
                var tmnCode = _configuration["Vnpay:TmnCode"];

                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    throw new InvalidOperationException("Vnpay:BaseUrl is not configured");
                }

                if (string.IsNullOrWhiteSpace(hashSecret))
                {
                    throw new InvalidOperationException("Vnpay:HashSecret is not configured");
                }

                if (string.IsNullOrWhiteSpace(tmnCode))
                {
                    throw new InvalidOperationException("Vnpay:TmnCode is not configured");
                }

                pay.AddRequestData("vnp_Version", _configuration["Vnpay:Version"] ?? "2.1.0");
                pay.AddRequestData("vnp_Command", _configuration["Vnpay:Command"] ?? "pay");
                pay.AddRequestData("vnp_TmnCode", tmnCode);
                pay.AddRequestData("vnp_Amount", ((int)model.Amount * 100).ToString());
                pay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
                pay.AddRequestData("vnp_CurrCode", _configuration["Vnpay:CurrCode"] ?? "VND");
                pay.AddRequestData("vnp_IpAddr", pay.GetIpAddress(context));
                pay.AddRequestData("vnp_Locale", _configuration["Vnpay:Locale"] ?? "vn");
                pay.AddRequestData("vnp_OrderInfo", $"{model.Name} {model.OrderDescription} {model.Amount}");
                pay.AddRequestData("vnp_OrderType", model.OrderType);
                pay.AddRequestData("vnp_ReturnUrl", urlCallBack ?? "");
                pay.AddRequestData("vnp_TxnRef", orderId); // Dùng orderId được truyền vào thay vì tạo mới

                var paymentUrl = pay.CreateRequestUrl(baseUrl, hashSecret);

                return paymentUrl;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create VNPay payment URL: {ex.Message}", ex);
            }
        }

        public PaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            var pay = new VnPayLibrary();
            var response = pay.GetFullResponseData(collections, _configuration["Vnpay:HashSecret"] ?? "");

            return response;
        }
        private TimeZoneInfo ResolveTimeZone()
        {
            var configuredId = _configuration["TimeZoneId"];
            var candidateIds = new[]
            {
                configuredId,
                "SE Asia Standard Time",
                "Asia/Ho_Chi_Minh"
            };

            foreach (var id in candidateIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                    continue;
                }
                catch (InvalidTimeZoneException)
                {
                    continue;
                }
            }

            return TimeZoneInfo.Utc;
        }
    }
}

