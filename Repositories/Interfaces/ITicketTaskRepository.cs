using System.Collections.Generic;
using System.Threading.Tasks;
using EGC_Ticketing_System.Models;

namespace EGC_Ticketing_System.Repositories.Interfaces
{
    public interface ITicketTaskRepository : IGenericRepository<TicketTask>
    {
        Task<TicketTask?> GetWithDetailsAsync(int id);
        Task<IEnumerable<TicketTask>> GetTasksByTicketIdAsync(int ticketId);
    }
}
