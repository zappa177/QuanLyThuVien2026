using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Interfaces.IServices
{
    public interface IShelfService
    {
        Task<IEnumerable<Shelves>> GetShelvesWithTiersAsync();  //lấy danh sách Kệ kèm theo các Tầng kệ bên trong
    }
}
