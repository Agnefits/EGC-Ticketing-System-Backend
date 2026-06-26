using System.Threading.Tasks;
using EGC_Ticketing_System.Models;

namespace EGC_Ticketing_System.Repositories.Interfaces
{
    public interface ITeamRepository : IGenericRepository<Team>
    {
        Task<Team?> GetWithMembersAndTicketsAsync(int id);
    }
}
