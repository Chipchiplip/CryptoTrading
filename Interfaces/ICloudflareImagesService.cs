using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Interfaces
{
    public interface ICloudflareImagesService
    {
        Task<CloudflareDirectUploadResponseDto> RequestDirectUploadUrlAsync(string? fileName = null);
    }
}
