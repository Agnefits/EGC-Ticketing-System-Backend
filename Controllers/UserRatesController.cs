using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EGC_Ticketing_System.DTOs.UserRates;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.UnitOfWork;
using EGC_Ticketing_System.Enums;
using EGC_Ticketing_System.Services.Interfaces;

namespace EGC_Ticketing_System.Controllers
{
    [Authorize]
    public class UserRatesController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidationService _validationService;

        public UserRatesController(IUnitOfWork unitOfWork, IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _validationService = validationService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var fromUserId = GetCurrentUserId();

            await _validationService.ValidateUserRateCreateAsync(fromUserId, dto);

            var fromUser = await _unitOfWork.Users.GetByIdAsync(fromUserId);
            var toUser = await _unitOfWork.Users.GetByIdAsync(dto.ToUserId);

            var rateType = UserRateType.Standard;
            var isApproved = true;

            if (fromUser!.Role == UserRole.Admin)
            {
                rateType = UserRateType.Standard;
                isApproved = true;
            }
            else if (fromUser.Role == UserRole.Manager)
            {
                if (toUser!.Role == UserRole.Admin)
                {
                    rateType = UserRateType.Report;
                    isApproved = false;
                }
                else
                {
                    rateType = UserRateType.Standard;
                    isApproved = true;
                }
            }
            else // Member
            {
                var memberships = (await _unitOfWork.TeamMembers.FindAsync(tm => tm.MemberId == fromUserId)).ToList();
                bool isLeader = memberships.Any(tm => tm.IsTeamLeader);

                if (isLeader)
                {
                    if (toUser!.Role == UserRole.Admin || toUser.Role == UserRole.Manager)
                    {
                        rateType = UserRateType.Report;
                        isApproved = false;
                    }
                    else
                    {
                        rateType = UserRateType.Standard;
                        isApproved = true;
                    }
                }
                else
                {
                    rateType = UserRateType.Report;
                    isApproved = false;
                }
            }

            var userRate = new UserRate
            {
                FromUserId = fromUserId,
                ToUserId = dto.ToUserId,
                Type = rateType,
                Comment = dto.Comment,
                IsApproved = isApproved,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var item in dto.RateItems)
            {
                userRate.RateItems.Add(new RateItem
                {
                    Title = item.Title,
                    Value = item.Value,
                    MaxValue = item.MaxValue
                });
            }

            await _unitOfWork.UserRates.AddAsync(userRate);
            await _unitOfWork.CompleteAsync();

            await LogActivityAsync(_unitOfWork, "CreateUserRate", "UserRate", userRate.Id.ToString(), $"User Rate created by user ID {fromUserId} for user ID {dto.ToUserId} (Type: {rateType}, Approved: {isApproved})");

            var freshRate = await _unitOfWork.UserRates.GetWithDetailsAsync(userRate.Id);
            return CreatedAtAction(nameof(GetById), new { id = userRate.Id }, MapToUserRateResponseDto(freshRate!));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userRate = await _unitOfWork.UserRates.GetWithDetailsAsync(id);
            if (userRate == null)
            {
                return NotFound(new { message = "User rating not found." });
            }

            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            if (role != UserRole.Admin && userRate.FromUserId != userId && userRate.ToUserId != userId)
            {
                bool hasAccess = false;
                if (role == UserRole.Manager)
                {
                    var managedTeams = await _unitOfWork.Teams.FindAsync(t => t.CreatedById == userId && t.Status != TeamStatus.Deleted);
                    var managedTeamIds = managedTeams.Select(t => t.Id).ToList();
                    var targetMemberships = await _unitOfWork.TeamMembers.FindAsync(tm => tm.MemberId == userRate.ToUserId);
                    hasAccess = targetMemberships.Any(tm => managedTeamIds.Contains(tm.TeamId));
                }
                else
                {
                    var leaderMemberships = await _unitOfWork.TeamMembers.FindAsync(tm => tm.MemberId == userId && tm.IsTeamLeader);
                    var leadTeamIds = leaderMemberships.Select(tm => tm.TeamId).ToList();
                    var targetMemberships = await _unitOfWork.TeamMembers.FindAsync(tm => tm.MemberId == userRate.ToUserId && leadTeamIds.Contains(tm.TeamId));
                    hasAccess = targetMemberships.Any();
                }

                if (!hasAccess)
                {
                    return StatusCode(403, new { message = "Forbidden. You do not have permission to view this rating." });
                }
            }

            return Ok(MapToUserRateResponseDto(userRate));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            var allRates = await _unitOfWork.UserRates.GetAllWithDetailsAsync();
            var allTeamMembers = await _unitOfWork.TeamMembers.GetAllAsync();

            IEnumerable<UserRate> visibleRates;

            if (role == UserRole.Admin)
            {
                visibleRates = allRates;
            }
            else if (role == UserRole.Manager)
            {
                var managedTeams = await _unitOfWork.Teams.FindAsync(t => t.CreatedById == userId && t.Status != TeamStatus.Deleted);
                var managedTeamIds = managedTeams.Select(t => t.Id).ToList();

                visibleRates = allRates.Where(ur =>
                    ur.FromUserId == userId ||
                    ur.ToUserId == userId ||
                    allTeamMembers.Any(tm => tm.MemberId == ur.ToUserId && managedTeamIds.Contains(tm.TeamId))
                );
            }
            else
            {
                var memberships = allTeamMembers.Where(tm => tm.MemberId == userId).ToList();
                var leadTeamIds = memberships.Where(tm => tm.IsTeamLeader).Select(tm => tm.TeamId).ToList();

                if (leadTeamIds.Any())
                {
                    visibleRates = allRates.Where(ur =>
                        ur.FromUserId == userId ||
                        ur.ToUserId == userId ||
                        allTeamMembers.Any(tm => tm.MemberId == ur.ToUserId && leadTeamIds.Contains(tm.TeamId))
                    );
                }
                else
                {
                    visibleRates = allRates.Where(ur => ur.FromUserId == userId || ur.ToUserId == userId);
                }
            }

            var response = visibleRates.Select(MapToUserRateResponseDto).ToList();
            return Ok(response);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            var pendingRates = await _unitOfWork.UserRates.GetPendingRatesAsync();
            var allTeamMembers = await _unitOfWork.TeamMembers.GetAllAsync();

            IEnumerable<UserRate> visiblePending;

            if (role == UserRole.Admin)
            {
                visiblePending = pendingRates;
            }
            else if (role == UserRole.Manager)
            {
                var managedTeams = await _unitOfWork.Teams.FindAsync(t => t.CreatedById == userId && t.Status != TeamStatus.Deleted);
                var managedTeamIds = managedTeams.Select(t => t.Id).ToList();

                visiblePending = pendingRates.Where(ur =>
                    allTeamMembers.Any(tm => tm.MemberId == ur.FromUserId && managedTeamIds.Contains(tm.TeamId))
                );
            }
            else
            {
                var leaderMemberships = allTeamMembers.Where(tm => tm.MemberId == userId && tm.IsTeamLeader).ToList();
                var leadTeamIds = leaderMemberships.Select(tm => tm.TeamId).ToList();

                if (leadTeamIds.Any())
                {
                    visiblePending = pendingRates.Where(ur =>
                        allTeamMembers.Any(tm => tm.MemberId == ur.FromUserId && leadTeamIds.Contains(tm.TeamId))
                    );
                }
                else
                {
                    visiblePending = Array.Empty<UserRate>();
                }
            }

            var response = visiblePending.Select(MapToUserRateResponseDto).ToList();
            return Ok(response);
        }

        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveRateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userRate = await _unitOfWork.UserRates.GetWithDetailsAsync(id);
            if (userRate == null)
            {
                return NotFound(new { message = "User rating not found." });
            }

            if (userRate.IsApproved && userRate.ApprovedById.HasValue)
            {
                return BadRequest(new { message = "This rating has already been processed." });
            }

            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            bool canApprove = false;
            if (role == UserRole.Admin)
            {
                canApprove = true;
            }
            else
            {
                var leaderMemberships = await _unitOfWork.TeamMembers.FindAsync(tm => tm.MemberId == userId && tm.IsTeamLeader);
                var leadTeamIds = leaderMemberships.Select(tm => tm.TeamId).ToList();

                if (leadTeamIds.Any())
                {
                    var raterMemberships = await _unitOfWork.TeamMembers.FindAsync(tm => tm.MemberId == userRate.FromUserId && leadTeamIds.Contains(tm.TeamId));
                    canApprove = raterMemberships.Any();
                }

                if (userRate.FromUserId == userId || userRate.ToUserId == userId)
                {
                    canApprove = false;
                }
            }

            if (!canApprove)
            {
                return StatusCode(403, new { message = "Forbidden. You do not have permission to approve/reject this rating." });
            }

            if (dto.Approve)
            {
                userRate.IsApproved = true;
                userRate.ApprovedById = userId;
                userRate.ApprovedAt = DateTime.UtcNow;
                userRate.ApprovalComment = dto.ApprovalComment;
                _unitOfWork.UserRates.Update(userRate);
                await _unitOfWork.CompleteAsync();
                await LogActivityAsync(_unitOfWork, "ApproveUserRate", "UserRate", userRate.Id.ToString(), $"User Rate ID {userRate.Id} approved by user ID {userId}");
            }
            else
            {
                _unitOfWork.UserRates.Delete(userRate);
                await _unitOfWork.CompleteAsync();
                await LogActivityAsync(_unitOfWork, "RejectUserRate", "UserRate", userRate.Id.ToString(), $"User Rate ID {userRate.Id} rejected and removed by user ID {userId}");
            }

            return Ok(new { message = dto.Approve ? "Rating approved successfully." : "Rating rejected and deleted successfully." });
        }

        private UserRateResponseDto MapToUserRateResponseDto(UserRate rate)
        {
            double averageScore = 0;
            if (rate.RateItems.Any())
            {
                var sumPercentage = rate.RateItems.Sum(ri => (ri.Value / ri.MaxValue) * 10.0);
                averageScore = Math.Round(sumPercentage / rate.RateItems.Count, 2);
            }

            return new UserRateResponseDto
            {
                Id = rate.Id,
                FromUserId = rate.FromUserId,
                FromUserName = rate.FromUser?.FullName ?? "Unknown",
                ToUserId = rate.ToUserId,
                ToUserName = rate.ToUser?.FullName ?? "Unknown",
                Type = rate.Type.ToString(),
                Comment = rate.Comment,
                IsApproved = rate.IsApproved,
                ApprovedById = rate.ApprovedById,
                ApprovedByName = rate.ApprovedBy?.FullName,
                ApprovedAt = rate.ApprovedAt,
                ApprovalComment = rate.ApprovalComment,
                CreatedAt = rate.CreatedAt,
                RateItems = rate.RateItems.Select(ri => new RateItemResponseDto
                {
                    Id = ri.Id,
                    Title = ri.Title,
                    Value = ri.Value,
                    MaxValue = ri.MaxValue
                }).ToList(),
                AverageScore = averageScore
            };
        }
    }
}
