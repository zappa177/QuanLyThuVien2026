namespace QuanLyThuVien.Web.Models
{
    public class HeaderViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string DisplayRole { get; set; } = string.Empty; // Hiển thị: admin, khách, thủ thư
        public bool IsReader { get; set; }
        public bool IsLibrarian { get; set; }
        public int CartItemCount { get; set; } // Số lượng sách trong giỏ
    }
}
