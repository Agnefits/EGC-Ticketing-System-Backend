using System;
using System.Collections.Generic;

namespace EGC_Ticketing_System.DTOs.Tickets
{
    public class TicketTaskResponseDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string TicketTitle { get; set; } = string.Empty;
        public int? MemberId { get; set; }
        public string? MemberName { get; set; }
        public int CreatedById { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public List<TicketTaskStatusHistoryResponseDto> StatusHistories { get; set; } = new List<TicketTaskStatusHistoryResponseDto>();
    }
}
