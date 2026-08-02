using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Domain.Entities;
using QuanLyThuVien.Infrastructure.Data;
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

        // 1. GIAO DIỆN CHÍNH
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

        // 2. LẤY THÔNG TIN ĐỂ SỬA
        [HttpGet]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return Json(new { id = category.Id, name = category.Name });
        }

        // 2. Thêm / Cập nhật (Trả về JSON)
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

        // 3. Ẩn / Hiện Thể loại (Set IsActive = false/true - Trả về JSON)
        [HttpPost]
        public async Task<IActionResult> ToggleCategoryStatus(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return Json(new { success = false, message = "Không tìm thấy thể loại." });

            category.IsActive = !category.IsActive;
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
