using QuanLyThuVien.Web.Enums;

namespace QuanLyThuVien.Web.Models
{
    public class BookCopyIndexViewModel
    {
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string? CoverImage { get; set; }

        public List<BookCopyItemViewModel> Copies { get; set; } = new();
    }

    public class BookCopyItemViewModel
    {
        public int Id { get; set; }
        public string CopyCode { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int ShelfId { get; set; }
        public int TierId { get; set; }
        public BookCopyStatus Status { get; set; }
        public bool IsReferenceOnly { get; set; }
        public bool IsActive { get; set; }
    }
}