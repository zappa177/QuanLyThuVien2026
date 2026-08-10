using Microsoft.AspNetCore.Identity;
using QuanLyThuVien.Web.Entities.Identity;

namespace QuanLyThuVien.Web.Data
{
    public static class AdminSeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
        {

            //lấy usermanager và rolemanager từ Dependency Injection (DI) container
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            //tạo role mặc định 
            string adminRole = "Admin";
            string librarianRole = "Librarian";
            string readerRole = "Reader";
            //kiểm tra nếu role chưa tồn tại thì tạo mới
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = adminRole, Description = "Administrator role" });
            }
            if (!await roleManager.RoleExistsAsync(librarianRole))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = librarianRole, Description = "Librarian role" });
            }
            if (!await roleManager.RoleExistsAsync(readerRole))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = readerRole, Description = "Reader role" });
            }
            //tạo tài khoản admin mặc định nếu chưa tồn tại
            string adminEmail = "admin@examplemail.com";
            //kiểm tra tài khoản đã tồn tại chưa bằng mail (mỗi tài khoản chỉ được 1 mail duy nhất)
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                ApplicationUser admin = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    FullName = "Administrator"
                };
                //tạo tài khoản admin
                var result = await userManager.CreateAsync(admin, "admin");
                //nếu tạo thành công thì gán role admin cho tài khoản
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, adminRole);
                }
            }

        }
    }
}
