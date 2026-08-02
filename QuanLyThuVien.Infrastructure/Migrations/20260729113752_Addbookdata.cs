using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyThuVien.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addbookdata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Books",
                columns: new[] { "ISBN", "Title", "Author", "Publisher", "PublishYear", "CoverImage", "Status", "CategoryId", "ShelfTierId", "IsActive", "CreatedAt" },
                values: new object[,]
                {
                    {  "ISBN-0001", "Lập trình C# cơ bản", "Nguyễn Văn A", "NXB Giáo dục", 2020, "/images/books/sample1.png", "Available", 1, 1, true, DateTime.UtcNow },
                    {  "ISBN-0002", "ASP.NET Core nâng cao", "Trần Văn B", "NXB Công nghệ", 2021, "/images/books/sample2.png", "Available", 2, 3, true, DateTime.UtcNow },
                    { "ISBN-0003", "Cơ sở dữ liệu SQL", "Lê Văn C", "NXB Khoa học", 2019, "/images/books/sample3.png", "Available", 3, 6, true, DateTime.UtcNow },
                    {  "ISBN-0004", "Thiết kế phần mềm", "Phạm Văn D", "NXB Công nghệ", 2018, "/images/books/sample4.png", "Available", 4, 1, true, DateTime.UtcNow },
                    {  "ISBN-0005", "Machine Learning nhập môn", "Nguyễn Văn E", "NXB Khoa học", 2022, "/images/books/sample5.png", "Available", 5, 2, true, DateTime.UtcNow }
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
