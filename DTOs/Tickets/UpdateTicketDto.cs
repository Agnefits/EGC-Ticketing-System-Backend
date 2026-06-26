using System;
using System.ComponentModel.DataAnnotations;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.DTOs.Tickets
{
    public class UpdateTicketDto
    {
        public int? MemberId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime? Deadline { get; set; }

        [Required]
        public TicketStatus Status { get; set; }

        [Required]
        public TicketPriority Priority { get; set; }
    }
}
