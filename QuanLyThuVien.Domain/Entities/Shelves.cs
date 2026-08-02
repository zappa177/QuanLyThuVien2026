using QuanLyThuVien.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Domain.Entities
{
    //kệ sách
    public class Shelves : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Name { get; set; } // Dãy kệ (VD: kệ sách 1)
        //1 kệ có nhiều tầng
        public virtual ICollection<ShelfTiers> ShelfTiers { get; set; } = new List<ShelfTiers>();
    }
}
