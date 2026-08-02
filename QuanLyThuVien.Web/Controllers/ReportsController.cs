using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Domain.Enums;
using QuanLyThuVien.Infrastructure.Data;
using QuanLyThuVien.Web.Models;
using Rotativa.AspNetCore;

namespace QuanLyThuVien.Web.Controllers
{

    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string reportType = "category")
        {
            ViewBag.ReportType = reportType;

            if (reportType == "category")
            {
                // 1. THỐNG KÊ THEO THỂ LOẠI (Dữ liệu thực)
                // Đếm số lượng sách (Books) nằm trong mỗi thể loại (Categories)
                var categoryData = await _context.Categories
                    .Select(c => new CategoryReportViewModel
                    {
                        CategoryName = c.Name,
                        BookCount = c.Books.Count // Giả định Categories có navigation property là ICollection<Books> Books
                    })
                    .OrderByDescending(c => c.BookCount) // Ưu tiên xếp thể loại nhiều sách lên trước
                    .ToListAsync();

                ViewBag.CategoryData = categoryData;
            }
            else if (reportType == "status")
            {
                // 2. THỐNG KÊ THEO TÌNH TRẠNG SÁCH (Dữ liệu thực)
                // Nhóm các cuốn sách theo trạng thái (Status) và đếm số lượng
                // BƯỚC 1: Kéo dữ liệu thô (Enum và Count) từ Database lên trước
                var rawStatusData = await _context.Books
                    .GroupBy(b => b.Status)
                    .Select(g => new
                    {
                        StatusKey = g.Key,
                        BookCount = g.Count()
                    })
                    .ToListAsync(); // Lệnh này sẽ thực thi truy vấn SQL

                // BƯỚC 2: Map sang Tiếng Việt và đưa vào ViewModel ở trên bộ nhớ (RAM)
                var statusData = rawStatusData
                    .Select(x => new StatusReportViewModel
                    {
                        // Chuyển đổi Enum sang chuỗi tiếng Việt (Lúc này switch hoạt động bình thường)
                        StatusName = x.StatusKey switch
                        {
                            BookStatus.Available => "Sẵn sàng cho mượn",
                            BookStatus.Borrowed => "Đang cho mượn",
                            BookStatus.Lost => "Mất / Hư hỏng",
                            BookStatus.Damaged => "Đang bảo trì",
                            _ => "Khác"
                        },
                        BookCount = x.BookCount
                    })
                    .OrderByDescending(s => s.BookCount)
                    .ToList();

                ViewBag.StatusData = statusData;

                ViewBag.StatusData = statusData!;
            }
            else if (reportType == "top10")
            {
                // 3. THỐNG KÊ 10 SÁCH ĐƯỢC MƯỢN NHIỀU NHẤT (Dữ liệu thực)
                // Đếm số lượng phiếu mượn/chi tiết mượn của từng cuốn sách
                var topBooksData = await _context.Books
                    .Select(b => new TopBookReportViewModel
                    {
                        BookTitle = b.Title, // Tên cuốn sách
                        // Đếm số lượng bản ghi trong bảng trung gian (BorrowTickets hoặc BorrowTicketDetails)
                        BorrowCount = b.BorrowTicketDetails.Count
                    })
                    .OrderByDescending(b => b.BorrowCount) // Xếp từ cao xuống thấp
                    .Take(10) // Chỉ lấy đúng 10 cuốn
                    .ToListAsync();

                ViewBag.TopBookData = topBooksData;
            }

            return View();
        }

        // ==========================================
        // CÁC HÀM XUẤT FILE 
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ExportExcel(string reportType = "category")
        {
            // 1. Khởi tạo Workbook
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Báo Cáo Thống Kê");
            var currentRow = 1;

            // 2. Viết Header và đổ dữ liệu tùy theo loại báo cáo
            if (reportType == "category")
            {
                worksheet.Cell(currentRow, 1).Value = "Tên thể loại";
                worksheet.Cell(currentRow, 2).Value = "Số lượng sách";
                worksheet.Range(currentRow, 1, currentRow, 2).Style.Font.Bold = true;

                var data = await _context.Categories
                    .Select(c => new { c.Name, BookCount = c.Books.Count })
                    .OrderByDescending(c => c.BookCount).ToListAsync();

                foreach (var item in data)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = item.Name;
                    worksheet.Cell(currentRow, 2).Value = item.BookCount;
                }
            }
            else if (reportType == "status")
            {
                worksheet.Cell(currentRow, 1).Value = "Tình trạng sách";
                worksheet.Cell(currentRow, 2).Value = "Số lượng";
                worksheet.Range(currentRow, 1, currentRow, 2).Style.Font.Bold = true;

                var data = await _context.Books
                    .GroupBy(b => b.Status)
                    .Select(g => new
                    {
                        // Bỏ kiểm tra null, dùng toán tử 3 ngôi để dịch sang Tiếng Việt
                        StatusName =
                            g.Key == BookStatus.Available ? "Sẵn sàng cho mượn" :
                            g.Key == BookStatus.Borrowed ? "Đang cho mượn" :
                            g.Key == BookStatus.Lost ? "Mất / Hư hỏng" :
                            g.Key == BookStatus.Damaged ? "Đang bảo trì" :
                            "Khác",

                        BookCount = g.Count()
                    })
                    .OrderByDescending(s => s.BookCount).ToListAsync();

                foreach (var item in data)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = item.StatusName;
                    worksheet.Cell(currentRow, 2).Value = item.BookCount;
                }

                foreach (var item in data)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = item.StatusName;
                    worksheet.Cell(currentRow, 2).Value = item.BookCount;
                }
            }
            else if (reportType == "top10")
            {
                worksheet.Cell(currentRow, 1).Value = "Tên sách";
                worksheet.Cell(currentRow, 2).Value = "Lượt mượn";
                worksheet.Range(currentRow, 1, currentRow, 2).Style.Font.Bold = true;

                var data = await _context.Books
                    .Select(b => new { BookTitle = b.Title, BorrowCount = b.BorrowTicketDetails.Count })
                    .OrderByDescending(b => b.BorrowCount).Take(10).ToListAsync();

                foreach (var item in data)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = item.BookTitle;
                    worksheet.Cell(currentRow, 2).Value = item.BorrowCount;
                }
            }

            // Tự động căn chỉnh độ rộng cột
            worksheet.Columns().AdjustToContents();

            // 3. Chuyển file ra dạng Stream và trả về
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            string fileName = $"BaoCao_{reportType}_{DateTime.Now:ddMMyyyy_HHmm}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(string reportType = "category")
        {
            // Khởi tạo Model chuyên dụng
            var reportModel = new PdfReportModel { ReportType = reportType };

            if (reportType == "category")
            {
                reportModel.CategoryData = await _context.Categories
                    .Select(c => new CategoryReportViewModel { CategoryName = c.Name, BookCount = c.Books.Count })
                    .OrderByDescending(c => c.BookCount).ToListAsync();
            }
            else if (reportType == "status")
            {
                reportModel.StatusData = await _context.Books
                    .GroupBy(b => b.Status)
                    .Select(g => new StatusReportViewModel
                    {
                        StatusName =
                            g.Key == BookStatus.Available ? "Sẵn sàng cho mượn" :
                            g.Key == BookStatus.Borrowed ? "Đang cho mượn" :
                            g.Key == BookStatus.Lost ? "Mất / Hư hỏng" :
                            g.Key == BookStatus.Damaged ? "Đang bảo trì" :
                            "Khác",
                        BookCount = g.Count()
                    })
                    .OrderByDescending(s => s.BookCount).ToListAsync();
            }
            else if (reportType == "top10")
            {
                reportModel.TopBookData = await _context.Books
                    .Select(b => new TopBookReportViewModel { BookTitle = b.Title, BorrowCount = b.BorrowTicketDetails.Count })
                    .OrderByDescending(b => b.BorrowCount).Take(10).ToListAsync();
            }

            // Truyền Model vào Rotativa
            return new ViewAsPdf("ReportPdf", reportModel)
            {
                FileName = $"BaoCao_{reportType}_{DateTime.Now:ddMMyyyy_HHmm}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--disable-smart-shrinking"
            };
        }
    }
}

