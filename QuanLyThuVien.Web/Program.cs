using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Web.Data;
using QuanLyThuVien.Web.Entities.Identity;

var builder = WebApplication.CreateBuilder(args);

// In-Memory Cache 
builder.Services.AddMemoryCache();

// Đăng ký DbContext
builder.Services.AddDbContext<ApplicationDbContext>(option =>
    option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// đăng ký Identity
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

//add controllers with views
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

// Seed Admin Data
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