using Telecom.Models;

namespace Telecom.Interfaces
{
    public interface IUserService
    {
        Task<User> RegisterUser(User user);
        Task<User> GetUserById(int userId);
        Task<string> Login(string username, string password);
    }
}
