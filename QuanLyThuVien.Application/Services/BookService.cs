using QuanLyThuVien.Application.Interfaces;
using QuanLyThuVien.Application.Interfaces.IRepositories;
using QuanLyThuVien.Application.Interfaces.IServices;
using QuanLyThuVien.Domain.Common;
using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Services
{
    public class BookService : IBookService
    {
        private readonly IBookRepository _bookRepository;
        private readonly IApplicationDbContext _context;

        public BookService(IBookRepository bookRepository, IApplicationDbContext context)
        {
            _bookRepository = bookRepository;
            _context = context;
        }
        // Lấy danh sách sách theo tiêu chí tìm kiếm và phân trang
        public async Task<PagedResult<Books>> GetPagedBooksAsync(string? searchTitle, string? searchAuthor, string? searchISBN, int? publishYear, int pageNumber, int pageSize, int? categoryId, string? sortBy, bool? isActiveFilter, bool onlyAvailable)
        {
            return await _bookRepository.GetPagedBooksAsync(pageNumber, pageSize, searchTitle, searchAuthor, searchISBN, publishYear, categoryId, sortBy, isActiveFilter, onlyAvailable);
        }
        // Lấy thông tin chi tiết của một sách theo ID
        public async Task<Books?> GetBookByIdAsync(int id)
        {
            return await _bookRepository.GetByIdAsync(id);
        }
        // Tạo mới một sách
        public async Task<bool> CreateBookAsync(Books book)
        {
            // Kiểm tra trùng mã ISBN
            if (await _bookRepository.IsIsbnExistsAsync(book.ISBN))
            {
                throw new Exception("Mã ISBN này đã tồn tại trong hệ thống.");
            }

            await _bookRepository.AddAsync(book);

            // Chốt lưu dữ liệu
            return await _context.SaveChangesAsync() > 0;
        }
        // Cập nhật thông tin sách
        public async Task<bool> UpdateBookAsync(Books book)
        {
            if (await _bookRepository.IsIsbnExistsAsync(book.ISBN, book.Id))
            {
                throw new Exception("Mã ISBN này đã bị trùng với một sách khác.");
            }

            _bookRepository.Update(book);
            return await _context.SaveChangesAsync() > 0;
        }
        // Xóa sách (xóa mềm)
        public async Task<bool> DeleteBookAsync(int id)
        {
            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null) return false;

            // Chuyển sang xóa mềm
            book.IsActive = false;
            _bookRepository.Update(book);

            return await _context.SaveChangesAsync() > 0;
        }
        // Khôi phục sách đã xóa mềm
        public async Task<bool> RestoreBookAsync(int id)
        {
            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null) return false;
            // Khôi phục sách
            book.IsActive = true;
            _bookRepository.Update(book);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
