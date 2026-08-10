using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Web.Entities;
using QuanLyThuVien.Web.Data;
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
            // lấy thông tin thể loại
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thể loại." });
            }

            // đảo ngược trạng thái của thể loại
            category.IsActive = !category.IsActive;

            // lấy toàn bộ sách thuộc thể loại này
            var relatedBooks = await _context.Books
                .Where(b => b.CategoryId == id)
                .ToListAsync();

            // cập nhật trạng thái của tất cả sách liên quan theo trạng thái của thể loại
            foreach (var book in relatedBooks)
            {
                book.IsActive = category.IsActive;
            }

            // Không cần gọi _context.Categories.Update(category)  vì chỉ cập nhật 1 thuộc tính
            await _context.SaveChangesAsync();

            // Trả về câu thông báo chi tiết hơn để hiển thị cho người dùng
            var statusText = category.IsActive ? "hiện" : "ẩn";
            return Json(new
            {
                success = true,
                message = $"Đã {statusText} thể loại và {relatedBooks.Count} cuốn sách liên quan."
            });
        }
    }
}
