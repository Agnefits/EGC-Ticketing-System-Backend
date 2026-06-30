using System.Collections.Generic;
using System.Threading.Tasks;
using EGC_Ticketing_System.Models;

namespace EGC_Ticketing_System.Repositories.Interfaces
{
    public interface IUserRateRepository : IGenericRepository<UserRate>
    {
        Task<UserRate?> GetWithDetailsAsync(int id);
        Task<IEnumerable<UserRate>> GetRatesForUserAsync(int userId);
        Task<IEnumerable<UserRate>> GetPendingRatesAsync();
        Task<IEnumerable<UserRate>> GetAllWithDetailsAsync();
    }
}
