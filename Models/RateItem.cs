using System.Text.Json.Serialization;

namespace EGC_Ticketing_System.Models
{
    public class RateItem
    {
        public int Id { get; set; }
        public int UserRateId { get; set; }
        public string Title { get; set; } = string.Empty;
        public double Value { get; set; }
        public double MaxValue { get; set; }

        // Navigation Properties
        [JsonIgnore]
        public virtual UserRate? UserRate { get; set; }
    }
}
