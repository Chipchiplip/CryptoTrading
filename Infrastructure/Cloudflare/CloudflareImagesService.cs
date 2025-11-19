using Amazon.S3;
using Amazon.S3.Model;
using CryptoTrading.Interfaces;
using CryptoTrading.Models.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text;

namespace CryptoTrading.Infrastructure.Cloudflare
{
    public class CloudflareImagesService : ICloudflareImagesService
    {
        private readonly CloudflareImagesOptions _options;
        private readonly ILogger<CloudflareImagesService> _logger;
        private readonly IAmazonS3 _s3Client;
        private readonly TimeProvider _timeProvider;

        public CloudflareImagesService(
            IOptions<CloudflareImagesOptions> options,
            ILogger<CloudflareImagesService> logger,
            TimeProvider? timeProvider = null)
        {
            _options = options.Value;
            _logger = logger;
            _timeProvider = timeProvider ?? TimeProvider.System;
            _s3Client = CreateClient();
        }

        public Task<CloudflareDirectUploadResponseDto> RequestDirectUploadUrlAsync(string? fileName = null)
        {
            ValidateOptions();

            var objectKey = BuildObjectKey(fileName);
            var expiresAt = _timeProvider.GetUtcNow().AddMinutes(
                _options.UploadUrlExpiryMinutes <= 0 ? 15 : _options.UploadUrlExpiryMinutes);

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey,
                Verb = HttpVerb.PUT,
                Expires = expiresAt.UtcDateTime,
                Protocol = Protocol.HTTPS,
            };

            // ✅ Không set ContentType ở đây - để client tự set khi upload
            // Điều này tránh signature mismatch error

            string uploadUrl;
            try
            {
                uploadUrl = _s3Client.GetPreSignedURL(request);
                _logger.LogInformation("Generated R2 presigned URL for {ObjectKey}: {UploadUrl}", objectKey, uploadUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate R2 presigned URL for {ObjectKey}", objectKey);
                throw;
            }

            // ✅ Build public URL từ DeliveryUrl (pub-xxx.r2.dev)
            var publicUrl = BuildPublicUrl(objectKey);

            return Task.FromResult(new CloudflareDirectUploadResponseDto
            {
                UploadId = objectKey,
                UploadUrl = uploadUrl,
                PublicUrl = publicUrl,
                PublicUrlBase = BuildPublicUrlBase()
            });
        }

        private void ValidateOptions()
        {
            if (string.IsNullOrWhiteSpace(_options.AccessKeyId) || string.IsNullOrWhiteSpace(_options.SecretAccessKey))
            {
                throw new InvalidOperationException("Cloudflare R2 credentials are missing");
            }

            if (string.IsNullOrWhiteSpace(_options.BucketName))
            {
                throw new InvalidOperationException("Cloudflare R2 bucket name is not configured");
            }

            if (string.IsNullOrWhiteSpace(_options.AccountId))
            {
                throw new InvalidOperationException("Cloudflare Account ID is not configured");
            }

            if (string.IsNullOrWhiteSpace(_options.DeliveryUrl))
            {
                throw new InvalidOperationException("Cloudflare delivery URL is not configured");
            }
        }

        private string BuildObjectKey(string? fileName)
        {
            var safeName = string.IsNullOrWhiteSpace(fileName)
                ? "avatar"
                : Path.GetFileName(fileName);

            var extension = Path.GetExtension(safeName);
            var prefix = _options.UploadPrefix?.Trim('/') ?? string.Empty;
            var key = $"{Guid.NewGuid():N}{extension}";

            return string.IsNullOrEmpty(prefix)
                ? key
                : $"{prefix}/{key}".Replace("//", "/");
        }

        private string? BuildPublicUrlBase()
        {
            if (!Uri.TryCreate(_options.DeliveryUrl, UriKind.Absolute, out var uri))
            {
                return null;
            }

            return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        }

        private string? BuildPublicUrl(string objectKey)
        {
            var baseUrl = BuildPublicUrlBase();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return null;
            }

            // ✅ Public URL format: https://pub-xxx.r2.dev/<objectKey>
            // KHÔNG bao gồm bucket name vì R2 public URL đã map sẵn
            return CombineUrl(baseUrl, objectKey);
        }

        private static string CombineUrl(string? baseUrl, params string?[] segments)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(baseUrl.TrimEnd('/'));

            foreach (var segment in segments)
            {
                if (string.IsNullOrWhiteSpace(segment))
                {
                    continue;
                }

                builder.Append('/');
                builder.Append(segment.Trim('/'));
            }

            return builder.ToString();
        }

        private IAmazonS3 CreateClient()
        {
            if (string.IsNullOrWhiteSpace(_options.AccountId))
            {
                throw new InvalidOperationException("Cloudflare Account ID is not configured");
            }

            // ✅ S3 API endpoint cho Cloudflare R2
            // Format: https://<accountid>.r2.cloudflarestorage.com
            var s3Endpoint = $"https://{_options.AccountId}.r2.cloudflarestorage.com";

            var config = new AmazonS3Config
            {
                ServiceURL = s3Endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = "auto",
                UseHttp = false
            };

            _logger.LogInformation("Creating R2 S3 client with endpoint: {Endpoint}", s3Endpoint);

            return new AmazonS3Client(_options.AccessKeyId, _options.SecretAccessKey, config);
        }
    }
}
