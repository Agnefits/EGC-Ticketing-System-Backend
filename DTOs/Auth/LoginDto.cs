using System.ComponentModel.DataAnnotations;

namespace EGC_Ticketing_System.DTOs.Auth
{
    public class LoginDto
    {
        [Required]
        public string Identifier { get; set; } = string.Empty; // Email, Username, or Phone Number

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
