using System;
using System.Collections.Generic;

namespace EGC_Ticketing_System.DTOs.Teams
{
    public class TeamResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int CreatedById { get; set; }
        public List<TeamMemberDetailsDto> Members { get; set; } = new List<TeamMemberDetailsDto>();
    }
}
