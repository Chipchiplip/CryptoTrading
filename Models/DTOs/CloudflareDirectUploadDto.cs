namespace CryptoTrading.Models.DTOs
{
    public class CloudflareDirectUploadRequestDto
    {
        public string? FileName { get; set; }
    }

    public class CloudflareDirectUploadResponseDto
    {
        public required string UploadId { get; set; }
        public required string UploadUrl { get; set; }
        public string? PublicUrl { get; set; }
        public string? PublicUrlBase { get; set; }
    }
}
