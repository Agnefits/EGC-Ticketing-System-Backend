using System;

namespace EGC_Ticketing_System.DTOs.Logs
{
    public class LogDetailsResponseDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? UserFullName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string Details { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
