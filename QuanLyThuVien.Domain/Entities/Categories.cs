using QuanLyThuVien.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Domain.Entities
{
    public class Categories : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;
        [MaxLength(500)]
        public string? Description { get; set; }
        //1 category có nhiều books
        public virtual ICollection<Books> Books { get; set; } = new List<Books>();
    }
}
