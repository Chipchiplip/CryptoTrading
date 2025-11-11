# 🔍 Portfolio Calculation Verification

## 📊 Dữ Liệu Từ UI

Từ screenshot Portfolio page:
- **Amount**: 2.050000 SOL
- **Avg Price**: $170.41
- **Current Price**: $170.04
- **Value**: $348.58
- **Cost**: $349.34 (từ Total Cost card)
- **PnL**: -$0.76
- **PnL %**: -0.22%

---

## ✅ Verification - Các Tính Toán Hiện Tại

### 1. Value Calculation
```
Value = Amount × Current Price
Value = 2.05 × $170.04
Value = $348.582
Value ≈ $348.58 ✅ CORRECT
```

### 2. Cost Calculation
```
Cost = Amount × Avg Price
Cost = 2.05 × $170.41
Cost = $349.3405
Cost ≈ $349.34 ✅ CORRECT
```

### 3. PnL Calculation
```
PnL = Value - Cost
PnL = $348.58 - $349.34
PnL = -$0.76 ✅ CORRECT
```

### 4. PnL % Calculation
```
PnL % = (PnL / Cost) × 100
PnL % = (-$0.76 / $349.34) × 100
PnL % = -0.2176%
PnL % ≈ -0.22% ✅ CORRECT
```

### 5. Allocation Calculation
```
Allocation = (Value / Total Value) × 100
Allocation = ($348.58 / $348.58) × 100
Allocation = 100.0% ✅ CORRECT (chỉ có 1 holding)
```

---

## ❓ Vấn Đề: User Nói "Mua 2 SOL = $348.58"

### Phân Tích:

**Nếu user mua đúng 2 SOL với tổng $348.58:**
- Avg Price = $348.58 / 2 = **$174.29**
- Nhưng UI hiển thị: Avg Price = **$170.41** ❌

**Có 3 khả năng:**

#### Khả Năng 1: User Mua 2 SOL + Fee
```
Giả sử mua 2 SOL @ $170.41 mỗi SOL:
- Trade value: 2 × $170.41 = $340.82
- Fee (0.1%): $340.82 × 0.001 = $0.34
- Total cost: $340.82 + $0.34 = $341.16

Nhưng UI hiển thị Cost = $349.34, không phải $341.16
```

#### Khả Năng 2: User Có Thêm 0.05 SOL Từ Nguồn Khác
```
Có thể:
- User mua 2 SOL với giá $170.41 mỗi SOL
- Sau đó nhận thêm 0.05 SOL từ deposit hoặc trade khác
- Hoặc có multiple trades

Tổng: 2.05 SOL với avg price = $170.41
Cost = 2.05 × $170.41 = $349.34 ✅
```

#### Khả Năng 3: User Nhớ Nhầm Số Tiền
```
User có thể nhớ:
- Số tiền đã chi: $348.58 (nhưng thực tế là $349.34)
- Hoặc nhớ giá trị hiện tại: $348.58 (Value, không phải Cost)
```

---

## 🔍 Cách Kiểm Tra

### 1. Kiểm Tra Trade History
Cần xem:
- User có bao nhiêu BUY trades cho SOL?
- Mỗi trade có quantity và price bao nhiêu?
- Có fee bao nhiêu?

**Query để check:**
```sql
SELECT 
    t.Id,
    t.QuantityCoin,
    t.PriceUsd,
    t.FeeUsd,
    t.CreatedAt,
    o.Side
FROM Trades t
JOIN Orders o ON t.OrderId = o.Id
WHERE o.UserId = [USER_ID]
  AND t.CryptocurrencyId = [SOL_CRYPTO_ID]
ORDER BY t.CreatedAt;
```

### 2. Kiểm Tra Cost Basis Calculation

**Theo code hiện tại:**
```csharp
// Cost basis bao gồm cả fee
var costPerUnit = (buyTrade.PriceUsd * buyTrade.QuantityCoin + buyTrade.FeeUsd) / buyTrade.QuantityCoin;
```

**Ví dụ:**
- BUY 2 SOL @ $170.41, Fee = $0.34
- Cost per unit = ($170.41 × 2 + $0.34) / 2 = $170.58
- Nhưng UI hiển thị: $170.41 ❌

**Có thể có vấn đề:** Code tính cost basis bao gồm fee, nhưng UI hiển thị không đúng?

---

## 🛠️ Cần Kiểm Tra Code

### 1. GetAverageCostBasisAsync Method

**Code hiện tại (line 263):**
```csharp
var costPerUnit = (buyTrade.PriceUsd * buyTrade.QuantityCoin + buyTrade.FeeUsd) / buyTrade.QuantityCoin;
```

**Vấn đề:** Cost per unit đã bao gồm fee, nhưng có thể:
- Fee được tính 2 lần?
- Hoặc fee không được tính vào cost basis đúng cách?

### 2. Portfolio Service - Avg Price Display

**Code hiện tại (line 80-81):**
```csharp
var avgPrice = await GetAverageCostBasisAsync(userId, symbol, DateTime.UtcNow);
var cost = balance * avgPrice;
```

**Nếu avgPrice đã bao gồm fee:**
- Cost = balance × (price + fee/quantity)
- Điều này đúng ✅

**Nhưng nếu user mua 2 SOL:**
- Giả sử: 2 SOL @ $170.41, Fee = $0.34
- Cost per unit = ($170.41 × 2 + $0.34) / 2 = $170.58
- Nhưng UI hiển thị: $170.41

**Có thể:** UI đang hiển thị PriceUsd thay vì cost per unit?

---

## 📝 Kết Luận

### Tính Toán Hiện Tại: ✅ ĐÚNG
- Value, Cost, PnL, PnL % đều tính đúng
- Logic FIFO hoạt động đúng

### Vấn Đề: ❓ Cần Clarify
1. **User nói "mua 2 SOL = $348.58"** nhưng:
   - UI hiển thị Amount = 2.05 SOL (không phải 2 SOL)
   - UI hiển thị Cost = $349.34 (không phải $348.58)

2. **Có thể:**
   - User có thêm 0.05 SOL từ nguồn khác
   - User nhớ nhầm số tiền (nhớ Value thay vì Cost)
   - Hoặc có multiple trades

### Khuyến Nghị:
1. ✅ **Kiểm tra Trade History** để xem chính xác user đã mua như thế nào
2. ✅ **Verify cost basis calculation** có bao gồm fee đúng không
3. ✅ **Clarify với user** về số tiền thực tế đã chi

---

## 🔧 Test Case Để Verify

### Test Case: Mua 2 SOL @ $170.41
```
Input:
- BUY 2 SOL @ $170.41
- Fee: 2 × $170.41 × 0.001 = $0.34

Expected:
- Amount: 2.000000 SOL
- Avg Price: ($170.41 × 2 + $0.34) / 2 = $170.58
- Cost: 2 × $170.58 = $341.16

Actual (từ UI):
- Amount: 2.050000 SOL
- Avg Price: $170.41
- Cost: $349.34

Conclusion: Có sự khác biệt, cần kiểm tra trade history
```

---

**Last Updated:** 2024-11-XX  
**Status:** ⚠️ Cần Verify Trade History

