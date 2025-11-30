# Fix Encoding Issues in AiTradingChatService.cs

## Vấn đề
File `Services/Ai/AiTradingChatService.cs` có 2 chỗ bị lỗi encoding UTF-8:

1. **Dòng 212**: String bị lỗi encoding
2. **Dòng 723**: String bị lỗi encoding

## Cách sửa

### 1. Sửa dòng 212

**Tìm:**
```csharp
"Äá»ƒ dá»±ng bot cho báº¡n mÃ¬nh cáº§n thÃªm má»™t chÃºt thÃ´ng tin."
```

**Thay bằng:**
```csharp
"Để dựng bot cho bạn mình cần thêm một chút thông tin."
```

### 2. Sửa dòng 723

**Tìm:**
```csharp
parts.Add($"khung thá»i gian {ctx.TimeHorizon}");
```

**Thay bằng:**
```csharp
parts.Add($"khung thời gian {ctx.TimeHorizon}");
```

## Cách tìm và sửa trong VS Code

1. Mở file `Services/Ai/AiTradingChatService.cs`
2. Nhấn `Ctrl+F` để mở Find
3. Tìm string bị lỗi (có thể copy từ file này)
4. Thay thế bằng string đúng (UTF-8)
5. Lưu file với encoding UTF-8

## Kiểm tra sau khi sửa

Sau khi sửa, test lại endpoint `/api/ai/chat` với message `/taobot` và kiểm tra response không còn lỗi encoding.

