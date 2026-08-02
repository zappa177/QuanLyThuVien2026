using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Application.Interfaces.IRepositories;
using QuanLyThuVien.Domain.Common;
using QuanLyThuVien.Domain.Entities.Identity;


namespace QuanLyThuVien.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public UserRepository(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }
        // Lấy danh sách người dùng theo phân trang và tìm kiếm
        public async Task<PagedResult<ApplicationUser>> GetPagedUsersAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            // Lấy danh sách người dùng từ UserManager
            var query = _userManager.Users.AsQueryable();
            // Nếu có từ khóa tìm kiếm, lọc danh sách người dùng theo UserName, Email hoặc PhoneNumber

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(u => u.UserName!.ToLower().Contains(searchTerm) ||
                                         u.Email!.ToLower().Contains(searchTerm) ||
                                         u.PhoneNumber!.Contains(searchTerm));
            }
            // Đếm tổng số bản ghi sau khi lọc
            int totalCount = await query.CountAsync();
            //sắp xếp theo Id giảm dần, bỏ qua các bản ghi trước đó và lấy số lượng bản ghi theo pageSize
            var items = await query.OrderByDescending(u => u.Id)
                                   .Skip((pageNumber - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            return new PagedResult<ApplicationUser> { Items = items, TotalRecords = totalCount, PageNumber = pageNumber, PageSize = pageSize };
        }
        // Lấy người dùng theo Id
        public async Task<ApplicationUser?> GetByIdAsync(Guid userId)
        {
            return await _userManager.FindByIdAsync(userId.ToString());
        }
        //tạo người dùng mới với mật khẩu và role
        public async Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password, string role)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded && !string.IsNullOrEmpty(role))
            {
                // Kiểm tra Role có tồn tại không, nếu không thì tạo mới
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new ApplicationRole { Name = role });
                }
                await _userManager.AddToRoleAsync(user, role);
            }
            return result;
        }
        // Cập nhật thông tin người dùng
        public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user)
        {
            return await _userManager.UpdateAsync(user);
        }
        // Cập nhật role của người dùng
        public async Task<IdentityResult> UpdateUserRoleAsync(ApplicationUser user, string newRole)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            return await _userManager.AddToRoleAsync(user, newRole);
        }
        // thay đổi mật khẩu của người dùng
        public async Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string newPassword)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            return await _userManager.ResetPasswordAsync(user, token, newPassword);
        }
        //khóa tài khoản
        public async Task<IdentityResult> ToggleActiveStatusAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return IdentityResult.Failed(new IdentityError { Description = "Không tìm thấy người dùng." });

            // Đảo ngược trạng thái khóa: LockoutEnd = MaxValue (Khóa vĩnh viễn) hoặc Null (Mở khóa)
            if (await _userManager.IsLockedOutAsync(user))
            {
                return await _userManager.SetLockoutEndDateAsync(user, null);
            }
            else
            {
                return await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }
        }
    }
}
