using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Domain.Entities.Identity;
using QuanLyThuVien.Infrastructure.Data; // Thay bằng namespace DbContext thực tế của bạn
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

        //lấy thông tin cá nhân của thủ thư , reader
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            // Lấy thông tin user đang đăng nhập hiện tại
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Kiểm tra xem user thuộc nhóm quyền nào
            var roles = await _userManager.GetRolesAsync(user);
            bool isReader = roles.Contains("Reader");

            var model = new UserProfileViewModel
            {
                FullName = user.FullName ?? user.UserName ?? string.Empty,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsReader = isReader
            };

            // Nếu user là Khách (Reader), lấy thêm thông tin từ bảng Readers (như Lớp)
            if (isReader)
            {
                var readerInfo = await _context.Readers
                    .FirstOrDefaultAsync(r => r.ApplicationUserId == user.Id);

                if (readerInfo != null)
                {
                    model.ClassName = readerInfo.ClassName;
                }
            }

            return View(model);
        }

        //cập nhật thông tin cá nhân của thủ thư , reader
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            bool isReader = roles.Contains("Reader");
            model.IsReader = isReader;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Cập nhật thông tin vào bảng ApplicationUser (Identity)
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // Nếu là Khách, cập nhật thêm thông tin riêng vào bảng Readers
            if (isReader)
            {
                var readerInfo = await _context.Readers
                    .FirstOrDefaultAsync(r => r.ApplicationUserId == user.Id);

                if (readerInfo != null)
                {
                    readerInfo.ClassName = model.ClassName; // Cập nhật tên lớp
                    _context.Readers.Update(readerInfo);
                    await _context.SaveChangesAsync();
                }
            }

            ViewBag.SuccessMessage = "Cập nhật thông tin thành công!";
            return View(model);
        }
    }
}