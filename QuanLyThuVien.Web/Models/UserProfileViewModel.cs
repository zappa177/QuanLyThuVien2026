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

        // Trường riêng dành cho Khách (Reader) như trong hình mẫu
        public string? ClassName { get; set; } // Tên lớp (nếu entity của bạn đặt tên khác như Class, hãy đổi lại cho khớp)

        public bool IsReader { get; set; } // Dùng để View phân biệt hiển thị ô "Lớp"
    }
}
