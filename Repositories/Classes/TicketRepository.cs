using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EGC_Ticketing_System.Data;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.Repositories.Interfaces;

namespace EGC_Ticketing_System.Repositories.Classes
{
    public class TicketRepository : GenericRepository<Ticket>, ITicketRepository
    {
        public TicketRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Ticket?> GetWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(t => t.Team)
                .Include(t => t.Member)
                .Include(t => t.CreatedBy)
                .Include(t => t.StatusHistories)
                    .ThenInclude(sh => sh.ChangedBy)
                .Include(t => t.TicketTasks)
                    .ThenInclude(tt => tt.Member)
                .Include(t => t.TicketTasks)
                    .ThenInclude(tt => tt.CreatedBy)
                .Include(t => t.TicketTasks)
                    .ThenInclude(tt => tt.StatusHistories)
                        .ThenInclude(sh => sh.ChangedBy)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<Ticket>> GetTicketsByTeamAsync(int teamId)
        {
            return await _dbSet
                .Include(t => t.Member)
                .Include(t => t.CreatedBy)
                .Where(t => t.TeamId == teamId)
                .ToListAsync();
        }
    }
}
