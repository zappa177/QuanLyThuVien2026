using QuanLyThuVien.Domain.Common;
using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Interfaces.IRepositories
{
    public interface IBookRepository
    {
        Task<IEnumerable<Books>> GetAllAsync(); //lấy toàn bộ sách
        Task<Books?> GetByIdAsync(int id);  //lấ sách theo id
        Task AddAsync(Books book);  //thêm sách
        void Update(Books book);    //cập nhật sách
        void Delete(Books book);    //xóa sách

        Task<PagedResult<Books>> GetPagedBooksAsync(
            int pageNumber, int pageSize, string? searchTitle, string? searchAuthor, string? searchISBN, int? publishYear, int? categoryId, string? sortBy, bool? isActiveFilter, bool onlyAvailable);  //lấy sách theo phân trang, tìm kiếm và lọc

        Task<bool> IsIsbnExistsAsync(string isbn, int? excludeId = null);   //kiểm tra xem ISBN đã tồn tại chưa, để cập nhật sách , trừ trường hợp sách đang cập nhật(excludeId)
    }
}
