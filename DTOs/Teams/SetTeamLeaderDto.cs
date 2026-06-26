using System.ComponentModel.DataAnnotations;

namespace EGC_Ticketing_System.DTOs.Teams
{
    public class SetTeamLeaderDto
    {
        [Required]
        public int LeaderId { get; set; }
    }
}
