using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Interfaces.IServices
{
    public interface ICartService
    {
        Task<IEnumerable<CartItems>> GetMyCartAsync(int readerId);  //lấy giỏ hàng của độc giả theo id
        Task<int> GetCartCountAsync(int readerId);  //đếm số lượng sách trong giỏ hàng của độc giả theo id
        Task<bool> AddToCartAsync(int readerId, int bookId);    //thêm sách vào giỏ hàng của độc giả theo id
        Task<bool> RemoveFromCartAsync(int cartItemId); //xóa sách khỏi giỏ hàng của độc giả theo id
    }
}
