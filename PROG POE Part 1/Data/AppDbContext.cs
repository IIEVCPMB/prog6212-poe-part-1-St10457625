using Microsoft.EntityFrameworkCore;
using PROG_POE_Part_1.Models;

namespace PROG_POE_Part_1.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<ClaimReview> ClaimReviews { get; set; }
        public DbSet<UploadedDocument> UploadedDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasMany<Claim>()
                .WithOne()
                .HasForeignKey(c => c.Lecturer_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Claim>()
                .HasMany(c => c.Reviews)
                .WithOne()
                .HasForeignKey(r => r.ClaimID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Claim>()
                .HasMany(c => c.Documents)
                .WithOne()
                .HasForeignKey(d => d.ID) 
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>().Property(u => u.HourlyRate).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Claim>().Property(c => c.Total_Hours).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Claim>().Property(c => c.Hourly_Rate).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Claim>().Property(c => c.Total_Payment).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<User>().HasData(
               new User
               {
                   UserID = 1,
                   Name = "Alice",
                   Surname = "HR",
                   Email = "alice.hr@example.com",
                   Password = "1234",
                   Role = "HR",
                   HourlyRate = 0
               },
               new User
               {
                   UserID = 2,
                   Name = "Bob",
                   Surname = "Lecturer",
                   Email = "bob.lecturer@example.com",
                   Password = "1234",
                   Role = "Lecturer",
                   HourlyRate = 200
               },
               new User
               {
                   UserID = 3,
                   Name = "Charlie",
                   Surname = "Coordinator",
                   Email = "charlie.coord@example.com",
                   Password = "1234",
                   Role = "Coordinator",
                   HourlyRate = 0
               },
               new User
               {
                   UserID = 4,
                   Name = "Diana",
                   Surname = "Manager",
                   Email = "diana.manager@example.com",
                   Password = "1234",
                   Role = "Manager",
                   HourlyRate = 0
               }
           );
        }
    }
}
