using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Application.Interfaces.IRepositories;
using QuanLyThuVien.Domain.Entities;
using QuanLyThuVien.Infrastructure.Data;

namespace QuanLyThuVien.Infrastructure.Repositories
{
    public class BorrowTicketRepository : IBorrowTicketRepository
    {
        private readonly ApplicationDbContext _context;

        public BorrowTicketRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        // Lấy phiếu mượn theo ID
        public async Task<BorrowTickets?> GetByIdAsync(int id)
            => await _context.BorrowTickets.FindAsync(id);
        //thêm phiếu mượn mới
        public async Task AddAsync(BorrowTickets ticket)
            => await _context.BorrowTickets.AddAsync(ticket);
        // Cập nhật phiếu mượn
        public void Update(BorrowTickets ticket)
            => _context.BorrowTickets.Update(ticket);
        // Lấy tất cả phiếu mượn với chi tiết phiếu
        public async Task<BorrowTickets?> GetTicketWithDetailsAsync(int ticketId)
        {
            return await _context.BorrowTickets
                .Include(t => t.Reader)
                .Include(t => t.TicketDetails)
                    .ThenInclude(d => d.Book)
                .FirstOrDefaultAsync(t => t.Id == ticketId);
        }
        // đếm số sách mượn đang mượn của một độc giả
        public async Task<int> CountActiveBorrowedBooksAsync(int readerId)
        {
            return await _context.BorrowTicketDetails
                .Include(d => d.BorrowTicket)
                .Where(d => d.BorrowTicket!.ReaderId == readerId
                         && d.BorrowTicket.Status == Domain.Enums.BorrowStatus.Borrowing
                         && d.IsActive)
                .CountAsync();
        }
    }
}
