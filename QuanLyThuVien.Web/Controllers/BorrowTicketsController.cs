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

            // Quét các phiếu đang mượn để tự động chuyển sang Overdue nếu quá hạn
            var borrowingTickets = await query.Where(t => t.Status == BorrowStatus.Borrowing).ToListAsync();
            bool hasChanges = false;

            foreach (var ticket in borrowingTickets)
            {
                // So sánh ngày hiện tại với hạn trả (ExpectedReturnDate)
                if (DateTime.Now.Date > ticket.ExpectedReturnDate.Date)
                {
                    ticket.Status = BorrowStatus.Overdue;

                    // Ghi log nhẹ vào note nếu chưa có
                    if (ticket.Note == null || !ticket.Note.Contains("Quá hạn"))
                    {
                        ticket.Note = string.IsNullOrEmpty(ticket.Note)
                            ? $"[{DateTime.Now:dd/MM/yyyy}] Hệ thống tự động chuyển sang trạng thái Quá hạn."
                            : ticket.Note + $"\n[{DateTime.Now:dd/MM/yyyy}] Hệ thống tự động chuyển sang trạng thái Quá hạn.";
                    }

                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }

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
            bool isAllFinished = ticket.TicketDetails.All(d => d.IsReturned == true);

            if (isAllFinished)
            {
                ticket.Status = BorrowStatus.Returned;

                if (!ticket.ActualReturnDate.HasValue)
                {
                    ticket.ActualReturnDate = DateTime.Now;
                }
            }
            ViewBag.IsStaff = User.IsInRole("Admin") || User.IsInRole("Librarian");
            return View(ticket);
        }


        // Kiểm tra tồn kho trước khi duyệt phiếu (Pending -> Accepted)
        [HttpPost]
        [Authorize(Roles = "Admin, Librarian")]
        public async Task<IActionResult> CheckInventory(int ticketId)
        {
            var ticket = await _context.BorrowTickets
                .Include(t => t.TicketDetails)
                    .ThenInclude(d => d.Book)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null) return Json(new { success = false, message = "Không tìm thấy phiếu." });

            // Lấy danh sách số lượng yêu cầu của từng tựa sách
            var requiredBooks = ticket.TicketDetails
                .GroupBy(d => d.BookId)
                .Select(g => new { BookId = g.Key, Title = g.First().Book?.Title, Qty = g.Count() })
                .ToList();

            var bookIds = requiredBooks.Select(r => r.BookId).ToList();

            // Lấy số lượng khả dụng của TẤT CẢ tựa sách trong 1 lần truy vấn
            // Gộp cả trạng thái Available và Pending
            var availableStocks = await _context.BookCopies
                .Where(c => bookIds.Contains(c.BookId)
                         && (c.Status == BookCopyStatus.Available || c.Status == BookCopyStatus.Pending)
                         && c.IsActive
                         && !c.IsReferenceOnly)
                .GroupBy(c => c.BookId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            // Đối chiếu số lượng
            foreach (var req in requiredBooks)
            {
                int available = availableStocks.ContainsKey(req.BookId) ? availableStocks[req.BookId] : 0;

                if (available < req.Qty)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Lỗi tồn kho: '{req.Title}' cần {req.Qty} cuốn, nhưng kho chỉ còn {available}. Vui lòng Giảm/Xóa sách khỏi phiếu!"
                    });
                }
            }

            return Json(new { success = true, message = "Tồn kho hợp lệ! Bạn có thể duyệt phiếu." });
        }


        //duyệt phiếu chuyển trạng thái từ Pending -> Accepted (Admin, Librarian)
        [HttpPost]
        [Authorize(Roles = "Admin, Librarian")]
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
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
            if (copy.Status != BookCopyStatus.Available && copy.Status != BookCopyStatus.Pending) return Json(new { isValid = false, message = "Sách đã bị mượn hoặc không khả dụng!" });
            return Json(new { isValid = true });
        }

        //Lưu mã sách đã quét vào phiếu mượn, chuyển trạng thái sách sang OnHold (giữ chỗ chờ lấy)
        [HttpPost]
        [Authorize(Roles = "Admin, Librarian")]
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
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
            var ticket = await _context.BorrowTickets
                .Include(t => t.TicketDetails)
                    .ThenInclude(d => d.BookCopy)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null) return Json(new { success = false, message = "Không tìm thấy phiếu mượn." });

            // CHỈ thu hồi những sách chưa trả VÀ không bị sự cố (Mất/Hỏng)
            var normalBooksToReturn = ticket.TicketDetails.Where(d =>
                d.IsReturned == false &&
                d.IssueStatus != BookCopyStatus.Lost &&
                d.IssueStatus != BookCopyStatus.Damaged).ToList();

            if (!normalBooksToReturn.Any())
            {
                return Json(new { success = false, message = "Không có sách bình thường nào để thu hồi (Sách đang chờ xử lý đền bù)." });
            }

            // Tiến hành thu hồi sách bình thường về kho
            foreach (var detail in normalBooksToReturn)
            {
                detail.IsReturned = true;
                if (detail.BookCopy != null)
                {
                    detail.BookCopy.Status = BookCopyStatus.Pending; // Chuyển về kho chờ xếp lên kệ
                }
            }


            // Kiểm tra xem phiếu này đã hoàn thành 100% chưa?
            // Nếu có sách đang báo Mất/Hỏng (IsReturned = false) thì isAllReturned = false -> Không đóng phiếu.
            bool isAllReturned = ticket.TicketDetails.All(d => d.IsReturned == true);
            if (isAllReturned)
            {
                ticket.Status = BorrowStatus.Returned; // Khép hồ sơ
                ticket.ActualReturnDate = DateTime.Now;
            }

            // Ghi chú
            string logMessage = isAllReturned ? "Thu hồi toàn bộ sách (Đã đóng phiếu)." : "Thu hồi các sách bình thường (Vẫn còn sách chờ xử lý đền bù).";
            ticket.Note = string.IsNullOrEmpty(ticket.Note)
                ? $"[{DateTime.Now:dd/MM}] {logMessage}"
                : ticket.Note + $"\n[{DateTime.Now:dd/MM}] {logMessage}";

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        //gợi ý vị trí sách cho Thủ thư đi nhặt
        [HttpGet]
        [Authorize(Roles = "Admin, Librarian")]
        public async Task<IActionResult> GetAvailableCopiesForTicket(int ticketId)
        {
            var ticket = await _context.BorrowTickets
                .Include(t => t.TicketDetails)
                .ThenInclude(d => d.Book)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null) return NotFound();

            // Nhóm các sách lại, chỉ quan tâm những sách chưa được gán mã vật lý
            var groupedRequests = ticket.TicketDetails
                .Where(d => d.BookCopyId == null)
                .GroupBy(d => d.BookId)
                .ToList();

            var result = new List<object>();

            foreach (var group in groupedRequests)
            {
                var book = group.First().Book;
                int reqQty = group.Count();

                // Tìm tất cả các bản sao đang khả dụng của tựa sách này
                var availableCopies = await _context.BookCopies
                    .Include(bc => bc.ShelfTier)
                        .ThenInclude(st => st!.Shelf)
                    .Where(bc => bc.BookId == book!.Id
                              && (bc.Status == BookCopyStatus.Available || bc.Status == BookCopyStatus.Pending)
                              && bc.IsActive
                              && !bc.IsReferenceOnly)
                    .Select(bc => new
                    {
                        copyCode = bc.CopyCode,
                        location = bc.Status == BookCopyStatus.Pending ? "Tại vị trí chờ xếp lên kệ" : (
                        bc.ShelfTier != null && bc.ShelfTier.Shelf != null
                            ? $"{bc.ShelfTier.Shelf.Name} - {bc.ShelfTier.TierName}"
                            : "Chưa xếp kệ")
                    })
                    .ToListAsync();

                result.Add(new
                {
                    bookTitle = book!.Title,
                    requiredQty = reqQty,
                    copies = availableCopies
                });
            }

            return Json(result);
        }

        //thu hồi 1 phần sách trong tổng số sách mượn 
        [HttpPost]
        [Authorize(Roles = "Admin, Librarian")]
        public async Task<IActionResult> ConfirmPartialReturn(int ticketId, List<string> scannedCodes)
        {
            if (scannedCodes == null || !scannedCodes.Any())
            {
                return Json(new { success = false, message = "Vui lòng quét ít nhất một cuốn sách khách trả!" });
            }

            var ticket = await _context.BorrowTickets
                .Include(t => t.TicketDetails)
                    .ThenInclude(d => d.BookCopy)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phiếu mượn." });
            }

            // Lọc ra các chi tiết ứng với mã quét và CHƯA TRẢ (IsReturned == false)
            var detailsToReturn = ticket.TicketDetails
                .Where(d => d.BookCopyId.HasValue &&
                            d.BookCopy != null &&
                            scannedCodes.Contains(d.BookCopy.CopyCode.ToUpper()) &&
                            d.IsReturned == false)
                .ToList();

            if (!detailsToReturn.Any())
            {
                return Json(new { success = false, message = "Không tìm thấy mã sách hợp lệ cần thu hồi." });
            }

            foreach (var detail in detailsToReturn)
            {
                if (detail.BookCopy != null)
                {
                    // Chuyển sách vật lý về quầy chờ xếp kệ
                    detail.BookCopy.Status = BookCopyStatus.Pending;
                }

                // Không xóa, chỉ đánh dấu cuốn này đã được trả
                detail.IsReturned = true;
                detail.IssueStatus = null;
            }

            // Kiểm tra xem phiếu còn cuốn nào ĐANG MƯỢN (chưa trả) không?
            bool stillHasActiveBorrows = ticket.TicketDetails.Any(d => d.BookCopyId.HasValue && d.IsReturned == false);

            if (!stillHasActiveBorrows)
            {
                // Trả sạch 100% -> phiếu thành trạng thái đã trả
                ticket.Status = BorrowStatus.Returned;
            }
            else
            {
                // Vẫn còn sách chưa trả -> Giữ trạng thái Borrowing
                ticket.Status = BorrowStatus.Borrowing;
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Thu hồi thành công {detailsToReturn.Count} cuốn sách! Lịch sử mượn đã được lưu lại."
            });
        }

        //báo sách mất hỏng
        [HttpPost]
        [Authorize(Roles = "Admin, Librarian")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportBookIssue(int ticketId, string barcode, int issueType)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return Json(new { success = false, message = "Mã vạch không hợp lệ." });

            var ticket = await _context.BorrowTickets
                .Include(t => t.TicketDetails)
                    .ThenInclude(d => d.BookCopy)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null) return Json(new { success = false, message = "Không tìm thấy phiếu mượn." });

            var detail = ticket.TicketDetails.FirstOrDefault(d =>
                d.BookCopy != null &&
                d.BookCopy.CopyCode.ToUpper() == barcode.ToUpper() &&
                d.IsReturned == false); // Chỉ tìm các sách chưa trả

            if (detail == null) return Json(new { success = false, message = "Không tìm thấy mã sách này hoặc sách đã được thu hồi." });

            // cập nhật sự cố (issue)
            detail.IssueStatus = (BookCopyStatus)issueType;
            detail.BookCopy!.Status = (BookCopyStatus)issueType;

            // ghi chú ý
            string issueText = issueType == 4 ? "Mất sách" : "Hỏng sách";
            ticket.Note = string.IsNullOrEmpty(ticket.Note)
                ? $"[{DateTime.Now:dd/MM}] Báo {issueText} mã {barcode}."
                : ticket.Note + $"\n[{DateTime.Now:dd/MM}] Báo {issueText} mã {barcode}.";

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Đã ghi nhận {issueText} cho mã vạch {barcode}." });
        }
        //admin thủ thư hủy phiếu có lý do
        [HttpPost]
        [Authorize(Roles = "Admin, Librarian")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelTicket(int ticketId, string reason)
        {
            var ticket = await _context.BorrowTickets
                .Include(t => t.TicketDetails)
                    .ThenInclude(d => d.BookCopy)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null) return Json(new { success = false, message = "Không tìm thấy phiếu mượn." });

            // Chỉ cho phép hủy khi phiếu chưa được giao cho khách
            if (ticket.Status != BorrowStatus.Pending && ticket.Status != BorrowStatus.Accepted)
            {
                return Json(new { success = false, message = "Chỉ có thể hủy phiếu khi đang chờ duyệt hoặc chờ giao sách!" });
            }

            // Giải phóng các cuốn sách đã được quét
            var assignedDetails = ticket.TicketDetails.Where(d => d.BookCopy != null).ToList();
            foreach (var detail in assignedDetails)
            {
                // Trả sách về trạng thái Pending (Chờ xếp lại lên kệ) 
                detail.BookCopy!.Status = BookCopyStatus.Pending;
            }

            // Đổi trạng thái phiếu thành Đã Hủy
            ticket.Status = BorrowStatus.Canceled;

            // Lưu lý do hủy vào Ghi chú
            string cancelReason = string.IsNullOrWhiteSpace(reason) ? "Hủy do quản trị viên/thủ thư." : reason;
            ticket.Note = string.IsNullOrEmpty(ticket.Note)
                ? $"[{DateTime.Now:dd/MM}] Hủy phiếu: {cancelReason}"
                : ticket.Note + $"\n[{DateTime.Now:dd/MM}] Hủy phiếu: {cancelReason}";

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã hủy phiếu mượn thành công!" });
        }

        //giải quyết sự cố sách mất hỏng
        [HttpPost]
        [Authorize(Roles = "Admin, Librarian")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveBookIssue(int ticketId, string barcode, int resolveType, string resolveNote)
        {
            var ticket = await _context.BorrowTickets
                .Include(t => t.TicketDetails)
                    .ThenInclude(d => d.BookCopy)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null) return Json(new { success = false, message = "Không tìm thấy phiếu mượn." });

            // Tìm chi tiết của mã sách đang bị báo mất/hỏng trong phiếu này
            var detail = ticket.TicketDetails.FirstOrDefault(d =>
                d.BookCopy != null &&
                d.BookCopy.CopyCode.ToUpper() == barcode.ToUpper());

            if (detail == null) return Json(new { success = false, message = "Không tìm thấy mã sách này." });

            string actionLog = "";

            // Xử lý theo hình thức
            if (resolveType == 1)
            {
                // TÌM THẤY SÁCH: Khôi phục trạng thái ngoài kho (pending-chờ xếp)
                detail.BookCopy!.Status = BookCopyStatus.Pending;

                detail.IsReturned = true; // Đánh dấu đã thu hồi
                                          // detail.IssueStatus vẫn đang giữ là Lost/Damaged từ trước để giao diện hiện gạch ngang.

                actionLog = "Tìm thấy và thu hồi sách cũ.";
            }
            else if (resolveType == 2)
            {
                // ĐỀN TIỀN: sách vẫn mất , nhưng khách đã đền tiền mặt, đánh dấu sách này đã được giải quyết
                detail.IsReturned = true; // Đánh dấu đã thu hồi (dù sách vẫn mất) , detail.IssueStatus vẫn giữ là Lost/Damaged để giao diện hiện gạch ngang.
                actionLog = "Khách đền tiền mặt.";
            }
            else if (resolveType == 3)
            {
                // ĐỀN SÁCH MỚI: sách vẫn mất , sách mới sẽ được nhập mã mới
                detail.IsReturned = true;   // Đánh dấu đã thu hồi (dù sách vẫn mất) , detail.IssueStatus vẫn giữ là Lost/Damaged để giao diện hiện gạch ngang.
                actionLog = "Khách đền sách mới (Chờ Admin nhập kho).";
            }

            // Cập nhật Ghi chú lịch sử phiếu mượn 
            ticket.Note = string.IsNullOrEmpty(ticket.Note)
                ? $"[{DateTime.Now:dd/MM}] ĐÃ GIẢI QUYẾT ({barcode}): {actionLog} - {resolveNote}"
                : ticket.Note + $"\n[{DateTime.Now:dd/MM}] ĐÃ GIẢI QUYẾT ({barcode}): {actionLog} - {resolveNote}";

            // Kiểm tra xem tất cả các sách trong phiếu này đã được thu hồi / đền bù xong hết chưa?
            bool isAllReturned = ticket.TicketDetails.All(d => d.IsReturned == true);

            if (isAllReturned)
            {
                ticket.Status = BorrowStatus.Returned; // Chuyển trạng thái phiếu thành Đã Trả 
                ticket.ActualReturnDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Lịch sử mượn của 1 khách
        [HttpGet]
        [Authorize(Roles = "Admin, Librarian")]
        public async Task<IActionResult> UserHistory(Guid userId)
        {
            if (userId == Guid.Empty) return NotFound("Mã người dùng không hợp lệ.");

            // Lấy thông tin người dùng
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return NotFound("Không tìm thấy thông tin độc giả.");

            // Lấy toàn bộ phiếu mượn của người này (kèm chi tiết sách)
            var tickets = await _context.BorrowTickets
                .Include(t => t.TicketDetails)
                    .ThenInclude(d => d.Book) // Lấy thông tin tựa sách
                .Include(t => t.TicketDetails)
                    .ThenInclude(d => d.BookCopy) // Lấy thông tin mã vật lý
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.BorrowDate) // Xếp phiếu mới nhất lên đầu
                .ToListAsync();

            // Tính toán thống kê nhanh gửi ra View
            ViewBag.TotalBorrowed = tickets.Count;
            ViewBag.ActiveTickets = tickets.Count(t => t.Status == BorrowStatus.Borrowing || t.Status == BorrowStatus.Pending || t.Status == BorrowStatus.Accepted);

            // Tính số phiếu đang quá hạn
            ViewBag.OverdueTickets = tickets.Count(t => t.Status == BorrowStatus.Overdue ||
                                    (t.Status == BorrowStatus.Borrowing && DateTime.Now.Date > t.ExpectedReturnDate.Date));

            // Đếm số lần làm mất/hỏng sách (dựa vào IssueStatus)
            ViewBag.Violations = tickets.SelectMany(t => t.TicketDetails)
                                        .Count(d => d.IssueStatus == BookCopyStatus.Lost || d.IssueStatus == BookCopyStatus.Damaged);

            ViewBag.UserInfo = user;

            return View(tickets);
        }
    }
}