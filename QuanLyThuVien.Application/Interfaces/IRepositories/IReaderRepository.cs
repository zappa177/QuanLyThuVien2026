using QuanLyThuVien.Domain.Common;
using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Interfaces.IRepositories
{
    public interface IReaderRepository
    {
        Task<Readers?> GetByIdAsync(int id);    //lấy độc giả theo id
        Task<Readers?> GetByApplicationUserIdAsync(Guid userId);   //lấy độc giả theo userId
        Task<PagedResult<Readers>> GetPagedReadersAsync(int pageNumber, int pageSize, string? searchTerm);  //lấy độc giả theo phân trang và tìm kiếm
        Task AddAsync(Readers reader);    //thêm độc giả
        void Update(Readers reader);   //cập nhật độc giả
    }
}
