using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Interfaces.IRepositories
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Categories>> GetAllAsync();    //lấy toàn bộ danh mục
        Task<Categories?> GetByIdAsync(int id); //lấy danh mục theo id
        Task AddAsync(Categories category); //thêm danh mục
        void Update(Categories category);   //cập nhật danh mục
    }
}
