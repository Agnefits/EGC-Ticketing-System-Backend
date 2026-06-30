using System;
using System.Threading.Tasks;
using EGC_Ticketing_System.Data;
using EGC_Ticketing_System.Repositories.Classes;
using EGC_Ticketing_System.Repositories.Interfaces;

namespace EGC_Ticketing_System.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private bool _disposed = false;

        public IUserRepository Users { get; private set; }
        public ITeamRepository Teams { get; private set; }
        public ITeamMemberRepository TeamMembers { get; private set; }
        public ITicketRepository Tickets { get; private set; }
        public ILogRepository Logs { get; private set; }
        public ITicketTaskRepository TicketTasks { get; private set; }
        public ITicketStatusHistoryRepository TicketStatusHistories { get; private set; }
        public ITicketTaskStatusHistoryRepository TicketTaskStatusHistories { get; private set; }
        public IUserRateRepository UserRates { get; private set; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Users = new UserRepository(_context);
            Teams = new TeamRepository(_context);
            TeamMembers = new TeamMemberRepository(_context);
            Tickets = new TicketRepository(_context);
            Logs = new LogRepository(_context);
            TicketTasks = new TicketTaskRepository(_context);
            TicketStatusHistories = new TicketStatusHistoryRepository(_context);
            TicketTaskStatusHistories = new TicketTaskStatusHistoryRepository(_context);
            UserRates = new UserRateRepository(_context);
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
