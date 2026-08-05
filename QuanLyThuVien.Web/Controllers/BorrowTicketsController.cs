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

        //lấy danh sách phiếu và phân trang
        public async Task<IActionResult> Index(string? SearchTicketId, string? SearchBorrower, DateTime? fromDate, DateTime? toDate, BorrowStatus? status, string sortOrder = "date_desc", int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var query = _context.BorrowTickets
                .Include(t => t.Reader)
                    .ThenInclude(r => r!.ApplicationUser)
                .AsQueryable();

            // phân quyền hiển thị
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
            //admin và thủ thư thì xem tất cả phiếu

            // Cập nhật trạng thái Overdue (Quá hạn > 7 ngày) tự động khi load danh sách
            var pendingOrBorrowingTickets = await query
                .Where(t => t.Status == BorrowStatus.Borrowing)
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

            // lọc tìm kiếm theo mã phiếu
            if (!string.IsNullOrEmpty(SearchTicketId))
                query = query.Where(t => t.Id.ToString().Contains(SearchTicketId));

            // CHỈ ADMIN VÀ THỦ THƯ MỚI ĐƯỢC TÌM THEO TÊN NGƯỜI MƯỢN
            if ((User.IsInRole("Admin") || User.IsInRole("Librarian")) && !string.IsNullOrEmpty(SearchBorrower))
            {
                query = query.Where(t => t.Reader!.ApplicationUser!.UserName!.Contains(SearchBorrower));
            }

            if (fromDate.HasValue)
                query = query.Where(t => t.BorrowDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(t => t.BorrowDate <= toDate.Value);
            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);

            // sắp xếp
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
                SearchTicketId = SearchTicketId,
                SearchBorrower = SearchBorrower,
                FromDate = fromDate,
                ToDate = toDate,
                Status = status
            };
            // truyền dữ liệu thể loại và kệ cho chỉnh sửa sách trong danh sách phiếu mượn
            if (User.IsInRole("Admin"))
            {
                var categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                var shelves = await _context.Shelves.Where(s => s.IsActive).ToListAsync();

                // Lưu ý: Cần gõ đầy đủ Microsoft.AspNetCore.Mvc.Rendering.SelectList nếu trên cùng file chưa using
                ViewBag.Categories = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(categories, "Id", "Name");
                ViewBag.Shelves = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(shelves, "Id", "Name");
            }

            return View(model);
        }

        //lấy thông tin chi tiết cho phiếu mượn
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

            //reader không xem phiếu khác
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
                borrowerName = ticket.Reader?.ApplicationUser?.UserName,
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


        // cập nhật borrow status
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

            // nếu cập nhật là returned thì actualreturndate là thời gian lúc cập nhật
            if (model.NewStatus == BorrowStatus.Returned && ticket.Status != BorrowStatus.Returned)
            {
                ticket.ActualReturnDate = DateTime.UtcNow;
            }

            // Cập nhật trạng thái mới cho phiếu
            ticket.Status = model.NewStatus;

            // 
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


        //kiểm tra phiếu mượn trước khi xác nhận
        [HttpPost]
        [Authorize(Roles = "Admin, Librarian")]
        public async Task<IActionResult> CheckTicket(int ticketId)
        {
            // 1. Lấy thông tin phiếu mượn kèm theo chi tiết và sách
            var ticket = await _context.BorrowTickets
                .Include(t => t.TicketDetails)
                    .ThenInclude(d => d.Book)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phiếu mượn." });
            }

            // 2. Kiểm tra xem có bất kỳ cuốn sách nào khác trạng thái Available không
            bool hasUnavailableBook = ticket.TicketDetails
                .Any(d => d.Book != null && d.Book.Status != BookStatus.Available);

            if (hasUnavailableBook)
            {
                // 3. Nếu có sách không khả dụng -> Hủy phiếu
                ticket.Status = BorrowStatus.Canceled;
                ticket.Note = string.IsNullOrEmpty(ticket.Note)
                    ? "Hệ thống tự động hủy vì có sách không khả dụng."
                    : ticket.Note + "\n(Hệ thống tự động hủy vì có sách không khả dụng).";

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    isCanceled = true,
                    message = "Phiếu đã tự động bị HỦY do có sách trong phiếu không khả dụng (đã bị mượn hoặc hỏng)."
                });
            }

            // 4. Nếu tất cả sách đều Available
            return Json(new
            {
                success = true,
                isCanceled = false,
                message = "Tất cả sách đều khả dụng. Phiếu hợp lệ!"
            });
        }
    }
}
