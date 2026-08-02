using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Domain.Entities.Identity;
using QuanLyThuVien.Infrastructure.Data;
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

                    // lấy số lượng cartitem của user hiện tại để hiện lên header
                    var reader = await _context.Readers.FirstOrDefaultAsync(r => r.ApplicationUserId == user.Id);
                    if (reader != null)
                    {
                        model.CartItemCount = await _context.CartItems.CountAsync(c => c.ReaderId == reader.Id);
                    }
                    else
                    {
                        model.CartItemCount = 0;
                    }
                }
            }

            return View(model);
        }
    }
}