using QuanLyThuVien.Web.Enums;

namespace QuanLyThuVien.Web.EnumExtensions
{
    public static class EnumExtensions
    {
        // Hàm mở rộng (Extension method) cho Enum BookStatus
        public static string ToVietnamese(this BookCopyStatus status)
        {
            return status switch
            {
                BookCopyStatus.Available => "Sẵn sàng",
                BookCopyStatus.Borrowed => "Đang mượn",
                BookCopyStatus.Damaged => "Hư hỏng",
                BookCopyStatus.Lost => "Mất",
                _ => "Không xác định"
            };
        }
    }
}
