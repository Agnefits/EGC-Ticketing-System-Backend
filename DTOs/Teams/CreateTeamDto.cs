using System.ComponentModel.DataAnnotations;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.DTOs.Teams
{
    public class CreateTeamDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public TeamStatus Status { get; set; } = TeamStatus.Active;
    }
}
