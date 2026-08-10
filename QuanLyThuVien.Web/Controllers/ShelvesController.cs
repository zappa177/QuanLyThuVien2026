using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Web.Entities;
using QuanLyThuVien.Web.Data;

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

        // Hiện danh sách kệ shelves/index
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

        // Thêm mới hoặc chỉnh sửa tên kệ sách
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

        // Ẩn hiện kệ sách theo isactive (true/false)
        [HttpPost]
        public async Task<IActionResult> ToggleShelfStatus(int id)
        {
            var shelf = await _context.Shelves.FindAsync(id);
            if (shelf == null)
            {
                return Json(new { success = false, message = "Không tìm thấy kệ sách." });
            }

            // Đảo trạng thái của kệ sách
            shelf.IsActive = !shelf.IsActive;

            // Lấy toàn bộ các tầng (ShelfTiers) thuộc kệ này
            var relatedTiers = await _context.ShelfTiers
                .Where(t => t.ShelfId == id)
                .ToListAsync();

            var tierIds = relatedTiers.Select(t => t.Id).ToList();

            // Lấy toàn bộ CÁC BẢN SAO VẬT LÝ (BookCopies) đang nằm trên các tầng này
            var relatedCopies = await _context.BookCopies
                .Where(bc => tierIds.Contains(bc.ShelfTierId))
                .ToListAsync();

            // Cập nhật trạng thái tầng theo kệ
            foreach (var tier in relatedTiers)
            {
                tier.IsActive = shelf.IsActive;
            }

            // Cập nhật trạng thái bản sao vật lý theo kệ
            foreach (var copy in relatedCopies)
            {
                copy.IsActive = shelf.IsActive;
            }

            await _context.SaveChangesAsync();

            var statusText = shelf.IsActive ? "hiện" : "ẩn";
            return Json(new
            {
                success = true,
                message = $"Đã {statusText} kệ sách, {relatedTiers.Count} tầng kệ và {relatedCopies.Count} bản sao sách bên trong."
            });
        }

        // Lấy danh sách tầng của 1 kệ cụ thể (Thống kê số lượng BookCopies thay vì Books cũ)
        [HttpGet]
        public async Task<IActionResult> GetTiersByShelf(int shelfId)
        {
            var shelf = await _context.Shelves.FindAsync(shelfId);
            if (shelf == null) return NotFound(new { message = "Không tìm thấy kệ sách." });

            var tiers = await _context.ShelfTiers
                .Where(t => t.ShelfId == shelfId)
                .Include(t => t.BookCopies) // Lấy danh sách bản sao vật lý trên tầng
                .Select(t => new
                {
                    id = t.Id,
                    tierName = t.TierName,
                    capacity = t.Capacity,
                    currentBooks = t.BookCopies.Count, // Đếm số lượng bản sao vật lý thực tế trên kệ
                    isActive = t.IsActive
                }).ToListAsync();

            return Json(new
            {
                shelfName = shelf.Name,
                tiers = tiers
            });
        }

        // Thêm mới hoặc Cập nhật Tầng
        [HttpPost]
        public async Task<IActionResult> SaveTier(int id, int shelfId, string tierName, int capacity)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tierName))
                    return Json(new { success = false, message = "Tên tầng không được để trống." });

                if (capacity <= 0 || capacity > 50)
                    return Json(new { success = false, message = "Sức chứa giới hạn sách phải lớn hơn 0 nhỏ hơn 50." });

                if (id > 0)
                {
                    var tier = await _context.ShelfTiers.FindAsync(id);
                    if (tier == null) return Json(new { success = false, message = "Không tìm thấy tầng." });

                    tier.TierName = tierName;
                    tier.Capacity = capacity;
                    _context.ShelfTiers.Update(tier);
                }
                else
                {
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

        // Ẩn / Hiện Tầng (Set IsActive = false/true)
        [HttpPost]
        public async Task<IActionResult> ToggleTierStatus(int id)
        {
            var tier = await _context.ShelfTiers.FindAsync(id);
            if (tier == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tầng." });
            }

            // Đảo trạng thái tầng
            tier.IsActive = !tier.IsActive;

            // Lấy toàn bộ các bản sao vật lý nằm trên tầng này
            var relatedCopies = await _context.BookCopies
                .Where(bc => bc.ShelfTierId == id)
                .ToListAsync();

            // Cập nhật trạng thái bản sao vật lý theo tầng
            foreach (var copy in relatedCopies)
            {
                copy.IsActive = tier.IsActive;
            }

            await _context.SaveChangesAsync();

            var statusText = tier.IsActive ? "hiện" : "ẩn";
            return Json(new
            {
                success = true,
                message = $"Đã {statusText} tầng và {relatedCopies.Count} bản sao sách nằm trên tầng này."
            });
        }
    }
}