namespace CryptoTrading.Core.Models.DTOs
{
    public class TwoFactorSetupDto
    {
        public string Secret { get; set; } = string.Empty;
        public string QrCodeUri { get; set; } = string.Empty;
        public string ManualEntryKey { get; set; } = string.Empty;
    }

    public class EnableTwoFactorDto
    {
        public string Code { get; set; } = string.Empty;
    }

    public class VerifyTwoFactorDto
    {
        public string Code { get; set; } = string.Empty;
    }

    public class DisableTwoFactorDto
    {
        public string Password { get; set; } = string.Empty;
    }
}