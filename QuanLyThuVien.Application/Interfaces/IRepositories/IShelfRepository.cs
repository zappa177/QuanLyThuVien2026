using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Interfaces.IRepositories
{
    public interface IShelfRepository
    {
        // Lấy danh sách Kệ kèm theo các Tầng kệ bên trong
        Task<IEnumerable<Shelves>> GetShelvesWithTiersAsync();
        Task<Shelves?> GetByIdAsync(int id);    //lấy kệ theo id
        Task AddAsync(Shelves shelf);   //thêm kệ
        void Update(Shelves shelf); //cập nhật kệ
    }
}
