using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Interfaces.IRepositories
{
    public interface IBorrowTicketRepository
    {
        Task<BorrowTickets?> GetByIdAsync(int id);  //lấy phiếu mượn theo id
        Task AddAsync(BorrowTickets ticket);    //thêm phiếu mượn
        void Update(BorrowTickets ticket);  //cập nhật phiếu mượn

        Task<BorrowTickets?> GetTicketWithDetailsAsync(int ticketId);   //lấy phiếu mượn theo id kèm chi tiết sách mượn
        Task<int> CountActiveBorrowedBooksAsync(int readerId);  //đếm số lượng sách đang mượn của một độc giả
    }
}
