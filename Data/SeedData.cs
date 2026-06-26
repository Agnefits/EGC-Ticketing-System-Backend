using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var context = new AppDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());

            // Auto-migrate database on startup
            context.Database.Migrate();

            // Look for any users.
            if (context.Users.Any(u=>u.Role == UserRole.Admin && u.Status == UserStatus.Active))
            {
                return;   // DB has been seeded
            }

            // Seed Admin User
            var adminUser = new User
            {
                FullName = "System Admin",
                Username = "admin",
                Email = "admin@aitu.edu",
                PhoneNumber = "+1234567890",
                HashPassword = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                JobTitle = "Administrator",
                Role = UserRole.Admin,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                SignatureUrl = null,
                CreatedById = null
            };

            context.Users.Add(adminUser);
            context.SaveChanges();
        }
    }
}
