using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Application.Interfaces.IRepositories;
using QuanLyThuVien.Domain.Common;
using QuanLyThuVien.Domain.Entities;
using QuanLyThuVien.Infrastructure.Data;


namespace QuanLyThuVien.Infrastructure.Repositories
{
    public class ReaderRepository : IReaderRepository
    {
        private readonly ApplicationDbContext _context;

        public ReaderRepository(ApplicationDbContext context) => _context = context;
        // Lấy thông tin độc giả theo ID
        public async Task<Readers?> GetByIdAsync(int id)
            => await _context.Readers.FindAsync(id);
        // Lấy thông tin độc giả theo ApplicationUserId
        public async Task<Readers?> GetByApplicationUserIdAsync(Guid userId)
            => await _context.Readers.FirstOrDefaultAsync(r => r.ApplicationUserId == userId);
        // Lấy thông tin độc giả theo và phân trang
        public async Task<PagedResult<Readers>> GetPagedReadersAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            //lấy thông tin độc giả và thông tin người dùng liên quan
            var query = _context.Readers.Include(r => r.ApplicationUser).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                //lọc thông tin độc giả theo tên người dùng hoặc mã sinh viên
                query = query.Where(r =>
                    r.ApplicationUser!.FullName!.ToLower().Contains(searchTerm) ||
                    r.StudentCode.ToLower().Contains(searchTerm));
            }
            //đếm tổng số bản ghi sau khi lọc
            int totalCount = await query.CountAsync();
            //sáp xếp theo ngày tạo giảm dần, bỏ qua các bản ghi trước đó và lấy số lượng bản ghi theo pageSize
            var items = await query.OrderByDescending(r => r.CreatedAt)
                                   .Skip((pageNumber - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            return new PagedResult<Readers>
            {
                Items = items,
                TotalRecords = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        // Thêm mới độc giả
        public async Task AddAsync(Readers reader) => await _context.Readers.AddAsync(reader);
        // Cập nhật thông tin độc giả
        public void Update(Readers reader) => _context.Readers.Update(reader);
    }
}
