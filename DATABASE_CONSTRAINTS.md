# Database Constraints - Quy Tắc Bắt Buộc

## ✅ QUY TẮC ĐÃ XÁC NHẬN

### 🚫 KHÔNG ĐƯỢC PHÉP
- ❌ **ALTER TABLE** - Thay đổi cấu trúc bảng hiện có
- ❌ **DROP TABLE** - Xóa bảng hiện có  
- ❌ **RENAME TABLE/COLUMN** - Đổi tên bảng/cột
- ❌ **MODIFY COLUMN** - Thay đổi kiểu dữ liệu cột
- ❌ **CHANGE COLUMN** - Đổi tên/thay đổi cột
- ❌ **DROP COLUMN** - Xóa cột
- ❌ **EF Core Migrations** thay đổi schema hiện có

### ✅ ĐƯỢC PHÉP
- ✅ **CREATE TABLE** - Tạo bảng mới
- ✅ **CREATE INDEX** - Tạo index mới (không ảnh hưởng schema)
- ✅ **Thay đổi CODE** - Service, Repository, Domain Logic
- ✅ **Tạo Entity mới** - Mapping với bảng mới
- ✅ **Tạo DTO/Mapping Layer** - Xử lý logic trong code
- ✅ **Computed Fields** - Tính toán trong code, không lưu DB

## 📋 PHƯƠNG PHÁP MỞ RỘNG

Khi cần thêm tính năng mới:

1. **Logic mới** → Implement trong Service/Repository
2. **Dữ liệu mới** → Tạo bảng mới hoặc Entity mới
3. **Mapping** → Dùng DTO để map giữa schema cũ và logic mới
4. **Computed** → Tính toán trong code, không thay đổi DB

## 🔍 KIỂM TRA HIỆN TRẠNG

### Migration Files
- ✅ Không có EF Core migration files trong thư mục Migrations
- ✅ SQL scripts chỉ dùng `CREATE TABLE IF NOT EXISTS` (an toàn)
- ⚠️ Program.cs có code auto-apply migrations - cần đảm bảo không tạo migration mới

### Database Context
- ✅ ApplicationDbContext chỉ định nghĩa entities, không có migration code
- ✅ Tất cả configuration trong `OnModelCreating` phải match với schema hiện tại

## 🎯 CAM KẾT

**Tất cả thay đổi sẽ:**
- ✅ Chỉ sửa code (C#)
- ✅ Tạo bảng mới nếu cần lưu dữ liệu
- ✅ Không động vào bảng/cột hiện có
- ✅ Tương thích 100% với database hiện tại
- ✅ Không gây lỗi migration
- ✅ Không vi phạm foreign key

---

**Ngày tạo:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Trạng thái:** ✅ Đã xác nhận và cam kết tuân thủ

