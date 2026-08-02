using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Interfaces.IServices
{
    public interface IBorrowTicketService
    {
        Task<BorrowTickets?> GetTicketWithDetailsAsync(int ticketId);   //lấy phiếu mượn theo id kèm chi tiết sách mượn
        //Task<bool> CreateBorrowTicketAsync(int readerId, List<int> bookIds, string note);   //tạo phiếu mượn mới
        // Task<bool> ReturnBooksAsync(int ticketId, string note); // Có thể phát triển thêm logic trả sách
    }
}
