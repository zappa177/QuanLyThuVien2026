using Microsoft.AspNetCore.Identity;

namespace QuanLyThuVien.Web.Entities.Identity
{
    //class kế thừa identity role để thêm các thuộc tính bổ sung cho vai trò người dùng
    public class ApplicationRole : IdentityRole<Guid>
    {
        public string? Description { get; set; }
    }
}
