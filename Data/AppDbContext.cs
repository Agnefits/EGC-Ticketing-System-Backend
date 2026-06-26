using Microsoft.EntityFrameworkCore;
using EGC_Ticketing_System.Models;

namespace EGC_Ticketing_System.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Log> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Self-referential relationship (CreatedBy)
            modelBuilder.Entity<User>()
                .HasOne(u => u.CreatedBy)
                .WithMany(u => u.CreatedUsers)
                .HasForeignKey(u => u.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Team -> CreatedBy User
            modelBuilder.Entity<Team>()
                .HasOne(t => t.CreatedBy)
                .WithMany(u => u.CreatedTeams)
                .HasForeignKey(t => t.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // TeamMember composite key and relationships
            modelBuilder.Entity<TeamMember>()
                .HasKey(tm => new { tm.TeamId, tm.MemberId });

            modelBuilder.Entity<TeamMember>()
                .HasOne(tm => tm.Team)
                .WithMany(t => t.TeamMembers)
                .HasForeignKey(tm => tm.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TeamMember>()
                .HasOne(tm => tm.Member)
                .WithMany(u => u.TeamMemberships)
                .HasForeignKey(tm => tm.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ticket relationships
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Team)
                .WithMany(t => t.Tickets)
                .HasForeignKey(t => t.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Member)
                .WithMany(u => u.AssignedTickets)
                .HasForeignKey(t => t.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.CreatedBy)
                .WithMany(u => u.CreatedTickets)
                .HasForeignKey(t => t.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Log relationship
            modelBuilder.Entity<Log>()
                .HasOne(l => l.User)
                .WithMany(u => u.Logs)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // User entity properties validation
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.FullName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.Username)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(u => u.PhoneNumber)
                    .IsRequired()
                    .HasMaxLength(11);

                entity.Property(u => u.JobTitle)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.SignatureUrl)
                    .HasMaxLength(500);

                entity.Property(u => u.HashPassword)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.HasIndex(u => u.Email)
                    .IsUnique()
                    .HasFilter("[Status] <> 2");

                entity.HasIndex(u => u.Username)
                    .IsUnique()
                    .HasFilter("[Status] <> 2");

                entity.HasIndex(u => u.PhoneNumber)
                    .IsUnique()
                    .HasFilter("[Status] <> 2");
            });

            // Team entity constraints
            modelBuilder.Entity<Team>(entity =>
            {
                entity.Property(t => t.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(t => t.Description)
                    .IsRequired()
                    .HasMaxLength(1000);
            });

            // Ticket entity constraints
            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.Property(t => t.Title)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(t => t.Description)
                    .HasMaxLength(1000);

                entity.Property(t => t.Priority)
                    .IsRequired()
                    .HasDefaultValue(EGC_Ticketing_System.Enums.TicketPriority.Medium);
            });
        }
    }
}
