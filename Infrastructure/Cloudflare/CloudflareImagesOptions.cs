namespace CryptoTrading.Infrastructure.Cloudflare
{
    public class CloudflareImagesOptions
    {
        public string AccountId { get; set; } = string.Empty;
        public string ApiToken { get; set; } = string.Empty;
        public string DeliveryUrl { get; set; } = string.Empty;
        public string[]? AllowedAvatarDomains { get; set; }
    }
}
