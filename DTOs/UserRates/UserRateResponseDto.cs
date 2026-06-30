using System;
using System.Collections.Generic;

namespace EGC_Ticketing_System.DTOs.UserRates
{
    public class RateItemResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public double Value { get; set; }
        public double MaxValue { get; set; }
    }

    public class UserRateResponseDto
    {
        public int Id { get; set; }
        public int FromUserId { get; set; }
        public string FromUserName { get; set; } = string.Empty;
        public int ToUserId { get; set; }
        public string ToUserName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public bool IsApproved { get; set; }
        public int? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalComment { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<RateItemResponseDto> RateItems { get; set; } = new List<RateItemResponseDto>();
        public double AverageScore { get; set; }
    }
}
