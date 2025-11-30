# Gemini AI Chat Integration

## Tổng quan

Hệ thống đã được tích hợp với Gemini API để:
1. **Chat thông thường**: Gọi trực tiếp Gemini API thay vì Python service
2. **Tạo bot (/taobot)**: Extract thông tin từ conversation history và tạo bot config JSON

## Kiến trúc

### Flow Chat thông thường:
```
User → .NET Backend → Gemini API → Response
```

### Flow /taobot:
```
User (/taobot) → .NET Backend → Gemini API (extract config) → JSON → Create Bot
```

## Cấu hình

### 1. API Key
Đảm bảo `appsettings.json` có:
```json
{
  "LLMApiKeys": {
    "Google": "YOUR_GEMINI_API_KEY"
  }
}
```

### 2. Service Registration
Đã được đăng ký trong `Program.cs`:
```csharp
builder.Services.AddScoped<IGeminiService, GeminiService>();
```

## Cách hoạt động

### Chat thông thường

1. User gửi message qua `/api/ai/chat`
2. `AiTradingChatService` build conversation history từ session
3. Gọi `GeminiService.ChatAsync()` với:
   - User message
   - Conversation history (last 10 messages)
   - Market highlights (nếu có)
4. Gemini trả về reply
5. Response được gửi về user

### Tạo Bot (/taobot)

1. User gửi `/taobot`
2. `AiTradingChatService` lấy toàn bộ conversation history
3. Gọi `GeminiService.ExtractBotConfigAsync()` với:
   - Toàn bộ conversation history
   - Prompt để extract bot config thành JSON
4. Gemini trả về JSON với format:
   ```json
   {
     "name": "Bot name",
     "symbols": ["BTCUSD", "ETHUSD"],
     "strategy_type": "grid",
     "risk_mode": "balanced",
     "max_capital_per_trade": 1000.0,
     "max_daily_exposure": 5000.0,
     "time_horizon": "intraday"
   }
   ```
5. Parse JSON và tạo `AiChatBotSuggestionDto`
6. Lưu vào database và trả về cho user

## Files đã tạo/sửa

### Mới tạo:
- `Services/Ai/GeminiService.cs` - Service để call Gemini API

### Đã sửa:
- `Services/Ai/AiTradingChatService.cs` - Dùng Gemini thay vì Python service
- `Program.cs` - Register GeminiService

## API Models

### ChatMessage
```csharp
public class ChatMessage
{
    public string Role { get; set; } // "user" or "assistant"
    public string Content { get; set; }
}
```

### BotConfigJson
```csharp
public class BotConfigJson
{
    public string? Name { get; set; }
    public List<string> Symbols { get; set; }
    public string StrategyType { get; set; }
    public string RiskMode { get; set; }
    public double MaxCapitalPerTrade { get; set; }
    public double MaxDailyExposure { get; set; }
    public string TimeHorizon { get; set; }
}
```

## Testing

### Test Chat:
```http
POST /api/ai/chat
{
  "userId": 1,
  "message": "Tôi muốn trade BTC với vốn 5000 USD"
}
```

### Test /taobot:
```http
POST /api/ai/chat
{
  "userId": 1,
  "message": "/taobot"
}
```

## Lưu ý

1. **API Key**: Cần có Gemini API key hợp lệ
2. **Rate Limits**: Gemini có rate limits, cần handle errors
3. **Conversation History**: Chỉ lấy last 10 messages để tránh token limit
4. **JSON Parsing**: Gemini có thể trả về JSON không hoàn hảo, cần validate
5. **Fallback**: Nếu Gemini fail, có thể fallback về Python service (chưa implement)

## Cải thiện tương lai

1. Add retry logic cho Gemini API calls
2. Add caching cho conversation history
3. Add fallback mechanism nếu Gemini fail
4. Improve prompt engineering cho bot config extraction
5. Add streaming response support

