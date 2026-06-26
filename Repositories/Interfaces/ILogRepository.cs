using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EGC_Ticketing_System.Models;

namespace EGC_Ticketing_System.Repositories.Interfaces
{
    public interface ILogRepository : IGenericRepository<Log>
    {
        Task<IEnumerable<Log>> GetLogsAsync(
            int? userId,
            string? action,
            string? entityName,
            string? entityId,
            DateTime? startDate,
            DateTime? endDate,
            int skip,
            int limit);

        Task<Log?> GetLogWithUserAsync(int id);

        Task<int> GetLogsCountAsync(
            int? userId,
            string? action,
            string? entityName,
            string? entityId,
            DateTime? startDate,
            DateTime? endDate);
    }
}
