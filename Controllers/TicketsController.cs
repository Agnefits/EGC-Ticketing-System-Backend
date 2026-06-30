using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EGC_Ticketing_System.DTOs.Tickets;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.UnitOfWork;
using EGC_Ticketing_System.Enums;
using EGC_Ticketing_System.Services.Interfaces;

using System.IO;
using Microsoft.AspNetCore.Http;

namespace EGC_Ticketing_System.Controllers
{
    [Authorize]
    public class TicketsController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidationService _validationService;

        public TicketsController(IUnitOfWork unitOfWork, IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _validationService = validationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            IEnumerable<Ticket> tickets;

            if (role == UserRole.Admin || role == UserRole.Manager)
            {
                // Admins and Managers get all non-deleted tickets
                tickets = await _unitOfWork.Tickets.FindAsync(t => t.Status != TicketStatus.Deleted);
            }
            else
            {
                // Members get tickets for the teams they belong to
                var memberships = await _unitOfWork.TeamMembers.FindAsync(tm => tm.MemberId == userId);
                var userTeamIds = memberships.Select(tm => tm.TeamId).ToList();

                tickets = await _unitOfWork.Tickets.FindAsync(t => t.Status != TicketStatus.Deleted && userTeamIds.Contains(t.TeamId));
            }

            var response = new List<TicketResponseDto>();
            foreach (var t in tickets)
            {
                var ticketWithDetails = await _unitOfWork.Tickets.GetWithDetailsAsync(t.Id);
                if (ticketWithDetails != null)
                {
                    response.Add(MapToTicketResponseDto(ticketWithDetails));
                }
            }

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var ticket = await _unitOfWork.Tickets.GetWithDetailsAsync(id);
            if (ticket == null || ticket.Status == TicketStatus.Deleted)
            {
                return NotFound(new { message = "Ticket not found." });
            }

            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            // If user is a member, make sure they belong to the team of this ticket
            if (role == UserRole.Member)
            {
                var membership = await _unitOfWork.TeamMembers.GetByKeyAsync(ticket.TeamId, userId);
                if (membership == null)
                {
                    return StatusCode(403, new { message = "Forbidden. You do not belong to the team assigned to this ticket." });
                }
            }

            return Ok(MapToTicketResponseDto(ticket));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTicketDto dto)
        {
            var creatorId = GetCurrentUserId();

            // Verify authority to create ticket in the team
            if (!await CanManageTicketsInTeamAsync(dto.TeamId))
            {
                return StatusCode(403, new { message = "Forbidden. Only Admin, Manager, or Team Leader can create tickets in this team." });
            }

            await _validationService.ValidateTicketCreateAsync(dto);

            var newTicket = new Ticket
            {
                TeamId = dto.TeamId,
                MemberId = dto.MemberId,
                CreatedById = creatorId,
                Title = dto.Title,
                Description = dto.Description,
                Deadline = dto.Deadline,
                CreatedAt = DateTime.UtcNow,
                Status = dto.MemberId.HasValue ? TicketStatus.Pending : TicketStatus.NotAssigned,
                Priority = dto.Priority
            };

            await _unitOfWork.Tickets.AddAsync(newTicket);
            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "CreateTicket", "Ticket", newTicket.Id.ToString(), $"Ticket '{newTicket.Title}' created by user ID {creatorId}");

            var freshTicket = await _unitOfWork.Tickets.GetWithDetailsAsync(newTicket.Id);
            return CreatedAtAction(nameof(GetById), new { id = newTicket.Id }, MapToTicketResponseDto(freshTicket!));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTicketDto dto)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(id);
            if (ticket == null || ticket.Status == TicketStatus.Deleted)
            {
                return NotFound(new { message = "Ticket not found." });
            }

            // Verify authority
            if (!await CanManageTicketsInTeamAsync(ticket.TeamId))
            {
                return StatusCode(403, new { message = "Forbidden. Only Admin, Manager, or Team Leader can edit tickets in this team." });
            }

            await _validationService.ValidateTicketUpdateAsync(id, dto);

            var editorId = GetCurrentUserId();

            ticket.Title = dto.Title;
            ticket.Description = dto.Description;
            ticket.Deadline = dto.Deadline;
            ticket.MemberId = dto.MemberId;
            ticket.Status = dto.Status;
            ticket.Priority = dto.Priority;

            // If ticket was marked completed, log completion time
            if (dto.Status == TicketStatus.Completed && !ticket.CompletedAt.HasValue)
            {
                ticket.CompletedAt = DateTime.UtcNow;
            }
            else if (dto.Status != TicketStatus.Completed)
            {
                ticket.CompletedAt = null;
            }

            _unitOfWork.Tickets.Update(ticket);
            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "UpdateTicket", "Ticket", ticket.Id.ToString(), $"Ticket ID {ticket.Id} updated by user ID {editorId}");

            return Ok(new { message = "Ticket updated successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(id);
            if (ticket == null || ticket.Status == TicketStatus.Deleted)
            {
                return NotFound(new { message = "Ticket not found." });
            }

            // Verify authority
            if (!await CanManageTicketsInTeamAsync(ticket.TeamId))
            {
                return StatusCode(403, new { message = "Forbidden. Only Admin, Manager, or Team Leader can delete tickets in this team." });
            }

            var userId = GetCurrentUserId();

            // Soft delete
            ticket.Status = TicketStatus.Deleted;
            _unitOfWork.Tickets.Update(ticket);
            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "DeleteTicket", "Ticket", ticket.Id.ToString(), $"Ticket ID {ticket.Id} soft-deleted by user ID {userId}");

            return Ok(new { message = "Ticket deleted successfully." });
        }

        [HttpPut("{id}/status")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ChangeStatus(int id, [FromForm] ChangeTicketStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ticket = await _unitOfWork.Tickets.GetByIdAsync(id);
            if (ticket == null || ticket.Status == TicketStatus.Deleted)
            {
                return NotFound(new { message = "Ticket not found." });
            }

            // Verify authority
            if (!await CanChangeStatusAsync(ticket))
            {
                return StatusCode(403, new { message = "Forbidden. You do not have permission to change the status of this ticket." });
            }

            var userId = GetCurrentUserId();
            var oldStatus = ticket.Status;

            // Handle file upload size check (5MB)
            string? fileUrl = null;
            if (dto.File != null)
            {
                if (dto.File.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { message = "Attachment file must not exceed 5MB." });
                }
                fileUrl = await SaveAttachmentFileAsync(dto.File);
            }

            ticket.Status = dto.Status;

            // If ticket is marked completed, log completion time
            if (dto.Status == TicketStatus.Completed)
            {
                ticket.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                ticket.CompletedAt = null;
            }

            _unitOfWork.Tickets.Update(ticket);

            // Log status history
            var history = new TicketStatusHistory
            {
                TicketId = ticket.Id,
                OldStatus = oldStatus,
                NewStatus = dto.Status,
                ChangedById = userId,
                ChangedAt = DateTime.UtcNow,
                Comment = dto.Comment,
                FileUrl = fileUrl,
                LinkUrl = dto.LinkUrl
            };
            await _unitOfWork.TicketStatusHistories.AddAsync(history);

            // Handle AddAnotherTask option
            if (dto.AddAnotherTask == true)
            {
                var newTask = new TicketTask
                {
                    TicketId = ticket.Id,
                    Title = !string.IsNullOrWhiteSpace(dto.NewTaskTitle) ? dto.NewTaskTitle : $"Follow-up: {ticket.Title}",
                    Description = dto.NewTaskDescription ?? "Follow-up task created during status change.",
                    Deadline = dto.NewTaskDeadline,
                    MemberId = dto.NewTaskMemberId ?? ticket.MemberId,
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                    Status = dto.NewTaskMemberId.HasValue ? TicketTaskStatus.Pending : TicketTaskStatus.NotAssigned,
                    Priority = dto.NewTaskPriority ?? TicketTaskPriority.Medium
                };
                await _unitOfWork.TicketTasks.AddAsync(newTask);
            }

            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "ChangeTicketStatus", "Ticket", ticket.Id.ToString(), $"Ticket ID {ticket.Id} status changed from {oldStatus} to {dto.Status} by user ID {userId}");

            return Ok(new { message = "Ticket status updated successfully." });
        }

        [HttpGet("{ticketId}/tasks")]
        public async Task<IActionResult> GetTasks(int ticketId)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (ticket == null || ticket.Status == TicketStatus.Deleted)
            {
                return NotFound(new { message = "Ticket not found." });
            }

            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            // Verify team membership for members
            if (role == UserRole.Member)
            {
                var membership = await _unitOfWork.TeamMembers.GetByKeyAsync(ticket.TeamId, userId);
                if (membership == null)
                {
                    return StatusCode(403, new { message = "Forbidden. You do not belong to the team assigned to this ticket." });
                }
            }

            var tasks = await _unitOfWork.TicketTasks.GetTasksByTicketIdAsync(ticketId);
            var response = tasks.Select(MapToTicketTaskResponseDto).ToList();
            return Ok(response);
        }

        [HttpGet("{ticketId}/tasks/{taskId}")]
        public async Task<IActionResult> GetTaskById(int ticketId, int taskId)
        {
            var task = await _unitOfWork.TicketTasks.GetWithDetailsAsync(taskId);
            if (task == null || task.Status == TicketTaskStatus.Deleted || task.TicketId != ticketId)
            {
                return NotFound(new { message = "Ticket task not found." });
            }

            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            // Verify team membership for members
            if (role == UserRole.Member)
            {
                var membership = await _unitOfWork.TeamMembers.GetByKeyAsync(task.Ticket?.TeamId ?? 0, userId);
                if (membership == null)
                {
                    return StatusCode(403, new { message = "Forbidden. You do not belong to the team assigned to this ticket." });
                }
            }

            return Ok(MapToTicketTaskResponseDto(task));
        }

        [HttpPost("{ticketId}/tasks")]
        public async Task<IActionResult> CreateTask(int ticketId, [FromBody] CreateTicketTaskDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (ticket == null || ticket.Status == TicketStatus.Deleted)
            {
                return NotFound(new { message = "Ticket not found." });
            }

            if (!await CanManageTicketsInTeamAsync(ticket.TeamId))
            {
                return StatusCode(403, new { message = "Forbidden. Only Admin, Manager, or Team Leader can create tasks." });
            }

            await _validationService.ValidateTicketTaskCreateAsync(ticketId, dto);

            var creatorId = GetCurrentUserId();

            var newTask = new TicketTask
            {
                TicketId = ticketId,
                MemberId = dto.MemberId,
                CreatedById = creatorId,
                Title = dto.Title,
                Description = dto.Description,
                Deadline = dto.Deadline,
                CreatedAt = DateTime.UtcNow,
                Status = dto.MemberId.HasValue ? TicketTaskStatus.Pending : TicketTaskStatus.NotAssigned,
                Priority = dto.Priority
            };

            await _unitOfWork.TicketTasks.AddAsync(newTask);
            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "CreateTicketTask", "TicketTask", newTask.Id.ToString(), $"Task '{newTask.Title}' created by user ID {creatorId} on Ticket ID {ticketId}");

            var freshTask = await _unitOfWork.TicketTasks.GetWithDetailsAsync(newTask.Id);
            return CreatedAtAction(nameof(GetTaskById), new { ticketId = ticketId, taskId = newTask.Id }, MapToTicketTaskResponseDto(freshTask!));
        }

        [HttpPut("{ticketId}/tasks/{taskId}")]
        public async Task<IActionResult> UpdateTask(int ticketId, int taskId, [FromBody] UpdateTicketTaskDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (ticket == null || ticket.Status == TicketStatus.Deleted)
            {
                return NotFound(new { message = "Ticket not found." });
            }

            var task = await _unitOfWork.TicketTasks.GetByIdAsync(taskId);
            if (task == null || task.Status == TicketTaskStatus.Deleted || task.TicketId != ticketId)
            {
                return NotFound(new { message = "Ticket task not found." });
            }

            if (!await CanManageTicketsInTeamAsync(ticket.TeamId))
            {
                return StatusCode(403, new { message = "Forbidden. Only Admin, Manager, or Team Leader can update tasks." });
            }

            await _validationService.ValidateTicketTaskUpdateAsync(ticketId, taskId, dto);

            var editorId = GetCurrentUserId();

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.Deadline = dto.Deadline;
            task.MemberId = dto.MemberId;
            task.Status = dto.Status;
            task.Priority = dto.Priority;

            if (dto.Status == TicketTaskStatus.Completed && !task.CompletedAt.HasValue)
            {
                task.CompletedAt = DateTime.UtcNow;
            }
            else if (dto.Status != TicketTaskStatus.Completed)
            {
                task.CompletedAt = null;
            }

            _unitOfWork.TicketTasks.Update(task);
            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "UpdateTicketTask", "TicketTask", task.Id.ToString(), $"Task ID {task.Id} updated by user ID {editorId}");

            return Ok(new { message = "Task updated successfully." });
        }

        [HttpPut("{ticketId}/tasks/{taskId}/status")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ChangeTaskStatus(int ticketId, int taskId, [FromForm] ChangeTicketTaskStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (ticket == null || ticket.Status == TicketStatus.Deleted)
            {
                return NotFound(new { message = "Ticket not found." });
            }

            var task = await _unitOfWork.TicketTasks.GetByIdAsync(taskId);
            if (task == null || task.Status == TicketTaskStatus.Deleted || task.TicketId != ticketId)
            {
                return NotFound(new { message = "Ticket task not found." });
            }

            // Verify authority
            if (!await CanChangeTaskStatusAsync(task, ticket.TeamId))
            {
                return StatusCode(403, new { message = "Forbidden. You do not have permission to change the status of this task." });
            }

            var userId = GetCurrentUserId();
            var oldStatus = task.Status;

            // Handle file upload size check (5MB)
            string? fileUrl = null;
            if (dto.File != null)
            {
                if (dto.File.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { message = "Attachment file must not exceed 5MB." });
                }
                fileUrl = await SaveAttachmentFileAsync(dto.File);
            }

            task.Status = dto.Status;

            if (dto.Status == TicketTaskStatus.Completed)
            {
                task.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                task.CompletedAt = null;
            }

            _unitOfWork.TicketTasks.Update(task);

            // Log status history
            var history = new TicketTaskStatusHistory
            {
                TicketTaskId = task.Id,
                OldStatus = oldStatus,
                NewStatus = dto.Status,
                ChangedById = userId,
                ChangedAt = DateTime.UtcNow,
                Comment = dto.Comment,
                FileUrl = fileUrl,
                LinkUrl = dto.LinkUrl
            };
            await _unitOfWork.TicketTaskStatusHistories.AddAsync(history);

            // Handle AddAnotherTask option
            if (dto.AddAnotherTask == true)
            {
                var newTask = new TicketTask
                {
                    TicketId = ticket.Id,
                    Title = !string.IsNullOrWhiteSpace(dto.NewTaskTitle) ? dto.NewTaskTitle : $"Follow-up: {task.Title}",
                    Description = dto.NewTaskDescription ?? $"Follow-up task created during status change of task: {task.Title}.",
                    Deadline = dto.NewTaskDeadline,
                    MemberId = dto.NewTaskMemberId ?? task.MemberId,
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                    Status = dto.NewTaskMemberId.HasValue ? TicketTaskStatus.Pending : TicketTaskStatus.NotAssigned,
                    Priority = dto.NewTaskPriority ?? TicketTaskPriority.Medium
                };
                await _unitOfWork.TicketTasks.AddAsync(newTask);
            }

            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "ChangeTicketTaskStatus", "TicketTask", task.Id.ToString(), $"Task ID {task.Id} status changed from {oldStatus} to {dto.Status} by user ID {userId}");

            return Ok(new { message = "Task status updated successfully." });
        }

        private async Task<bool> CanChangeTaskStatusAsync(TicketTask task, int teamId)
        {
            var role = GetCurrentUserRole();
            if (role == UserRole.Admin || role == UserRole.Manager)
            {
                return true;
            }

            var userId = GetCurrentUserId();
            if (task.MemberId == userId)
            {
                return true;
            }

            var membership = await _unitOfWork.TeamMembers.GetByKeyAsync(teamId, userId);
            return membership != null && membership.IsTeamLeader;
        }

        private async Task<string?> SaveAttachmentFileAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "attachments");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/attachments/{uniqueFileName}";
        }

        private async Task<bool> CanManageTicketsInTeamAsync(int teamId)
        {
            var role = GetCurrentUserRole();
            if (role == UserRole.Admin || role == UserRole.Manager)
            {
                return true;
            }

            var userId = GetCurrentUserId();
            var membership = await _unitOfWork.TeamMembers.GetByKeyAsync(teamId, userId);
            return membership != null && membership.IsTeamLeader;
        }

        private async Task<bool> CanChangeStatusAsync(Ticket ticket)
        {
            var role = GetCurrentUserRole();
            if (role == UserRole.Admin || role == UserRole.Manager)
            {
                return true;
            }

            var userId = GetCurrentUserId();
            if (ticket.MemberId == userId)
            {
                return true;
            }

            var membership = await _unitOfWork.TeamMembers.GetByKeyAsync(ticket.TeamId, userId);
            return membership != null && membership.IsTeamLeader;
        }

        private TicketResponseDto MapToTicketResponseDto(Ticket ticket)
        {
            return new TicketResponseDto
            {
                Id = ticket.Id,
                TeamId = ticket.TeamId,
                TeamName = ticket.Team?.Name ?? string.Empty,
                MemberId = ticket.MemberId,
                MemberName = ticket.Member?.FullName,
                CreatedById = ticket.CreatedById,
                CreatedByName = ticket.CreatedBy?.FullName ?? string.Empty,
                Title = ticket.Title,
                Description = ticket.Description,
                Deadline = ticket.Deadline,
                CreatedAt = ticket.CreatedAt,
                CompletedAt = ticket.CompletedAt,
                Status = ticket.Status.ToString(),
                Priority = ticket.Priority.ToString(),
                TicketTasks = ticket.TicketTasks?.Select(MapToTicketTaskResponseDto).ToList() ?? new List<TicketTaskResponseDto>(),
                StatusHistories = ticket.StatusHistories?.Select(MapToTicketStatusHistoryResponseDto).ToList() ?? new List<TicketStatusHistoryResponseDto>()
            };
        }

        private TicketTaskResponseDto MapToTicketTaskResponseDto(TicketTask task)
        {
            return new TicketTaskResponseDto
            {
                Id = task.Id,
                TicketId = task.TicketId,
                TicketTitle = task.Ticket?.Title ?? string.Empty,
                MemberId = task.MemberId,
                MemberName = task.Member?.FullName,
                CreatedById = task.CreatedById,
                CreatedByName = task.CreatedBy?.FullName ?? string.Empty,
                Title = task.Title,
                Description = task.Description,
                Deadline = task.Deadline,
                CreatedAt = task.CreatedAt,
                CompletedAt = task.CompletedAt,
                Status = task.Status.ToString(),
                Priority = task.Priority.ToString(),
                StatusHistories = task.StatusHistories?.Select(MapToTicketTaskStatusHistoryResponseDto).ToList() ?? new List<TicketTaskStatusHistoryResponseDto>()
            };
        }

        private TicketStatusHistoryResponseDto MapToTicketStatusHistoryResponseDto(TicketStatusHistory history)
        {
            return new TicketStatusHistoryResponseDto
            {
                Id = history.Id,
                TicketId = history.TicketId,
                OldStatus = history.OldStatus.ToString(),
                NewStatus = history.NewStatus.ToString(),
                ChangedById = history.ChangedById,
                ChangedByName = history.ChangedBy?.FullName ?? "Unknown",
                ChangedAt = history.ChangedAt,
                Comment = history.Comment,
                FileUrl = history.FileUrl,
                LinkUrl = history.LinkUrl
            };
        }

        private TicketTaskStatusHistoryResponseDto MapToTicketTaskStatusHistoryResponseDto(TicketTaskStatusHistory history)
        {
            return new TicketTaskStatusHistoryResponseDto
            {
                Id = history.Id,
                TicketTaskId = history.TicketTaskId,
                OldStatus = history.OldStatus.ToString(),
                NewStatus = history.NewStatus.ToString(),
                ChangedById = history.ChangedById,
                ChangedByName = history.ChangedBy?.FullName ?? "Unknown",
                ChangedAt = history.ChangedAt,
                Comment = history.Comment,
                FileUrl = history.FileUrl,
                LinkUrl = history.LinkUrl
            };
        }
    }
}
