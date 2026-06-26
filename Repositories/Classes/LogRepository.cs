using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EGC_Ticketing_System.Data;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.Repositories.Interfaces;

namespace EGC_Ticketing_System.Repositories.Classes
{
    public class LogRepository : GenericRepository<Log>, ILogRepository
    {
        public LogRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Log>> GetLogsAsync(
            int? userId,
            string? action,
            string? entityName,
            string? entityId,
            DateTime? startDate,
            DateTime? endDate,
            int skip,
            int limit)
        {
            var query = _dbSet.Include(l => l.User).AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(l => l.UserId == userId.Value);
            }

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(l => l.Action.Contains(action));
            }

            if (!string.IsNullOrEmpty(entityName))
            {
                query = query.Where(l => l.EntityName.Contains(entityName));
            }

            if (!string.IsNullOrEmpty(entityId))
            {
                query = query.Where(l => l.EntityId == entityId);
            }

            if (startDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt <= endDate.Value);
            }

            return await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<Log?> GetLogWithUserAsync(int id)
        {
            return await _dbSet
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<int> GetLogsCountAsync(
            int? userId,
            string? action,
            string? entityName,
            string? entityId,
            DateTime? startDate,
            DateTime? endDate)
        {
            var query = _dbSet.AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(l => l.UserId == userId.Value);
            }

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(l => l.Action.Contains(action));
            }

            if (!string.IsNullOrEmpty(entityName))
            {
                query = query.Where(l => l.EntityName.Contains(entityName));
            }

            if (!string.IsNullOrEmpty(entityId))
            {
                query = query.Where(l => l.EntityId == entityId);
            }

            if (startDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt <= endDate.Value);
            }

            return await query.CountAsync();
        }
    }
}
