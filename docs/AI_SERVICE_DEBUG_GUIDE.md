# AI Service Debug Guide

## Vấn đề: "Xin lỗi, mình đang gặp sự cố kỹ thuật..."

Khi thấy thông báo này, có nghĩa là call tới AI service (`/api/ai/chat`) bị lỗi và bị catch trong `HandleChatMessageAsync`.

## Các bước kiểm tra

### 1. Kiểm tra log backend

Tìm log error tại thời điểm xảy ra lỗi (ví dụ 01:06):
```
Error calling AI chat service for user {UserId}
```

Log sẽ hiển thị:
- **HTTP 4xx/5xx**: Service trả về lỗi (400 Bad Request, 404 Not Found, 500 Internal Server Error)
- **Timeout**: Request quá 45 giây (timeout được cấu hình trong Program.cs)
- **Payload bị reject**: Lỗi parsing JSON hoặc thiếu field

### 2. Kiểm tra cấu hình client

**File: `Program.cs` (dòng 191-196)**
```csharp
builder.Services.AddHttpClient("AiChatService", client =>
{
    var aiServiceUrl = builder.Configuration["AiService:BaseUrl"] ?? "http://localhost:8000";
    client.BaseAddress = new Uri(aiServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(45);
});
```

**File: `appsettings.Development.json`**
```json
"AiService": {
  "BaseUrl": "http://localhost:8000"
}
```

**Kiểm tra:**
- BaseUrl có đúng không? (mặc định: `http://localhost:8000`)
- Python AI service có đang chạy không?
- Port có đúng không? (8000)

### 3. Kiểm tra Python AI service

Nếu AI service đang chạy Python (FastAPI), kiểm tra:

**a. Service có đang chạy không?**
```bash
# Kiểm tra process
ps aux | grep python
# hoặc
netstat -an | grep 8000
```

**b. Kiểm tra log Python service**
- Xem traceback trong console/log của Python service
- Lỗi thường gặp:
  - **Parsing payload**: Payload format không đúng (thiếu field, sai type)
  - **Model API error**: LLM API (OpenAI/Google) bị lỗi hoặc hết quota
  - **Validation error**: Pydantic model validation fail

**c. Kiểm tra endpoint `/ai/chat`**
```bash
# Test endpoint
curl -X POST http://localhost:8000/ai/chat \
  -H "Content-Type: application/json" \
  -d '{"user_message": "test", "intent": "chat"}'
```

### 4. Kiểm tra payload được gửi

Log đã được cải thiện để hiển thị:
- **Payload preview**: 500 ký tự đầu của payload (trong log Information)
- **Full payload**: Toàn bộ payload khi có lỗi (trong log Error)

**Các field có thể gây lỗi:**
- `market_snapshot`: Có thể null nếu không có symbol
- `market_highlights`: Format không đúng
- `market_down`: Boolean value
- `trading_plan`: Thiếu field bắt buộc

### 5. Các lỗi thường gặp và cách fix

#### Lỗi 404 Not Found
- **Nguyên nhân**: Endpoint `/ai/chat` không tồn tại hoặc route sai
- **Fix**: Kiểm tra route trong Python service (FastAPI)

#### Lỗi 400 Bad Request
- **Nguyên nhân**: Payload format không đúng hoặc thiếu field
- **Fix**: 
  - Kiểm tra Pydantic model trong Python service
  - So sánh payload được gửi với model expected

#### Lỗi 500 Internal Server Error
- **Nguyên nhân**: Lỗi trong Python service (exception không được catch)
- **Fix**: Xem traceback trong log Python service

#### Timeout (TaskCanceledException)
- **Nguyên nhân**: AI service xử lý quá lâu (> 45s) hoặc không phản hồi
- **Fix**: 
  - Kiểm tra Python service có đang xử lý không
  - Tăng timeout nếu cần (không khuyến khích)
  - Kiểm tra LLM API có bị rate limit không

#### Empty/null payload
- **Nguyên nhân**: Python service trả về response nhưng body rỗng
- **Fix**: Kiểm tra Python service có return đúng format không

## Cải thiện đã thực hiện

### 1. Logging chi tiết hơn
- Log payload preview khi gọi API
- Log full payload khi có lỗi
- Log baseAddress, timeout, exception type

### 2. Fallback message cải thiện
- **direct_advice intent**: "Tạm thời mình không thể đưa ra lệnh giao dịch. Vui lòng thử lại sau hoặc kiểm tra kết nối AI service."
- **chat intent**: "Xin lỗi, mình đang gặp sự cố kỹ thuật. Vui lòng thử lại sau."
- Không hỏi follow-up question khi AI service lỗi

## Checklist khi debug

- [ ] Kiểm tra log backend tại thời điểm lỗi
- [ ] Kiểm tra cấu hình `AiService:BaseUrl` trong appsettings
- [ ] Kiểm tra Python service có đang chạy không
- [ ] Kiểm tra log Python service (traceback)
- [ ] Test endpoint `/ai/chat` bằng curl/Postman
- [ ] Kiểm tra payload format có đúng không
- [ ] Kiểm tra LLM API key/quota

## Liên hệ

Nếu vẫn không tìm ra nguyên nhân, cung cấp:
1. Log backend (full exception và stack trace)
2. Log Python service (nếu có)
3. Payload được gửi (từ log)
4. Response từ Python service (nếu có)

