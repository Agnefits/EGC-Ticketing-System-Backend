using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        
        [JsonIgnore]
        public string HashPassword { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserRole Role { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? SignatureUrl { get; set; }
        public int? CreatedById { get; set; }

        // Navigation Properties
        [JsonIgnore]
        public virtual User? CreatedBy { get; set; }
        
        [JsonIgnore]
        public virtual ICollection<User> CreatedUsers { get; set; } = new List<User>();

        [JsonIgnore]
        public virtual ICollection<Team> CreatedTeams { get; set; } = new List<Team>();

        [JsonIgnore]
        public virtual ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();

        [JsonIgnore]
        public virtual ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();

        [JsonIgnore]
        public virtual ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();

        [JsonIgnore]
        public virtual ICollection<Log> Logs { get; set; } = new List<Log>();

        [JsonIgnore]
        public virtual ICollection<UserRate> RatesGiven { get; set; } = new List<UserRate>();

        [JsonIgnore]
        public virtual ICollection<UserRate> RatesReceived { get; set; } = new List<UserRate>();

        [JsonIgnore]
        public virtual ICollection<UserRate> RatesApproved { get; set; } = new List<UserRate>();

        [JsonIgnore]
        public virtual ICollection<TicketTask> AssignedTasks { get; set; } = new List<TicketTask>();

        [JsonIgnore]
        public virtual ICollection<TicketTask> CreatedTasks { get; set; } = new List<TicketTask>();
    }
}
