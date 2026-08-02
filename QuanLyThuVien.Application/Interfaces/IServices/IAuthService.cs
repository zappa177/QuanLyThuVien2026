using Microsoft.AspNetCore.Identity;

namespace QuanLyThuVien.Application.Interfaces.IServices
{
    public interface IAuthService
    {
        Task<SignInResult> LoginAsync(string username, string password);    //đăng nhập
        Task LogoutAsync(); //đăng xuất
    }
}
