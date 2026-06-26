using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.Models
{
    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CreatedById { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TeamStatus Status { get; set; } = TeamStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [JsonIgnore]
        public virtual User? CreatedBy { get; set; }

        [JsonIgnore]
        public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();

        [JsonIgnore]
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
