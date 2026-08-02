using QuanLyThuVien.Application.Interfaces;
using QuanLyThuVien.Application.Interfaces.IRepositories;
using QuanLyThuVien.Application.Interfaces.IServices;
using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;
        private readonly IApplicationDbContext _context;

        public CategoryService(ICategoryRepository repo, IApplicationDbContext context)
        {
            _repo = repo;
            _context = context;
        }
        // Lấy danh sách tất cả thể loại
        public async Task<IEnumerable<Categories>> GetAllCategoriesAsync() => await _repo.GetAllAsync();
        // Lấy thể loại theo Id
        public async Task<bool> CreateCategoryAsync(Categories category)
        {
            await _repo.AddAsync(category);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
