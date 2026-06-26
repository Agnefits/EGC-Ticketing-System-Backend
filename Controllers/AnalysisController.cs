using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EGC_Ticketing_System.DTOs.Analysis;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.UnitOfWork;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.Controllers
{
    [Authorize]
    public class AnalysisController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;

        public AnalysisController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> GetAnalysis()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            var response = new AnalysisResponseDto();

            if (role == UserRole.Admin || role == UserRole.Manager)
            {
                // Admin/Manager get global statistics
                var teams = (await _unitOfWork.Teams.FindAsync(t => t.Status != TeamStatus.Deleted)).ToList();
                var tickets = (await _unitOfWork.Tickets.FindAsync(t => t.Status != TicketStatus.Deleted)).ToList();

                response.TotalTeams = teams.Count;
                response.TotalTickets = tickets.Count;
                response.TicketsNotAssigned = tickets.Count(t => t.Status == TicketStatus.NotAssigned);
                response.TicketsPending = tickets.Count(t => t.Status == TicketStatus.Pending);
                response.TicketsOnProgress = tickets.Count(t => t.Status == TicketStatus.OnProgress);
                response.TicketsCompleted = tickets.Count(t => t.Status == TicketStatus.Completed);

                foreach (var team in teams)
                {
                    var teamTickets = tickets.Where(t => t.TeamId == team.Id).ToList();
                    response.TeamSummaries.Add(new TeamTicketSummaryDto
                    {
                        TeamId = team.Id,
                        TeamName = team.Name,
                        TotalTickets = teamTickets.Count,
                        CompletedTickets = teamTickets.Count(t => t.Status == TicketStatus.Completed)
                    });
                }
            }
            else
            {
                // Member gets statistics for their own teams only
                var memberships = (await _unitOfWork.TeamMembers.FindAsync(tm => tm.MemberId == userId)).ToList();
                var memberTeamIds = memberships.Select(tm => tm.TeamId).ToList();

                var teams = (await _unitOfWork.Teams.FindAsync(t => t.Status != TeamStatus.Deleted && memberTeamIds.Contains(t.Id))).ToList();
                var tickets = (await _unitOfWork.Tickets.FindAsync(t => t.Status != TicketStatus.Deleted && memberTeamIds.Contains(t.TeamId))).ToList();

                response.TotalTeams = teams.Count;
                response.TotalTickets = tickets.Count;
                response.TicketsNotAssigned = tickets.Count(t => t.Status == TicketStatus.NotAssigned);
                response.TicketsPending = tickets.Count(t => t.Status == TicketStatus.Pending);
                response.TicketsOnProgress = tickets.Count(t => t.Status == TicketStatus.OnProgress);
                response.TicketsCompleted = tickets.Count(t => t.Status == TicketStatus.Completed);

                foreach (var team in teams)
                {
                    var teamTickets = tickets.Where(t => t.TeamId == team.Id).ToList();
                    response.TeamSummaries.Add(new TeamTicketSummaryDto
                    {
                        TeamId = team.Id,
                        TeamName = team.Name,
                        TotalTickets = teamTickets.Count,
                        CompletedTickets = teamTickets.Count(t => t.Status == TicketStatus.Completed)
                    });
                }
            }

            return Ok(response);
        }
    }
}
