namespace QuanLyThuVien.Web.Enums
{
    public enum BookCopyStatus
    {
        Available = 0,  // Sẵn sàng trên kệ
        OnHold = 1,     // Đang giữ chỗ
        Borrowed = 2,   // Đang được mượn
        Damaged = 3,    // Rách/Hỏng
        Lost = 4        // Đã thất lạc
    }
}
