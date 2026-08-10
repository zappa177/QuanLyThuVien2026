namespace QuanLyThuVien.Web.Enums
{
    public enum BorrowStatus
    {
        Pending = 1,    // Chờ thủ thư duyệt
        Accepted = 2,   // Thủ thư đã duyệt
        Borrowing = 3,  // Đang mượn (đã nhận sách)
        Returned = 4,   // Đã trả đủ
        Overdue = 5,    // Quá hạn chưa trả
        Canceled = 6    // Bị hủy
    }
}
