using System.ComponentModel.DataAnnotations;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.DTOs.Teams
{
    public class UpdateTeamDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public TeamStatus Status { get; set; }
    }
}
