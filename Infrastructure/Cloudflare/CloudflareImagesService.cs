using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;
using CryptoTrading.Interfaces;
using CryptoTrading.Models.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CryptoTrading.Infrastructure.Cloudflare
{
    public class CloudflareImagesService : ICloudflareImagesService
    {
        private readonly HttpClient _httpClient;
        private readonly CloudflareImagesOptions _options;
        private readonly ILogger<CloudflareImagesService> _logger;

        public CloudflareImagesService(
            HttpClient httpClient,
            IOptions<CloudflareImagesOptions> options,
            ILogger<CloudflareImagesService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;

            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("https://api.cloudflare.com/");
            }
        }

        public async Task<CloudflareDirectUploadResponseDto> RequestDirectUploadUrlAsync(string? fileName = null)
        {
            EnsureOptions();

            var requestUri = $"client/v4/accounts/{_options.AccountId}/images/v2/direct_upload";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);

            var payload = new Dictionary<string, object?>();
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                payload["metadata"] = new { fileName };
            }

            request.Content = JsonContent.Create(payload, options: new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadFromJsonAsync<CloudflareDirectUploadApiResponse>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (!response.IsSuccessStatusCode || json == null || !json.Success || json.Result == null)
            {
                var message = json?.Errors?.FirstOrDefault()?.Message ?? response.ReasonPhrase ?? "Unable to request upload URL";
                _logger.LogError("Failed to get Cloudflare direct upload URL: {Message}", message);
                throw new InvalidOperationException(message);
            }

            var uploadId = json.Result.Id ?? string.Empty;
            var uploadUrl = json.Result.UploadURL ?? string.Empty;

            if (string.IsNullOrWhiteSpace(uploadId) || string.IsNullOrWhiteSpace(uploadUrl))
            {
                throw new InvalidOperationException("Cloudflare response missing upload data");
            }

            return new CloudflareDirectUploadResponseDto
            {
                UploadId = uploadId,
                UploadUrl = uploadUrl,
                PublicUrl = BuildPublicUrl(uploadId),
                PublicUrlBase = BuildPublicUrlBase()
            };
        }

        private void EnsureOptions()
        {
            if (string.IsNullOrWhiteSpace(_options.AccountId) || string.IsNullOrWhiteSpace(_options.ApiToken))
            {
                throw new InvalidOperationException("Cloudflare account configuration is missing");
            }
        }

        private string? BuildPublicUrl(string uploadId)
        {
            var baseUrl = BuildPublicUrlBase();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return null;
            }

            return $"{baseUrl}/{uploadId}/public";
        }

        private string? BuildPublicUrlBase()
        {
            return string.IsNullOrWhiteSpace(_options.DeliveryUrl)
                ? null
                : _options.DeliveryUrl.TrimEnd('/');
        }

        private sealed class CloudflareDirectUploadApiResponse
        {
            public bool Success { get; set; }
            public CloudflareDirectUploadResult? Result { get; set; }
            public List<CloudflareError>? Errors { get; set; }
        }

        private sealed class CloudflareDirectUploadResult
        {
            public string? Id { get; set; }
            public string? UploadURL { get; set; }
        }

        private sealed class CloudflareError
        {
            public string? Message { get; set; }
        }
    }
}
