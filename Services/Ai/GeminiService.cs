using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services.Ai;

public interface IGeminiService
{
    Task<string> ChatAsync(string userMessage, List<ChatMessage>? conversationHistory = null, CancellationToken ct = default);
    Task<BotConfigJson> ExtractBotConfigAsync(List<ChatMessage> conversationHistory, CancellationToken ct = default);
    Task<List<string>> ListAvailableModelsAsync(CancellationToken ct = default);
}

public class GeminiService : IGeminiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiService> _logger;
    private readonly string _apiKey;
    // Use gemini-2.5-flash as shown in the frontend code
    private const string GeminiBaseUrl = "https://generativelanguage.googleapis.com/v1beta";
    private string _modelName = "gemini-2.5-flash"; // Model name from frontend
    private string? _cachedModelName = null; // Cache the working model name

    public GeminiService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _apiKey = _configuration["LLMApiKeys:Google"] ?? throw new InvalidOperationException("Gemini API key not configured");
    }

    private string GetApiUrl()
    {
        var modelName = _cachedModelName ?? _modelName;
        return $"{GeminiBaseUrl}/models/{modelName}:generateContent";
    }

    public async Task<List<string>> ListAvailableModelsAsync(CancellationToken ct = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var url = $"{GeminiBaseUrl}/models?key={_apiKey}";
            var response = await httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            var json = JsonSerializer.Deserialize<JsonElement>(content);
            
            var models = new List<string>();
            if (json.TryGetProperty("models", out var modelsArray))
            {
                foreach (var model in modelsArray.EnumerateArray())
                {
                    if (model.TryGetProperty("name", out var name))
                    {
                        var modelName = name.GetString();
                        if (!string.IsNullOrEmpty(modelName))
                        {
                            // Extract just the model name part (e.g., "models/gemini-pro" -> "gemini-pro")
                            var parts = modelName.Split('/');
                            if (parts.Length > 1)
                            {
                                models.Add(parts[parts.Length - 1]);
                            }
                        }
                    }
                }
            }

            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing Gemini models");
            return new List<string>();
        }
    }

    private async Task TryFindWorkingModelAsync(CancellationToken ct)
    {
        // Try common model names (gemini-2.5-flash should work)
        var modelNamesToTry = new[]
        {
            "gemini-2.5-flash",
            "gemini-1.5-flash-latest",
            "gemini-1.5-pro-latest",
            "gemini-pro",
            "gemini-1.5-flash",
            "gemini-1.5-pro"
        };

        foreach (var modelName in modelNamesToTry)
        {
            try
            {
                var testUrl = $"{GeminiBaseUrl}/models/{modelName}:generateContent?key={_apiKey}";
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);

                // Simple test request
                var testRequest = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = "test" }
                            }
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(testRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var request = new HttpRequestMessage(HttpMethod.Post, testUrl)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                var response = await httpClient.SendAsync(request, ct);
                if (response.IsSuccessStatusCode)
                {
                    _cachedModelName = modelName;
                    _logger.LogInformation("Found working Gemini model: {ModelName}", modelName);
                    return;
                }
            }
            catch
            {
                // Try next model
                continue;
            }
        }

        // If no model works, use default
        _cachedModelName = modelNamesToTry[0];
        _logger.LogWarning("Could not find working model, using default: {ModelName}", _cachedModelName);
    }

    public async Task<string> ChatAsync(string userMessage, List<ChatMessage>? conversationHistory = null, CancellationToken ct = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var requestBody = BuildChatRequest(userMessage, conversationHistory);
            var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            // Try to find a working model if we don't have one cached
            if (_cachedModelName == null)
            {
                await TryFindWorkingModelAsync(ct);
            }

            var apiUrl = GetApiUrl();
            var url = $"{apiUrl}?key={_apiKey}";
            _logger.LogInformation("Calling Gemini API: {Url} (API key present: {HasKey})", 
                apiUrl, !string.IsNullOrEmpty(_apiKey));
            _logger.LogDebug("Request body length: {Length}", jsonContent.Length);
            _logger.LogDebug("Request body length: {Length}", jsonContent.Length);
            
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            var response = await httpClient.SendAsync(request, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Gemini API returned error: {StatusCode}, {ErrorContent}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Gemini API returned {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogDebug("Gemini API response received, length: {Length}", responseContent.Length);
            
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0)
            {
                _logger.LogWarning("Gemini API returned empty candidates. Full response: {Response}", responseContent);
                return "Xin lỗi, mình không thể xử lý câu hỏi này lúc này. Vui lòng thử lại sau.";
            }

            var reply = geminiResponse.Candidates[0].Content?.Parts?[0]?.Text ?? "Xin lỗi, mình không thể xử lý câu hỏi này.";
            _logger.LogInformation("Gemini API reply extracted, length: {Length}", reply.Length);
            return reply.Trim();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Gemini API: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Gemini API: {ExceptionType}, {Message}", ex.GetType().Name, ex.Message);
            throw;
        }
    }

    public async Task<BotConfigJson> ExtractBotConfigAsync(List<ChatMessage> conversationHistory, CancellationToken ct = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var prompt = BuildBotConfigExtractionPrompt(conversationHistory);
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    topK = 40,
                    topP = 0.95,
                    maxOutputTokens = 1024,
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var apiUrl = GetApiUrl();
            var url = $"{apiUrl}?key={_apiKey}";
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            var response = await httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0)
            {
                throw new InvalidOperationException("Gemini API returned empty response");
            }

            var jsonText = geminiResponse.Candidates[0].Content?.Parts?[0]?.Text ?? "{}";
            
            // Parse JSON response
            var botConfig = JsonSerializer.Deserialize<BotConfigJson>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (botConfig == null)
            {
                throw new InvalidOperationException("Failed to parse bot config from Gemini response");
            }

            return botConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting bot config from Gemini API");
            throw;
        }
    }

    private object BuildChatRequest(string userMessage, List<ChatMessage>? conversationHistory)
    {
        var contents = new List<object>();

        // Add conversation history (alternating user/model)
        if (conversationHistory != null && conversationHistory.Count > 0)
        {
            foreach (var msg in conversationHistory.TakeLast(10)) // Limit to last 10 messages
            {
                contents.Add(new
                {
                    role = msg.Role == "user" ? "user" : "model",
                    parts = new[]
                    {
                        new { text = msg.Content }
                    }
                });
            }
        }

        // Add current user message
        contents.Add(new
        {
            role = "user",
            parts = new[]
            {
                new { text = userMessage }
            }
        });

        // System instruction (like in frontend)
        var systemInstruction = new
        {
            parts = new[]
            {
                new
                {
                    text = @"Bạn là một AI trading assistant thân thiện, chuyên nghiệp. Nhiệm vụ của bạn:
1. Trả lời các câu hỏi về trading, thị trường crypto
2. Đưa ra lời khuyên trading dựa trên thông tin người dùng cung cấp
3. Hỏi thêm thông tin nếu cần (vốn, risk mode, symbols, time horizon)
4. Gợi ý các chiến lược phù hợp
5. Luôn trả lời bằng tiếng Việt, thân thiện và dễ hiểu

QUAN TRỌNG - PHẢI TUÂN THỦ:
- Trả lời ngắn gọn, súc tích (tối đa 3-4 câu)
- TUYỆT ĐỐI KHÔNG dùng markdown formatting (KHÔNG dùng **, *, #, -, số thứ tự với dấu chấm)
- Trả lời bằng văn bản thuần, tự nhiên như đang chat với bạn
- Nếu cần liệt kê, dùng dấu phẩy hoặc xuống dòng, KHÔNG dùng số thứ tự
- Nếu user hỏi về coin cụ thể, đưa ra lời khuyên rõ ràng và ngắn gọn
- Giữ tone thân thiện, không quá formal"
                }
            }
        };

        return new
        {
            contents = contents.ToArray(),
            systemInstruction = systemInstruction,
            generationConfig = new
            {
                temperature = 0.7,
                topK = 32,
                topP = 1.0,
                maxOutputTokens = 1024
            }
        };
    }

    private string BuildBotConfigExtractionPrompt(List<ChatMessage> conversationHistory)
    {
        var conversationText = string.Join("\n", conversationHistory.Select((msg, idx) => 
            $"{idx + 1}. {msg.Role}: {msg.Content}"));

        return $@"Bạn là một AI assistant chuyên extract thông tin bot trading từ cuộc trò chuyện.

Dựa vào đoạn hội thoại sau, hãy extract thông tin và trả về JSON với format sau:

{{
  ""name"": ""Tên bot (nếu không có thì để null)"",
  ""symbols"": [""BTCUSD"", ""ETHUSD""], // Danh sách symbols user muốn trade
  ""strategy_type"": ""grid"", // grid, trend_following, dca, breakout, momentum_scalping, aggressive_forex
  ""risk_mode"": ""balanced"", // aggressive, balanced, safe (PHẢI extract đúng từ conversation)
  ""max_capital_per_trade"": 1000.0, // Vốn tối đa mỗi lệnh (USD) - PHẢI tính dựa trên vốn user cung cấp
  ""max_daily_exposure"": 5000.0, // Vốn tối đa mỗi ngày (USD) - PHẢI tính dựa trên vốn user cung cấp
  ""time_horizon"": ""intraday"" // scalping, intraday, swing
}}

QUAN TRỌNG - PHẢI TUÂN THỦ:
1. risk_mode: PHẢI extract đúng từ conversation:
   - Nếu user nói ""aggressive"", ""mạo hiểm"", ""risk cao"" -> dùng ""aggressive""
   - Nếu user nói ""balanced"", ""cân bằng"", ""vừa phải"" -> dùng ""balanced""
   - Nếu user nói ""safe"", ""an toàn"", ""thận trọng"" -> dùng ""safe""
   - KHÔNG được dùng mặc định ""balanced"" nếu user đã nói rõ risk mode

2. max_capital_per_trade và max_daily_exposure: PHẢI tính dựa trên vốn user cung cấp:
   - Tìm số vốn user đề cập (ví dụ: 10000, 15000, 20000 USD)
   - max_capital_per_trade = 10-20% vốn (aggressive: 15-20%, balanced: 10-15%, safe: 5-10%)
   - max_daily_exposure = 50-100% vốn (aggressive: 80-100%, balanced: 60-80%, safe: 40-60%)
   - KHÔNG được để 0 hoặc giá trị quá nhỏ

3. strategy_type: Chọn phù hợp với risk_mode và time_horizon:
   - aggressive + intraday/scalping -> ""momentum_scalping"" hoặc ""aggressive_forex""
   - balanced + intraday -> ""grid"" hoặc ""trend_following""
   - safe + swing -> ""grid"" hoặc ""dca""

4. symbols: Extract đúng từ conversation (BTC, ETH, SOL -> BTCUSD, ETHUSD, SOLUSD)

5. time_horizon: Extract đúng từ conversation (scalping, intraday, swing)

Đoạn hội thoại:
{conversationText}

Hãy trả về JSON hợp lệ với các giá trị đã được extract chính xác:";
    }
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
}

public class BotConfigJson
{
    public string? Name { get; set; }
    public List<string> Symbols { get; set; } = new();
    public string StrategyType { get; set; } = "grid";
    public string RiskMode { get; set; } = "balanced";
    public double MaxCapitalPerTrade { get; set; }
    public double MaxDailyExposure { get; set; }
    public string TimeHorizon { get; set; } = "intraday";
}

// Gemini API Response Models
public class GeminiResponse
{
    public List<GeminiCandidate>? Candidates { get; set; }
}

public class GeminiCandidate
{
    public GeminiContent? Content { get; set; }
}

public class GeminiContent
{
    public List<GeminiPart>? Parts { get; set; }
}

public class GeminiPart
{
    public string? Text { get; set; }
}

