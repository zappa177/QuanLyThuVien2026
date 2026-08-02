using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Application.Interfaces;
using QuanLyThuVien.Application.Interfaces.IRepositories;
using QuanLyThuVien.Application.Interfaces.IServices;
using QuanLyThuVien.Application.Services;
using QuanLyThuVien.Application.Settings;
using QuanLyThuVien.Domain.Entities.Identity;
using QuanLyThuVien.Infrastructure.Data;
using QuanLyThuVien.Infrastructure.Repositories;
using Serilog;

// 1. Khởi tạo Serilog Bootstrap Logger để bắt lỗi ngay từ lúc ứng dụng mới chạy lên
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Khởi động ứng dụng ...");
    var builder = WebApplication.CreateBuilder(args);

    // Sử dụng Serilog thay thế bộ log mặc định của ASP.NET Core
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
        .WriteTo.ApplicationInsights(
            context.Configuration["ApplicationInsights:ConnectionString"],
            TelemetryConverter.Traces));

    // đăng ký cấu hình từ appsettings.json vào DI container để có thể inject vào các service
    builder.Services.Configure<LibraryRules>(builder.Configuration.GetSection("LibraryRules"));

    //Đăng ký Redis Distributed Cache
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("RedisCache");
        options.InstanceName = "LibraryApp_";
    });

    // dang ky DbContext
    builder.Services.AddDbContext<ApplicationDbContext>(option =>
        option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("QuanLyThuVien.Infrastructure"))
    );
    // Đăng ký Interface DbContext
    builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

    // đăng ký asp.net identity 
    builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(option =>
    {
        // cấu hình mật khẩu
        option.Password.RequiredLength = 4;
        option.Password.RequireUppercase = false;
        option.Password.RequireLowercase = false;
        option.Password.RequireNonAlphanumeric = false;
        option.Password.RequireDigit = false;
        // cấu hình user
        option.User.RequireUniqueEmail = true;
        option.Lockout.AllowedForNewUsers = true;
    })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddUserStore<UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, Guid>>()
        .AddRoleStore<RoleStore<ApplicationRole, ApplicationDbContext, Guid>>();
    builder.Services.ConfigureApplicationCookie(option =>
    {
        // Đường dẫn khi chưa đăng nhập
        option.LoginPath = "/";

        // ĐƯỜNG DẪN KHI BỊ TỪ CHỐI QUYỀN TRUY CẬP (403)
        option.AccessDeniedPath = "/Account/AccessDenied";
    });


    //đăng ký repository
    builder.Services.AddScoped<IBookRepository, BookRepository>();
    builder.Services.AddScoped<ICartRepository, CartRepository>();
    builder.Services.AddScoped<IBorrowTicketRepository, BorrowTicketRepository>();
    builder.Services.AddScoped<IReaderRepository, ReaderRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
    builder.Services.AddScoped<IShelfRepository, ShelfRepository>();
    builder.Services.AddScoped<IShelfTierRepository, ShelfTierRepository>();


    // Đăng ký SERVICES
    builder.Services.AddScoped<IBookService, BookService>();
    builder.Services.AddScoped<ICartService, CartService>();
    builder.Services.AddScoped<IBorrowTicketService, BorrowTicketService>();
    builder.Services.AddScoped<IReaderService, ReaderService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<IShelfService, ShelfService>();
    builder.Services.AddScoped<IShelfTierService, ShelfTierService>();
    builder.Services.AddScoped<IAuthService, AuthService>();

    //đăng ký service cho xác thực phân quyền
    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization();

    //đăng ký service cho MVC
    builder.Services.AddControllersWithViews();

    var app = builder.Build();

    // Kích hoạt Serilog Request Logging để ghi log các request HTTP
    app.UseSerilogRequestLogging();

    //cấu hình khi ứng dụng chạy trong production
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }
    //dùng https
    app.UseHttpsRedirection();


    //truy cập file tĩnh (css, js, hình ảnh)
    app.UseStaticFiles();
    // routing
    app.UseRouting();

    // middleware cho xác thực và phân quyền
    app.UseAuthentication();
    app.UseAuthorization();

    // Tạo route mặc định cho ứng dụng, nếu không có controller và action nào được chỉ định thì sẽ chuyển hướng đến Account/Login
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Account}/{action=Login}/{id?}");

    // seed admin user mặc định khi chạy ứng dụng
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            await AdminSeeder.SeedAdminAsync(services);
            Log.Information("Seed Admin user thành công."); // Bổ sung ghi log
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Lỗi xảy ra khi seed dữ liệu Admin."); // Ghi log nếu lỗi
        }
    }

    // Đăng ký Rotativa để tạo file PDF từ HTML
    Rotativa.AspNetCore.RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

    app.Run();
}
catch (Exception ex)
{
    // Bắt lỗi nghiêm trọng khiến ứng dụng bị sập
    Log.Fatal(ex, "Ứng dụng bị lỗi");
}
finally
{
    Log.CloseAndFlush(); //ghi hết lỗi vào file log trước khi kết thúc ứng dụng
}