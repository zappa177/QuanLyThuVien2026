namespace QuanLyThuVien.Web.Common // (Thay đổi namespace cho đúng với project của bạn)
{
    public class PagedResult<T>
    {
        // danh sách dữ liệu để hiển thị
        public List<T> Items { get; set; } = new List<T>();

        // thông số cơ bản để phân trang
        public int TotalRecords { get; set; } //tổng số dòng dữ liệu
        public int PageNumber { get; set; } //trang hiện tại
        public int PageSize { get; set; } //số dòng dữ liệu trên 1 trang


        // Tổng số trang = Tổng số dòng / Số dòng 1 trang (Làm tròn lên)
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        // Số trang hiện tại lớn hơn 1 thì có trang trước, ngược lại không có trang trước
        public bool HasPreviousPage => PageNumber > 1;

        // Số trang hiện tại nhỏ hơn tổng số trang thì có trang sau, ngược lại không có trang sau
        public bool HasNextPage => PageNumber < TotalPages;

        // Constructor
        public PagedResult()
        {
        }

        // Constructor có tham số để dùng trong hàm phân trang(GetPagedBooksAsync)
        public PagedResult(List<T> items, int totalRecords, int pageNumber, int pageSize)
        {
            Items = items;
            TotalRecords = totalRecords;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}