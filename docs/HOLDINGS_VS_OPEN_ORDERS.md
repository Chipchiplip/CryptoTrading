# 📊 Holdings vs Open Orders - Sự Khác Biệt

## ❌ Holdings KHÔNG PHẢI là Limit Orders Đang Chờ Khớp

### 🔍 Holdings (Số Lượng Crypto Đã Sở Hữu)

**Holdings** là số lượng crypto mà bạn **đã sở hữu** (đã mua và đã được fill).

#### Cách Tính Holdings:
```csharp
// Từ code PortfolioService.cs (line 44-48)
var movements = await _context.WalletMovements
    .Where(m => walletIds.Contains(m.WalletId))
    .GroupBy(m => m.WalletId)
    .Select(g => new { WalletId = g.Key, Balance = g.Sum(m => m.Amount) })
    .ToDictionaryAsync(x => x.WalletId, x => x.Balance);
```

**Holdings được tính từ:**
- ✅ **WalletMovements** - Các giao dịch đã được thực hiện (filled)
- ✅ Chỉ tính các **Trades có Status = "FILLED"** (line 59)
- ✅ Số lượng crypto đã được **credit vào wallet** sau khi trade thành công

**Ví dụ:**
- Bạn đặt lệnh BUY 2 SOL @ $170
- Lệnh được **FILLED** (khớp thành công)
- → 2 SOL được credit vào wallet của bạn
- → **Holdings = 2 SOL** ✅

---

### ⏳ Open Orders (Lệnh Đang Chờ Khớp)

**Open Orders** là các lệnh **chưa được fill** hoặc chỉ fill một phần, đang chờ khớp.

#### Đặc Điểm:
- ❌ **Status = "NEW"** - Lệnh mới, chưa khớp
- ❌ **Status = "PARTIAL"** - Lệnh đã khớp một phần
- ✅ **Status = "FILLED"** - Lệnh đã khớp hoàn toàn (KHÔNG phải open order)

**Ví dụ:**
- Bạn đặt lệnh BUY 2 SOL @ $170 (LIMIT order)
- Lệnh chưa khớp (giá hiện tại = $175 > $170)
- → Lệnh có **Status = "NEW"**
- → Đây là **Open Order**, KHÔNG phải Holding
- → Số tiền $340 bị **LOCK** trong OrderHolds
- → Holdings vẫn = 0 SOL (chưa có gì)

---

## 🔄 Quy Trình Từ Order → Holding

### Bước 1: Đặt Lệnh (Place Order)
```
User đặt lệnh: BUY 2 SOL @ $170 (LIMIT)
→ Order được tạo với Status = "NEW"
→ Số tiền $340 bị LOCK trong OrderHolds
→ Holdings = 0 SOL (chưa có)
```

### Bước 2: Lệnh Đang Chờ Khớp (Open Order)
```
Order Status = "NEW" hoặc "PARTIAL"
→ Đây là Open Order
→ Số tiền vẫn bị LOCK
→ Holdings vẫn = 0 SOL
```

### Bước 3: Lệnh Được Khớp (Filled)
```
Khi giá SOL xuống $170:
→ Order được match và fill
→ Status = "FILLED"
→ Trade được tạo
→ WalletMovement được tạo: +2 SOL vào wallet
→ OrderHold được release: -$340 từ locked balance
→ Holdings = 2 SOL ✅
```

---

## 📊 So Sánh Holdings vs Open Orders

| Đặc Điểm | Holdings | Open Orders |
|----------|----------|-------------|
| **Định nghĩa** | Crypto đã sở hữu | Lệnh đang chờ khớp |
| **Nguồn dữ liệu** | WalletMovements | Orders table |
| **Status** | Từ FILLED trades | NEW, PARTIAL |
| **Hiển thị ở đâu** | Portfolio page | Orders page |
| **Có thể trade** | ✅ Có (available balance) | ❌ Không (bị lock) |
| **Tính PnL** | ✅ Có | ❌ Không (chưa sở hữu) |

---

## 💡 Ví Dụ Thực Tế

### Scenario 1: Chỉ Có Open Orders
```
Bạn đặt:
- BUY 2 SOL @ $170 (LIMIT) - Status: NEW
- BUY 1 BTC @ $50,000 (LIMIT) - Status: NEW

Kết quả:
- Holdings: 0 SOL, 0 BTC (chưa có gì)
- Open Orders: 2 orders đang chờ
- Locked Balance: $340 (SOL) + $50,000 (BTC) = $50,340
- Available Balance: $0 (tất cả đã bị lock)
```

### Scenario 2: Có Holdings + Open Orders
```
Bạn đã có:
- Holdings: 2 SOL (đã mua và fill trước đó)

Bạn đặt thêm:
- BUY 1 SOL @ $165 (LIMIT) - Status: NEW

Kết quả:
- Holdings: 2 SOL (chỉ tính số đã sở hữu)
- Open Orders: 1 order đang chờ
- Locked Balance: $165 (cho order mới)
- Available Balance: $0 (nếu không còn tiền)
```

### Scenario 3: Chỉ Có Holdings
```
Bạn đã mua:
- BUY 2 SOL @ $170 - Status: FILLED ✅

Kết quả:
- Holdings: 2 SOL ✅
- Open Orders: 0 (không có lệnh nào đang chờ)
- Locked Balance: $0
- Available Balance: $0 (đã dùng hết để mua)
```

---

## 🔍 Code Reference

### Holdings Calculation (PortfolioService.cs)
```csharp
// Line 44-48: Tính balance từ WalletMovements
var movements = await _context.WalletMovements
    .Where(m => walletIds.Contains(m.WalletId))
    .GroupBy(m => m.WalletId)
    .Select(g => new { WalletId = g.Key, Balance = g.Sum(m => m.Amount) })
    .ToDictionaryAsync(x => x.WalletId, x => x.Balance);

// Line 59: Chỉ tính từ FILLED trades
var allTrades = await _context.Trades
    .Where(t => t.Order.UserId == userId && t.Order.Status == "FILLED")
    ...
```

### Open Orders (TradingController.cs)
```csharp
// Open orders là các orders với Status = NEW hoặc PARTIAL
var openOrders = await _db.Orders
    .Where(o => o.UserId == userId && 
                (o.Status == "NEW" || o.Status == "PARTIAL"))
    ...
```

### Locked Balance (TradingController.cs)
```csharp
// Line 148-152: Tính locked balance từ OrderHolds
var orderHolds = await _db.OrderHolds
    .Where(h => walletIds.Contains(h.WalletId) && h.ReleasedAt == null)
    .GroupBy(h => h.WalletId)
    .Select(g => new { WalletId = g.Key, LockedAmount = g.Sum(h => h.Amount) })
    ...
```

---

## ✅ Kết Luận

**Holdings ≠ Open Orders**

- **Holdings**: Crypto đã sở hữu (từ filled trades)
- **Open Orders**: Lệnh đang chờ khớp (chưa fill)

**Trong Portfolio page:**
- Chỉ hiển thị **Holdings** (crypto đã sở hữu)
- **KHÔNG** hiển thị Open Orders
- Open Orders được hiển thị ở **Orders page**

**Để xem Open Orders:**
- Vào **Orders page** hoặc **Trading page**
- Xem các lệnh có Status = NEW hoặc PARTIAL

---

**Last Updated:** 2024-11-XX  
**Status:** ✅ Clarified

