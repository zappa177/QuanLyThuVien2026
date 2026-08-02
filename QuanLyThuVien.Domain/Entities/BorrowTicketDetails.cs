using QuanLyThuVien.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Domain.Entities
{
    public class BorrowTicketDetails : BaseEntity
    {
        [Key]
        public int Id { get; set; } //mã chi tiết phiếu mượn

        [Required]
        public int BorrowTicketId { get; set; } //mã phiếu mượn

        [Required]
        public int BookId { get; set; } //mã sách

        [MaxLength(255)]
        public string? Note { get; set; } // Ghi chú tình trạng sách 

        public virtual BorrowTickets? BorrowTicket { get; set; }    //phiếu mượn có thể có nhiều chi tiết phiếu mượn

        public virtual Books? Book { get; set; }    //1 sách có thể trong nhiều chi tiết phiếu mượn
    }
}
