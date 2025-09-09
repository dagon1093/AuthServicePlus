using AuthServicePlus.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace AuthServicePlus.Persistence.Context
{
    public class ApplicationDbContext: DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RefreshToken>(b =>
            {
                b.HasKey(rt => rt.Id);

                b.Property(rt => rt.Token)
                    .IsRequired()
                    .HasMaxLength(512);

                b.HasIndex(rt => rt.Token).IsUnique();

                b.HasOne(rt => rt.User)
                 .WithMany(u => u.RefreshTokens)
                 .HasForeignKey(rt => rt.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
