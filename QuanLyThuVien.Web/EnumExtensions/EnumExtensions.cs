using QuanLyThuVien.Domain.Enums;

namespace QuanLyThuVien.Web.EnumExtensions
{
    public static class EnumExtensions
    {
        // Hàm mở rộng (Extension method) cho Enum BookStatus
        public static string ToVietnamese(this BookStatus status)
        {
            return status switch
            {
                BookStatus.Available => "Sẵn sàng",
                BookStatus.Borrowed => "Đang mượn",
                BookStatus.Damaged => "Hư hỏng",
                BookStatus.Lost => "Mất",
                _ => "Không xác định"
            };
        }
    }
}
