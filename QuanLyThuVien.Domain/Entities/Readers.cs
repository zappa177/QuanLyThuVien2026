using QuanLyThuVien.Domain.Common;
using QuanLyThuVien.Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Domain.Entities
{
    public class Readers : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required Guid ApplicationUserId { get; set; } // Liên kết với IdentityUser

        [Required]
        [MaxLength(50)]
        public required string StudentCode { get; set; } // Mã học sinh

        [MaxLength(50)]
        public string? ClassName { get; set; }  // Tên lớp
        public virtual ApplicationUser? ApplicationUser { get; set; } // Liên kết với IdentityUser
        public virtual ICollection<BorrowTickets> BorrowTickets { get; set; } = new List<BorrowTickets>();  // Một độc giả có thể có nhiều phiếu mượn
        public virtual ICollection<CartItems> CartItems { get; set; } = new List<CartItems>();  // Một độc giả có thể có nhiều giỏ hàng
    }
}
