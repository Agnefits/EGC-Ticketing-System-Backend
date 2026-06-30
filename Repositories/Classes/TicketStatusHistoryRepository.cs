using EGC_Ticketing_System.Data;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.Repositories.Interfaces;

namespace EGC_Ticketing_System.Repositories.Classes
{
    public class TicketStatusHistoryRepository : GenericRepository<TicketStatusHistory>, ITicketStatusHistoryRepository
    {
        public TicketStatusHistoryRepository(AppDbContext context) : base(context)
        {
        }
    }
}
