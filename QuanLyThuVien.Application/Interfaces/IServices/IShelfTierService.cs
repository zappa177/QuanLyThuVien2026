using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Interfaces.IServices
{
    public interface IShelfTierService
    {
        Task<bool> CreateTierAsync(ShelfTiers tier);    // tạo thêm tầng kệ 
        Task<bool> IsTierFullAsync(int tierId); // kiểm tra tầng kệ đã đầy chưa
        Task<IEnumerable<ShelfTiers>> GetAvailableTiersByShelfIdAsync(int shelfId); // lấy danh sách các tầng kệ còn chỗ trống theo shelfId
    }
}
