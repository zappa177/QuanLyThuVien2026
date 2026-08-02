namespace QuanLyThuVien.Domain.Common
{
    public abstract class BaseEntity
    {
        public bool IsActive { get; set; } = true;  // Trạng thái hoạt động của thực thể
        public DateTime CreatedAt { get; set; } = DateTime.Now; // Ngày tạo thực thể
        public string? CreatedBy { get; set; }  // Người tạo thực thể
        public DateTime? UpdatedAt { get; set; }    // Ngày cập nhật thực thể
        public string? UpdatedBy { get; set; }  // Người cập nhật thực thể
    }
}
