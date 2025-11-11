# 📋 Portfolio Test Cases - Frontend Testing Guide

## 🎯 Mục Đích
Tài liệu này mô tả các test cases chi tiết cho Portfolio feature, test từng luồng (flow) từ frontend.

---

## 📦 Test Environment Setup

### Prerequisites
1. Backend API đang chạy tại `http://localhost:5186` hoặc `https://localhost:7269`
2. Frontend đang chạy tại `http://localhost:5173`
3. Database đã được seed với test data
4. User đã đăng nhập và có JWT token

### Test Data Requirements
- User có ít nhất 1 wallet với balance > 0
- User có ít nhất 1 trade (BUY/SELL) đã được execute
- Market data (crypto prices) đã được sync

---

## 🔄 Test Flows

### **FLOW 1: Load Portfolio Page - Happy Path**

#### **Test Case 1.1: Load Portfolio với Holdings**
**Mục đích:** Kiểm tra portfolio page load thành công với data đầy đủ

**Preconditions:**
- User đã đăng nhập
- User có holdings (wallets với balance > 0)
- User có trades history

**Steps:**
1. Navigate đến `/portfolio` page
2. Đợi page load hoàn tất

**Expected Results:**
- ✅ Loading indicator hiển thị trong quá trình fetch data
- ✅ Portfolio summary cards hiển thị:
  - Total Value: số tiền > 0
  - Total Cost: số tiền > 0
  - Unrealized PnL: có thể âm hoặc dương
  - Unrealized PnL Percent: có thể âm hoặc dương
  - Realized PnL: có thể âm hoặc dương
- ✅ Performance chart hiển thị với NAV history (30 days)
- ✅ Asset Allocation pie chart hiển thị
- ✅ Holdings table hiển thị với các columns:
  - Asset (symbol + name)
  - Amount
  - Avg Price
  - Current Price
  - Value
  - PnL (với icon TrendingUp/TrendingDown)
  - PnL % (với badge màu xanh/đỏ)
  - Allocation %

**Postconditions:**
- Portfolio data được hiển thị chính xác
- Không có error messages

---

#### **Test Case 1.2: Load Portfolio với Empty Holdings**
**Mục đích:** Kiểm tra portfolio page khi user chưa có holdings

**Preconditions:**
- User đã đăng nhập
- User chưa có holdings (tất cả wallets có balance = 0)
- User chưa có trades

**Steps:**
1. Navigate đến `/portfolio` page
2. Đợi page load hoàn tất

**Expected Results:**
- ✅ Loading indicator hiển thị
- ✅ Portfolio summary cards hiển thị với giá trị 0:
  - Total Value: $0.00
  - Total Cost: $0.00
  - Unrealized PnL: $0.00
  - Unrealized PnL Percent: 0.00%
  - Realized PnL: $0.00
- ✅ Performance chart hiển thị nhưng không có data points
- ✅ Asset Allocation pie chart không hiển thị (hoặc hiển thị empty state)
- ✅ Holdings table hiển thị empty state hoặc "No holdings"

**Postconditions:**
- Page load thành công, không crash
- UI hiển thị empty state đúng cách

---

### **FLOW 2: Portfolio Calculations - Accuracy**

#### **Test Case 2.1: Verify Cost Basis Calculation**
**Mục đích:** Kiểm tra cost basis được tính đúng với FIFO method

**Preconditions:**
- User có multiple BUY trades cho cùng 1 symbol
- User có SELL trades cho symbol đó

**Test Data Setup:**
```
BUY BTC: 0.1 BTC @ $50,000 (Fee: $5)
BUY BTC: 0.2 BTC @ $55,000 (Fee: $10)
SELL BTC: 0.15 BTC @ $60,000 (Fee: $6)
Current Price: $58,000
Remaining: 0.15 BTC
```

**Steps:**
1. Load portfolio page
2. Tìm BTC trong holdings table
3. Verify Avg Price và Cost

**Expected Results:**
- ✅ Avg Price = (0.1 * 50000 + 5 + 0.2 * 55000 + 10 - 0.15 * 50000 - 5) / 0.15
  - = (5000 + 5 + 11000 + 10 - 7500 - 5) / 0.15
  - = 8500 / 0.15 = $56,666.67
- ✅ Cost = 0.15 * 56666.67 = $8,500
- ✅ Value = 0.15 * 58000 = $8,700
- ✅ PnL = 8700 - 8500 = $200
- ✅ PnL % = (200 / 8500) * 100 = 2.35%

**Postconditions:**
- Cost basis được tính đúng theo FIFO
- SELL trades được trừ đúng khỏi cost basis

---

#### **Test Case 2.2: Verify Realized PnL Calculation**
**Mục đích:** Kiểm tra realized PnL được tính đúng từ closed trades

**Preconditions:**
- User có SELL trades đã được filled

**Test Data Setup:**
```
BUY BTC: 0.1 BTC @ $50,000 (Fee: $5) - Cost: $5,005
SELL BTC: 0.1 BTC @ $60,000 (Fee: $6) - Revenue: $5,994
Realized PnL = 5994 - 5005 = $989
```

**Steps:**
1. Load portfolio page
2. Check Realized PnL card

**Expected Results:**
- ✅ Realized PnL = $989
- ✅ Realized PnL được tính từ tất cả SELL trades đã filled
- ✅ Cost basis của SELL trades được tính đúng tại thời điểm bán

**Postconditions:**
- Realized PnL chính xác
- Không bị double-count

---

#### **Test Case 2.3: Verify Unrealized PnL Calculation**
**Mục đích:** Kiểm tra unrealized PnL được tính đúng từ current holdings

**Preconditions:**
- User có holdings với current price khác avg price

**Test Data Setup:**
```
Holdings:
- BTC: 0.15 BTC, Avg Price: $56,666.67, Current Price: $58,000
- ETH: 2 ETH, Avg Price: $3,000, Current Price: $3,200

Total Cost = (0.15 * 56666.67) + (2 * 3000) = 8500 + 6000 = $14,500
Total Value = (0.15 * 58000) + (2 * 3200) = 8700 + 6400 = $15,100
Unrealized PnL = 15100 - 14500 = $600
Unrealized PnL % = (600 / 14500) * 100 = 4.14%
```

**Steps:**
1. Load portfolio page
2. Check Unrealized PnL card
3. Verify từng holding trong table

**Expected Results:**
- ✅ Total Cost = $14,500
- ✅ Total Value = $15,100
- ✅ Unrealized PnL = $600
- ✅ Unrealized PnL % = 4.14%
- ✅ Mỗi holding có PnL và PnL % chính xác

**Postconditions:**
- Unrealized PnL chính xác
- Percentages được tính đúng

---

#### **Test Case 2.4: Verify Allocation Calculation**
**Mục đích:** Kiểm tra allocation % được tính đúng

**Test Data Setup:**
```
Holdings:
- BTC: Value = $8,700
- ETH: Value = $6,400
Total Value = $15,100

BTC Allocation = (8700 / 15100) * 100 = 57.62%
ETH Allocation = (6400 / 15100) * 100 = 42.38%
```

**Steps:**
1. Load portfolio page
2. Check Asset Allocation pie chart
3. Check Allocation column trong holdings table

**Expected Results:**
- ✅ BTC Allocation = 57.62%
- ✅ ETH Allocation = 42.38%
- ✅ Tổng allocation = 100%
- ✅ Pie chart hiển thị đúng tỷ lệ
- ✅ Legend hiển thị đúng percentages

**Postconditions:**
- Allocations chính xác
- Pie chart và table đồng bộ

---

### **FLOW 3: Error Handling**

#### **Test Case 3.1: API Error - Network Failure**
**Mục đích:** Kiểm tra error handling khi API call fail

**Preconditions:**
- User đã đăng nhập
- Backend API bị down hoặc network error

**Steps:**
1. Stop backend API
2. Navigate đến `/portfolio` page
3. Đợi page load

**Expected Results:**
- ✅ Loading indicator hiển thị
- ✅ Error banner hiển thị với message: "Failed to load portfolio data" hoặc "Network error"
- ✅ Error banner có màu đỏ và icon AlertCircle
- ✅ Page không crash, vẫn hiển thị layout
- ✅ Holdings table và charts hiển thị empty state

**Postconditions:**
- Error được handle gracefully
- User có thể retry bằng cách refresh page

---

#### **Test Case 3.2: API Error - Unauthorized**
**Mục đích:** Kiểm tra error handling khi token hết hạn

**Preconditions:**
- User đã đăng nhập nhưng token đã hết hạn

**Steps:**
1. Expire JWT token (hoặc remove token)
2. Navigate đến `/portfolio` page
3. Đợi page load

**Expected Results:**
- ✅ Error banner hiển thị: "Unauthorized" hoặc "Invalid token"
- ✅ User được redirect về login page (nếu có auth guard)
- ✅ Hoặc error message rõ ràng

**Postconditions:**
- User được thông báo về lỗi authentication
- User có thể login lại

---

#### **Test Case 3.3: Partial Data Error**
**Mục đích:** Kiểm tra khi một số API calls fail nhưng một số thành công

**Preconditions:**
- User đã đăng nhập
- Backend trả về partial data (một số endpoints fail)

**Steps:**
1. Mock API để một số endpoints fail
2. Navigate đến `/portfolio` page
3. Đợi page load

**Expected Results:**
- ✅ Nếu overview API fail: Error banner hiển thị
- ✅ Nếu overview API thành công nhưng thiếu data: Hiển thị data có sẵn
- ✅ Empty states được hiển thị cho missing data
- ✅ Page không crash

**Postconditions:**
- Graceful degradation
- User vẫn thấy được một phần data

---

### **FLOW 4: UI/UX Testing**

#### **Test Case 4.1: Loading States**
**Mục đích:** Kiểm tra loading indicators

**Steps:**
1. Navigate đến `/portfolio` page
2. Observe loading behavior

**Expected Results:**
- ✅ Loading indicator (Loader2 icon) hiển thị ngay khi page load
- ✅ Text "Loading portfolio data..." hiển thị
- ✅ Loading indicator có animation (spin)
- ✅ Loading indicator biến mất khi data đã load xong
- ✅ Không có flash of empty content

**Postconditions:**
- Loading state rõ ràng
- Smooth transition từ loading sang content

---

#### **Test Case 4.2: Responsive Design**
**Mục đích:** Kiểm tra portfolio page trên các screen sizes

**Steps:**
1. Load portfolio page trên desktop (1920x1080)
2. Resize browser xuống tablet (768x1024)
3. Resize browser xuống mobile (375x667)

**Expected Results:**
- ✅ Desktop: Grid layout 4 columns cho summary cards
- ✅ Tablet: Grid layout 2 columns cho summary cards
- ✅ Mobile: Grid layout 1 column cho summary cards
- ✅ Charts responsive và scrollable trên mobile
- ✅ Holdings table có horizontal scroll trên mobile
- ✅ Text không bị overflow
- ✅ All elements visible và accessible

**Postconditions:**
- Page responsive trên tất cả devices
- UX tốt trên mobile

---

#### **Test Case 4.3: Data Formatting**
**Mục đích:** Kiểm tra số tiền và percentages được format đúng

**Steps:**
1. Load portfolio page với holdings
2. Check formatting của các số

**Expected Results:**
- ✅ Currency values format với 2 decimal places: `$12,345.67`
- ✅ Percentages format với 2 decimal places: `12.34%`
- ✅ Large numbers có thousand separators
- ✅ Negative values hiển thị với dấu `-`
- ✅ Positive values có thể có dấu `+` (optional)
- ✅ Zero values hiển thị: `$0.00` hoặc `0.00%`

**Postconditions:**
- Data formatting consistent
- Dễ đọc và hiểu

---

#### **Test Case 4.4: Color Coding**
**Mục đích:** Kiểm tra màu sắc cho profit/loss

**Steps:**
1. Load portfolio page với holdings có profit và loss
2. Check màu sắc của PnL values

**Expected Results:**
- ✅ Positive PnL: Màu xanh (emerald-500)
- ✅ Negative PnL: Màu đỏ (red-500)
- ✅ Positive PnL %: Badge màu xanh
- ✅ Negative PnL %: Badge màu đỏ
- ✅ TrendingUp icon cho positive
- ✅ TrendingDown icon cho negative
- ✅ Unrealized PnL card: Màu xanh nếu positive, đỏ nếu negative
- ✅ Realized PnL card: Màu xanh nếu positive, đỏ nếu negative

**Postconditions:**
- Color coding rõ ràng và consistent
- User dễ phân biệt profit/loss

---

### **FLOW 5: Performance Chart**

#### **Test Case 5.1: NAV History Display**
**Mục đích:** Kiểm tra performance chart hiển thị NAV history

**Preconditions:**
- User có NAV history data (30 days)

**Steps:**
1. Load portfolio page
2. Check performance chart

**Expected Results:**
- ✅ Chart hiển thị AreaChart với gradient fill
- ✅ X-axis hiển thị dates (format: "Jan 1", "Jan 2", etc.)
- ✅ Y-axis hiển thị values
- ✅ Tooltip hiển thị khi hover
- ✅ Chart có 30 data points (last 30 days)
- ✅ Chart responsive và fill container

**Postconditions:**
- Chart hiển thị đúng data
- Interactive và responsive

---

#### **Test Case 5.2: Empty NAV History**
**Mục đích:** Kiểm tra chart khi không có NAV history

**Preconditions:**
- User mới, chưa có NAV history

**Steps:**
1. Load portfolio page
2. Check performance chart

**Expected Results:**
- ✅ Chart container vẫn hiển thị
- ✅ Empty state message hoặc no data indicator
- ✅ Chart không crash

**Postconditions:**
- Empty state được handle
- UX tốt

---

### **FLOW 6: Asset Allocation Chart**

#### **Test Case 6.1: Pie Chart Display**
**Mục đích:** Kiểm tra asset allocation pie chart

**Preconditions:**
- User có multiple holdings

**Steps:**
1. Load portfolio page
2. Check asset allocation pie chart

**Expected Results:**
- ✅ Pie chart hiển thị với donut style (innerRadius > 0)
- ✅ Mỗi holding có màu khác nhau
- ✅ Colors từ COLORS array: green, blue, orange, gray, red, purple, pink, teal
- ✅ Tooltip hiển thị khi hover
- ✅ Legend hiển thị bên dưới với:
  - Color dot
  - Symbol name
  - Percentage
- ✅ Percentages tổng = 100%

**Postconditions:**
- Chart hiển thị đúng
- Legend và chart đồng bộ

---

#### **Test Case 6.2: Single Holding**
**Mục đích:** Kiểm tra pie chart với chỉ 1 holding

**Preconditions:**
- User chỉ có 1 holding

**Steps:**
1. Load portfolio page
2. Check asset allocation pie chart

**Expected Results:**
- ✅ Pie chart hiển thị full circle (100%)
- ✅ Legend hiển thị 1 item với 100%
- ✅ Chart không crash

**Postconditions:**
- Chart handle single holding correctly

---

### **FLOW 7: Holdings Table**

#### **Test Case 7.1: Table Display**
**Mục đích:** Kiểm tra holdings table hiển thị đúng

**Preconditions:**
- User có multiple holdings

**Steps:**
1. Load portfolio page
2. Check holdings table

**Expected Results:**
- ✅ Table có header với các columns:
  - Asset
  - Amount (right-aligned)
  - Avg Price (right-aligned)
  - Current Price (right-aligned)
  - Value (right-aligned)
  - PnL (right-aligned)
  - PnL % (right-aligned)
  - Allocation (right-aligned)
- ✅ Mỗi row hiển thị:
  - Asset icon/circle với symbol
  - Name và symbol
  - Amount với 6 decimal places
  - Prices với 2 decimal places
  - Values với 2 decimal places
  - PnL với icon và color
  - PnL % với badge
  - Allocation với 1 decimal place
- ✅ Rows có hover effect
- ✅ Table sortable (nếu có)

**Postconditions:**
- Table hiển thị đầy đủ thông tin
- Formatting đúng

---

#### **Test Case 7.2: Empty Holdings Table**
**Mục đích:** Kiểm tra table khi không có holdings

**Preconditions:**
- User không có holdings

**Steps:**
1. Load portfolio page
2. Check holdings table

**Expected Results:**
- ✅ Table header vẫn hiển thị
- ✅ Empty state message: "No holdings" hoặc tương tự
- ✅ Table không crash

**Postconditions:**
- Empty state được handle
- UX tốt

---

#### **Test Case 7.3: Large Holdings List**
**Mục đích:** Kiểm tra table với nhiều holdings

**Preconditions:**
- User có > 10 holdings

**Steps:**
1. Load portfolio page
2. Check holdings table
3. Scroll table (nếu có)

**Expected Results:**
- ✅ Tất cả holdings được hiển thị
- ✅ Table có scroll nếu cần
- ✅ Performance tốt, không lag
- ✅ Sorting hoạt động (nếu có)

**Postconditions:**
- Table handle large datasets
- Performance acceptable

---

### **FLOW 8: Real-time Updates**

#### **Test Case 8.1: Price Updates**
**Mục đích:** Kiểm tra portfolio cập nhật khi prices thay đổi

**Preconditions:**
- User đã load portfolio page
- Prices thay đổi (từ market data)

**Steps:**
1. Load portfolio page
2. Đợi prices update (từ SignalR hoặc polling)
3. Observe changes

**Expected Results:**
- ✅ Current Price cập nhật trong holdings table
- ✅ Value cập nhật
- ✅ PnL và PnL % cập nhật
- ✅ Total Value cập nhật
- ✅ Unrealized PnL cập nhật
- ✅ Performance chart cập nhật (nếu có real-time)
- ✅ Asset allocation cập nhật

**Postconditions:**
- Real-time updates hoạt động
- UI responsive

---

### **FLOW 9: Edge Cases**

#### **Test Case 9.1: Very Small Holdings**
**Mục đích:** Kiểm tra với holdings có giá trị rất nhỏ

**Test Data:**
```
Holding: 0.000001 BTC @ $50,000
Value: $0.05
```

**Steps:**
1. Load portfolio page với very small holdings
2. Check formatting

**Expected Results:**
- ✅ Amount hiển thị với đủ decimal places: `0.000001`
- ✅ Value hiển thị: `$0.05` (không bị round về 0)
- ✅ Percentages tính đúng
- ✅ Không có division by zero errors

**Postconditions:**
- Small values được handle correctly
- No errors

---

#### **Test Case 9.2: Very Large Holdings**
**Mục đích:** Kiểm tra với holdings có giá trị rất lớn

**Test Data:**
```
Holding: 100 BTC @ $50,000
Value: $5,000,000
```

**Steps:**
1. Load portfolio page với very large holdings
2. Check formatting

**Expected Results:**
- ✅ Large numbers có thousand separators: `$5,000,000.00`
- ✅ Amount hiển thị đúng: `100.000000`
- ✅ Percentages tính đúng
- ✅ UI không bị overflow

**Postconditions:**
- Large values được handle correctly
- Formatting đúng

---

#### **Test Case 9.3: Negative PnL**
**Mục đích:** Kiểm tra với holdings có negative PnL

**Test Data:**
```
Holding: 1 BTC
Avg Price: $60,000
Current Price: $50,000
PnL: -$10,000
PnL %: -16.67%
```

**Steps:**
1. Load portfolio page với negative PnL
2. Check display

**Expected Results:**
- ✅ PnL hiển thị với dấu `-`: `-$10,000.00`
- ✅ PnL % hiển thị với dấu `-`: `-16.67%`
- ✅ Màu đỏ cho negative values
- ✅ TrendingDown icon
- ✅ Badge màu đỏ

**Postconditions:**
- Negative values hiển thị đúng
- Color coding rõ ràng

---

#### **Test Case 9.4: Zero Cost Basis**
**Mục đích:** Kiểm tra khi cost basis = 0 (edge case)

**Test Data:**
```
Holding: 1 BTC (received as gift, no cost)
Current Price: $50,000
Cost: $0
PnL: $50,000
PnL %: N/A hoặc Infinity
```

**Steps:**
1. Load portfolio page với zero cost basis
2. Check calculations

**Expected Results:**
- ✅ Cost hiển thị: `$0.00`
- ✅ PnL = Value (vì cost = 0)
- ✅ PnL % hiển thị: `N/A` hoặc `Infinity` hoặc `--`
- ✅ Không có division by zero errors

**Postconditions:**
- Zero cost basis được handle
- No errors

---

#### **Test Case 9.5: Missing Crypto Price**
**Mục đích:** Kiểm tra khi crypto không có trong market data

**Preconditions:**
- User có holding với symbol không có trong market data

**Steps:**
1. Load portfolio page
2. Check holdings với missing price

**Expected Results:**
- ✅ Current Price = 0 hoặc "N/A"
- ✅ Value = 0
- ✅ PnL tính dựa trên cost basis
- ✅ Không crash
- ✅ Error message hoặc warning (optional)

**Postconditions:**
- Missing prices được handle
- No errors

---

## 🧪 Test Execution Checklist

### Pre-Test
- [ ] Backend API đang chạy
- [ ] Frontend đang chạy
- [ ] Database đã được seed
- [ ] User đã đăng nhập
- [ ] Browser console mở để check errors

### Test Execution
- [ ] Flow 1: Load Portfolio Page
- [ ] Flow 2: Portfolio Calculations
- [ ] Flow 3: Error Handling
- [ ] Flow 4: UI/UX Testing
- [ ] Flow 5: Performance Chart
- [ ] Flow 6: Asset Allocation Chart
- [ ] Flow 7: Holdings Table
- [ ] Flow 8: Real-time Updates
- [ ] Flow 9: Edge Cases

### Post-Test
- [ ] Tất cả test cases đã pass
- [ ] Không có console errors
- [ ] Không có network errors
- [ ] Performance acceptable (< 2s load time)
- [ ] UI responsive trên các devices

---

## 📊 Test Results Template

```
Test Date: [Date]
Tester: [Name]
Environment: [Dev/Staging/Prod]

Flow 1: Load Portfolio Page
- Test 1.1: [PASS/FAIL] - Notes: [Notes]
- Test 1.2: [PASS/FAIL] - Notes: [Notes]

Flow 2: Portfolio Calculations
- Test 2.1: [PASS/FAIL] - Notes: [Notes]
- Test 2.2: [PASS/FAIL] - Notes: [Notes]
...

Issues Found:
1. [Issue description]
2. [Issue description]

Recommendations:
1. [Recommendation]
2. [Recommendation]
```

---

## 🔧 Automated Testing (Future)

### Unit Tests
- Test cost basis calculation logic
- Test realized PnL calculation
- Test unrealized PnL calculation
- Test allocation calculation

### Integration Tests
- Test API endpoints
- Test data flow từ backend đến frontend
- Test error handling

### E2E Tests (Cypress/Playwright)
- Test full user flow
- Test UI interactions
- Test responsive design

---

**Last Updated:** 2024-11-XX  
**Version:** 1.0  
**Status:** ✅ Ready for Testing

