using QuanLyThuVien.Web.Enums;

namespace QuanLyThuVien.Web.EnumExtensions
{
    public static class EnumExtensions
    {
        //  DÀNH CHO TRẠNG THÁI CUỐN SÁCH (BookCopyStatus)
        public static string ToVietnamese(this BookCopyStatus status)
        {
            return status switch
            {
                BookCopyStatus.Available => "Sẵn sàng",
                BookCopyStatus.Borrowed => "Đang mượn",
                BookCopyStatus.Damaged => "Hư hỏng",
                BookCopyStatus.Lost => "Mất",
                BookCopyStatus.OnHold => "Giữ",
                BookCopyStatus.Pending => "Chờ",
                _ => "Không xác định"
            };
        }


        //  DÀNH CHO TRẠNG THÁI PHIẾU MƯỢN (BorrowStatus)

        public static string ToVietnamese(this BorrowStatus status)
        {
            return status switch
            {
                BorrowStatus.Pending => "Chờ duyệt",
                BorrowStatus.Accepted => "Đã duyệt",
                BorrowStatus.Borrowing => "Đang mượn",
                BorrowStatus.Returned => "Đã trả đủ",
                BorrowStatus.Overdue => "Quá hạn",
                BorrowStatus.Canceled => "Bị hủy",
                _ => "Không xác định"
            };
        }
    }
}