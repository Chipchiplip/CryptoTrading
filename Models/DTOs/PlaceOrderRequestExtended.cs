namespace CryptoTrading.Models.DTOs
{
    /// <summary>
    /// Extended order request with idempotency support
    /// </summary>
    public class PlaceOrderRequestExtended : PlaceOrderRequest
    {
        /// <summary>
        /// Client-provided unique identifier for idempotency
        /// If the same clientOrderId is sent multiple times, only one order will be created
        /// </summary>
        public string? ClientOrderId { get; set; }
    }
}

