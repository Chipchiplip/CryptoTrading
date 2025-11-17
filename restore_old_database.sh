#!/bin/bash

echo "========================================"
echo "Khôi phục Database về OLD SCHEMA"
echo "========================================"
echo ""
echo "⚠️  CẢNH BÁO: Script này sẽ XÓA TẤT CẢ DATA!"
echo ""
read -p "Bạn có chắc chắn? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo "Đã hủy."
    exit 0
fi

echo ""
echo "[1/5] Kiểm tra MySQL connection..."
mysql -u root -p -e "SELECT 1;" > /dev/null 2>&1
if [ $? -ne 0 ]; then
    echo "❌ Không thể kết nối MySQL! Vui lòng kiểm tra lại."
    exit 1
fi
echo "✅ Kết nối MySQL thành công"

echo ""
echo "[2/5] Backup database (nếu có)..."
BACKUP_DIR="backups"
mkdir -p "$BACKUP_DIR"
BACKUP_FILE="$BACKUP_DIR/backup_$(date +%Y%m%d_%H%M%S).sql"
echo "Đang backup vào: $BACKUP_FILE"
mysqldump -u root -p crypto_trading > "$BACKUP_FILE" 2>/dev/null
if [ $? -eq 0 ]; then
    echo "✅ Backup thành công: $BACKUP_FILE"
else
    echo "⚠️  Không thể backup (có thể database chưa tồn tại - tiếp tục...)"
fi

echo ""
echo "[3/5] Drop database cũ..."
mysql -u root -p -e "DROP DATABASE IF EXISTS crypto_trading;" 2>/dev/null
if [ $? -eq 0 ]; then
    echo "✅ Đã drop database cũ"
else
    echo "⚠️  Lỗi drop database (có thể chưa tồn tại - tiếp tục...)"
fi

echo ""
echo "[4/5] Tạo lại database với OLD SCHEMA..."
mysql -u root -p -e "CREATE DATABASE IF NOT EXISTS crypto_trading;" 2>/dev/null
if [ $? -ne 0 ]; then
    echo "❌ Lỗi tạo database!"
    exit 1
fi

echo "Đang chạy OLD SCHEMA script..."
mysql -u root -p crypto_trading < database/mysql/001_InitialSchema_MySQL_OLD_SCHEMA.sql 2>/dev/null
if [ $? -ne 0 ]; then
    echo "❌ Lỗi chạy OLD SCHEMA script!"
    exit 1
fi
echo "✅ Đã tạo OLD SCHEMA"

echo ""
echo "[5/5] Xóa EF Core migrations history..."
mysql -u root -p crypto_trading -e "DROP TABLE IF EXISTS __EFMigrationsHistory;" 2>/dev/null
echo "✅ Đã xóa migrations history"

echo ""
echo "========================================"
echo "✅ Hoàn thành! Database đã được khôi phục về OLD SCHEMA"
echo "========================================"
echo ""
echo "📋 Tiếp theo bạn cần làm:"
echo ""
echo "1. Tạo migration mới từ OLD SCHEMA:"
echo "   dotnet ef migrations add InitialCreate_OldSchema"
echo ""
echo "2. Apply migration:"
echo "   dotnet ef database update"
echo ""
echo "3. Hoặc chạy app (nếu đã enable auto-apply):"
echo "   dotnet run"
echo ""

