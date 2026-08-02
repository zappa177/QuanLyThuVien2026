namespace QuanLyThuVien.Web.Models
{
    public class PdfReportModel
    {
        public string ReportType { get; set; } = "category";
        public List<CategoryReportViewModel>? CategoryData { get; set; }
        public List<StatusReportViewModel>? StatusData { get; set; }
        public List<TopBookReportViewModel>? TopBookData { get; set; }
    }
}
