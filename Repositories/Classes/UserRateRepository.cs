using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EGC_Ticketing_System.Data;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.Repositories.Interfaces;

namespace EGC_Ticketing_System.Repositories.Classes
{
    public class UserRateRepository : GenericRepository<UserRate>, IUserRateRepository
    {
        public UserRateRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<UserRate?> GetWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(ur => ur.FromUser)
                .Include(ur => ur.ToUser)
                .Include(ur => ur.ApprovedBy)
                .Include(ur => ur.RateItems)
                .FirstOrDefaultAsync(ur => ur.Id == id);
        }

        public async Task<IEnumerable<UserRate>> GetRatesForUserAsync(int userId)
        {
            return await _dbSet
                .Include(ur => ur.FromUser)
                .Include(ur => ur.ToUser)
                .Include(ur => ur.ApprovedBy)
                .Include(ur => ur.RateItems)
                .Where(ur => ur.ToUserId == userId && ur.IsApproved)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserRate>> GetPendingRatesAsync()
        {
            return await _dbSet
                .Include(ur => ur.FromUser)
                .Include(ur => ur.ToUser)
                .Include(ur => ur.RateItems)
                .Where(ur => !ur.IsApproved)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserRate>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(ur => ur.FromUser)
                .Include(ur => ur.ToUser)
                .Include(ur => ur.ApprovedBy)
                .Include(ur => ur.RateItems)
                .ToListAsync();
        }
    }
}
