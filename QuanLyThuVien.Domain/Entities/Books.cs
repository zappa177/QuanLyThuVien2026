using QuanLyThuVien.Domain.Common;
using QuanLyThuVien.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Domain.Entities
{
    //kế thừa base entity để có các thuộc tính chung như isActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
    public class Books : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "ISBN is required")]
        [MaxLength(50)]
        public required string ISBN { get; set; } //mã sách

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(255)]
        public required string Title { get; set; } //tên sách

        [Required(ErrorMessage = "Author is required")]
        [MaxLength(255)]
        public required string Author { get; set; } //tác giả

        [MaxLength(255)]
        public string? Publisher { get; set; } //nhà xuất bản

        public int? PublishYear { get; set; } //năm xuất bản

        [MaxLength(500)]
        public string? CoverImage { get; set; } //đường dẫn đến ảnh bìa sách

        [Required]//tình trang sách, mặc định là Available
        public BookStatus Status { get; set; } = BookStatus.Available; //enum BookStatus trong QuanLyThuVien.Domain.Enums

        //khóa ngoại đến bảng Categories và ShelfTiers
        public int CategoryId { get; set; }
        public int ShelfTierId { get; set; }

        public virtual Categories? Category { get; set; } //mối quan hệ nhiều sách thuộc một thể loại
        public virtual ShelfTiers? ShelfTier { get; set; } //mối quan hệ nhiều sách thuộc một tầng kệ
        public virtual ICollection<CartItems> CartItems { get; set; } = new List<CartItems>(); //1 sách có thể có trong nhiều giỏ hàng
        public virtual ICollection<BorrowTicketDetails> BorrowTicketDetails { get; set; } = new List<BorrowTicketDetails>(); //1 sách có thể trong nhiều chi tiết phiếu mượn
    }
}
