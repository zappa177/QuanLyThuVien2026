using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Web.Models
{
    //nhận dữ liệu từ form đăng nhập (Login.cshtml) gửi lên
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
