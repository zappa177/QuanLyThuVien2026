using QuanLyThuVien.Application.Interfaces.IRepositories;
using QuanLyThuVien.Application.Interfaces.IServices;
using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Services
{
    public class ShelfService : IShelfService
    {
        private readonly IShelfRepository _repo;
        public ShelfService(IShelfRepository repo) => _repo = repo;

        public async Task<IEnumerable<Shelves>> GetShelvesWithTiersAsync() => await _repo.GetShelvesWithTiersAsync();// Lấy danh sách kệ cùng các tầng của chúng
    }
}
