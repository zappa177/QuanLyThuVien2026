using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Web.Data;
using QuanLyThuVien.Web.Enums;
using QuanLyThuVien.Web.Models;


namespace QuanLyThuVien.Web.Controllers
{
    [Authorize(Roles = "Admin, Librarian")] // Cho phép cả Admin và Thủ thư xem báo cáo
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hiển thị thống kê
        public async Task<IActionResult> Index(string reportType = "category")
        {
            ViewBag.ReportType = reportType;

            if (reportType == "category")
            {
                // Thống kê số lượng tựa sách theo thể loại
                var categoryData = await _context.Categories
                    .Select(c => new CategoryReportViewModel
                    {
                        CategoryName = c.Name,
                        BookCount = c.Books.Count   // 1 thể loại có nhiều tựa sách
                    })
                    .OrderByDescending(c => c.BookCount)
                    .ToListAsync();

                ViewBag.CategoryData = categoryData;
            }
            else if (reportType == "status")
            {
                // MỚI: Thống kê tình trạng sách dựa trên BẢN SAO VẬT LÝ (BookCopies)
                var rawStatusData = await _context.BookCopies
                    .GroupBy(bc => bc.Status)
                    .Select(g => new
                    {
                        StatusKey = g.Key,
                        BookCount = g.Count()
                    })
                    .ToListAsync();

                // Map dữ liệu sang tiếng Việt
                var statusData = rawStatusData
                    .Select(x => new StatusReportViewModel
                    {
                        StatusName = x.StatusKey switch
                        {
                            BookCopyStatus.Available => "Sẵn sàng cho mượn",
                            BookCopyStatus.Borrowed => "Đang cho mượn",
                            BookCopyStatus.Damaged => "Hư hỏng",
                            BookCopyStatus.Lost => "Bị mất",
                            _ => "Khác"
                        },
                        BookCount = x.BookCount
                    })
                    .OrderByDescending(s => s.BookCount)
                    .ToList();

                ViewBag.StatusData = statusData!;
            }
            else if (reportType == "top10")
            {
                // Thống kê Top 10 tựa sách được mượn nhiều nhất
                var topBooksData = await _context.Books
                    .Select(b => new TopBookReportViewModel
                    {
                        BookTitle = b.Title,
                        BorrowCount = b.BorrowTicketDetails.Count // Đếm số lần xuất hiện trong chi tiết phiếu
                    })
                    .OrderByDescending(b => b.BorrowCount)
                    .Take(10)
                    .ToListAsync();

                ViewBag.TopBookData = topBooksData;
            }

            return View();
        }

        // Xuất file Excel
        [HttpGet]
        public async Task<IActionResult> ExportExcel(string reportType = "category")
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Báo Cáo Thống Kê");
            var currentRow = 1;

            if (reportType == "category")
            {
                worksheet.Cell(currentRow, 1).Value = "Tên thể loại";
                worksheet.Cell(currentRow, 2).Value = "Số lượng tựa sách";
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
                worksheet.Cell(currentRow, 1).Value = "Tình trạng bản sao sách";
                worksheet.Cell(currentRow, 2).Value = "Số lượng";
                worksheet.Range(currentRow, 1, currentRow, 2).Style.Font.Bold = true;

                // Thống kê theo BookCopies
                var data = await _context.BookCopies
                    .GroupBy(bc => bc.Status)
                    .Select(g => new
                    {
                        StatusName =
                            g.Key == BookCopyStatus.Available ? "Sẵn sàng cho mượn" :
                            g.Key == BookCopyStatus.Borrowed ? "Đang cho mượn" :
                            g.Key == BookCopyStatus.Damaged ? "Hư hỏng" :
                            g.Key == BookCopyStatus.Lost ? "Bị mất" :
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
                // (Đã dọn dẹp đoạn code bị lặp foreach ở đây)
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

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            string fileName = $"BaoCao_{reportType}_{DateTime.Now:ddMMyyyy_HHmm}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}