using System.ComponentModel.DataAnnotations;
using EGC_Ticketing_System.Enums;
using Microsoft.AspNetCore.Http;

namespace EGC_Ticketing_System.DTOs.Users
{
    public class CreateUserDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Username { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public string? JobTitle { get; set; }

        [Required]
        public UserRole Role { get; set; }

        public IFormFile? Signature { get; set; }
    }
}
