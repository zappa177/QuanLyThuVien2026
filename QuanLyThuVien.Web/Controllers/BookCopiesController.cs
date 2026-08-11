using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Web.Data;
using QuanLyThuVien.Web.Entities;
using QuanLyThuVien.Web.Enums;
using QuanLyThuVien.Web.Models;

namespace QuanLyThuVien.Web.Controllers
{
    [Authorize(Roles = "Admin, Librarian")]
    public class BookCopiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookCopiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Giao diện danh sách bản sao của 1 tựa sách
        [HttpGet]
        public async Task<IActionResult> Index(int bookId)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null) return NotFound("Không tìm thấy tựa sách.");

            // 1. Tạo câu truy vấn cơ bản
            var query = _context.BookCopies
                .Include(c => c.ShelfTier)
                    .ThenInclude(t => t!.Shelf)
                .Where(c => c.BookId == bookId)
                .AsQueryable();

            // Nếu không phải admin mà là thủ thư thì chỉ hiển thị các bản sao đang hoạt động
            if (!User.IsInRole("Admin"))
            {
                query = query.Where(c => c.IsActive == true);
            }

            var copies = await query.ToListAsync();

            var model = new BookCopyIndexViewModel
            {
                BookId = book.Id,
                BookTitle = book.Title,
                CoverImage = string.IsNullOrEmpty(book.CoverImage) ? "/images/no-cover.png" : book.CoverImage,
                Copies = copies.Select(c => new BookCopyItemViewModel
                {
                    Id = c.Id,
                    CopyCode = c.CopyCode,
                    Location = c.ShelfTier != null ? $"{c.ShelfTier.Shelf!.Name} - {c.ShelfTier.TierName}" : "Chưa xếp kệ",
                    ShelfId = c.ShelfTier?.ShelfId ?? 0,
                    TierId = c.ShelfTierId,
                    Status = c.Status,
                    IsReferenceOnly = c.IsReferenceOnly,
                    IsActive = c.IsActive
                }).OrderBy(c => c.CopyCode).ToList()
            };

            // Truyền danh sách Kệ ra View để cho vào Dropdown
            ViewBag.Shelves = new SelectList(await _context.Shelves.Where(s => s.IsActive).ToListAsync(), "Id", "Name");
            return View(model);
        }

        // Lưu bản sao
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCopy(int id, int bookId, string copyCode, int shelfTierId, BookCopyStatus status, bool isReferenceOnly)
        {
            // Kiểm tra dữ liệu đầu vào 
            if (string.IsNullOrWhiteSpace(copyCode))
                return Json(new { success = false, message = "Mã bản sao  không được để trống." });

            // Bắt buộc phải có Tầng kệ hợp lệ
            if (shelfTierId <= 0)
                return Json(new { success = false, message = "Vui lòng chọn Vị trí (Kệ và Tầng) hợp lệ." });

            // Kiểm tra trùng mã vạch
            bool isExist = await _context.BookCopies.AnyAsync(c => c.CopyCode == copyCode && c.Id != id);
            if (isExist)
                return Json(new { success = false, message = "Mã bản sao này đã tồn tại trong hệ thống. Vui lòng nhập mã khác." });

            if (id > 0)
            {
                // CẬP NHẬT
                var copy = await _context.BookCopies.FindAsync(id);
                if (copy == null) return Json(new { success = false, message = "Không tìm thấy bản sao." });

                // Phân quyền cập nhật
                if (User.IsInRole("Admin"))
                {
                    copy.CopyCode = copyCode;
                    copy.IsReferenceOnly = isReferenceOnly;
                }

                copy.ShelfTierId = shelfTierId;
                copy.Status = status;

                _context.BookCopies.Update(copy);
            }
            else
            {
                //thêm mới chỉ admin mới có quyền thêm bản sao mới
                if (!User.IsInRole("Admin"))
                    return Json(new { success = false, message = "Chỉ Admin mới có quyền thêm bản sao mới." });

                var newCopy = new BookCopies
                {
                    BookId = bookId,
                    CopyCode = copyCode,
                    ShelfTierId = shelfTierId,
                    Status = status,
                    IsReferenceOnly = isReferenceOnly,
                    IsActive = true
                };
                _context.BookCopies.Add(newCopy);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Lưu bản sao thành công!" });
        }

        // Ẩn / Hiện bản sao (Chỉ Admin)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var copy = await _context.BookCopies.FindAsync(id);
            if (copy == null) return Json(new { success = false, message = "Không tìm thấy bản sao." });

            copy.IsActive = !copy.IsActive;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
