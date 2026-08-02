using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Application.Interfaces.IRepositories;
using QuanLyThuVien.Domain.Entities;
using QuanLyThuVien.Infrastructure.Data;

namespace QuanLyThuVien.Infrastructure.Repositories
{
    public class ShelfRepository : IShelfRepository
    {
        private readonly ApplicationDbContext _context;
        public ShelfRepository(ApplicationDbContext context) => _context = context;
        // Lấy danh sách kệ cùng các tầng của chúng
        public async Task<IEnumerable<Shelves>> GetShelvesWithTiersAsync()
        {
            return await _context.Shelves
                .Include(s => s.ShelfTiers) // Load kèm danh sách tầng kệ
                .ToListAsync();
        }
        // Lấy kệ theo ID kệ
        public async Task<Shelves?> GetByIdAsync(int id) => await _context.Shelves.FindAsync(id);
        //thêm kệ mới
        public async Task AddAsync(Shelves shelf) => await _context.Shelves.AddAsync(shelf);
        // cập nhật kệ
        public void Update(Shelves shelf) => _context.Shelves.Update(shelf);
    }
}
