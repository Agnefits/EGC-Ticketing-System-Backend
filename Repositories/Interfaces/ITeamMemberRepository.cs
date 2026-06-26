using System.Collections.Generic;
using System.Threading.Tasks;
using EGC_Ticketing_System.Models;

namespace EGC_Ticketing_System.Repositories.Interfaces
{
    public interface ITeamMemberRepository : IGenericRepository<TeamMember>
    {
        Task<TeamMember?> GetByKeyAsync(int teamId, int memberId);
        Task<IEnumerable<TeamMember>> GetByTeamIdAsync(int teamId);
    }
}
