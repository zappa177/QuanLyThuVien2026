using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Application.Interfaces.IServices;
using QuanLyThuVien.Domain.Entities;
using QuanLyThuVien.Domain.Entities.Identity;
using QuanLyThuVien.Domain.Enums;
using QuanLyThuVien.Infrastructure.Data;
using QuanLyThuVien.Web.EnumExtensions;
using QuanLyThuVien.Web.Models;

namespace QuanLyThuVien.Web.Controllers
{
    [Authorize] // Bắt buộc đăng nhập mới được vào trang chủ
    public class HomeController : Controller
    {
        private readonly IBookService _bookService;
        private readonly ICategoryService _categoryService;
        private readonly IShelfService _shelfService;
        private readonly IShelfTierService _shelfTierService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        public HomeController(IBookService bookService, ICategoryService categoryService, IShelfService shelfService, IShelfTierService shelfTierService, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _bookService = bookService;
            _categoryService = categoryService;
            _shelfService = shelfService;
            _shelfTierService = shelfTierService;
            _context = dbContext;
            _userManager = userManager;
            _configuration = configuration;
        }

        //trang chủ index là trang hiển thị sách , có phân trang, tìm kiếm, lọc, sắp xếp
        [HttpGet]
        public async Task<IActionResult> Index(
            string searchTitle, string searchAuthor, string searchISBN,
            int? categoryId, int? publishYear,
            string sortBy = "PublishYear", int pageNumber = 1)
        {
            int pageSize = 9; // Số lượng sách hiển thị trên mỗi trang tạo grid 3x3

            //phân biệc role để hiện sách 
            bool? isActiveFilter = User.IsInRole("Admin") ? null : true; //nếu là admin thì hiện tất cả sách, còn lại chỉ hiện sách đang hoạt động
            bool onlyAvailable = User.IsInRole("Admin") || User.IsInRole("Librarian") ? false : true; //nếu là admin hoặc thủ thư thì hiện tất cả sách, còn lại chỉ hiện sách đang sẵn sàng

            // Lấy dữ liệu sách phân trang từ Service
            var pagedBooks = await _bookService.GetPagedBooksAsync(searchTitle, searchAuthor, searchISBN, publishYear, pageNumber, pageSize, categoryId, sortBy, isActiveFilter, onlyAvailable);

            // Lấy dữ liệu cho Dropdown
            var categories = await _categoryService.GetAllCategoriesAsync() ?? new List<Categories>();
            var shelves = await _shelfService.GetShelvesWithTiersAsync() ?? new List<Shelves>();

            // Đổ dữ liệu vào ViewModel
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
            ViewBag.Shelves = new SelectList(shelves, "Id", "Name");

            return View(model);
        }

        //lấy chi tiết sách
        [HttpGet]
        [AllowAnonymous] // Hoặc giữ nguyên [Authorize] mặc định của Controller
        public async Task<IActionResult> GetBookDetails(int id)
        {
            var book = await _bookService.GetBookByIdAsync(id);
            if (book == null) return NotFound();

            var data = new
            {
                id = book.Id,
                title = book.Title,
                categoryId = book.CategoryId,
                categoryName = book.Category?.Name,
                shelfId = book.ShelfTier?.ShelfId,
                shelfName = book.ShelfTier?.Shelf?.Name,
                tierId = book.ShelfTierId,
                tierName = book.ShelfTier?.TierName,
                isbn = book.ISBN,
                author = book.Author,
                publishYear = book.PublishYear,
                publisher = book.Publisher,
                status = (int)book.Status,
                statusName = book.Status.ToVietnamese(), // Chuyển đổi Enum ra string
                coverImage = book.CoverImage,
                isActive = book.IsActive
            };
            return Json(data);
        }

        // Cập nhật tình trạng sách (chỉ dành cho thủ thư)
        [HttpPost]
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(status))
                {
                    return Json(new { success = false, message = "Vui lòng chọn tình trạng sách hợp lệ." });
                }
                var book = await _bookService.GetBookByIdAsync(id);
                if (book == null) return Json(new { success = false, message = "Không tìm thấy sách." });


                int enumValue = 0;

                switch (status.Trim())
                {
                    case "Sẵn sàng":
                        enumValue = (int)BookStatus.Available; // enum bookstatus
                        break;
                    case "Đang mượn":
                        enumValue = (int)BookStatus.Borrowed;
                        break;
                    case "Hư hỏng":
                        enumValue = (int)BookStatus.Damaged;
                        break;
                    case "Mất":
                        enumValue = (int)BookStatus.Lost;
                        break;
                    default:
                        if (Enum.TryParse(book.Status.GetType(), status, out var parsed))
                        {
                            book.Status = (dynamic)parsed;
                            await _bookService.UpdateBookAsync(book);
                            return Json(new { success = true });
                        }
                        return Json(new { success = false, message = "Tình trạng sách không hợp lệ." });
                }

                // Chuyển đổi số int thành enum và gán lại cho book.Status
                book.Status = (dynamic)Enum.ToObject(book.Status.GetType(), enumValue);

                await _bookService.UpdateBookAsync(book);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        //admin lấy danh sách tier theo shelfId để hiển thị trong dropdown khi tạo hoặc chỉnh sửa sách , tạo chỉnh sửa kệ
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAvailableTiers(int shelfId)
        {
            var tiers = await _shelfTierService.GetAvailableTiersByShelfIdAsync(shelfId);
            var result = tiers.Select(t => new { id = t.Id, tierName = t.TierName });
            return Json(result);
        }
        //****************************************************************************
        //
        // Hàm phụ trợ để xử lý upload ảnh lên Azure gọn gàng hơn
        ////////////////////private async Task<string> UploadImageToAzureAsync(IFormFile file)
        ////////////////////{
        ////////////////////    string connectionString = _configuration.GetConnectionString("AzureStorage");
        ////////////////////    string containerName = _configuration["AzureBlob:ContainerName"];

        ////////////////////    var blobServiceClient = new BlobServiceClient(connectionString);
        ////////////////////    var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

        ////////////////////    // Đảm bảo container tồn tại
        ////////////////////    await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        ////////////////////    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        ////////////////////    var blobClient = blobContainerClient.GetBlobClient(fileName);

        ////////////////////    using (var stream = file.OpenReadStream())
        ////////////////////    {
        ////////////////////        // Upload và set ContentType để trình duyệt hiểu đây là hình ảnh
        ////////////////////        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
        ////////////////////    }

        ////////////////////    // Trả về đường dẫn URL tuyệt đối của ảnh trên Azure
        ////////////////////    return blobClient.Uri.ToString();
        ////////////////////}

        ////////////////////// --- ADMIN THÊM SÁCH MỚI ---
        ////////////////////[Authorize(Roles = "Admin")]
        ////////////////////[HttpPost]
        ////////////////////[ValidateAntiForgeryToken]
        ////////////////////public async Task<IActionResult> AddBook([FromForm] CreateBookViewModel model)
        ////////////////////{
        ////////////////////    try
        ////////////////////    {
        ////////////////////        string imagePath = "/images/no-cover.png"; // Ảnh mặc định nếu không upload

        ////////////////////        if (model.CoverImage != null && model.CoverImage.Length > 0)
        ////////////////////        {
        ////////////////////            // Gọi hàm upload lên Azure thay vì lưu local
        ////////////////////            imagePath = await UploadImageToAzureAsync(model.CoverImage);
        ////////////////////        }

        ////////////////////        var newBook = new Books
        ////////////////////        {
        ////////////////////            Title = model.Title,
        ////////////////////            CategoryId = model.CategoryId,
        ////////////////////            ShelfTierId = model.ShelfTierId,
        ////////////////////            ISBN = model.ISBN,
        ////////////////////            Author = model.Author,
        ////////////////////            PublishYear = model.PublishYear,
        ////////////////////            Publisher = model.Publisher,
        ////////////////////            Status = model.Status,
        ////////////////////            CoverImage = imagePath, // Lúc này imagePath là một URL (VD: https://storage.../bookcovers/abc.jpg)
        ////////////////////            IsActive = true
        ////////////////////        };

        ////////////////////        var result = await _bookService.CreateBookAsync(newBook);
        ////////////////////        if (result) return Json(new { success = true });

        ////////////////////        return Json(new { success = false, message = "Không thể lưu sách vào cơ sở dữ liệu." });
        ////////////////////    }
        ////////////////////    catch (Exception ex)
        ////////////////////    {
        ////////////////////        return Json(new { success = false, message = ex.Message });
        ////////////////////    }
        ////////////////////}

        ////////////////////// --- ADMIN CHỈNH SỬA SÁCH ---
        ////////////////////[Authorize(Roles = "Admin")]
        ////////////////////[HttpPost]
        ////////////////////[ValidateAntiForgeryToken]
        ////////////////////public async Task<IActionResult> EditBook([FromForm] EditBookViewModel model)
        ////////////////////{
        ////////////////////    try
        ////////////////////    {
        ////////////////////        var existingBook = await _bookService.GetBookByIdAsync(model.Id);
        ////////////////////        if (existingBook == null)
        ////////////////////            return Json(new { success = false, message = "Không tìm thấy sách để chỉnh sửa." });

        ////////////////////        string imagePath = model.ExistingCoverImage ?? "/images/no-cover.png";

        ////////////////////        if (model.CoverImage != null && model.CoverImage.Length > 0)
        ////////////////////        {
        ////////////////////            // Gọi hàm upload lên Azure
        ////////////////////            imagePath = await UploadImageToAzureAsync(model.CoverImage);

        ////////////////////            // Tùy chọn nâng cao (Không bắt buộc):
        ////////////////////            // Nếu sách đã có ảnh cũ trên Azure, bạn có thể viết thêm code xóa ảnh cũ đi để tiết kiệm dung lượng.
        ////////////////////        }

        ////////////////////        existingBook.Title = model.Title;
        ////////////////////        existingBook.CategoryId = model.CategoryId;
        ////////////////////        existingBook.ShelfTierId = model.ShelfTierId;
        ////////////////////        existingBook.Author = model.Author;
        ////////////////////        existingBook.PublishYear = model.PublishYear;
        ////////////////////        existingBook.Publisher = model.Publisher;
        ////////////////////        existingBook.Status = model.Status;
        ////////////////////        existingBook.CoverImage = imagePath; // Cập nhật URL mới

        ////////////////////        await _bookService.UpdateBookAsync(existingBook);
        ////////////////////        return Json(new { success = true });
        ////////////////////    }
        ////////////////////    catch (Exception ex)
        ////////////////////    {
        ////////////////////        return Json(new { success = false, message = ex.Message });
        ////////////////////    }
        ////////////////////}
        //*****************************************************************************
        //

        //admin thêm sách mới
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

                var newBook = new Books
                {
                    Title = model.Title,
                    CategoryId = model.CategoryId,
                    ShelfTierId = model.ShelfTierId,
                    ISBN = model.ISBN,
                    Author = model.Author,
                    PublishYear = model.PublishYear,
                    Publisher = model.Publisher,
                    Status = model.Status,
                    CoverImage = imagePath,
                    IsActive = true
                };

                var result = await _bookService.CreateBookAsync(newBook);
                if (result) return Json(new { success = true });

                return Json(new { success = false, message = "Không thể lưu sách vào cơ sở dữ liệu." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        //admin chỉnh sửa sách
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBook([FromForm] EditBookViewModel model)
        {
            try
            {
                var existingBook = await _bookService.GetBookByIdAsync(model.Id);
                if (existingBook == null)
                    return Json(new { success = false, message = "Không tìm thấy sách để chỉnh sửa." });

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

                existingBook.Title = model.Title;
                existingBook.CategoryId = model.CategoryId;
                existingBook.ShelfTierId = model.ShelfTierId;
                existingBook.ISBN = model.ISBN;
                existingBook.Author = model.Author;
                existingBook.PublishYear = model.PublishYear;
                existingBook.Publisher = model.Publisher;
                existingBook.Status = model.Status;
                existingBook.CoverImage = imagePath;

                await _bookService.UpdateBookAsync(existingBook);
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
                await _bookService.DeleteBookAsync(id); // Giả định Service Delete đang set IsActive = false
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
                bool isRestored = await _bookService.RestoreBookAsync(id);
                if (isRestored) return Json(new { success = true });

                return Json(new { success = false, message = "Không tìm thấy sách hoặc có lỗi xảy ra." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        //đếm số sách trong giỏ hàng của người dùng hiện tại
        [Authorize(Roles = "Librarian, Reader")]
        [HttpGet]
        public async Task<IActionResult> GetCartItemCount()
        {
            // Lấy thông tin tài khoản đang đăng nhập
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(0);

            // Tìm hồ sơ mượn sách của tài khoản này
            var reader = await _context.Readers.FirstOrDefaultAsync(r => r.ApplicationUserId == user.Id);
            if (reader == null) return Json(0); // Chưa có hồ sơ thì giỏ hàng = 0

            // Đếm số lượng sách đang có trong giỏ
            int count = await _context.CartItems.CountAsync(c => c.ReaderId == reader.Id);
            return Json(count);
        }

        // 2. HÀM THÊM SÁCH VÀO GIỎ
        [HttpPost]
        public async Task<IActionResult> AddToCartDb(int bookId)
        {
            var user = await _userManager.GetUserAsync(User);

            // Lấy hồ sơ mượn sách
            var reader = await _context.Readers.FirstOrDefaultAsync(r => r.ApplicationUserId == user!.Id);

            // Nếu là Thủ thư chưa có hồ sơ mượn, tự động tạo mới 1 lần duy nhất
            if (reader == null && User.IsInRole("Librarian"))
            {
                reader = new Readers { ApplicationUserId = user!.Id, StudentCode = "LIB-" + DateTime.Now.ToString("ddMMyyHHmm") };
                _context.Readers.Add(reader);
                await _context.SaveChangesAsync();
            }
            else if (reader == null)
            {
                return Json(new { success = false, message = "Không tìm thấy hồ sơ người mượn." });
            }

            // --- KIỂM TRA ĐIỀU KIỆN ---
            // 1. Kiểm tra giới hạn 2 cuốn
            int currentCount = await _context.CartItems.CountAsync(c => c.ReaderId == reader.Id);
            if (currentCount >= 2)
            {
                return Json(new { success = false, message = "Giỏ hàng đã đầy (tối đa 2 cuốn)." });
            }

            // 2. Kiểm tra sách có bị trùng không
            bool isExist = await _context.CartItems.AnyAsync(c => c.ReaderId == reader.Id && c.BookId == bookId);
            if (isExist)
            {
                return Json(new { success = false, message = "Sách này đã có trong giỏ hàng!" });
            }

            // --- LƯU VÀO DATABASE ---
            var newItem = new CartItems
            {
                ReaderId = reader.Id,
                BookId = bookId
            };

            _context.CartItems.Add(newItem);
            await _context.SaveChangesAsync();

            // Trả về thành công kèm theo số lượng sách mới nhất
            return Json(new { success = true, newCount = currentCount + 1 });
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)] //không lưu cache  
        public IActionResult Error()
        {
            return View();
        }
    }
}