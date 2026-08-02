namespace QuanLyThuVien.Web.Models
{
    public class EditBookViewModel : CreateBookViewModel
    {
        public int Id { get; set; }

        // Lưu lại đường dẫn ảnh cũ nếu Admin không chọn upload ảnh mới
        public string? ExistingCoverImage { get; set; }
    }
}
