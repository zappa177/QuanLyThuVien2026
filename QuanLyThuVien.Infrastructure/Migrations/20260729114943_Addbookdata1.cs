using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyThuVien.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addbookdata1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Books",
                columns: new[] { "ISBN", "Title", "Author", "Publisher", "PublishYear", "CoverImage", "Status", "CategoryId", "ShelfTierId", "IsActive", "CreatedAt" },
                values: new object[,]
                {
                    { "ISBN-0006", "Phân tích và thiết kế thuật toán", "Hoàng Văn K", "NXB Khoa học", 2021, "/images/books/sample6.png", "Available", 2, 3, true, DateTime.UtcNow },
{ "ISBN-0007", "Kinh tế học vi mô", "Vũ Thị M", "NXB Kinh tế", 2020, "/images/books/sample7.png", "Available", 3, 6, true, DateTime.UtcNow },
{ "ISBN-0008", "Đại Việt sử ký toàn thư", "Ngô Sĩ Liên", "NXB Giáo dục", 2017, "/images/books/sample8.png", "Available", 4, 1, true, DateTime.UtcNow },
{ "ISBN-0009", "Đắc nhân tâm", "Dale Carnegie", "NXB Tổng hợp", 2019, "/images/books/sample9.png", "Available", 5, 2, true, DateTime.UtcNow },
{ "ISBN-0010", "Nhà giả kim", "Paulo Coelho", "NXB Văn học", 2018, "/images/books/sample10.png", "Available", 1, 2, true, DateTime.UtcNow }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            for (int i = 1; i <= 5; i++)
            {
                migrationBuilder.DeleteData(
                    table: "Books",
                    keyColumn: "Id",
                    keyValue: i);
            }
        }
    }
}
