using System.Collections.Generic;

namespace EGC_Ticketing_System.DTOs.Analysis
{
    public class TeamTicketSummaryDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int TotalTickets { get; set; }
        public int CompletedTickets { get; set; }
    }
}
