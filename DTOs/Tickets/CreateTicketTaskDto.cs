using System;
using System.ComponentModel.DataAnnotations;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.DTOs.Tickets
{
    public class CreateTicketTaskDto
    {
        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public DateTime? Deadline { get; set; }

        public int? MemberId { get; set; }

        public TicketTaskPriority Priority { get; set; } = TicketTaskPriority.Medium;
    }
}
