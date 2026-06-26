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
    }
}
