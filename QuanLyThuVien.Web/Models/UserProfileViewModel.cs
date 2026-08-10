using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Web.Models
{
    public class UserProfileViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên.")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        // Mã người dùng (Mã thẻ / Mã sinh viên / Mã nhân viên)
        public string? UserCode { get; set; }

        // Thay vì ClassName riêng cho Reader, dùng Position chung cho mọi Role (Sinh viên, Giảng viên, Thủ thư...)
        public string? Position { get; set; }
    }
}