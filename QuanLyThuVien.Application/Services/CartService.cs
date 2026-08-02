using QuanLyThuVien.Application.Interfaces;
using QuanLyThuVien.Application.Interfaces.IRepositories;
using QuanLyThuVien.Application.Interfaces.IServices;
using QuanLyThuVien.Domain.Entities;

namespace QuanLyThuVien.Application.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IBookRepository _bookRepository;
        private readonly IApplicationDbContext _context;

        public CartService(ICartRepository cartRepository, IBookRepository bookRepository, IApplicationDbContext context)
        {
            _cartRepository = cartRepository;
            _bookRepository = bookRepository;
            _context = context;
        }
        // Lấy danh sách sách trong giỏ hàng của độc giả
        public async Task<IEnumerable<CartItems>> GetMyCartAsync(int readerId)
        {
            return await _cartRepository.GetCartItemsByReaderIdAsync(readerId);
        }
        // Lấy số lượng sách trong giỏ hàng của độc giả
        public async Task<int> GetCartCountAsync(int readerId)
        {
            return await _cartRepository.GetCartCountAsync(readerId);
        }
        // Thêm sách vào giỏ hàng
        public async Task<bool> AddToCartAsync(int readerId, int bookId)
        {
            // Kiểm tra sách có tồn tại và còn sẵn sàng không
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null || book.Status != Domain.Enums.BookStatus.Available)
            {
                throw new Exception("Sách không tồn tại hoặc đã được mượn.");
            }

            // Kiểm tra xem sách đã có trong giỏ chưa
            if (await _cartRepository.IsBookInCartAsync(readerId, bookId))
            {
                throw new Exception("Sách này đã có trong giỏ hàng của bạn.");
            }

            //Kiểm tra giới hạn giỏ hàng
            int currentCount = await _cartRepository.GetCartCountAsync(readerId);
            if (currentCount >= 2)
            {
                throw new Exception("Giỏ hàng của bạn đã đầy (Tối đa 2 quyển).");
            }

            // Thêm vào giỏ
            var cartItem = new CartItems
            {
                ReaderId = readerId,
                BookId = bookId,
                IsActive = true
            };

            await _cartRepository.AddAsync(cartItem);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> RemoveFromCartAsync(int cartItemId)
        {

            // _cartRepository.Remove(item);
            // return await _context.SaveChangesAsync() > 0;
            return true;
        }
    }
}
