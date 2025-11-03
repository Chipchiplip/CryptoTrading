# Danh sách lỗi API Trading

## 🔴 LỖI NGHIÊM TRỌNG (Critical Errors)

### 1. **Endpoint không tồn tại: `/api/trading/history`**
   - **Mô tả**: File `TradingAPI.http` có endpoint `/api/trading/history` nhưng controller chỉ có `/api/trading/trades`
   - **Vị trí**: 
     - `TradingAPI.http:135-140` - Endpoint được test nhưng không tồn tại
     - `Controllers/TradingController.cs:606` - Chỉ có `[HttpGet("trades")]`
   - **Giải pháp**: Thêm endpoint `/api/trading/history` hoặc cập nhật HTTP file để dùng `/api/trading/trades`

### 2. **Endpoint không tồn tại: `/api/trading/holdings`**
   - **Mô tả**: File `TradingAPI.http` có endpoint `/api/trading/holdings` nhưng không được implement trong controller
   - **Vị trí**: 
     - `TradingAPI.http:147` - Endpoint được test
     - `Controllers/TradingController.cs` - Không có endpoint này
   - **Giải pháp**: Implement endpoint `GET /api/trading/holdings` để trả về portfolio holdings

### 3. **Route mismatch: Orderbook endpoint**
   - **Mô tả**: Controller dùng path parameter `[HttpGet("orderbook/{symbol}")]` nhưng HTTP test file dùng query parameter `?symbol=`
   - **Vị trí**: 
     - `Controllers/TradingController.cs:411` - `[HttpGet("orderbook/{symbol}")]`
     - `TradingAPI.http:123` - `GET /api/trading/orderbook?symbol=BTC/USD`
   - **Hậu quả**: Request từ HTTP file sẽ không match route, trả về 404
   - **Giải pháp**: Sửa route thành `[HttpGet("orderbook")]` với `[FromQuery] string symbol`

## ⚠️ LỖI CHỨC NĂNG (Functional Errors)

### 4. **Locked Balance không được tính toán**
   - **Mô tả**: Locked balance luôn trả về 0 vì chưa implement logic tính toán từ OrderHolds
   - **Vị trí**: 
     - `Controllers/TradingController.cs:460` - `var locked = 0m; // TODO: Calculate locked from OrderHolds`
   - **Hậu quả**: 
     - Balance API không hiển thị đúng số tiền bị lock bởi orders
     - Available balance không chính xác
   - **Giải pháp**: Implement logic tính locked balance từ bảng `OrderHolds`

### 5. **Filtering multiple status không hoạt động**
   - **Mô tả**: `OrdersQuery.Status` chỉ là `string?` nhưng HTTP file test với multiple values `?status=NEW&status=PARTIAL`
   - **Vị trí**: 
     - `TradingAPI.http:107` - `GET /api/trading/orders?status=NEW&status=PARTIAL`
     - `Models/DTOs/TradingDtos.cs:190` - `public string? Status { get; set; }`
   - **Hậu quả**: Chỉ filter được 1 status, không thể filter nhiều status cùng lúc
   - **Giải pháp**: Đổi `Status` thành `List<string>?` hoặc `string[]?`

### 6. **Symbol format không nhất quán**
   - **Mô tả**: HTTP file dùng `BTC/USD` và `ETH/USD` nhưng code có thể expect `BTC/USDT`
   - **Vị trí**: 
     - `TradingAPI.http:70, 82, 95` - Dùng `BTC/USD`, `ETH/USD`
     - `Services/Trading/TradingService.cs:957` - Map symbol thành `{coinSymbol}/USDT`
   - **Hậu quả**: Symbol format không match giữa request và response
   - **Giải pháp**: Chuẩn hóa symbol format (dùng USD hoặc USDT nhất quán)

## 🟡 LỖI XỬ LÝ LỖI (Error Handling Issues)

### 7. **Error response không có cấu trúc chuẩn**
   - **Mô tả**: Nhiều endpoint catch exception nhưng không trả về error code/message chuẩn
   - **Vị trí**: 
     - `Controllers/TradingController.cs:529-532` - Generic `StatusCode(500)` không có error code
     - `Controllers/TradingController.cs:552-555` - Generic exception handling
   - **Hậu quả**: Frontend khó xử lý lỗi cụ thể
   - **Giải pháp**: Dùng `ErrorHandlingMiddleware` hoặc trả về error response có structure nhất quán

### 8. **Exception details bị mất trong catch blocks**
   - **Mô tả**: Nhiều catch blocks không log exception details
   - **Vị trí**: 
     - `Controllers/TradingController.cs:570-573` - `catch (Exception)` không log
     - `Controllers/TradingController.cs:615-618` - `catch (Exception)` không log
   - **Hậu quả**: Khó debug khi có lỗi production
   - **Giải pháp**: Thêm logging trong catch blocks hoặc để middleware xử lý

### 9. **InvalidOperationException trả về NotFound thay vì BadRequest**
   - **Mô tả**: `GetOrder` endpoint catch `InvalidOperationException` và trả về `NotFound` nhưng nên là `BadRequest`
   - **Vị trí**: 
     - `Controllers/TradingController.cs:548-550` - `InvalidOperationException` → `NotFound`
   - **Hậu quả**: Status code không đúng semantic
   - **Giải pháp**: Phân biệt giữa "order không tồn tại" (NotFound) và "order invalid" (BadRequest)

## 🔵 LỖI VALIDATION (Validation Issues)

### 10. **Missing validation cho OrdersQuery**
   - **Mô tả**: `OrdersQuery` có validation attributes nhưng không được validate tự động
   - **Vị trí**: 
     - `Models/DTOs/TradingDtos.cs:170-213` - Có `[Range]` attributes
   - **Hậu quả**: Invalid values có thể pass vào service layer
   - **Giải pháp**: Đảm bảo model validation được enable trong controller

### 11. **Symbol parsing không handle edge cases**
   - **Mô tả**: `ParseSymbol` method throw `ArgumentException` nhưng không có validation chi tiết
   - **Vị trí**: 
     - `Services/Trading/TradingService.cs:938-947` - Chỉ check `Split('/').Length != 2`
   - **Hậu quả**: Symbols như `"BTC//USD"`, `"BTC/USD/"` có thể không được detect
   - **Giải pháp**: Thêm validation chi tiết hơn (trim, empty check, etc.)

## 🟢 VẤN ĐỀ HIỆU SUẤT (Performance Issues)

### 12. **N+1 query problem trong Dashboard**
   - **Mô tả**: Dashboard endpoints có thể gây N+1 queries khi load nhiều cryptocurrencies
   - **Vị trí**: 
     - `Controllers/TradingController.cs:218-231` - Loop qua positions và query price cho mỗi crypto
     - `Controllers/TradingController.cs:311-321` - Loop qua positions trong NAV history
   - **Hậu quả**: Slow response khi có nhiều positions
   - **Giải pháp**: Batch load prices hoặc dùng `Include` với query optimization

### 13. **Market data được fetch nhiều lần**
   - **Mô tả**: `GetMarketDataAsync()` được gọi nhiều lần trong cùng một request
   - **Vị trí**: 
     - `Controllers/TradingController.cs:162, 450` - Fetch market data riêng biệt
   - **Hậu quả**: Unnecessary API calls đến CoinGecko
   - **Giải pháp**: Cache market data trong request scope hoặc dependency injection

## 📋 TÓM TẮT

| Loại lỗi | Số lượng | Mức độ |
|---------|---------|--------|
| Critical (Endpoint missing, Route mismatch) | 3 | 🔴 |
| Functional (Logic chưa implement) | 3 | ⚠️ |
| Error Handling | 3 | 🟡 |
| Validation | 2 | 🔵 |
| Performance | 2 | 🟢 |
| **TỔNG** | **13** | |

## 🎯 ƯU TIÊN SỬA LỖI

1. **Ưu tiên cao (P0)**:
   - Sửa route mismatch orderbook (#3)
   - Implement endpoint holdings (#2)
   - Implement endpoint history hoặc cập nhật HTTP file (#1)

2. **Ưu tiên trung bình (P1)**:
   - Fix locked balance calculation (#4)
   - Standardize error response structure (#7)
   - Fix symbol format inconsistency (#6)

3. **Ưu tiên thấp (P2)**:
   - Improve error handling và logging (#8, #9)
   - Add validation improvements (#10, #11)
   - Optimize performance (#12, #13)


