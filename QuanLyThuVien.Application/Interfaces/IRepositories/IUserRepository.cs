using Microsoft.AspNetCore.Identity;
using QuanLyThuVien.Domain.Common;
using QuanLyThuVien.Domain.Entities.Identity;

namespace QuanLyThuVien.Application.Interfaces.IRepositories
{
    public interface IUserRepository
    {
        // Lấy danh sách có phân trang và tìm kiếm
        Task<PagedResult<ApplicationUser>> GetPagedUsersAsync(int pageNumber, int pageSize, string? searchTerm);

        Task<ApplicationUser?> GetByIdAsync(Guid userId);   // Lấy người dùng theo Id

        // Thêm người dùng mới kèm mật khẩu và gán Role (Admin, Librarian, Reader)
        Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password, string role);

        // Cập nhật thông tin cơ bản
        Task<IdentityResult> UpdateUserAsync(ApplicationUser user);

        // Cập nhật quyền (Role)
        Task<IdentityResult> UpdateUserRoleAsync(ApplicationUser user, string newRole);

        // Đổi mật khẩu trực tiếp (Dành cho Admin reset mật khẩu)
        Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string newPassword);

        // Bật/Tắt trạng thái hoạt động (Khóa tài khoản)
        Task<IdentityResult> ToggleActiveStatusAsync(Guid userId);
    }
}
