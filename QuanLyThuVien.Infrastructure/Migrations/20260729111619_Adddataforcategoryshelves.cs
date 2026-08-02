using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyThuVien.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Adddataforcategoryshelves : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. SEED DỮ LIỆU CHO BẢNG CATEGORIES (Thể loại)
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name", "Description", "CreatedAt", "IsActive" },
                values: new object[,]
                {
                    { 1, "Văn học", "Sách tiểu thuyết, truyện ngắn, thơ ca.", DateTime.UtcNow, true },
                    { 2, "Khoa học - Công nghệ", "Sách lập trình, công nghệ thông tin, vật lý, toán học.", DateTime.UtcNow, true },
                    { 3, "Kinh tế - Quản trị", "Sách khởi nghiệp, kinh doanh, tài chính, quản trị.", DateTime.UtcNow, true },
                    { 4, "Lịch sử - Địa lý", "Sách nghiên cứu lịch sử, địa lý các vùng miền.", DateTime.UtcNow, true },
                    { 5, "Kỹ năng sống", "Sách phát triển bản thân, tâm lý học.", DateTime.UtcNow, true }
                });

            // 2. SEED DỮ LIỆU CHO BẢNG SHELVES (Kệ sách)
            // Lưu ý: DbContext của bạn đặt tên bảng số ít hay số nhiều (Shelves hay Shelf) thì thay vào cột table cho đúng
            migrationBuilder.InsertData(
                table: "Shelves",
                columns: new[] { "Id", "Name", "CreatedAt", "IsActive" },
                values: new object[,]
                {
                    { 1, "Kệ A1 (Văn học)", DateTime.UtcNow, true },
                    { 2, "Kệ B2 (Công nghệ)", DateTime.UtcNow, true },
                    { 3, "Kệ C3 (Kinh tế)", DateTime.UtcNow, true }
                });

            // 3. SEED DỮ LIỆU CHO BẢNG SHELFTIERS (Tầng kệ)
            migrationBuilder.InsertData(
                table: "ShelfTiers",
                columns: new[] { "Id", "ShelfId", "TierName", "Capacity", "CreatedAt", "IsActive" },
                values: new object[,]
                {
                    // Kệ 1 (Kệ A1) có 2 tầng
                    { 1, 1, "Tầng 1", 10, DateTime.UtcNow, true },
                    { 2, 1, "Tầng 2", 10, DateTime.UtcNow, true },
                    
                    // Kệ 2 (Kệ B2) có 3 tầng
                    { 3, 2, "Tầng 1", 15, DateTime.UtcNow, true },
                    { 4, 2, "Tầng 2", 15, DateTime.UtcNow, true },
                    { 5, 2, "Tầng 3", 10, DateTime.UtcNow, true },

                    // Kệ 3 (Kệ C3) có 1 tầng
                    { 6, 3, "Tầng 1", 20, DateTime.UtcNow, true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Viết câu lệnh xóa ngược lại nếu rollback migration
            migrationBuilder.DeleteData(table: "ShelfTiers", keyColumn: "Id", keyValues: new object[] { 1, 2, 3, 4, 5, 6 });
            migrationBuilder.DeleteData(table: "Shelves", keyColumn: "Id", keyValues: new object[] { 1, 2, 3 });
            migrationBuilder.DeleteData(table: "Categories", keyColumn: "Id", keyValues: new object[] { 1, 2, 3, 4, 5 });
        }
    }
}
