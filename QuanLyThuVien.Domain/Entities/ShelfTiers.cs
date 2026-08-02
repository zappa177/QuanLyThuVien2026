using QuanLyThuVien.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Domain.Entities
{
    //tầng của kệ sách, ví dụ: Tầng 1, Tầng 2, Tầng 3
    public class ShelfTiers : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ShelfId { get; set; }    //mã kệ sách, khóa ngoại đến bảng Shelves

        [Required]
        [MaxLength(50)]
        public required string TierName { get; set; } // Tên tầng (VD: Tầng 1, Tầng 2)

        public int Capacity { get; set; } = 50; // Sức chứa tối đa
        //nhiều tầng thuộc 1 kệ
        public virtual Shelves? Shelf { get; set; }
        //1 tầng có nhiều sách
        public virtual ICollection<Books> Books { get; set; } = new List<Books>();
    }
}
