namespace QuanLyThuVien.Web.Models
{
    public class BookDisplayViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string? CoverImage { get; set; }
        public bool IsActive { get; set; }

        // Tổng số lượng bản sao vật lý
        public int TotalCopies { get; set; }

        // Số lượng bản sao còn khả dụng (Status = Available)
        public int AvailableCopies { get; set; }
    }
}
