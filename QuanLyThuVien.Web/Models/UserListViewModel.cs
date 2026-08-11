namespace QuanLyThuVien.Web.Models
{
    public class UserListViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty; // Hiển thị FullName trên danh sách
        public string Role { get; set; } = string.Empty;

        // Đã đổi từ ReaderId thành UserCode cho khớp với Entity ApplicationUser
        public string UserCode { get; set; } = string.Empty;
        public string? Position { get; set; }

        public bool IsHidden { get; set; }
    }
}