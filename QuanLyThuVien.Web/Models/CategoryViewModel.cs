using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Web.Models
{
    public class CategoryViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập tên thể loại.")]
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
