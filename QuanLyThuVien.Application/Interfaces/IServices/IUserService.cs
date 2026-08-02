using Microsoft.AspNetCore.Identity;
using QuanLyThuVien.Domain.Common;
using QuanLyThuVien.Domain.Entities.Identity;

namespace QuanLyThuVien.Application.Interfaces.IServices
{
    public interface IUserService
    {
        Task<PagedResult<ApplicationUser>> GetPagedUsersAsync(int pageNumber, int pageSize, string? searchTerm);    //lấy người dùng theo phân trang và tìm kiếm
        Task<ApplicationUser?> GetUserByIdAsync(Guid userId);   //lấy người dùng theo id
        Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password, string role);   //thêm người dùng mới kèm mật khẩu và gán Role (Admin, Librarian, Reader)
        Task<IdentityResult> UpdateUserRoleAsync(ApplicationUser user, string newRole); //cập nhật quyền (Role)
        Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string newPassword);  //đổi mật khẩu trực tiếp 
        Task<IdentityResult> ToggleActiveStatusAsync(Guid userId);  //bật/tắt trạng thái hoạt động (Khóa tài khoản)
    }
}
