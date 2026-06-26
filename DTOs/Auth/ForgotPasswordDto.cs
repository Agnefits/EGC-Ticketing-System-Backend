using System.ComponentModel.DataAnnotations;

namespace EGC_Ticketing_System.DTOs.Auth
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
