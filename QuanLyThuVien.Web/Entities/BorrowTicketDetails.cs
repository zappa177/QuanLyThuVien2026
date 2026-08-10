using QuanLyThuVien.Web.Common;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Web.Entities
{
    public class BorrowTicketDetails : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BorrowTicketId { get; set; }
        public virtual BorrowTickets? BorrowTicket { get; set; }

        [Required]
        public int BookId { get; set; } // Lưu lại Tựa sách mà độc giả yêu cầu lúc đầu
        public virtual Books? Book { get; set; }

        // tùy thuộc vào sách mà thủ thư duyệt
        public int? BookCopyId { get; set; }
        public virtual BookCopies? BookCopy { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; } // Ghi chú riêng cho tình trạng của cuốn sách này khi mượn/trả
    }
}
