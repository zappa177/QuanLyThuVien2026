using QuanLyThuVien.Domain.Common;
using QuanLyThuVien.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Domain.Entities
{
    public class BorrowTickets : BaseEntity
    {
        [Key]
        public int Id { get; set; } //mã phiếu mượn

        [Required]
        public int ReaderId { get; set; }   //mã độc giả

        [Required]
        public DateTime BorrowDate { get; set; }    //ngày mượn

        [Required]
        public DateTime ExpectedReturnDate { get; set; }    //ngày dự kiến trả

        public DateTime? ActualReturnDate { get; set; } //ngày trả thực tế, có thể null nếu chưa trả

        [Required]
        public BorrowStatus Status { get; set; } = BorrowStatus.Pending;    //trạng thái phiếu mượn, mặc định là Pending

        [MaxLength(500)]
        public string? Note { get; set; }   //ghi chú phiếu mượn

        public virtual Readers? Reader { get; set; }    //một phiếu mượn thuộc về một độc giả

        public virtual ICollection<BorrowTicketDetails> TicketDetails { get; set; } = new List<BorrowTicketDetails>();//một phiếu mượn có thể có nhiều chi tiết phiếu mượn
    }
}
