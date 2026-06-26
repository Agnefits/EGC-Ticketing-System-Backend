using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EGC_Ticketing_System.Data;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.Repositories.Interfaces;

namespace EGC_Ticketing_System.Repositories.Classes
{
    public class TeamRepository : GenericRepository<Team>, ITeamRepository
    {
        public TeamRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Team?> GetWithMembersAndTicketsAsync(int id)
        {
            return await _dbSet
                .Include(t => t.TeamMembers)
                    .ThenInclude(tm => tm.Member)
                .Include(t => t.Tickets)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
    }
}
