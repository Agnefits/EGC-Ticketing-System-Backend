using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EGC_Ticketing_System.Data;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.Repositories.Interfaces;

namespace EGC_Ticketing_System.Repositories.Classes
{
    public class TeamMemberRepository : GenericRepository<TeamMember>, ITeamMemberRepository
    {
        public TeamMemberRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<TeamMember?> GetByKeyAsync(int teamId, int memberId)
        {
            return await _dbSet.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.MemberId == memberId);
        }

        public async Task<IEnumerable<TeamMember>> GetByTeamIdAsync(int teamId)
        {
            return await _dbSet
                .Include(tm => tm.Member)
                .Where(tm => tm.TeamId == teamId)
                .ToListAsync();
        }
    }
}
