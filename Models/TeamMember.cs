using System;
using System.Text.Json.Serialization;

namespace EGC_Ticketing_System.Models
{
    public class TeamMember
    {
        public int TeamId { get; set; }
        public int MemberId { get; set; }
        public bool IsTeamLeader { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [JsonIgnore]
        public virtual Team? Team { get; set; }

        [JsonIgnore]
        public virtual User? Member { get; set; }
    }
}
