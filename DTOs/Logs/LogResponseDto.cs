using System;

namespace EGC_Ticketing_System.DTOs.Logs
{
    public class LogResponseDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? UserFullName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
