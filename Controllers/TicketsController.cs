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
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeTicketStatusDto dto)
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
            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "ChangeTicketStatus", "Ticket", ticket.Id.ToString(), $"Ticket ID {ticket.Id} status changed from {oldStatus} to {dto.Status} by user ID {userId}");

            return Ok(new { message = "Ticket status updated successfully." });
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
                Priority = ticket.Priority.ToString()
            };
        }
    }
}
