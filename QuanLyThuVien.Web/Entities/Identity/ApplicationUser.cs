using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Web.Entities.Identity
{
    // Class kế thừa IdentityUser để thêm các thuộc tính bổ sung cho người dùng
    public class ApplicationUser : IdentityUser<Guid>
    {
        [MaxLength(100)]
        public string? FullName { get; set; } = string.Empty;

        // Mã người dùng
        [MaxLength(50)]
        public string? UserCode { get; set; }

        // chức vụ ( học sinh , giáo viên, nhân viên)
        [MaxLength(50)]
        public string? Position { get; set; }

        // 1 người dùng có thể có nhiều giỏ hàng và phiếu mượn
        public virtual ICollection<CartItems> CartItems { get; set; } = new List<CartItems>();
        public virtual ICollection<BorrowTickets> BorrowTickets { get; set; } = new List<BorrowTickets>();
    }
}