using System;
using System.Threading.Tasks;
using EGC_Ticketing_System.Repositories.Interfaces;

namespace EGC_Ticketing_System.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        ITeamRepository Teams { get; }
        ITeamMemberRepository TeamMembers { get; }
        ITicketRepository Tickets { get; }
        ILogRepository Logs { get; }
        ITicketTaskRepository TicketTasks { get; }
        ITicketStatusHistoryRepository TicketStatusHistories { get; }
        ITicketTaskStatusHistoryRepository TicketTaskStatusHistories { get; }
        IUserRateRepository UserRates { get; }
        Task<int> CompleteAsync();
    }
}
