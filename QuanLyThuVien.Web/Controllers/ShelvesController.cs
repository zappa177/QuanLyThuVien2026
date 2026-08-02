using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Domain.Entities;
using QuanLyThuVien.Infrastructure.Data;

namespace QuanLyThuVien.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ShelvesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShelvesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. GIAO DIỆN CHÍNH: DANH SÁCH KỆ SÁCH
        // ==========================================
        public async Task<IActionResult> Index(string? searchString, string sortOrder = "name_asc", int page = 1)
        {
            var query = _context.Shelves
                .Include(s => s.ShelfTiers)
                .AsQueryable();

            // Xử lý tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(s => s.Name.Contains(searchString));
            }

            // Xử lý sắp xếp
            query = sortOrder == "name_asc" ? query.OrderBy(s => s.Name) : query.OrderByDescending(s => s.Name);

            // Lưu trạng thái ra View
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentSort = sortOrder;

            return View(await query.ToListAsync());
        }

        // ==========================================
        // CÁC API XỬ LÝ KỆ SÁCH (THÊM, SỬA, ẨN/HIỆN)
        // ==========================================

        // Thêm mới hoặc Cập nhật Kệ sách (Chỉ quản lý Tên kệ)
        [HttpPost]
        public async Task<IActionResult> SaveShelf(int id, string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return Json(new { success = false, message = "Tên kệ không được để trống." });

                if (id > 0)
                {
                    // Cập nhật kệ
                    var shelf = await _context.Shelves.FindAsync(id);
                    if (shelf == null) return Json(new { success = false, message = "Không tìm thấy kệ sách." });

                    shelf.Name = name;
                    _context.Shelves.Update(shelf);
                }
                else
                {
                    // Thêm mới kệ
                    var newShelf = new Shelves
                    {
                        Name = name,
                        IsActive = true
                    };
                    _context.Shelves.Add(newShelf);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Ẩn / Hiện Kệ sách (Set IsActive = false/true thay vì xóa)
        [HttpPost]
        public async Task<IActionResult> ToggleShelfStatus(int id)
        {
            var shelf = await _context.Shelves.FindAsync(id);
            if (shelf == null) return Json(new { success = false, message = "Không tìm thấy kệ sách." });

            shelf.IsActive = !shelf.IsActive; // Đảo ngược trạng thái Active
            _context.Shelves.Update(shelf);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ==========================================
        // CÁC API XỬ LÝ AJAX CHO TẦNG KỆ (MODAL)
        // ==========================================

        // Lấy danh sách tầng của 1 kệ cụ thể
        [HttpGet]
        public async Task<IActionResult> GetTiersByShelf(int shelfId)
        {
            var shelf = await _context.Shelves.FindAsync(shelfId);
            if (shelf == null) return NotFound(new { message = "Không tìm thấy kệ sách." });

            var tiers = await _context.ShelfTiers
                .Where(t => t.ShelfId == shelfId)
                .Include(t => t.Books)
                .Select(t => new
                {
                    id = t.Id,
                    tierName = t.TierName,
                    capacity = t.Capacity,
                    currentBooks = t.Books.Count,
                    isActive = t.IsActive
                }).ToListAsync();

            return Json(new
            {
                shelfName = shelf.Name,
                tiers = tiers
            });
        }

        // Thêm mới hoặc Cập nhật Tầng (Bỏ kiểm tra giới hạn số lượng tầng của kệ)
        [HttpPost]
        public async Task<IActionResult> SaveTier(int id, int shelfId, string tierName, int capacity)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tierName))
                    return Json(new { success = false, message = "Tên tầng không được để trống." });

                if (capacity <= 0)
                    return Json(new { success = false, message = "Sức chứa giới hạn sách phải lớn hơn 0." });

                if (id > 0)
                {
                    // Cập nhật tầng hiện có
                    var tier = await _context.ShelfTiers.FindAsync(id);
                    if (tier == null) return Json(new { success = false, message = "Không tìm thấy tầng." });

                    tier.TierName = tierName;
                    tier.Capacity = capacity;
                    _context.ShelfTiers.Update(tier);
                }
                else
                {
                    // Thêm mới tầng (Không giới hạn số lượng)
                    var newTier = new ShelfTiers
                    {
                        ShelfId = shelfId,
                        TierName = tierName,
                        Capacity = capacity,
                        IsActive = true
                    };
                    _context.ShelfTiers.Add(newTier);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Ẩn / Hiện Tầng (Set IsActive = false/true thay vì xóa)
        [HttpPost]
        public async Task<IActionResult> ToggleTierStatus(int id)
        {
            var tier = await _context.ShelfTiers.FindAsync(id);
            if (tier == null) return Json(new { success = false, message = "Không tìm thấy tầng." });

            tier.IsActive = !tier.IsActive; // Đảo ngược trạng thái
            _context.ShelfTiers.Update(tier);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}