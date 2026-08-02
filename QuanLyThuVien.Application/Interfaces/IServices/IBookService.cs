using QuanLyThuVien.Domain.Common;
using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Interfaces.IServices
{
    public interface IBookService
    {
        Task<PagedResult<Books>> GetPagedBooksAsync(string? searchTitle, string? searchAuthor, string? searchISBN, int? publishYear, int pageNumber, int pageSize, int? categoryId, string? sortBy, bool? isActiveFilter, bool onlyAvailable);  //lấy sách theo phân trang, tìm kiếm và lọc
        Task<Books?> GetBookByIdAsync(int id);  //lấy sách theo id

        Task<bool> CreateBookAsync(Books book); //thêm sách
        Task<bool> UpdateBookAsync(Books book); //cập nhật sách
        Task<bool> DeleteBookAsync(int id); //xóa sách
        Task<bool> RestoreBookAsync(int id);    //khôi phục sách
    }
}
