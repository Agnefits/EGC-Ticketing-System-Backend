using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EGC_Ticketing_System.DTOs.Profile
{
    public class UpdateProfileDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? JobTitle { get; set; }

        public IFormFile? Signature { get; set; }
    }
}
