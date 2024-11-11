using Telecom.Models;
using Telecom.Interfaces;

namespace Telecom.Services
{
    public class UserService : IUserService
    {
        public Task<User> GetUserById(int userId)
        {
            throw new NotImplementedException();
        }

        public Task<string> Login(string username, string password)
        {
            throw new NotImplementedException();
        }

        public Task<User> RegisterUser(User user)
        {

            throw new NotImplementedException();
        }
    }
}
