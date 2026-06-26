using System.ComponentModel.DataAnnotations;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.DTOs.Tickets
{
    public class ChangeTicketStatusDto
    {
        [Required]
        public TicketStatus Status { get; set; }
    }
}
