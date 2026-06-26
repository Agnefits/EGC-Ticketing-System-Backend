using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EGC_Ticketing_System.Data;
using EGC_Ticketing_System.Models;
using EGC_Ticketing_System.Enums;
using EGC_Ticketing_System.Repositories.Interfaces;

namespace EGC_Ticketing_System.Repositories.Classes
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByPhoneNumberAsync(string phoneNumber)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _dbSet.AnyAsync(u => u.Email == email && u.Status != UserStatus.Deleted);
        }

        public async Task<bool> ExistsByPhoneAsync(string phoneNumber)
        {
            return await _dbSet.AnyAsync(u => u.PhoneNumber == phoneNumber && u.Status != UserStatus.Deleted);
        }

        public async Task<bool> ExistsByUsernameAsync(string username)
        {
            return await _dbSet.AnyAsync(u => u.Username == username && u.Status != UserStatus.Deleted);
        }

        public async Task<bool> ExistsByEmailExceptAsync(int id, string email)
        {
            return await _dbSet.AnyAsync(u => u.Email == email && u.Id != id && u.Status != UserStatus.Deleted);
        }

        public async Task<bool> ExistsByPhoneExceptAsync(int id, string phoneNumber)
        {
            return await _dbSet.AnyAsync(u => u.PhoneNumber == phoneNumber && u.Id != id && u.Status != UserStatus.Deleted);
        }

        public async Task<bool> ExistsByUsernameExceptAsync(int id, string username)
        {
            return await _dbSet.AnyAsync(u => u.Username == username && u.Id != id && u.Status != UserStatus.Deleted);
        }
    }
}
