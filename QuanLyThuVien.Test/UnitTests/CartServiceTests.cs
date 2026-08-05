using Moq;
using QuanLyThuVien.Application.Interfaces;
using QuanLyThuVien.Application.Interfaces.IRepositories;
using QuanLyThuVien.Application.Services;
using QuanLyThuVien.Domain.Entities;
using QuanLyThuVien.Domain.Enums;

namespace QuanLyThuVien.Tests.UnitTests
{
    public class CartServiceTests
    {
        private readonly Mock<ICartRepository> _cartRepoMock;
        private readonly Mock<IBookRepository> _bookRepoMock;
        private readonly Mock<IApplicationDbContext> _contextMock;
        private readonly CartService _cartService;

        public CartServiceTests()
        {
            // Khởi tạo các đối tượng giả (Mock objects)
            _cartRepoMock = new Mock<ICartRepository>();
            _bookRepoMock = new Mock<IBookRepository>();
            _contextMock = new Mock<IApplicationDbContext>();

            // Truyền các đối tượng giả vào CartService
            _cartService = new CartService(
                _cartRepoMock.Object,
                _bookRepoMock.Object,
                _contextMock.Object);
        }


        // TEST CASE 1: THẤT BẠI - sách không tồn tại hoặc hỏng mất
        [Fact]
        public async Task AddToCartAsync_NenNemRaNgoaiLe_KhiSachKhongKhaDung()
        {
            // Arrange
            int readerId = 1;
            int bookId = 100;

            // Giả lập: Tìm thấy sách nhưng trạng thái là đang được mượn (Borrowed)
            var unavailableBook = new Books
            {
                Id = bookId,
                ISBN = "123",
                Title = "Test",
                Author = "Test",
                Status = BookStatus.Borrowed
            };

            _bookRepoMock.Setup(repo => repo.GetByIdAsync(bookId))
                         .ReturnsAsync(unavailableBook);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _cartService.AddToCartAsync(readerId, bookId));

            Assert.Equal("Sách không tồn tại hoặc đã được mượn.", exception.Message);

            // Đảm bảo không gọi lệnh lưu CSDL
            _cartRepoMock.Verify(repo => repo.AddAsync(It.IsAny<CartItems>()), Times.Never);
        }

        // TEST CASE 2: THẤT BẠI - sách đã có trong giỏ
        [Fact]
        public async Task AddToCartAsync_NenNemRaNgoaiLe_KhiSachDaCoTrongGio()
        {
            // Arrange
            int readerId = 1;
            int bookId = 100;

            _bookRepoMock.Setup(repo => repo.GetByIdAsync(bookId))
                .ReturnsAsync(new Books { Id = bookId, ISBN = "123", Title = "Test", Author = "Test", Status = BookStatus.Available });

            // Giả lập: sách đã có trong giỏ
            _cartRepoMock.Setup(repo => repo.IsBookInCartAsync(readerId, bookId))
                .ReturnsAsync(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _cartService.AddToCartAsync(readerId, bookId));

            Assert.Equal("Sách này đã có trong giỏ hàng của bạn.", exception.Message);
        }

        // TEST CASE 3: THẤT BẠI - giỏ hàng đã đầy 2 quyển
        [Fact]
        public async Task AddToCartAsync_NenNemRaNgoaiLe_KhiGioHangDaDay()
        {
            // Arrange
            int readerId = 1;
            int bookId = 100;

            _bookRepoMock.Setup(repo => repo.GetByIdAsync(bookId))
                .ReturnsAsync(new Books { Id = bookId, ISBN = "123", Title = "Test", Author = "Test", Status = BookStatus.Available });

            _cartRepoMock.Setup(repo => repo.IsBookInCartAsync(readerId, bookId))
                .ReturnsAsync(false);

            // Giả lập: Giỏ hàng hiện tại ĐÃ CÓ 2 CUỐN
            _cartRepoMock.Setup(repo => repo.GetCartCountAsync(readerId))
                .ReturnsAsync(2);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _cartService.AddToCartAsync(readerId, bookId));

            Assert.Equal("Giỏ hàng của bạn đã đầy (Tối đa 2 quyển).", exception.Message);
            _cartRepoMock.Verify(repo => repo.AddAsync(It.IsAny<CartItems>()), Times.Never);
        }

        // TEST CASE 4: THÀNH CÔNG - thêm và lưu vào database thành công
        [Fact]
        public async Task AddToCartAsync_NenTraVeTrue_KhiThemThanhCong()
        {
            // Arrange
            int readerId = 1;
            int bookId = 100;

            // 1. Sách tồn tại và khả dụng
            _bookRepoMock.Setup(repo => repo.GetByIdAsync(bookId))
                .ReturnsAsync(new Books { Id = bookId, ISBN = "123", Title = "Test", Author = "Test", Status = BookStatus.Available });

            // 2. Sách CHƯA có trong giỏ
            _cartRepoMock.Setup(repo => repo.IsBookInCartAsync(readerId, bookId))
                .ReturnsAsync(false);

            // 3. Giỏ hàng CHƯA ĐẦY (mới có 1 cuốn)
            _cartRepoMock.Setup(repo => repo.GetCartCountAsync(readerId))
                .ReturnsAsync(1);

            // 4. Giả lập hàm SaveChangesAsync thực thi thành công (trả về 1 dòng bị ảnh hưởng)
            _contextMock.Setup(ctx => ctx.SaveChangesAsync(default))
                .ReturnsAsync(1);

            // Act
            bool result = await _cartService.AddToCartAsync(readerId, bookId);

            // Assert
            Assert.True(result); // Phải trả về true

            // Đảm bảo hàm AddAsync đã được gọi ĐÚNG 1 LẦN với đúng dữ liệu
            _cartRepoMock.Verify(repo => repo.AddAsync(It.Is<CartItems>(c =>
                c.ReaderId == readerId && c.BookId == bookId && c.IsActive == true
            )), Times.Once);

            // Đảm bảo hàm SaveChangesAsync được gọi ĐÚNG 1 LẦN để chốt DB
            _contextMock.Verify(ctx => ctx.SaveChangesAsync(default), Times.Once);
        }
    }
}