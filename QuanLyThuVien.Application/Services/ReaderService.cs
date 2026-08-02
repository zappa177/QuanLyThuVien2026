using QuanLyThuVien.Application.Interfaces;
using QuanLyThuVien.Application.Interfaces.IRepositories;
using QuanLyThuVien.Application.Interfaces.IServices;
using QuanLyThuVien.Domain.Common;
using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Services
{
    public class ReaderService : IReaderService
    {
        private readonly IReaderRepository _readerRepo;
        private readonly IApplicationDbContext _context;

        public ReaderService(IReaderRepository readerRepo, IApplicationDbContext context)
        {
            _readerRepo = readerRepo;
            _context = context;
        }
        // Lấy danh sách độc giả theo phân trang và tìm kiếm
        public async Task<PagedResult<Readers>> GetPagedReadersAsync(int pageNumber, int pageSize, string? searchTerm)
            => await _readerRepo.GetPagedReadersAsync(pageNumber, pageSize, searchTerm);
        // Lấy thông tin độc giả theo ID
        public async Task<Readers?> GetReaderByIdAsync(int id) => await _readerRepo.GetByIdAsync(id);
        // Lấy thông tin độc giả theo ApplicationUserId
        public async Task<Readers?> GetReaderByUserIdAsync(Guid userId) => await _readerRepo.GetByApplicationUserIdAsync(userId);
        // Tạo mới một độc giả
        public async Task<bool> CreateReaderAsync(Readers reader)
        {
            await _readerRepo.AddAsync(reader);
            return await _context.SaveChangesAsync() > 0;
        }
        // Cập nhật thông tin độc giả
        public async Task<bool> UpdateReaderAsync(Readers reader)
        {
            _readerRepo.Update(reader);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
