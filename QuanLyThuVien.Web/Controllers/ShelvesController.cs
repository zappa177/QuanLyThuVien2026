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

        //hiện danh sách kệ shelves/index
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

        // ẩn hiện kệ sách theo isactive (true/false)
        [HttpPost]
        public async Task<IActionResult> ToggleShelfStatus(int id)
        {
            // Lấy thông tin kệ sách
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

            // lấy danh sách các tầng của kệ
            var tierIds = relatedTiers.Select(t => t.Id).ToList();

            // Tìm toàn bộ sách đang nằm trên các tầng này
            var relatedBooks = await _context.Books
                .Where(b => tierIds.Contains(b.ShelfTierId))
                .ToListAsync();

            // Cập nhật trạng thái tầng theo kệ
            foreach (var tier in relatedTiers)
            {
                tier.IsActive = shelf.IsActive;
            }

            // Cập nhật trạng thái sách theo kệ
            foreach (var book in relatedBooks)
            {
                book.IsActive = shelf.IsActive;
            }

            // Lưu thay đổi vào cơ sở dữ liệu (EF Core sẽ track cả Kệ, Tầng và Sách)
            await _context.SaveChangesAsync();

            var statusText = shelf.IsActive ? "hiện" : "ẩn";
            return Json(new
            {
                success = true,
                message = $"Đã {statusText} kệ sách, {relatedTiers.Count} tầng kệ và {relatedBooks.Count} cuốn sách bên trong."
            });
        }


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
            // lấy thông tin tầng kệ
            var tier = await _context.ShelfTiers.FindAsync(id);
            if (tier == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tầng." });
            }

            // đảo trạng thái tầng
            tier.IsActive = !tier.IsActive;

            // lấy toàn bộ sách đang được xếp trên tầng này
            var relatedBooks = await _context.Books
                .Where(b => b.ShelfTierId == id)
                .ToListAsync();

            // cập nhật trạng thái sách theo tầng
            foreach (var book in relatedBooks)
            {
                book.IsActive = tier.IsActive;
            }


            await _context.SaveChangesAsync();

            var statusText = tier.IsActive ? "hiện" : "ẩn";
            return Json(new
            {
                success = true,
                message = $"Đã {statusText} tầng và {relatedBooks.Count} cuốn sách nằm trên tầng này."
            });
        }
    }
}