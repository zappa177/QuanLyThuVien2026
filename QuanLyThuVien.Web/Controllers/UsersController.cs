using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Web.Data;
using QuanLyThuVien.Web.Entities.Identity;
using QuanLyThuVien.Web.Models;

namespace QuanLyThuVien.Web.Controllers
{
    [Authorize(Roles = "Admin")] // Chỉ có Admin được quản lý người dùng
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // Hiển thị danh sách người dùng (Reader và Librarian)
        public async Task<IActionResult> Index(string? searchName, string? searchUserCode, string? sortOrder)
        {
            var query = from user in _context.Users
                        join userRole in _context.UserRoles on user.Id equals userRole.UserId
                        join role in _context.Roles on userRole.RoleId equals role.Id
                        where role.Name == "Reader" || role.Name == "Librarian"
                        select new
                        {
                            UserId = user.Id,
                            FullName = user.FullName ?? user.UserName,
                            UserCode = user.UserCode ?? "",
                            Position = user.Position ?? "", // <-- MỚI LẤY THÊM POSITION
                            RoleName = role.Name,
                            LockoutEnd = user.LockoutEnd
                        };

            // Tìm kiếm theo tên
            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(u => u.FullName!.Contains(searchName));
            }

            // Tìm theo mã người dùng (UserCode)
            if (!string.IsNullOrEmpty(searchUserCode))
            {
                query = query.Where(u => !string.IsNullOrEmpty(u.UserCode) && u.UserCode.Contains(searchUserCode));
            }

            // Sắp xếp
            if (sortOrder == "name_desc")
            {
                query = query.OrderByDescending(u => u.FullName);
            }
            else
            {
                query = query.OrderBy(u => u.FullName);
            }

            var rawData = await query.AsNoTracking().ToListAsync();

            // Map dữ liệu vào UserListViewModel
            var userList = rawData.Select(u => new UserListViewModel
            {
                Id = u.UserId.ToString(),
                FullName = u.FullName ?? string.Empty,
                Role = u.RoleName == "Reader" ? "Khách" : "Thủ thư",
                UserCode = u.UserCode,
                Position = u.Position, // <-- MỚI MAP THÊM POSITION
                IsHidden = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow
            }).ToList();

            // Truyền sang view để giữ giá trị ở các ô tìm kiếm và sắp xếp
            ViewBag.SearchName = searchName;
            ViewBag.SearchUserCode = searchUserCode;
            ViewBag.SortOrder = sortOrder;

            return View(userList);
        }


        //them người dùng

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserFormViewModel model)
        {
            if (string.IsNullOrEmpty(model.UserName))
                ModelState.AddModelError("UserName", "Vui lòng nhập tên đăng nhập.");
            if (string.IsNullOrEmpty(model.Password))
                ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu khi thêm mới.");
            if (string.IsNullOrEmpty(model.UserCode))
                ModelState.AddModelError("UserCode", "Vui lòng nhập mã định danh (Mã thẻ / Mã nhân viên).");

            // Nếu form gửi lên thiếu dữ liệu bắt buộc
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["ErrorMessage"] = errors;
                return RedirectToAction(nameof(Index));
            }

            // KIỂM TRA TRÙNG TÊN ĐĂNG NHẬP
            var existingUser = await _userManager.FindByNameAsync(model.UserName);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = $"Tên đăng nhập '{model.UserName}' đã tồn tại trong hệ thống. Vui lòng chọn tên khác.";
                return RedirectToAction(nameof(Index));
            }

            // KIỂM TRA TRÙNG MÃ ĐỊNH DANH (UserCode) nếu có nhập
            if (!string.IsNullOrEmpty(model.UserCode))
            {
                var existingCode = await _context.Users.FirstOrDefaultAsync(u => u.UserCode == model.UserCode);
                if (existingCode != null)
                {
                    TempData["ErrorMessage"] = $"Mã định danh '{model.UserCode}' đã được sử dụng cho một tài khoản khác.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Khởi tạo ApplicationUser
            var user = new ApplicationUser
            {
                UserName = model.UserName,
                FullName = model.UserName,
                UserCode = model.UserCode,
                Position = model.Position, // <-- MỚI LƯU POSITION
                Email = $"{model.UserName}@demoproject.me"
            };

            var result = await _userManager.CreateAsync(user, model.Password!);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);
                TempData["SuccessMessage"] = "Thêm người dùng thành công!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                // Bắt các lỗi khác từ Identity (ví dụ: Mật khẩu quá yếu)
                var identityErrors = string.Join(" ", result.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = $"Lỗi tạo tài khoản: {identityErrors}";
                return RedirectToAction(nameof(Index));
            }
        }


        // SỬA THÔNG TIN NGƯỜI DÙNG 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserFormViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id!);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Index));
            }

            // KIỂM TRA TRÙNG MÃ ĐỊNH DANH KHI SỬA
            if (!string.IsNullOrEmpty(model.UserCode))
            {
                // Kiểm tra xem UserCode này có bị ai KHÁC (khác user.Id hiện tại) dùng chưa
                var existingCode = await _context.Users.FirstOrDefaultAsync(u => u.UserCode == model.UserCode && u.Id != user.Id);
                if (existingCode != null)
                {
                    TempData["ErrorMessage"] = $"Mã định danh '{model.UserCode}' đã được sử dụng bởi người khác.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Cập nhật mật khẩu (nếu có nhập)
            if (!string.IsNullOrEmpty(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                if (!passResult.Succeeded)
                {
                    var passErrors = string.Join(" ", passResult.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Lỗi đổi mật khẩu: {passErrors}";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Cập nhật Role nếu có thay đổi
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(model.Role))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            // Cập nhật UserCode và Position
            user.UserCode = model.UserCode;
            user.Position = model.Position; // <-- MỚI LƯU POSITION
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = "Cập nhật thông tin người dùng thành công!";
            return RedirectToAction(nameof(Index));
        }


        // KHÓA / MỞ KHÓA TÀI KHOẢN 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                bool isHidden = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow;

                if (isHidden)
                    await _userManager.SetLockoutEndDateAsync(user, null); // Mở khóa
                else
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue); // Khóa vĩnh viễn
            }
            return RedirectToAction(nameof(Index));
        }
    }
}