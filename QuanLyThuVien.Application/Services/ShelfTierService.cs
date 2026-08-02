using QuanLyThuVien.Application.Interfaces;
using QuanLyThuVien.Application.Interfaces.IRepositories;
using QuanLyThuVien.Application.Interfaces.IServices;
using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Services
{
    public class ShelfTierService : IShelfTierService
    {
        private readonly IShelfTierRepository _repo;
        private readonly IApplicationDbContext _context;

        public ShelfTierService(IShelfTierRepository repo, IApplicationDbContext context)
        {
            _repo = repo;
            _context = context;
        }
        // Kiểm tra xem tầng kệ đã đầy chưa
        public async Task<bool> IsTierFullAsync(int tierId) => await _repo.IsTierFullAsync(tierId);
        // Tạo tầng kệ
        public async Task<bool> CreateTierAsync(ShelfTiers tier)
        {
            await _repo.AddAsync(tier);
            return await _context.SaveChangesAsync() > 0;
        }

        // lấy tầng kệ theo id kệ
        public async Task<IEnumerable<ShelfTiers>> GetAvailableTiersByShelfIdAsync(int shelfId)
        {
            // Lấy tất cả các tầng của kệ này
            var allTiers = await _repo.GetTiersByShelfIdAsync(shelfId);

            var availableTiers = new List<ShelfTiers>(); //tạo danh sách các tầng kệ chưa đầy

            // Lọc ra những tầng chưa đầy
            foreach (var tier in allTiers)
            {
                bool isFull = await _repo.IsTierFullAsync(tier.Id);
                if (!isFull)
                {
                    availableTiers.Add(tier);// thêm tầng kệ chưa đầy vào danh sách
                }
            }

            return availableTiers;
        }
    }
}
