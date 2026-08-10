using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Web.Data;
using QuanLyThuVien.Web.Entities;
using QuanLyThuVien.Web.Entities.Identity;
using QuanLyThuVien.Web.Enums;

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

        // 2. THÊM HÀM MỚI NÀY VÀO VỊ TRÍ ĐÓ:
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var cartItems = await _context.CartItems
                .Include(c => c.Book)
                .Include(c => c.User) // Nạp thông tin người dùng để lấy Username hiển thị
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            return View(cartItems);
        }

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

        // Tạo phiếu mượn (Pending) và làm sạch giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTicket(string? targetUsername)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var cartItems = await _context.CartItems
                .Include(c => c.Book)
                .Where(c => c.UserId == currentUser.Id)
                .ToListAsync();

            if (!cartItems.Any()) return Json(new { success = false, message = "Giỏ hàng trống." });

            ApplicationUser targetUser = currentUser;
            if ((User.IsInRole("Librarian") || User.IsInRole("Admin")) && !string.IsNullOrWhiteSpace(targetUsername))
            {
                var foundUser = await _userManager.FindByNameAsync(targetUsername.Trim());
                if (foundUser != null) targetUser = foundUser;
                else return Json(new { success = false, message = $"Không tìm thấy người dùng: {targetUsername}" });
            }

            // Lấy hạn trả sách từ Database (nếu không có thì mặc định là 7 ngày)
            var maxBorrowDaysSetting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == "MaxBorrowDays");
            int maxDays = maxBorrowDaysSetting != null ? int.Parse(maxBorrowDaysSetting.SettingValue) : 7;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Tạo phiếu mượn ở trạng thái Pending
                var ticket = new BorrowTickets
                {
                    UserId = targetUser.Id,
                    BorrowDate = DateTime.Now,
                    ExpectedReturnDate = DateTime.Now.AddDays(maxDays),
                    Status = BorrowStatus.Pending,
                    Note = (targetUser.Id != currentUser.Id) ? $"Tạo hộ bởi {currentUser.UserName}" : "Đặt online từ giỏ hàng"
                };

                _context.BorrowTickets.Add(ticket);
                await _context.SaveChangesAsync();

                // 2. QUAN TRỌNG: Duyệt giỏ hàng, nếu số lượng > 1 thì tách thành các dòng chi tiết riêng lẻ
                foreach (var cart in cartItems)
                {
                    for (int i = 0; i < cart.Quantity; i++)
                    {
                        _context.BorrowTicketDetails.Add(new BorrowTicketDetails
                        {
                            BorrowTicketId = ticket.Id,
                            BookId = cart.BookId,
                            BookCopyId = null // Ban đầu để trống, chờ Thủ thư ra quầy gán bản sao vật lý
                        });
                    }
                }
                await _context.SaveChangesAsync();

                // 3. Xóa sạch giỏ hàng sau khi đặt thành công
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Json(new { success = true, message = "Tạo phiếu mượn thành công! Vui lòng đến quầy thư viện để nhận sách." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // ĐÀO SÂU VÀO LỖI THẬT SỰ CỦA SQL SERVER ĐỂ BÁO LÊN MÀN HÌNH
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                return Json(new { success = false, message = "Lỗi hệ thống: " + errorMsg });
            }
        }
    }
}