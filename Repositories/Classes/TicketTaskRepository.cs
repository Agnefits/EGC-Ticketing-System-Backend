using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EGC_Ticketing_System.Data;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.Repositories.Classes;
using EGC_Ticketing_System.Repositories.Interfaces;

namespace EGC_Ticketing_System.Repositories.Classes
{
    public class TicketTaskRepository : GenericRepository<TicketTask>, ITicketTaskRepository
    {
        public TicketTaskRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<TicketTask?> GetWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(tt => tt.Ticket)
                .Include(tt => tt.Member)
                .Include(tt => tt.CreatedBy)
                .Include(tt => tt.StatusHistories)
                    .ThenInclude(sh => sh.ChangedBy)
                .FirstOrDefaultAsync(tt => tt.Id == id);
        }

        public async Task<IEnumerable<TicketTask>> GetTasksByTicketIdAsync(int ticketId)
        {
            return await _dbSet
                .Include(tt => tt.Member)
                .Include(tt => tt.CreatedBy)
                .Where(tt => tt.TicketId == ticketId)
                .ToListAsync();
        }
    }
}
