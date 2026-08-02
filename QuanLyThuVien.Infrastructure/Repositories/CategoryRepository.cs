using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Application.Interfaces.IRepositories;
using QuanLyThuVien.Domain.Entities;
using QuanLyThuVien.Infrastructure.Data;


namespace QuanLyThuVien.Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _context;
        public CategoryRepository(ApplicationDbContext context) => _context = context;
        // Lấy tất cả thể loại
        public async Task<IEnumerable<Categories>> GetAllAsync() => await _context.Categories.ToListAsync();
        // Lấy thể loại theo Id
        public async Task<Categories?> GetByIdAsync(int id) => await _context.Categories.FindAsync(id);
        // Thêm thể loại mới
        public async Task AddAsync(Categories category) => await _context.Categories.AddAsync(category);
        // Cập nhật thể loại
        public void Update(Categories category) => _context.Categories.Update(category);
    }
}
