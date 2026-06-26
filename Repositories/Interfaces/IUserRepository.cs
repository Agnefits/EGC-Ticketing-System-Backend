using System.Threading.Tasks;
using EGC_Ticketing_System.Models;

namespace EGC_Ticketing_System.Repositories.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByPhoneNumberAsync(string phoneNumber);
        
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsByPhoneAsync(string phoneNumber);
        Task<bool> ExistsByUsernameAsync(string username);
        Task<bool> ExistsByEmailExceptAsync(int id, string email);
        Task<bool> ExistsByPhoneExceptAsync(int id, string phoneNumber);
        Task<bool> ExistsByUsernameExceptAsync(int id, string username);
    }
}
