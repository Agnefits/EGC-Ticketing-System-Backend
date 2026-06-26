using System;

namespace EGC_Ticketing_System.DTOs.Teams
{
    public class TeamMemberDetailsDto
    {
        public int MemberId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsTeamLeader { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
