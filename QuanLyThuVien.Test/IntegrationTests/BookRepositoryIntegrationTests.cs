using Microsoft.EntityFrameworkCore; // Chứa DbContextOptionsBuilder và UseInMemoryDatabase
using QuanLyThuVien.Domain.Entities;
using QuanLyThuVien.Infrastructure.Data;
using QuanLyThuVien.Infrastructure.Repositories;

namespace QuanLyThuVien.Tests.IntegrationTests
{
    public class BookRepositoryIntegrationTests
    {
        // Hàm tạo CSDL giả lập trong RAM (In-Memory) với tên ngẫu nhiên để tránh trùng lặp khi chạy nhiều test
        private DbContextOptions<ApplicationDbContext> CreateNewContextOptions()
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }
        //khi thực hiện xóa thì chỉ set IsActive = false, không xóa khỏi database
        [Fact]
        public async Task DeleteBook_NenThucHienXoaMem_KhongXoaCungKhoiDatabase()
        {
            // ARRANGE (Chuẩn bị dữ liệu và kết nối DB)
            var options = CreateNewContextOptions();

            // Khởi tạo DbContext và Repository thực tế (Không dùng Moq)
            using var context = new ApplicationDbContext(options);
            var repository = new BookRepository(context);

            // Thêm dữ liệu giả vào In-Memory Database
            var book = new Books
            {
                Id = 1,
                ISBN = "123456789",
                Title = "test",
                Author = "test",
                IsActive = true
            };

            await repository.AddAsync(book);
            await context.SaveChangesAsync(); // Lúc này sách đã được lưu vào DB trong RAM

            // ACT (Thực thi lệnh xóa)
            repository.Delete(book);
            await context.SaveChangesAsync(); // Kích hoạt hàm override SaveChangesAsync trong DbContext chỉ cập nhật IsActive = false mà không xóa khỏi DB

            // ASSERT (Kiểm chứng DB)
            // Tìm lại cuốn sách vừa xóa trực tiếp từ Context
            var deletedBook = await context.Books.FindAsync(1);

            Assert.NotNull(deletedBook); // pass : Cuốn sách vẫn tồn tại trong DB
            Assert.False(deletedBook.IsActive); // pass : Cuốn sách đã được đánh dấu là không còn hoạt động (IsActive = false)
            Assert.NotNull(deletedBook.UpdatedAt); // pass : UpdatedAt đã được cập nhật
        }
    }
}