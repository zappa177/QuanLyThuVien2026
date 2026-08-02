using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Application.Interfaces;
using QuanLyThuVien.Domain.Common;
using QuanLyThuVien.Domain.Entities;
using QuanLyThuVien.Domain.Entities.Identity;

namespace QuanLyThuVien.Infrastructure.Data
{

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        //các bảng trong csdl
        public DbSet<Categories> Categories { get; set; }
        public DbSet<Shelves> Shelves { get; set; }
        public DbSet<ShelfTiers> ShelfTiers { get; set; }
        public DbSet<Books> Books { get; set; }
        public DbSet<Readers> Readers { get; set; }
        public DbSet<BorrowTickets> BorrowTickets { get; set; }
        public DbSet<BorrowTicketDetails> BorrowTicketDetails { get; set; }
        public DbSet<CartItems> CartItems { get; set; }
        public object TicketDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            //categories table configuration
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("ApplicationUsers");
                entity.Property(e => e.FullName).HasMaxLength(100);
            });
            builder.Entity<ApplicationRole>(entity =>
            {
                entity.ToTable("ApplicationRoles");
                entity.Property(e => e.Description).HasMaxLength(200);
            });
            builder.Entity<Categories>(entity =>
            {
                entity.ToTable("Categories");
                entity.HasIndex(e => e.Name).IsUnique();
            });
            //shelves table configuration
            builder.Entity<Shelves>(entity =>
            {
                entity.ToTable("Shelves");
                entity.HasIndex(e => e.Name).IsUnique();
            });
            builder.Entity<ShelfTiers>(entity =>
            {
                entity.ToTable("ShelfTiers", tb =>
                {
                    tb.HasCheckConstraint("CK_ShelfTiers_Capacity", "Capacity >= 0");
                });
                entity.HasIndex(e => new { e.ShelfId, e.TierName }).IsUnique();
                entity.HasOne(e => e.Shelf)
                      .WithMany(s => s.ShelfTiers)
                      .HasForeignKey(e => e.ShelfId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<Books>(entity =>
            {
                entity.ToTable("Books");
                entity.HasIndex(e => e.ISBN).IsUnique();
                entity.Property(e => e.Status).HasConversion<string>();
                entity.HasOne(e => e.Category)
                      .WithMany(c => c.Books)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ShelfTier)
                      .WithMany(st => st.Books)
                      .HasForeignKey(e => e.ShelfTierId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<Readers>(entity =>
            {
                entity.ToTable("Readers");
                entity.HasIndex(e => e.StudentCode).IsUnique();
                entity.HasOne(e => e.ApplicationUser)
                      .WithOne()
                      .HasForeignKey<Readers>(e => e.ApplicationUserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            builder.Entity<BorrowTickets>(entity =>
            {
                entity.ToTable("BorrowTickets", tb =>
                {
                    tb.HasCheckConstraint("CK_BorrowTicket_Dates", "ExpectedReturnDate >= BorrowDate");
                });
                entity.Property(e => e.Status).HasConversion<string>();
                entity.HasOne(bt => bt.Reader)
                      .WithMany(r => r.BorrowTickets)
                      .HasForeignKey(bt => bt.ReaderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            builder.Entity<BorrowTicketDetails>(entity =>
            {
                entity.ToTable("BorrowTicketDetails");
                entity.HasIndex(e => new { e.BorrowTicketId, e.BookId }).IsUnique();
                entity.HasOne(btd => btd.BorrowTicket)
                      .WithMany(bt => bt.TicketDetails)
                      .HasForeignKey(btd => btd.BorrowTicketId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(btd => btd.Book)
                      .WithMany(b => b.BorrowTicketDetails)
                      .HasForeignKey(btd => btd.BookId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<CartItems>(entity =>
            {
                entity.ToTable("CartItems");
                entity.HasIndex(ci => new { ci.ReaderId, ci.BookId }).IsUnique();
                entity.HasOne(ci => ci.Reader)
                      .WithMany(r => r.CartItems)
                      .HasForeignKey(ci => ci.ReaderId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(ci => ci.Book)
                      .WithMany(b => b.CartItems)
                      .HasForeignKey(ci => ci.BookId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var createdAtProperty = entityType.FindProperty("CreatedAt");
                var isActiveProperty = entityType.FindProperty("IsActive");

                if (createdAtProperty != null && createdAtProperty.ClrType == typeof(DateTime))
                {
                    // SQL Server tự sinh giờ hệ thống khi có lệnh Insert mới
                    createdAtProperty.SetDefaultValueSql("GETDATE()");
                }

                if (isActiveProperty != null && isActiveProperty.ClrType == typeof(bool))
                {
                    isActiveProperty.SetDefaultValue(true);
                }
            }
        }
        //tạo lại savechanges để tự động set CreatedAt và IsActive
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.Now;
                        entry.Entity.IsActive = true;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.Now;
                        break;

                    case EntityState.Deleted:
                        // Nếu là Giỏ hàng hoặc Chi tiết phiếu mượn thì CHO PHÉP XÓA CỨNG
                        if (entry.Entity is CartItems || entry.Entity is BorrowTicketDetails)
                        {
                            break; // Thoát khỏi case, giữ nguyên trạng thái Deleted
                        }

                        // Các bảng khác (Sách, Độc giả, Phiếu mượn...) thì Xóa mềm (Cập nhật IsActive = false)
                        entry.State = EntityState.Modified;
                        entry.Entity.IsActive = false;
                        entry.Entity.UpdatedAt = DateTime.Now;
                        break;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
