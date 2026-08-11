using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuanLyThuVien.Web.Common;
using QuanLyThuVien.Web.Data;
using QuanLyThuVien.Web.Entities;
using QuanLyThuVien.Web.Entities.Identity;
using QuanLyThuVien.Web.Enums;
using QuanLyThuVien.Web.Models;

namespace QuanLyThuVien.Web.Controllers
{
    [Authorize] // Bắt buộc đăng nhập mới được vào trang chủ
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMemoryCache _cache;
        private const string CATEGORY_CACHE_KEY = "ActiveCategoriesList";

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
        }

        // Trang chủ Index hiển thị danh mục tựa sách (Có phân trang, tìm kiếm, lọc, sắp xếp)
        [HttpGet]
        public async Task<IActionResult> Index(
            string searchTitle, string searchAuthor, string searchISBN,
            int? categoryId, int? publishYear,
            string sortBy = "PublishYear", int pageNumber = 1)
        {
            int pageSize = 9;
            bool? isActiveFilter = User.IsInRole("Admin") ? null : true;

            // 1. Tạo Query trực tiếp từ DbContext
            var query = _context.Books
                 .Include(b => b.Category)
                 .Include(b => b.BookCopies)
                 .AsQueryable();

            if (isActiveFilter.HasValue)
                query = query.Where(b => b.IsActive == isActiveFilter.Value);

            if (!string.IsNullOrWhiteSpace(searchTitle))
                query = query.Where(b => b.Title.ToLower().Contains(searchTitle.ToLower()));

            if (!string.IsNullOrWhiteSpace(searchAuthor))
                query = query.Where(b => b.Author.ToLower().Contains(searchAuthor.ToLower()));

            if (!string.IsNullOrWhiteSpace(searchISBN))
                query = query.Where(b => b.ISBN != null && b.ISBN.ToLower().Contains(searchISBN.ToLower()));

            if (publishYear.HasValue)
                query = query.Where(b => b.PublishYear == publishYear.Value);

            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(b => b.CategoryId == categoryId.Value);

            int totalCount = await query.CountAsync();

            query = sortBy switch
            {
                "year_desc" => query.OrderByDescending(b => b.PublishYear),
                "year_asc" => query.OrderBy(b => b.PublishYear),
                "title_desc" => query.OrderByDescending(b => b.Title),
                "title_asc" => query.OrderBy(b => b.Title),
                _ => query.OrderByDescending(b => b.CreatedAt)
            };

            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            var pagedBooks = new PagedResult<Books>(items, totalCount, pageNumber, pageSize);


            // Load thể loại từ cache để giảm tải truy vấn Database, đặc biệt khi có nhiều người dùng truy cập cùng lúc
            var categories = await _cache.GetOrCreateAsync(CATEGORY_CACHE_KEY, async entry =>
            {
                // Nếu không có ai truy cập trong 30 phút, xóa cache
                entry.SlidingExpiration = TimeSpan.FromMinutes(30);

                // Cứ sau 12 giờ là bắt buộc phải làm mới cache từ Database
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);

                // Lấy dữ liệu từ Database (Chỉ chạy khi Cache trống)
                return await _context.Categories.Where(c => c.IsActive).ToListAsync();
            });

            var model = new HomeIndexViewModel
            {
                Books = pagedBooks,
                SearchTitle = searchTitle,
                SearchAuthor = searchAuthor,
                SearchISBN = searchISBN,
                SelectedCategoryId = categoryId,
                PublishYear = publishYear,
                SortBy = sortBy,
                Categories = new SelectList(categories, "Id", "Name", categoryId),
            };

            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(model);
        }

        // lấy chi tiết sách
        [HttpGet]
        public async Task<IActionResult> GetBookDetails(int id)
        {
            var book = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.BookCopies)
                    .ThenInclude(bc => bc.ShelfTier)
                        .ThenInclude(st => st!.Shelf)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null) return NotFound();

            // ... (Logic đếm sách giữ nguyên như cũ, chỉ là giờ gọi trên biến book) ...
            int totalCopies = book.BookCopies?.Count ?? 0;
            var availableCopies = book.BookCopies?.Where(bc =>
                bc.Status == BookCopyStatus.Available && bc.IsActive && !bc.IsReferenceOnly).ToList();
            int availableCount = availableCopies?.Count ?? 0;

            string suggestedLocation = "Chưa xếp kệ";
            if (availableCopies != null && availableCopies.Any())
            {
                var firstAvailable = availableCopies.First();
                if (firstAvailable.ShelfTier?.Shelf != null)
                    suggestedLocation = $"{firstAvailable.ShelfTier.Shelf.Name} - {firstAvailable.ShelfTier.TierName}";
            }

            return Json(new
            {
                id = book.Id,
                title = book.Title,
                categoryId = book.CategoryId,
                categoryName = book.Category?.Name,
                isbn = book.ISBN,
                author = book.Author,
                publishYear = book.PublishYear,
                publisher = book.Publisher,
                coverImage = string.IsNullOrEmpty(book.CoverImage) ? "/images/no-cover.png" : book.CoverImage,
                isActive = book.IsActive,
                totalCopies = totalCopies,
                availableCount = availableCount,
                suggestedLocation = suggestedLocation
            });
        }

        // Cập nhật tình trạng của một Bản sao vật lý (BookCopy) cụ thể (chỉ dành cho thủ thư hoặc admin)
        [HttpPost]
        [Authorize(Roles = "Librarian, Admin")]
        public async Task<IActionResult> UpdateCopyStatus(int copyId, string status)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(status))
                {
                    return Json(new { success = false, message = "Vui lòng chọn tình trạng sách hợp lệ." });
                }

                // Tìm kiếm bản sao vật lý (BookCopy) theo ID
                var bookCopy = await _context.BookCopies.FindAsync(copyId);
                if (bookCopy == null)
                    return Json(new { success = false, message = "Không tìm thấy bản sao vật lý của sách." });

                BookCopyStatus newStatus;

                // Chuyển đổi chuỗi tiếng Việt hoặc enum sang giá trị BookStatus thực tế
                switch (status.Trim())
                {
                    case "Sẵn sàng":
                    case "Available":
                        newStatus = BookCopyStatus.Available;
                        break;
                    case "Đang mượn":
                    case "Borrowed":
                        newStatus = BookCopyStatus.Borrowed;
                        break;
                    case "Hư hỏng":
                    case "Damaged":
                        newStatus = BookCopyStatus.Damaged;
                        break;
                    case "Mất":
                    case "Lost":
                        newStatus = BookCopyStatus.Lost;
                        break;
                    default:
                        if (!Enum.TryParse<BookCopyStatus>(status, true, out newStatus))
                        {
                            return Json(new { success = false, message = "Tình trạng sách không hợp lệ." });
                        }
                        break;
                }

                // Cập nhật trạng thái cho bản sao vật lý
                bookCopy.Status = newStatus;
                _context.BookCopies.Update(bookCopy);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật tình trạng bản sao thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        //admin lấy danh sách tier theo shelfId để hiển thị trong dropdown khi tạo hoặc chỉnh sửa sách , tạo chỉnh sửa kệ
        [Authorize(Roles = "Admin,Librarian")]
        [HttpGet]
        public async Task<IActionResult> GetAvailableTiers(int shelfId)
        {
            // Viết lại logic GetAvailableTiersByShelfIdAsync trực tiếp
            var tiers = await _context.ShelfTiers.Where(t => t.ShelfId == shelfId && t.IsActive).ToListAsync();
            var availableTiers = new List<ShelfTiers>();

            foreach (var tier in tiers)
            {
                int currentCopiesCount = await _context.BookCopies.CountAsync(bc => bc.ShelfTierId == tier.Id && bc.IsActive);
                if (currentCopiesCount < tier.Capacity)
                {
                    availableTiers.Add(tier);
                }
            }

            return Json(availableTiers.Select(t => new { id = t.Id, tierName = t.TierName }));
        }


        // admin thêm sách mới
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBook([FromForm] CreateBookViewModel model)
        {
            try
            {
                string imagePath = "/images/no-cover.png";
                if (model.CoverImage != null && model.CoverImage.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.CoverImage.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/books", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CoverImage.CopyToAsync(stream);
                    }
                    imagePath = "/images/books/" + fileName;
                }

                // CHỈ TẠO THÔNG TIN TỰA SÁCH CƠ BẢN
                var newBook = new Books
                {
                    Title = model.Title,
                    CategoryId = model.CategoryId,
                    ISBN = model.ISBN,
                    Author = model.Author,
                    PublishYear = model.PublishYear,
                    Publisher = model.Publisher,
                    CoverImage = imagePath,
                    IsActive = true
                };

                // Lưu trực tiếp Tựa Sách (Không sinh ra bản sao vật lý nào ở bước này)
                _context.Books.Add(newBook);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // admin chỉnh sửa sách
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBook([FromForm] EditBookViewModel model)
        {
            try
            {
                // 1. Dùng _context để tìm sách thay vì _bookService
                var existingBook = await _context.Books.FindAsync(model.Id);
                if (existingBook == null)
                    return Json(new { success = false, message = "Không tìm thấy sách để chỉnh sửa." });

                // 2. Logic kiểm tra trùng mã ISBN (Trước đây nằm trong Service, giờ mang lên Controller)
                if (!string.IsNullOrWhiteSpace(model.ISBN))
                {
                    bool isIsbnExist = await _context.Books.AnyAsync(b => b.ISBN == model.ISBN && b.Id != model.Id);
                    if (isIsbnExist)
                        return Json(new { success = false, message = "Mã ISBN này đã bị trùng với một sách khác." });
                }

                string imagePath = model.ExistingCoverImage ?? "/images/no-cover.png";
                if (model.CoverImage != null && model.CoverImage.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.CoverImage.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/books", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CoverImage.CopyToAsync(stream);
                    }
                    imagePath = "/images/books/" + fileName;
                }

                // 3. Chỉ cập nhật các thông tin chung của Tựa sách
                existingBook.Title = model.Title;
                existingBook.CategoryId = model.CategoryId;
                existingBook.ISBN = model.ISBN;
                existingBook.Author = model.Author;
                existingBook.PublishYear = model.PublishYear;
                existingBook.Publisher = model.Publisher;
                existingBook.CoverImage = imagePath;

                // 4. Lưu trực tiếp bằng _context
                _context.Books.Update(existingBook);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        //admin ẩn sách (set IsActive = false)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> HideBook(int id)
        {
            try
            {
                var book = await _context.Books.FindAsync(id);
                if (book == null)
                    return Json(new { success = false, message = "Không tìm thấy sách." });

                // Xóa mềm: Chuyển trạng thái IsActive thành false
                book.IsActive = false;
                _context.Books.Update(book);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        //admin khôi phục sách (set IsActive = true)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> RestoreBook(int id)
        {
            try
            {
                var book = await _context.Books.FindAsync(id);
                if (book == null)
                    return Json(new { success = false, message = "Không tìm thấy sách hoặc có lỗi xảy ra." });

                // Khôi phục: Chuyển trạng thái IsActive thành true
                book.IsActive = true;
                _context.Books.Update(book);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        // đếm số sách trong giỏ hàng của người dùng hiện tại
        [Authorize(Roles = "Admin, Librarian, Reader")]
        [HttpGet]
        public async Task<IActionResult> GetCartItemCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(0);

            // Dùng SUM để tính tổng số lượng (Quantity) thay vì đếm số dòng
            int count = await _context.CartItems
                .Where(c => c.UserId == user.Id)
                .SumAsync(c => (int?)c.Quantity) ?? 0;

            return Json(count);
        }

        // Thêm sách vào giỏ (Đã cập nhật logic áp dụng LibraryRules chuẩn xác)
        [HttpPost]
        public async Task<IActionResult> AddToCartDb(int bookId, int quantity = 1)
        {
            // 1. Kiểm tra đăng nhập
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thêm sách vào giỏ hàng!" });
            }

            // 2. Tính TỔNG số lượng cuốn sách hiện đang có trong giỏ hàng (Cộng dồn cột Quantity)
            // Nếu giỏ hàng trống, SumAsync() đối với kiểu số nguyên nullable có thể lỗi, nên phải gán mặc định về 0
            int totalBooksInCart = await _context.CartItems
                .Where(c => c.UserId == user.Id)
                .Select(c => c.Quantity)
                .SumAsync();


            // 4. Kiểm tra tính hợp lệ của sách
            var book = await _context.Books.FindAsync(bookId);
            if (book == null || !book.IsActive)
            {
                return Json(new { success = false, message = "Sách không tồn tại hoặc đã ngừng hoạt động." });
            }

            // 5. Thêm sách vào giỏ hoặc cập nhật số lượng
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.BookId == bookId);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
                _context.CartItems.Update(cartItem);
            }
            else
            {
                cartItem = new CartItems
                {
                    UserId = user.Id,
                    BookId = bookId,
                    Quantity = quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();

            // 6. Tính tổng số sách trong giỏ để cập nhật Badge hiển thị trên Header
            int newCount = await _context.CartItems
                .Where(c => c.UserId == user.Id)
                .Select(c => c.Quantity)
                .SumAsync();

            return Json(new { success = true, newCount = newCount });
        }

        //trang error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)] //không lưu cache  
        public IActionResult Error()
        {
            return View();
        }
    }
}