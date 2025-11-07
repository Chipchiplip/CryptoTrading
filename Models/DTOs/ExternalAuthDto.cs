using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CryptoTrading.Models.DTOs.ExternalAuth
{
    /// <summary>
    /// DTO để nhận token từ client (idToken cho Google, code cho GitHub)
    /// </summary>
    public class ExternalAuthDto
    {
        [Required]
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO chứa thông tin người dùng chuẩn hóa lấy từ provider
    /// </summary>
    public class ExternalUser
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    // --- DTOs cho Phản hồi API GitHub ---

    /// <summary>
    /// DTO để nhận Access Token từ GitHub (sau khi đổi code)
    /// </summary>
    public class GitHubTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// DTO để nhận thông tin user từ GitHub API (/user)
    /// </summary>
    public class GitHubUser
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("login")] public string Login { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; } // Có thể null
    }

    /// <summary>
    /// DTO để nhận email từ GitHub API (/user/emails)
    /// </summary>
    public class GitHubUserEmail
    {
        [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
        [JsonPropertyName("primary")] public bool Primary { get; set; }
        [JsonPropertyName("verified")] public bool Verified { get; set; }
    }
}