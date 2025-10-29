# CryptoTrade - Complete Trading Platform

## Tổng quan hệ thống

Hệ thống CryptoTrade hoàn chỉnh với 3 loại người dùng và 32 trang chức năng.

## Cấu trúc hệ thống

### 1. GUEST USERS (8 trang)
Người dùng chưa đăng nhập có thể:

- **Home** (`/home`) - Trang chủ với giới thiệu sản phẩm
- **Markets** (`/markets`) - Xem bảng giá tổng hợp với tìm kiếm và filter
- **Coin Detail** (`/coin-detail`) - Chi tiết coin với chart, order book view-only
- **Login** (`/login`) - Đăng nhập với social login
- **Register** (`/register`) - Đăng ký tài khoản với validation
- **Forgot Password** (`/forgot-password`) - Quên mật khẩu
- **Reset Password** (`/reset-password`) - Đặt lại mật khẩu
- **Verify Email** (`/verify-email`) - Xác thực email

### 2. TRADER/USER (12 trang)
Người dùng đã đăng nhập có thể:

- **Dashboard** (`/trader-dashboard`) - Tổng quan NAV, PnL, số dư, lệnh gần đây
- **Watchlist** (`/watchlist`) - Quản lý danh sách coin yêu thích
- **Trade** (`/trade`) - Tạo lệnh Market/Limit với BUY/SELL
- **Orders** (`/orders`) - Liệt kê, lọc và hủy lệnh
- **Order Detail** (`/order-detail`) - Chi tiết lệnh với fill timeline
- **Trades History** (`/trades-history`) - Lịch sử khớp lệnh
- **Portfolio** (`/portfolio`) - Vị thế, NAV mark-to-market
- **Wallets** (`/wallets`) - Quản lý số dư FIAT & COIN
- **Deposit** (`/deposit`) - Tạo yêu cầu nạp tiền
- **Withdraw** (`/withdraw`) - Tạo yêu cầu rút tiền với 2FA
- **Subscription** (`/subscription`) - Quản lý plan và billing
- **Settings** (`/settings`) - Profile, 2FA, API Keys, Login Activity

### 3. ADMIN (12 trang)
Quản trị viên có thể:

- **Dashboard** (`/dashboard`) - Metrics tổng quan hệ thống
- **Users** (`/users`) - Quản lý users (CRUD, khóa/mở)
- **User Detail** (`/user-detail`) - Chi tiết user với tabs
- **Roles & Permissions** (`/roles`) - Quản lý phân quyền
- **Orders Monitor** (`/orders-monitor`) - Giám sát tất cả lệnh
- **Trades Monitor** (`/trades-monitor`) - Giám sát giao dịch
- **Deposits Approvals** (`/deposits-approvals`) - Duyệt nạp tiền
- **Withdrawals Approvals** (`/withdrawals-approvals`) - Duyệt rút tiền
- **Plans & Subscriptions** (`/plans-subscriptions`) - Quản lý gói dịch vụ
- **Coins Catalog** (`/coins`) - Quản lý danh sách coin
- **Price Feeds Monitor** (`/price-feeds`) - Giám sát nguồn giá
- **Market Statistics** (`/market-stats`) - Thống kê thị trường

## Thiết kế

### Theme
- **Dark Theme**: Background đen (#000000)
- **Accent Color**: Emerald Green (#22c55e)
- **Text**: Trắng với độ tương phản cao

### Layout
- **Guest**: Navigation bar ở top + Footer
- **Trader**: Sidebar navigation với quick stats
- **Admin**: Sidebar navigation với header search

### Components
- Sử dụng shadcn/ui components
- Recharts cho biểu đồ
- Responsive design cho mobile/tablet/desktop

## Navigation

### Chuyển đổi giữa các loại user

Từ trang Home, click:
- **"Sign Up"** → Guest registration flow
- **"Login"** → Login page
- **"Demo Trader"** → Truy cập Trader Dashboard (demo)

Trong code (App.tsx):
- `userRole` state quản lý quyền: 'guest' | 'trader' | 'admin'
- Routing dựa trên `currentPage` state

## Tech Stack

- **React** - UI framework
- **TypeScript** - Type safety
- **Tailwind CSS** - Styling
- **shadcn/ui** - UI components
- **Recharts** - Charts/graphs
- **Lucide React** - Icons

## Cách sử dụng

1. Mở ứng dụng → Hiển thị Home page (Guest)
2. Click "Demo Trader" → Chuyển sang Trader Dashboard
3. Trong Trader Dashboard:
   - Click "Watchlist" để quản lý coin yêu thích
   - Click "Trade" để tạo lệnh mới
   - Click "Orders" để xem danh sách lệnh
   - Click vào order ID để xem chi tiết
   - Và các trang khác...

## Features đầy đủ

### Guest Features
✅ Xem markets real-time
✅ Xem chi tiết coin với chart
✅ Authentication flow hoàn chỉnh
✅ Responsive design

### Trader Features
✅ Dashboard với NAV, PnL tracking
✅ Trading với Market/Limit orders
✅ Portfolio management
✅ Wallet management (Crypto + Fiat)
✅ Deposit/Withdraw workflow
✅ Subscription management
✅ 2FA security
✅ API keys management
✅ Login activity tracking

### Admin Features
✅ User management (CRUD)
✅ Order & trade monitoring
✅ Approval workflows
✅ System statistics
✅ Real-time notifications

## File Structure

```
/components
  /pages
    /guest       - 8 trang Guest
    /trader      - 12 trang Trader
    /           - 12 trang Admin
  /ui            - shadcn components
  GuestLayout.tsx
  TraderLayout.tsx
  Sidebar.tsx
  NotificationsPanel.tsx
/App.tsx         - Main routing
/styles/globals.css - Dark theme
```

## Notes

- Tất cả data hiện tại là mock data
- Các chức năng API đã được stub sẵn
- 2FA workflow được simulate
- File upload được handle
- Responsive trên tất cả devices
