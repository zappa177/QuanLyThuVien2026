namespace QuanLyThuVien.Web.Models
{
    public class UserListViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; } // Hiển thị FullName trên danh sách
        public string Role { get; set; }
        public string ReaderId { get; set; }
        public bool IsHidden { get; set; }
    }
}
