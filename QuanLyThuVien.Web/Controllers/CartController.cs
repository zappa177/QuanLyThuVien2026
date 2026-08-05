using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Domain.Entities;
using QuanLyThuVien.Domain.Entities.Identity;
using QuanLyThuVien.Domain.Enums;
using QuanLyThuVien.Infrastructure.Data;

namespace QuanLyThuVien.Web.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // lấy thông tin giỏ hàng của user hiện tại
        [HttpGet]
        public async Task<IActionResult> GetCartData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var reader = await GetOrCreateReaderAsync(user.Id.ToString());

            var items = await _context.CartItems
                .Include(c => c.Book)
                .Where(c => c.ReaderId == reader.Id)
                .Select(c => new
                {
                    id = c.Id,
                    title = c.Book!.Title,
                    coverImage = string.IsNullOrEmpty(c.Book.CoverImage) ? "/images/no-cover.png" : c.Book.CoverImage
                }).ToListAsync();

            bool isLibrarian = User.IsInRole("Librarian") || User.IsInRole("Admin");

            return Json(new
            {
                success = true,
                items = items,
                username = user.UserName,
                isLibrarian = isLibrarian
            });
        }

        //// 2. THÊM SÁCH VÀO GIỎ
        //[HttpPost]
        //[ValidateAntiForgeryToken] // <-- KHIÊN BẢO MẬT ĐÃ ĐƯỢC BẬT LẠI
        //public async Task<IActionResult> AddToCart(int bookId)
        //{
        //    var user = await _userManager.GetUserAsync(User);
        //    if (user == null) return Unauthorized();

        //    // 1. KIỂM TRA TÌNH TRẠNG SÁCH NGAY TỪ ĐẦU
        //    var book = await _context.Books.FindAsync(bookId);
        //    if (book == null)
        //    {
        //        return Json(new { success = false, message = "Không tìm thấy cuốn sách này!" });
        //    }

        //    if (book.Status != QuanLyThuVien.Domain.Enums.BookStatus.Available)
        //    {
        //        return Json(new { success = false, message = "Sách này hiện không có sẵn để mượn (Đang được mượn hoặc thất lạc)." });
        //    }

        //    // 2. KHỞI TẠO HOẶC LẤY THÔNG TIN ĐỘC GIẢ
        //    var reader = await GetOrCreateReaderAsync(user.Id.ToString());

        //    // 3. KIỂM TRA GIỚI HẠN GIỎ HÀNG
        //    var cartCount = await _context.CartItems.CountAsync(c => c.ReaderId == reader.Id);
        //    if (cartCount >= 2)
        //    {
        //        return Json(new { success = false, message = "Giỏ hàng đã đầy. Tối đa chỉ được mượn 2 cuốn sách." });
        //    }

        //    // 4. KIỂM TRA TRÙNG LẶP TRONG GIỎ HÀNG
        //    var isExist = await _context.CartItems.AnyAsync(c => c.ReaderId == reader.Id && c.BookId == bookId);
        //    if (isExist)
        //    {
        //        return Json(new { success = false, message = "Cuốn sách này đã có trong giỏ hàng." });
        //    }

        //    // 5. THÊM VÀO GIỎ VÀ LƯU LẠI
        //    _context.CartItems.Add(new CartItems { ReaderId = reader.Id, BookId = bookId });
        //    await _context.SaveChangesAsync();

        //    return Json(new { success = true, newCount = cartCount + 1, message = "Đã thêm sách vào giỏ hàng thành công." });
        //}




        // XÓA SẢN PHẨM KHỎI GIỎ 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int id)
        {
            var item = await _context.CartItems.FindAsync(id);
            if (item != null)
            {

                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Không tìm thấy dữ liệu giỏ hàng cần xóa." });
        }

        // tạo phiếu và xóa sạch giỏ hàng của người dùng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTicket(string? targetUsername)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var currentReader = await GetOrCreateReaderAsync(currentUser.Id.ToString());

            var cartItems = await _context.CartItems.Where(c => c.ReaderId == currentReader.Id).ToListAsync();
            if (!cartItems.Any()) return Json(new { success = false, message = "Giỏ hàng trống." });

            Readers targetReader = currentReader;

            if ((User.IsInRole("Librarian") || User.IsInRole("Admin")) && !string.IsNullOrWhiteSpace(targetUsername))
            {
                var targetUser = await _userManager.FindByNameAsync(targetUsername.Trim());
                if (targetUser != null)
                {
                    targetReader = await GetOrCreateReaderAsync(targetUser.Id.ToString());
                }
                else
                {
                    return Json(new { success = false, message = $"Không tìm thấy người dùng có tên đăng nhập: {targetUsername}" });
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var ticket = new BorrowTickets
                {
                    ReaderId = targetReader.Id,
                    BorrowDate = DateTime.UtcNow,
                    ExpectedReturnDate = DateTime.UtcNow.AddDays(7),
                    Status = BorrowStatus.Pending,
                    Note = (targetReader.Id != currentReader.Id) ? $"Được tạo hộ bởi {currentUser.UserName}" : "Tự động tạo từ giỏ hàng"
                };
                _context.BorrowTickets.Add(ticket);
                await _context.SaveChangesAsync();

                foreach (var cart in cartItems)
                {
                    _context.BorrowTicketDetails.Add(new BorrowTicketDetails
                    {
                        BorrowTicketId = ticket.Id,
                        BookId = cart.BookId
                    });
                }
                await _context.SaveChangesAsync();

                // Xóa sạch giỏ hàng 
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Json(new { success = true, message = "Tạo phiếu mượn thành công và đã làm sạch giỏ hàng!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        //
        private async Task<Readers> GetOrCreateReaderAsync(string userId)
        {
            Guid userGuid = Guid.Parse(userId);
            var reader = await _context.Readers.FirstOrDefaultAsync(r => r.ApplicationUserId == userGuid);

            if (reader == null) //thủ thư có thể thêm sách vào giỏ hàng và tạo phiếu , nếu là thủ thư thì tạo studentcode để thông tin trên phiếu
            {
                reader = new Readers
                {
                    ApplicationUserId = userGuid,
                    StudentCode = "RD-" + DateTime.Now.ToString("ddMMyyHHmm")
                };
                _context.Readers.Add(reader);
                await _context.SaveChangesAsync();
            }
            return reader;
        }
    }
}