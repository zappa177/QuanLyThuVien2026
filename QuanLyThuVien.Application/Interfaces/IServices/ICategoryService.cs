using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Interfaces.IServices
{
    public interface ICategoryService
    {
        Task<IEnumerable<Categories>> GetAllCategoriesAsync();  //lấy toàn bộ danh mục
        Task<bool> CreateCategoryAsync(Categories category);    //thêm danh mục
    }
}
