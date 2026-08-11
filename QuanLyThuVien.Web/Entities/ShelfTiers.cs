using QuanLyThuVien.Web.Common;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Web.Entities
{
    //tầng của kệ sách, ví dụ: Tầng 1, Tầng 2, Tầng 3
    public class ShelfTiers : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ShelfId { get; set; }

        [Required]
        [MaxLength(50)]
        public required string TierName { get; set; }

        public int Capacity { get; set; } = 50;

        public virtual Shelves? Shelf { get; set; }

        //Tầng chứa Bản sao vật lý, không chứa Tựa sách
        public virtual ICollection<BookCopies> BookCopies { get; set; } = new List<BookCopies>();
    }
}
