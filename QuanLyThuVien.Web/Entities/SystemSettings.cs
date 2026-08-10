using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Web.Entities
{
    public class SystemSettings
    {
        [Key]
        [MaxLength(50)]
        public string SettingKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string SettingValue { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; } // Ghi chú để Admin hiểu cài đặt này làm gì
    }
}
