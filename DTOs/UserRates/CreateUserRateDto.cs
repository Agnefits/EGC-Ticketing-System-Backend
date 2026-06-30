using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EGC_Ticketing_System.DTOs.UserRates
{
    public class CreateUserRateDto
    {
        [Required]
        public int ToUserId { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one rate item is required.")]
        public List<RateItemDto> RateItems { get; set; } = new List<RateItemDto>();
    }
}
