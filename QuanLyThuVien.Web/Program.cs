using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Web.Data;
using QuanLyThuVien.Web.Entities.Identity;

var builder = WebApplication.CreateBuilder(args);

// 1. Thêm In-Memory Cache (Thay thế Redis Cache)
builder.Services.AddMemoryCache();

// 2. Bộ Logger mặc định của ASP.NET Core đã được tự động thêm vào thông qua WebApplication.CreateBuilder
// Bạn không cần phải cấu hình Serilog phức tạp nữa.


// 4. Đăng ký DbContext
builder.Services.AddDbContext<ApplicationDbContext>(option =>
    option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// 5. Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(option =>
{
    option.Password.RequiredLength = 4;
    option.Password.RequireUppercase = false;
    option.Password.RequireLowercase = false;
    option.Password.RequireNonAlphanumeric = false;
    option.Password.RequireDigit = false;
    option.User.RequireUniqueEmail = true;
    option.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
    .AddUserStore<UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, Guid>>()
    .AddRoleStore<RoleStore<ApplicationRole, ApplicationDbContext, Guid>>();


builder.Services.ConfigureApplicationCookie(option =>
{
    option.LoginPath = "/";
    option.AccessDeniedPath = "/Account/AccessDenied";

    //option.ExpireTimeSpan = TimeSpan.FromDays(30);//nhớ đăng nhập 30 ngày
    //option.SlidingExpiration = false; //không tự gia hạn khi người dùng có tương tác
    //option.Cookie.IsEssential = true; //lưu cookie để xác thực
});

// 6. MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// 7. Seed Admin Data (Sử dụng ILogger mặc định)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        await AdminSeeder.SeedAdminAsync(services);
        logger.LogInformation("Đã Seed tài khoản Admin thành công.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Lỗi xảy ra khi seed dữ liệu Admin.");
    }
}

app.Run();