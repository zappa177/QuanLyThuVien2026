using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Interfaces.IRepositories
{
    public interface ICartRepository
    {
        // Lấy toàn bộ sách trong giỏ của một độc giả
        Task<IEnumerable<CartItems>> GetCartItemsByReaderIdAsync(int readerId);

        // Đếm số lượng để hiển thị lên giỏ hàng
        Task<int> GetCartCountAsync(int readerId);

        // Kiểm tra xem sách đã có trong giỏ chưa để tránh thêm trùng
        Task<bool> IsBookInCartAsync(int readerId, int bookId);

        Task AddAsync(CartItems cartItem);  // Thêm sách vào giỏ hàng
        void Remove(CartItems cartItem);    // Xóa sách khỏi giỏ hàng

        Task ClearCartAsync(int readerId);  // Xóa toàn bộ giỏ hàng của một độc giả
    }
}
