using System.Collections.Generic;
using System.Threading.Tasks;
using EGC_Ticketing_System.Models;

namespace EGC_Ticketing_System.Repositories.Interfaces
{
    public interface ITicketRepository : IGenericRepository<Ticket>
    {
        Task<Ticket?> GetWithDetailsAsync(int id);
        Task<IEnumerable<Ticket>> GetTicketsByTeamAsync(int teamId);
    }
}
