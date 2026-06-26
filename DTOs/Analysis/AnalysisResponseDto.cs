using System.Collections.Generic;

namespace EGC_Ticketing_System.DTOs.Analysis
{
    public class AnalysisResponseDto
    {
        public int TotalTeams { get; set; }
        public int TotalTickets { get; set; }
        public int TicketsNotAssigned { get; set; }
        public int TicketsPending { get; set; }
        public int TicketsOnProgress { get; set; }
        public int TicketsCompleted { get; set; }
        public List<TeamTicketSummaryDto> TeamSummaries { get; set; } = new List<TeamTicketSummaryDto>();
    }
}
