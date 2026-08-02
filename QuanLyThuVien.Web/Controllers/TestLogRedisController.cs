using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace QuanLyThuVien.Web.Controllers
{
    public class TestLogRedisController : Controller
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<TestLogRedisController> _logger; // Bổ sung Logger

        // Inject cả Cache và Logger thông qua Constructor
        public TestLogRedisController(IDistributedCache cache, ILogger<TestLogRedisController> logger)
        {
            _cache = cache;
            _logger = logger;
        }
        // 1. Hàm này tự chạy khi vào trang chủ (localhost:xxxx) để test Serilog
        public IActionResult Index()
        {
            _logger.LogInformation("🚀 [TEST SERILOG] Ai đó vừa truy cập vào trang chủ.");
            _logger.LogWarning("⚠️ [TEST SERILOG] Test thử cảnh báo hệ thống.");

            try
            {
                // Cố tình tạo lỗi chia cho 0
                int a = 10;
                int b = 0;
                int result = a / b;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [TEST SERILOG] Lỗi chia cho 0 đã bị bắt lại!");
            }

            return Content("Test Serilog completed.");
        }

        // 2. Hàm này dùng để test Redis (truy cập: localhost:xxxx/Home/TestRedis)
        public async Task<IActionResult> TestRedis()
        {
            var cacheKey = "TestKey";
            var currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            // Lưu thử dữ liệu vào Redis
            await _cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes($"Redis hoạt động! Thời gian: {currentTime}"));

            // Lấy dữ liệu ra
            var dataFromCache = await _cache.GetAsync(cacheKey);
            var result = Encoding.UTF8.GetString(dataFromCache);

            return Content(result);
        }
    }
}
