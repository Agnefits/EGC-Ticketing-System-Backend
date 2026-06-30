using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.Models
{
    public class UserRate
    {
        public int Id { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserRateType Type { get; set; }
        public string? Comment { get; set; }
        public bool IsApproved { get; set; } = true;
        public int? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalComment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [JsonIgnore]
        public virtual User? FromUser { get; set; }

        [JsonIgnore]
        public virtual User? ToUser { get; set; }

        [JsonIgnore]
        public virtual User? ApprovedBy { get; set; }

        public virtual ICollection<RateItem> RateItems { get; set; } = new List<RateItem>();
    }
}
