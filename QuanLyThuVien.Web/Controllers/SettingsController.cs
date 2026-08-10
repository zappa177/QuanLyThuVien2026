using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Web.Entities;
using QuanLyThuVien.Web.Data;

namespace QuanLyThuVien.Web.Controllers
{
    [Authorize(Roles = "Admin")] // Chỉ Admin mới được vào trang này
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hiện giao diện điền thông số
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var settings = await _context.SystemSettings.ToListAsync();
            return View(settings);
        }

        // Lưu thông số do Admin sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSettings(IFormCollection formData)
        {
            foreach (var key in formData.Keys)
            {
                // Bỏ qua các key hệ thống của form
                if (key == "__RequestVerificationToken") continue;

                var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == key);

                if (setting != null)
                {
                    setting.SettingValue = formData[key].ToString();
                }
                else
                {
                    // Nếu DB chưa có dòng này thì tạo mới
                    _context.SystemSettings.Add(new SystemSettings
                    {
                        SettingKey = key,
                        SettingValue = formData[key].ToString()
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMsg"] = "Đã lưu cấu hình hệ thống thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}