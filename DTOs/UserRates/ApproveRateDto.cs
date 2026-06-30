using System.ComponentModel.DataAnnotations;

namespace EGC_Ticketing_System.DTOs.UserRates
{
    public class ApproveRateDto
    {
        [Required]
        public bool Approve { get; set; }

        [MaxLength(1000)]
        public string? ApprovalComment { get; set; }
    }
}
