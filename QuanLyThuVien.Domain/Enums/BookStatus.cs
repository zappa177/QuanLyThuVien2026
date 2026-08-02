namespace QuanLyThuVien.Domain.Enums
{
    public enum BookStatus
    {
        Available = 1,  // Sẵn sàng trên kệ
        Borrowed = 2,   // Đang được mượn
        Damaged = 3,    // Rách/Hỏng
        Lost = 4        // Đã thất lạc
    }
}
