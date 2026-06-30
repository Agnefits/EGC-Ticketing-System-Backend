using System.Collections.Generic;

namespace EGC_Ticketing_System.DTOs.Analysis
{
    public class UserRateSummaryDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public double AverageRate { get; set; }
    }

    public class UserTasksDoneSummaryDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int TasksDoneCount { get; set; }
    }

    public class AnalysisResponseDto
    {
        public int TotalTeams { get; set; }
        public int TotalTickets { get; set; }
        public int TicketsNotAssigned { get; set; }
        public int TicketsPending { get; set; }
        public int TicketsOnProgress { get; set; }
        public int TicketsCompleted { get; set; }
        public List<TeamTicketSummaryDto> TeamSummaries { get; set; } = new List<TeamTicketSummaryDto>();

        public UserRateSummaryDto? HighestRatedMember { get; set; }
        public UserRateSummaryDto? HighestRatedLeader { get; set; }
        public UserRateSummaryDto? HighestRatedManager { get; set; }
        public UserRateSummaryDto? LowestRatedMember { get; set; }
        public UserRateSummaryDto? LowestRatedLeader { get; set; }
        public UserRateSummaryDto? LowestRatedManager { get; set; }
        public UserTasksDoneSummaryDto? MostTasksDoneMember { get; set; }
    }
}
