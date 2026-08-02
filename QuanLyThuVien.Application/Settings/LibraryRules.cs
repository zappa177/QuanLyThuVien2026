namespace QuanLyThuVien.Application.Settings
{
    public class LibraryRules
    {
        public int MaxBooksPerTicket { get; set; }  // Số lượng sách tối đa mà một phiếu mượn có thể chứa
        public int MaxBorrowDays { get; set; }      // Số ngày tối đa cho phép mượn sách
    }
}