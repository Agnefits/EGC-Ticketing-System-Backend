using System.Collections.Generic;
using System.Threading.Tasks;
using EGC_Ticketing_System.DTOs.Users;
using EGC_Ticketing_System.DTOs.Profile;
using EGC_Ticketing_System.DTOs.Teams;
using EGC_Ticketing_System.DTOs.Tickets;
using EGC_Ticketing_System.UnitOfWork;
using EGC_Ticketing_System.Validation;
using EGC_Ticketing_System.Enums;
using EGC_Ticketing_System.Services.Interfaces;
using EGC_Ticketing_System.DTOs.UserRates;
using System.Linq;

namespace EGC_Ticketing_System.Services.Classes
{
    public class ValidationService : IValidationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ValidationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task ValidateUserCreateAsync(CreateUserDto dto)
        {
            var errors = new Dictionary<string, string[]>();

            if (await _unitOfWork.Users.ExistsByUsernameAsync(dto.Username))
            {
                errors.Add("Username", new[] { "Username already exists." });
            }

            if (!string.IsNullOrEmpty(dto.Email) && await _unitOfWork.Users.ExistsByEmailAsync(dto.Email))
            {
                errors.Add("Email", new[] { "Email already exists." });
            }

            if (!string.IsNullOrEmpty(dto.PhoneNumber) && await _unitOfWork.Users.ExistsByPhoneAsync(dto.PhoneNumber))
            {
                errors.Add("PhoneNumber", new[] { "Phone number already exists." });
            }

            if (errors.Count > 0)
            {
                throw new BusinessValidationException(errors);
            }
        }

        public async Task ValidateUserUpdateAsync(int id, UpdateUserDto dto)
        {
            var errors = new Dictionary<string, string[]>();

            if (!string.IsNullOrEmpty(dto.Email) && await _unitOfWork.Users.ExistsByEmailExceptAsync(id, dto.Email))
            {
                errors.Add("Email", new[] { "Email already exists." });
            }

            if (!string.IsNullOrEmpty(dto.PhoneNumber) && await _unitOfWork.Users.ExistsByPhoneExceptAsync(id, dto.PhoneNumber))
            {
                errors.Add("PhoneNumber", new[] { "Phone number already exists." });
            }

            if (errors.Count > 0)
            {
                throw new BusinessValidationException(errors);
            }
        }

        public async Task ValidateProfileUpdateAsync(int userId, UpdateProfileDto dto)
        {
            var errors = new Dictionary<string, string[]>();

            if (!string.IsNullOrEmpty(dto.Email) && await _unitOfWork.Users.ExistsByEmailExceptAsync(userId, dto.Email))
            {
                errors.Add("Email", new[] { "Email already exists." });
            }

            if (!string.IsNullOrEmpty(dto.PhoneNumber) && await _unitOfWork.Users.ExistsByPhoneExceptAsync(userId, dto.PhoneNumber))
            {
                errors.Add("PhoneNumber", new[] { "Phone number already exists." });
            }

            if (errors.Count > 0)
            {
                throw new BusinessValidationException(errors);
            }
        }

        public Task ValidateTeamCreateAsync(CreateTeamDto dto)
        {
            return Task.CompletedTask;
        }

        public Task ValidateTeamUpdateAsync(int id, UpdateTeamDto dto)
        {
            return Task.CompletedTask;
        }

        public async Task ValidateTicketCreateAsync(CreateTicketDto dto)
        {
            var errors = new Dictionary<string, string[]>();

            var team = await _unitOfWork.Teams.GetByIdAsync(dto.TeamId);
            if (team == null || team.Status == TeamStatus.Deleted)
            {
                errors.Add("TeamId", new[] { "The specified team does not exist." });
            }

            if (dto.MemberId.HasValue)
            {
                var member = await _unitOfWork.Users.GetByIdAsync(dto.MemberId.Value);
                if (member == null || member.Status == UserStatus.Deleted)
                {
                    errors.Add("MemberId", new[] { "The assigned member does not exist." });
                }
                else
                {
                    var isMemberInTeam = await _unitOfWork.TeamMembers.GetByKeyAsync(dto.TeamId, dto.MemberId.Value);
                    if (isMemberInTeam == null)
                    {
                        errors.Add("MemberId", new[] { "The assigned member must belong to the specified team." });
                    }
                }
            }

            if (errors.Count > 0)
            {
                throw new BusinessValidationException(errors);
            }
        }

        public async Task ValidateTicketUpdateAsync(int id, UpdateTicketDto dto)
        {
            var errors = new Dictionary<string, string[]>();

            var ticket = await _unitOfWork.Tickets.GetByIdAsync(id);
            if (ticket == null || ticket.Status == TicketStatus.Deleted)
            {
                errors.Add("TicketId", new[] { "The specified ticket does not exist." });
                throw new BusinessValidationException(errors);
            }

            if (dto.MemberId.HasValue)
            {
                var member = await _unitOfWork.Users.GetByIdAsync(dto.MemberId.Value);
                if (member == null || member.Status == UserStatus.Deleted)
                {
                    errors.Add("MemberId", new[] { "The assigned member does not exist." });
                }
                else
                {
                    var isMemberInTeam = await _unitOfWork.TeamMembers.GetByKeyAsync(ticket.TeamId, dto.MemberId.Value);
                    if (isMemberInTeam == null)
                    {
                        errors.Add("MemberId", new[] { "The assigned member must belong to the specified team." });
                    }
                }
            }

            if (errors.Count > 0)
            {
                throw new BusinessValidationException(errors);
            }
        }

        public async Task ValidateTicketTaskCreateAsync(int ticketId, CreateTicketTaskDto dto)
        {
            var errors = new Dictionary<string, string[]>();

            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (ticket == null || ticket.Status == TicketStatus.Deleted)
            {
                errors.Add("TicketId", new[] { "The specified ticket does not exist." });
                throw new BusinessValidationException(errors);
            }

            if (dto.MemberId.HasValue)
            {
                var member = await _unitOfWork.Users.GetByIdAsync(dto.MemberId.Value);
                if (member == null || member.Status == UserStatus.Deleted)
                {
                    errors.Add("MemberId", new[] { "The assigned member does not exist." });
                }
                else
                {
                    var isMemberInTeam = await _unitOfWork.TeamMembers.GetByKeyAsync(ticket.TeamId, dto.MemberId.Value);
                    if (isMemberInTeam == null)
                    {
                        errors.Add("MemberId", new[] { "The assigned member must belong to the specified team." });
                    }
                }
            }

            if (errors.Count > 0)
            {
                throw new BusinessValidationException(errors);
            }
        }

        public async Task ValidateTicketTaskUpdateAsync(int ticketId, int taskId, UpdateTicketTaskDto dto)
        {
            var errors = new Dictionary<string, string[]>();

            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (ticket == null || ticket.Status == TicketStatus.Deleted)
            {
                errors.Add("TicketId", new[] { "The specified ticket does not exist." });
                throw new BusinessValidationException(errors);
            }

            var task = await _unitOfWork.TicketTasks.GetByIdAsync(taskId);
            if (task == null || task.Status == TicketTaskStatus.Deleted || task.TicketId != ticketId)
            {
                errors.Add("TaskId", new[] { "The specified task does not exist or does not belong to this ticket." });
                throw new BusinessValidationException(errors);
            }

            if (dto.MemberId.HasValue)
            {
                var member = await _unitOfWork.Users.GetByIdAsync(dto.MemberId.Value);
                if (member == null || member.Status == UserStatus.Deleted)
                {
                    errors.Add("MemberId", new[] { "The assigned member does not exist." });
                }
                else
                {
                    var isMemberInTeam = await _unitOfWork.TeamMembers.GetByKeyAsync(ticket.TeamId, dto.MemberId.Value);
                    if (isMemberInTeam == null)
                    {
                        errors.Add("MemberId", new[] { "The assigned member must belong to the specified team." });
                    }
                }
            }

            if (errors.Count > 0)
            {
                throw new BusinessValidationException(errors);
            }
        }

        public async Task ValidateUserRateCreateAsync(int fromUserId, CreateUserRateDto dto)
        {
            var errors = new Dictionary<string, string[]>();

            if (fromUserId == dto.ToUserId)
            {
                errors.Add("ToUserId", new[] { "You cannot rate yourself." });
                throw new BusinessValidationException(errors);
            }

            var fromUser = await _unitOfWork.Users.GetByIdAsync(fromUserId);
            var toUser = await _unitOfWork.Users.GetByIdAsync(dto.ToUserId);

            if (fromUser == null || fromUser.Status == UserStatus.Deleted)
            {
                errors.Add("FromUserId", new[] { "Rater user does not exist." });
                throw new BusinessValidationException(errors);
            }

            if (toUser == null || toUser.Status == UserStatus.Deleted)
            {
                errors.Add("ToUserId", new[] { "The rated user does not exist." });
                throw new BusinessValidationException(errors);
            }

            if (fromUser.Role == UserRole.Admin)
            {
                // Admin can rate anyone
            }
            else if (fromUser.Role == UserRole.Manager)
            {
                if (toUser.Role == UserRole.Admin)
                {
                    // OK: Manager rating Admin is a Report (upward rate)
                }
                else
                {
                    var managerTeams = await _unitOfWork.Teams.FindAsync(t => t.CreatedById == fromUserId && t.Status != TeamStatus.Deleted);
                    var managerTeamIds = managerTeams.Select(t => t.Id).ToList();
                    var userMemberships = await _unitOfWork.TeamMembers.FindAsync(tm => tm.MemberId == dto.ToUserId);
                    var userInManagerTeam = userMemberships.Any(tm => managerTeamIds.Contains(tm.TeamId));

                    if (!userInManagerTeam)
                    {
                        errors.Add("ToUserId", new[] { "Manager can only rate members in teams they manage, or Admin." });
                    }
                }
            }
            else // fromUser.Role == UserRole.Member
            {
                var fromMemberships = (await _unitOfWork.TeamMembers.FindAsync(tm => tm.MemberId == fromUserId)).ToList();
                var leadTeamIds = fromMemberships.Where(tm => tm.IsTeamLeader).Select(tm => tm.TeamId).ToList();
                bool isRaterLeader = leadTeamIds.Any();

                if (isRaterLeader)
                {
                    if (toUser.Role == UserRole.Admin)
                    {
                        // OK: Leader rating Admin is a Report
                    }
                    else if (toUser.Role == UserRole.Manager)
                    {
                        var managerTeams = await _unitOfWork.Teams.FindAsync(t => leadTeamIds.Contains(t.Id) && t.CreatedById == dto.ToUserId);
                        if (!managerTeams.Any())
                        {
                            errors.Add("ToUserId", new[] { "Leader can only rate the manager of the teams they lead, or Admin." });
                        }
                    }
                    else
                    {
                        var toMemberships = await _unitOfWork.TeamMembers.FindAsync(tm => tm.MemberId == dto.ToUserId && leadTeamIds.Contains(tm.TeamId));
                        if (!toMemberships.Any())
                        {
                            errors.Add("ToUserId", new[] { "Leader can only rate members in teams they lead." });
                        }
                    }
                }
                else
                {
                    if (toUser.Role == UserRole.Admin)
                    {
                        // OK
                    }
                    else
                    {
                        var memberTeamIds = fromMemberships.Select(tm => tm.TeamId).ToList();

                        bool isToUserLeader = false;
                        bool isToUserManager = false;

                        var toLeaderMemberships = await _unitOfWork.TeamMembers.FindAsync(tm => tm.MemberId == dto.ToUserId && tm.IsTeamLeader && memberTeamIds.Contains(tm.TeamId));
                        isToUserLeader = toLeaderMemberships.Any();

                        var toManagerTeams = await _unitOfWork.Teams.FindAsync(t => memberTeamIds.Contains(t.Id) && t.CreatedById == dto.ToUserId);
                        isToUserManager = toManagerTeams.Any();

                        if (!isToUserLeader && !isToUserManager)
                        {
                            errors.Add("ToUserId", new[] { "Member can only rate their Team Leader, Manager, or Admin." });
                        }
                    }
                }
            }

            if (errors.Count > 0)
            {
                throw new BusinessValidationException(errors);
            }
        }
    }
}
