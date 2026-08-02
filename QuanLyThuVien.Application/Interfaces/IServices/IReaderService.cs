using QuanLyThuVien.Domain.Common;
using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Interfaces.IServices
{
    public interface IReaderService
    {
        Task<PagedResult<Readers>> GetPagedReadersAsync(int pageNumber, int pageSize, string? searchTerm);  //lấy độc giả theo phân trang và tìm kiếm
        Task<Readers?> GetReaderByIdAsync(int id);  //lấy độc giả theo id
        Task<Readers?> GetReaderByUserIdAsync(Guid userId); //lấy độc giả theo userId
        Task<bool> CreateReaderAsync(Readers reader);   //thêm độc giả
        Task<bool> UpdateReaderAsync(Readers reader);   //cập nhật độc giả
    }
}
