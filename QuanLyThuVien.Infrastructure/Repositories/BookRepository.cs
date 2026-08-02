using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Application.Interfaces.IRepositories;
using QuanLyThuVien.Domain.Common;
using QuanLyThuVien.Domain.Entities;
using QuanLyThuVien.Domain.Enums;
using QuanLyThuVien.Infrastructure.Data;

namespace QuanLyThuVien.Infrastructure.Repositories
{
    public class BookRepository : IBookRepository
    {
        private readonly ApplicationDbContext _context;

        public BookRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        //lấy tất cả sách
        public async Task<IEnumerable<Books>> GetAllAsync()
            => await _context.Books.ToListAsync();
        //lấy sách theo id
        public async Task<Books?> GetByIdAsync(int id)
        {
            return await _context.Books
        .Include(b => b.Category)             // Nạp thông tin Thể loại
        .Include(b => b.ShelfTier)            // Nạp thông tin Tầng
            .ThenInclude(st => st!.Shelf)      // Nạp thông tin Kệ (thông qua Tầng)
        .FirstOrDefaultAsync(b => b.Id == id);
        }
        //thêm sách
        public async Task AddAsync(Books book)
            => await _context.Books.AddAsync(book);
        //cập nhật sách
        public void Update(Books book)
            => _context.Books.Update(book);
        //xóa sách
        public void Delete(Books book)
            => _context.Books.Remove(book);

        // kiểm tra ISBN đã tồn tại hay chưa,loại trừ một Id của sách đang được cập nhật
        public async Task<bool> IsIsbnExistsAsync(string isbn, int? excludeId = null)
        {
            var query = _context.Books.Where(b => b.ISBN == isbn);
            if (excludeId.HasValue)
            {
                query = query.Where(b => b.Id != excludeId.Value);
            }
            return await query.AnyAsync();
        }
        // Lấy danh sách sách theo tiêu chí tìm kiếm và phân trang
        public async Task<PagedResult<Books>> GetPagedBooksAsync(
            int pageNumber, int pageSize, string? searchTitle, string? searchAuthor, string? searchISBN, int? publishYear, int? categoryId, string? sortBy, bool? isActiveFilter = null, bool onlyAvailable = false)
        {
            var query = _context.Books
                 .Include(b => b.Category)
                 .Include(b => b.ShelfTier)
                     .ThenInclude(st => st!.Shelf)
                 .AsQueryable();
            //chỉ lấy sách có trạng thái IsActive = true nếu isActiveFilter = true, hoặc IsActive = false nếu isActiveFilter = false
            if (isActiveFilter.HasValue)
            {
                query = query.Where(b => b.IsActive == isActiveFilter.Value);
            }
            //chỉ lấy sách có trạng thái Status = BookStatus.Available nếu onlyAvailable = true
            if (onlyAvailable)
            {
                // Lọc những sách có Status là BookStatus.Available (Sẵn sàng trên kệ)
                query = query.Where(b => b.Status == BookStatus.Available);
            }
            //tìm kiến theo tên sách
            if (!string.IsNullOrWhiteSpace(searchTitle))
            {
                searchTitle = searchTitle.ToLower();
                query = query.Where(b => b.Title.ToLower().Contains(searchTitle));
            }
            //tìm kiếm tên tác giả
            if (!string.IsNullOrWhiteSpace(searchAuthor))
            {
                searchAuthor = searchAuthor.ToLower();
                query = query.Where(b => b.Author.ToLower().Contains(searchAuthor));
            }
            //tìm kiếm theo mã sách
            if (!string.IsNullOrWhiteSpace(searchISBN))
            {
                searchISBN = searchISBN.ToLower();
                query = query.Where(b => b.ISBN.ToLower().Contains(searchISBN));
            }
            //tìm kiếm theo năm xuất bản
            if (publishYear.HasValue)
            {
                query = query.Where(b => b.PublishYear == publishYear.Value);
            }
            //tim kiếm theo thể loại sách
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(b => b.CategoryId == categoryId.Value);
            }

            int totalCount = await query.CountAsync();// Lấy tổng số bản ghi trước khi phân trang
            // Sắp xếp kết quả theo tiêu chí sortBy
            query = sortBy switch
            {
                "year_desc" => query.OrderByDescending(b => b.PublishYear),
                "year_asc" => query.OrderBy(b => b.PublishYear),
                "title_desc" => query.OrderByDescending(b => b.Title),
                "title_asc" => query.OrderBy(b => b.Title),
                _ => query.OrderByDescending(b => b.CreatedAt)
            };
            // Phân trang kết quả
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<Books> { Items = items, TotalRecords = totalCount, PageNumber = pageNumber, PageSize = pageSize };
        }
    }
}
