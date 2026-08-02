using QuanLyThuVien.Application.Interfaces;
using QuanLyThuVien.Application.Interfaces.IRepositories;
using QuanLyThuVien.Application.Interfaces.IServices;
using QuanLyThuVien.Domain.Entities;
using QuanLyThuVien.Domain.Enums;

namespace QuanLyThuVien.Application.Services
{
    public class BorrowTicketService : IBorrowTicketService
    {
        private readonly IBorrowTicketRepository _ticketRepo;
        private readonly IBookRepository _bookRepo;
        private readonly IApplicationDbContext _context;

        public BorrowTicketService(IBorrowTicketRepository ticketRepo, IBookRepository bookRepo, IApplicationDbContext context)
        {
            _ticketRepo = ticketRepo;
            _bookRepo = bookRepo;
            _context = context;
        }
        // Lấy danh sách phiếu mượn theo độc giả
        public async Task<BorrowTickets?> GetTicketWithDetailsAsync(int ticketId)
        {
            return await _ticketRepo.GetTicketWithDetailsAsync(ticketId);
        }
        // Tạo phiếu mượn mới
        public async Task<bool> CreateBorrowTicketAsync(int readerId, List<int> bookIds, string note)
        {
            //Kiểm tra giới hạn mượn
            int currentBorrowed = await _ticketRepo.CountActiveBorrowedBooksAsync(readerId);
            if (currentBorrowed + bookIds.Count > 2)
                throw new Exception("Độc giả đã vượt quá giới hạn mượn 2 cuốn sách.");

            //Tạo phiếu mượn
            var ticket = new BorrowTickets
            {
                ReaderId = readerId,
                BorrowDate = DateTime.Now,
                ExpectedReturnDate = DateTime.Now.AddDays(14),
                Status = BorrowStatus.Borrowing,
                Note = note,
                IsActive = true
            };
            await _ticketRepo.AddAsync(ticket);

            //Cập nhật trạng thái sách và tạo chi tiết
            foreach (var bookId in bookIds)
            {
                var book = await _bookRepo.GetByIdAsync(bookId);
                if (book == null || book.Status != Domain.Enums.BookStatus.Available)
                    throw new Exception($"Sách (ID: {bookId}) không khả dụng.");

                book.Status = Domain.Enums.BookStatus.Borrowed;
                _bookRepo.Update(book);

                ticket.TicketDetails.Add(new BorrowTicketDetails
                {
                    BookId = bookId,
                    IsActive = true
                });
            }

            // Lưu thay đổi
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
