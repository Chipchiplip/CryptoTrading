using Amazon.S3;
using Amazon.S3.Model;
using CryptoTrading.Interfaces;
using CryptoTrading.Models.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;

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

            string uploadUrl;
            try
            {
                uploadUrl = _s3Client.GetPreSignedURL(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate R2 presigned URL for {ObjectKey}", objectKey);
                throw;
            }

            var publicBase = BuildPublicUrlBase();
            var publicUrl = string.IsNullOrWhiteSpace(publicBase)
                ? null
                : $"{publicBase}/{_options.BucketName}/{objectKey}".Replace("//", "/");

            return Task.FromResult(new CloudflareDirectUploadResponseDto
            {
                UploadId = objectKey,
                UploadUrl = uploadUrl,
                PublicUrl = publicUrl,
                PublicUrlBase = string.IsNullOrWhiteSpace(publicBase)
                    ? null
                    : $"{publicBase}/{_options.BucketName}".TrimEnd('/')
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

        private IAmazonS3 CreateClient()
        {
            if (string.IsNullOrWhiteSpace(_options.DeliveryUrl))
            {
                throw new InvalidOperationException("Cloudflare delivery URL is not configured");
            }

            var endpoint = _options.DeliveryUrl;
            if (Uri.TryCreate(_options.DeliveryUrl, UriKind.Absolute, out var uri))
            {
                endpoint = uri.GetLeftPart(UriPartial.Authority);
            }

            var config = new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = "auto",
                UseHttp = false
            };

            return new AmazonS3Client(_options.AccessKeyId, _options.SecretAccessKey, config);
        }
    }
}
