using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.DTOs.Tickets
{
    public class ChangeTicketTaskStatusDto
    {
        [Required]
        public TicketTaskStatus Status { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        [MaxLength(500)]
        public string? LinkUrl { get; set; }

        public IFormFile? File { get; set; }

        public bool? AddAnotherTask { get; set; }

        [MaxLength(150)]
        public string? NewTaskTitle { get; set; }

        [MaxLength(1000)]
        public string? NewTaskDescription { get; set; }

        public DateTime? NewTaskDeadline { get; set; }

        public int? NewTaskMemberId { get; set; }

        public TicketTaskPriority? NewTaskPriority { get; set; }
    }
}
