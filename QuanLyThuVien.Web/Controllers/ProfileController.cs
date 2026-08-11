using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien.Web.Data; // Thay bằng namespace DbContext thực tế của bạn
using QuanLyThuVien.Web.Entities.Identity;
using QuanLyThuVien.Web.Models;

namespace QuanLyThuVien.Web.Controllers
{
    [Authorize] // Bắt buộc đăng nhập mới được truy cập
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ProfileController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        /// Lấy thông tin cá nhân (áp dụng chung cho mọi Role)
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            // Lấy thông tin user đang đăng nhập hiện tại
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var model = new UserProfileViewModel
            {
                FullName = user.FullName ?? string.Empty,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                UserCode = user.UserCode,
                Position = user.Position
            };

            return View(model);
        }

        // Cập nhật thông tin cá nhân
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // AI CŨNG ĐƯỢC SỬA: Tên, Email, SĐT
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            // PHÂN QUYỀN ĐẶC BIỆT: Khách và Thủ thư không được tự sửa Mã và Vị trí/Lớp
            if (User.IsInRole("Admin"))
            {
                user.UserCode = model.UserCode;
                user.Position = model.Position;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            ViewBag.SuccessMessage = "Cập nhật thông tin thành công!";

            // Gán lại dữ liệu cũ cho Model để trả về View (tránh việc view hiển thị dữ liệu người dùng cố tình F12 nhập)
            model.UserCode = user.UserCode;
            model.Position = user.Position;

            return View(model);
        }
    }
}