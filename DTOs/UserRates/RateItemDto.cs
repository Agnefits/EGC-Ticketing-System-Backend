using System.ComponentModel.DataAnnotations;

namespace EGC_Ticketing_System.DTOs.UserRates
{
    public class RateItemDto
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Range(0, 1000)]
        public double Value { get; set; }

        [Range(1, 1000)]
        public double MaxValue { get; set; }
    }
}
