using System;

namespace EGC_Ticketing_System.DTOs.Logs
{
    public class LogFilterDto
    {
        public int? UserId { get; set; }
        public string? Action { get; set; }
        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Skip { get; set; } = 0;
        public int Limit { get; set; } = 50;
    }
}
