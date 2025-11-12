# Hướng dẫn nâng cấp Node.js

## Vấn đề
Project này yêu cầu **Node.js 18+** (hoặc **Node.js 20+** cho một số packages như react-router-dom).

Phiên bản hiện tại: **Node.js v16.16.0** (quá cũ)

## Cách nâng cấp Node.js trên Windows

### Cách 1: Tải và cài đặt từ nodejs.org (Khuyến nghị)

1. Truy cập: https://nodejs.org/
2. Tải **Node.js LTS** (version 20.x hoặc 18.x)
3. Chạy file installer và làm theo hướng dẫn
4. Mở lại terminal/PowerShell mới
5. Kiểm tra phiên bản:
   ```powershell
   node --version
   ```
   Nên thấy: `v20.x.x` hoặc `v18.x.x`

### Cách 2: Sử dụng nvm-windows (Quản lý nhiều phiên bản)

1. Tải nvm-windows: https://github.com/coreybutler/nvm-windows/releases
2. Cài đặt nvm-windows
3. Mở PowerShell **với quyền Administrator**
4. Cài đặt Node.js 20:
   ```powershell
   nvm install 20
   nvm use 20
   ```
5. Kiểm tra:
   ```powershell
   node --version
   ```

## Sau khi nâng cấp

1. Xóa node_modules và package-lock.json:
   ```powershell
   cd frontend
   Remove-Item -Recurse -Force node_modules
   Remove-Item package-lock.json
   ```

2. Cài đặt lại dependencies:
   ```powershell
   npm install
   ```

3. Khôi phục Vite về version 6 (nếu muốn):
   ```powershell
   npm install vite@6.3.5 --save-dev
   ```

4. Chạy dev server:
   ```powershell
   npm run dev
   ```

## Lưu ý

- Nếu bạn đang dùng nvm, đảm bảo chọn đúng phiên bản Node.js trong terminal mới
- Nếu gặp lỗi permission, chạy PowerShell với quyền Administrator

