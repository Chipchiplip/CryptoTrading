using CryptoTrading.Models.Payment;

namespace CryptoTrading.Services.Payment
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(PaymentInformationModel model, HttpContext context, string orderId);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);
    }
}

