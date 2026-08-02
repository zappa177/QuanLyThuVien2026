using Microsoft.AspNetCore.Identity;

namespace QuanLyThuVien.Domain.Entities.Identity
{
    //class kế thừa identity user để thêm các thuộc tính bổ sung cho người dùng
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string? FullName { get; set; }
    }
}
