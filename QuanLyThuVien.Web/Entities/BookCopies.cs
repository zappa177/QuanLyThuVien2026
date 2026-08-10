using QuanLyThuVien.Web.Common;
using QuanLyThuVien.Web.Enums;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Web.Entities
{
    public class BookCopies : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public required string CopyCode { get; set; } // Mã của từng cuốn

        [Required]
        public int BookId { get; set; } // Thuộc về Tựa sách nào
        public virtual Books? Book { get; set; }

        [Required]
        public int ShelfTierId { get; set; } // Cuốn này đang nằm ở kệ nào/tầng nào
        public virtual ShelfTiers? ShelfTier { get; set; }

        [Required]
        public BookCopyStatus Status { get; set; } = BookCopyStatus.Available; // Tình trạng: Available, Borrowed, Damaged, Lost

        // Cờ đánh dấu: true = Chỉ đọc tại chỗ, false = Được mượn về nhà
        public bool IsReferenceOnly { get; set; } = false;

        // cuốn sách vật lý này có thể xuất hiện trong nhiều chi tiết phiếu mượn khác nhau
        public virtual ICollection<BorrowTicketDetails> BorrowTicketDetails { get; set; } = new List<BorrowTicketDetails>();
    }
}
