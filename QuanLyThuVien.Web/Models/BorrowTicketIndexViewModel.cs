using QuanLyThuVien.Web.Common;
using QuanLyThuVien.Web.Entities;
using QuanLyThuVien.Web.Enums;

namespace QuanLyThuVien.Web.Models
{
    public class BorrowTicketIndexViewModel
    {
        public PagedResult<BorrowTickets>? Tickets { get; set; } // Giả sử bạn có class PagedResult
        public string? SearchTicketId { get; set; }
        public string? SearchBorrower { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public BorrowStatus? Status { get; set; }
    }

    // ViewModel để nhận dữ liệu khi cập nhật từ Modal
    public class UpdateTicketStatusModel
    {
        public int TicketId { get; set; }
        public BorrowStatus NewStatus { get; set; }
        public string? Note { get; set; }

        // Danh sách ID của các bản sao vật lý mà Thủ thư chọn (Dùng khi duyệt phiếu)
        // DetailId (ID của BorrowTicketDetails) | Value: BookCopyId (ID của cuốn vật lý)
        public Dictionary<int, int>? SelectedCopies { get; set; }
    }
}
