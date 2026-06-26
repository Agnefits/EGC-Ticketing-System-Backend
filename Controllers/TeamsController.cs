using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EGC_Ticketing_System.DTOs.Teams;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.Middleware;
using EGC_Ticketing_System.UnitOfWork;
using EGC_Ticketing_System.Enums;

namespace EGC_Ticketing_System.Controllers
{
    [Authorize]
    [AuthorizedRoles(UserRole.Admin, UserRole.Manager)]
    public class TeamsController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;

        public TeamsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var teams = await _unitOfWork.Teams.FindAsync(t => t.Status != TeamStatus.Deleted);
            var response = new List<TeamResponseDto>();

            foreach (var t in teams)
            {
                var teamWithMembers = await _unitOfWork.Teams.GetWithMembersAndTicketsAsync(t.Id);
                if (teamWithMembers != null)
                {
                    response.Add(MapToTeamResponseDto(teamWithMembers));
                }
            }

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var team = await _unitOfWork.Teams.GetWithMembersAndTicketsAsync(id);
            if (team == null || team.Status == TeamStatus.Deleted)
            {
                return NotFound(new { message = "Team not found." });
            }

            return Ok(MapToTeamResponseDto(team));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTeamDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var creatorId = GetCurrentUserId();

            var newTeam = new Team
            {
                Name = dto.Name,
                Description = dto.Description,
                Status = dto.Status,
                CreatedById = creatorId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Teams.AddAsync(newTeam);
            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "CreateTeam", "Team", newTeam.Id.ToString(), $"Team '{newTeam.Name}' created by user ID: {creatorId}");

            var freshTeam = await _unitOfWork.Teams.GetWithMembersAndTicketsAsync(newTeam.Id);
            return CreatedAtAction(nameof(GetById), new { id = newTeam.Id }, MapToTeamResponseDto(freshTeam!));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTeamDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var team = await _unitOfWork.Teams.GetByIdAsync(id);
            if (team == null || team.Status == TeamStatus.Deleted)
            {
                return NotFound(new { message = "Team not found." });
            }

            var userId = GetCurrentUserId();

            team.Name = dto.Name;
            team.Description = dto.Description;
            team.Status = dto.Status;

            _unitOfWork.Teams.Update(team);
            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "UpdateTeam", "Team", team.Id.ToString(), $"Team ID: {team.Id} updated by user ID: {userId}");

            return Ok(new { message = "Team updated successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var team = await _unitOfWork.Teams.GetByIdAsync(id);
            if (team == null || team.Status == TeamStatus.Deleted)
            {
                return NotFound(new { message = "Team not found." });
            }

            var userId = GetCurrentUserId();

            // Soft delete
            team.Status = TeamStatus.Deleted;
            _unitOfWork.Teams.Update(team);
            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "DeleteTeam", "Team", team.Id.ToString(), $"Team ID: {team.Id} soft-deleted by user ID: {userId}");

            return Ok(new { message = "Team deleted successfully." });
        }

        [HttpPost("{teamId}/members")]
        public async Task<IActionResult> AddMember(int teamId, [FromBody] AddRemoveMemberDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var team = await _unitOfWork.Teams.GetByIdAsync(teamId);
            if (team == null || team.Status == TeamStatus.Deleted)
            {
                return NotFound(new { message = "Team not found." });
            }

            var user = await _unitOfWork.Users.GetByIdAsync(dto.MemberId);
            if (user == null || user.Status == UserStatus.Deleted)
            {
                return NotFound(new { message = "Member user not found." });
            }

            var existingMember = await _unitOfWork.TeamMembers.GetByKeyAsync(teamId, dto.MemberId);
            if (existingMember != null)
            {
                return BadRequest(new { message = "User is already a member of this team." });
            }

            var teamMember = new TeamMember
            {
                TeamId = teamId,
                MemberId = dto.MemberId,
                IsTeamLeader = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.TeamMembers.AddAsync(teamMember);
            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "AddTeamMember", "TeamMember", $"{teamId}-{dto.MemberId}", $"User ID {dto.MemberId} added to Team ID {teamId}");

            return Ok(new { message = "Member added to team successfully." });
        }

        [HttpDelete("{teamId}/members/{memberId}")]
        public async Task<IActionResult> RemoveMember(int teamId, int memberId)
        {
            var team = await _unitOfWork.Teams.GetByIdAsync(teamId);
            if (team == null || team.Status == TeamStatus.Deleted)
            {
                return NotFound(new { message = "Team not found." });
            }

            var teamMember = await _unitOfWork.TeamMembers.GetByKeyAsync(teamId, memberId);
            if (teamMember == null)
            {
                return NotFound(new { message = "Member is not in this team." });
            }

            // If we are removing the team leader, warn/handle it, but proceed
            bool wasLeader = teamMember.IsTeamLeader;

            _unitOfWork.TeamMembers.Delete(teamMember);
            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "RemoveTeamMember", "TeamMember", $"{teamId}-{memberId}", $"User ID {memberId} removed from Team ID {teamId}. Was leader: {wasLeader}");

            return Ok(new { message = "Member removed from team successfully." });
        }

        [HttpPost("{teamId}/leader")]
        public async Task<IActionResult> SetTeamLeader(int teamId, [FromBody] SetTeamLeaderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var team = await _unitOfWork.Teams.GetByIdAsync(teamId);
            if (team == null || team.Status == TeamStatus.Deleted)
            {
                return NotFound(new { message = "Team not found." });
            }

            // Fetch all current members of this team
            var members = (await _unitOfWork.TeamMembers.GetByTeamIdAsync(teamId)).ToList();
            
            var newLeaderMember = members.FirstOrDefault(tm => tm.MemberId == dto.LeaderId);
            if (newLeaderMember == null)
            {
                return BadRequest(new { message = "The specified user is not a member of this team." });
            }

            // Set IsTeamLeader = true for the chosen one, and false for all others
            foreach (var member in members)
            {
                if (member.MemberId == dto.LeaderId)
                {
                    member.IsTeamLeader = true;
                    _unitOfWork.TeamMembers.Update(member);
                }
                else if (member.IsTeamLeader)
                {
                    member.IsTeamLeader = false;
                    _unitOfWork.TeamMembers.Update(member);
                }
            }

            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "SetTeamLeader", "Team", teamId.ToString(), $"User ID {dto.LeaderId} set as Team Leader for Team ID {teamId}");

            return Ok(new { message = "Team leader updated successfully." });
        }

        private TeamResponseDto MapToTeamResponseDto(Team team)
        {
            return new TeamResponseDto
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description,
                Status = team.Status.ToString(),
                CreatedAt = team.CreatedAt,
                CreatedById = team.CreatedById,
                Members = team.TeamMembers
                    .Where(tm => tm.Member != null && tm.Member.Status != UserStatus.Deleted)
                    .Select(tm => new TeamMemberDetailsDto
                    {
                        MemberId = tm.MemberId,
                        FullName = tm.Member?.FullName ?? string.Empty,
                        Username = tm.Member?.Username ?? string.Empty,
                        Email = tm.Member?.Email ?? string.Empty,
                        IsTeamLeader = tm.IsTeamLeader,
                        JoinedAt = tm.CreatedAt
                    }).ToList()
            };
        }
    }
}
