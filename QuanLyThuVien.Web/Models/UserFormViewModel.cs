using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Web.Models
{
    public class UserFormViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên người dùng.")]
        public string UserName { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
        public string Role { get; set; } = string.Empty;

        public string? UserCode { get; set; }
        public string? Position { get; set; } // THÊM DÒNG NÀY
    }
}