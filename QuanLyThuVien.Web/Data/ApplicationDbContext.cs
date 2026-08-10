using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Web.Common;
using QuanLyThuVien.Web.Entities;
using QuanLyThuVien.Web.Entities.Identity;

namespace QuanLyThuVien.Web.Data
{

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // CÁC BẢNG TRONG CSDL
        public DbSet<Categories> Categories { get; set; }
        public DbSet<Shelves> Shelves { get; set; }
        public DbSet<ShelfTiers> ShelfTiers { get; set; }
        public DbSet<Books> Books { get; set; }
        public DbSet<BookCopies> BookCopies { get; set; } // BẢNG MỚI: BẢN SAO VẬT LÝ
        public DbSet<BorrowTickets> BorrowTickets { get; set; }
        public DbSet<BorrowTicketDetails> BorrowTicketDetails { get; set; }
        public DbSet<CartItems> CartItems { get; set; }
        public DbSet<SystemSettings> SystemSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Cấu hình ApplicationUser
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("ApplicationUsers");
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.UserCode).HasMaxLength(50);
                entity.Property(e => e.Position).HasMaxLength(50);

                // Đảm bảo Mã người dùng không được trùng lặp (nếu có nhập)
                entity.HasIndex(e => e.UserCode).IsUnique().HasFilter("[UserCode] IS NOT NULL");
            });

            // 2. Cấu hình ApplicationRole
            builder.Entity<ApplicationRole>(entity =>
            {
                entity.ToTable("ApplicationRoles");
                entity.Property(e => e.Description).HasMaxLength(200);
            });

            // 3. Cấu hình Categories
            builder.Entity<Categories>(entity =>
            {
                entity.ToTable("Categories");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // 4. Cấu hình Shelves
            builder.Entity<Shelves>(entity =>
            {
                entity.ToTable("Shelves");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // 5. Cấu hình ShelfTiers
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

            // 6. Cấu hình Books (Tựa sách)
            builder.Entity<Books>(entity =>
            {
                entity.ToTable("Books");
                entity.HasIndex(e => e.ISBN).IsUnique().HasFilter("[ISBN] IS NOT NULL");
                entity.HasOne(e => e.Category)
                      .WithMany(c => c.Books)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 7. Cấu hình BookCopies (Bản sao vật lý - MỚI)
            builder.Entity<BookCopies>(entity =>
            {
                entity.ToTable("BookCopies");
                entity.HasIndex(e => e.CopyCode).IsUnique(); // Mã cá biệt (Mã vạch) là duy nhất
                entity.Property(e => e.Status).HasConversion<string>();

                entity.HasOne(e => e.Book)
                      .WithMany(b => b.BookCopies)
                      .HasForeignKey(e => e.BookId)
                      .OnDelete(DeleteBehavior.Cascade); // Xóa tựa sách thì xóa luôn tất cả sách vật lý

                entity.HasOne(e => e.ShelfTier)
                      .WithMany(st => st.BookCopies)
                      .HasForeignKey(e => e.ShelfTierId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 8. Cấu hình BorrowTickets (Phiếu mượn)
            builder.Entity<BorrowTickets>(entity =>
            {
                entity.ToTable("BorrowTickets", tb =>
                {
                    tb.HasCheckConstraint("CK_BorrowTicket_Dates", "ExpectedReturnDate >= BorrowDate");
                });
                entity.Property(e => e.Status).HasConversion<string>();

                entity.HasOne(bt => bt.User)
                      .WithMany(u => u.BorrowTickets)
                      .HasForeignKey(bt => bt.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 9. Cấu hình BorrowTicketDetails
            builder.Entity<BorrowTicketDetails>(entity =>
            {
                entity.ToTable("BorrowTicketDetails");

                // Đảm bảo 1 phiếu mượn không chọn trùng 2 lần cùng 1 Tựa sách
                //entity.HasIndex(e => new { e.BorrowTicketId, e.BookId }).IsUnique();

                entity.HasOne(btd => btd.BorrowTicket)
                      .WithMany(bt => bt.TicketDetails)
                      .HasForeignKey(btd => btd.BorrowTicketId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(btd => btd.Book)
                      .WithMany(b => b.BorrowTicketDetails)
                      .HasForeignKey(btd => btd.BookId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(btd => btd.BookCopy)
                      .WithMany(bc => bc.BorrowTicketDetails)
                      .HasForeignKey(btd => btd.BookCopyId)
                      .OnDelete(DeleteBehavior.Restrict); // Có thể null ban đầu
            });

            // 10. Cấu hình CartItems
            builder.Entity<CartItems>(entity =>
            {
                entity.ToTable("CartItems");
                entity.HasIndex(ci => new { ci.UserId, ci.BookId }).IsUnique();

                entity.HasOne(ci => ci.User)
                      .WithMany(u => u.CartItems)
                      .HasForeignKey(ci => ci.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ci => ci.Book)
                      .WithMany(b => b.CartItems)
                      .HasForeignKey(ci => ci.BookId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình BaseEntity (Tự động set CreatedAt, IsActive)
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var createdAtProperty = entityType.FindProperty("CreatedAt");
                var isActiveProperty = entityType.FindProperty("IsActive");

                if (createdAtProperty != null && createdAtProperty.ClrType == typeof(DateTime))
                {
                    createdAtProperty.SetDefaultValueSql("GETDATE()");
                }

                if (isActiveProperty != null && isActiveProperty.ClrType == typeof(bool))
                {
                    isActiveProperty.SetDefaultValue(true);
                }
            }
        }

        // Tự động xử lý Xóa cứng / Xóa mềm
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
                        // Cho phép XÓA CỨNG với Giỏ hàng và Chi tiết phiếu
                        if (entry.Entity is CartItems || entry.Entity is BorrowTicketDetails)
                        {
                            break;
                        }

                        // XÓA MỀM với Sách, Kệ, Tầng...
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
