using QuanLyThuVien.Web.Common;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Web.Entities
{
    // Thông tin Tựa sách (Đầu sách)
    public class Books : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(50)]
        public string? ISBN { get; set; } // Mã ISBN sách

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(255)]
        public required string Title { get; set; } // Tên sách

        [Required(ErrorMessage = "Author is required")]
        [MaxLength(255)]
        public required string Author { get; set; } // Tác giả

        [MaxLength(255)]
        public string? Publisher { get; set; } // Nhà xuất bản

        public int? PublishYear { get; set; } // Năm xuất bản

        [MaxLength(500)]
        public string? CoverImage { get; set; } // Đường dẫn đến ảnh bìa sách

        [Required]
        public int CategoryId { get; set; } // Khóa ngoại đến bảng Thể loại

        public virtual Categories? Category { get; set; }

        // 1 tựa sách có nhiều bản sao vật lý, mỗi bản sao có thể được mượn riêng lẻ
        public virtual ICollection<BookCopies> BookCopies { get; set; } = new List<BookCopies>();
        // 1 tựa sách có thể được thêm vào nhiều giỏ hàng của người dùng khác nhau
        public virtual ICollection<CartItems> CartItems { get; set; } = new List<CartItems>();
        // 1 tựa sách có thể xuất hiện trong nhiều chi tiết phiếu mượn khác nhau
        public virtual ICollection<BorrowTicketDetails> BorrowTicketDetails { get; set; } = new List<BorrowTicketDetails>();
    }
}