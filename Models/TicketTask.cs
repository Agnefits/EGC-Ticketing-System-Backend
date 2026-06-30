using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.Models
{
    public class TicketTask
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int? MemberId { get; set; }
        public int CreatedById { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TicketTaskStatus Status { get; set; } = TicketTaskStatus.NotAssigned;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TicketTaskPriority Priority { get; set; } = TicketTaskPriority.Medium;

        // Navigation Properties
        [JsonIgnore]
        public virtual Ticket? Ticket { get; set; }

        [JsonIgnore]
        public virtual User? Member { get; set; }

        [JsonIgnore]
        public virtual User? CreatedBy { get; set; }

        [JsonIgnore]
        public virtual ICollection<TicketTaskStatusHistory> StatusHistories { get; set; } = new List<TicketTaskStatusHistory>();
    }
}
