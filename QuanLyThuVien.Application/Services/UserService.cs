using Microsoft.AspNetCore.Identity;
using QuanLyThuVien.Application.Interfaces.IRepositories;
using QuanLyThuVien.Application.Interfaces.IServices;
using QuanLyThuVien.Domain.Common;
using QuanLyThuVien.Domain.Entities.Identity;

namespace QuanLyThuVien.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;

        public UserService(IUserRepository userRepo) => _userRepo = userRepo;
        // Lấy danh sách người dùng theo phân trang và tìm kiếm
        public async Task<PagedResult<ApplicationUser>> GetPagedUsersAsync(int pageNumber, int pageSize, string? searchTerm)
            => await _userRepo.GetPagedUsersAsync(pageNumber, pageSize, searchTerm);
        // Lấy người dùng theo ID
        public async Task<ApplicationUser?> GetUserByIdAsync(Guid userId) => await _userRepo.GetByIdAsync(userId);
        // Lấy người dùng theo tên đăng nhập
        public async Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password, string role)
            => await _userRepo.CreateUserAsync(user, password, role);
        // Cập nhật thông tin người dùng
        public async Task<IdentityResult> UpdateUserRoleAsync(ApplicationUser user, string newRole)
            => await _userRepo.UpdateUserRoleAsync(user, newRole);
        // reset mật khẩu người dùng
        public async Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string newPassword)
            => await _userRepo.ResetPasswordAsync(user, newPassword);
        // Xóa người dùng(khóa tài khoản)
        public async Task<IdentityResult> ToggleActiveStatusAsync(Guid userId)
            => await _userRepo.ToggleActiveStatusAsync(userId);
    }
}
