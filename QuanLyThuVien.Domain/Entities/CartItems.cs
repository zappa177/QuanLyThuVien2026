using QuanLyThuVien.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien.Domain.Entities
{
    public class CartItems : BaseEntity
    {
        [Key]
        public int Id { get; set; } //mã giỏ hàng

        [Required]
        public int ReaderId { get; set; }   //mã độc giả

        [Required]
        public int BookId { get; set; } //mã sách

        public virtual Readers? Reader { get; set; }    //một giỏ hàng thuộc về một độc giả

        public virtual Books? Book { get; set; }    //một giỏ hàng thuộc về một sách
    }
}
