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
        public async Task<IActionResult> GetAnalysis([FromQuery] int? teamId, [FromQuery] int? managerId)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            var response = new AnalysisResponseDto();

            // Fetch all required data from UnitOfWork
            var allUsers = (await _unitOfWork.Users.FindAsync(u => u.Status != UserStatus.Deleted)).ToList();
            var allTeams = (await _unitOfWork.Teams.FindAsync(t => t.Status != TeamStatus.Deleted)).ToList();
            var allTeamMembers = (await _unitOfWork.TeamMembers.GetAllAsync()).ToList();
            var allTickets = (await _unitOfWork.Tickets.FindAsync(t => t.Status != TicketStatus.Deleted)).ToList();
            var allTasks = (await _unitOfWork.TicketTasks.FindAsync(tt => tt.Status != TicketTaskStatus.Deleted)).ToList();
            var allRates = (await _unitOfWork.UserRates.GetAllWithDetailsAsync()).Where(r => r.IsApproved).ToList();

            // Determine filter set of Team IDs
            List<int> targetTeamIds;
            if (teamId.HasValue)
            {
                targetTeamIds = new List<int> { teamId.Value };
            }
            else if (managerId.HasValue)
            {
                targetTeamIds = allTeams.Where(t => t.CreatedById == managerId.Value).Select(t => t.Id).ToList();
            }
            else
            {
                // No filter applied
                targetTeamIds = allTeams.Select(t => t.Id).ToList();
            }

            // Filter Teams, Tickets, Tasks based on active filters
            var filteredTeams = allTeams.Where(t => targetTeamIds.Contains(t.Id)).ToList();
            var filteredTickets = allTickets.Where(t => targetTeamIds.Contains(t.TeamId)).ToList();
            var filteredTasks = allTasks.Where(tt => filteredTickets.Any(ticket => ticket.Id == tt.TicketId)).ToList();

            // Determine visible users for team list
            List<User> filteredUsers;
            if (teamId.HasValue || managerId.HasValue)
            {
                var memberIdsInTargetTeams = allTeamMembers
                    .Where(tm => targetTeamIds.Contains(tm.TeamId))
                    .Select(tm => tm.MemberId)
                    .Distinct()
                    .ToList();

                var managersOfTargetTeams = filteredTeams.Select(t => t.CreatedById).Distinct().ToList();
                var combinedUserIds = memberIdsInTargetTeams.Concat(managersOfTargetTeams).Distinct().ToList();

                filteredUsers = allUsers.Where(u => combinedUserIds.Contains(u.Id)).ToList();
            }
            else
            {
                filteredUsers = allUsers;
            }

            // Base statistics
            response.TotalTeams = filteredTeams.Count;
            response.TotalTickets = filteredTickets.Count;
            response.TicketsNotAssigned = filteredTickets.Count(t => t.Status == TicketStatus.NotAssigned);
            response.TicketsPending = filteredTickets.Count(t => t.Status == TicketStatus.Pending);
            response.TicketsOnProgress = filteredTickets.Count(t => t.Status == TicketStatus.OnProgress);
            response.TicketsCompleted = filteredTickets.Count(t => t.Status == TicketStatus.Completed);

            foreach (var team in filteredTeams)
            {
                var teamTickets = filteredTickets.Where(t => t.TeamId == team.Id).ToList();
                response.TeamSummaries.Add(new TeamTicketSummaryDto
                {
                    TeamId = team.Id,
                    TeamName = team.Name,
                    TotalTickets = teamTickets.Count,
                    CompletedTickets = teamTickets.Count(t => t.Status == TicketStatus.Completed)
                });
            }

            // Define User Classifications
            var leadersUserIds = allTeamMembers
                .Where(tm => tm.IsTeamLeader && targetTeamIds.Contains(tm.TeamId))
                .Select(tm => tm.MemberId)
                .Distinct()
                .ToList();

            var managers = filteredUsers.Where(u => u.Role == UserRole.Manager).ToList();
            var leaders = filteredUsers.Where(u => u.Role == UserRole.Member && leadersUserIds.Contains(u.Id)).ToList();
            var members = filteredUsers.Where(u => u.Role == UserRole.Member && !leadersUserIds.Contains(u.Id)).ToList();

            // Calculate Rates
            var userAverages = new Dictionary<int, double>();
            foreach (var user in filteredUsers)
            {
                var ratesForUser = allRates.Where(r => r.ToUserId == user.Id).ToList();
                if (ratesForUser.Any())
                {
                    var averages = ratesForUser.Select(r =>
                    {
                        if (!r.RateItems.Any()) return 0.0;
                        return r.RateItems.Average(ri => (ri.Value / ri.MaxValue) * 10.0);
                    }).ToList();

                    userAverages[user.Id] = Math.Round(averages.Average(), 2);
                }
            }

            UserRateSummaryDto? GetHighest(List<User> list)
            {
                var rated = list
                    .Where(u => userAverages.ContainsKey(u.Id))
                    .Select(u => new UserRateSummaryDto
                    {
                        UserId = u.Id,
                        FullName = u.FullName,
                        AverageRate = userAverages[u.Id]
                    })
                    .OrderByDescending(s => s.AverageRate)
                    .FirstOrDefault();
                return rated;
            }

            UserRateSummaryDto? GetLowest(List<User> list)
            {
                var rated = list
                    .Where(u => userAverages.ContainsKey(u.Id))
                    .Select(u => new UserRateSummaryDto
                    {
                        UserId = u.Id,
                        FullName = u.FullName,
                        AverageRate = userAverages[u.Id]
                    })
                    .OrderBy(s => s.AverageRate)
                    .FirstOrDefault();
                return rated;
            }

            response.HighestRatedMember = GetHighest(members);
            response.HighestRatedLeader = GetHighest(leaders);
            response.HighestRatedManager = GetHighest(managers);

            response.LowestRatedMember = GetLowest(members);
            response.LowestRatedLeader = GetLowest(leaders);
            response.LowestRatedManager = GetLowest(managers);

            // Most tasks done member (Only consider Members role)
            var tasksDoneCounts = filteredTasks
                .Where(t => t.Status == TicketTaskStatus.Completed && t.MemberId.HasValue)
                .GroupBy(t => t.MemberId!.Value)
                .Select(g => new
                {
                    UserId = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var topMemberTask = tasksDoneCounts
                .Where(tc => members.Any(m => m.Id == tc.UserId))
                .OrderByDescending(tc => tc.Count)
                .FirstOrDefault();

            if (topMemberTask != null)
            {
                var memberUser = allUsers.FirstOrDefault(u => u.Id == topMemberTask.UserId);
                if (memberUser != null)
                {
                    response.MostTasksDoneMember = new UserTasksDoneSummaryDto
                    {
                        UserId = memberUser.Id,
                        FullName = memberUser.FullName,
                        TasksDoneCount = topMemberTask.Count
                    };
                }
            }

            return Ok(response);
        }
    }
}
