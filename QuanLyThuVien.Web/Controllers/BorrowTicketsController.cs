using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Domain.Common;
using QuanLyThuVien.Domain.Entities;
using QuanLyThuVien.Domain.Entities.Identity;
using QuanLyThuVien.Domain.Enums;
using QuanLyThuVien.Infrastructure.Data;
using QuanLyThuVien.Web.Models;

namespace QuanLyThuVien.Web.Controllers
{
    [Authorize]
    public class BorrowTicketsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BorrowTicketsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index(string? ticketId, string? borrower, DateTime? fromDate, DateTime? toDate, BorrowStatus? status, string sortOrder = "date_desc", int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var query = _context.BorrowTickets
                .Include(t => t.Reader)
                    .ThenInclude(r => r!.ApplicationUser)
                .AsQueryable();

            // --- PHÂN QUYỀN DỮ LIỆU THEO ROLE ---
            if (User.IsInRole("Reader"))
            {
                // Khách (Reader): Chỉ được xem phiếu của chính mình
                var reader = await _context.Readers.FirstOrDefaultAsync(r => r.ApplicationUserId == user.Id);
                if (reader == null)
                {
                    // Nếu khách chưa có hồ sơ reader, trả về danh sách trống
                    var emptyModel = new BorrowTicketIndexViewModel
                    {
                        Tickets = new PagedResult<BorrowTickets>(new List<BorrowTickets>(), 0, page, 9)
                    };
                    return View(emptyModel);
                }
                query = query.Where(t => t.ReaderId == reader.Id);
            }
            // Nếu là Admin hoặc Librarian: Giữ nguyên query để xem tất cả

            // Cập nhật trạng thái Overdue (Quá hạn > 7 ngày) tự động khi load danh sách
            var pendingOrBorrowingTickets = await query
                .Where(t => t.Status == BorrowStatus.Borrowing || t.Status == BorrowStatus.Pending)
                .ToListAsync();

            bool hasChanges = false;
            foreach (var ticket in pendingOrBorrowingTickets)
            {
                if ((DateTime.UtcNow - ticket.BorrowDate).TotalDays > 7)
                {
                    ticket.Status = BorrowStatus.Overdue;
                    hasChanges = true;
                }
            }
            if (hasChanges) await _context.SaveChangesAsync();

            // --- ÁP DỤNG BỘ LỌC TÌM KIẾM ---
            if (!string.IsNullOrEmpty(ticketId))
                query = query.Where(t => t.Id.ToString().Contains(ticketId));

            // CHỈ ADMIN VÀ THỦ THƯ MỚI ĐƯỢC TÌM THEO TÊN NGƯỜI MƯỢN
            if ((User.IsInRole("Admin") || User.IsInRole("Librarian")) && !string.IsNullOrEmpty(borrower))
            {
                query = query.Where(t => t.Reader!.ApplicationUser!.FullName!.Contains(borrower));
            }

            if (fromDate.HasValue)
                query = query.Where(t => t.BorrowDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(t => t.BorrowDate <= toDate.Value);
            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);

            // --- SẮP XẾP ---
            ViewBag.CurrentSort = sortOrder;
            switch (sortOrder)
            {
                case "date_asc":
                    query = query.OrderBy(t => t.BorrowDate);
                    break;
                case "date_desc":
                default:
                    query = query.OrderByDescending(t => t.BorrowDate);
                    break;
            }

            // Phân trang (9 item / trang cho lưới 3x3)
            int pageSize = 9;
            var totalItems = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var model = new BorrowTicketIndexViewModel
            {
                Tickets = new PagedResult<BorrowTickets>(items, totalItems, page, pageSize),
                SearchTicketId = ticketId,
                SearchBorrower = borrower,
                FromDate = fromDate,
                ToDate = toDate,
                Status = status
            };

            return View(model);
        }
        // ==========================================
        // 2. LẤY CHI TIẾT PHIẾU LÊN MODAL
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetTicketDetails(int id)
        {
            var ticket = await _context.BorrowTickets
                .Include(t => t.Reader)
                    .ThenInclude(r => r!.ApplicationUser)
                .Include(t => t.TicketDetails)
                    .ThenInclude(d => d.Book)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound();

            // Bảo mật bổ sung: Nếu là Khách, chặn không cho xem chi tiết phiếu của người khác
            if (User.IsInRole("Reader"))
            {
                var user = await _userManager.GetUserAsync(User);
                var reader = await _context.Readers.FirstOrDefaultAsync(r => r.ApplicationUserId == user!.Id);
                if (reader == null || ticket.ReaderId != reader.Id)
                {
                    return Forbid();
                }
            }

            var data = new
            {
                id = ticket.Id,
                borrowerName = ticket.Reader?.ApplicationUser?.FullName,
                borrowDate = ticket.BorrowDate.ToString("dd/MM/yyyy"),
                dueDate = ticket.ExpectedReturnDate.ToString("dd/MM/yyyy"),
                returnDate = ticket.ActualReturnDate?.ToString("dd/MM/yyyy") ?? "Chưa trả",
                status = (int)ticket.Status,
                statusName = ticket.Status.ToString(), // Phục vụ hiển thị text cho khách
                note = ticket.Note,
                books = ticket.TicketDetails.Select(d => new { bookId = d.BookId, name = d.Book!.Title }).ToList()
            };

            return Json(data);
        }

        // ==========================================
        // 3. CẬP NHẬT TRẠNG THÁI (CHỈ ADMIN VÀ THỦ THƯ)
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "Admin, Librarian")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus([FromForm] UpdateTicketStatusModel model)
        {
            var ticket = await _context.BorrowTickets
                .Include(t => t.TicketDetails)
                    .ThenInclude(d => d.Book)
                .FirstOrDefaultAsync(t => t.Id == model.TicketId);

            if (ticket == null) return Json(new { success = false, message = "Không tìm thấy phiếu." });

            ticket.Note = model.Note;

            // Xử lý gán ngày trả thực tế nếu chuyển sang Returned
            if (model.NewStatus == BorrowStatus.Returned && ticket.Status != BorrowStatus.Returned)
            {
                ticket.ActualReturnDate = DateTime.UtcNow;
            }

            // Cập nhật trạng thái mới cho phiếu
            ticket.Status = model.NewStatus;

            // ĐỒNG BỘ TRẠNG THÁI SÁCH (BookStatus) CHO TẤT CẢ SÁCH TRONG PHIẾU
            foreach (var detail in ticket.TicketDetails)
            {
                if (detail.Book != null)
                {
                    if (model.NewStatus == BorrowStatus.Borrowing || model.NewStatus == BorrowStatus.Overdue)
                    {
                        // Nếu phiếu là Borrowing hoặc Overdue -> Sách ở trạng thái Đang mượn
                        detail.Book.Status = BookStatus.Borrowed;
                    }
                    else if (model.NewStatus == BorrowStatus.Pending ||
                             model.NewStatus == BorrowStatus.Returned ||
                             model.NewStatus == BorrowStatus.Accepted ||
                             model.NewStatus == BorrowStatus.Canceled)
                    {
                        // Nếu phiếu là Pending, Returned, Accepted, hoặc Canceled -> Sách ở trạng thái Sẵn sàng
                        detail.Book.Status = BookStatus.Available;
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
