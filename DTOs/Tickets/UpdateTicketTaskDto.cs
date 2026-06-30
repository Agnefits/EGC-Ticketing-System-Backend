using System;
using System.ComponentModel.DataAnnotations;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.DTOs.Tickets
{
    public class UpdateTicketTaskDto
    {
        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public DateTime? Deadline { get; set; }

        public int? MemberId { get; set; }

        [Required]
        public TicketTaskStatus Status { get; set; }

        [Required]
        public TicketTaskPriority Priority { get; set; }
    }
}
