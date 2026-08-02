using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Interfaces.IRepositories
{
    public interface IShelfTierRepository
    {
        Task<ShelfTiers?> GetByIdAsync(int id); // Lấy tầng kệ theo id

        // Lấy danh sách các tầng thuộc về một kệ sách cụ thể
        Task<IEnumerable<ShelfTiers>> GetTiersByShelfIdAsync(int shelfId);

        // Kiểm tra xem tầng kệ đã đầy chưa (dựa trên sức chứa)
        Task<bool> IsTierFullAsync(int tierId);

        Task AddAsync(ShelfTiers tier); // Thêm tầng kệ
        void Update(ShelfTiers tier);   // Cập nhật tầng kệ
        void Delete(ShelfTiers tier);   // Xóa tầng kệ
    }
}
