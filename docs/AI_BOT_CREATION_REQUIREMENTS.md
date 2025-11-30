# AI Bot Creation Requirements

## Tổng quan
AI agent cần 4 thông tin chính để tạo bot tự động:
1. **Vốn (Capital)** - Số tiền muốn đầu tư
2. **Risk Profile** - Mức độ rủi ro
3. **Symbols** - Cặp coin muốn trade
4. **Time Horizon** - Khung thời gian trade

## Chi tiết các thông tin

### 1. Vốn (Capital)
- **Field**: `TotalEquity` hoặc `capital`
- **Ví dụ cách nói**:
  - "tôi có 100k USD"
  - "vốn 50000"
  - "100000 USD"
  - "tôi muốn đầu tư 200k"
- **Parse logic**: Tìm số tiền trong message (USD)
- **Default**: Nếu không có, AI sẽ hỏi lại

### 2. Risk Profile (Mức độ rủi ro)
- **Field**: `RiskMode`
- **Các giá trị**:
  - `AGGRESSIVE` / `mạo hiểm` - Rủi ro cao, lợi nhuận cao
  - `BALANCED` / `cân bằng` - Rủi ro vừa phải
  - `SAFE` / `an toàn` - Rủi ro thấp, bảo toàn vốn
- **Ví dụ cách nói**:
  - "mạo hiểm"
  - "aggressive"
  - "cân bằng"
  - "an toàn"
  - "safe"
- **Default**: `BALANCED` nếu không chỉ định

### 3. Symbols (Cặp coin)
- **Field**: `PreferredSymbols`
- **Format**: `BTCUSD`, `ETHUSD`, `BTC/USD`, `ETH/USD`
- **Ví dụ cách nói**:
  - "BTCUSD"
  - "ETH"
  - "Bitcoin"
  - "Ethereum"
  - "BTC và ETH"
- **Parse logic**: Tìm ticker symbols trong message
- **Default**: `["BTCUSD", "ETHUSD"]` nếu không chỉ định

### 4. Time Horizon (Khung thời gian)
- **Field**: `TimeHorizon`
- **Các giá trị**:
  - `scalping` - Trade nhanh, giữ vài phút/giờ
  - `intraday` - Trade trong ngày
  - `swing` - Giữ vài ngày
- **Ví dụ cách nói**:
  - "scalping"
  - "trade nhanh"
  - "intraday"
  - "trong ngày"
  - "swing"
  - "giữ vài ngày"
- **Default**: `intraday` nếu không chỉ định

## Quy trình tạo bot

### Bước 1: User chat với AI
```
User: "tôi có 100k USD muốn đặt lệnh"
AI: "Bạn dự định dùng khoảng bao nhiêu vốn cho kế hoạch này để mình canh tỷ trọng chuẩn hơn?"
```

### Bước 2: AI collect thông tin
AI sẽ hỏi từng field còn thiếu:
- Nếu thiếu capital → hỏi về vốn
- Nếu thiếu risk → hỏi về risk profile
- Nếu thiếu symbols → hỏi về cặp coin
- Nếu thiếu horizon → hỏi về time horizon

### Bước 3: User cung cấp đủ thông tin
```
User: "mạo hiểm"
AI: "Bạn muốn tập trung vào cặp nào? Ví dụ BTCUSD hay ETHUSD cũng được."
```

### Bước 4: User gõ `/taobot`
Khi đã có đủ 4 thông tin, user gõ `/taobot` để AI tạo bot.

### Bước 5: AI tạo bot
AI sẽ:
1. Tạo bot proposals dựa trên thông tin đã collect
2. Set parameters:
   - `orderType: "MARKET"` - Bot dùng market orders
   - `maxBuyOrders: 3` - Tối đa 3 BUY orders
   - `maxSellOrders: 3` - Tối đa 3 SELL orders
   - `orderSize: 0.1` - Mỗi order 0.1 coin
   - `capitalAllocation` - Dựa trên vốn user cung cấp
3. Trả về bot proposals cho user chọn

## Ví dụ đầy đủ

### Conversation flow:
```
User: "xin chào tôi muốn đặt lệnh"
AI: "Bạn dự định dùng khoảng bao nhiêu vốn cho kế hoạch này để mình canh tỷ trọng chuẩn hơn?"

User: "tôi có 100k USD"
AI: "Bạn thiên về phong cách mạo hiểm, cân bằng hay an toàn để mình chọn chiến lược phù hợp?"

User: "mạo hiểm"
AI: "Bạn muốn tập trung vào cặp nào? Ví dụ BTCUSD hay ETHUSD cũng được."

User: "BTCUSD"
AI: "Bạn đang trade nhanh kiểu scalping, intraday hay giữ swing vài ngày?"

User: "intraday"
AI: "Nếu muốn mình dựng bot tự động, cứ gõ /taobot bất kỳ lúc nào."

User: "/taobot"
AI: "Cấu hình mạo hiểm với hạn mức ~30000 USD/lệnh, mình đề xuất các bot sau:
- Bot 1: Aggressive BTCUSD Grid Trading (BTCUSD) • grid • 30000 USD/lệnh, tối đa ngày ~150000.
  Ghi chú: Rủi ro cao, có thể chịu drawdown lớn. Bot sẽ dùng MARKET orders để trade trực tiếp với market ảo, orders được execute ngay lập tức."
```

## Bot Parameters được tạo tự động

Khi AI tạo bot, các parameters sau được set:

```json
{
  "orderType": "MARKET",
  "maxBuyOrders": 3,
  "maxSellOrders": 3,
  "orderSize": 0.1,
  "capitalAllocation": 100000,
  "gridLevels": 20,
  "lowerBound": 70000,  // 70% currentPrice
  "upperBound": 150000, // 150% currentPrice
  "refreshIntervalSeconds": 60
}
```

## Lưu ý

1. **Bot dùng MARKET orders**: Bot sẽ trade trực tiếp với market ảo, không match với limit orders của users
2. **Orders execute ngay**: Market orders được execute ngay lập tức với market price
3. **Grid Trading Strategy**: Tất cả bots đều dùng Grid Trading strategy với market orders
4. **Risk scaling**: 
   - Aggressive: scale = 1.0 (dùng 100% vốn)
   - Balanced: scale = 0.7 (dùng 70% vốn)
   - Safe: scale = 0.4 (dùng 40% vốn)

## Testing

Để test AI bot creation:
1. Chat với AI và cung cấp đủ 4 thông tin
2. Gõ `/taobot`
3. Kiểm tra bot proposals được tạo
4. Apply suggestion để tạo bot thật

