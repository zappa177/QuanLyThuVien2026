using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Domain.Entities;
using QuanLyThuVien.Domain.Entities.Identity;
using QuanLyThuVien.Infrastructure.Data;
using QuanLyThuVien.Web.Models;

namespace QuanLyThuVien.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // ==========================================
        // 1. HIỂN THỊ DANH SÁCH & TÌM KIẾM
        // ==========================================
        public async Task<IActionResult> Index(string searchName, string searchReaderId, string sortOrder)
        {
            var allUsers = await _userManager.Users.ToListAsync();
            var userList = new List<UserListViewModel>();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                string mainRole = roles.FirstOrDefault() ?? "";

                if (mainRole != "Reader" && mainRole != "Librarian") continue;

                bool isHidden = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow;
                string readerId = "";

                if (mainRole == "Reader")
                {
                    // Lấy StudentCode từ bảng Readers
                    var readerInfo = await _context.Readers.FirstOrDefaultAsync(r => r.ApplicationUserId == user.Id);
                    if (readerInfo != null) readerId = readerInfo.StudentCode;
                }

                userList.Add(new UserListViewModel
                {
                    Id = user.Id.ToString(),
                    FullName = user.FullName ?? user.UserName, // Hiển thị FullName
                    Role = mainRole == "Reader" ? "Khách" : "Thủ thư",
                    ReaderId = readerId,
                    IsHidden = isHidden
                });
            }

            // Tìm kiếm theo tên (FullName)
            if (!string.IsNullOrEmpty(searchName))
                userList = userList.Where(u => u.FullName.Contains(searchName, StringComparison.OrdinalIgnoreCase)).ToList();

            // Tìm kiếm theo mã (StudentCode)
            if (!string.IsNullOrEmpty(searchReaderId))
                userList = userList.Where(u => !string.IsNullOrEmpty(u.ReaderId) && u.ReaderId.Contains(searchReaderId, StringComparison.OrdinalIgnoreCase)).ToList();

            // Sắp xếp
            if (sortOrder == "name_desc")
                userList = userList.OrderByDescending(u => u.FullName).ToList();
            else
                userList = userList.OrderBy(u => u.FullName).ToList();

            ViewBag.SearchName = searchName;
            ViewBag.SearchReaderId = searchReaderId;
            ViewBag.SortOrder = sortOrder;

            return View(userList);
        }

        // ==========================================
        // 2. THÊM NGƯỜI DÙNG (POST)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> Create(UserFormViewModel model)
        {
            if (string.IsNullOrEmpty(model.UserName))
                ModelState.AddModelError("UserName", "Vui lòng nhập tên người dùng.");
            if (string.IsNullOrEmpty(model.Password))
                ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu khi thêm mới.");
            if (model.Role == "Reader" && string.IsNullOrEmpty(model.ReaderId))
                ModelState.AddModelError("ReaderId", "Vui lòng nhập mã người dùng cho Khách.");

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserName, // Dùng làm tài khoản đăng nhập Identity
                    FullName = model.UserName, // Gán luôn làm FullName hiển thị
                    Email = $"{model.UserName}@thuvien.com" // Identity thường yêu cầu Email (nếu hệ thống bạn cấu hình require)
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.Role);

                    // Nếu là Khách -> lưu vào bảng Readers với StudentCode
                    if (model.Role == "Reader")
                    {
                        var reader = new Readers
                        {
                            ApplicationUserId = user.Id,
                            StudentCode = model.ReaderId
                        };
                        _context.Readers.Add(reader);
                        await _context.SaveChangesAsync();
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 3. SỬA NGƯỜI DÙNG (POST)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> Edit(UserFormViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            // 1. Cập nhật mật khẩu (nếu có nhập)
            if (!string.IsNullOrEmpty(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, model.Password);
            }

            // 2. Cập nhật Vai trò và Mã người dùng
            var currentRoles = await _userManager.GetRolesAsync(user);
            bool roleChanged = !currentRoles.Contains(model.Role);

            if (roleChanged)
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.Role);

                var reader = await _context.Readers.FirstOrDefaultAsync(r => r.ApplicationUserId == user.Id);

                if (model.Role == "Reader") // Đổi từ Thủ thư -> Khách
                {
                    if (reader == null)
                    {
                        _context.Readers.Add(new Readers { ApplicationUserId = user.Id, StudentCode = model.ReaderId });
                    }
                    else
                    {
                        reader.StudentCode = model.ReaderId;
                    }
                }
                else // Đổi từ Khách -> Thủ thư
                {
                    if (reader != null) _context.Readers.Remove(reader);
                }
                await _context.SaveChangesAsync();
            }
            else if (model.Role == "Reader") // Role giữ nguyên là Khách, nhưng đổi Mã người dùng
            {
                var reader = await _context.Readers.FirstOrDefaultAsync(r => r.ApplicationUserId == user.Id);
                if (reader != null)
                {
                    reader.StudentCode = model.ReaderId;
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 4. ẨN/HIỆN NGƯỜI DÙNG (Khóa/Mở khóa)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                bool isHidden = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow;

                if (isHidden)
                    await _userManager.SetLockoutEndDateAsync(user, null); // Mở khóa (Hiện)
                else
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue); // Khóa (Ẩn)
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
