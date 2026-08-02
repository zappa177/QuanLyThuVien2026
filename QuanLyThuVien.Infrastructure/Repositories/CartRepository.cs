using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Application.Interfaces.IRepositories;
using QuanLyThuVien.Domain.Entities;
using QuanLyThuVien.Infrastructure.Data;

namespace QuanLyThuVien.Infrastructure.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _context;

        public CartRepository(ApplicationDbContext context) => _context = context;
        // Lấy danh sách các mục trong giỏ hàng của người đọc
        public async Task<IEnumerable<CartItems>> GetCartItemsByReaderIdAsync(int readerId)
        {
            return await _context.CartItems
                .Include(c => c.Book)
                .Where(c => c.ReaderId == readerId && c.IsActive)
                .ToListAsync();
        }
        // Lấy số lượng mục trong giỏ hàng của người đọc
        public async Task<int> GetCartCountAsync(int readerId)
        {
            return await _context.CartItems.CountAsync(c => c.ReaderId == readerId && c.IsActive);
        }
        // Kiểm tra xem sách đã có trong giỏ hàng của người đọc chưa
        public async Task<bool> IsBookInCartAsync(int readerId, int bookId)
        {
            return await _context.CartItems.AnyAsync(c => c.ReaderId == readerId && c.BookId == bookId && c.IsActive);
        }
        // Thêm mục vào giỏ hàng
        public async Task AddAsync(CartItems cartItem) => await _context.CartItems.AddAsync(cartItem);
        // xóa mục trong giỏ hàng
        public void Remove(CartItems cartItem) => _context.CartItems.Remove(cartItem);
        // Xóa tất cả các mục trong giỏ hàng của người đọc
        public async Task ClearCartAsync(int readerId)
        {
            var items = await _context.CartItems.Where(c => c.ReaderId == readerId).ToListAsync();
            _context.CartItems.RemoveRange(items);
        }
    }
}
