# Hướng dẫn sửa lỗi encoding trong AiTradingChatService.cs

## Các dòng cần sửa thủ công:

### 1. Dòng 208:
**Tìm:**
```csharp
"Äá»ƒ dá»±ng bot cho báº¡n mÃ¬nh cáº§n thÃªm má»™t chÃºt thÃ´ng tin."
```

**Thay bằng:**
```csharp
"Để dựng bot cho bạn mình cần thêm một chút thông tin."
```

### 2. Dòng 652:
**Tìm:**
```csharp
parts.Add($"khung thá»i gian {ctx.TimeHorizon}");
```

**Thay bằng:**
```csharp
parts.Add($"khung thời gian {ctx.TimeHorizon}");
```

### 3. Dòng 693:
**Tìm:**
```csharp
"capital" => "Báº¡n dá»± Ä'á»‹nh dÃ¹ng khoáº£ng bao nhiÃªu vá»'n cho káº¿ hoáº¡ch nÃ y Ä'á»ƒ mÃ¬nh canh tá»· trá»ng chuáº©n hÆ¡n?",
```

**Thay bằng:**
```csharp
"capital" => "Bạn dự định dùng khoảng bao nhiêu vốn cho kế hoạch này để mình canh tỷ trọng chuẩn hơn?",
```

### 4. Dòng 694:
**Tìm:**
```csharp
"risk" => "Báº¡n thiÃªn vá» phong cÃ¡ch máº¡o hiá»ƒm, cÃ¢n báº±ng hay an toÃ n Ä'á»ƒ mÃ¬nh chá»n chiáº¿n lÆ°á»£c phÃ¹ há»£p?",
```

**Thay bằng:**
```csharp
"risk" => "Bạn thiên về phong cách mạo hiểm, cân bằng hay an toàn để mình chọn chiến lược phù hợp?",
```

## Cách sửa:

1. Mở file `Services/Ai/AiTradingChatService.cs` trong editor
2. Tìm từng dòng bị lỗi (dùng Ctrl+F với text bị lỗi)
3. Thay thế bằng text đúng (copy từ trên)
4. Lưu file với encoding UTF-8

## Đã sửa tự động:

✅ USDT → USD (2 chỗ)
✅ BTCUSDT → BTCUSD (2 chỗ)

