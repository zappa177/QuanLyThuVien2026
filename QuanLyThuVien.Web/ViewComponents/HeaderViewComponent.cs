using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Web.Data;
using QuanLyThuVien.Web.Entities.Identity;
using QuanLyThuVien.Web.Models;

namespace QuanLyThuVien.Web.ViewComponents
{
    public class HeaderViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public HeaderViewComponent(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = new HeaderViewModel();

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(UserClaimsPrincipal);

                if (user != null)
                {
                    model.FullName = user.FullName ?? user.UserName!;

                    var roles = await _userManager.GetRolesAsync(user);

                    if (roles.Contains("Admin"))
                    {
                        model.DisplayRole = "admin";
                    }
                    else if (roles.Contains("Librarian"))
                    {
                        model.DisplayRole = "thủ thư";
                        model.IsLibrarian = true;
                    }
                    else
                    {
                        model.DisplayRole = "khách";
                        model.IsReader = true;
                    }

                    // Đếm trực tiếp số lượng giỏ hàng thông qua UserId (ApplicationUser.Id)
                    // Áp dụng chung cho cả Khách, Thủ thư lẫn Admin (vì ai cũng có thể có giỏ hàng)
                    // Tính TỔNG số lượng sách trong giỏ hàng (Cộng dồn cột Quantity)
                    // Lấy danh sách cột Quantity của user này về dạng List rồi dùng hàm Sum() mặc định của C#
                    int count = (await _context.CartItems
                        .Where(c => c.UserId == user.Id)
                        .Select(c => c.Quantity)
                        .ToListAsync())
                        .Sum();
                }
            }

            return View(model);
        }
    }
}