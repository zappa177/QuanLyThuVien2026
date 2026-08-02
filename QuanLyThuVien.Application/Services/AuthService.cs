using Microsoft.AspNetCore.Identity;
using QuanLyThuVien.Application.Interfaces.IServices;
using QuanLyThuVien.Domain.Entities.Identity;

namespace QuanLyThuVien.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthService(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public async Task<SignInResult> LoginAsync(string username, string password)
        {
            //tìm user theo username hoặc email(có thể người dùng đăng nhập bằng email)
            var user = await _userManager.FindByNameAsync(username)
                       ?? await _userManager.FindByEmailAsync(username);

            // Nếu không tìm thấy user, trả về SignInResult thất bại có sẵn của Identity
            if (user == null)
            {
                return SignInResult.Failed;
            }

            // Kiểm tra mật khẩu và đăng nhập
            // isPersistent: false -> không lưu cookie lâu dài
            // lockoutOnFailure: false -> không khóa tài khoản khi sai
            return await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync(); // Đăng xuất người dùng
        }
    }
}
