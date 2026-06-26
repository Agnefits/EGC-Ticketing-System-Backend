using System.ComponentModel.DataAnnotations;

namespace EGC_Ticketing_System.DTOs.Teams
{
    public class AddRemoveMemberDto
    {
        [Required]
        public int MemberId { get; set; }
    }
}
