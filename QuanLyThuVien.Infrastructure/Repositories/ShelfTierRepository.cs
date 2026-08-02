using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Application.Interfaces.IRepositories;
using QuanLyThuVien.Domain.Entities;
using QuanLyThuVien.Infrastructure.Data;


namespace QuanLyThuVien.Infrastructure.Repositories
{
    public class ShelfTierRepository : IShelfTierRepository
    {
        private readonly ApplicationDbContext _context;

        public ShelfTierRepository(ApplicationDbContext context) => _context = context;
        // Lấy tầng kệ theo ID tầng kệ
        public async Task<ShelfTiers?> GetByIdAsync(int id)
            => await _context.ShelfTiers.FindAsync(id);
        // Lấy tất cả tầng kệ theo ID kệ
        public async Task<IEnumerable<ShelfTiers>> GetTiersByShelfIdAsync(int shelfId)
        {
            return await _context.ShelfTiers
                .Where(t => t.ShelfId == shelfId && t.IsActive)
                .ToListAsync();
        }
        // Kiểm tra xem tầng kệ có đầy hay không
        public async Task<bool> IsTierFullAsync(int tierId)
        {
            var tier = await _context.ShelfTiers.FindAsync(tierId);
            if (tier == null) return true;

            // Đếm số lượng sách đang được xếp trên tầng kệ này
            int currentBooksCount = await _context.Books
                .CountAsync(b => b.ShelfTierId == tierId && b.IsActive);

            return currentBooksCount >= tier.Capacity;
        }
        // Thêm tầng kệ mới
        public async Task AddAsync(ShelfTiers tier) => await _context.ShelfTiers.AddAsync(tier);
        // Cập nhật thông tin tầng kệ
        public void Update(ShelfTiers tier) => _context.ShelfTiers.Update(tier);
        // Xóa tầng kệ
        public void Delete(ShelfTiers tier) => _context.ShelfTiers.Remove(tier);
    }
}
