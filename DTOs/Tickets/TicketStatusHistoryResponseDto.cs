using System;

namespace EGC_Ticketing_System.DTOs.Tickets
{
    public class TicketStatusHistoryResponseDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public int ChangedById { get; set; }
        public string ChangedByName { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string? Comment { get; set; }
        public string? FileUrl { get; set; }
        public string? LinkUrl { get; set; }
    }
}
