using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing; // <-- DÙNG THƯ VIỆN NÀY ĐỂ ĐỌC ANONYMOUS TYPE
using Microsoft.EntityFrameworkCore;
using Moq;
using QuanLyThuVien.Web.Controllers;
using QuanLyThuVien.Web.Data;
using QuanLyThuVien.Web.Entities;
using QuanLyThuVien.Web.Entities.Identity;
using System.Security.Claims;

namespace QuanLyThuVien.Test.Controllers
{
    public class HomeControllerTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private Mock<UserManager<ApplicationUser>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        // =========================================================================
        // [UTC-CART-01]
        // =========================================================================
        [Fact]
        public async Task AddToCartDb_UTC_CART_01_UserNotLoggedIn_ReturnsFalse()
        {
            var context = GetInMemoryDbContext();
            var mockUserManager = GetMockUserManager();

            mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                           .ReturnsAsync((ApplicationUser)null!);

            var controller = new HomeController(context, mockUserManager.Object);

            var result = await controller.AddToCartDb(bookId: 1, quantity: 1) as JsonResult;

            // CÁCH MỚI: Dùng RouteValueDictionary để đọc Anonymous Object
            var dict = new RouteValueDictionary(result!.Value);

            bool isSuccess = Convert.ToBoolean(dict["success"]);
            string message = dict["message"]?.ToString() ?? "";

            Assert.False(isSuccess);
            Assert.Equal("Vui lòng đăng nhập để thêm sách vào giỏ hàng!", message);
        }
        // =========================================================================
        // [UTC-CART-02]
        // =========================================================================
        [Fact]
        public async Task AddToCartDb_UTC_CART_02_BookNotFoundOrInactive_ReturnsFalse()
        {
            var context = GetInMemoryDbContext();

            // BƯỚC 1: Thêm sách vào (DbContext sẽ tự động ép IsActive = true do trạng thái Added)
            var book = new Books { Id = 99, Title = "Sách Khóa", Author = "TG", CategoryId = 1 };
            context.Books.Add(book);
            await context.SaveChangesAsync();

            // BƯỚC 2: Cập nhật lại sách thành IsActive = false
            // (Trạng thái Modified không bị DbContext ghi đè IsActive)
            book.IsActive = false;
            context.Books.Update(book);
            await context.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            var validUser = new ApplicationUser { Id = Guid.NewGuid() };
            mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(validUser);

            var controller = new HomeController(context, mockUserManager.Object);

            // Act
            var result = await controller.AddToCartDb(bookId: 99, quantity: 1) as JsonResult;

            // Đọc dữ liệu
            var dict = new Microsoft.AspNetCore.Routing.RouteValueDictionary(result!.Value);
            bool isSuccess = Convert.ToBoolean(dict["success"]);
            string message = dict["message"]?.ToString() ?? "";

            // Assert
            Assert.False(isSuccess);
            Assert.Equal("Sách không tồn tại hoặc đã ngừng hoạt động.", message);
        }

        // =========================================================================
        // [UTC-CART-03]
        // =========================================================================
        [Fact]
        public async Task AddToCartDb_UTC_CART_03_NewBook_AddsToCartAndReturnsTrue()
        {
            var context = GetInMemoryDbContext();
            var userId = Guid.NewGuid();

            context.Books.Add(new Books { Id = 1, Title = "Sách Mới", Author = "TG", CategoryId = 1, IsActive = true });
            await context.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            var validUser = new ApplicationUser { Id = userId };
            mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(validUser);

            var controller = new HomeController(context, mockUserManager.Object);

            var result = await controller.AddToCartDb(bookId: 1, quantity: 2) as JsonResult;

            // Đọc dữ liệu
            var dict = new RouteValueDictionary(result!.Value);
            bool isSuccess = Convert.ToBoolean(dict["success"]);
            int newCount = Convert.ToInt32(dict["newCount"]);

            Assert.True(isSuccess);
            Assert.Equal(2, newCount);

            var cartItemInDb = await context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.BookId == 1);
            Assert.NotNull(cartItemInDb);
            Assert.Equal(2, cartItemInDb.Quantity);
        }

        // =========================================================================
        // [UTC-CART-04]
        // =========================================================================
        [Fact]
        public async Task AddToCartDb_UTC_CART_04_ExistingBook_IncreasesQuantity()
        {
            var context = GetInMemoryDbContext();
            var userId = Guid.NewGuid();

            context.Books.Add(new Books { Id = 1, Title = "Sách Cũ", Author = "TG", CategoryId = 1, IsActive = true });
            context.CartItems.Add(new CartItems { UserId = userId, BookId = 1, Quantity = 1 });
            await context.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            var validUser = new ApplicationUser { Id = userId };
            mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(validUser);

            var controller = new HomeController(context, mockUserManager.Object);

            var result = await controller.AddToCartDb(bookId: 1, quantity: 2) as JsonResult;

            // Đọc dữ liệu
            var dict = new RouteValueDictionary(result!.Value);
            bool isSuccess = Convert.ToBoolean(dict["success"]);
            int newCount = Convert.ToInt32(dict["newCount"]);

            Assert.True(isSuccess);
            Assert.Equal(3, newCount);

            var cartItemsCount = await context.CartItems.CountAsync(c => c.UserId == userId);
            var cartItemInDb = await context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.BookId == 1);

            Assert.Equal(1, cartItemsCount);
            Assert.Equal(3, cartItemInDb!.Quantity);
        }

    }
}