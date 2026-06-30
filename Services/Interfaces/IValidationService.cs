using System.Threading.Tasks;
using EGC_Ticketing_System.DTOs.Users;
using EGC_Ticketing_System.DTOs.Profile;
using EGC_Ticketing_System.DTOs.Teams;
using EGC_Ticketing_System.DTOs.Tickets;
using EGC_Ticketing_System.DTOs.UserRates;

namespace EGC_Ticketing_System.Services.Interfaces
{
    public interface IValidationService
    {
        Task ValidateUserCreateAsync(CreateUserDto dto);
        Task ValidateUserUpdateAsync(int id, UpdateUserDto dto);
        Task ValidateProfileUpdateAsync(int userId, UpdateProfileDto dto);
        Task ValidateTeamCreateAsync(CreateTeamDto dto);
        Task ValidateTeamUpdateAsync(int id, UpdateTeamDto dto);
        Task ValidateTicketCreateAsync(CreateTicketDto dto);
        Task ValidateTicketUpdateAsync(int id, UpdateTicketDto dto);
        Task ValidateTicketTaskCreateAsync(int ticketId, CreateTicketTaskDto dto);
        Task ValidateTicketTaskUpdateAsync(int ticketId, int taskId, UpdateTicketTaskDto dto);
        Task ValidateUserRateCreateAsync(int fromUserId, CreateUserRateDto dto);
    }
}
