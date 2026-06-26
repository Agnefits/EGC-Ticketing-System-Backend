using System;
using System.Text.Json.Serialization;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public int? MemberId { get; set; }
        public int CreatedById { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TicketStatus Status { get; set; } = TicketStatus.NotAssigned;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;

        // Navigation Properties
        [JsonIgnore]
        public virtual Team? Team { get; set; }

        [JsonIgnore]
        public virtual User? Member { get; set; }

        [JsonIgnore]
        public virtual User? CreatedBy { get; set; }
    }
}
