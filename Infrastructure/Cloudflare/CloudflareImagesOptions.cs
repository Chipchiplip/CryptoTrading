namespace CryptoTrading.Infrastructure.Cloudflare
{
    public class CloudflareImagesOptions
    {
        public string AccountId { get; set; } = string.Empty;
        public string ApiToken { get; set; } = string.Empty;
        public string DeliveryUrl { get; set; } = string.Empty;
        public string[]? AllowedAvatarDomains { get; set; }
        public string BucketName { get; set; } = string.Empty;
        public string UploadPrefix { get; set; } = string.Empty;
        public string AccessKeyId { get; set; } = string.Empty;
        public string SecretAccessKey { get; set; } = string.Empty;
        public int UploadUrlExpiryMinutes { get; set; } = 15;
    }
}
