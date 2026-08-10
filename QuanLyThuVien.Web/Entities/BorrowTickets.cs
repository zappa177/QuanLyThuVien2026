using QuanLyThuVien.Web.Common;
using QuanLyThuVien.Web.Entities.Identity;
using QuanLyThuVien.Web.Enums;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Web.Entities
{
    public class BorrowTickets : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid UserId { get; set; } // UserId của người dùng đang mượn sách

        [Required]
        public DateTime BorrowDate { get; set; } // Ngày tạo phiếu

        [Required]
        public DateTime ExpectedReturnDate { get; set; } // Ngày dự kiến trả sách

        public DateTime? ActualReturnDate { get; set; } // Ngày thực tế trả sách  

        [Required]
        public BorrowStatus Status { get; set; } = BorrowStatus.Pending;

        [MaxLength(500)]
        public string? Note { get; set; }

        public virtual ApplicationUser? User { get; set; }

        public virtual ICollection<BorrowTicketDetails> TicketDetails { get; set; } = new List<BorrowTicketDetails>();
    }
}
