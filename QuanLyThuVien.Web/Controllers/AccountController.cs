using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien.Application.Interfaces.IServices;
using QuanLyThuVien.Domain.Entities.Identity;
using QuanLyThuVien.Web.Models;

namespace QuanLyThuVien.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(IAuthService authService, UserManager<ApplicationUser> userManager)
        {
            _authService = authService;
            _userManager = userManager;
        }

        // GET: /Account/Login
        [Route("/")]
        [Route("dang-nhap")]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [Route("/")]
        [Route("dang-nhap")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            // 1. Kiểm tra xem người dùng đã nhập đầy đủ ô dữ liệu chưa
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 2. Gọi AuthService kiểm tra tài khoản (nhận về SignInResult)
            var result = await _authService.LoginAsync(model.Username, model.Password);

            // 3. NẾU THÀNH CÔNG -> Chuyển hướng sang Trang chủ (hoặc link trang trước đó)
            if (result.Succeeded)
            {
                // 1. Nếu có returnUrl (người dùng đang xem dở trang nào đó bị văng ra), thì trả về trang đó
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // 2. KHÔNG CẦN CHIA ROLE Ở ĐÂY. 
                // Dù là Admin, Thủ thư hay Khách, tất cả đều đẩy hết về HomeController!
                return RedirectToAction("Index", "Home");
            }

            // 4. NẾU THẤT BẠI -> Thêm câu lỗi vào ModelState để đẩy ra màn hình View
            ModelState.AddModelError(string.Empty, "Sai tên đăng nhập hoặc mật khẩu");
            return View(model);
        }

        // POST: /Account/Logout
        [Route("dang-xuat")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return RedirectToAction("Login", "Account");
        }
        [Authorize]
        // 1. Hiển thị form
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // 2. Xử lý khi nhấn nút "Đổi mật khẩu"
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Lấy thông tin tài khoản đang đăng nhập hiện tại
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Gọi hàm của Identity: Tự động kiểm tra mật khẩu cũ và lưu mật khẩu mới
            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

            if (result.Succeeded)
            {
                ViewBag.SuccessMessage = "Đổi mật khẩu thành công!";
                ModelState.Clear(); // Xóa dữ liệu cũ trên form
                return View();
            }

            // Nếu mật khẩu cũ sai, hoặc mật khẩu mới quá yếu, Identity sẽ báo lỗi tại đây
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
        // Thêm vào trong AccountController.cs
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
