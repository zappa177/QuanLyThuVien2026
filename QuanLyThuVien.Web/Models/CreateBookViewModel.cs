using QuanLyThuVien.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Web.Models
{
    public class CreateBookViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên sách")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn thể loại")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn tầng của kệ")]
        public int ShelfTierId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã sách")]
        public string ISBN { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên tác giả")]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập năm xuất bản")]
        public int PublishYear { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nhà xuất bản")]
        public string Publisher { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn tình trạng sách")]
        public BookStatus Status { get; set; }

        // IFormFile dùng để hứng file ảnh upload từ trình duyệt
        public IFormFile? CoverImage { get; set; }
    }
}
