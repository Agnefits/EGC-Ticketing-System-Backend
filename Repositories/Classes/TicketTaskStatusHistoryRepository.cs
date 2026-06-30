using EGC_Ticketing_System.Data;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.Repositories.Interfaces;

namespace EGC_Ticketing_System.Repositories.Classes
{
    public class TicketTaskStatusHistoryRepository : GenericRepository<TicketTaskStatusHistory>, ITicketTaskStatusHistoryRepository
    {
        public TicketTaskStatusHistoryRepository(AppDbContext context) : base(context)
        {
        }
    }
}
