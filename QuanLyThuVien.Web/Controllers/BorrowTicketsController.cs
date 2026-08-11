using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Web.Common;
using QuanLyThuVien.Web.Data;
using QuanLyThuVien.Web.Entities;
using QuanLyThuVien.Web.Entities.Identity;
using QuanLyThuVien.Web.Enums;
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

        // lấy danh sách phiếu và hiển thị
        public async Task<IActionResult> Index(string? SearchTicketId, string? SearchBorrower, DateTime? fromDate, DateTime? toDate, BorrowStatus? status, string sortOrder = "date_desc", int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var query = _context.BorrowTickets.Include(t => t.User).AsQueryable();

            if (User.IsInRole("Reader"))
            {
                query = query.Where(t => t.UserId == user.Id);
            }

            var pendingOrBorrowingTickets = await query.Where(t => t.Status == BorrowStatus.Borrowing).ToListAsync();
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

            if (!string.IsNullOrEmpty(SearchTicketId))
                query = query.Where(t => t.Id.ToString().Contains(SearchTicketId));

            if ((User.IsInRole("Admin") || User.IsInRole("Librarian")) && !string.IsNullOrEmpty(SearchBorrower))
            {
                query = query.Where(t => t.User!.UserName!.Contains(SearchBorrower)
                                      || t.User!.FullName!.Contains(SearchBorrower)
                                      || t.User!.Position!.Contains(SearchBorrower)
                                      || t.User!.UserCode!.Contains(SearchBorrower));
            }

            if (fromDate.HasValue) query = query.Where(t => t.BorrowDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(t => t.BorrowDate <= toDate.Value);
            if (status.HasValue) query = query.Where(t => t.Status == status.Value);

            ViewBag.CurrentSort = sortOrder;
            query = sortOrder == "date_asc" ? query.OrderBy(t => t.BorrowDate) : query.OrderByDescending(t => t.BorrowDate);

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

            return View(model);
        }

        // chi tiết phiếu mượn
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _context.BorrowTickets
                .Include(t => t.User)
                .Include(t => t.TicketDetails).ThenInclude(d => d.Book)
                .Include(t => t.TicketDetails).ThenInclude(d => d.BookCopy)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound("Không tìm thấy phiếu mượn.");

            if (User.IsInRole("Reader"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || ticket.UserId != user.Id) return Forbid();
            }

            ViewBag.IsStaff = User.IsInRole("Admin") || User.IsInRole("Librarian");
            return View(ticket);
        }


        // Kiểm tra tồn kho trước khi duyệt phiếu (Pending -> Accepted)

        [HttpPost]
        [Authorize(Roles = "Admin, Librarian")]
        public async Task<IActionResult> CheckInventory(int ticketId)
        {
            var ticket = await _context.BorrowTickets.Include(t => t.TicketDetails).ThenInclude(d => d.Book)
                                       .FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null) return Json(new { success = false, message = "Không tìm thấy phiếu." });

            var requiredBooks = ticket.TicketDetails.GroupBy(d => d.BookId)
                                      .Select(g => new { BookId = g.Key, Title = g.First().Book?.Title, Qty = g.Count() }).ToList();

            foreach (var req in requiredBooks)
            {
                var available = await _context.BookCopies.CountAsync(c => c.BookId == req.BookId && c.Status == BookCopyStatus.Available && c.IsActive && !c.IsReferenceOnly);
                if (available < req.Qty)
                    return Json(new { success = false, message = $"Lỗi tồn kho: Tựa sách '{req.Title}' cần {req.Qty} cuốn, nhưng kho chỉ còn {available}. Vui lòng Giảm/Xóa sách khỏi phiếu để tiếp tục!" });
            }

            return Json(new { success = true, message = "Tồn kho hợp lệ! Bạn có thể duyệt phiếu." });
        }


        //duyệt phiếu chuyển trạng thái từ Pending -> Accepted (Admin, Librarian)

        [HttpPost]
        [Authorize(Roles = "Admin, Librarian")]
        public async Task<IActionResult> ApproveTicket(int ticketId)
        {
            var ticket = await _context.BorrowTickets.FindAsync(ticketId);
            if (ticket == null || ticket.Status != BorrowStatus.Pending) return Json(new { success = false });

            ticket.Status = BorrowStatus.Accepted;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        //xóa sách từ phiếu
        [HttpPost]
        [Authorize(Roles = "Admin, Librarian")]
        public async Task<IActionResult> RemoveBookFromTicket(int ticketId, int bookId, bool removeAll)
        {
            var detailsToRemove = await _context.BorrowTicketDetails.Where(d => d.BorrowTicketId == ticketId && d.BookId == bookId).ToListAsync();
            if (!detailsToRemove.Any()) return Json(new { success = false });

            if (removeAll) _context.BorrowTicketDetails.RemoveRange(detailsToRemove);
            else _context.BorrowTicketDetails.Remove(detailsToRemove.First());

            await _context.SaveChangesAsync();

            if (!await _context.BorrowTicketDetails.AnyAsync(d => d.BorrowTicketId == ticketId))
            {
                var ticket = await _context.BorrowTickets.FindAsync(ticketId);
                ticket!.Status = BorrowStatus.Canceled;
                ticket.Note += "\n[Hủy tự động do xóa hết sách]";
                await _context.SaveChangesAsync();
                return Json(new { success = true, isCanceled = true, message = "Đã xóa sách cuối cùng, phiếu bị hủy!" });
            }
            return Json(new { success = true, isCanceled = false });
        }

        //Kiểm tra mã sách trước khi lưu tránh lỗi quét nhầm sách khác tựa, hoặc sách đã bị mượn
        [HttpPost]
        [Authorize(Roles = "Admin, Librarian")]
        public async Task<IActionResult> ValidateBarcode(int ticketId, int bookId, string barcode)
        {
            var copy = await _context.BookCopies.FirstOrDefaultAsync(c => c.CopyCode.ToUpper() == barcode.Trim().ToUpper());
            if (copy == null) return Json(new { isValid = false, message = "Mã sách không tồn tại!" });
            if (copy.BookId != bookId) return Json(new { isValid = false, message = "Mã sách không thuộc tựa này!" });
            if (copy.Status != BookCopyStatus.Available) return Json(new { isValid = false, message = "Sách đã bị mượn hoặc không khả dụng!" });
            return Json(new { isValid = true });
        }

        //Lưu mã sách đã quét vào phiếu mượn, chuyển trạng thái sách sang OnHold (giữ chỗ chờ lấy)
        [HttpPost]
        [Authorize(Roles = "Admin, Librarian")]
        public async Task<IActionResult> SaveScannedCodes(int ticketId, [FromForm] List<string> scannedCodes)
        {
            var ticket = await _context.BorrowTickets.Include(t => t.TicketDetails).FirstOrDefaultAsync(t => t.Id == ticketId);

            foreach (var code in scannedCodes)
            {
                var copy = await _context.BookCopies.FirstOrDefaultAsync(c => c.CopyCode.ToUpper() == code.Trim().ToUpper());
                if (copy != null)
                {
                    var emptyDetail = ticket!.TicketDetails.FirstOrDefault(d => d.BookId == copy.BookId && d.BookCopyId == null);
                    if (emptyDetail != null)
                    {
                        emptyDetail.BookCopyId = copy.Id;
                        copy.Status = BookCopyStatus.OnHold; // Sách giữ chỗ chờ lấy
                    }
                }
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Xác nhận đã giao sách cho bạn đọc, chuyển trạng thái phiếu từ Accepted -> Borrowing và sách từ OnHold -> Borrowed
        [HttpPost]
        [Authorize(Roles = "Admin, Librarian")]
        public async Task<IActionResult> ConfirmHandover(int ticketId)
        {
            var ticket = await _context.BorrowTickets.Include(t => t.TicketDetails).ThenInclude(d => d.BookCopy)
                                       .FirstOrDefaultAsync(t => t.Id == ticketId);

            ticket!.Status = BorrowStatus.Borrowing;
            foreach (var detail in ticket.TicketDetails)
            {
                if (detail.BookCopy != null && detail.BookCopy.Status == BookCopyStatus.OnHold)
                    detail.BookCopy.Status = BookCopyStatus.Borrowed;
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        //trả sách 
        [HttpPost]
        [Authorize(Roles = "Admin, Librarian")]
        public async Task<IActionResult> ConfirmReturnAll(int ticketId)
        {
            var ticket = await _context.BorrowTickets.Include(t => t.TicketDetails).ThenInclude(d => d.BookCopy)
                                       .FirstOrDefaultAsync(t => t.Id == ticketId);

            ticket!.ActualReturnDate = DateTime.Now;
            ticket.Status = BorrowStatus.Returned;
            foreach (var detail in ticket.TicketDetails)
            {
                if (detail.BookCopy != null) detail.BookCopy.Status = BookCopyStatus.Available;
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}