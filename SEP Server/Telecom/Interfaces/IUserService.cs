using Telecom.Models;
using Telecom.DTO;

namespace Telecom.Interfaces
{
    public interface IUserService
    {
        Task<User> RegisterUser(User user);
        Task<User> GetUserById(int userId);
        Task<LoginResponse> Login(string username, string password);
    }
}
