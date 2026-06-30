using System;
using System.Text.Json.Serialization;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.Models
{
    public class TicketTaskStatusHistory
    {
        public int Id { get; set; }
        public int TicketTaskId { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TicketTaskStatus OldStatus { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TicketTaskStatus NewStatus { get; set; }
        
        public int ChangedById { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public string? Comment { get; set; }
        public string? FileUrl { get; set; }
        public string? LinkUrl { get; set; }

        // Navigation Properties
        [JsonIgnore]
        public virtual TicketTask? TicketTask { get; set; }

        [JsonIgnore]
        public virtual User? ChangedBy { get; set; }
    }
}
