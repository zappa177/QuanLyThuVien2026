using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Web.Models
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu cũ.")]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [DataType(DataType.Password)]
        // Bạn có thể bỏ comment dòng dưới nếu muốn bắt buộc mật khẩu dài tối thiểu 6 ký tự
        // [StringLength(100, ErrorMessage = "Mật khẩu phải từ {2} đến {1} ký tự.", MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
