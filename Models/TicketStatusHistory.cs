using System;
using System.Text.Json.Serialization;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.Models
{
    public class TicketStatusHistory
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TicketStatus OldStatus { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TicketStatus NewStatus { get; set; }
        
        public int ChangedById { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public string? Comment { get; set; }
        public string? FileUrl { get; set; }
        public string? LinkUrl { get; set; }

        // Navigation Properties
        [JsonIgnore]
        public virtual Ticket? Ticket { get; set; }

        [JsonIgnore]
        public virtual User? ChangedBy { get; set; }
    }
}
