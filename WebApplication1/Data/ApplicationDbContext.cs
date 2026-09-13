using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students => Set<Student>();
        public DbSet<Advisor> Advisors => Set<Advisor>();
        public DbSet<Parent> Parents => Set<Parent>();
        public DbSet<ParentStudent> ParentStudents => Set<ParentStudent>();
        public DbSet<Club> Clubs => Set<Club>();
        public DbSet<ClubMembership> ClubMemberships => Set<ClubMembership>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Student>()
                .HasIndex(s => s.StudentNumber)
                .IsUnique();

            builder.Entity<Student>()
                .HasIndex(s => s.UserId)
                .IsUnique();

            builder.Entity<Advisor>()
                .HasIndex(a => a.UserId)
                .IsUnique();

            builder.Entity<Parent>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            builder.Entity<ParentStudent>()
                .HasOne(ps => ps.Parent)
                .WithMany(p => p.Children)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ParentStudent>()
                .HasOne(ps => ps.Student)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ParentStudent>()
                .HasIndex(ps => new { ps.ParentId, ps.StudentId })
                .IsUnique();

            builder.Entity<Club>()
                .HasIndex(c => c.ClubName)
                .IsUnique();

            builder.Entity<Club>()
                .HasOne(c => c.Advisor)
                .WithMany(a => a.Clubs)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ClubMembership>()
                .HasOne(m => m.Student)
                .WithMany(s => s.Memberships)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ClubMembership>()
                .HasOne(m => m.Club)
                .WithMany(c => c.Memberships)
                .OnDelete(DeleteBehavior.Cascade);

            // Prevent duplicate pending/approved membership requests for the same student+club.
            builder.Entity<ClubMembership>()
                .HasIndex(m => new { m.StudentId, m.ClubId })
                .IsUnique()
                .HasFilter("[Status] <> 2");

            builder.Entity<Event>()
                .HasOne(e => e.Club)
                .WithMany(c => c.Events)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
