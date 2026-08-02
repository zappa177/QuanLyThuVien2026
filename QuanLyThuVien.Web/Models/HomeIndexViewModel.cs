using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyThuVien.Domain.Common;
using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Web.Models
{

    public class HomeIndexViewModel
    {
        // Danh sách sách đã phân trang (ví dụ 9 cuốn/trang để tạo grid 3x3)
        public PagedResult<Books> Books { get; set; } = new PagedResult<Books>();

        // Nguồn dữ liệu cho Dropdown
        public SelectList? Categories { get; set; }

        // Lưu trữ lại từ khóa tìm kiếm để hiển thị lại trên Form
        public string SearchTitle { get; set; } = string.Empty;
        public string SearchAuthor { get; set; } = string.Empty;
        public string SearchISBN { get; set; } = string.Empty;
        public int? PublishYear { get; set; }
        public int? SelectedCategoryId { get; set; }
        public string SortBy { get; set; } = "PublishYear"; // Mặc định sắp xếp theo Năm xuất bản
    }
}
