namespace QuanLyThuVien.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        // Khai báo hàm lưu thay đổi bất đồng bộ
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);  // Lưu thay đổi vào cơ sở dữ liệu
    }
}
