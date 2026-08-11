using QuanLyThuVien.Web.Common;
using QuanLyThuVien.Web.Entities.Identity;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Web.Entities
{
    public class CartItems : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid UserId { get; set; } // UserId của người dùng đang thêm sách vào giỏ

        [Required]
        public int BookId { get; set; } // Tựa sách được thêm vào giỏ

        [Required]
        public int Quantity { get; set; } = 1;

        public virtual ApplicationUser? User { get; set; }
        public virtual Books? Book { get; set; }
    }
}
