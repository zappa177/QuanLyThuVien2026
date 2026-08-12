using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Web.Data;
using QuanLyThuVien.Web.Entities;
using QuanLyThuVien.Web.Models;

namespace QuanLyThuVien.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // categories/index
        public async Task<IActionResult> Index(string searchName)
        {
            var query = _context.Categories.AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(c => c.Name.Contains(searchName));
            }

            var categories = await query
                .Select(c => new CategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            ViewBag.SearchName = searchName;
            return View(categories);
        }



        // lưu category khi thêm hoặc chỉnh sửa
        [HttpPost]
        public async Task<IActionResult> SaveCategory(int id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Tên thể loại không được để trống." });

            if (id > 0)
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null) return Json(new { success = false, message = "Không tìm thấy thể loại." });
                category.Name = name;
                _context.Categories.Update(category);
            }
            else
            {
                _context.Categories.Add(new Categories { Name = name, IsActive = true });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ẩn hiện thể loại
        [HttpPost]
        public async Task<IActionResult> ToggleCategoryStatus(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return Json(new { success = false, message = "Không tìm thấy thể loại." });

            category.IsActive = !category.IsActive; // Đảo trạng thái

            // Lấy toàn bộ sách thuộc thể loại này VÀ BAO GỒM CẢ BẢN SAO VẬT LÝ
            var relatedBooks = await _context.Books
                .Include(b => b.BookCopies) // <--- QUAN TRỌNG: Include thêm bảng này
                .Where(b => b.CategoryId == id)
                .ToListAsync();

            int totalCopiesAffected = 0;

            // Cập nhật trạng thái tựa sách và bản sao
            foreach (var book in relatedBooks)
            {
                book.IsActive = category.IsActive;

                if (book.BookCopies != null)
                {
                    foreach (var copy in book.BookCopies)
                    {
                        copy.IsActive = category.IsActive;
                        totalCopiesAffected++;
                    }
                }
            }

            await _context.SaveChangesAsync();

            var statusText = category.IsActive ? "hiện" : "ẩn";
            return Json(new
            {
                success = true,
                message = $"Đã {statusText} thể loại, {relatedBooks.Count} tựa sách và {totalCopiesAffected} bản sao liên quan."
            });
        }
    }
}
